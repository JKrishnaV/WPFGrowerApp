using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.DataAccess.Interfaces;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Enhanced service for generating PDF documents for all types of cheques.
    /// Supports regular, advance, and consolidated cheques with type-specific layouts.
    /// </summary>
    public class EnhancedChequePdfGenerator
    {
        private readonly string _connectionString;
        private readonly IAdvanceChequeService _advanceChequeService;
        private readonly ICrossBatchPaymentService _crossBatchPaymentService;

        public EnhancedChequePdfGenerator(
            IAdvanceChequeService advanceChequeService,
            ICrossBatchPaymentService crossBatchPaymentService)
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                Logger.Fatal("FATAL ERROR: Connection string 'DefaultConnection' not found or empty in App.config.");
                throw new ConfigurationErrorsException("Connection string 'DefaultConnection' is missing or empty in App.config.");
            }
            
            _advanceChequeService = advanceChequeService;
            _crossBatchPaymentService = crossBatchPaymentService;
        }

        /// <summary>
        /// Generates a PDF for a single cheque item (supports all payment types).
        /// </summary>
        /// <param name="chequeItem">The cheque item to generate PDF for.</param>
        /// <returns>Byte array containing the PDF document.</returns>
        public async Task<byte[]> GenerateSingleChequePdfAsync(ChequeItem chequeItem)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Set up fonts
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Create cheque layout based on type
                await CreateEnhancedChequeLayoutAsync(document, chequeItem, font, boldFont);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating PDF for cheque {chequeItem.ChequeNumber}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a PDF for multiple cheque items (supports all payment types).
        /// </summary>
        /// <param name="chequeItems">List of cheque items to include in the PDF.</param>
        /// <returns>Byte array containing the multi-page PDF document.</returns>
        public async Task<byte[]> GenerateBatchChequePdfAsync(List<ChequeItem> chequeItems)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Set up fonts
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Create a page for each cheque
                foreach (var chequeItem in chequeItems)
                {
                    await CreateEnhancedChequeLayoutAsync(document, chequeItem, font, boldFont);
                    
                    // Add page break if not the last cheque
                    if (chequeItem != chequeItems.Last())
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                }

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating batch PDF for {chequeItems.Count} cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a PDF for a single cheque item with preview watermark.
        /// </summary>
        /// <param name="chequeItem">The cheque item to generate PDF for.</param>
        /// <returns>Byte array containing the PDF document with watermark.</returns>
        public async Task<byte[]> GenerateSingleChequePreviewPdfAsync(ChequeItem chequeItem)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Set up fonts
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Create enhanced cheque layout
                await CreateEnhancedChequeLayoutAsync(document, chequeItem, font, boldFont);

                // Close the document first to ensure all pages are properly finalized
                document.Close();

                // Add watermark to all pages after document is closed
                AddWatermarkToAllPages(pdf, "PREVIEW ONLY - NOT FOR PAYMENT");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating preview PDF for cheque {chequeItem.ChequeNumber}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a PDF for multiple cheque items with preview watermark.
        /// </summary>
        /// <param name="chequeItems">List of cheque items to include in the PDF.</param>
        /// <returns>Byte array containing the multi-page PDF document with watermark.</returns>
        public async Task<byte[]> GenerateBatchChequePreviewPdfAsync(List<ChequeItem> chequeItems)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                using var writer = new PdfWriter(memoryStream);
                using var pdf = new PdfDocument(writer);
                using var document = new Document(pdf);

                // Set up fonts
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                // Create a page for each cheque
                foreach (var chequeItem in chequeItems)
                {
                    await CreateEnhancedChequeLayoutAsync(document, chequeItem, font, boldFont);
                    
                    // Add page break if not the last cheque
                    if (chequeItem != chequeItems.Last())
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                }

                // Close the document first to ensure all pages are properly finalized
                document.Close();

                // Add watermark to all pages after document is closed
                AddWatermarkToAllPages(pdf, "PREVIEW ONLY - NOT FOR PAYMENT");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating preview batch PDF for {chequeItems.Count} cheques: {ex.Message}", ex);
                throw;
            }
        }

        private async Task CreateEnhancedChequeLayoutAsync(Document document, ChequeItem chequeItem, PdfFont font, PdfFont boldFont)
        {
            try
            {
                // Create real-world cheque format with type-specific enhancements
                await CreateRealWorldChequeLayoutAsync(document, chequeItem, font, boldFont);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating enhanced cheque layout for {chequeItem.ChequeNumber}: {ex.Message}", ex);
                throw;
            }
        }

        private async Task CreateRealWorldChequeLayoutAsync(Document document, ChequeItem chequeItem, PdfFont font, PdfFont boldFont)
        {
            // Set page size to standard cheque size (8.5" x 3.5")
            document.SetMargins(20, 20, 20, 20);

            // Create main table for cheque layout
            var table = new Table(2).UseAllAvailableWidth();
            table.SetBorder(new SolidBorder(1));

            // Header section with type indicator
            var headerCell = new Cell(1, 2);
            headerCell.SetBorder(new SolidBorder(0));
            headerCell.SetPadding(10);
            
            var headerText = new Paragraph()
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add($"BERRY FARMS GROWER PAYMENT - {chequeItem.TypeDisplay.ToUpper()}");
            
            headerCell.Add(headerText);
            table.AddCell(headerCell);

            // Left column - Payee information
            var leftCell = new Cell();
            leftCell.SetBorder(new SolidBorder(0));
            leftCell.SetPadding(10);
            leftCell.SetVerticalAlignment(VerticalAlignment.TOP);

            // Payee name
            var payeeName = new Paragraph()
                .SetFont(boldFont)
                .SetFontSize(12)
                .Add($"Pay to: {chequeItem.GrowerName}");
            leftCell.Add(payeeName);

            // Grower number
            var growerNumber = new Paragraph()
                .SetFont(font)
                .SetFontSize(10)
                .Add($"Grower #: {chequeItem.GrowerNumber}");
            leftCell.Add(growerNumber);

            // Cheque number
            var chequeNumber = new Paragraph()
                .SetFont(font)
                .SetFontSize(10)
                .Add($"Cheque #: {chequeItem.ChequeNumber}");
            leftCell.Add(chequeNumber);

            // Date
            var date = new Paragraph()
                .SetFont(font)
                .SetFontSize(10)
                .Add($"Date: {chequeItem.DateDisplay}");
            leftCell.Add(date);

            // Type-specific information
            await AddTypeSpecificInformationAsync(leftCell, chequeItem, font, boldFont);

            table.AddCell(leftCell);

            // Right column - Amount and details
            var rightCell = new Cell();
            rightCell.SetBorder(new SolidBorder(0));
            rightCell.SetPadding(10);
            rightCell.SetVerticalAlignment(VerticalAlignment.TOP);
            rightCell.SetTextAlignment(TextAlignment.RIGHT);

            // Amount
            var amount = new Paragraph()
                .SetFont(boldFont)
                .SetFontSize(16)
                .Add($"Amount: {chequeItem.AmountDisplay}");
            rightCell.Add(amount);

            // Net amount if different from gross amount
            if (chequeItem.NetAmount != chequeItem.Amount)
            {
                var netAmount = new Paragraph()
                    .SetFont(font)
                    .SetFontSize(12)
                    .Add($"Net Amount: {chequeItem.NetAmountDisplay}");
                rightCell.Add(netAmount);
            }

            // Status
            var status = new Paragraph()
                .SetFont(font)
                .SetFontSize(10)
                .Add($"Status: {chequeItem.StatusDisplay}");
            rightCell.Add(status);

            // Type-specific details
            await AddTypeSpecificDetailsAsync(rightCell, chequeItem, font, boldFont);

            table.AddCell(rightCell);

            // Add table to document
            document.Add(table);

            // Add footer with additional information
            await AddFooterInformationAsync(document, chequeItem, font, boldFont);
        }

        private async Task AddTypeSpecificInformationAsync(Cell cell, ChequeItem chequeItem, PdfFont font, PdfFont boldFont)
        {
            switch (chequeItem.PaymentType)
            {
                case ChequePaymentType.Advance:
                    // Advance cheque specific information
                    var advanceReason = new Paragraph()
                        .SetFont(font)
                        .SetFontSize(10)
                        .Add($"Advance Reason: {chequeItem.AdvanceReason ?? "N/A"}");
                    cell.Add(advanceReason);
                    break;

                case ChequePaymentType.Distribution:
                    // Distribution payment specific information
                    var sourceBatches = new Paragraph()
                        .SetFont(font)
                        .SetFontSize(10)
                        .Add($"Source Batches: {chequeItem.SourceBatchesDisplay}");
                    cell.Add(sourceBatches);
                    break;

                case ChequePaymentType.Regular:
                default:
                    // Regular cheque - no additional information needed
                    break;
            }
        }

        private async Task AddTypeSpecificDetailsAsync(Cell cell, ChequeItem chequeItem, PdfFont font, PdfFont boldFont)
        {
            switch (chequeItem.PaymentType)
            {
                case ChequePaymentType.Advance:
                    // Advance cheque specific details
                    var advanceDetails = new Paragraph()
                        .SetFont(font)
                        .SetFontSize(10)
                        .Add("ADVANCE PAYMENT");
                    cell.Add(advanceDetails);
                    break;

                case ChequePaymentType.Distribution:
                    // Distribution payment specific details
                    var consolidatedDetails = new Paragraph()
                        .SetFont(font)
                        .SetFontSize(10)
                        .Add("DISTRIBUTION PAYMENT");
                    cell.Add(consolidatedDetails);
                    break;

                case ChequePaymentType.Regular:
                default:
                    // Regular cheque - no additional details needed
                    break;
            }
        }

        private async Task AddFooterInformationAsync(Document document, ChequeItem chequeItem, PdfFont font, PdfFont boldFont)
        {
            // Add footer with additional information
            var footer = new Paragraph()
                .SetFont(font)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add($"Generated on {DateTime.Now:MMM dd, yyyy HH:mm} | Berry Farms Grower Payment System");

            document.Add(footer);

            // Add type-specific footer information
            switch (chequeItem.PaymentType)
            {
                case ChequePaymentType.Advance:
                    var advanceFooter = new Paragraph()
                        .SetFont(font)
                        .SetFontSize(8)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .Add("This is an advance payment that will be deducted from future payments.");
                    document.Add(advanceFooter);
                    break;

                case ChequePaymentType.Distribution:
                    var consolidatedFooter = new Paragraph()
                        .SetFont(font)
                        .SetFontSize(8)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .Add("This is a distribution payment combining multiple batch payments.");
                    document.Add(consolidatedFooter);
                    break;

                case ChequePaymentType.Regular:
                default:
                    // Regular cheque - no additional footer needed
                    break;
            }
        }

        private void AddWatermarkToAllPages(PdfDocument pdf, string watermarkText)
        {
            try
            {
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var color = new DeviceRgb(0.8f, 0.8f, 0.8f); // Light gray color

                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    var page = pdf.GetPage(i);
                    var pageSize = page.GetPageSize();
                    var canvas = new PdfCanvas(page);

                    // Set watermark properties
                    canvas.SetFontAndSize(font, 48);
                    canvas.SetFillColor(color);

                    // Calculate position for diagonal watermark
                    var x = pageSize.GetWidth() / 2;
                    var y = pageSize.GetHeight() / 2;

                    // Save graphics state
                    canvas.SaveState();

                    // Rotate and position watermark
                    canvas.ConcatMatrix(1, 0, 0, 1, x, y);
                    canvas.ConcatMatrix(0.707f, 0.707f, -0.707f, 0.707f, 0, 0);

                    // Draw watermark text
                    canvas.BeginText();
                    canvas.SetTextMatrix(0, 0);
                    canvas.ShowText(watermarkText);
                    canvas.EndText();

                    // Restore graphics state
                    canvas.RestoreState();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding watermark to PDF: {ex.Message}", ex);
                // Don't throw - watermark is not critical
            }
        }
    }
}
