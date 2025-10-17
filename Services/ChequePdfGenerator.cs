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
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Geom;
using iText.Kernel.Pdf.Canvas;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.DataAccess.Interfaces;
using Microsoft.Data.SqlClient;
using Dapper;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Service for generating PDF documents for cheques.
    /// Creates professional cheque layouts with grower information and payment details.
    /// </summary>
    public class ChequePdfGenerator
    {
        private readonly string _connectionString;

        public ChequePdfGenerator()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                Logger.Fatal("FATAL ERROR: Connection string 'DefaultConnection' not found or empty in App.config.");
                throw new ConfigurationErrorsException("Connection string 'DefaultConnection' is missing or empty in App.config.");
            }
        }

        /// <summary>
        /// Generates a PDF for a single cheque.
        /// </summary>
        /// <param name="cheque">The cheque to generate PDF for.</param>
        /// <returns>Byte array containing the PDF document.</returns>
        public async Task<byte[]> GenerateSingleChequePdfAsync(Cheque cheque)
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

                // Create cheque layout
                await CreateRealWorldChequeLayoutAsync(document, cheque, font, boldFont);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating PDF for cheque {cheque.ChequeNumber}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a multi-page PDF for multiple cheques.
        /// </summary>
        /// <param name="cheques">List of cheques to include in the PDF.</param>
        /// <returns>Byte array containing the multi-page PDF document.</returns>
        public async Task<byte[]> GenerateBatchChequePdfAsync(List<Cheque> cheques)
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
                foreach (var cheque in cheques)
                {
                    await CreateRealWorldChequeLayoutAsync(document, cheque, font, boldFont);
                    
                    // Add page break if not the last cheque
                    if (cheque != cheques.Last())
                    {
                        document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                    }
                }

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating batch PDF for {cheques.Count} cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a PDF for a single cheque with preview watermark.
        /// </summary>
        /// <param name="cheque">The cheque to generate PDF for.</param>
        /// <returns>Byte array containing the PDF document with watermark.</returns>
        public async Task<byte[]> GenerateSingleChequePreviewPdfAsync(Cheque cheque)
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

                // Create real-world cheque layout
                await CreateRealWorldChequeLayoutAsync(document, cheque, font, boldFont);

                // Close the document first to ensure all pages are properly finalized
                document.Close();

                // Add watermark to all pages after document is closed
                AddWatermarkToAllPages(pdf, "PREVIEW ONLY - NOT FOR PAYMENT");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating preview PDF for cheque {cheque.ChequeNumber}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a multi-page PDF for multiple cheques with preview watermark.
        /// </summary>
        /// <param name="cheques">List of cheques to include in the PDF.</param>
        /// <returns>Byte array containing the multi-page PDF document with watermark.</returns>
        public async Task<byte[]> GenerateBatchChequePreviewPdfAsync(List<Cheque> cheques)
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
                foreach (var cheque in cheques)
                {
                    await CreateRealWorldChequeLayoutAsync(document, cheque, font, boldFont);
                    
                    // Add page break if not the last cheque
                    if (cheque != cheques.Last())
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
                Logger.Error($"Error generating preview batch PDF for {cheques.Count} cheques: {ex.Message}", ex);
                throw;
            }
        }

        private async Task CreateChequeLayoutAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            // Create real-world cheque format
            await CreateRealWorldChequeLayoutAsync(document, cheque, font, boldFont);
        }

        private async Task CreateRealWorldChequeLayoutAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont, bool isReprint = false, string? reprintReason = null)
        {
            // Set page size to standard cheque format (8.5" x 3.5")
            var pageSize = new PageSize(612f, 252f); // 8.5" x 3.5" in points
            document.SetMargins(36f, 36f, 36f, 36f); // 0.5" margins

            // 1. COMPANY HEADER
            await CreateCompanyHeaderAsync(document, font, boldFont);
            
            // 2. MAIN CHEQUE AREA (Standard Bank Format)
            await CreateMainChequeAreaAsync(document, cheque, font, boldFont);
            
            // 3. MICR LINE
            await CreateMICRLineAsync(document, cheque, font);
            
            // 4. SIGNATURE AREA
            await CreateSignatureAreaAsync(document, font, boldFont);
            
            // Add watermark for reprints
            if (isReprint)
            {
                await AddReprintWatermarkAsync(document, reprintReason ?? "REPRINT", font);
            }
            
            // 5. COMPREHENSIVE SUMMARY (Separate page for grower records)
            await CreateChequeSummaryAsync(document, cheque, font, boldFont);
        }

        private async Task CreateCompanyHeaderAsync(Document document, PdfFont font, PdfFont boldFont)
        {
            // Company header with logo placeholder and info
            var companyHeader = new Table(2).UseAllAvailableWidth();
            
            // Left side - Company logo placeholder and name
            var companyCell = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .Add(new Paragraph("🌾 BERRY FARMS INC.")
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetMarginBottom(3))
                .Add(new Paragraph("123 Farm Road")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetMarginBottom(1))
                .Add(new Paragraph("Berryville, BC V1A 2B3")
                    .SetFont(font)
                    .SetFontSize(9)
                    .SetMarginBottom(1))
                .Add(new Paragraph("(555) 123-4567")
                    .SetFont(font)
                    .SetFontSize(9));
            
            // Right side - Date field (blank for manual entry)
            var dateCell = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .Add(new Paragraph("Date: _______________")
                    .SetFont(boldFont)
                    .SetFontSize(11)
                    .SetMarginTop(15));
            
            companyHeader.AddCell(companyCell);
            companyHeader.AddCell(dateCell);
            
            document.Add(companyHeader);
            document.Add(new Paragraph().SetMarginTop(8));
        }

        private async Task CreateMainChequeAreaAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            // Get payee name for display
            var payeeName = await GetPayeeNameAsync(cheque);
            
            // Main cheque area with proper bank format - single column layout
            var chequeContainer = new Table(1).UseAllAvailableWidth();
            
            // Pay to the Order of section
            var payToSection = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(5);
            
            // Pay to the Order of line with proper spacing
            var payToHeader = new Paragraph("Pay to the Order of:")
                .SetFont(boldFont)
                .SetFontSize(9)
                .SetMarginBottom(2);
            
            var payeeNameLine = new Paragraph(payeeName)
                .SetFont(font)
                .SetFontSize(11)
                .SetMarginBottom(8);
            
            payToSection.Add(payToHeader);
            payToSection.Add(payeeNameLine);
            
            // Amount section (right-aligned)
            var amountSection = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetPadding(5)
                .Add(new Paragraph($"$ {cheque.ChequeAmount:N2}")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetMarginBottom(8));
            
            chequeContainer.AddCell(payToSection);
            chequeContainer.AddCell(amountSection);
            document.Add(chequeContainer);
            
            // Amount in words line (full width)
            var amountWords = new Paragraph(ConvertAmountToWords(cheque.ChequeAmount))
                .SetFont(font)
                .SetFontSize(10)
                .SetMarginTop(3)
                .SetMarginBottom(8);
            document.Add(amountWords);
            
            // Bank and memo section
            var bankMemoContainer = new Table(2).UseAllAvailableWidth();
            
            // Bank line
            var bankCell = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(5)
                .Add(new Paragraph("Bank of America")
                    .SetFont(boldFont)
                    .SetFontSize(9)
                    .SetMarginBottom(3))
                .Add(new Paragraph("________________________________")
                    .SetFont(font)
                    .SetFontSize(10));
            
            // Memo line
            var memoCell = new Cell()
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetPadding(5)
                .Add(new Paragraph("Memo:")
                    .SetFont(boldFont)
                    .SetFontSize(9)
                    .SetMarginBottom(3))
                .Add(new Paragraph(cheque.Memo ?? "Payment for Berry Harvest 2025")
                    .SetFont(font)
                    .SetFontSize(10));
            
            bankMemoContainer.AddCell(bankCell);
            bankMemoContainer.AddCell(memoCell);
            document.Add(bankMemoContainer);
            
            // Signature line area (full width)
            var signatureArea = new Paragraph("________________________________________________")
                .SetFont(font)
                .SetFontSize(11)
                .SetMarginTop(12)
                .SetMarginBottom(3);
            document.Add(signatureArea);
        }

        private async Task CreateMICRLineAsync(Document document, Cheque cheque, PdfFont font)
        {
            // Proper MICR line format with actual cheque data
            // Standard format: :routing:account:cheque: (with colons as separators)
            var micrLine = new Paragraph($":021000021:1234567890123456:{cheque.ChequeNumber}:")
                .SetFont(font)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(8)
                .SetBorder(new iText.Layout.Borders.SolidBorder(0.5f))
                .SetPadding(8);
            document.Add(micrLine);
        }

        private async Task CreateSignatureAreaAsync(Document document, PdfFont font, PdfFont boldFont)
        {
            // Authorized signature line
            var signatureLine = new Paragraph("Authorized Signature: ___________________________")
                .SetFont(boldFont)
                .SetFontSize(9)
                .SetMarginTop(8);
            document.Add(signatureLine);
        }

        private async Task CreateChequeSummaryAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            try
            {
                // Add page break for summary section (separate page for grower records)
                document.Add(new AreaBreak());
                
                // Summary header
                var summaryHeader = new Paragraph("PAYMENT SUMMARY & DETAILS")
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(summaryHeader);
                
                // 1. Payment Summary Section
                await CreatePaymentSummarySectionAsync(document, cheque, font, boldFont);
                
                // 2. Receipt Details Section
                await CreateReceiptDetailsSectionAsync(document, cheque, font, boldFont);
                
                // 3. Quality Summary Section
                await CreateQualitySummarySectionAsync(document, cheque, font, boldFont);
                
                // 4. Payment History Section
                await CreatePaymentHistorySectionAsync(document, cheque, font, boldFont);
                
                // 5. Business Information Section
                await CreateBusinessInformationSectionAsync(document, cheque, font, boldFont);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating cheque summary: {ex.Message}", ex);
                // Add fallback summary if data retrieval fails
                var fallbackSummary = new Paragraph("Summary data unavailable - contact system administrator")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginTop(20);
                document.Add(fallbackSummary);
            }
        }

        private async Task CreatePaymentSummarySectionAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            // Payment Summary Section
            var paymentSummary = new Table(2).UseAllAvailableWidth();
            
            var headerCell = new Cell(1, 2)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .Add(new Paragraph("PAYMENT SUMMARY")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));
            paymentSummary.AddCell(headerCell);
            
            // Calculate GST (assuming 5% GST rate for agricultural products)
            var gstRate = 0.05m; // 5% GST
            var gstAmount = cheque.ChequeAmount * gstRate;
            var subtotal = cheque.ChequeAmount - gstAmount;
            
            // Payment details
            var paymentDetails = new[]
            {
                ("Payment Type:", GetPaymentTypeName(cheque.PaymentBatchId)),
                ("Batch Number:", GetBatchNumber(cheque.PaymentBatchId)),
                ("Cheque Number:", cheque.ChequeNumber),
                ("Subtotal:", $"${subtotal:N2}"),
                ("GST (5%):", $"${gstAmount:N2}"),
                ("Total Amount:", $"${cheque.ChequeAmount:N2}"),
                ("Payment Date:", cheque.ChequeDate.ToString("MMM dd, yyyy")),
                ("Grower Number:", $"G-{cheque.GrowerId}")
            };
            
            foreach (var (label, value) in paymentDetails)
            {
                paymentSummary.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(5)
                    .Add(new Paragraph(label).SetFont(boldFont).SetFontSize(10)));
                paymentSummary.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(5)
                    .Add(new Paragraph(value).SetFont(font).SetFontSize(10)));
            }
            
            document.Add(paymentSummary);
            document.Add(new Paragraph().SetMarginTop(15));
        }

        private async Task CreateReceiptDetailsSectionAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            try
            {
                // Get receipt data for this grower and payment batch
                var receipts = await GetReceiptsForChequeAsync(cheque);
                
                if (!receipts.Any())
                {
                    var noReceiptsMsg = new Paragraph("No receipt details available for this payment.")
                        .SetFont(font)
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(15);
                    document.Add(noReceiptsMsg);
                    return;
                }
                
                // Receipt Details Section
                var receiptDetails = new Table(2).UseAllAvailableWidth();
                
                var headerCell = new Cell(1, 2)
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .Add(new Paragraph("RECEIPT DETAILS")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));
                receiptDetails.AddCell(headerCell);
                
                // Receipt table headers
                var table = new Table(6).UseAllAvailableWidth();
                table.AddHeaderCell(new Cell().Add(new Paragraph("Receipt #").SetFont(boldFont).SetFontSize(9)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Date").SetFont(boldFont).SetFontSize(9)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Product").SetFont(boldFont).SetFontSize(9)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Grade").SetFont(boldFont).SetFontSize(9)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Net Wt").SetFont(boldFont).SetFontSize(9)));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Amount").SetFont(boldFont).SetFontSize(9)));
                
                // Receipt data rows
                decimal totalWeight = 0;
                decimal totalAmount = 0;
                
                foreach (var receipt in receipts.Take(10)) // Limit to 10 receipts for space
                {
                    table.AddCell(new Cell().Add(new Paragraph(receipt.ReceiptNumber.ToString()).SetFont(font).SetFontSize(8)));
                    table.AddCell(new Cell().Add(new Paragraph(receipt.ReceiptDate.ToString("MM/dd/yy")).SetFont(font).SetFontSize(8)));
                    table.AddCell(new Cell().Add(new Paragraph("Strawberry").SetFont(font).SetFontSize(8))); // Default product
                    table.AddCell(new Cell().Add(new Paragraph($"Grade {receipt.Grade}").SetFont(font).SetFontSize(8)));
                    table.AddCell(new Cell().Add(new Paragraph($"{receipt.NetWeight:N0}").SetFont(font).SetFontSize(8)));
                    table.AddCell(new Cell().Add(new Paragraph($"${receipt.NetWeight * 3.50m:N2}").SetFont(font).SetFontSize(8))); // Sample price calculation
                    
                    totalWeight += receipt.NetWeight;
                    totalAmount += receipt.NetWeight * 3.50m;
                }
                
                document.Add(table);
                
                // Summary totals
                var totalsTable = new Table(2).UseAllAvailableWidth();
                totalsTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5)
                    .Add(new Paragraph($"Total Receipts: {receipts.Count}").SetFont(boldFont).SetFontSize(9)));
                totalsTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5)
                    .Add(new Paragraph($"Total Weight: {totalWeight:N0} lbs").SetFont(boldFont).SetFontSize(9)));
                
                totalsTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5)
                    .Add(new Paragraph($"Advance Rate: 65.75%").SetFont(boldFont).SetFontSize(9)));
                totalsTable.AddCell(new Cell().SetBorder(iText.Layout.Borders.Border.NO_BORDER).SetPadding(5)
                    .Add(new Paragraph($"This Payment: ${cheque.ChequeAmount:N2}").SetFont(boldFont).SetFontSize(9)));
                
                document.Add(totalsTable);
                document.Add(new Paragraph().SetMarginTop(15));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating receipt details: {ex.Message}", ex);
                var errorMsg = new Paragraph("Receipt details unavailable")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(15);
                document.Add(errorMsg);
            }
        }

        private async Task CreateQualitySummarySectionAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            // Quality Summary Section
            var qualitySummary = new Table(2).UseAllAvailableWidth();
            
            var headerCell = new Cell(1, 2)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .Add(new Paragraph("QUALITY SUMMARY")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));
            qualitySummary.AddCell(headerCell);
            
            // Quality details (sample data - would be calculated from receipts)
            var qualityDetails = new[]
            {
                ("Average Grade:", "1A"),
                ("Average Dock %:", "2.5%"),
                ("Total Gross Weight:", "5,680 lbs"),
                ("Total Dock Weight:", "142 lbs"),
                ("Final Net Weight:", "5,538 lbs"),
                ("Quality Factor:", "1.00")
            };
            
            foreach (var (label, value) in qualityDetails)
            {
                qualitySummary.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(5)
                    .Add(new Paragraph(label).SetFont(boldFont).SetFontSize(10)));
                qualitySummary.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(5)
                    .Add(new Paragraph(value).SetFont(font).SetFontSize(10)));
            }
            
            document.Add(qualitySummary);
            document.Add(new Paragraph().SetMarginTop(15));
        }

        private async Task CreatePaymentHistorySectionAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            // Payment History Section
            var paymentHistory = new Table(2).UseAllAvailableWidth();
            
            var headerCell = new Cell(1, 2)
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .Add(new Paragraph("PAYMENT HISTORY")
                    .SetFont(boldFont)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));
            paymentHistory.AddCell(headerCell);
            
            // Payment history details (sample data - would be calculated from payment allocations)
            var historyDetails = new[]
            {
                ("Advance 1:", "$8,500.00 (Paid 08/15/25)"),
                ("Advance 2:", $"${cheque.ChequeAmount:N2} (This Payment)"),
                ("Advance 3:", "$0.00 (Pending)"),
                ("Final Payment:", "$0.00 (Pending)"),
                ("", ""),
                ("Total Paid:", $"${8500m + cheque.ChequeAmount:N2}"),
                ("Remaining:", "$8,500.00"),
                ("Final Est.:", $"${8500m + cheque.ChequeAmount + 8500m:N2}")
            };
            
            foreach (var (label, value) in historyDetails)
            {
                paymentHistory.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(5)
                    .Add(new Paragraph(label).SetFont(boldFont).SetFontSize(10)));
                paymentHistory.AddCell(new Cell()
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .SetPadding(5)
                    .Add(new Paragraph(value).SetFont(font).SetFontSize(10)));
            }
            
            document.Add(paymentHistory);
            document.Add(new Paragraph().SetMarginTop(15));
        }

        private async Task CreateBusinessInformationSectionAsync(Document document, Cheque cheque, PdfFont font, PdfFont boldFont)
        {
            try
            {
                // Get grower information
                var grower = await GetGrowerInfoAsync(cheque.GrowerId);
                
                // Business Information Section
                var businessInfo = new Table(2).UseAllAvailableWidth();
                
                var headerCell = new Cell(1, 2)
                    .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                    .Add(new Paragraph("BUSINESS INFORMATION")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));
                businessInfo.AddCell(headerCell);
                
                // Business details
                var businessDetails = new[]
                {
                    ("Grower:", grower?.FullName ?? "Unknown Grower"),
                    ("Address:", $"{grower?.Address ?? ""}"),
                    ("", $"{grower?.City ?? ""}, {grower?.Province ?? ""} {grower?.PostalCode ?? ""}"),
                    ("Phone:", grower?.PhoneNumber ?? ""),
                    ("Email:", grower?.Email ?? ""),
                    ("GST Number:", grower?.GSTNumber ?? ""),
                    ("Business #:", grower?.BusinessNumber ?? ""),
                    ("GST Rate:", "5% (Agricultural Products)"),
                    ("Payment Terms:", "Net 30 Days")
                };
                
                foreach (var (label, value) in businessDetails)
                {
                    businessInfo.AddCell(new Cell()
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPadding(5)
                        .Add(new Paragraph(label).SetFont(boldFont).SetFontSize(10)));
                    businessInfo.AddCell(new Cell()
                        .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                        .SetPadding(5)
                        .Add(new Paragraph(value).SetFont(font).SetFontSize(10)));
                }
                
                document.Add(businessInfo);
                document.Add(new Paragraph().SetMarginTop(15));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating business information: {ex.Message}", ex);
                var errorMsg = new Paragraph("Business information unavailable")
                    .SetFont(font)
                    .SetFontSize(10)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(15);
                document.Add(errorMsg);
            }
        }

        private async Task CreateFooterAsync(Document document, PdfFont font)
        {
            // Footer with generation info
            var footer = new Paragraph($"Generated on {DateTime.Now:MMM dd, yyyy at h:mm tt}")
                .SetFont(font)
                .SetFontSize(8)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(30);
            document.Add(footer);
        }

        // Helper methods for summary data
        private string GetPaymentTypeName(int? paymentBatchId)
        {
            if (!paymentBatchId.HasValue) return "Unknown";
            
            // Map payment batch ID to type name (this would typically come from database)
            return paymentBatchId switch
            {
                1 => "Advance 1",
                2 => "Advance 2", 
                3 => "Advance 3",
                4 => "Final Payment",
                _ => $"Payment Type {paymentBatchId}"
            };
        }

        private string GetBatchNumber(int? paymentBatchId)
        {
            if (!paymentBatchId.HasValue) return "N/A";
            return $"Batch #{paymentBatchId}";
        }

        private async Task<List<Receipt>> GetReceiptsForChequeAsync(Cheque cheque)
        {
            try
            {
                // This would typically query the database for receipts related to this cheque
                // For now, return sample data
                await Task.CompletedTask;
                
                var sampleReceipts = new List<Receipt>
                {
                    new Receipt
                    {
                        ReceiptId = 1,
                        ReceiptNumber = "1234",
                        ReceiptDate = DateTime.Now.AddDays(-30),
                        NetWeight = 2450,
                        Grade = 1,
                        DockPercentage = 2.5m
                    },
                    new Receipt
                    {
                        ReceiptId = 2,
                        ReceiptNumber = "1235",
                        ReceiptDate = DateTime.Now.AddDays(-25),
                        NetWeight = 1890,
                        Grade = 1,
                        DockPercentage = 2.0m
                    },
                    new Receipt
                    {
                        ReceiptId = 3,
                        ReceiptNumber = "1236",
                        ReceiptDate = DateTime.Now.AddDays(-20),
                        NetWeight = 1200,
                        Grade = 2,
                        DockPercentage = 3.0m
                    }
                };
                
                return sampleReceipts;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipts for cheque: {ex.Message}", ex);
                return new List<Receipt>();
            }
        }

        private async Task<Grower?> GetGrowerInfoAsync(int growerId)
        {
            try
            {
                // This would typically query the database for grower information
                // For now, return sample data
                await Task.CompletedTask;
                
                return new Grower
                {
                    GrowerId = growerId,
                    GrowerNumber = $"G-{growerId}",
                    FullName = "John Smith Berry Farm",
                    Address = "123 Berry Lane",
                    City = "Berryville",
                    Province = "BC",
                    PostalCode = "V1A 2B3",
                    PhoneNumber = "(555) 123-4567",
                    Email = "john@berryfarm.com",
                    GSTNumber = "12345 6789 RT0001",
                    BusinessNumber = "BN123456789"
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting grower info: {ex.Message}", ex);
                return null;
            }
        }

        private string ConvertAmountToWords(decimal amount)
        {
            // Simple amount to words conversion
            // This is a basic implementation - for production, consider using a more robust library
            var dollars = (int)Math.Floor(amount);
            var cents = (int)((amount - dollars) * 100);

            var result = NumberToWords(dollars) + " dollars";
            if (cents > 0)
            {
                result += " and " + NumberToWords(cents) + " cents";
            }

            return result.ToUpper();
        }

        private string NumberToWords(int number)
        {
            if (number == 0) return "zero";

            var ones = new[] { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
            var tens = new[] { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

            if (number < 20) return ones[number];
            if (number < 100) return tens[number / 10] + (number % 10 != 0 ? "-" + ones[number % 10] : "");
            if (number < 1000) return ones[number / 100] + " hundred" + (number % 100 != 0 ? " " + NumberToWords(number % 100) : "");
            if (number < 1000000) return NumberToWords(number / 1000) + " thousand" + (number % 1000 != 0 ? " " + NumberToWords(number % 1000) : "");

            return "amount too large";
        }

        /// <summary>
        /// Adds a watermark to all pages of the PDF document.
        /// </summary>
        /// <param name="pdf">The PDF document to add watermark to.</param>
        /// <param name="watermarkText">The text to display as watermark.</param>
        private void AddWatermarkToAllPages(PdfDocument pdf, string watermarkText)
        {
            try
            {
                // Check if PDF has any pages
                var pageCount = pdf.GetNumberOfPages();
                if (pageCount == 0)
                {
                    Logger.Warn("PDF has no pages, skipping watermark addition");
                    return;
                }

                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var color = new DeviceRgb(180, 180, 180); // Slightly darker for better visibility
                
                for (int i = 1; i <= pageCount; i++)
                {
                    var page = pdf.GetPage(i);
                    
                    // Safely get page size with fallback
                    Rectangle pageSize;
                    try
                    {
                        pageSize = page.GetPageSize();
                    }
                    catch
                    {
                        // Fallback to standard cheque size if page size cannot be determined
                        pageSize = new Rectangle(612f, 252f); // 8.5" x 3.5" in points
                    }
                    
                    try
                    {
                        var canvas = new PdfCanvas(page);
                        canvas.SaveState();
                        
                        // Set transparency
                        canvas.SetFillColor(color);
                        canvas.SetStrokeColor(color);
                        
                        // Calculate position for diagonal watermark - positioned to avoid content overlap
                        var x = pageSize.GetWidth() / 2;
                        var y = pageSize.GetHeight() / 2;
                        
                        // Draw multiple smaller watermarks instead of one large one
                        var watermarkSize = 24; // Smaller font size
                        
                        // Top-right diagonal watermark
                        canvas.BeginText()
                              .SetFontAndSize(font, watermarkSize)
                              .SetTextMatrix(0.6f, -0.6f, 0.6f, 0.6f, x + 50, y + 100)
                              .SetTextRenderingMode(1) // Fill only
                              .ShowText(watermarkText)
                              .EndText();
                        
                        // Bottom-left diagonal watermark
                        canvas.BeginText()
                              .SetFontAndSize(font, watermarkSize)
                              .SetTextMatrix(0.6f, -0.6f, 0.6f, 0.6f, x - 200, y - 100)
                              .SetTextRenderingMode(1) // Fill only
                              .ShowText(watermarkText)
                              .EndText();
                        
                        // Add border watermark at top
                        canvas.BeginText()
                              .SetFontAndSize(font, 14)
                              .SetTextMatrix(1, 0, 0, 1, 50, pageSize.GetHeight() - 30)
                              .SetTextRenderingMode(1) // Fill only
                              .ShowText($"PREVIEW DOCUMENT - {watermarkText}")
                              .EndText();
                        
                        // Add footer watermark with timestamp
                        canvas.BeginText()
                              .SetFontAndSize(font, 10)
                              .SetTextMatrix(1, 0, 0, 1, 50, 20)
                              .SetTextRenderingMode(1) // Fill only
                              .ShowText($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {watermarkText}")
                              .EndText();
                        
                        canvas.RestoreState();
                    }
                    catch (Exception pageEx)
                    {
                        Logger.Warn($"Error adding watermark to page {i}: {pageEx.Message}");
                        // Continue with next page instead of failing completely
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding watermark to PDF: {ex.Message}", ex);
                // Don't throw - watermark is not critical for functionality
            }
        }

        /// <summary>
        /// Gets the payee name for a cheque, fetching from grower data if needed.
        /// </summary>
        /// <param name="cheque">The cheque to get payee name for.</param>
        /// <returns>The payee name to display on the cheque.</returns>
        private async Task<string> GetPayeeNameAsync(Cheque cheque)
        {
            try
            {
                // If PayeeName is already populated and not "Unknown Payee", use it
                if (!string.IsNullOrWhiteSpace(cheque.PayeeName) && 
                    !cheque.PayeeName.Equals("Unknown Payee", StringComparison.OrdinalIgnoreCase) &&
                    !cheque.PayeeName.Equals("Unknown Grower", StringComparison.OrdinalIgnoreCase))
                {
                    return cheque.PayeeName;
                }

                // Otherwise, fetch the grower information
                using var connection = new SqlConnection(_connectionString);
                var sql = @"
                    SELECT 
                        CASE 
                            WHEN ISNULL(CheckPayeeName, '') != '' THEN CheckPayeeName
                            WHEN ISNULL(FullName, '') != '' THEN FullName
                            ELSE 'Unknown Grower'
                        END as PayeeName
                    FROM Growers 
                    WHERE GrowerId = @GrowerId";

                var result = await connection.QuerySingleOrDefaultAsync<string>(sql, new { GrowerId = cheque.GrowerId });
                return result ?? "Unknown Grower";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payee name for cheque {cheque.ChequeNumber}: {ex.Message}", ex);
                return "Unknown Grower";
            }
        }

        /// <summary>
        /// Adds a reprint watermark to the document.
        /// </summary>
        /// <param name="document">The document to add the watermark to.</param>
        /// <param name="reason">The reason for reprinting.</param>
        /// <param name="font">The font to use for the watermark.</param>
        private async Task AddReprintWatermarkAsync(Document document, string reason, PdfFont font)
        {
            try
            {
                // Add a watermark text overlay
                var watermarkText = $"REPRINT - {reason}";
                
                // Create a paragraph with watermark styling
                var watermark = new Paragraph(watermarkText)
                    .SetFont(font)
                    .SetFontSize(24)
                    .SetFontColor(ColorConstants.RED)
                    .SetOpacity(0.3f)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMargin(0)
                    .SetPadding(0);

                // Position the watermark in the center of the page
                document.Add(watermark);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding reprint watermark: {ex.Message}", ex);
                // Don't throw - watermark is not critical
            }
        }

        /// <summary>
        /// Generates a reprint PDF for a single cheque with reprint watermark.
        /// </summary>
        /// <param name="cheque">Cheque to include in the reprint PDF.</param>
        /// <param name="reason">Reason for reprinting.</param>
        /// <returns>Byte array containing the reprint PDF document.</returns>
        public async Task<byte[]> GenerateSingleChequeReprintPdfAsync(Cheque cheque, string reason)
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

                // Create the cheque page
                await CreateRealWorldChequeLayoutAsync(document, cheque, font, boldFont, true, reason);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating single cheque reprint PDF: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generates a reprint PDF for multiple cheques with reprint watermark.
        /// </summary>
        /// <param name="cheques">List of cheques to include in the reprint PDF.</param>
        /// <param name="reason">Reason for reprinting.</param>
        /// <returns>Byte array containing the reprint PDF document.</returns>
        public async Task<byte[]> GenerateBatchChequeReprintPdfAsync(List<Cheque> cheques, string reason)
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
                foreach (var cheque in cheques)
                {
                    await CreateRealWorldChequeLayoutAsync(document, cheque, font, boldFont);
                    
                    // Add page break except for the last cheque
                    if (cheque != cheques.Last())
                    {
                        document.Add(new AreaBreak());
                    }
                }

                // Add reprint watermark to all pages
                AddReprintWatermarkToAllPages(pdf, reason);

                document.Close();
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating reprint PDF for {cheques.Count} cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Adds a reprint watermark to all pages of the PDF document.
        /// </summary>
        /// <param name="pdf">The PDF document to add watermark to.</param>
        /// <param name="reason">The reason for reprinting.</param>
        private void AddReprintWatermarkToAllPages(PdfDocument pdf, string reason)
        {
            try
            {
                var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var color = new DeviceRgb(255, 165, 0); // Orange color for reprint
                
                for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
                {
                    var page = pdf.GetPage(i);
                    var pageSize = page.GetPageSize();
                    
                    var canvas = new PdfCanvas(page);
                    canvas.SaveState();
                    
                    // Set transparency
                    canvas.SetFillColor(color);
                    canvas.SetFontAndSize(font, 24);
                    
                    // Add diagonal reprint watermark
                    var centerX = pageSize.GetWidth() / 2;
                    var centerY = pageSize.GetHeight() / 2;
                    
                    canvas.BeginText();
                    canvas.SetTextMatrix(0.707f, 0.707f, -0.707f, 0.707f, centerX - 100, centerY);
                    canvas.ShowText("REPRINT - " + reason.ToUpper());
                    canvas.EndText();
                    
                    // Add footer watermark
                    canvas.SetFontAndSize(font, 12);
                    canvas.SetFillColor(new DeviceRgb(100, 100, 100));
                    canvas.BeginText();
                    canvas.SetTextMatrix(0, 0, 0, 0, 20, 20);
                    canvas.ShowText($"REPRINT DOCUMENT - Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Reason: {reason}");
                    canvas.EndText();
                    
                    canvas.RestoreState();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Error adding reprint watermark: {ex.Message}");
                // Don't throw - watermark is not critical for functionality
            }
        }
    }
}
