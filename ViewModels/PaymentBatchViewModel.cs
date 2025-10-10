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
    /// ViewModel for managing payment batches (view, filter, void)
    /// </summary>
    public class PaymentBatchViewModel : ViewModelBase
    {
        private readonly IPaymentBatchManagementService _batchService;
        private readonly IPaymentTypeService _paymentTypeService;
        private readonly IChequeGenerationService _chequeService;
        private readonly IDialogService _dialogService;

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
        public ICommand ProcessPaymentsCommand { get; }
        public ICommand RollbackBatchCommand { get; }

        public PaymentBatchViewModel(
            IPaymentBatchManagementService batchService,
            IPaymentTypeService paymentTypeService,
            IChequeGenerationService chequeService,
            IDialogService dialogService)
        {
            _batchService = batchService ?? throw new ArgumentNullException(nameof(batchService));
            _paymentTypeService = paymentTypeService ?? throw new ArgumentNullException(nameof(paymentTypeService));
            _chequeService = chequeService ?? throw new ArgumentNullException(nameof(chequeService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

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
            ProcessPaymentsCommand = new RelayCommand(async o => await ProcessPaymentsAsync(), o => CanProcessPayments());
            RollbackBatchCommand = new RelayCommand(async o => await RollbackBatchAsync(), o => CanRollbackBatch());

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
            if (SelectedBatch == null || BatchSummary == null)
                return;

            var message = $"Batch Details:\n\n" +
                         $"Batch Number: {BatchSummary.BatchNumber}\n" +
                         $"Payment Type: {BatchSummary.PaymentTypeName}\n" +
                         $"Batch Date: {BatchSummary.BatchDate:yyyy-MM-dd}\n" +
                         $"Crop Year: {BatchSummary.CropYear}\n" +
                         $"Status: {BatchSummary.Status}\n\n" +
                         $"Totals:\n" +
                         $"  Growers: {BatchSummary.TotalGrowers}\n" +
                         $"  Receipts: {BatchSummary.TotalReceipts}\n" +
                         $"  Amount: ${BatchSummary.TotalAmount:N2}\n" +
                         $"  Cheques: {BatchSummary.ChequesGenerated}\n\n" +
                         $"Created: {BatchSummary.CreatedAt:g} by {BatchSummary.CreatedBy}";

            if (BatchSummary.PostedAt.HasValue)
            {
                message += $"\nPosted: {BatchSummary.PostedAt:g} by {BatchSummary.PostedBy}";
            }

            if (!string.IsNullOrWhiteSpace(BatchSummary.Notes))
            {
                message += $"\n\nNotes: {BatchSummary.Notes}";
            }

            await _dialogService.ShowMessageBoxAsync(message, "Batch Details");
        }

        private async Task VoidBatchAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                // Confirm
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will mark the batch as voided and prevent it from being used.\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n\n" +
                    $"Continue?",
                    $"Void Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                // Get reason
                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for voiding this batch:",
                    "Void Reason");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    await _dialogService.ShowMessageBoxAsync("Void reason is required.", "Required");
                    return;
                }

                IsLoading = true;

                // Void the batch
                var voidedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.VoidBatchAsync(
                    SelectedBatch.PaymentBatchId,
                    reason,
                    voidedBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Batch {SelectedBatch.BatchNumber} has been voided.",
                        "Batch Voided");

                    // Refresh list
                    await LoadBatchesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error voiding batch", ex);
                await _dialogService.ShowMessageBoxAsync($"Error voiding batch: {ex.Message}", "Error");
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
                    $"This will approve and post the batch for payment processing.\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n" +
                    $"Growers: {SelectedBatch.TotalGrowers}\n\n" +
                    $"Continue with approval?",
                    $"Approve Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                IsLoading = true;

                // Approve the batch (Draft → Posted)
                var approvedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.ApproveBatchAsync(
                    SelectedBatch.PaymentBatchId,
                    approvedBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Batch {SelectedBatch.BatchNumber} has been approved and posted.\n\n" +
                        $"Status: Posted (Ready for payment processing)",
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

        private async Task ProcessPaymentsAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                // Confirm processing
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will finalize the batch and process all payments.\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n" +
                    $"Growers: {SelectedBatch.TotalGrowers}\n\n" +
                    $"This action cannot be undone. Continue?",
                    $"Process Payments for Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                IsLoading = true;

                // Process payments (Posted → Finalized)
                var processedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.ProcessPaymentsAsync(
                    SelectedBatch.PaymentBatchId,
                    processedBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Payments for batch {SelectedBatch.BatchNumber} have been processed successfully.\n\n" +
                        $"Status: Finalized (Payments completed)",
                        "Payments Processed");

                    // Refresh list
                    await LoadBatchesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing payments", ex);
                await _dialogService.ShowMessageBoxAsync($"Error processing payments: {ex.Message}", "Error");
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

        private bool CanProcessPayments()
        {
            return SelectedBatch != null &&
                   SelectedBatch.Status == "Posted" &&
                   !SelectedBatch.IsDeleted;
        }

        private async Task RollbackBatchAsync()
        {
            if (SelectedBatch == null)
                return;

            try
            {
                // Confirm rollback
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will rollback and void the batch and all its allocations.\n\n" +
                    $"Batch: {SelectedBatch.BatchNumber}\n" +
                    $"Status: {SelectedBatch.Status}\n" +
                    $"Amount: ${SelectedBatch.TotalAmount:N2}\n" +
                    $"Growers: {SelectedBatch.TotalGrowers}\n\n" +
                    $"This action cannot be undone. Continue?",
                    $"Rollback Batch {SelectedBatch.BatchNumber}?");

                if (confirm != true)
                    return;

                // Get reason
                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for rolling back this batch:",
                    "Rollback Reason");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    await _dialogService.ShowMessageBoxAsync("Rollback reason is required.", "Required");
                    return;
                }

                IsLoading = true;

                // Rollback the batch
                var rolledBackBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _batchService.RollbackBatchAsync(
                    SelectedBatch.PaymentBatchId,
                    reason,
                    rolledBackBy);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Batch {SelectedBatch.BatchNumber} has been rolled back and voided.\n\n" +
                        $"All allocations have been voided.",
                        "Batch Rolled Back");

                    // Refresh list
                    await LoadBatchesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error rolling back batch", ex);
                await _dialogService.ShowMessageBoxAsync($"Error rolling back batch: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanRollbackBatch()
        {
            return SelectedBatch != null &&
                   (SelectedBatch.Status == "Draft" || SelectedBatch.Status == "Posted") &&
                   !SelectedBatch.IsDeleted;
        }

        private bool CanVoidBatch()
        {
            return SelectedBatch != null &&
                   (SelectedBatch.Status == "Draft" || SelectedBatch.Status == "Posted") &&
                   !SelectedBatch.IsDeleted;
        }
    }
}


