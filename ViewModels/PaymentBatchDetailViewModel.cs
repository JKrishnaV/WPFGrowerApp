using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.ViewModels.Dialogs;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Payment Batch Detail window
    /// Displays comprehensive details about a payment batch across multiple tabs
    /// </summary>
    public class PaymentBatchDetailViewModel : ViewModelBase
    {
        private readonly IPaymentBatchManagementService _batchService;
        private readonly IPaymentService _paymentService;
        private readonly IChequeGenerationService _chequeService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IPaymentBatchExportService _exportService;

        // Properties
        private PaymentBatch _selectedBatch;
        private PaymentBatchSummary _batchSummary;
        private ObservableCollection<GrowerPaymentSummary> _growerPayments;
        private ObservableCollection<GrowerPaymentSummary> _filteredGrowerPayments;
        private ObservableCollection<ReceiptPaymentAllocation> _batchAllocations;
        private ObservableCollection<ReceiptPaymentAllocation> _filteredAllocations;
        private ObservableCollection<Cheque> _batchCheques;
        private ObservableCollection<Cheque> _filteredCheques;
        private string _batchParameters;
        private bool _isLoading;
        private bool _isExporting;
        private int _selectedTabIndex;
        private string _growerSearchText = string.Empty;
        private int? _selectedGrowerFilter;
        private string _selectedChequeStatusFilter = "All";
        private PaymentBatchAnalytics _analyticsSummary;

        public PaymentBatch SelectedBatch
        {
            get => _selectedBatch;
            set => SetProperty(ref _selectedBatch, value);
        }

        public PaymentBatchSummary BatchSummary
        {
            get => _batchSummary;
            set => SetProperty(ref _batchSummary, value);
        }

        public ObservableCollection<GrowerPaymentSummary> GrowerPayments
        {
            get => _growerPayments;
            set => SetProperty(ref _growerPayments, value);
        }

        public ObservableCollection<GrowerPaymentSummary> FilteredGrowerPayments
        {
            get => _filteredGrowerPayments;
            set => SetProperty(ref _filteredGrowerPayments, value);
        }

        public ObservableCollection<ReceiptPaymentAllocation> BatchAllocations
        {
            get => _batchAllocations;
            set => SetProperty(ref _batchAllocations, value);
        }

        public ObservableCollection<ReceiptPaymentAllocation> FilteredAllocations
        {
            get => _filteredAllocations;
            set => SetProperty(ref _filteredAllocations, value);
        }

        public ObservableCollection<Cheque> BatchCheques
        {
            get => _batchCheques;
            set => SetProperty(ref _batchCheques, value);
        }

        public ObservableCollection<Cheque> FilteredCheques
        {
            get => _filteredCheques;
            set => SetProperty(ref _filteredCheques, value);
        }

        public string BatchParameters
        {
            get => _batchParameters;
            set => SetProperty(ref _batchParameters, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsExporting
        {
            get => _isExporting;
            set => SetProperty(ref _isExporting, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    // Load data for tab if not already loaded
                    _ = LoadTabDataAsync(value);
                }
            }
        }

        public string GrowerSearchText
        {
            get => _growerSearchText;
            set
            {
                if (SetProperty(ref _growerSearchText, value))
                {
                    FilterGrowerPayments();
                }
            }
        }

        public int? SelectedGrowerFilter
        {
            get => _selectedGrowerFilter;
            set
            {
                if (SetProperty(ref _selectedGrowerFilter, value))
                {
                    FilterAllocations();
                }
            }
        }

        public string SelectedChequeStatusFilter
        {
            get => _selectedChequeStatusFilter;
            set
            {
                if (SetProperty(ref _selectedChequeStatusFilter, value))
                {
                    FilterCheques();
                }
            }
        }

        public PaymentBatchAnalytics AnalyticsSummary
        {
            get => _analyticsSummary;
            set => SetProperty(ref _analyticsSummary, value);
        }

        // For Tab 2 - Grower filter dropdown
        public ObservableCollection<GrowerFilterOption> GrowerFilterOptions { get; }

        // For Tab 3 - Status filter
        public ObservableCollection<string> ChequeStatusOptions { get; }

        // Computed properties for header
        public string WindowTitle => $"Batch Details - {SelectedBatch?.BatchNumber ?? "Unknown"}";
        public string StatusDisplayText => BatchSummary?.Status ?? SelectedBatch?.Status ?? "Unknown";

        // Commands
        public ICommand NavigateBackToBatchListCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand PrintBatchReportCommand { get; }

        public PaymentBatchDetailViewModel(
            PaymentBatch batch,
            IPaymentBatchManagementService batchService,
            IPaymentService paymentService,
            IChequeGenerationService chequeService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IPaymentBatchExportService exportService)
        {
            _selectedBatch = batch ?? throw new ArgumentNullException(nameof(batch));
            _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _chequeService = chequeService ?? throw new ArgumentNullException(nameof(chequeService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            // Initialize collections
            _growerPayments = new ObservableCollection<GrowerPaymentSummary>();
            _filteredGrowerPayments = new ObservableCollection<GrowerPaymentSummary>();
            _batchAllocations = new ObservableCollection<ReceiptPaymentAllocation>();
            _filteredAllocations = new ObservableCollection<ReceiptPaymentAllocation>();
            _batchCheques = new ObservableCollection<Cheque>();
            _filteredCheques = new ObservableCollection<Cheque>();
            GrowerFilterOptions = new ObservableCollection<GrowerFilterOption>();
            ChequeStatusOptions = new ObservableCollection<string> { "All", "Issued", "Cleared", "Voided", "Stopped" };

            // Initialize commands
            NavigateBackToBatchListCommand = new RelayCommand(NavigateBackToBatchListExecute);
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());
            ExportToExcelCommand = new RelayCommand(async o => await ExportToExcelAsync());
            ExportToPdfCommand = new RelayCommand(async o => await ExportToPdfAsync());
            PrintBatchReportCommand = new RelayCommand(async o => await PrintBatchReportAsync());

            // Load initial data
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;

                // Load batch summary first
                BatchSummary = await _batchService.GetBatchSummaryAsync(SelectedBatch.PaymentBatchId);

                // Load data for first tab (Grower Payments)
                await LoadGrowerPaymentsAsync();

                // Format batch parameters for Tab 4
                FormatBatchParameters();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error initializing batch detail view for batch {SelectedBatch.PaymentBatchId}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading batch details: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadTabDataAsync(int tabIndex)
        {
            try
            {
                switch (tabIndex)
                {
                    case 0: // Grower Payments
                        if (GrowerPayments.Count == 0)
                            await LoadGrowerPaymentsAsync();
                        break;
                    case 1: // Receipt Allocations
                        if (BatchAllocations.Count == 0)
                            await LoadBatchAllocationsAsync();
                        break;
                    case 2: // Cheques
                        if (BatchCheques.Count == 0)
                            await LoadBatchChequesAsync();
                        break;
                    case 3: // Analytics
                        if (AnalyticsSummary == null)
                            await LoadAnalyticsAsync();
                        break;
                    case 4: // Parameters
                        // Already loaded in Initialize
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading tab {tabIndex} data", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading tab data: {ex.Message}", "Error");
            }
        }

        private async Task LoadGrowerPaymentsAsync()
        {
            try
            {
                IsLoading = true;

                var payments = await _paymentService.GetGrowerPaymentsForBatchAsync(SelectedBatch.PaymentBatchId);

                GrowerPayments.Clear();
                foreach (var payment in payments)
                {
                    GrowerPayments.Add(payment);
                }

                FilterGrowerPayments();

                Logger.Info($"Loaded {payments.Count} grower payments for batch {SelectedBatch.BatchNumber}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading grower payments for batch {SelectedBatch.PaymentBatchId}", ex);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBatchAllocationsAsync()
        {
            try
            {
                IsLoading = true;

                var allocations = await _paymentService.GetBatchAllocationsAsync(SelectedBatch.PaymentBatchId);

                BatchAllocations.Clear();
                foreach (var allocation in allocations)
                {
                    BatchAllocations.Add(allocation);
                }

                // Populate grower filter dropdown
                GrowerFilterOptions.Clear();
                GrowerFilterOptions.Add(new GrowerFilterOption { GrowerId = null, DisplayText = "All Growers" });
                
                var uniqueGrowers = allocations
                    .Where(a => a.GrowerId > 0)
                    .Select(a => new { GrowerId = a.GrowerId, GrowerName = a.GrowerName ?? "" })
                    .Distinct()
                    .OrderBy(g => g.GrowerName);

                foreach (var grower in uniqueGrowers)
                {
                    GrowerFilterOptions.Add(new GrowerFilterOption 
                    { 
                        GrowerId = grower.GrowerId, 
                        DisplayText = grower.GrowerName 
                    });
                }

                FilterAllocations();

                Logger.Info($"Loaded {allocations.Count} receipt allocations for batch {SelectedBatch.BatchNumber}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading batch allocations for batch {SelectedBatch.PaymentBatchId}", ex);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBatchChequesAsync()
        {
            try
            {
                IsLoading = true;

                var cheques = await _chequeService.GetBatchChequesAsync(SelectedBatch.PaymentBatchId);

                BatchCheques.Clear();
                foreach (var cheque in cheques)
                {
                    BatchCheques.Add(cheque);
                }

                FilterCheques();

                Logger.Info($"Loaded {cheques.Count} cheques for batch {SelectedBatch.BatchNumber}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading batch cheques for batch {SelectedBatch.PaymentBatchId}", ex);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FormatBatchParameters()
        {
            try
            {
                var parameters = $"Batch Creation Parameters\n\n";
                parameters += $"Batch Number: {SelectedBatch.BatchNumber}\n";
                parameters += $"Payment Type: {SelectedBatch.PaymentTypeName}\n";
                parameters += $"Batch Date: {SelectedBatch.BatchDate:MMM dd, yyyy}\n";
                parameters += $"Crop Year: {SelectedBatch.CropYear}\n\n";

                if (SelectedBatch.CutoffDate.HasValue)
                {
                    parameters += $"Cutoff Date: {SelectedBatch.CutoffDate:MMM dd, yyyy}\n";
                }

                if (!string.IsNullOrWhiteSpace(SelectedBatch.FilterPayGroup))
                {
                    parameters += $"\nFilters Applied:\n";
                    parameters += $"  Pay Groups: {SelectedBatch.FilterPayGroup}\n";
                }

                if (SelectedBatch.FilterGrower.HasValue)
                {
                    parameters += $"  Specific Grower: {SelectedBatch.FilterGrower}\n";
                }

                if (!string.IsNullOrWhiteSpace(SelectedBatch.Notes))
                {
                    parameters += $"\nNotes:\n{SelectedBatch.Notes}\n";
                }

                parameters += $"\nAudit Information:\n";
                parameters += $"Created: {SelectedBatch.CreatedAt:MMM dd, yyyy HH:mm} by {SelectedBatch.CreatedBy}\n";

                if (SelectedBatch.ProcessedAt.HasValue)
                {
                    parameters += $"Processed: {SelectedBatch.ProcessedAt:MMM dd, yyyy HH:mm} by {SelectedBatch.ProcessedBy}\n";
                }

                BatchParameters = parameters;
            }
            catch (Exception ex)
            {
                Logger.Error("Error formatting batch parameters", ex);
                BatchParameters = "Error loading batch parameters";
            }
        }

        private void FilterGrowerPayments()
        {
            if (GrowerPayments == null)
            {
                FilteredGrowerPayments = new ObservableCollection<GrowerPaymentSummary>();
                return;
            }

            if (string.IsNullOrWhiteSpace(GrowerSearchText))
            {
                FilteredGrowerPayments = new ObservableCollection<GrowerPaymentSummary>(GrowerPayments);
            }
            else
            {
                var searchLower = GrowerSearchText.ToLower();
                FilteredGrowerPayments = new ObservableCollection<GrowerPaymentSummary>(
                    GrowerPayments.Where(g =>
                        (g.GrowerNumber?.ToLower().Contains(searchLower) == true) ||
                        (g.GrowerName?.ToLower().Contains(searchLower) == true)
                    )
                );
            }
        }

        private void FilterAllocations()
        {
            if (BatchAllocations == null)
            {
                FilteredAllocations = new ObservableCollection<ReceiptPaymentAllocation>();
                return;
            }

            if (!SelectedGrowerFilter.HasValue || SelectedGrowerFilter == null)
            {
                FilteredAllocations = new ObservableCollection<ReceiptPaymentAllocation>(BatchAllocations);
            }
            else
            {
                FilteredAllocations = new ObservableCollection<ReceiptPaymentAllocation>(
                    BatchAllocations.Where(a => a.GrowerId == SelectedGrowerFilter.Value)
                );
            }
        }

        private void FilterCheques()
        {
            if (BatchCheques == null)
            {
                FilteredCheques = new ObservableCollection<Cheque>();
                return;
            }

            if (SelectedChequeStatusFilter == "All")
            {
                FilteredCheques = new ObservableCollection<Cheque>(BatchCheques);
            }
            else
            {
                FilteredCheques = new ObservableCollection<Cheque>(
                    BatchCheques.Where(c => c.Status == SelectedChequeStatusFilter)
                );
            }
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
            
            // Reload current tab
            await LoadTabDataAsync(SelectedTabIndex);
        }

        private void NavigateBackToBatchListExecute(object parameter)
        {
            try
            {
                // Navigate back to Payment Batches view through MainViewModel
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // Navigate back to the stored PaymentBatchViewModel instance
                    // This preserves all filters, search text, and state
                    if (mainViewModel.PaymentBatchViewModel != null)
                    {
                        mainViewModel.CurrentView = mainViewModel.PaymentBatchViewModel;
                    }
                    else
                    {
                        // Fallback: trigger navigation to create new instance
                        if (mainViewModel.NavigateToPaymentBatchesCommand?.CanExecute(null) == true)
                        {
                            mainViewModel.NavigateToPaymentBatchesCommand.Execute(null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating back to batch list", ex);
            }
        }

        private void NavigateToDashboardExecute(object parameter)
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (mainViewModel.NavigateToDashboardCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToDashboardCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to dashboard", ex);
            }
        }

        /// <summary>
        /// Load analytics data for the batch
        /// </summary>
        private async Task LoadAnalyticsAsync()
        {
            try
            {
                IsLoading = true;

                var analytics = await _paymentService.CalculateBatchAnalyticsAsync(SelectedBatch.PaymentBatchId);
                AnalyticsSummary = analytics;

                Logger.Info($"Loaded analytics for batch {SelectedBatch.BatchNumber}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading analytics for batch {SelectedBatch.PaymentBatchId}", ex);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Export batch data to Excel
        /// </summary>
        private async Task ExportToExcelAsync()
        {
            try
            {
                IsExporting = true;

                // Show selection dialog for export type
                var exportOptions = new System.Collections.Generic.List<string>
                {
                    "Complete Batch (All Data)",
                    "Grower Payments Only",
                    "Receipt Allocations Only",
                    "Cheques Only"
                };

                // For now, use a simple approach - export complete batch
                // TODO: Implement selection dialog in future iteration
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    FileName = $"PaymentBatch_{SelectedBatch.BatchNumber}_{DateTime.Now:yyyyMMdd}.xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    bool success = await _exportService.ExportCompleteBatchToExcelAsync(
                        SelectedBatch.PaymentBatchId, saveDialog.FileName);

                    if (success)
                    {
                        await _dialogService.ShowMessageBoxAsync(
                            "Export completed successfully!", "Success");

                        Logger.Info($"Exported batch {SelectedBatch.BatchNumber} to {saveDialog.FileName}");

                        // Open the file
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync(
                            "Export failed. Please check the log for details.", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting batch {SelectedBatch.PaymentBatchId} to Excel", ex);
                await _dialogService.ShowMessageBoxAsync($"Export failed: {ex.Message}", "Error");
            }
            finally
            {
                IsExporting = false;
            }
        }

        /// <summary>
        /// Export batch summary to PDF
        /// </summary>
        private async Task ExportToPdfAsync()
        {
            try
            {
                IsExporting = true;

                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF Document (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = $"PaymentBatch_{SelectedBatch.BatchNumber}_Summary_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    bool success = await _exportService.ExportBatchSummaryToPdfAsync(
                        SelectedBatch.PaymentBatchId, saveDialog.FileName);

                    if (success)
                    {
                        await _dialogService.ShowMessageBoxAsync(
                            "PDF export completed successfully!", "Success");

                        Logger.Info($"Exported batch {SelectedBatch.BatchNumber} to PDF: {saveDialog.FileName}");

                        // Open the file
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDialog.FileName,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync(
                            "PDF export failed. Please check the log for details.", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting batch {SelectedBatch.PaymentBatchId} to PDF", ex);
                await _dialogService.ShowMessageBoxAsync($"PDF export failed: {ex.Message}", "Error");
            }
            finally
            {
                IsExporting = false;
            }
        }

        /// <summary>
        /// Print batch report
        /// </summary>
        private async Task PrintBatchReportAsync()
        {
            try
            {
                IsExporting = true;

                var printDialog = new System.Windows.Controls.PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // Create FlowDocument for printing
                    var document = CreatePrintableDocument();

                    // Print the document
                    printDialog.PrintDocument(
                        ((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator,
                        $"Payment Batch {SelectedBatch.BatchNumber}");

                    await _dialogService.ShowMessageBoxAsync(
                        "Print job sent successfully!", "Success");

                    Logger.Info($"Printed batch report for {SelectedBatch.BatchNumber}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing batch {SelectedBatch.PaymentBatchId}", ex);
                await _dialogService.ShowMessageBoxAsync($"Print failed: {ex.Message}", "Error");
            }
            finally
            {
                IsExporting = false;
            }
        }

        /// <summary>
        /// Create a printable FlowDocument for the batch
        /// </summary>
        private System.Windows.Documents.FlowDocument CreatePrintableDocument()
        {
            var document = new System.Windows.Documents.FlowDocument();
            document.PagePadding = new System.Windows.Thickness(50);
            document.ColumnWidth = double.PositiveInfinity;

            // Title
            var title = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"Payment Batch Report - {SelectedBatch.BatchNumber}"))
            {
                FontSize = 24,
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 0, 0, 20)
            };
            document.Blocks.Add(title);

            // Batch Info
            var batchInfo = new System.Windows.Documents.Paragraph();
            batchInfo.Inlines.Add(new System.Windows.Documents.Run($"Payment Type: {SelectedBatch.PaymentTypeName}\n"));
            batchInfo.Inlines.Add(new System.Windows.Documents.Run($"Batch Date: {SelectedBatch.BatchDate:MMMM dd, yyyy}\n"));
            batchInfo.Inlines.Add(new System.Windows.Documents.Run($"Crop Year: {SelectedBatch.CropYear}\n"));
            batchInfo.Inlines.Add(new System.Windows.Documents.Run($"Status: {SelectedBatch.Status}\n"));
            batchInfo.Margin = new System.Windows.Thickness(0, 0, 0, 20);
            document.Blocks.Add(batchInfo);

            // Financial Summary
            if (BatchSummary != null)
            {
                var financialHeader = new System.Windows.Documents.Paragraph(
                    new System.Windows.Documents.Run("Financial Summary"))
                {
                    FontSize = 18,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Margin = new System.Windows.Thickness(0, 10, 0, 10)
                };
                document.Blocks.Add(financialHeader);

                var financial = new System.Windows.Documents.Paragraph();
                financial.Inlines.Add(new System.Windows.Documents.Run($"Total Amount: {BatchSummary.TotalAmount:C2}\n"));
                financial.Inlines.Add(new System.Windows.Documents.Run($"Total Growers: {BatchSummary.TotalGrowers}\n"));
                financial.Inlines.Add(new System.Windows.Documents.Run($"Total Receipts: {BatchSummary.TotalReceipts}\n"));
                
                // Calculate total weight from grower payments
                decimal totalWeight = GrowerPayments.Sum(p => p.TotalWeight);
                financial.Inlines.Add(new System.Windows.Documents.Run($"Total Weight: {totalWeight:N2} lbs\n"));
                document.Blocks.Add(financial);
            }

            // Grower Payments Table
            var growerHeader = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run("Grower Payments"))
            {
                FontSize = 18,
                FontWeight = System.Windows.FontWeights.Bold,
                Margin = new System.Windows.Thickness(0, 20, 0, 10)
            };
            document.Blocks.Add(growerHeader);

            var table = CreateGrowerPaymentsTable();
            document.Blocks.Add(table);

            // Footer
            var footer = new System.Windows.Documents.Paragraph(
                new System.Windows.Documents.Run($"Generated: {DateTime.Now:MMMM dd, yyyy HH:mm:ss}"))
            {
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new System.Windows.Thickness(0, 20, 0, 0)
            };
            document.Blocks.Add(footer);

            return document;
        }

        /// <summary>
        /// Create table for grower payments in print document
        /// </summary>
        private System.Windows.Documents.Table CreateGrowerPaymentsTable()
        {
            var table = new System.Windows.Documents.Table();
            table.CellSpacing = 0;
            table.BorderBrush = System.Windows.Media.Brushes.Black;
            table.BorderThickness = new System.Windows.Thickness(1);

            // Define columns
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(80) });  // Grower #
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(150) }); // Name
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(60) });  // Receipts
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(80) });  // Weight
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new System.Windows.GridLength(80) });  // Amount

            // Add table row group
            table.RowGroups.Add(new System.Windows.Documents.TableRowGroup());

            // Header row
            var headerRow = new System.Windows.Documents.TableRow();
            headerRow.Background = System.Windows.Media.Brushes.LightGray;

            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Grower #")))
                { FontWeight = System.Windows.FontWeights.Bold });
            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Name")))
                { FontWeight = System.Windows.FontWeights.Bold });
            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Receipts")))
                { FontWeight = System.Windows.FontWeights.Bold });
            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Weight")))
                { FontWeight = System.Windows.FontWeights.Bold });
            headerRow.Cells.Add(new System.Windows.Documents.TableCell(
                new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("Amount")))
                { FontWeight = System.Windows.FontWeights.Bold });

            table.RowGroups[0].Rows.Add(headerRow);

            // Data rows
            foreach (var payment in GrowerPayments.Take(50)) // Limit to first 50 for print
            {
                var dataRow = new System.Windows.Documents.TableRow();

                dataRow.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(payment.GrowerNumber))));
                dataRow.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(payment.GrowerName))));
                dataRow.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(payment.ReceiptCount.ToString()))));
                dataRow.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"{payment.TotalWeight:N2}"))));
                dataRow.Cells.Add(new System.Windows.Documents.TableCell(
                    new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"{payment.TotalAmount:C2}"))));

                table.RowGroups[0].Rows.Add(dataRow);
            }

            return table;
        }

        /// <summary>
        /// Show contextual help for the Payment Batch Detail view
        /// </summary>
        private async void ShowHelpExecute(object parameter)
        {
            try
            {
                // Get help content for this view
                var helpContent = _helpContentProvider.GetHelpContent("PaymentBatchDetailView");
                
                // Create help dialog ViewModel
                var helpViewModel = new HelpDialogViewModel(
                    helpContent.Title,
                    helpContent.Content,
                    helpContent.QuickTips,
                    helpContent.KeyboardShortcuts
                );
                
                // Show the help dialog
                await _dialogService.ShowDialogAsync(helpViewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Error showing help", ex);
                await _dialogService.ShowMessageBoxAsync("Unable to display help content at this time.", "Error");
            }
        }
    }

    /// <summary>
    /// Helper class for grower filter dropdown
    /// </summary>
    public class GrowerFilterOption
    {
        public int? GrowerId { get; set; }
        public string DisplayText { get; set; } = string.Empty;
    }
}

