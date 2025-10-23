using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using WPFGrowerApp.Models;
using WPFGrowerApp.ViewModels.Dialogs;
using WPFGrowerApp.Views.Dialogs;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for reviewing printed cheques and approving them for delivery
    /// </summary>
    public class ChequeReviewViewModel : ViewModelBase, IDisposable
    {
        private readonly IChequeService _chequeService;
        private readonly WPFGrowerApp.Services.ChequePdfGenerator _pdfGenerator;
        private readonly IGrowerService _growerService;
        private readonly IDialogService _dialogService;
        private readonly IPaymentService _paymentService;
        private readonly IReceiptService _receiptService;

        // Properties
        private ObservableCollection<Cheque> _cheques;
        private Cheque? _selectedCheque;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private string _searchChequeNumber = string.Empty;
        private string _searchGrowerNumber = string.Empty;
        private string _searchStatus = "All";
        private string _searchChequeType = "All";
        private DateTime? _startDate;
        private DateTime? _endDate;
        
        // Debouncing timer for search
        private Timer? _searchDebounceTimer;

        public ObservableCollection<Cheque> Cheques
        {
            get => _cheques;
            set 
            { 
                SetProperty(ref _cheques, value);
                RefreshComputedProperties();
            }
        }

        public Cheque? SelectedCheque
        {
            get => _selectedCheque;
            set => SetProperty(ref _selectedCheque, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public decimal TotalAmount => _cheques?.Sum(c => c.ChequeAmount) ?? 0;

        public int SelectedCount => _cheques?.Count(c => c.IsSelected) ?? 0;

        public string SearchChequeNumber
        {
            get => _searchChequeNumber;
            set 
            {
                if (SetProperty(ref _searchChequeNumber, value))
                {
                    // Debounced auto-search when cheque number changes
                    DebouncedSearch();
                }
            }
        }

        public string SearchGrowerNumber
        {
            get => _searchGrowerNumber;
            set 
            {
                if (SetProperty(ref _searchGrowerNumber, value))
                {
                    // Debounced auto-search when grower number changes
                    DebouncedSearch();
                }
            }
        }

        public string SearchStatus
        {
            get => _searchStatus;
            set 
            {
                if (SetProperty(ref _searchStatus, value))
                {
                    // Immediate search when status changes (no debouncing needed)
                    _ = SearchChequesAsync();
                }
            }
        }

        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>
        {
            "All",
            "Printed",
            "Voided",
            "Stopped"
        };

        public string SearchChequeType
        {
            get => _searchChequeType;
            set 
            {
                if (SetProperty(ref _searchChequeType, value))
                {
                    // Immediate search when cheque type changes
                    _ = SearchChequesAsync();
                }
            }
        }

        public ObservableCollection<string> ChequeTypeOptions { get; } = new ObservableCollection<string>
        {
            "All",
            "Regular",
            "Advance"
        };

        public DateTime? StartDate
        {
            get => _startDate;
            set 
            {
                if (SetProperty(ref _startDate, value))
                {
                    // Immediate search when date changes (no debouncing needed)
                    _ = SearchChequesAsync();
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
                    // Immediate search when date changes (no debouncing needed)
                    _ = SearchChequesAsync();
                }
            }
        }

        // Commands
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ViewChequeCommand { get; }
        public ICommand VoidChequeCommand { get; }
        public ICommand EmergencyReprintCommand { get; }
        public ICommand ApproveForDeliveryCommand { get; }
        public ICommand ShowCalculationDetailsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToPaymentManagementCommand { get; }

        public ChequeReviewViewModel(
            IChequeService chequeService,
            WPFGrowerApp.Services.ChequePdfGenerator pdfGenerator,
            IGrowerService growerService,
            IDialogService dialogService,
            IPaymentService paymentService,
            IReceiptService receiptService)
        {
            _chequeService = chequeService ?? throw new ArgumentNullException(nameof(chequeService));
            _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));

            _cheques = new ObservableCollection<Cheque>();

            // Initialize commands
            SearchCommand = new RelayCommand(async o => await SearchChequesAsync());
            ClearSearchCommand = new RelayCommand(o => ClearSearch());
            ViewChequeCommand = new RelayCommand(async o => await ViewChequeDetailsAsync(), o => SelectedCheque != null);
            VoidChequeCommand = new RelayCommand(async o => await VoidChequeAsync(), o => CanVoidCheque());
            EmergencyReprintCommand = new RelayCommand(async o => await EmergencyReprintAsync(), o => SelectedCheque != null);
            ApproveForDeliveryCommand = new RelayCommand(async o => await ApproveForDeliveryAsync(), o => CanApproveForDelivery());
            ShowCalculationDetailsCommand = new RelayCommand(async o => await ShowCalculationDetailsAsync(), o => SelectedCheque != null);
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToPaymentManagementCommand = new RelayCommand(NavigateToPaymentManagementExecute);

            // Initialize with printed cheques
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
                StatusMessage = "Loading cheques for review...";

                // Load all cheques initially
                await SearchChequesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing ChequeReviewViewModel", ex);
                StatusMessage = $"Error loading cheques: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ==============================================================
        // DATA LOADING
        // ==============================================================

        private async Task LoadPrintedChequesAsync()
        {
            try
            {
                var cheques = await _chequeService.GetChequesByStatusAsync("Printed");
                Cheques.Clear();
                
                foreach (var cheque in cheques)
                {
                    Cheques.Add(cheque);
                }

                // Set up collection change notification for computed properties
                Cheques.CollectionChanged += (s, e) => RefreshComputedProperties();

                StatusMessage = $"Loaded {cheques.Count} printed cheques ready for review";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading printed cheques: {ex.Message}";
                Logger.Error("Error loading printed cheques", ex);
            }
        }

        // ==============================================================
        // SEARCH
        // ==============================================================

        /// <summary>
        /// Debounced search to prevent too many searches while user is typing
        /// </summary>
        private void DebouncedSearch()
        {
            // Cancel previous timer if it exists
            _searchDebounceTimer?.Dispose();
            
            // Create new timer with 500ms delay
            _searchDebounceTimer = new Timer(async _ => 
            {
                // Only search if we have some criteria or if fields are being cleared
                if (!string.IsNullOrWhiteSpace(SearchChequeNumber) || 
                    !string.IsNullOrWhiteSpace(SearchGrowerNumber) || 
                    SearchStatus != "All")
                {
                    await SearchChequesAsync();
                }
                else
                {
                    // If all fields are empty, show all cheques
                    await SearchChequesAsync();
                }
            }, null, 500, Timeout.Infinite);
        }

        private async Task SearchChequesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Searching cheques...";

                List<Cheque> cheques;

                // Get all cheques including advances first
                cheques = await _chequeService.GetAllChequesIncludingAdvancesAsync();

                // Apply filters
                cheques = ApplyAllFilters(cheques);

                UpdateChequesList(cheques);
                StatusMessage = $"Search returned {Cheques.Count} cheques";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching cheques: {ex.Message}";
                Logger.Error("Error searching cheques", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private List<Cheque> ApplyAllFilters(List<Cheque> cheques)
        {
            var filteredCheques = cheques.AsEnumerable();

            // Apply cheque type filter
            if (!string.IsNullOrWhiteSpace(SearchChequeType) && SearchChequeType != "All")
            {
                if (SearchChequeType == "Regular")
                {
                    filteredCheques = filteredCheques.Where(c => !c.IsAdvanceCheque);
                }
                else if (SearchChequeType == "Advance")
                {
                    filteredCheques = filteredCheques.Where(c => c.IsAdvanceCheque);
                }
            }

            // Apply cheque number filter
            if (!string.IsNullOrWhiteSpace(SearchChequeNumber))
            {
                filteredCheques = filteredCheques.Where(c => 
                    c.ChequeNumber.Contains(SearchChequeNumber, StringComparison.OrdinalIgnoreCase));
            }

            // Apply grower number filter
            if (!string.IsNullOrWhiteSpace(SearchGrowerNumber) && decimal.TryParse(SearchGrowerNumber, out decimal growerNumber))
            {
                filteredCheques = filteredCheques.Where(c => 
                    c.GrowerNumber == growerNumber.ToString() || 
                    c.GrowerId.ToString() == growerNumber.ToString());
            }

            // Apply date range filter if set
            if (StartDate.HasValue)
            {
                filteredCheques = filteredCheques.Where(c => c.ChequeDate.Date >= StartDate.Value.Date);
            }

            if (EndDate.HasValue)
            {
                filteredCheques = filteredCheques.Where(c => c.ChequeDate.Date <= EndDate.Value.Date);
            }

            // Apply status filter if set
            if (!string.IsNullOrWhiteSpace(SearchStatus) && SearchStatus != "All")
            {
                filteredCheques = filteredCheques.Where(c => c.Status == SearchStatus);
            }

            return filteredCheques.ToList();
        }

        private void UpdateChequesList(List<Cheque> cheques)
        {
            Cheques.Clear();
            foreach (var cheque in cheques)
            {
                Cheques.Add(cheque);
            }

            RefreshComputedProperties();
        }

        private void ClearSearch()
        {
            SearchChequeNumber = string.Empty;
            SearchGrowerNumber = string.Empty;
            SearchStatus = "All";
            SearchChequeType = "All";
            StartDate = null;
            EndDate = null;
            _ = SearchChequesAsync();
        }

        // ==============================================================
        // COMMANDS
        // ==============================================================

        private async Task RefreshAsync()
        {
            await LoadPrintedChequesAsync();
        }

        private async Task ViewChequeDetailsAsync()
        {
            if (SelectedCheque == null)
                return;

            var message = $"Cheque Details:\n\n" +
                         $"Cheque Number: {SelectedCheque.DisplayChequeNumber}\n" +
                         $"Grower: {SelectedCheque.GrowerName}\n" +
                         $"Amount: ${SelectedCheque.ChequeAmount:N2}\n" +
                         $"Date: {SelectedCheque.ChequeDate:yyyy-MM-dd}\n" +
                         $"Status: {SelectedCheque.Status}\n" +
                         $"Currency: {SelectedCheque.CurrencyCode}\n\n" +
                         $"Payee: {SelectedCheque.PayeeName}\n" +
                         $"Memo: {SelectedCheque.Memo}\n\n" +
                         $"Created: {SelectedCheque.CreatedAt:g} by {SelectedCheque.CreatedBy}";

            if (SelectedCheque.PrintedAt.HasValue)
            {
                message += $"\nPrinted: {SelectedCheque.PrintedAt:g} by {SelectedCheque.PrintedBy}";
            }

            if (SelectedCheque.IsVoided)
            {
                message += $"\n\nVOIDED: {SelectedCheque.VoidedDate:g}\n" +
                          $"Reason: {SelectedCheque.VoidedReason}\n" +
                          $"By: {SelectedCheque.VoidedBy}";
            }

            await _dialogService.ShowMessageBoxAsync(message, "Cheque Details");
        }

        private async Task ApproveForDeliveryAsync()
        {
            if (SelectedCheque == null)
                return;

            try
            {
                // Confirm approval
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"Approve cheque {SelectedCheque.DisplayChequeNumber} for delivery?\n\n" +
                    $"Grower: {SelectedCheque.GrowerName}\n" +
                    $"Amount: ${SelectedCheque.ChequeAmount:N2}\n\n" +
                    $"This will mark the cheque as ready for delivery.",
                    "Approve for Delivery");

                if (confirm != true)
                    return;

                IsLoading = true;
                StatusMessage = $"Approving cheque {SelectedCheque.DisplayChequeNumber} for delivery...";

                // Approve for delivery (updates status to "Delivered")
                var approvedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _chequeService.ApproveChequesForDeliveryAsync(
                    new List<int> { SelectedCheque.ChequeId }, 
                    approvedBy);

                if (success)
                {
                    StatusMessage = $"Cheque {SelectedCheque.DisplayChequeNumber} approved for delivery";
                    
                    // Refresh list to remove approved cheque
                    await LoadPrintedChequesAsync();
                }
                else
                {
                    StatusMessage = "Error approving cheque for delivery";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving cheque: {ex.Message}";
                Logger.Error("Error approving cheque for delivery", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task VoidChequeAsync()
        {
            if (SelectedCheque == null)
                return;

            try
            {
                // Confirm
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"Void cheque {SelectedCheque.DisplayChequeNumber}?\n\n" +
                    $"Grower: {SelectedCheque.GrowerName}\n" +
                    $"Amount: ${SelectedCheque.ChequeAmount:N2}\n\n" +
                    $"This action cannot be undone.",
                    "Void Cheque");

                if (confirm != true)
                    return;

                // Get reason
                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for voiding:",
                    "Void Reason");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    await _dialogService.ShowMessageBoxAsync("Void reason is required.", "Required");
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Voiding cheque {SelectedCheque.DisplayChequeNumber}...";

                // Void the cheque
                var voidedBy = App.CurrentUser?.Username ?? "SYSTEM";
                await _chequeService.VoidChequesAsync(
                    new List<int> { SelectedCheque.ChequeId }, 
                    reason, 
                    voidedBy);

                StatusMessage = $"Cheque {SelectedCheque.DisplayChequeNumber} voided successfully";
                
                // Refresh list
                await LoadPrintedChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error voiding cheque: {ex.Message}";
                Logger.Error("Error voiding cheque", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task EmergencyReprintAsync()
        {
            if (SelectedCheque == null)
                return;

            try
            {
                // Show confirmation with warning
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"EMERGENCY REPRINT\n\n" +
                    $"This is an emergency reprint of cheque {SelectedCheque.DisplayChequeNumber}.\n\n" +
                    $"Grower: {SelectedCheque.GrowerName}\n" +
                    $"Amount: ${SelectedCheque.ChequeAmount:N2}\n\n" +
                    $"⚠️ WARNING: This should only be used in emergency situations.\n" +
                    $"The original cheque may still be valid.\n\n" +
                    $"Continue with emergency reprint?",
                    "Emergency Reprint Confirmation");

                if (confirm != true)
                    return;

                // Get reason
                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for emergency reprint:",
                    "Emergency Reprint Reason");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    await _dialogService.ShowMessageBoxAsync("Emergency reprint reason is required.", "Required");
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Emergency reprinting cheque {SelectedCheque.DisplayChequeNumber}...";

                // Generate reprint PDF with watermark
                var pdfBytes = await _pdfGenerator.GenerateSingleChequeReprintPdfAsync(SelectedCheque, reason);
                
                // Save PDF to desktop
                var fileName = $"EMERGENCY_REPRINT_{SelectedCheque.ChequeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var filePath = System.IO.Path.Combine(desktopPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                // Log reprint activity
                await _chequeService.LogReprintActivityAsync(
                    new List<int> { SelectedCheque.ChequeId }, 
                    reason, 
                    App.CurrentUser?.Username ?? "SYSTEM");

                StatusMessage = $"Emergency reprint generated: {fileName}";
                
                // Open the file
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during emergency reprint: {ex.Message}";
                Logger.Error("Error during emergency reprint", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ShowCalculationDetailsAsync()
        {
            if (SelectedCheque == null)
                return;

            try
            {
                IsLoading = true;
                StatusMessage = "Loading cheque calculation details...";

                // Convert Cheque to ChequeItem for the calculation dialog
                var chequeItem = await ConvertChequeToChequeItemAsync(SelectedCheque);
                
                if (chequeItem != null)
                {
                    // Create the invoice-style calculation details dialog
                    var dialogViewModel = new ChequeCalculationDialogViewModel(chequeItem, _paymentService, _receiptService, _chequeService);
                    
                    // Load the data before showing the dialog
                    await dialogViewModel.LoadInvoiceStyleDetailsAsync();
                    
                    var dialogView = new InvoiceStyleChequeCalculationDialogView(dialogViewModel);

                    // Show the dialog
                    dialogView.Owner = Application.Current.MainWindow;
                    dialogView.ShowDialog();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync(
                        "Unable to load calculation details for this cheque. The cheque may not have detailed breakdown information available.",
                        "Calculation Details Unavailable");
                }

                StatusMessage = "Calculation details dialog closed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading calculation details: {ex.Message}";
                Logger.Error("Error loading calculation details", ex);
                await _dialogService.ShowMessageBoxAsync(
                    $"Error loading calculation details: {ex.Message}",
                    "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<ChequeItem?> ConvertChequeToChequeItemAsync(Cheque cheque)
        {
            try
            {
                // Create a basic ChequeItem from the Cheque
                var chequeItem = new ChequeItem
                {
                    ChequeId = cheque.ChequeId,
                    ChequeNumber = cheque.ChequeNumber,
                    GrowerId = cheque.GrowerId,
                    GrowerName = cheque.GrowerName ?? "Unknown",
                    GrowerNumber = cheque.GrowerNumber ?? cheque.GrowerId.ToString(),
                    Amount = cheque.ChequeAmount,
                    ChequeDate = cheque.ChequeDate,
                    Status = cheque.Status,
                    PaymentType = DeterminePaymentType(cheque),
                    IsAdvanceCheque = cheque.IsAdvanceCheque,
                    AdvanceChequeId = cheque.AdvanceChequeId,
                    PaymentBatchId = cheque.PaymentBatchId,
                    BatchNumber = cheque.BatchNumber ?? "N/A",
                    ConsolidatedFromBatches = cheque.ConsolidatedFromBatches ?? string.Empty
                };

                // Load additional details if available
                await LoadChequeCalculationDetailsAsync(chequeItem);

                return chequeItem;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error converting cheque to cheque item: {ex.Message}", ex);
                return null;
            }
        }

        private ChequePaymentType DeterminePaymentType(Cheque cheque)
        {
            // Determine payment type based on cheque properties
            if (cheque.IsAdvanceCheque)
                return ChequePaymentType.Advance;
            
            if (!string.IsNullOrEmpty(cheque.ConsolidatedFromBatches))
                return ChequePaymentType.Consolidated;
            
            return ChequePaymentType.Regular;
        }

        private async Task LoadChequeCalculationDetailsAsync(ChequeItem chequeItem)
        {
            try
            {
                // Load advance deductions if this is a regular or consolidated cheque
                if (chequeItem.PaymentType == ChequePaymentType.Regular || 
                    chequeItem.PaymentType == ChequePaymentType.Consolidated)
                {
                    var advanceDeductions = await _chequeService.GetAdvanceDeductionsByChequeNumberAsync(chequeItem.ChequeNumber);
                    chequeItem.AdvanceDeductions = advanceDeductions;
                }

                // Load batch breakdowns for consolidated cheques
                if (chequeItem.PaymentType == ChequePaymentType.Consolidated)
                {
                    // For consolidated cheques, we would need to parse the ConsolidatedFromBatches JSON
                    // and load individual batch details. For now, we'll create a basic breakdown.
                    chequeItem.BatchBreakdowns = new List<BatchBreakdown>
                    {
                        new BatchBreakdown
                        {
                            BatchId = chequeItem.PaymentBatchId ?? 0,
                            BatchNumber = chequeItem.BatchNumber,
                            Amount = chequeItem.Amount,
                            BatchDate = chequeItem.ChequeDate,
                            Status = "Processed"
                        }
                    };
                }

                // TotalDeductions and NetAmount are calculated automatically by the ChequeItem properties
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading cheque calculation details: {ex.Message}", ex);
            }
        }

        private void ShowHelpExecute(object? parameter)
        {
            System.Windows.MessageBox.Show(
                "Cheque Review Help:\n\n" +
                "1. Review printed cheques before delivery\n" +
                "2. Approve cheques for delivery (updates status to 'Delivered')\n" +
                "3. Void cheques if needed (with reprint option)\n" +
                "4. Emergency reprint only for critical situations\n" +
                "5. View calculation details to see how the cheque amount was calculated\n" +
                "6. Use F5 to refresh the list",
                "Cheque Review Help",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        // ==============================================================
        // HELPER METHODS
        // ==============================================================

        private bool CanVoidCheque()
        {
            return SelectedCheque != null && SelectedCheque.Status == "Printed";
        }

        private bool CanApproveForDelivery()
        {
            return SelectedCheque != null && SelectedCheque.Status == "Printed";
        }

        private void NavigateToDashboardExecute(object? parameter)
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
                Logger.Error("Error navigating to Dashboard", ex);
            }
        }

        private void NavigateToPaymentManagementExecute(object? parameter)
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
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

        private void RefreshComputedProperties()
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(SelectedCount));
        }

        // ==============================================================
        // DISPOSAL
        // ==============================================================

        public void Dispose()
        {
            _searchDebounceTimer?.Dispose();
        }
    }
}
