using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for managing payment batches (view, filter, approve, process, void)
    /// </summary>
    public class PaymentBatchViewModel : ViewModelBase
    {
        private readonly IPaymentBatchManagementService _batchService;
        private readonly IPaymentTypeService _paymentTypeService;
        private readonly IChequeGenerationService _chequeService;
        private readonly IPaymentService _paymentService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IPaymentBatchExportService _exportService;

        // Properties
        private ObservableCollection<PaymentBatch> _paymentBatches;
        private PaymentBatch? _selectedBatch;
        private PaymentBatchSummary? _batchSummary;
        private ObservableCollection<Cheque> _batchCheques;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private int? _filterCropYear;
        private string? _filterStatus;
        private int? _filterPaymentType;

        public ObservableCollection<PaymentBatch> PaymentBatches
        {
            get => _paymentBatches;
            set => SetProperty(ref _paymentBatches, value);
        }

        public PaymentBatch? SelectedBatch
        {
            get => _selectedBatch;
            set
            {
                if (SetProperty(ref _selectedBatch, value))
                {
                    _ = LoadBatchDetailsAsync();
                }
            }
        }

        public PaymentBatchSummary? BatchSummary
        {
            get => _batchSummary;
            set => SetProperty(ref _batchSummary, value);
        }

        public ObservableCollection<Cheque> BatchCheques
        {
            get => _batchCheques;
            set => SetProperty(ref _batchCheques, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = SearchBatchesAsync();
                }
            }
        }

        public int? FilterCropYear
        {
            get => _filterCropYear ?? DateTime.Today.Year;
            set
            {
                if (SetProperty(ref _filterCropYear, value))
                {
                    _ = LoadBatchesAsync();
                }
            }
        }

        public string? FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (SetProperty(ref _filterStatus, value))
                {
                    _ = LoadBatchesAsync();
                }
            }
        }

        public int? FilterPaymentType
        {
            get => _filterPaymentType;
            set
            {
                if (SetProperty(ref _filterPaymentType, value))
                {
                    _ = LoadBatchesAsync();
                }
            }
        }

        // Filter options
        public ObservableCollection<int> CropYears { get; } = new ObservableCollection<int>();
        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>
        {
            "All",
            "Draft",
            "Posted",
            "Finalized",
            "Voided"
        };
        public ObservableCollection<PaymentType> PaymentTypes { get; } = new ObservableCollection<PaymentType>();

        // Commands
        public ICommand LoadBatchesCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewBatchCommand { get; }
        public ICommand VoidBatchCommand { get; }
        public ICommand ExportBatchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ApproveBatchCommand { get; }
        public ICommand PostBatchCommand { get; }
        public ICommand ProcessPaymentsCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToPaymentManagementCommand { get; }

        public PaymentBatchViewModel(
            IPaymentBatchManagementService batchService,
            IPaymentTypeService paymentTypeService,
            IChequeGenerationService chequeService,
            IPaymentService paymentService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IPaymentBatchExportService exportService)
        {
            _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
            _paymentTypeService = paymentTypeService ?? throw new ArgumentNullException(nameof(paymentTypeService));
            _chequeService = chequeService ?? throw new ArgumentNullException(nameof(chequeService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));

            _paymentBatches = new ObservableCollection<PaymentBatch>();
            _batchCheques = new ObservableCollection<Cheque>();

            // Initialize commands
            LoadBatchesCommand = new RelayCommand(async o => await LoadBatchesAsync());
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());
            ViewBatchCommand = new RelayCommand(async o => await ViewBatchDetailsAsync(), o => SelectedBatch != null);
            VoidBatchCommand = new RelayCommand(async o => await VoidBatchAsync(), o => CanVoidBatch());
            ExportBatchCommand = new RelayCommand(async o => await ExportBatchAsync(), o => SelectedBatch != null);
            ClearFiltersCommand = new RelayCommand(o => ClearFilters());
            ApproveBatchCommand = new RelayCommand(async o => await ApproveBatchAsync(), o => CanApproveBatch());
            PostBatchCommand = new RelayCommand(async o => await PostBatchAsync(), o => CanPostBatch());
            ProcessPaymentsCommand = new RelayCommand(async o => await ProcessPaymentsAsync(), o => CanProcessPayments());
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToPaymentManagementCommand = new RelayCommand(NavigateToPaymentManagementExecute);

            // Initialize filters
            InitializeFilters();

            // Load data
            _ = InitializeAsync();
        }

        // ==============================================================
        // INITIALIZATION
        // ==============================================================

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;

                // Load payment types
                var types = await _paymentTypeService.GetAllPaymentTypesAsync();
                PaymentTypes.Clear();
                PaymentTypes.Add(new PaymentType { PaymentTypeId = 0, TypeName = "All Types" }); // For "All" filter
                foreach (var type in types)
                {
                    PaymentTypes.Add(type);
                }

                // Load crop years (current year and previous 3 years)
                CropYears.Clear();
                CropYears.Add(0); // For "All Years" filter
                for (int i = 0; i < 4; i++)
                {
                    CropYears.Add(DateTime.Today.Year - i);
                }

                // Set default filter to current year
                FilterCropYear = DateTime.Today.Year;

                // Load batches
                await LoadBatchesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing PaymentBatchViewModel", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading batches: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void InitializeFilters()
        {
            FilterCropYear = DateTime.Today.Year;
            FilterStatus = "All";
            FilterPaymentType = 0; // All types
        }

        // ==============================================================
        // DATA LOADING
        // ==============================================================

        private async Task LoadBatchesAsync()
        {
            try
            {
                IsLoading = true;

                var cropYear = FilterCropYear == 0 ? (int?)null : FilterCropYear;
                var status = FilterStatus == "All" ? null : FilterStatus;
                var paymentTypeId = FilterPaymentType == 0 ? (int?)null : FilterPaymentType;

                var batches = await _batchService.GetAllPaymentBatchesAsync(
                    cropYear: cropYear,
                    status: status,
                    paymentTypeId: paymentTypeId);

                PaymentBatches.Clear();
                foreach (var batch in batches)
                {
                    PaymentBatches.Add(batch);
                }

                Logger.Info($"Loaded {batches.Count} payment batches");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading batches", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading batches: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBatchDetailsAsync()
        {
            if (SelectedBatch == null)
            {
                BatchSummary = null;
                BatchCheques.Clear();
                return;
            }

            try
            {
                // Load batch summary
                BatchSummary = await _batchService.GetBatchSummaryAsync(SelectedBatch.PaymentBatchId);

                // Load cheques for this batch
                var cheques = await _chequeService.GetBatchChequesAsync(SelectedBatch.PaymentBatchId);
                BatchCheques.Clear();
                foreach (var cheque in cheques)
                {
                    BatchCheques.Add(cheque);
                }

                Logger.Info($"Loaded details for batch {SelectedBatch.BatchNumber}: {cheques.Count} cheques");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading batch details", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading batch details: {ex.Message}", "Error");
            }
        }

        // ==============================================================
        // COMMANDS
        // ==============================================================

        private async Task RefreshAsync()
        {
            await LoadBatchesAsync();
            if (SelectedBatch != null)
            {
                await LoadBatchDetailsAsync();
            }
        }

        private async Task SearchBatchesAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadBatchesAsync();
                return;
            }

            try
            {
                IsLoading = true;

                var allBatches = await _batchService.GetAllPaymentBatchesAsync();
                var filtered = allBatches.Where(b =>
                    b.BatchNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (b.PaymentTypeName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));

                PaymentBatches.Clear();
                foreach (var batch in filtered)
                {
                    PaymentBatches.Add(batch);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error searching batches", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ViewBatchDetailsAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                // Create detail view ViewModel
                var detailViewModel = new PaymentBatchDetailViewModel(
                    SelectedBatch,
                    _batchService,
                    _paymentService,
                    _chequeService,
                    _dialogService,
                    _helpContentProvider,
                    _exportService);

                // Navigate to detail view through MainViewModel
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // Store reference to this ViewModel so navigation back preserves state
                    mainViewModel.PaymentBatchViewModel = this;
                    
                    // Navigate to detail view - MainWindow's ContentControl will create the view
                    mainViewModel.CurrentView = detailViewModel;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error showing batch details", ex);
                await _dialogService.ShowMessageBoxAsync($"Error showing batch details: {ex.Message}", "Error");
            }
        }

        private async Task VoidBatchAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                IsLoading = true;

                // STEP 1: VALIDATE - Check if batch can be voided
                var (canVoid, validationReasons) = await _batchService.ValidateCanVoidBatchAsync(
                    SelectedBatch.PaymentBatchId);

                if (!canVoid)
                {
                    // Show validation error with detailed explanation
                    var errorMessage = string.Join("\n", validationReasons);
                    await _dialogService.ShowMessageBoxAsync(
                        errorMessage,
                        "⚠️ Cannot Void Batch - Payment Sequence Conflict");
                    return;
                }

                // STEP 2: CONFIRM - Validation passed, now confirm with user
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will void the batch and all associated data:\n\n" +
                    $"✓ Batch will be marked as Voided\n" +
                    $"✓ All payment allocations will be voided\n" +
                    $"✓ All cheques will be cancelled\n" +
                    $"✓ Receipts will become available for future batches\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Status: {SelectedBatch.Status}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n" +
                    $"Growers: {SelectedBatch.TotalGrowers}\n\n" +
                    $"This action cannot be undone. Continue?",
                    $"Void Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                // STEP 3: GET REASON - Ask for void reason
                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for voiding this batch:",
                    "Void Reason");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    await _dialogService.ShowMessageBoxAsync("Void reason is required.", "Required");
                    return;
                }

                // STEP 4: EXECUTE VOID - Proceed with void operation
                var voidedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.VoidBatchAsync(
                    SelectedBatch.PaymentBatchId,
                    reason,
                    voidedBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"✓ Batch {SelectedBatch.BatchNumber} has been voided.\n\n" +
                        $"All allocations and cheques have been cancelled.\n" +
                        $"Receipts are now available for future payment runs.",
                        "Batch Voided Successfully");

                    // Refresh list
                    await LoadBatchesAsync();
                }
            }
            catch (InvalidOperationException ex)
            {
                // This handles validation errors that were caught by the service
                Logger.Warn($"Void batch blocked: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync(
                    ex.Message,
                    "⚠️ Cannot Void Batch");
            }
            catch (Exception ex)
            {
                Logger.Error("Error voiding batch", ex);
                await _dialogService.ShowMessageBoxAsync(
                    $"An unexpected error occurred while voiding the batch:\n\n{ex.Message}",
                    "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportBatchAsync()
        {
            // TODO: Implement export to Excel/PDF
            await _dialogService.ShowMessageBoxAsync("Export functionality coming soon!", "Not Implemented");
        }

        private void ClearFilters()
        {
            FilterCropYear = 0;
            FilterStatus = "All";
            FilterPaymentType = 0;
            SearchText = string.Empty;
        }

        // ==============================================================
        // HELPER METHODS
        // ==============================================================

        private async Task ApproveBatchAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                // Confirm approval
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will approve the batch for posting.\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n" +
                    $"Growers: {SelectedBatch.TotalGrowers}\n\n" +
                    $"After approval, you can post the batch to create payment records.\n" +
                    $"Continue with approval?",
                    $"Approve Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                IsLoading = true;

                // Approve the batch (Draft → Approved)
                var approvedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.ApproveBatchAsync(
                    SelectedBatch.PaymentBatchId,
                    approvedBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Batch {SelectedBatch.BatchNumber} has been approved.\n\n" +
                        $"Status: Approved (Ready for posting)",
                        "Batch Approved");

                    // Refresh list
                    await LoadBatchesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error approving batch", ex);
                await _dialogService.ShowMessageBoxAsync($"Error approving batch: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PostBatchAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                // Confirm posting
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will post the batch and create payment records.\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n" +
                    $"Growers: {SelectedBatch.TotalGrowers}\n\n" +
                    $"After posting, you can generate cheques or leave as payables.\n" +
                    $"Continue with posting?",
                    $"Post Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                IsLoading = true;

                // Post the batch (Approved → Posted)
                var postedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.PostBatchAsync(
                    SelectedBatch.PaymentBatchId,
                    postedBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Batch {SelectedBatch.BatchNumber} has been posted.\n\n" +
                        $"Status: Posted (Ready for payment distribution)",
                        "Batch Posted");

                    // Refresh list
                    await LoadBatchesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error posting batch", ex);
                await _dialogService.ShowMessageBoxAsync($"Error posting batch: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ProcessPaymentsAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                // Confirm finalization
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will finalize the batch and lock all payment records.\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n" +
                    $"Growers: {SelectedBatch.TotalGrowers}\n\n" +
                    $"This action cannot be undone. Continue?",
                    $"Finalize Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                IsLoading = true;

                // Finalize payments (Posted → Finalized)
                var processedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.ProcessPaymentsAsync(
                    SelectedBatch.PaymentBatchId,
                    processedBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Batch {SelectedBatch.BatchNumber} has been finalized.\n\n" +
                        $"Status: Finalized (Payments locked)",
                        "Batch Finalized");

                    // Refresh list
                    await LoadBatchesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error finalizing batch", ex);
                await _dialogService.ShowMessageBoxAsync($"Error finalizing batch: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanApproveBatch()
        {
            return SelectedBatch != null &&
                   SelectedBatch.Status == "Draft" &&
                   !SelectedBatch.IsDeleted;
        }

        private bool CanPostBatch()
        {
            return SelectedBatch != null &&
                   SelectedBatch.Status == "Approved" &&
                   !SelectedBatch.IsDeleted;
        }

        private bool CanProcessPayments()
        {
            return SelectedBatch != null &&
                   SelectedBatch.Status == "Posted" &&
                   !SelectedBatch.IsDeleted;
        }

        private bool CanVoidBatch()
        {
            return SelectedBatch != null &&
                   (SelectedBatch.Status == "Draft" || SelectedBatch.Status == "Approved" || SelectedBatch.Status == "Posted") &&
                   !SelectedBatch.IsDeleted;
        }

        private void NavigateToDashboardExecute(object? parameter)
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // Execute the dashboard navigation command
                    if (mainViewModel.NavigateToDashboardCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToDashboardCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Dashboard", ex);
            }
        }

        private void NavigateToPaymentManagementExecute(object? parameter)
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // Execute the payment management navigation command
                    if (mainViewModel.NavigateToPaymentManagementCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToPaymentManagementCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Payment Management", ex);
            }
        }
    }
}


