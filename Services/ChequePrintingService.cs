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
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Service for printing cheques using configured formats
    /// Handles physical cheque printing and PDF generation
    /// </summary>
    public class ChequePrintingService : IChequePrintingService
    {
        private readonly IChequeGenerationService _chequeGenerationService;

        public ChequePrintingService(IChequeGenerationService chequeGenerationService)
        {
            _chequeGenerationService = chequeGenerationService;
        }

        /// <summary>
        /// Print a single cheque
        /// </summary>
        public async Task<bool> PrintChequeAsync(Cheque cheque, ChequeFormat? format = null)
        {
            try
            {
                format ??= GetDefaultChequeFormat();

                // Create print document
                var printDocument = CreateChequePrintDocument(cheque, format);

                // Show print dialog
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(printDocument.DocumentPaginator, $"Cheque {cheque.DisplayChequeNumber}");
                    
                    // Mark cheque as printed
                    var printedBy = App.CurrentUser?.Username ?? "SYSTEM";
                    await _chequeGenerationService.MarkChequeAsPrintedAsync(cheque.ChequeId, printedBy);
                    
                    Logger.Info($"Printed cheque {cheque.DisplayChequeNumber}");
                    return true;
                }

                return false; // User cancelled
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing cheque: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Print multiple cheques in batch
        /// </summary>
        public async Task<int> PrintChequesAsync(List<Cheque> cheques, ChequeFormat? format = null)
        {
            try
            {
                if (cheques == null || !cheques.Any())
                {
                    return 0;
                }

                format ??= GetDefaultChequeFormat();

                // Create print documents for all cheques
                var printDocument = CreateBatchChequePrintDocument(cheques, format);

                // Show print dialog
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(printDocument.DocumentPaginator, 
                        $"Cheques Batch ({cheques.Count} cheques)");
                    
                    // Mark all cheques as printed
                    var printedBy = App.CurrentUser?.Username ?? "SYSTEM";
                    var chequeIds = cheques.Select(c => c.ChequeId).ToList();
                    await _chequeGenerationService.MarkChequesAsPrintedAsync(chequeIds, printedBy);
                    
                    Logger.Info($"Printed {cheques.Count} cheques in batch");
                    return cheques.Count;
                }

                return 0; // User cancelled
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing cheque batch: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Print test cheque (sample without marking as printed)
        /// </summary>
        public Task PrintTestChequeAsync(ChequeFormat format)
        {
            try
            {
                // Create sample cheque data
                var sampleCheque = new Cheque
                {
                    ChequeNumber = "12345",
                    ChequeDate = DateTime.Today,
                    ChequeAmount = 1234.56m,
                    PayeeName = "SAMPLE GROWER NAME",
                    Memo = "Test Cheque - Do Not Cash",
                    SeriesCode = "TEST"
                };

                var printDocument = CreateChequePrintDocument(sampleCheque, format);

                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintDocument(printDocument.DocumentPaginator, "Test Cheque");
                }

                Logger.Info("Printed test cheque");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing test cheque: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generate cheque preview (for UI display before printing)
        /// </summary>
        public Task<FixedDocument> GenerateChequePreviewAsync(Cheque cheque, ChequeFormat? format = null)
        {
            try
            {
                format ??= GetDefaultChequeFormat();
                var document = CreateChequePrintDocument(cheque, format);
                return Task.FromResult(document);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating cheque preview: {ex.Message}", ex);
                throw;
            }
        }

        // ==============================================================
        // HELPER METHODS
        // ==============================================================

        /// <summary>
        /// Create print document for a single cheque
        /// </summary>
        private FixedDocument CreateChequePrintDocument(Cheque cheque, ChequeFormat format)
        {
            var document = new FixedDocument();
            var pageContent = new PageContent();
            var fixedPage = new FixedPage
            {
                Width = format.PaperWidthInPixels,
                Height = format.PaperHeightInPixels
            };

            // Add cheque elements at configured positions
            AddChequeDate(fixedPage, cheque.ChequeDate, format);
            AddPayeeName(fixedPage, cheque.PayeeName ?? "UNKNOWN", format);
            AddAmount(fixedPage, cheque.ChequeAmount, format);
            AddAmountInWords(fixedPage, AmountToWords(cheque.ChequeAmount), format);
            AddMemo(fixedPage, cheque.Memo, format);

            ((IAddChild)pageContent).AddChild(fixedPage);
            document.Pages.Add(pageContent);

            return document;
        }

        /// <summary>
        /// Create print document for multiple cheques
        /// </summary>
        private FixedDocument CreateBatchChequePrintDocument(List<Cheque> cheques, ChequeFormat format)
        {
            var document = new FixedDocument();

            foreach (var cheque in cheques)
            {
                var pageContent = new PageContent();
                var fixedPage = new FixedPage
                {
                    Width = format.PaperWidthInPixels,
                    Height = format.PaperHeightInPixels
                };

                // Add cheque elements
                AddChequeDate(fixedPage, cheque.ChequeDate, format);
                AddPayeeName(fixedPage, cheque.PayeeName ?? "UNKNOWN", format);
                AddAmount(fixedPage, cheque.ChequeAmount, format);
                AddAmountInWords(fixedPage, AmountToWords(cheque.ChequeAmount), format);
                AddMemo(fixedPage, cheque.Memo, format);

                ((IAddChild)pageContent).AddChild(fixedPage);
                document.Pages.Add(pageContent);
            }

            return document;
        }

        /// <summary>
        /// Add cheque date to page
        /// </summary>
        private void AddChequeDate(FixedPage page, DateTime date, ChequeFormat format)
        {
            var textBlock = new TextBlock
            {
                Text = date.ToString("MMMM dd, yyyy"),
                FontFamily = new FontFamily(format.FontName),
                FontSize = format.FontSize
            };

            FixedPage.SetLeft(textBlock, format.DateX);
            FixedPage.SetTop(textBlock, format.DateY);
            page.Children.Add(textBlock);
        }

        /// <summary>
        /// Add payee name to page
        /// </summary>
        private void AddPayeeName(FixedPage page, string payeeName, ChequeFormat format)
        {
            var textBlock = new TextBlock
            {
                Text = payeeName.ToUpper(),
                FontFamily = new FontFamily(format.FontName),
                FontSize = format.FontSize,
                FontWeight = FontWeights.Bold
            };

            FixedPage.SetLeft(textBlock, format.PayeeX);
            FixedPage.SetTop(textBlock, format.PayeeY);
            page.Children.Add(textBlock);
        }

        /// <summary>
        /// Add amount (numeric) to page
        /// </summary>
        private void AddAmount(FixedPage page, decimal amount, ChequeFormat format)
        {
            var textBlock = new TextBlock
            {
                Text = $"${amount:N2}",
                FontFamily = new FontFamily(format.FontName),
                FontSize = format.FontSize,
                FontWeight = FontWeights.Bold
            };

            FixedPage.SetLeft(textBlock, format.AmountX);
            FixedPage.SetTop(textBlock, format.AmountY);
            page.Children.Add(textBlock);
        }

        /// <summary>
        /// Add amount in words to page
        /// </summary>
        private void AddAmountInWords(FixedPage page, string amountWords, ChequeFormat format)
        {
            var textBlock = new TextBlock
            {
                Text = amountWords.ToUpper(),
                FontFamily = new FontFamily(format.FontName),
                FontSize = format.FontSize - 2
            };

            FixedPage.SetLeft(textBlock, format.AmountWordsX);
            FixedPage.SetTop(textBlock, format.AmountWordsY);
            page.Children.Add(textBlock);
        }

        /// <summary>
        /// Add memo to page
        /// </summary>
        private void AddMemo(FixedPage page, string? memo, ChequeFormat format)
        {
            if (string.IsNullOrWhiteSpace(memo))
                return;

            var textBlock = new TextBlock
            {
                Text = memo,
                FontFamily = new FontFamily(format.FontName),
                FontSize = format.FontSize - 4
            };

            FixedPage.SetLeft(textBlock, format.MemoX);
            FixedPage.SetTop(textBlock, format.MemoY);
            page.Children.Add(textBlock);
        }

        /// <summary>
        /// Convert amount to words (e.g., 1234.56 -> "One Thousand Two Hundred Thirty-Four Dollars and Fifty-Six Cents")
        /// </summary>
        public string AmountToWords(decimal amount)
        {
            if (amount == 0)
                return "Zero Dollars";

            int dollars = (int)amount;
            int cents = (int)((amount - dollars) * 100);

            string result = ConvertToWords(dollars) + " Dollar" + (dollars == 1 ? "" : "s");
            
            if (cents > 0)
            {
                result += " and " + ConvertToWords(cents) + " Cent" + (cents == 1 ? "" : "s");
            }

            return result;
        }

        /// <summary>
        /// Convert number to words helper
        /// </summary>
        private string ConvertToWords(int number)
        {
            if (number == 0)
                return "Zero";

            if (number < 0)
                return "Minus " + ConvertToWords(Math.Abs(number));

            string words = "";

            if ((number / 1000000) > 0)
            {
                words += ConvertToWords(number / 1000000) + " Million ";
                number %= 1000000;
            }

            if ((number / 1000) > 0)
            {
                words += ConvertToWords(number / 1000) + " Thousand ";
                number %= 1000;
            }

            if ((number / 100) > 0)
            {
                words += ConvertToWords(number / 100) + " Hundred ";
                number %= 100;
            }

            if (number > 0)
            {
                if (words != "")
                    words += "and ";

                var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
                var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

                if (number < 20)
                    words += unitsMap[number];
                else
                {
                    words += tensMap[number / 10];
                    if ((number % 10) > 0)
                        words += "-" + unitsMap[number % 10];
                }
            }

            return words.Trim();
        }

        /// <summary>
        /// Get default cheque format
        /// TODO: Load from database ChequeSeries configuration
        /// </summary>
        private ChequeFormat GetDefaultChequeFormat()
        {
            // Default format for standard business cheque (8.5" x 3.5")
            // Positions are in pixels (96 DPI)
            return new ChequeFormat
            {
                ChequeFormatId = 1,
                FormatName = "Standard Business Cheque",
                Description = "Default format for 8.5 x 3.5 inch cheques",
                
                // Page size (8.5" x 3.5" at 96 DPI)
                PaperWidthInPixels = 816,  // 8.5"
                PaperHeightInPixels = 336, // 3.5"
                
                // Element positions (in pixels from top-left)
                DateX = 600,
                DateY = 30,
                
                PayeeX = 100,
                PayeeY = 100,
                
                AmountX = 650,
                AmountY = 100,
                
                AmountWordsX = 100,
                AmountWordsY = 140,
                
                MemoX = 100,
                MemoY = 200,
                
                // Font settings
                FontName = "Arial",
                FontSize = 12
            };
        }
    }

    // ==============================================================
    // SUPPORTING MODELS
    // ==============================================================

    /// <summary>
    /// Cheque format configuration
    /// Defines where each element appears on the physical cheque
    /// </summary>
    public class ChequeFormat
    {
        public int ChequeFormatId { get; set; }
        public string FormatName { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Paper size in pixels (96 DPI)
        public double PaperWidthInPixels { get; set; } = 816;  // 8.5 inches
        public double PaperHeightInPixels { get; set; } = 336; // 3.5 inches

        // Position coordinates (in pixels from top-left corner)
        public double DateX { get; set; }
        public double DateY { get; set; }
        
        public double PayeeX { get; set; }
        public double PayeeY { get; set; }
        
        public double AmountX { get; set; }
        public double AmountY { get; set; }
        
        public double AmountWordsX { get; set; }
        public double AmountWordsY { get; set; }
        
        public double MemoX { get; set; }
        public double MemoY { get; set; }

        // Font settings
        public string FontName { get; set; } = "Arial";
        public int FontSize { get; set; } = 12;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Interface for cheque printing service
    /// </summary>
    public interface IChequePrintingService
    {
        Task<bool> PrintChequeAsync(Cheque cheque, ChequeFormat? format = null);
        Task<int> PrintChequesAsync(List<Cheque> cheques, ChequeFormat? format = null);
        Task PrintTestChequeAsync(ChequeFormat format);
        Task<FixedDocument> GenerateChequePreviewAsync(Cheque cheque, ChequeFormat? format = null);
        string AmountToWords(decimal amount);
    }
}


