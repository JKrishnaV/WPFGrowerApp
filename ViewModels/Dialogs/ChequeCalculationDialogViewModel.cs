using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WPFGrowerApp.Models;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    /// <summary>
    /// ViewModel for the Cheque Calculation Details dialog
    /// </summary>
    public class ChequeCalculationDialogViewModel : INotifyPropertyChanged
    {
        private ChequeItem _chequeItem;
        private ChequeCalculationDetails _calculationDetails;
        private InvoiceStyleChequeDetails _invoiceStyleDetails;
        private bool _isExporting;
        private string _exportStatus;
        private readonly ChequeDetailsExportService _exportService;
        private readonly IReceiptService _receiptService;
        private readonly IPaymentService _paymentService;
        private readonly IChequeService _chequeService;

        public ChequeItem ChequeItem
        {
            get => _chequeItem;
            set
            {
                if (SetProperty(ref _chequeItem, value))
                {
                    if (value != null)
                    {
                        CalculationDetails = new ChequeCalculationDetails(value);
                    }
                }
            }
        }

        public ChequeCalculationDetails CalculationDetails
        {
            get => _calculationDetails;
            set => SetProperty(ref _calculationDetails, value);
        }

        public InvoiceStyleChequeDetails InvoiceStyleDetails
        {
            get => _invoiceStyleDetails;
            set => SetProperty(ref _invoiceStyleDetails, value);
        }

        public bool IsExporting
        {
            get => _isExporting;
            set => SetProperty(ref _isExporting, value);
        }

        public string ExportStatus
        {
            get => _exportStatus;
            set => SetProperty(ref _exportStatus, value);
        }

        // Delegated properties for binding
        public string ChequeNumber => ChequeItem?.ChequeNumber ?? "N/A";
        public string GrowerDisplay => ChequeItem?.GrowerDisplay ?? "N/A";
        public string PaymentTypeDisplay => ChequeItem?.TypeDisplay ?? "N/A";
        public string DateDisplay => ChequeItem?.DateDisplay ?? "N/A";
        public string StatusDisplay => ChequeItem?.StatusDisplay ?? "N/A";
        public string Status => ChequeItem?.Status ?? "Unknown";
        public string CalculationSummary => CalculationDetails?.CalculationSummary ?? "No calculation details available";
        public List<CalculationLineItem> LineItems => CalculationDetails?.LineItems ?? new List<CalculationLineItem>();
        public List<AdvanceDeduction> AdvanceDeductions => CalculationDetails?.AdvanceDeductions ?? new List<AdvanceDeduction>();
        public List<BatchBreakdown> BatchBreakdowns => CalculationDetails?.BatchBreakdowns ?? new List<BatchBreakdown>();
        public bool HasDeductions => CalculationDetails?.HasDeductions ?? false;
        public bool HasMultipleBatches => CalculationDetails?.HasMultipleBatches ?? false;

        // Commands
        public ICommand ExportToPdfCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToCsvCommand { get; }
        public ICommand ExportToHtmlCommand { get; }
        public ICommand PrintCommand { get; }

        public ChequeCalculationDialogViewModel()
        {
            _exportService = new ChequeDetailsExportService();
            _receiptService = null; // Will be injected in real implementation
            _paymentService = null; // Will be injected in real implementation
            _chequeService = new ChequeService(); // Initialize with default service
            
            // Initialize commands
            ExportToPdfCommand = new RelayCommand(async (obj) => await ExportToPdfAsync());
            ExportToExcelCommand = new RelayCommand(async (obj) => await ExportToExcelAsync());
            ExportToCsvCommand = new RelayCommand(async (obj) => await ExportToCsvAsync());
            ExportToHtmlCommand = new RelayCommand(async (obj) => await ExportToHtmlAsync());
            PrintCommand = new RelayCommand(async (obj) => await PrintAsync());

            CalculationDetails = new ChequeCalculationDetails();
            InvoiceStyleDetails = new InvoiceStyleChequeDetails();
        }

        public ChequeCalculationDialogViewModel(ChequeItem chequeItem) : this()
        {
            ChequeItem = chequeItem;
            // Data will be loaded explicitly before showing the dialog
        }

        public ChequeCalculationDialogViewModel(ChequeItem chequeItem, IPaymentService paymentService, IReceiptService receiptService, IChequeService chequeService) : this()
        {
            _paymentService = paymentService;
            _receiptService = receiptService;
            _chequeService = chequeService;
            ChequeItem = chequeItem;
            // Data will be loaded explicitly before showing the dialog
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #region Export Methods

        private async Task ExportToPdfAsync()
        {
            await ExportAsync("PDF", "pdf", _exportService.ExportToPdfAsync);
        }

        private async Task ExportToExcelAsync()
        {
            await ExportAsync("Excel", "xlsx", _exportService.ExportToExcelAsync);
        }

        private async Task ExportToCsvAsync()
        {
            await ExportAsync("CSV", "csv", _exportService.ExportToCsvAsync);
        }

        private async Task ExportToHtmlAsync()
        {
            await ExportAsync("HTML", "html", _exportService.ExportToHtmlAsync);
        }

        private async Task PrintAsync()
        {
            try
            {
                IsExporting = true;
                ExportStatus = "Preparing for print...";

                // Create a temporary PDF file for printing
                var tempFile = System.IO.Path.GetTempFileName() + ".pdf";
                await _exportService.ExportToPdfAsync(InvoiceStyleDetails, tempFile);

                // Print the PDF
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFile,
                    Verb = "print",
                    UseShellExecute = true
                });

                ExportStatus = "Print job sent successfully";
            }
            catch (Exception ex)
            {
                ExportStatus = $"Error printing: {ex.Message}";
                Logger.Error($"Error printing cheque details: {ex.Message}", ex);
            }
            finally
            {
                IsExporting = false;
            }
        }

        private async Task ExportAsync(string formatName, string extension, Func<InvoiceStyleChequeDetails, string, Task<string>> exportMethod)
        {
            try
            {
                IsExporting = true;
                ExportStatus = $"Preparing {formatName} export...";

                var saveFileDialog = new SaveFileDialog
                {
                    Title = $"Export Cheque Details to {formatName}",
                    Filter = $"{formatName} Files (*.{extension})|*.{extension}|All Files (*.*)|*.*",
                    DefaultExt = extension,
                    FileName = $"ChequeDetails_{ChequeNumber}_{DateTime.Now:yyyyMMdd}.{extension}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    ExportStatus = $"Exporting to {formatName}...";
                    await exportMethod(InvoiceStyleDetails, saveFileDialog.FileName);
                    ExportStatus = $"{formatName} export completed successfully";
                }
                else
                {
                    ExportStatus = "Export cancelled";
                }
            }
            catch (Exception ex)
            {
                ExportStatus = $"Error exporting to {formatName}: {ex.Message}";
                Logger.Error($"Error exporting cheque details to {formatName}: {ex.Message}", ex);
            }
            finally
            {
                IsExporting = false;
            }
        }

        #endregion

        #region Helper Methods


        public async Task LoadInvoiceStyleDetailsAsync()
        {
            if (ChequeItem == null) 
            {
                Logger.Warn("ChequeItem is null, cannot load invoice style details");
                return;
            }

            Logger.Info($"Loading invoice style details for cheque: {ChequeItem.ChequeNumber}");

            try
            {
                // Load header information
                InvoiceStyleDetails.Header = new ChequeHeaderInfo
                {
                    ChequeNumber = ChequeItem.ChequeNumber,
                    ChequeDate = ChequeItem.ChequeDate,
                    GrowerInfo = ChequeItem.GrowerDisplay,
                    PayeeName = ChequeItem.GrowerName,
                    FiscalYear = ChequeItem.ChequeDate.Year,
                    Status = ChequeItem.Status
                };

                // Load summary - will be updated after loading deductions and receipts
                InvoiceStyleDetails.Summary = new Models.PaymentSummary
                {
                    TotalGrossPayments = 0, // Will be calculated from receipts
                    TotalDeductions = 0, // Will be calculated from deductions
                    NetChequeAmount = 0 // Will be calculated as Gross - Deductions
                };

                // Load payment batches with actual receipt data from database
                InvoiceStyleDetails.PaymentBatches = new List<PaymentBatchDetail>();
                
                // Always create a payment batch, even if PaymentBatchId is null
                Logger.Info($"ChequeItem.PaymentBatchId: {ChequeItem.PaymentBatchId}, ChequeItem.BatchNumber: {ChequeItem.BatchNumber}");
                
                var batch = new PaymentBatchDetail
                {
                    BatchNumber = ChequeItem.BatchNumber ?? "Unknown",
                    BatchDate = ChequeItem.ChequeDate,
                    PaymentType = ChequeItem.TypeDisplay,
                    Receipts = new List<ReceiptLineItem>()
                };

                // Load actual receipt data from database
                await LoadActualReceiptDataAsync(batch);

                InvoiceStyleDetails.PaymentBatches.Add(batch);
                
                // Trigger property change notification for UI refresh
                OnPropertyChanged(nameof(InvoiceStyleDetails));
                OnPropertyChanged(nameof(InvoiceStyleDetails.PaymentBatches));
                
                Logger.Info($"Added batch to PaymentBatches. Total batches: {InvoiceStyleDetails.PaymentBatches.Count}");
                if (InvoiceStyleDetails.PaymentBatches.Any())
                {
                    Logger.Info($"First batch has {InvoiceStyleDetails.PaymentBatches[0].Receipts.Count} receipts");
                }

                // Load advance runs
                InvoiceStyleDetails.AdvanceRuns = new List<AdvancePaymentRun>();
                if (ChequeItem.AdvanceChequeId.HasValue)
                {
                    InvoiceStyleDetails.AdvanceRuns.Add(new AdvancePaymentRun
                    {
                        RunDate = ChequeItem.ChequeDate.AddDays(-30),
                        RunNumber = "AR001",
                        Amount = 1000.0m
                    });
                }

                // Load advance deductions from database if not already loaded
                if (_chequeService != null && (ChequeItem.AdvanceDeductions == null || !ChequeItem.AdvanceDeductions.Any()))
                {
                    Logger.Info($"Loading advance deductions for cheque: {ChequeItem.ChequeNumber}");
                    var advanceDeductions = await _chequeService.GetAdvanceDeductionsByChequeNumberAsync(ChequeItem.ChequeNumber);
                    ChequeItem.AdvanceDeductions = advanceDeductions;
                    Logger.Info($"Found {advanceDeductions.Count} advance deductions for cheque {ChequeItem.ChequeNumber}");
                }
                else if (ChequeItem.AdvanceDeductions != null && ChequeItem.AdvanceDeductions.Any())
                {
                    Logger.Info($"Using existing advance deductions from ChequeItem ({ChequeItem.AdvanceDeductions.Count} deductions)");
                }

                // Load deductions - show ONLY the deductions for this specific cheque
                InvoiceStyleDetails.Deductions = new List<DeductionDetail>();
                if (ChequeItem.AdvanceDeductions != null && ChequeItem.AdvanceDeductions.Any())
                {
                    foreach (var deduction in ChequeItem.AdvanceDeductions)
                    {
                        var totalAdvanceAmount = deduction.OriginalAdvanceAmount?.ToString("C") ?? "N/A";
                        InvoiceStyleDetails.Deductions.Add(new DeductionDetail
                        {
                            Type = "Advance Deduction",
                            Description = $"Advance Cheque ID: {deduction.AdvanceChequeId}, Total Advance: {totalAdvanceAmount}, Deducted: {deduction.DeductionAmount:C}",
                            Amount = deduction.DeductionAmount
                        });
                    }
                    
                    Logger.Info($"Added {InvoiceStyleDetails.Deductions.Count} deduction details to invoice style details");
                    Logger.Info($"Total deduction amount for this cheque: {InvoiceStyleDetails.Deductions.Sum(d => d.Amount):C}");
                }
                else
                {
                    Logger.Info("No advance deductions found for this cheque");
                }

                // Update summary with actual deduction amounts
                if (InvoiceStyleDetails.Deductions.Any())
                {
                    var totalDeductions = InvoiceStyleDetails.Deductions.Sum(d => d.Amount);
                    InvoiceStyleDetails.Summary.TotalDeductions = totalDeductions;
                    Logger.Info($"Updated summary with total deductions: {totalDeductions:C}");
                }

                // Calculate gross amount from receipts and update summary
                if (InvoiceStyleDetails.PaymentBatches.Any() && InvoiceStyleDetails.PaymentBatches[0].Receipts.Any())
                {
                    var totalGrossPayments = InvoiceStyleDetails.PaymentBatches.Sum(b => b.Receipts.Sum(r => r.Amount));
                    InvoiceStyleDetails.Summary.TotalGrossPayments = totalGrossPayments;
                    Logger.Info($"Updated summary with total gross payments from receipts: {totalGrossPayments:C}");
                    
                    // Calculate Net Cheque Amount = Gross - Deductions
                    var calculatedNet = totalGrossPayments - InvoiceStyleDetails.Summary.TotalDeductions;
                    InvoiceStyleDetails.Summary.NetChequeAmount = calculatedNet;
                    Logger.Info($"Calculated Net Cheque Amount: Gross ({totalGrossPayments:C}) - Deductions ({InvoiceStyleDetails.Summary.TotalDeductions:C}) = {calculatedNet:C}");
                    
                    // Verify the calculation matches the expected cheque amount
                    Logger.Info($"Expected net from ChequeItem: {ChequeItem.Amount:C}");
                    if (Math.Abs(calculatedNet - ChequeItem.Amount) > 0.01m)
                    {
                        Logger.Warn($"Calculated net ({calculatedNet:C}) does not match ChequeItem.Amount ({ChequeItem.Amount:C}). Using calculated value.");
                    }
                }
                else
                {
                    // If no receipts loaded, use ChequeItem.Amount as net
                    InvoiceStyleDetails.Summary.NetChequeAmount = ChequeItem.Amount;
                    Logger.Info($"No receipts loaded, using ChequeItem.Amount as Net: {ChequeItem.Amount:C}");
                }

                // Load payment history for this grower
                await LoadPaymentHistoryAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading invoice style details: {ex.Message}", ex);
            }
        }

        private void LoadActualReceiptData(PaymentBatchDetail batch)
        {
            try
            {
                if (_chequeService == null)
                {
                    Logger.Warn("ChequeService not available for loading receipt data");
                    batch.Receipts = new List<ReceiptLineItem>();
                    batch.BatchSubtotal = 0;
                    return;
                }

                // For synchronous loading, we'll start with empty data
                // The async method will be called separately to load real data
                batch.Receipts = new List<ReceiptLineItem>();
                batch.BatchSubtotal = 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading actual receipt data: {ex.Message}", ex);
                batch.Receipts = new List<ReceiptLineItem>();
                batch.BatchSubtotal = 0;
            }
        }

        private async Task LoadActualReceiptDataAsync(PaymentBatchDetail batch)
        {
            try
            {
                if (_chequeService == null)
                {
                    Logger.Warn("ChequeService not available for loading receipt data");
                    batch.Receipts = new List<ReceiptLineItem>();
                    batch.BatchSubtotal = 0;
                    return;
                }

                Logger.Info($"Loading receipt details for cheque: {ChequeItem.ChequeNumber}");
                
                // Load actual receipt details from database
                var receiptDetails = await _chequeService.GetReceiptDetailsForChequeAsync(ChequeItem.ChequeNumber);
                
                Logger.Info($"Found {receiptDetails?.Count ?? 0} receipt details for cheque {ChequeItem.ChequeNumber}");
                
                if (receiptDetails != null && receiptDetails.Any())
                {
                    var receipts = new List<ReceiptLineItem>();
                    
                    foreach (var receipt in receiptDetails)
                    {
                        receipts.Add(new ReceiptLineItem
                        {
                            ReceiptNumber = receipt.ReceiptNumber,
                            BatchNumber = ChequeItem.BatchNumber ?? "Unknown",
                            ProductName = receipt.ProductName ?? "Unknown",
                            ProcessName = receipt.ProcessName ?? "Unknown",
                            Grade = receipt.Grade.ToString(),
                            Weight = receipt.FinalWeight,
                            PricePerPound = receipt.PricePerPound,
                            Amount = receipt.TotalAmountPaid,
                            AdvancePayment = receipt.AdvancePaymentDisplay
                        });
                    }
                    
                    batch.Receipts = receipts;
                    
                    // Calculate batch subtotal as sum of individual receipt amounts
                    batch.BatchSubtotal = batch.Receipts.Sum(r => r.Amount);
                    
                    Logger.Info($"Loaded {receipts.Count} receipt line items with total amount: {batch.BatchSubtotal:C}");
                    
                    // Verify the calculation
                    var calculatedTotal = batch.Receipts.Sum(r => r.Amount);
                    var targetAmount = ChequeItem.Amount;
                    
                    if (Math.Abs(calculatedTotal - targetAmount) > 0.01m)
                    {
                        Logger.Warn($"Receipt total ({calculatedTotal:C}) does not match cheque amount ({targetAmount:C})");
                    }
                }
                else
                {
                    Logger.Warn($"No receipt details found for cheque {ChequeItem.ChequeNumber}");
                    batch.Receipts = new List<ReceiptLineItem>();
                    batch.BatchSubtotal = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading actual receipt data for cheque {ChequeItem.ChequeNumber}: {ex.Message}", ex);
                batch.Receipts = new List<ReceiptLineItem>();
                batch.BatchSubtotal = 0;
            }
        }

        private async Task LoadPaymentHistoryAsync()
        {
            try
            {
                if (_chequeService == null)
                {
                    Logger.Warn("ChequeService not available for loading payment history");
                    InvoiceStyleDetails.History = new PaymentHistory
                    {
                        Payments = new List<PaymentHistoryItem>(),
                        SeasonTotal = ChequeItem.NetAmount
                    };
                    return;
                }

                Logger.Info($"Loading payment history for grower {ChequeItem.GrowerId}, excluding cheque {ChequeItem.ChequeNumber}");
                
                // Get previous cheques for this grower
                var previousCheques = await _chequeService.GetPaymentHistoryForGrowerAsync(
                    ChequeItem.GrowerId, 
                    ChequeItem.ChequeNumber);

                var historyItems = previousCheques.Select(c => new PaymentHistoryItem
                {
                    ChequeNumber = c.ChequeNumber ?? "N/A",
                    BatchNumber = c.BatchNumber ?? "N/A",
                    Date = c.Date,
                    Amount = c.Amount ?? 0m
                }).ToList();

                Logger.Info($"Found {historyItems.Count} previous cheques for grower");

                InvoiceStyleDetails.History = new PaymentHistory
                {
                    Payments = historyItems,
                    SeasonTotal = ChequeItem.NetAmount
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading payment history: {ex.Message}", ex);
                InvoiceStyleDetails.History = new PaymentHistory
                {
                    Payments = new List<PaymentHistoryItem>(),
                    SeasonTotal = ChequeItem.NetAmount
                };
            }
        }


        #endregion
    }
}
