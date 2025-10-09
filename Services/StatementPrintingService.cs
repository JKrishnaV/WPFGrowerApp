using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup; // For IAddChild
using System.Windows.Media;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Service for printing advance and year-end statements
    /// Generates statement documents that accompany payment cheques
    /// </summary>
    public class StatementPrintingService : IStatementPrintingService
    {
        private readonly IPaymentTypeService _paymentTypeService;
        private readonly IGrowerService _growerService;
        private readonly IReceiptService _receiptService;

        public StatementPrintingService(
            IPaymentTypeService paymentTypeService,
            IGrowerService growerService,
            IReceiptService receiptService)
        {
            _paymentTypeService = paymentTypeService;
            _growerService = growerService;
            _receiptService = receiptService;
        }

        /// <summary>
        /// Print advance statement for a grower
        /// </summary>
        public async Task<bool> PrintAdvanceStatementAsync(
            TestRunGrowerPayment growerPayment,
            int advanceNumber,
            int paymentBatchId,
            StatementFormat? format = null)
        {
            try
            {
                format ??= GetDefaultAdvanceStatementFormat();

                // Create statement document
                var document = await CreateAdvanceStatementDocumentAsync(growerPayment, advanceNumber, paymentBatchId, format);

                // Show print dialog
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(document.DocumentPaginator, 
                        $"Advance Statement - Grower {growerPayment.GrowerNumber}");
                    
                    Logger.Info($"Printed advance statement for grower {growerPayment.GrowerNumber}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing advance statement: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Print statements for all growers in a payment batch
        /// </summary>
        public async Task<int> PrintBatchStatementsAsync(
            List<TestRunGrowerPayment> growerPayments,
            int advanceNumber,
            int paymentBatchId,
            StatementFormat? format = null)
        {
            try
            {
                if (growerPayments == null || !growerPayments.Any())
                {
                    return 0;
                }

                format ??= GetDefaultAdvanceStatementFormat();

                // Create combined document with all statements
                var document = new FixedDocument();

                foreach (var growerPayment in growerPayments)
                {
                    var statementDoc = await CreateAdvanceStatementDocumentAsync(growerPayment, advanceNumber, paymentBatchId, format);
                    
                    // Add all pages from this statement to combined document
                    foreach (PageContent page in statementDoc.Pages)
                    {
                        document.Pages.Add(page);
                    }
                }

                // Show print dialog
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(document.DocumentPaginator, 
                        $"Advance Statements - Batch ({growerPayments.Count} growers)");
                    
                    Logger.Info($"Printed {growerPayments.Count} advance statements");
                    return growerPayments.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing batch statements: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generate year-end statement for a grower
        /// </summary>
        public async Task<bool> PrintYearEndStatementAsync(
            GrowerFinalPayment finalPayment,
            StatementFormat? format = null)
        {
            try
            {
                format ??= GetDefaultYearEndStatementFormat();

                // Create statement document
                var document = await CreateYearEndStatementDocumentAsync(finalPayment, format);

                // Show print dialog
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(document.DocumentPaginator, 
                        $"Year-End Statement - Grower {finalPayment.GrowerNumber}");
                    
                    Logger.Info($"Printed year-end statement for grower {finalPayment.GrowerNumber}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing year-end statement: {ex.Message}", ex);
                throw;
            }
        }

        // ==============================================================
        // DOCUMENT CREATION METHODS
        // ==============================================================

        /// <summary>
        /// Create advance statement document
        /// </summary>
        private async Task<FixedDocument> CreateAdvanceStatementDocumentAsync(
            TestRunGrowerPayment growerPayment,
            int advanceNumber,
            int paymentBatchId,
            StatementFormat format)
        {
            var document = new FixedDocument();
            var pageContent = new PageContent();
            var fixedPage = new FixedPage
            {
                Width = 816,  // 8.5" at 96 DPI
                Height = 1056 // 11" at 96 DPI
            };

            double currentY = 50;
            const double lineHeight = 20;
            const double leftMargin = 50;

            // Header
            AddTextBlock(fixedPage, $"ADVANCE PAYMENT STATEMENT - ADVANCE #{advanceNumber}", 
                leftMargin, currentY, 18, FontWeights.Bold);
            currentY += lineHeight * 2;

            // Grower info
            AddTextBlock(fixedPage, $"Grower: #{growerPayment.GrowerNumber} - {growerPayment.GrowerName}", 
                leftMargin, currentY, 14, FontWeights.SemiBold);
            currentY += lineHeight;

            AddTextBlock(fixedPage, $"Payment Batch: {paymentBatchId}", 
                leftMargin, currentY, 12);
            currentY += lineHeight;

            AddTextBlock(fixedPage, $"Statement Date: {DateTime.Today:MMMM dd, yyyy}", 
                leftMargin, currentY, 12);
            currentY += lineHeight * 2;

            // Summary section
            AddTextBlock(fixedPage, "PAYMENT SUMMARY", 
                leftMargin, currentY, 14, FontWeights.Bold);
            currentY += lineHeight * 1.5;

            AddTextBlock(fixedPage, $"Total Receipts: {growerPayment.ReceiptCount}", 
                leftMargin, currentY, 12);
            currentY += lineHeight;

            AddTextBlock(fixedPage, $"Total Weight: {growerPayment.TotalNetWeight:N2} lbs", 
                leftMargin, currentY, 12);
            currentY += lineHeight;

            AddTextBlock(fixedPage, $"Advance Amount: ${growerPayment.TotalCalculatedAdvanceAmount:N2}", 
                leftMargin, currentY, 12, FontWeights.SemiBold);
            currentY += lineHeight;

            if (growerPayment.TotalCalculatedPremiumAmount > 0)
            {
                AddTextBlock(fixedPage, $"Time Premium: ${growerPayment.TotalCalculatedPremiumAmount:N2}", 
                    leftMargin, currentY, 12);
                currentY += lineHeight;
            }

            if (growerPayment.TotalCalculatedDeductionAmount != 0)
            {
                AddTextBlock(fixedPage, $"Marketing Deduction: ${growerPayment.TotalCalculatedDeductionAmount:N2}", 
                    leftMargin, currentY, 12);
                currentY += lineHeight;
            }

            AddTextBlock(fixedPage, $"NET PAYMENT: ${growerPayment.TotalCalculatedPayment:N2}", 
                leftMargin, currentY, 14, FontWeights.Bold);
            currentY += lineHeight * 2;

            // Receipt details header
            if (growerPayment.ReceiptDetails.Any())
            {
                AddTextBlock(fixedPage, "RECEIPT DETAILS", 
                    leftMargin, currentY, 14, FontWeights.Bold);
                currentY += lineHeight * 1.5;

                // Column headers
                AddTextBlock(fixedPage, "Date", leftMargin, currentY, 10, FontWeights.SemiBold);
                AddTextBlock(fixedPage, "Receipt#", leftMargin + 80, currentY, 10, FontWeights.SemiBold);
                AddTextBlock(fixedPage, "Product", leftMargin + 150, currentY, 10, FontWeights.SemiBold);
                AddTextBlock(fixedPage, "Process", leftMargin + 250, currentY, 10, FontWeights.SemiBold);
                AddTextBlock(fixedPage, "Weight", leftMargin + 350, currentY, 10, FontWeights.SemiBold);
                AddTextBlock(fixedPage, "Price", leftMargin + 430, currentY, 10, FontWeights.SemiBold);
                AddTextBlock(fixedPage, "Amount", leftMargin + 510, currentY, 10, FontWeights.SemiBold);
                currentY += lineHeight;

                // Receipt lines (limit to first page for now)
                var receiptsToShow = growerPayment.ReceiptDetails.Take(25);
                foreach (var receipt in receiptsToShow)
                {
                    AddTextBlock(fixedPage, receipt.ReceiptDate.ToString("MM/dd"), leftMargin, currentY, 10);
                    AddTextBlock(fixedPage, receipt.ReceiptNumber.ToString(), leftMargin + 80, currentY, 10);
                    AddTextBlock(fixedPage, receipt.Product, leftMargin + 150, currentY, 10);
                    AddTextBlock(fixedPage, receipt.Process, leftMargin + 250, currentY, 10);
                    AddTextBlock(fixedPage, receipt.NetWeight.ToString("N2"), leftMargin + 350, currentY, 10);
                    AddTextBlock(fixedPage, receipt.CalculatedAdvancePrice.ToString("N4"), leftMargin + 430, currentY, 10);
                    AddTextBlock(fixedPage, receipt.CalculatedAdvanceAmount.ToString("N2"), leftMargin + 510, currentY, 10);
                    currentY += lineHeight;
                }

                if (growerPayment.ReceiptDetails.Count > 25)
                {
                    currentY += lineHeight;
                    AddTextBlock(fixedPage, $"... and {growerPayment.ReceiptDetails.Count - 25} more receipts (see detailed report)", 
                        leftMargin, currentY, 10, FontWeights.Normal);
                }
            }

            ((IAddChild)pageContent).AddChild(fixedPage);
            document.Pages.Add(pageContent);

            return document;
        }

        /// <summary>
        /// Create year-end statement document
        /// </summary>
        private async Task<FixedDocument> CreateYearEndStatementDocumentAsync(
            GrowerFinalPayment finalPayment,
            StatementFormat format)
        {
            var document = new FixedDocument();
            var pageContent = new PageContent();
            var fixedPage = new FixedPage
            {
                Width = 816,
                Height = 1056
            };

            double currentY = 50;
            const double lineHeight = 20;
            const double leftMargin = 50;

            // Header
            AddTextBlock(fixedPage, "YEAR-END PAYMENT STATEMENT", 
                leftMargin, currentY, 18, FontWeights.Bold);
            currentY += lineHeight * 2;

            // Grower info
            AddTextBlock(fixedPage, $"Grower: #{finalPayment.GrowerNumber} - {finalPayment.GrowerName}", 
                leftMargin, currentY, 14, FontWeights.SemiBold);
            currentY += lineHeight * 2;

            // Summary
            AddTextBlock(fixedPage, "YEAR-END SUMMARY", 
                leftMargin, currentY, 14, FontWeights.Bold);
            currentY += lineHeight * 1.5;

            AddTextBlock(fixedPage, $"Total Receipts: {finalPayment.ReceiptCount}", 
                leftMargin, currentY, 12);
            currentY += lineHeight;

            AddTextBlock(fixedPage, $"Total Weight: {finalPayment.TotalNetWeight:N2} lbs", 
                leftMargin, currentY, 12);
            currentY += lineHeight;

            AddTextBlock(fixedPage, $"Total Receipt Value: ${finalPayment.TotalReceiptValue:N2}", 
                leftMargin, currentY, 12, FontWeights.SemiBold);
            currentY += lineHeight * 2;

            // Advances section
            AddTextBlock(fixedPage, "ADVANCES PAID:", 
                leftMargin, currentY, 12, FontWeights.SemiBold);
            currentY += lineHeight;

            if (finalPayment.Advance1Amount > 0)
            {
                AddTextBlock(fixedPage, $"Advance 1: ${finalPayment.Advance1Amount:N2}", 
                    leftMargin + 20, currentY, 12);
                currentY += lineHeight;
            }

            if (finalPayment.Advance2Amount > 0)
            {
                AddTextBlock(fixedPage, $"Advance 2: ${finalPayment.Advance2Amount:N2}", 
                    leftMargin + 20, currentY, 12);
                currentY += lineHeight;
            }

            if (finalPayment.Advance3Amount > 0)
            {
                AddTextBlock(fixedPage, $"Advance 3: ${finalPayment.Advance3Amount:N2}", 
                    leftMargin + 20, currentY, 12);
                currentY += lineHeight;
            }

            AddTextBlock(fixedPage, $"Total Advances: ${finalPayment.TotalAdvancesPaid:N2}", 
                leftMargin, currentY, 12, FontWeights.SemiBold);
            currentY += lineHeight * 2;

            // Deductions section
            if (finalPayment.Deductions.Any())
            {
                AddTextBlock(fixedPage, "DEDUCTIONS:", 
                    leftMargin, currentY, 12, FontWeights.SemiBold);
                currentY += lineHeight;

                foreach (var deduction in finalPayment.Deductions)
                {
                    AddTextBlock(fixedPage, $"{deduction.Description}: ${deduction.Amount:N2}", 
                        leftMargin + 20, currentY, 12);
                    currentY += lineHeight;
                }

                AddTextBlock(fixedPage, $"Total Deductions: ${finalPayment.TotalDeductions:N2}", 
                    leftMargin, currentY, 12, FontWeights.SemiBold);
                currentY += lineHeight * 2;
            }

            // Final payment
            AddTextBlock(fixedPage, $"FINAL PAYMENT: ${finalPayment.CalculatedFinalPayment:N2}", 
                leftMargin, currentY, 16, FontWeights.Bold);
            currentY += lineHeight;

            AddTextBlock(fixedPage, $"NET AMOUNT DUE: ${finalPayment.NetPayment:N2}", 
                leftMargin, currentY, 16, FontWeights.Bold);

            ((IAddChild)pageContent).AddChild(fixedPage);
            document.Pages.Add(pageContent);

            return document;
        }

        /// <summary>
        /// Generate PDF of statement (for archival/emailing)
        /// </summary>
        public async Task<byte[]> GenerateStatementPdfAsync(
            TestRunGrowerPayment growerPayment,
            int advanceNumber,
            int paymentBatchId,
            StatementFormat? format = null)
        {
            // TODO: Implement PDF generation using library like iTextSharp or QuestPDF
            // For now, return empty byte array
            Logger.Warn("PDF generation not yet implemented");
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        // ==============================================================
        // HELPER METHODS
        // ==============================================================

        /// <summary>
        /// Add text block to page at specified position
        /// </summary>
        private void AddTextBlock(FixedPage page, string text, double x, double y, int fontSize, FontWeight? fontWeight = null)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily("Arial"),
                FontSize = fontSize,
                FontWeight = fontWeight ?? FontWeights.Normal
            };

            FixedPage.SetLeft(textBlock, x);
            FixedPage.SetTop(textBlock, y);
            page.Children.Add(textBlock);
        }

        /// <summary>
        /// Get default advance statement format
        /// </summary>
        private StatementFormat GetDefaultAdvanceStatementFormat()
        {
            return new StatementFormat
            {
                FormatName = "Standard Advance Statement",
                Description = "Default format for advance payment statements",
                IncludeReceiptDetails = true,
                IncludeContainerSummary = false, // Add container tracking later
                PaperSize = "Letter",
                Orientation = "Portrait"
            };
        }

        /// <summary>
        /// Get default year-end statement format
        /// </summary>
        private StatementFormat GetDefaultYearEndStatementFormat()
        {
            return new StatementFormat
            {
                FormatName = "Standard Year-End Statement",
                Description = "Default format for year-end statements",
                IncludeReceiptDetails = true,
                IncludeContainerSummary = true,
                IncludeAdvanceHistory = true,
                PaperSize = "Letter",
                Orientation = "Portrait"
            };
        }
    }

    // ==============================================================
    // SUPPORTING MODELS
    // ==============================================================

    /// <summary>
    /// Statement format configuration
    /// </summary>
    public class StatementFormat
    {
        public string FormatName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IncludeReceiptDetails { get; set; } = true;
        public bool IncludeContainerSummary { get; set; } = false;
        public bool IncludeAdvanceHistory { get; set; } = false;
        public string PaperSize { get; set; } = "Letter"; // Letter, Legal
        public string Orientation { get; set; } = "Portrait"; // Portrait, Landscape
    }

    /// <summary>
    /// Interface for statement printing service
    /// </summary>
    public interface IStatementPrintingService
    {
        Task<bool> PrintAdvanceStatementAsync(
            TestRunGrowerPayment growerPayment,
            int advanceNumber,
            int paymentBatchId,
            StatementFormat? format = null);

        Task<int> PrintBatchStatementsAsync(
            List<TestRunGrowerPayment> growerPayments,
            int advanceNumber,
            int paymentBatchId,
            StatementFormat? format = null);

        Task<bool> PrintYearEndStatementAsync(
            GrowerFinalPayment finalPayment,
            StatementFormat? format = null);

        Task<byte[]> GenerateStatementPdfAsync(
            TestRunGrowerPayment growerPayment,
            int advanceNumber,
            int paymentBatchId,
            StatementFormat? format = null);
    }
}


