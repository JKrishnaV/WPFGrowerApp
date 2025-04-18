using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class FileImportService : IFileImportService
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

        public FileImportService(IGrowerService growerService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
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

        public async Task<IEnumerable<Receipt>> ReadReceiptsFromFileAsync(
            string filePath,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Info($"Reading receipts from file: {filePath}");
                var receipts = new List<Receipt>();
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
                    if (!decimal.TryParse(growerIdStr, out var growerNumber) ||
                        !decimal.TryParse(netStr, out var net) ||
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


                    var receipt = new Receipt
                    {
                        Depot = depot,
                        ReceiptNumber = ticketNumber, // Use parsed TICKETNO
                        Product = productId,
                        GrowerNumber = growerNumber,
                        Net = net,
                        ThePrice = price,
                        DockPercent = dockPercent, // Parse DOCKPERCENT
                        // Grade and Process are derived later in ReceiptService
                        Date = parsedReceiptDateTime, // Use renamed variable
                        FromField = fieldId,
                        Imported = true,
                        DayUniq = await GenerateDayUniqAsync(receiptDateTime), // Consider if DayUniq needs adjustment

                        // Add new fields
                        TimeIn = timeInStr, // Store raw TimeIn string
                        GradeId = gradeId, // Store raw GradeId string
                        Voided = voidedStr, // Store raw Voided string
                        AddDate = addDate,
                        AddBy = addBy,
                        EditDate = editDate,
                        EditBy = editBy,
                        EditReason = editReason,
                        ContainerData = new List<ContainerInfo>() // Initialize container list
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
                return receipts;
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
            if (string.IsNullOrEmpty(gradeId) || gradeId.Length < 2)
            {
                throw new ArgumentException("Invalid gradeId format", nameof(gradeId));
            }
            if (int.TryParse(gradeId.Substring(2), out int grade))
            {
                return (grade, gradeId.Substring(0, 2));
            }

            // Keep original logic, but ensure it handles potential null/empty gradeId gracefully if needed
            if (string.IsNullOrEmpty(gradeId) || gradeId.Length < 2)
            {
                 // Return default or handle as needed, maybe log a warning
                 return (0, string.Empty);
                // Or throw: throw new ArgumentException("Invalid gradeId format", nameof(gradeId));
            }
            // Use TryParse for robustness
            if (int.TryParse(gradeId.Substring(gradeId.Length - 1), out int parsedGrade)) // Renamed grade
            {
                return (parsedGrade, gradeId.Substring(0, 2)); // First two chars for process
            }

            // Return default or handle as needed if parsing fails
            return (0, gradeId.Substring(0, 2)); // Keep original return structure for default
            // Or throw: throw new ArgumentException("Invalid gradeId format", nameof(gradeId));
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

                    // Validate grower exists
                    var grower = await _growerService.GetGrowerByNumberAsync(receipt.GrowerNumber);
                    if (grower == null)
                    {
                        errors.Add($"Invalid grower number: {receipt.GrowerNumber}");
                    }

                    // Validate weights
                    if (receipt.Gross <= 0)
                    {
                        errors.Add($"Invalid gross weight for receipt: {receipt.GrowerNumber}");
                    }

                    if (receipt.Net <= 0 || receipt.Net > receipt.Gross)
                    {
                        errors.Add($"Invalid net weight for receipt: {receipt.GrowerNumber}");
                    }

                    // Validate dates
                    if (receipt.Date > DateTime.Now)
                    {
                        errors.Add($"Future date not allowed: {receipt.Date} for receipt: {receipt.GrowerNumber}");
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
    }
}
