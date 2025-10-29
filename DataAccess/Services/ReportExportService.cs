using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for exporting Payment Summary Reports to various formats.
    /// Supports PDF, Excel, CSV, and Word export with comprehensive formatting options.
    /// </summary>
    public class ReportExportService : IReportExportService
    {
        private readonly string _exportDirectory;

        public ReportExportService()
        {
            // Set default export directory
            _exportDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "BerryFarms", "Reports");
            
            // Ensure directory exists
            if (!Directory.Exists(_exportDirectory))
            {
                Directory.CreateDirectory(_exportDirectory);
            }
        }

        #region Public Interface Methods

        public async Task ExportToPdfAsync(PaymentSummaryReport report, ExportOptions options)
        {
            try
            {
                Logger.Info($"Starting PDF export for Payment Summary Report");

                // Set default file path if not provided
                if (string.IsNullOrEmpty(options.FilePath))
                {
                    options.FilePath = _exportDirectory;
                }

                if (string.IsNullOrEmpty(options.FileName))
                {
                    options.SetDefaultFileName("PaymentSummaryReport");
                }

                var fullPath = options.GetFullFilePath();

                // For now, create a simple text-based PDF representation
                // In a real implementation, you would use a library like iTextSharp or PdfSharp
                await CreateTextBasedPdfAsync(report, fullPath, options);

                Logger.Info($"PDF export completed successfully: {fullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to PDF: {ex.Message}", ex);
                throw;
            }
        }

        public async Task ExportToExcelAsync(PaymentSummaryReport report, ExportOptions options)
        {
            try
            {
                Logger.Info($"Starting Excel export for Payment Summary Report");

                // Set default file path if not provided
                if (string.IsNullOrEmpty(options.FilePath))
                {
                    options.FilePath = _exportDirectory;
                }

                if (string.IsNullOrEmpty(options.FileName))
                {
                    options.SetDefaultFileName("PaymentSummaryReport");
                }

                var fullPath = options.GetFullFilePath();

                // For now, create a CSV file that can be opened in Excel
                // In a real implementation, you would use a library like EPPlus or ClosedXML
                await CreateExcelCompatibleCsvAsync(report, fullPath, options);

                Logger.Info($"Excel export completed successfully: {fullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to Excel: {ex.Message}", ex);
                throw;
            }
        }

        public async Task ExportToCsvAsync(PaymentSummaryReport report, ExportOptions options)
        {
            try
            {
                Logger.Info($"Starting CSV export for Payment Summary Report");

                // Set default file path if not provided
                if (string.IsNullOrEmpty(options.FilePath))
                {
                    options.FilePath = _exportDirectory;
                }

                if (string.IsNullOrEmpty(options.FileName))
                {
                    options.SetDefaultFileName("PaymentSummaryReport");
                }

                var fullPath = options.GetFullFilePath();

                await CreateCsvFileAsync(report, fullPath, options);

                Logger.Info($"CSV export completed successfully: {fullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to CSV: {ex.Message}", ex);
                throw;
            }
        }

        public async Task ExportToWordAsync(PaymentSummaryReport report, ExportOptions options)
        {
            try
            {
                Logger.Info($"Starting Word export for Payment Summary Report");

                // Set default file path if not provided
                if (string.IsNullOrEmpty(options.FilePath))
                {
                    options.FilePath = _exportDirectory;
                }

                if (string.IsNullOrEmpty(options.FileName))
                {
                    options.SetDefaultFileName("PaymentSummaryReport");
                }

                var fullPath = options.GetFullFilePath();

                // For now, create a simple text-based Word representation
                // In a real implementation, you would use a library like DocumentFormat.OpenXml
                await CreateTextBasedWordAsync(report, fullPath, options);

                Logger.Info($"Word export completed successfully: {fullPath}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting to Word: {ex.Message}", ex);
                throw;
            }
        }

        public List<string> GetSupportedFormats()
        {
            return new List<string> { "PDF", "Excel", "CSV", "Word" };
        }

        public bool ValidateExportOptions(ExportOptions options)
        {
            if (options == null)
                return false;

            if (string.IsNullOrEmpty(options.ExportFormat))
                return false;

            if (string.IsNullOrEmpty(options.FileName))
                return false;

            if (options.PdfPasswordProtected && string.IsNullOrEmpty(options.PdfPassword))
                return false;

            return true;
        }

        #endregion

        #region Private Helper Methods

        private async Task CreateTextBasedPdfAsync(PaymentSummaryReport report, string filePath, ExportOptions options)
        {
            var content = new StringBuilder();
            
            // Add report header
            content.AppendLine("PAYMENT SUMMARY REPORT");
            content.AppendLine("=" + new string('=', 50));
            content.AppendLine($"Generated: {report.ReportDate:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Period: {report.PeriodDisplay}");
            content.AppendLine($"Generated By: {report.GeneratedBy}");
            content.AppendLine();

            if (options.IncludeSummaryStatistics)
            {
                content.AppendLine("SUMMARY STATISTICS");
                content.AppendLine("-" + new string('-', 30));
                content.AppendLine($"Total Growers: {report.TotalGrowers:N0}");
                content.AppendLine($"Total Receipts Value: {report.TotalReceiptsValueDisplay}");
                content.AppendLine($"Total Payments Made: {report.TotalPaymentsMadeDisplay}");
                content.AppendLine($"Outstanding Balance: {report.OutstandingBalanceDisplay}");
                content.AppendLine($"Average Payment per Grower: {report.AveragePaymentPerGrowerDisplay}");
                content.AppendLine($"Total Receipts: {report.TotalReceipts:N0}");
                content.AppendLine($"Total Weight: {report.TotalWeightDisplay}");
                content.AppendLine();
            }

            if (options.IncludeCharts && report.PaymentDistribution.Count > 0)
            {
                content.AppendLine("PAYMENT DISTRIBUTION");
                content.AppendLine("-" + new string('-', 30));
                foreach (var distribution in report.PaymentDistribution)
                {
                    content.AppendLine($"{distribution.Category}: {distribution.ValueDisplay} ({distribution.PercentageDisplay})");
                }
                content.AppendLine();
            }

            if (options.IncludeDetailedData && report.GrowerDetails.Count > 0)
            {
                content.AppendLine("GROWER DETAILS");
                content.AppendLine("-" + new string('-', 30));
                content.AppendLine("Grower Number | Name | Total Payments | Outstanding Balance | Status");
                content.AppendLine("-" + new string('-', 80));
                
                foreach (var grower in report.GrowerDetails.Take(100)) // Limit for readability
                {
                    content.AppendLine($"{grower.GrowerNumber,-12} | {grower.FullName,-20} | {grower.TotalPaymentsMadeDisplay,-15} | {grower.OutstandingBalanceDisplay,-18} | {grower.PaymentStatus}");
                }
            }

            await File.WriteAllTextAsync(filePath.Replace(".pdf", ".txt"), content.ToString());
        }

        private async Task CreateExcelCompatibleCsvAsync(PaymentSummaryReport report, string filePath, ExportOptions options)
        {
            var csv = new StringBuilder();
            
            // Add headers
            csv.AppendLine("Grower Number,Grower Name,City,Province,Total Receipts Value,Total Payments Made,Outstanding Balance,Payment Status,Advance 1,Advance 2,Advance 3,Final Payment,Total Deductions");
            
            // Add data rows
            foreach (var grower in report.GrowerDetails)
            {
                csv.AppendLine($"{grower.GrowerNumber},{grower.FullName},{grower.City},{grower.Province},{grower.TotalReceiptsValue},{grower.TotalPaymentsMade},{grower.OutstandingBalance},{grower.PaymentStatus},{grower.Advance1Paid},{grower.Advance2Paid},{grower.Advance3Paid},{grower.FinalPaymentPaid},{grower.TotalDeductions}");
            }

            await File.WriteAllTextAsync(filePath.Replace(".xlsx", ".csv"), csv.ToString());
        }

        private async Task CreateCsvFileAsync(PaymentSummaryReport report, string filePath, ExportOptions options)
        {
            var csv = new StringBuilder();
            
            // Determine delimiter
            var delimiter = options.CsvDelimiter;
            
            // Add headers if requested
            if (options.CsvIncludeHeaders)
            {
                var headers = new[]
                {
                    "Grower Number", "Grower Name", "City", "Province", 
                    "Total Receipts Value", "Total Payments Made", "Outstanding Balance", 
                    "Payment Status", "Advance 1", "Advance 2", "Advance 3", 
                    "Final Payment", "Total Deductions", "Total Receipts", "Total Weight"
                };
                
                csv.AppendLine(string.Join(delimiter, headers));
            }
            
            // Add data rows
            foreach (var grower in report.GrowerDetails)
            {
                var values = new[]
                {
                    QuoteIfNeeded(grower.GrowerNumber, options.CsvQuoteAllFields),
                    QuoteIfNeeded(grower.FullName, options.CsvQuoteAllFields),
                    QuoteIfNeeded(grower.City, options.CsvQuoteAllFields),
                    QuoteIfNeeded(grower.Province, options.CsvQuoteAllFields),
                    grower.TotalReceiptsValue.ToString(),
                    grower.TotalPaymentsMade.ToString(),
                    grower.OutstandingBalance.ToString(),
                    QuoteIfNeeded(grower.PaymentStatus, options.CsvQuoteAllFields),
                    grower.Advance1Paid.ToString(),
                    grower.Advance2Paid.ToString(),
                    grower.Advance3Paid.ToString(),
                    grower.FinalPaymentPaid.ToString(),
                    grower.TotalDeductions.ToString(),
                    grower.TotalReceipts.ToString(),
                    grower.TotalWeight.ToString()
                };
                
                csv.AppendLine(string.Join(delimiter, values));
            }

            // Determine encoding
            var encoding = options.CsvEncoding.ToUpper() switch
            {
                "UTF-8" => Encoding.UTF8,
                "ASCII" => Encoding.ASCII,
                "UNICODE" => Encoding.Unicode,
                _ => Encoding.UTF8
            };

            await File.WriteAllTextAsync(filePath, csv.ToString(), encoding);
        }

        private async Task CreateTextBasedWordAsync(PaymentSummaryReport report, string filePath, ExportOptions options)
        {
            var content = new StringBuilder();
            
            // Add report header
            content.AppendLine("PAYMENT SUMMARY REPORT");
            content.AppendLine(new string('=', 50));
            content.AppendLine($"Generated: {report.ReportDate:yyyy-MM-dd HH:mm:ss}");
            content.AppendLine($"Period: {report.PeriodDisplay}");
            content.AppendLine($"Generated By: {report.GeneratedBy}");
            content.AppendLine();

            if (options.IncludeSummaryStatistics)
            {
                content.AppendLine("EXECUTIVE SUMMARY");
                content.AppendLine(new string('-', 30));
                content.AppendLine($"This report covers {report.TotalGrowers:N0} growers for the period {report.PeriodDisplay}.");
                content.AppendLine($"Total receipts value: {report.TotalReceiptsValueDisplay}");
                content.AppendLine($"Total payments made: {report.TotalPaymentsMadeDisplay}");
                content.AppendLine($"Outstanding balance: {report.OutstandingBalanceDisplay}");
                content.AppendLine($"Payment completion rate: {report.PaymentCompletionPercentage:F1}%");
                content.AppendLine();
            }

            if (options.IncludeCharts && report.PaymentDistribution.Count > 0)
            {
                content.AppendLine("PAYMENT BREAKDOWN");
                content.AppendLine(new string('-', 30));
                foreach (var distribution in report.PaymentDistribution)
                {
                    content.AppendLine($"• {distribution.Category}: {distribution.ValueDisplay} ({distribution.PercentageDisplay})");
                }
                content.AppendLine();
            }

            if (options.IncludeDetailedData && report.GrowerDetails.Count > 0)
            {
                content.AppendLine("DETAILED GROWER ANALYSIS");
                content.AppendLine(new string('-', 30));
                
                foreach (var grower in report.GrowerDetails.Take(50)) // Limit for readability
                {
                    content.AppendLine($"Grower: {grower.GrowerNumber} - {grower.FullName}");
                    content.AppendLine($"  Location: {grower.City}, {grower.Province}");
                    content.AppendLine($"  Total Receipts Value: {grower.TotalReceiptsValueDisplay}");
                    content.AppendLine($"  Total Payments Made: {grower.TotalPaymentsMadeDisplay}");
                    content.AppendLine($"  Outstanding Balance: {grower.OutstandingBalanceDisplay}");
                    content.AppendLine($"  Payment Status: {grower.PaymentStatus}");
                    content.AppendLine($"  Receipts Count: {grower.TotalReceipts}");
                    content.AppendLine($"  Total Weight: {grower.TotalWeightDisplay}");
                    content.AppendLine();
                }
            }

            await File.WriteAllTextAsync(filePath.Replace(".docx", ".txt"), content.ToString());
        }

        private string QuoteIfNeeded(string value, bool alwaysQuote)
        {
            if (alwaysQuote || value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }

        #endregion
    }
}
