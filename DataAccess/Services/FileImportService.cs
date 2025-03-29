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
            "DEPOT", "PRODUCT", "NUMBER", "GROSS", "TARE", "NET",
            "GRADE", "PROCESS", "DATE", "FROM_FIELD"
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

                    var headers = headerLine.Split(',').Select(h => h.Trim().ToUpper()).ToArray();
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
                var headers = lines[0].Split(',').Select(h => h.Trim().ToUpper()).ToArray();

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

                    var values = line.Split(',').Select(v => v.Trim()).ToArray();
                    var receipt = new Receipt
                    {
                        Depot = GetValue(values, headerIndexes, "DEPOT"),
                        Product = GetValue(values, headerIndexes, "PRODUCT"),
                        GrowerNumber = decimal.Parse(GetValue(values, headerIndexes, "NUMBER")),
                        Gross = decimal.Parse(GetValue(values, headerIndexes, "GROSS")),
                        Tare = decimal.Parse(GetValue(values, headerIndexes, "TARE")),
                        Net = decimal.Parse(GetValue(values, headerIndexes, "NET")),
                        Grade = decimal.Parse(GetValue(values, headerIndexes, "GRADE")),
                        Process = GetValue(values, headerIndexes, "PROCESS"),
                        Date = DateTime.Parse(GetValue(values, headerIndexes, "DATE")),
                        FromField = GetValue(values, headerIndexes, "FROM_FIELD"),
                        Imported = true,
                        DayUniq = await GenerateDayUniqAsync(DateTime.Parse(GetValue(values, headerIndexes, "DATE")))
                    };

                    receipts.Add(receipt);
                    progress?.Report((int)((i - 1) * 100.0 / totalLines));
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
            if (headerIndexes.TryGetValue(columnName, out int index) && index < values.Length)
            {
                return values[index];
            }
            throw new InvalidOperationException($"Column {columnName} not found or value missing");
        }

        private async Task<decimal> GenerateDayUniqAsync(DateTime date)
        {
            // This should be implemented based on your business logic for generating unique daily identifiers
            // For now, returning a timestamp-based value
            return decimal.Parse(date.ToString("yyyyMMdd"));
        }
    }
} 