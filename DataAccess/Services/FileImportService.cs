using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class FileImportService : BaseDatabaseService, IFileImportService
    {
        private readonly IGrowerService _growerService;
        private readonly string[] _expectedHeaders = new[]
        {
            "SITEID", "TICKETNO", "VOIDED", "BATCHNO", "PRODUCTID", 
            "GRADEID", "DATEIN", "TIMEIN", "GROWERID", "PRICE", 
            "DOCKPERCENT", "NET", "ADD DATE", "ADD BY", "EDIT DATE", 
            "EDIT BY", "EDIT REASON"
            //, "FIELDID"
        };

        // Cache for lookups to improve performance during bulk import
        private Dictionary<string, int> _productLookupCache;
        private Dictionary<string, int> _processLookupCache;
        private Dictionary<string, int> _depotLookupCache;
        private Dictionary<string, int> _growerLookupCache;

        public FileImportService(IGrowerService growerService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _productLookupCache = new Dictionary<string, int>();
            _processLookupCache = new Dictionary<string, int>();
            _depotLookupCache = new Dictionary<string, int>();
            _growerLookupCache = new Dictionary<string, int>();
        }

        public async Task<bool> ValidateFileFormatAsync(string filePath)
        {
            try
            {
                Logger.Info($"Validating file format: {filePath}");

                if (!File.Exists(filePath))
                {
                    Logger.Error($"File not found: {filePath}");
                    return false;
                }

                using (var reader = new StreamReader(filePath))
                {
                    // Read header line
                    var headerLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        Logger.Error("File is empty");
                        return false;
                    }

                    // Remove quotes and split by comma
                    var headers = headerLine.Replace("\"", "").Split(',')
                        .Select(h => h.Trim().ToUpper())
                        .ToArray();

                    var missingHeaders = _expectedHeaders.Except(headers).ToList();

                    if (missingHeaders.Any())
                    {
                        Logger.Error($"Missing required headers: {string.Join(", ", missingHeaders)}");
                        return false;
                    }

                    // Validate at least one data row exists
                    var firstDataLine = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(firstDataLine))
                    {
                        Logger.Error("File contains no data rows");
                        return false;
                    }
                }

                Logger.Info("File format validation successful");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating file format", ex);
                throw;
            }
        }

        public async Task<(IEnumerable<Receipt> receipts, List<string> errors)> ReadReceiptsFromFileAsync(
            string filePath,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Info($"Reading receipts from file: {filePath}");
                var receipts = new List<Receipt>();
                var errors = new List<string>();
                var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
                
                // Remove quotes and split by comma for headers
                var headers = lines[0].Replace("\"", "").Split(',')
                    .Select(h => h.Trim().ToUpper())
                    .ToArray();

                // Create header index mapping
                var headerIndexes = new Dictionary<string, int>();
                for (int i = 0; i < headers.Length; i++)
                {
                    headerIndexes[headers[i]] = i;
                }

                var totalLines = lines.Length - 1; // Exclude header
                for (int i = 1; i < lines.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Remove quotes and split by comma
                    var values = line.Replace("\"", "").Split(',')
                        .Select(v => v.Trim())
                        .ToArray();

                    //commented by Jay
                    //// Skip empty receipts (those with no product and zero NET)
                    //if (string.IsNullOrEmpty(GetValue(values, headerIndexes, "PRODUCTID")) &&
                    //    decimal.Parse(GetValue(values, headerIndexes, "NET")) == 0)
                    //{
                    //    continue;
                    //}

                    var dateIn = DateTime.Parse(GetValue(values, headerIndexes, "DATEIN"));
                    var timeIn = TimeSpan.Parse(GetValue(values, headerIndexes, "TIMEIN"));
                    var receiptDateTime = dateIn.Add(timeIn);
                    // Parse basic fields
                    var depot = GetValueSafe(values, headerIndexes, "SITEID");
                    var productId = GetValueSafe(values, headerIndexes, "PRODUCTID");
                    var growerIdStr = GetValueSafe(values, headerIndexes, "GROWERID");
                    var netStr = GetValueSafe(values, headerIndexes, "NET");
                    var priceStr = GetValueSafe(values, headerIndexes, "PRICE");
                    var fieldId = GetValueSafe(values, headerIndexes, "FIELDID");
                    var ticketNoStr = GetValueSafe(values, headerIndexes, "TICKETNO");
                    var timeInStr = GetValueSafe(values, headerIndexes, "TIMEIN"); // Get raw TimeIn
                    var gradeId = GetValueSafe(values, headerIndexes, "GRADEID"); // Get raw GradeId
                    var voidedStr = GetValueSafe(values, headerIndexes, "VOIDED"); // Get raw Voided
                    var addDateStr = GetValueSafe(values, headerIndexes, "ADD DATE");
                    var addBy = GetValueSafe(values, headerIndexes, "ADD BY");
                    var editDateStr = GetValueSafe(values, headerIndexes, "EDIT DATE");
                    var editBy = GetValueSafe(values, headerIndexes, "EDIT BY");
                    var editReason = GetValueSafe(values, headerIndexes, "EDIT REASON");
                    var dockPercentStr = GetValueSafe(values, headerIndexes, "DOCKPERCENT");

                    // Basic validation and parsing
                    var growerNumber = growerIdStr;
                    if (!decimal.TryParse(netStr, out var net) ||
                        !decimal.TryParse(priceStr, out var price) ||
                        !decimal.TryParse(ticketNoStr, out var ticketNumber) ||
                        !decimal.TryParse(dockPercentStr, out var dockPercent))
                    {
                        Logger.Warn($"Skipping line {i + 1} due to parsing error for basic numeric fields.");
                        continue; // Skip row if essential numbers can't be parsed
                    }

                    // Removed the check that skipped empty receipts

                    // Parse Date and Time
                    if (!DateTime.TryParse(GetValueSafe(values, headerIndexes, "DATEIN"), out var parsedDateIn) || // Renamed dateIn
                        !TimeSpan.TryParse(timeInStr, out var timeInSpan)) // Use timeInStr
                    {
                         Logger.Warn($"Skipping line {i + 1} due to parsing error for date/time fields.");
                         continue; // Skip row if date/time can't be parsed
                    }
                    var parsedReceiptDateTime = parsedDateIn.Add(timeInSpan); // Renamed receiptDateTime

                    // Parse Audit Dates (handle potential nulls/empty strings)
                    DateTime.TryParse(addDateStr, out var addDateParsed);
                    DateTime? addDate = string.IsNullOrWhiteSpace(addDateStr) ? null : addDateParsed;
                    DateTime.TryParse(editDateStr, out var editDateParsed);
                    DateTime? editDate = string.IsNullOrWhiteSpace(editDateStr) ? null : editDateParsed;


                    // Parse grade and process from GradeId (e.g., "EL1" → Process="EL", Grade=1)
                    var (grade, processCode) = ExtractGradeAndProcessID(gradeId);

                    // Lookup foreign keys (with caching for performance)
                    int? growerId = null;
                    int? priceClassId = null;
                    int? productIdFk = null;
                    int? processIdFk = null;
                    int? depotIdFk = null;

                    try
                    {
                        var growerInfo = await GetGrowerIdByNumberAsync(growerNumber);
                        growerId = growerInfo.GrowerId;
                        priceClassId = growerInfo.DefaultPriceClassId;
                        productIdFk = string.IsNullOrEmpty(productId) ? null : await GetProductIdByCodeAsync(productId);
                        processIdFk = string.IsNullOrEmpty(processCode) ? null : await GetProcessIdByCodeAsync(processCode);
                        depotIdFk = string.IsNullOrEmpty(depot) ? null : await GetDepotIdByCodeAsync(depot);
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Line {i + 1}: {ex.Message}";
                        errors.Add(errorMsg);
                        Logger.Warn($"Skipping line {i + 1} due to lookup error: {ex.Message}");
                        continue;
                    }

                    var receipt = new Receipt
                    {
                        // Legacy properties (for backward compatibility)
                        Depot = depot,
                        Product = productId,
                        GrowerNumber = growerNumber,
                        Gross = net, // CSV only has NET, no separate GROSS/TARE
                        Tare = 0,
                        Net = net,
                        ThePrice = price,
                        DockPercent = dockPercent,
                        // Grade = gradeId, // Removed - type conflict
                        Process = processCode,
                        ReceiptDate = parsedReceiptDateTime,
                        FromField = fieldId,
                        Imported = true,
                        DayUniq = await GenerateDayUniqAsync(parsedReceiptDateTime),
                        TimeIn = timeInStr,
                        GradeId = gradeId,
                        Voided = voidedStr,
                        AddDate = addDate,
                        AddBy = addBy,
                        EditDate = editDate,
                        EditBy = editBy,
                        EditReason = editReason,

                        // MODERN PROPERTIES (for new Receipts table)
                        ReceiptNumber = ticketNumber.ToString(),
                        // Removed duplicate ReceiptDate assignment
                        ReceiptTime = timeInSpan,
                        GrowerId = growerId ?? 0,
                        ProductId = productIdFk ?? 0,
                        ProcessId = processIdFk ?? 0,
                        DepotId = depotIdFk ?? 0,
                        GrossWeight = net, // CSV only has NET
                        TareWeight = 0,
                        DockPercentage = dockPercent,
                        Grade = (byte)grade,
                        PriceClassId = priceClassId ?? 1, // From Grower.DefaultPriceClassId
                        IsVoided = !string.IsNullOrEmpty(voidedStr) && voidedStr.ToUpper() == "VOID",
                        VoidedReason = string.IsNullOrEmpty(voidedStr) || string.IsNullOrEmpty(editReason) ? string.Empty : editReason,

                        // ImportBatchId will be set in ImportBatchProcessor.ProcessSingleReceiptAsync
                        ImportBatchId = null,

                        // Container data
                        ContainerData = new List<ContainerInfo>()
                    };

                    // Parse Container Data (CONT1, IN1, OUT1, CONT2, IN2, OUT2...)
                    for (int j = 1; j <= 12; j++) // Assuming max 12 containers based on CSV sample header
                    {
                        var contType = GetValueSafe(values, headerIndexes, $"CONT{j}");
                        if (!string.IsNullOrEmpty(contType))
                        {
                            var inCountStr = GetValueSafe(values, headerIndexes, $"IN{j}");
                            var outCountStr = GetValueSafe(values, headerIndexes, $"OUT{j}");

                            int.TryParse(inCountStr, out var inCount);
                            int.TryParse(outCountStr, out var outCount);

                            receipt.ContainerData.Add(new ContainerInfo
                            {
                                Type = contType,
                                InCount = inCount,
                                OutCount = outCount
                            });
                        }
                        else
                        {
                            // Stop parsing containers if CONTx is empty
                            break;
                        }
                    }


                    receipts.Add(receipt);
                    progress?.Report((int)((i) * 100.0 / totalLines)); // Corrected progress calculation index
                }

                progress?.Report(100);
                Logger.Info($"Successfully read {receipts.Count} receipts from file");
                if (errors.Any())
                {
                    Logger.Warn($"Skipped {errors.Count} receipts due to validation errors");
                }
                return (receipts, errors);
            }
            catch (Exception ex)
            {
                Logger.Error("Error reading receipts from file", ex);
                throw;
            }
        }
        //how to use this method?   
        private (int Grade, string ProcessID) ExtractGradeAndProcessID(string gradeId)
        {
            // Handle empty or null gradeId - return default values
            if (string.IsNullOrEmpty(gradeId))
            {
                return (0, string.Empty);
            }
            
            // Handle short gradeId (less than 2 characters)
            if (gradeId.Length < 2)
            {
                return (0, gradeId);
            }
            
            // Try to parse grade number from characters after position 2
            // Example: "EL1" → Process="EL", Grade=1
            if (int.TryParse(gradeId.Substring(2), out int grade))
            {
                return (grade, gradeId.Substring(0, 2));
            }

            // If parsing fails, return process code with default grade
            return (0, gradeId.Substring(0, 2));
        }

        public async Task<(bool IsValid, List<string> Errors)> ValidateReceiptsAsync(
            IEnumerable<Receipt> receipts,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Info("Starting receipt validation");
                var errors = new List<string>();
                var receiptList = receipts.ToList();
                var totalReceipts = receiptList.Count;
                var processedCount = 0;

                foreach (var receipt in receiptList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Validate foreign keys are set
                    if (receipt.GrowerId <= 0)
                    {
                        errors.Add($"Invalid or missing grower for receipt {receipt.ReceiptNumber}: {receipt.GrowerNumber}");
                    }

                    if (receipt.ProductId <= 0 && !string.IsNullOrEmpty(receipt.Product))
                    {
                        errors.Add($"Invalid or missing product for receipt {receipt.ReceiptNumber}: {receipt.Product}");
                    }

                    // Validate weights (using modern properties)
                    if (receipt.GrossWeight <= 0)
                    {
                        errors.Add($"Invalid gross weight for receipt {receipt.ReceiptNumber}: {receipt.GrossWeight}");
                    }

                    // Validate dates
                    if (receipt.ReceiptDate > DateTime.Now.Date)
                    {
                        errors.Add($"Future date not allowed: {receipt.ReceiptDate} for receipt: {receipt.ReceiptNumber}");
                    }

                    processedCount++;
                    progress?.Report((int)(processedCount * 100.0 / totalReceipts));
                }

                var isValid = !errors.Any();
                Logger.Info($"Receipt validation completed. Valid: {isValid}, Error count: {errors.Count}");
                return (isValid, errors);
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating receipts", ex);
                throw;
            }
        }

        public string GetFileFormatSpecification()
        {
            return @"Expected file format:
- CSV file with header row
- Required columns: DEPOT, PRODUCT, NUMBER, GROSS, TARE, NET, GRADE, PROCESS, DATE, FROM_FIELD
- DATE format: yyyy-MM-dd
- Numeric fields should not contain currency symbols or thousand separators
- All fields should be comma-separated
- Text fields may be optionally enclosed in double quotes";
        }

        private string GetValue(string[] values, Dictionary<string, int> headerIndexes, string columnName)
        {
            // Combine the two TryGetValue calls and rename the index variable
            if (headerIndexes.TryGetValue(columnName.ToUpper(), out int foundIndex) && foundIndex < values.Length)
            {
                return values[foundIndex];
            }
            // Logger.Warn($"Column {columnName} not found or value missing in CSV line.");
            return null; // Or string.Empty
        }

        // Helper function for safer parsing
        private string GetValueSafe(string[] values, Dictionary<string, int> headerIndexes, string columnName)
        {
            if (headerIndexes.TryGetValue(columnName.ToUpper(), out int index) && index < values.Length)
            {
                return values[index];
            }
            // Return empty string if column not found or value missing to avoid null reference issues downstream
            // Log a warning if this is unexpected
            // Logger.Warn($"Column '{columnName}' not found or value missing in CSV line.");
            return string.Empty;
        }


        private async Task<decimal> GenerateDayUniqAsync(DateTime date)
        {
            // This should be implemented based on your business logic for generating unique daily identifiers
            // For now, returning a timestamp-based value
            return decimal.Parse(date.ToString("yyyyMMdd"));
        }

        #region Foreign Key Lookup Methods with Caching

        private async Task<(int? GrowerId, int? DefaultPriceClassId)> GetGrowerIdByNumberAsync(string growerNumber)
        {
            var key = growerNumber;
            if (_growerLookupCache.TryGetValue(key, out var cachedId))
            {
                // Return cached GrowerId and fetch DefaultPriceClassId separately
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    const string priceClassSql = "SELECT DefaultPriceClassId FROM Growers WHERE GrowerId = @GrowerId AND DeletedAt IS NULL";
                    var priceClassId = await connection.ExecuteScalarAsync<int?>(priceClassSql, new { GrowerId = cachedId });
                    return (cachedId, priceClassId ?? 1); // Default to 1 (CL1) if not set
                }
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = "SELECT GrowerId, DefaultPriceClassId FROM Growers WHERE GrowerNumber = @Number AND DeletedAt IS NULL";
                var result = await connection.QueryFirstOrDefaultAsync<(int? GrowerId, int? DefaultPriceClassId)>(sql, new { Number = key });

                if (!result.GrowerId.HasValue)
                {
                    Logger.Warn($"Grower not found: {growerNumber}");
                    throw new InvalidOperationException($"Grower not found: {growerNumber}");
                }

                _growerLookupCache[key] = result.GrowerId.Value;
                return (result.GrowerId, result.DefaultPriceClassId ?? 1); // Default to 1 (CL1) if not set
            }
        }

        private async Task<int?> GetProductIdByCodeAsync(string productCode)
        {
            if (string.IsNullOrEmpty(productCode))
                return null;

            var key = productCode.ToUpper();
            if (_productLookupCache.TryGetValue(key, out var cachedId))
                return cachedId;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = "SELECT ProductId FROM Products WHERE ProductCode = @Code AND IsActive = 1";
                var result = await connection.ExecuteScalarAsync<int?>(sql, new { Code = key });

                if (!result.HasValue)
                {
                    Logger.Warn($"Product not found: {productCode}");
                    throw new InvalidOperationException($"Product not found: {productCode}");
                }

                _productLookupCache[key] = result.Value;
                return result.Value;
            }
        }

        private async Task<int?> GetProcessIdByCodeAsync(string processCode)
        {
            if (string.IsNullOrEmpty(processCode))
                return null;

            var key = processCode.ToUpper();
            if (_processLookupCache.TryGetValue(key, out var cachedId))
                return cachedId;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = "SELECT ProcessId FROM Processes WHERE ProcessCode = @Code AND DeletedAt IS NULL";
                var result = await connection.ExecuteScalarAsync<int?>(sql, new { Code = key });

                if (!result.HasValue)
                {
                    Logger.Warn($"Process not found: {processCode}");
                    throw new InvalidOperationException($"Process not found: {processCode}");
                }

                _processLookupCache[key] = result.Value;
                return result.Value;
            }
        }

        private async Task<int?> GetDepotIdByCodeAsync(string depotCode)
        {
            if (string.IsNullOrEmpty(depotCode))
                return null;

            var key = depotCode.ToUpper();
            if (_depotLookupCache.TryGetValue(key, out var cachedId))
                return cachedId;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                const string sql = "SELECT DepotId FROM Depots WHERE DepotCode = @Code AND IsActive = 1";
                var result = await connection.ExecuteScalarAsync<int?>(sql, new { Code = key });

                if (!result.HasValue)
                {
                    Logger.Warn($"Depot not found: {depotCode}");
                    throw new InvalidOperationException($"Depot not found: {depotCode}");
                }

                _depotLookupCache[key] = result.Value;
                return result.Value;
            }
        }

        #endregion
    }
}
