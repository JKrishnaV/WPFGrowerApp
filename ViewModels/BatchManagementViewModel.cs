using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.ViewModels.Dialogs;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// Batch Management ViewModel - Manages import batches and their receipts
    /// </summary>
    public class BatchManagementViewModel : ViewModelBase
    {
        private readonly IImportBatchService _importBatchService;
        private readonly IReceiptService _receiptService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IImportBatchProcessor _importBatchProcessor;

        private ObservableCollection<ImportBatch> _importBatches;
        private ObservableCollection<ImportBatch> _filteredBatches;
        private ImportBatch _selectedBatch;
        private ObservableCollection<Receipt> _batchReceipts;
        private ObservableCollection<Receipt> _filteredReceipts;
        private bool _isLoading;
        private bool _isExporting;
        private string _searchText;
        private string _batchSearchText;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private string _statusMessage = "Ready";
        private DateTime _lastUpdated = DateTime.Now;

        public BatchManagementViewModel(
            IImportBatchService importBatchService,
            IReceiptService receiptService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IImportBatchProcessor importBatchProcessor)
        {
            _importBatchService = importBatchService ?? throw new ArgumentNullException(nameof(importBatchService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _importBatchProcessor = importBatchProcessor ?? throw new ArgumentNullException(nameof(importBatchProcessor));

            _importBatches = new ObservableCollection<ImportBatch>();
            _filteredBatches = new ObservableCollection<ImportBatch>();
            _batchReceipts = new ObservableCollection<Receipt>();
            _filteredReceipts = new ObservableCollection<Receipt>();

            // Initialize commands
            InitializeCommands();

            // Load initial data
            _ = LoadBatchesAsync();
        }

        public ObservableCollection<ImportBatch> ImportBatches
        {
            get => _importBatches;
            set => SetProperty(ref _importBatches, value);
        }

        public ObservableCollection<ImportBatch> FilteredBatches
        {
            get => _filteredBatches;
            set => SetProperty(ref _filteredBatches, value);
        }

        public ImportBatch SelectedBatch
        {
            get => _selectedBatch;
            set
            {
                if (SetProperty(ref _selectedBatch, value))
                {
                    _ = LoadBatchReceiptsAsync();
                }
            }
        }

        public ObservableCollection<Receipt> BatchReceipts
        {
            get => _batchReceipts;
            set => SetProperty(ref _batchReceipts, value);
        }

        public ObservableCollection<Receipt> FilteredReceipts
        {
            get => _filteredReceipts;
            set => SetProperty(ref _filteredReceipts, value);
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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterBatches();
                }
            }
        }

        public string BatchSearchText
        {
            get => _batchSearchText;
            set
            {
                if (SetProperty(ref _batchSearchText, value))
                {
                    FilterReceipts();
                }
            }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadBatchesAsync();
                }
            }
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = LoadBatchesAsync();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        // Commands
        public ICommand ShowHelpCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand LoadBatchesCommand { get; private set; }
        public ICommand ViewBatchReceiptsCommand { get; private set; }
        public ICommand DeleteBatchCommand { get; private set; }
        public ICommand CloseBatchCommand { get; private set; }
        public ICommand ReopenBatchCommand { get; private set; }
        public ICommand ExportToExcelCommand { get; private set; }
        public ICommand ExportToPdfCommand { get; private set; }
        public ICommand PrintReportCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        
        // Navigation Commands
        public ICommand NavigateToDashboardCommand { get; private set; }
        public ICommand NavigateToImportHubCommand { get; private set; }

        private void InitializeCommands()
        {
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            LoadBatchesCommand = new RelayCommand(async _ => await LoadBatchesAsync());
            ViewBatchReceiptsCommand = new RelayCommand(async _ => await LoadBatchReceiptsAsync());
            DeleteBatchCommand = new RelayCommand(async _ => await DeleteBatchAsync(), _ => SelectedBatch != null);
            CloseBatchCommand = new RelayCommand(async _ => await CloseBatchAsync(), _ => SelectedBatch != null);
            ReopenBatchCommand = new RelayCommand(async _ => await ReopenBatchAsync(), _ => SelectedBatch != null);
            ExportToExcelCommand = new RelayCommand(async _ => await ExportToExcelAsync());
            ExportToPdfCommand = new RelayCommand(async _ => await ExportToPdfAsync());
            PrintReportCommand = new RelayCommand(async _ => await PrintReportAsync());
            ClearFiltersCommand = new RelayCommand(ClearFiltersExecute);
            
            // Navigation Commands
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToImportHubCommand = new RelayCommand(NavigateToImportHubExecute);
        }

        private async Task LoadBatchesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading import batches...";
                Logger.Info("Loading import batches...");

                var batches = await _importBatchService.GetImportBatchesAsync(StartDate, EndDate);
                
                // Debug: Log the first batch to see what data we're getting
                if (batches.Any())
                {
                    var firstBatch = batches.First();
                    Logger.Info($"First batch data - BatchNumber: {firstBatch.BatchNumber}, SourceFileName: {firstBatch.SourceFileName}, ImportDate: {firstBatch.ImportDate}, TotalReceipts: {firstBatch.TotalReceipts}, DepotId: {firstBatch.DepotId}");
                }
                
                ImportBatches.Clear();
                foreach (var batch in batches)
                {
                    ImportBatches.Add(batch);
                }

                FilterBatches();
                LastUpdated = DateTime.Now;
                StatusMessage = $"Loaded {batches.Count} import batches";
                Logger.Info($"Loaded {batches.Count} import batches");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading import batches", ex);
                StatusMessage = "Error loading batches";
                await _dialogService.ShowMessageBoxAsync($"Error loading batches: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBatchReceiptsAsync()
        {
            if (SelectedBatch == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = $"Loading receipts for batch {SelectedBatch.BatchNumber}...";
                Logger.Info($"Loading receipts for batch {SelectedBatch.BatchNumber}...");

                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(decimal.Parse(SelectedBatch.BatchNumber));
                
                BatchReceipts.Clear();
                foreach (var receipt in receipts)
                {
                    BatchReceipts.Add(receipt);
                }

                FilterReceipts();
                StatusMessage = $"Loaded {receipts.Count} receipts for batch {SelectedBatch.BatchNumber}";
                Logger.Info($"Loaded {receipts.Count} receipts for batch {SelectedBatch.BatchNumber}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading receipts for batch {SelectedBatch.BatchNumber}", ex);
                StatusMessage = "Error loading receipts";
                await _dialogService.ShowMessageBoxAsync($"Error loading receipts: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterBatches()
        {
            var filtered = ImportBatches.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(batch => 
                    batch.BatchNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    batch.SourceFileName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                    batch.Status?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
            }

            FilteredBatches.Clear();
            foreach (var batch in filtered)
            {
                FilteredBatches.Add(batch);
            }
        }

        private void FilterReceipts()
        {
            var filtered = BatchReceipts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(BatchSearchText))
            {
                filtered = filtered.Where(receipt => 
                    receipt.ReceiptNumber.ToString().Contains(BatchSearchText, StringComparison.OrdinalIgnoreCase) ||
                    receipt.GrowerName?.Contains(BatchSearchText, StringComparison.OrdinalIgnoreCase) == true);
            }

            FilteredReceipts.Clear();
            foreach (var receipt in filtered)
            {
                FilteredReceipts.Add(receipt);
            }
        }

        private async Task DeleteBatchAsync()
        {
            if (SelectedBatch == null) return;

            try
            {
                var confirm = await _dialogService.ShowConfirmationDialogAsync(
                    "Delete Batch", 
                    $"Are you sure you want to permanently delete batch {SelectedBatch.BatchNumber}? This action cannot be undone.");

                if (!confirm) return;

                // Check if batch has receipts
                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(decimal.Parse(SelectedBatch.BatchNumber));
                if (receipts.Any())
                {
                    var deleteWithReceipts = await _dialogService.ShowConfirmationDialogAsync(
                        "Batch Has Receipts", 
                        $"This batch contains {receipts.Count} receipts. Do you want to delete the batch and all its receipts?");
                    
                    if (!deleteWithReceipts) return;
                }

                // Use the new delete functionality
                var success = await _importBatchService.DeleteImportBatchAsync(SelectedBatch.BatchNumber);
                if (!success)
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to delete batch", "Error");
                    return;
                }
                
                Logger.Info($"Batch {SelectedBatch.BatchNumber} deleted successfully");
                await _dialogService.ShowMessageBoxAsync($"Batch {SelectedBatch.BatchNumber} deleted successfully", "Success");
                
                // Refresh the list
                await LoadBatchesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting batch {SelectedBatch.BatchNumber}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting batch: {ex.Message}", "Error");
            }
        }


        private async Task CloseBatchAsync()
        {
            if (SelectedBatch == null) return;

            try
            {
                var success = await _importBatchService.CloseImportBatchAsync(SelectedBatch.BatchNumber);
                if (success)
                {
                    Logger.Info($"Batch {SelectedBatch.BatchNumber} closed successfully");
                    await _dialogService.ShowMessageBoxAsync($"Batch {SelectedBatch.BatchNumber} closed successfully", "Success");
                    await LoadBatchesAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to close batch", "Error");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error closing batch {SelectedBatch.BatchNumber}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error closing batch: {ex.Message}", "Error");
            }
        }

        private async Task ReopenBatchAsync()
        {
            if (SelectedBatch == null) return;

            try
            {
                var success = await _importBatchService.ReopenImportBatchAsync(SelectedBatch.BatchNumber);
                if (success)
                {
                    Logger.Info($"Batch {SelectedBatch.BatchNumber} reopened successfully");
                    await _dialogService.ShowMessageBoxAsync($"Batch {SelectedBatch.BatchNumber} reopened successfully", "Success");
                    await LoadBatchesAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to reopen batch", "Error");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reopening batch {SelectedBatch.BatchNumber}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error reopening batch: {ex.Message}", "Error");
            }
        }

        private async Task RefreshAsync()
        {
            await LoadBatchesAsync();
        }

        private async Task ExportToExcelAsync()
        {
            try
            {
                IsExporting = true;
                StatusMessage = "Exporting to Excel...";

                // TODO: Implement Excel export functionality
                await _dialogService.ShowMessageBoxAsync("Excel export functionality will be implemented", "Info");
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting to Excel", ex);
                await _dialogService.ShowMessageBoxAsync($"Export failed: {ex.Message}", "Error");
            }
            finally
            {
                IsExporting = false;
            }
        }

        private async Task ExportToPdfAsync()
        {
            try
            {
                IsExporting = true;
                StatusMessage = "Exporting to PDF...";

                // TODO: Implement PDF export functionality
                await _dialogService.ShowMessageBoxAsync("PDF export functionality will be implemented", "Info");
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting to PDF", ex);
                await _dialogService.ShowMessageBoxAsync($"Export failed: {ex.Message}", "Error");
            }
            finally
            {
                IsExporting = false;
            }
        }

        private async Task PrintReportAsync()
        {
            try
            {
                // TODO: Implement print functionality
                await _dialogService.ShowMessageBoxAsync("Print functionality will be implemented", "Info");
            }
            catch (Exception ex)
            {
                Logger.Error("Error printing report", ex);
                await _dialogService.ShowMessageBoxAsync($"Print failed: {ex.Message}", "Error");
            }
        }

        private void ClearFiltersExecute(object parameter)
        {
            SearchText = string.Empty;
            BatchSearchText = string.Empty;
            StartDate = null;
            EndDate = null;
        }

        private async void ShowHelpExecute(object parameter)
        {
            try
            {
                var helpContent = _helpContentProvider.GetHelpContent("BatchManagementView");
                var helpViewModel = new HelpDialogViewModel(
                    helpContent.Title,
                    helpContent.Content,
                    helpContent.QuickTips,
                    helpContent.KeyboardShortcuts
                );
                await _dialogService.ShowDialogAsync(helpViewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Error showing help", ex);
                await _dialogService.ShowMessageBoxAsync("Unable to display help.", "Error");
            }
        }

        private void NavigateToDashboardExecute(object parameter)
        {
            try
            {
                Logger.Info("Navigating to Dashboard from Batch Management");
                NavigationHelper.NavigateToDashboard();
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Dashboard", ex);
            }
        }

        private void NavigateToImportHubExecute(object parameter)
        {
            try
            {
                Logger.Info("Navigating to Import Hub from Batch Management");
                NavigationHelper.NavigateToImportHub();
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Import Hub", ex);
            }
        }
    }
}
