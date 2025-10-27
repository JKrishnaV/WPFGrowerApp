using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Dapper;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Views;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// Enhanced Payment Distribution ViewModel with consolidation features
    /// </summary>
    public class EnhancedPaymentDistributionViewModel : ViewModelBase
    {
        private readonly IPaymentDistributionService _paymentDistributionService;
        private readonly IPaymentBatchService _paymentBatchService;
        private readonly ICrossBatchPaymentService _crossBatchPaymentService;
        private readonly IUnifiedAdvanceService _unifiedAdvanceService;
        private readonly IReceiptService _receiptService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly string _connectionString;

        // Collections
        private ObservableCollection<PaymentBatch> _availableBatches;
        private ObservableCollection<GrowerPaymentSelection> _growerSelections;
        private ObservableCollection<PaymentDistribution> _distributions;
        private ObservableCollection<PaymentDistributionItem> _distributionItems;
        private ObservableCollection<string> _paymentMethods;
        private ObservableCollection<AdvanceDeductionItem> _advanceDeductions;
        private ObservableCollection<GrowerAdvanceInfo> _growerAdvanceInfos;

        // Selected items
        private PaymentBatch _selectedBatch;
        private List<int> _selectedBatchIds;
        private GrowerPaymentSelection _selectedGrowerSelection;

        // Form properties
        private string _searchText;
        private bool _isByGrower;
        private bool _isByBatch;
        private bool _isAllPending;
        private string _selectedPaymentMethod;
        private bool _isHybridMode;
        private bool _enableConsolidation;
        private ChequePaymentType _selectedPaymentType;

        // Advance deduction properties
        private bool _hasAdvanceDeductions;
        private bool _showAdvanceDeductions;
        private decimal _totalDeductionAmount;
        private decimal _totalRemainingPayment;
        private bool _enableDeductionModification;

        // Statistics
        private int _totalBatches;
        private int _totalGrowers;
        private decimal _totalAmount;
        private int _consolidationOpportunities;

        // Commands
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand GeneratePaymentsCommand { get; }
        public ICommand GenerateSelectedPaymentsCommand { get; }
        public ICommand PreviewDistributionCommand { get; }
        public ICommand SelectPaymentMethodCommand { get; }
        public ICommand SelectBatchesForConsolidationCommand { get; }
        // Note: PreviewConsolidatedPaymentCommand removed - consolidated payments replaced by payment distributions
        public ICommand PreviewRegularPaymentCommand { get; }
        public ICommand PreviewCommand { get; }
        public ICommand GenerateChequesCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        
        // Advance Deduction Commands
        public ICommand ShowAdvanceDeductionsCommand { get; }
        public ICommand ShowGrowerDeductionsCommand { get; }
        public ICommand ShowGrowerPaymentDetailsCommand { get; }
        public ICommand ModifyDeductionCommand { get; }
        public ICommand RefreshAdvancesCommand { get; }
        public ICommand ApplyDeductionsCommand { get; }
        public ICommand ClearDeductionsCommand { get; }
        
        // Navigation Commands
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToPaymentManagementCommand { get; }

        public EnhancedPaymentDistributionViewModel(
            IPaymentDistributionService paymentDistributionService,
            IPaymentBatchService paymentBatchService,
            ICrossBatchPaymentService crossBatchPaymentService,
            IUnifiedAdvanceService unifiedAdvanceService,
            IReceiptService receiptService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider)
        {
            _paymentDistributionService = paymentDistributionService;
            _paymentBatchService = paymentBatchService;
            _crossBatchPaymentService = crossBatchPaymentService;
            _unifiedAdvanceService = unifiedAdvanceService;
            _receiptService = receiptService;
            _dialogService = dialogService;
            _helpContentProvider = helpContentProvider;
            _connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            // Initialize collections
            AvailableBatches = new ObservableCollection<PaymentBatch>();
            GrowerSelections = new ObservableCollection<GrowerPaymentSelection>();
            Distributions = new ObservableCollection<PaymentDistribution>();
            DistributionItems = new ObservableCollection<PaymentDistributionItem>();
            PaymentMethods = new ObservableCollection<string> { "Cheque", "Electronic", "Both" };
            AdvanceDeductions = new ObservableCollection<AdvanceDeductionItem>();
            GrowerAdvanceInfos = new ObservableCollection<GrowerAdvanceInfo>();

            // Initialize form properties
            SearchText = string.Empty;
            IsByGrower = true;
            IsByBatch = false;
            IsAllPending = false;
            SelectedPaymentMethod = "Cheque";
            IsHybridMode = false;
            EnableConsolidation = true; // Always enabled, no UI control
            SelectedPaymentType = ChequePaymentType.Regular;
            SelectedBatchIds = new List<int>();

            // Initialize commands
            SearchCommand = new RelayCommand(async p => await SearchAsync());
            ClearFiltersCommand = new RelayCommand(async p => await ClearFiltersAsync());
            RefreshCommand = new RelayCommand(async p => await RefreshAsync());
            GeneratePaymentsCommand = new RelayCommand(async p => await GeneratePaymentsAsync(), p => CanGeneratePayments());
            GenerateSelectedPaymentsCommand = new RelayCommand(async p => await GenerateSelectedPaymentsAsync(), p => CanGenerateSelectedPayments());
            PreviewDistributionCommand = new RelayCommand(async p => await PreviewDistributionAsync(), p => CanPreviewDistribution());
            SelectPaymentMethodCommand = new RelayCommand(async p => await SelectPaymentMethodAsync());
            SelectBatchesForConsolidationCommand = new RelayCommand(async p => await SelectBatchesForConsolidationAsync());
            // Note: PreviewConsolidatedPaymentCommand removed - consolidated payments replaced by payment distributions
            PreviewRegularPaymentCommand = new RelayCommand(async p => await PreviewRegularPaymentAsync(), p => CanPreviewRegularPayment());
            PreviewCommand = new RelayCommand(async p => await PreviewPaymentAsync(), p => CanPreviewPayment());
            GenerateChequesCommand = new RelayCommand(async p => await GenerateChequesAsync(), p => CanGenerateCheques());
            ShowHelpCommand = new RelayCommand(async p => await ShowHelpAsync());
            ExportCommand = new RelayCommand(async p => await ExportAsync(), p => CanExport());
            ExportToExcelCommand = new RelayCommand(async p => await ExportAsync("Excel"), p => CanExport());
            ExportToPdfCommand = new RelayCommand(async p => await ExportAsync("PDF"), p => CanExport());
            
            // Initialize advance deduction commands
            ShowAdvanceDeductionsCommand = new RelayCommand(async p => await ShowAdvanceDeductionsAsync(), p => CanShowAdvanceDeductions());
            ShowGrowerDeductionsCommand = new RelayCommand(async p => await ShowGrowerDeductionsAsync(p), p => CanShowGrowerDeductions(p));
            ShowGrowerPaymentDetailsCommand = new RelayCommand(async p => await ShowGrowerPaymentDetailsAsync(p), p => CanShowGrowerPaymentDetails(p));
            ModifyDeductionCommand = new RelayCommand(async p => await ModifyDeductionAsync(p), p => CanModifyDeduction(p));
            RefreshAdvancesCommand = new RelayCommand(async p => await RefreshAdvancesAsync());
            ApplyDeductionsCommand = new RelayCommand(async p => await ApplyDeductionsAsync(), p => CanApplyDeductions());
            ClearDeductionsCommand = new RelayCommand(async p => await ClearDeductionsAsync());
            
            // Initialize navigation commands
            NavigateToDashboardCommand = new RelayCommand(p => NavigateToDashboard());
            NavigateToPaymentManagementCommand = new RelayCommand(p => NavigateToPaymentManagement());

            // Load initial data
            _ = LoadDataAsync();
        }

        /// <summary>
        /// Cleanup method to unsubscribe from events
        /// </summary>
        public void Cleanup()
        {
            if (AvailableBatches != null)
            {
                foreach (var batch in AvailableBatches)
                {
                    batch.PropertyChanged -= OnBatchPropertyChanged;
                }
            }
        }

        #region Properties

        public ObservableCollection<PaymentBatch> AvailableBatches
        {
            get => _availableBatches;
            set 
            {
                // Unsubscribe from old collection
                if (_availableBatches != null)
                {
                    foreach (var batch in _availableBatches)
                    {
                        batch.PropertyChanged -= OnBatchPropertyChanged;
                    }
                }

                SetProperty(ref _availableBatches, value);

                // Subscribe to new collection
                if (_availableBatches != null)
                {
                    foreach (var batch in _availableBatches)
                    {
                        batch.PropertyChanged += OnBatchPropertyChanged;
                    }
                }
            }
        }

        public ObservableCollection<GrowerPaymentSelection> GrowerSelections
        {
            get => _growerSelections;
            set 
            {
                // Unsubscribe from old collection if it exists
                if (_growerSelections != null)
                {
                    foreach (var grower in _growerSelections)
                    {
                        grower.PropertyChanged -= OnGrowerSelectionPropertyChanged;
                    }
                }
                
                SetProperty(ref _growerSelections, value);
                
                // Subscribe to new collection
                if (_growerSelections != null)
                {
                    foreach (var grower in _growerSelections)
                    {
                        grower.PropertyChanged += OnGrowerSelectionPropertyChanged;
                    }
                }
            }
        }

        public ObservableCollection<PaymentDistribution> Distributions
        {
            get => _distributions;
            set => SetProperty(ref _distributions, value);
        }

        public ObservableCollection<PaymentDistributionItem> DistributionItems
        {
            get => _distributionItems;
            set => SetProperty(ref _distributionItems, value);
        }

        public ObservableCollection<AdvanceDeductionItem> AdvanceDeductions
        {
            get => _advanceDeductions;
            set => SetProperty(ref _advanceDeductions, value);
        }

        public ObservableCollection<GrowerAdvanceInfo> GrowerAdvanceInfos
        {
            get => _growerAdvanceInfos;
            set => SetProperty(ref _growerAdvanceInfos, value);
        }

        public bool HasAdvanceDeductions
        {
            get => _hasAdvanceDeductions;
            set => SetProperty(ref _hasAdvanceDeductions, value);
        }

        public bool ShowAdvanceDeductions
        {
            get => _showAdvanceDeductions;
            set => SetProperty(ref _showAdvanceDeductions, value);
        }

        public decimal TotalDeductionAmount
        {
            get => _totalDeductionAmount;
            set => SetProperty(ref _totalDeductionAmount, value);
        }

        public decimal TotalRemainingPayment
        {
            get => _totalRemainingPayment;
            set => SetProperty(ref _totalRemainingPayment, value);
        }

        public bool EnableDeductionModification
        {
            get => _enableDeductionModification;
            set => SetProperty(ref _enableDeductionModification, value);
        }

        public ObservableCollection<string> PaymentMethods
        {
            get => _paymentMethods;
            set => SetProperty(ref _paymentMethods, value);
        }

        public PaymentBatch SelectedBatch
        {
            get => _selectedBatch;
            set => SetProperty(ref _selectedBatch, value);
        }

        public List<int> SelectedBatchIds
        {
            get => _selectedBatchIds;
            set 
            {
                if (SetProperty(ref _selectedBatchIds, value))
                {
                    OnPropertyChangedInternal();
                }
            }
        }

        public GrowerPaymentSelection SelectedGrowerSelection
        {
            get => _selectedGrowerSelection;
            set => SetProperty(ref _selectedGrowerSelection, value);
        }

        public string SearchText
        {
            get => _searchText;
            set 
            {
                if (SetProperty(ref _searchText, value))
                {
                    OnPropertyChangedInternal();
                }
            }
        }

        public bool IsByGrower
        {
            get => _isByGrower;
            set => SetProperty(ref _isByGrower, value);
        }

        public bool IsByBatch
        {
            get => _isByBatch;
            set => SetProperty(ref _isByBatch, value);
        }

        public bool IsAllPending
        {
            get => _isAllPending;
            set => SetProperty(ref _isAllPending, value);
        }

        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        public bool IsHybridMode
        {
            get => _isHybridMode;
            set => SetProperty(ref _isHybridMode, value);
        }

        public bool EnableConsolidation
        {
            get => _enableConsolidation;
            set => SetProperty(ref _enableConsolidation, value);
        }

        private bool _selectAllBatches = false;

        public bool SelectAllBatches
        {
            get => _selectAllBatches;
            set
            {
                if (SetProperty(ref _selectAllBatches, value))
                {
                    // Temporarily unsubscribe from property change events to prevent multiple calls
                    foreach (var batch in AvailableBatches)
                    {
                        batch.PropertyChanged -= OnBatchPropertyChanged;
                    }
                    
                    // Set all batch selections
                    foreach (var batch in AvailableBatches)
                    {
                        batch.IsSelected = value;
                    }
                    
                    // Re-subscribe to property change events
                    foreach (var batch in AvailableBatches)
                    {
                        batch.PropertyChanged += OnBatchPropertyChanged;
                    }
                    
                    // Manually trigger the update once all batches are set
                    _ = UpdateSelectedBatchIdsAsync();
                }
            }
        }

        private bool _selectAllGrowers = true; // Default all checked

        public bool SelectAllGrowers
        {
            get => _selectAllGrowers;
            set
            {
                if (SetProperty(ref _selectAllGrowers, value))
                {
                    foreach (var grower in GrowerSelections)
                    {
                        grower.IsSelectedForPayment = value;
                    }
                }
            }
        }

        public ChequePaymentType SelectedPaymentType
        {
            get => _selectedPaymentType;
            set => SetProperty(ref _selectedPaymentType, value);
        }

        public int TotalBatches
        {
            get => _totalBatches;
            set => SetProperty(ref _totalBatches, value);
        }

        public int TotalGrowers
        {
            get => _totalGrowers;
            set => SetProperty(ref _totalGrowers, value);
        }

        public decimal TotalAmount
        {
            get 
            {
                Infrastructure.Logging.Logger.Info($"TotalAmount property getter called: returning {_totalAmount:C}");
                return _totalAmount;
            }
            set 
            {
                Infrastructure.Logging.Logger.Info($"TotalAmount property setter called: {_totalAmount:C} -> {value:C}");
                SetProperty(ref _totalAmount, value);
            }
        }

        public int ConsolidationOpportunities
        {
            get => _consolidationOpportunities;
            set => SetProperty(ref _consolidationOpportunities, value);
        }

        // Computed properties
        public string TotalAmountDisplay => TotalAmount.ToString("C");
        public string ConsolidationOpportunitiesDisplay => $"{ConsolidationOpportunities} {(ConsolidationOpportunities != 1 ? "opportunities" : "opportunity")}";
        public bool HasSelectedBatches => SelectedBatchIds.Any();
        public bool HasGrowerSelections => GrowerSelections.Any();

        #endregion

        #region Command Methods

        private async Task SearchAsync()
        {
            try
            {
                IsBusy = true;
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error searching: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ClearFiltersAsync()
        {
            try
            {
                IsBusy = true;
                
                SearchText = string.Empty;
                IsByGrower = true;
                IsByBatch = false;
                IsAllPending = false;
                SelectedPaymentMethod = "Cheque";
                EnableConsolidation = false;
                SelectedBatchIds.Clear();
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error clearing filters: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error refreshing data: {ex.Message}", "Error");
            }
        }

        private async Task GeneratePaymentsAsync()
        {
            try
            {
                IsBusy = true;
                
                // Check if we have grower selections
                if (!GrowerSelections.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select batches first to load grower data", "No Data Available");
                    return;
                }

                // Note: Consolidated payment generation removed - consolidated payments replaced by payment distributions

                // Check for existing distributions to prevent duplicates
                foreach (var batchId in SelectedBatchIds)
                {
                    var hasExisting = await _paymentDistributionService.HasExistingDistributionsAsync(batchId);
                    if (hasExisting)
                    {
                        await _dialogService.ShowMessageBoxAsync(
                            $"Batch {batchId} already has payment distributions. Please select a different batch.", 
                            "Duplicate Distribution");
                        return;
                    }
                }

                // Create distribution items from grower selections with proper audit trail
                var items = await CreateDistributionItemsFromSelectionsAsync();
                
                // Create payment distribution
                var distribution = new PaymentDistribution
                {
                    DistributionType = GetDistributionType(),
                    PaymentMethod = SelectedPaymentMethod,
                    TotalAmount = items.Sum(i => i.Amount),
                    TotalGrowers = items.Select(i => i.GrowerId).Distinct().Count(),
                    TotalBatches = SelectedBatchIds.Count,
                    Items = items
                };

                // Generate complete payment distribution in single transaction
                var createdDistribution = await _paymentDistributionService.GenerateCompletePaymentDistributionAsync(distribution, App.CurrentUser?.Username ?? "SYSTEM", SelectedBatchIds);
                
                await _dialogService.ShowMessageBoxAsync(
                    $"Successfully generated payments for distribution {createdDistribution.DistributionNumber}!\n\n" +
                    $"Distribution ID: {createdDistribution.DistributionId}\n" +
                    $"Total Amount: {createdDistribution.TotalAmount:C}\n" +
                    $"Growers: {createdDistribution.TotalGrowers}\n" +
                    $"Batches: {createdDistribution.TotalBatches}\n\n" +
                    $"You can now go to Enhanced Cheque Preparation to print the cheques.",
                    "Payments Generated Successfully");
                
                // Refresh data to show updated status
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error generating payments: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PreviewDistributionAsync()
        {
            try
            {
                IsBusy = true;
                
                // Check if we have grower selections
                if (!GrowerSelections.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select batches first to load grower data", "No Data Available");
                    return;
                }

                // Create distribution items from grower selections
                var items = await CreateDistributionItemsFromSelectionsAsync();
                
                // Clear and populate distribution items
                DistributionItems.Clear();
                foreach (var item in items)
                {
                    DistributionItems.Add(item);
                }
                
                // Trigger property changed notification for the collection
                OnPropertyChanged(nameof(DistributionItems));

                var totalAmount = DistributionItems.Sum(i => i.Amount);
                var itemCount = DistributionItems.Count;
                
                await _dialogService.ShowMessageBoxAsync(
                    $"Preview Generated Successfully!\n\n" +
                    $"Items: {itemCount}\n" +
                    $"Total Amount: {totalAmount:C}\n" +
                    $"Growers: {GrowerSelections.Count}\n" +
                    $"Selected Batches: {SelectedBatchIds.Count}",
                    "Distribution Preview");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error previewing distribution: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SelectPaymentMethodAsync()
        {
            try
            {
                // Implementation for selecting payment method
                await _dialogService.ShowMessageBoxAsync("Payment method selection functionality will be implemented in a future update", "Select Payment Method");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error selecting payment method: {ex.Message}", "Error");
            }
        }

        private async Task SelectBatchesForConsolidationAsync()
        {
            try
            {
                // Implementation for selecting batches for consolidation
                await _dialogService.ShowMessageBoxAsync("Batch selection for consolidation functionality will be implemented in a future update", "Select Batches");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error selecting batches: {ex.Message}", "Error");
            }
        }

        // Note: PreviewConsolidatedPaymentAsync method removed - consolidated payments replaced by payment distributions

        private async Task PreviewRegularPaymentAsync()
        {
            try
            {
                if (SelectedGrowerSelection == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a grower for preview", "Validation Error");
                    return;
                }

                // Get outstanding advances for detailed breakdown
                var outstandingAdvances = await _unifiedAdvanceService.GetOutstandingAdvancesAsync(SelectedGrowerSelection.GrowerId);
                
                // Create advance breakdowns
                var advanceBreakdowns = outstandingAdvances.Select(adv => new AdvanceBreakdown
                {
                    AdvanceChequeId = adv.AdvanceChequeId,
                    ChequeNumber = adv.ChequeNumber?.ToString() ?? $"ADV-{adv.AdvanceChequeId:D6}",
                    Amount = adv.AdvanceAmount,
                    AdvanceDate = adv.AdvanceDate,
                    Status = adv.Status
                }).ToList();

                // Use the new deduction logic from the grower selection
                var grossAmount = SelectedGrowerSelection.ConsolidatedAmount;
                var deductFromThisTransaction = SelectedGrowerSelection.DeductFromThisTransaction;
                var netPaymentAmount = SelectedGrowerSelection.NetPaymentAmount;
                var remainingDeductions = SelectedGrowerSelection.RemainingDeductions;

                var message = $"Regular Payment Preview for {SelectedGrowerSelection.GrowerName}:\n\n";
                
                // Show all selected batches with their individual amounts
                var selectedBatches = AvailableBatches.Where(b => b.IsSelected).ToList();
                if (selectedBatches.Any())
                {
                    // Get actual amounts for each batch
                    var batchAmounts = await GetBatchAmountsForGrowerAsync(SelectedGrowerSelection.GrowerId, selectedBatches.Select(b => b.PaymentBatchId).ToList());
                    
                    foreach (var batch in selectedBatches)
                    {
                        var batchAmount = batchAmounts.ContainsKey(batch.PaymentBatchId) ? batchAmounts[batch.PaymentBatchId] : 0;
                        message += $"Batch: {batch.BatchNumber} gross: {batchAmount:C}\n";
                    }
                }
                else
                {
                    message += $"Gross Amount: {grossAmount:C}\n";
                }
                
                // Outstanding advances information
                message += $"\nOutstanding Advances Count: {advanceBreakdowns.Count}\n";
                foreach (var advance in advanceBreakdowns)
                {
                    message += $"Outstanding Advance ({advance.ChequeNumber}): {advance.Amount:C}\n";
                }
                
                // New deduction logic - show current transaction details
                message += $"\nDeduction Details:\n";
                message += $"Total Outstanding Advances: {SelectedGrowerSelection.OutstandingAdvances:C}\n";
                message += $"Deduct From This Transaction: {deductFromThisTransaction:C}\n";
                message += $"Remaining Deductions: {remainingDeductions:C}\n";
                
                // Net payment amount (what the grower will actually receive)
                message += $"\nNet Payment Amount: {netPaymentAmount:C}";
                
                // Add informational note about remaining deductions
                if (remainingDeductions > 0)
                {
                    message += $"\n\nNote: ${remainingDeductions:C} in outstanding advances will be carried forward to future payments.";
                }

                await _dialogService.ShowMessageBoxAsync(message, "Regular Payment Preview");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error previewing regular payment: {ex.Message}", "Error");
            }
        }

        private async Task PreviewPaymentAsync()
        {
            try
            {
                if (SelectedGrowerSelection == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a grower for preview", "Validation Error");
                    return;
                }

                // Note: Consolidated payment preview removed - consolidated payments replaced by payment distributions
                await PreviewRegularPaymentAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error previewing payment: {ex.Message}", "Error");
            }
        }

        private async Task<Dictionary<int, decimal>> GetBatchAmountsForGrowerAsync(int growerId, List<int> batchIds)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var batchAmounts = new Dictionary<int, decimal>();

                foreach (var batchId in batchIds)
                {
                    var query = @"
                        SELECT ISNULL(SUM(rpa.AmountPaid), 0) as TotalAmount
                        FROM ReceiptPaymentAllocations rpa
                        INNER JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                        WHERE r.GrowerId = @GrowerId 
                        AND rpa.PaymentBatchId = @BatchId
                        AND r.IsVoided = 0";

                    using var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@GrowerId", growerId);
                    command.Parameters.AddWithValue("@BatchId", batchId);

                    var result = await command.ExecuteScalarAsync();
                    var amount = Convert.ToDecimal(result);
                    batchAmounts[batchId] = amount;
                }

                return batchAmounts;
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error getting batch amounts for grower {growerId}: {ex.Message}", ex);
                return new Dictionary<int, decimal>();
            }
        }

        private async Task GenerateChequesAsync()
        {
            try
            {
                // Implementation for generating cheques
                await _dialogService.ShowMessageBoxAsync("Cheque generation functionality will be implemented in a future update", "Generate Cheques");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error generating cheques: {ex.Message}", "Error");
            }
        }

        private async Task ShowHelpAsync()
        {
            try
            {
                var helpContent = _helpContentProvider.GetHelpContent("EnhancedPaymentDistributionView");
                await _dialogService.ShowMessageBoxAsync(helpContent.Content, "Payment Distribution Help");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error showing help: {ex.Message}", "Error");
            }
        }

        private async Task ExportAsync(string format = null)
        {
            try
            {
                if (!GrowerSelections.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("No payment data available to export. Please select batches first.", "No Data Available");
                    return;
                }

                string selectedFormat = format;

                // If no format specified, show dialog
                if (string.IsNullOrEmpty(selectedFormat))
                {
                    var exportDialog = new Views.Dialogs.ExportFormatDialog();
                    exportDialog.Owner = System.Windows.Application.Current.MainWindow;
                    
                    if (exportDialog.ShowDialog() != true)
                    {
                        return; // User cancelled
                    }

                    selectedFormat = exportDialog.SelectedFormat;
                }

                // Get selected batches for export
                var selectedBatches = AvailableBatches.Where(b => b.IsSelected).ToList();
                if (!selectedBatches.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("No batches selected. Please select at least one batch.", "No Batches Selected");
                    return;
                }

                // Show progress
                IsBusy = true;
                await _dialogService.ShowMessageBoxAsync($"Exporting payment distribution data to {selectedFormat} format...", "Exporting");

                // Create export service and export data
                var exportService = new Services.PaymentDistributionExportService();
                var growerSelectionsList = GrowerSelections.ToList();
                bool success = await exportService.ExportPaymentDistributionAsync(
                    growerSelectionsList,
                    selectedBatches,
                    selectedFormat,
                    "Enhanced Payment Distribution Report"
                );

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync($"Payment distribution data successfully exported to {selectedFormat} format!", "Export Complete");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error exporting data: {ex.Message}", "Export Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Command CanExecute Methods

        private bool CanGeneratePayments()
        {
            return HasGrowerSelections && !IsBusy;
        }

        private bool CanPreviewDistribution()
        {
            return HasGrowerSelections && !IsBusy;
        }

        // Note: CanPreviewConsolidatedPayment method removed - consolidated payments replaced by payment distributions

        private bool CanPreviewRegularPayment()
        {
            return SelectedGrowerSelection != null && !IsBusy;
        }

        private bool CanPreviewPayment()
        {
            return SelectedGrowerSelection != null && !IsBusy;
        }

        private bool CanGenerateCheques()
        {
            return HasGrowerSelections && !IsBusy;
        }

        private bool CanExport()
        {
            return HasGrowerSelections && !IsBusy;
        }

        #endregion

        #region Private Methods

        private async Task LoadDataAsync()
        {
            try
            {
                IsBusy = true;

                // Load available batches using the correct service method
                var batches = await _paymentDistributionService.GetAvailableBatchesAsync();
                AvailableBatches.Clear();
                foreach (var batch in batches)
                {
                    // Subscribe to property changes for each batch
                    batch.PropertyChanged += OnBatchPropertyChanged;
                    AvailableBatches.Add(batch);
                }

                // Load grower selections
                await LoadGrowerSelectionsAsync();

                // Calculate statistics
                await CalculateStatisticsAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error loading data: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool _isLoadingGrowerSelections = false;

        private async Task LoadGrowerSelectionsAsync()
        {
            if (_isLoadingGrowerSelections)
            {
                Infrastructure.Logging.Logger.Info("LoadGrowerSelectionsAsync already in progress, skipping duplicate call");
                return;
            }

            try
            {
                _isLoadingGrowerSelections = true;
                Infrastructure.Logging.Logger.Info($"LoadGrowerSelectionsAsync called - clearing {GrowerSelections.Count} existing selections");
                
                // Clear existing selections
                GrowerSelections.Clear();
                
                // Reset statistics to zero
                TotalGrowers = 0;
                TotalAmount = 0;
                ConsolidationOpportunities = 0;

                if (SelectedBatchIds.Any())
                {
                    Infrastructure.Logging.Logger.Info($"Loading grower selections for batch IDs: {string.Join(", ", SelectedBatchIds)}");
                    
                    // Get growers in selected batches
                    var growersInBatches = await _crossBatchPaymentService.GetGrowersInMultipleBatchesAsync(SelectedBatchIds);
                    
                    Infrastructure.Logging.Logger.Info($"Found {growersInBatches.Count} growers in batches");
                    
                    // Filter growers based on their cheque status
                    var filteredGrowers = await FilterGrowersByChequeStatusAsync(growersInBatches, SelectedBatchIds);
                    
                    Infrastructure.Logging.Logger.Info($"After filtering by cheque status: {filteredGrowers.Count} growers available for processing");
                    
                    foreach (var grower in filteredGrowers)
                    {
                        Infrastructure.Logging.Logger.Info($"Processing grower: {grower.GrowerName} (ID: {grower.GrowerId})");
                        
                        var selection = new GrowerPaymentSelection(grower.GrowerId, grower.GrowerName, grower.GrowerNumber);
                        
                        // Get payment details for this grower across batches
                        var payments = await _crossBatchPaymentService.GetGrowerPaymentsAcrossBatchesAsync(grower.GrowerId, SelectedBatchIds);
                        
                        Infrastructure.Logging.Logger.Info($"Found {payments.Count} payments for grower {grower.GrowerName}, total amount: {payments.Sum(p => p.Amount)}");
                        
                        // Get outstanding advances for this grower
                        var outstandingAdvances = await _unifiedAdvanceService.CalculateTotalOutstandingAdvancesAsync(grower.GrowerId);
                        var hasOutstandingAdvances = await _unifiedAdvanceService.HasOutstandingAdvancesAsync(grower.GrowerId);
                        
                        Infrastructure.Logging.Logger.Info($"Grower {grower.GrowerName} has outstanding advances: {outstandingAdvances:C} (HasAdvances: {hasOutstandingAdvances})");
                        
                        selection.RegularAmount = payments.Sum(p => p.Amount);
                        selection.ConsolidatedAmount = selection.RegularAmount;
                        selection.OutstandingAdvances = outstandingAdvances;
                        selection.HasOutstandingAdvances = hasOutstandingAdvances;
                        selection.CanBeConsolidated = grower.BatchCount > 1;
                        selection.SetRecommendedPaymentType();
                        selection.IsSelectedForPayment = true; // Default checked
                        
                        // Subscribe to property changes
                        selection.PropertyChanged += OnGrowerSelectionPropertyChanged;
                        
                        GrowerSelections.Add(selection);
                        Infrastructure.Logging.Logger.Info($"Added grower selection: {selection.GrowerName} with amount {selection.RegularAmount:C}");
                    }
                    
                    Infrastructure.Logging.Logger.Info($"Total grower selections loaded: {GrowerSelections.Count}");
                    
                    // Load outstanding advances for selected growers
                    await LoadOutstandingAdvancesAsync();
                    
                    // Update statistics after loading grower selections
                    await CalculateStatisticsAsync();
                }
                else
                {
                    Infrastructure.Logging.Logger.Info("No batch IDs selected");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't show dialog to prevent DialogHost conflicts
                Infrastructure.Logging.Logger.Error($"Error loading grower selections: {ex.Message}", ex);
            }
            finally
            {
                _isLoadingGrowerSelections = false;
            }
        }

        private async Task CalculateStatisticsAsync()
        {
            try
            {
                var newTotalBatches = AvailableBatches.Count;
                var newTotalGrowers = GrowerSelections.Count;
                var newTotalAmount = GrowerSelections.Sum(g => g.NetPaymentAmount); // Use NetPaymentAmount instead of RegularAmount
                var newConsolidationOpportunities = GrowerSelections.Count(g => g.CanBeConsolidated);
                
                TotalBatches = newTotalBatches;
                TotalGrowers = newTotalGrowers;
                TotalAmount = newTotalAmount;
                ConsolidationOpportunities = newConsolidationOpportunities;
                
                Infrastructure.Logging.Logger.Info($"CalculateStatisticsAsync: TotalBatches={TotalBatches}, TotalGrowers={TotalGrowers}, TotalAmount={TotalAmount:C}, ConsolidationOpportunities={ConsolidationOpportunities}");
                
                // Force property change notifications
                OnPropertyChangedInternal(nameof(TotalBatches));
                OnPropertyChangedInternal(nameof(TotalGrowers));
                OnPropertyChangedInternal(nameof(TotalAmount));
                OnPropertyChangedInternal(nameof(TotalAmountDisplay));
                OnPropertyChangedInternal(nameof(ConsolidationOpportunities));
                
                Infrastructure.Logging.Logger.Info($"Property change notifications fired for TotalAmount={TotalAmount:C} and TotalAmountDisplay={TotalAmountDisplay}");
                
                // Force UI refresh with a small delay
                await Task.Delay(100);
                OnPropertyChangedInternal(nameof(TotalAmount));
                OnPropertyChangedInternal(nameof(TotalAmountDisplay));
                Infrastructure.Logging.Logger.Info($"Delayed property change notifications fired for TotalAmount={TotalAmount:C} and TotalAmountDisplay={TotalAmountDisplay}");
            }
            catch (Exception ex)
            {
                // Log error but don't show to user
                Infrastructure.Logging.Logger.Error($"Error calculating statistics: {ex.Message}", ex);
            }
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    var filtered = GrowerSelections.Where(g => 
                        g.GrowerName.ToLower().Contains(searchLower) ||
                        g.GrowerNumber.ToLower().Contains(searchLower));
                    
                    // Update filtered collection
                    GrowerSelections.Clear();
                    foreach (var grower in filtered)
                    {
                        GrowerSelections.Add(grower);
                    }
                }
                
                // Add a small delay to make this properly async
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error applying filters: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Handle property changes on individual batches (specifically IsSelected)
        /// </summary>
        private async void OnBatchPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PaymentBatch.IsSelected) && sender is PaymentBatch batch)
            {
                await UpdateSelectedBatchIdsAsync();
            }
        }

        /// <summary>
        /// Update the SelectedBatchIds collection based on currently selected batches
        /// </summary>
        private async Task UpdateSelectedBatchIdsAsync()
        {
            try
            {
                IsBusy = true;
                
                var selectedIds = AvailableBatches
                    .Where(b => b.IsSelected)
                    .Select(b => b.PaymentBatchId)
                    .ToList();

                Infrastructure.Logging.Logger.Info($"UpdateSelectedBatchIdsAsync: Found {selectedIds.Count} selected batch IDs: {string.Join(", ", selectedIds)}");
                
                SelectedBatchIds = selectedIds;

                // Update select all checkbox state
                UpdateSelectAllBatchesState();

                // Trigger property change notifications
                OnPropertyChangedInternal(nameof(HasSelectedBatches));

                // Reload grower selections when batch selection changes
                await LoadGrowerSelectionsAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't show dialog to prevent DialogHost conflicts
                Infrastructure.Logging.Logger.Error($"Error updating batch selection: {ex.Message}", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Create distribution items from the current grower selections
        /// Uses primary receipt ID for audit trail while maintaining single cheque per grower
        /// </summary>
        private async Task<List<PaymentDistributionItem>> CreateDistributionItemsFromSelectionsAsync()
        {
            var items = new List<PaymentDistributionItem>();

            try
            {
                foreach (var growerSelection in GrowerSelections)
                {
                    // Get the primary receipt ID for this grower across selected batches
                    var primaryReceiptId = await GetPrimaryReceiptIdAsync(growerSelection.GrowerId);
                    
                    // Validate that we have valid batch IDs
                    if (!SelectedBatchIds.Any() || SelectedBatchIds.All(id => id <= 0))
                    {
                        throw new InvalidOperationException("No valid batches selected. Please select at least one batch before generating payments.");
                    }

                    // For consolidation, create one distribution item per grower with consolidated amount
                    // but assign it to the first batch for database constraint compliance
                    // Calculate actual cheque amount: if net amount is negative or zero, cheque amount is 0
                    var actualChequeAmount = Math.Max(0, growerSelection.ActualNetAmount);
                    
                    var distributionItem = new PaymentDistributionItem
                    {
                        GrowerId = growerSelection.GrowerId,
                        GrowerName = growerSelection.GrowerName,
                        GrowerNumber = growerSelection.GrowerNumber,
                        Amount = actualChequeAmount, // Use actual cheque amount (0 if net amount is negative)
                        PaymentMethod = SelectedPaymentMethod,
                        Status = "Draft",
                        CreatedAt = DateTime.Now,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM",
                        BatchNumber = string.Join(", ", SelectedBatchIds), // Store all batch numbers for audit trail
                        PaymentBatchId = SelectedBatchIds.First(id => id > 0), // Use first batch for database constraint
                        ReceiptId = primaryReceiptId // Use primary receipt ID for audit trail
                    };

                    // Note: Outstanding advances are automatically deducted in NetConsolidatedAmount
                    // The PaymentDistributionItem model uses the actual cheque amount (0 if net amount is negative)
                    // Full audit trail is maintained through the ConsolidatedCheque system for cross-batch tracking

                    items.Add(distributionItem);
                }

                Infrastructure.Logging.Logger.Info($"Created {items.Count} distribution items from {GrowerSelections.Count} grower selections with proper audit trail");
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error creating distribution items: {ex.Message}", ex);
                throw;
            }

            return items;
        }

        /// <summary>
        /// Get the distribution type based on current settings
        /// </summary>
        private string GetDistributionType()
        {
            if (IsByGrower) return "ByGrower";
            if (IsByBatch) return "ByBatch";
            if (IsAllPending) return "AllPending";
            return "ByGrower"; // Default to ByGrower for Enhanced Payment Distribution
        }

        /// <summary>
        /// Get the primary receipt ID for a grower across selected batches
        /// This provides audit trail while maintaining single cheque per grower
        /// </summary>
        private async Task<int> GetPrimaryReceiptIdAsync(int growerId)
        {
            try
            {
                // Get the most recent receipt for this grower across selected batches
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var batchIdsParam = string.Join(",", SelectedBatchIds.Select((id, index) => $"@BatchId{index}"));
                    var sql = $@"
                        SELECT TOP 1 r.ReceiptId
                        FROM Receipts r
                        INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        WHERE r.GrowerId = @GrowerId
                        AND rpa.PaymentBatchId IN ({batchIdsParam})
                        ORDER BY r.ReceiptDate DESC, r.ReceiptId DESC";
                    
                    using var command = new SqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@GrowerId", growerId);
                    
                    // Add parameters for each batch ID
                    for (int i = 0; i < SelectedBatchIds.Count; i++)
                    {
                        command.Parameters.AddWithValue($"@BatchId{i}", SelectedBatchIds[i]);
                    }
                    
                    var result = await command.ExecuteScalarAsync();
                    return result != null ? (int)result : 0; // Return 0 if no receipt found
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error getting primary receipt ID for grower {growerId}: {ex.Message}", ex);
                return 0; // Return 0 as fallback
            }
        }

        #endregion

        #region Property Change Handlers

        private void OnPropertyChangedInternal([CallerMemberName] string? propertyName = null)
        {
            try
            {
                // Call the base OnPropertyChanged to notify the UI
                OnPropertyChanged(propertyName);
                
                // Update command states when relevant properties change
                if (propertyName == nameof(SelectedGrowerSelection) || 
                    propertyName == nameof(SelectedBatchIds) ||
                    propertyName == nameof(IsBusy))
                {
                    // Note: PreviewConsolidatedPaymentCommand removed - consolidated payments replaced by payment distributions
                    (PreviewRegularPaymentCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PreviewCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }

                if (propertyName == nameof(GrowerSelections) || 
                    propertyName == nameof(IsBusy))
                {
                    (GeneratePaymentsCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PreviewDistributionCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (GenerateChequesCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (ExportCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }

                // Auto-apply filters when search criteria change
                if (propertyName == nameof(SearchText))
                {
                    _ = ApplyFiltersAsync();
                }

                // Load grower selections when batch selection changes
                if (propertyName == nameof(SelectedBatchIds))
                {
                    _ = LoadGrowerSelectionsAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                Infrastructure.Logging.Logger.Error($"Error in OnPropertyChangedInternal: {ex.Message}", ex);
            }
        }

        #endregion

        #region Navigation Methods

        private void NavigateToDashboard()
        {
            try
            {
                // Use the NavigationHelper for navigation
                NavigationHelper.NavigateToDashboard();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBoxAsync($"Error navigating to Dashboard: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToPaymentManagement()
        {
            try
            {
                // Use the NavigationHelper for navigation
                NavigationHelper.NavigateToPaymentManagement();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBoxAsync($"Error navigating to Payment Management: {ex.Message}", "Navigation Error");
            }
        }

        /// <summary>
        /// Filter growers based on their cheque status to show only those available for processing
        /// </summary>
        private async Task<List<GrowerBatchDetails>> FilterGrowersByChequeStatusAsync(List<GrowerBatchDetails> growers, List<int> batchIds)
        {
            var filteredGrowers = new List<GrowerBatchDetails>();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    foreach (var grower in growers)
                    {
                        // Check if this grower has any cheques in the selected batches
                        var chequeStatusSql = @"
                            SELECT c.ChequeId, c.Status, c.ChequeNumber
                            FROM Cheques c
                            WHERE c.GrowerId = @GrowerId 
                            AND c.PaymentBatchId IN ({0})";
                        
                        // Also check for pending payment distribution items
                        var pendingItemsSql = @"
                            SELECT COUNT(*) as PendingCount
                            FROM PaymentDistributionItems pdi
                            WHERE pdi.GrowerId = @GrowerId 
                            AND pdi.PaymentBatchId IN ({0})
                            AND pdi.Status = 'Pending'";
                        
                        var batchIdsParam = string.Join(",", batchIds.Select((id, index) => $"@BatchId{index}"));
                        var sql = string.Format(chequeStatusSql, batchIdsParam);
                        
                        using var command = new SqlCommand(sql, connection);
                        command.Parameters.AddWithValue("@GrowerId", grower.GrowerId);
                        
                        // Add parameters for each batch ID
                        for (int i = 0; i < batchIds.Count; i++)
                        {
                            command.Parameters.AddWithValue($"@BatchId{i}", batchIds[i]);
                        }
                        
                        var hasCheques = false;
                        var hasVoidedCheques = false;
                        var hasPrintedCheques = false;
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                hasCheques = true;
                                var status = reader["Status"].ToString();
                                
                                if (status == "Voided")
                                {
                                    hasVoidedCheques = true;
                                }
                                else if (status == "Printed" || status == "Generated")
                                {
                                    hasPrintedCheques = true;
                                }
                            }
                        }
                        
                        // Check for pending payment distribution items
                        var pendingSql = string.Format(pendingItemsSql, batchIdsParam);
                        using var pendingCommand = new SqlCommand(pendingSql, connection);
                        pendingCommand.Parameters.AddWithValue("@GrowerId", grower.GrowerId);
                        
                        // Add parameters for each batch ID
                        for (int i = 0; i < batchIds.Count; i++)
                        {
                            pendingCommand.Parameters.AddWithValue($"@BatchId{i}", batchIds[i]);
                        }
                        
                        var pendingCount = (int)await pendingCommand.ExecuteScalarAsync();
                        var hasPendingItems = pendingCount > 0;
                        
                        // Include grower if:
                        // 1. No cheques at all (unprocessed)
                        // 2. Has voided cheques (can be regenerated)
                        // 3. Has only voided cheques (no printed cheques)
                        // 4. BUT exclude if they have pending payment distribution items (already generated)
                        if ((!hasCheques || hasVoidedCheques) && !hasPendingItems)
                        {
                            filteredGrowers.Add(grower);
                            Infrastructure.Logging.Logger.Info($"Including grower {grower.GrowerName} (ID: {grower.GrowerId}) - HasCheques: {hasCheques}, HasVoided: {hasVoidedCheques}, HasPrinted: {hasPrintedCheques}, HasPending: {hasPendingItems}");
                        }
                        else
                        {
                            Infrastructure.Logging.Logger.Info($"Excluding grower {grower.GrowerName} (ID: {grower.GrowerId}) - HasPrinted: {hasPrintedCheques}, HasVoided: {hasVoidedCheques}, HasPending: {hasPendingItems}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error filtering growers by cheque status: {ex.Message}", ex);
                // Return all growers if filtering fails
                return growers;
            }
            
            return filteredGrowers;
        }

        #endregion

        #region Consolidation Methods

        private async Task GenerateConsolidatedPaymentsAsync()
        {
            try
            {
                // Get growers who have payments in multiple batches
                var growersWithMultipleBatches = await _crossBatchPaymentService.GetGrowersInMultipleBatchesAsync(SelectedBatchIds);
                
                if (!growersWithMultipleBatches.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("No growers found with payments in multiple batches. Consolidation is not needed.", "No Consolidation Opportunities");
                    return;
                }

                // Note: Consolidated payment functionality removed - consolidated payments replaced by payment distributions
                // This section is no longer needed as payment distributions handle cross-batch payments
                await _dialogService.ShowMessageBoxAsync(
                    "Consolidated payment functionality has been replaced by payment distributions.\n\n" +
                    "Please use the payment distribution system to handle cross-batch payments.",
                    "Functionality Replaced");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error generating consolidated payments: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }


        #endregion

        #region Selected Payments Methods

        /// <summary>
        /// Generate payments for selected growers only
        /// </summary>
        private async Task GenerateSelectedPaymentsAsync()
        {
            try
            {
                IsBusy = true;

                // Get selected growers only
                var selectedGrowers = GrowerSelections.Where(g => g.IsSelectedForPayment).ToList();
                
                // Validate at least one selected
                if (!selectedGrowers.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select at least one grower", "No Growers Selected");
                    return;
                }

                // Note: No need to check for excessive advances - the system now allows
                // users to control the exact deduction amount via the "Deduct from this transaction" column.
                // Remaining advances will automatically carry forward to future payments.

                // Create PaymentDistributionItems for selected growers only
                var distributionItems = new List<PaymentDistributionItem>();
                
                foreach (var growerSelection in selectedGrowers)
                {
                    // Get all ReceiptIds for this grower across all selected batches
                    var growerReceipts = await GetGrowerReceiptsAcrossBatchesAsync(growerSelection.GrowerId, SelectedBatchIds);
                    
                    if (!growerReceipts.Any())
                    {
                        Logger.Warn($"No receipts found for grower {growerSelection.GrowerId} in selected batches");
                        continue;
                    }
                    
                    // Use the first receipt as the primary ReceiptId for the main record
                    var primaryReceipt = growerReceipts.First();
                    
                    var distributionItem = new PaymentDistributionItem
                    {
                        GrowerId = growerSelection.GrowerId,
                        GrowerName = growerSelection.GrowerName,
                        GrowerNumber = growerSelection.GrowerNumber,
                        Amount = growerSelection.ConsolidatedAmount, // Store the FULL gross amount - deduction will be applied in GenerateChequeAsync
                        PaymentBatchId = primaryReceipt.PaymentBatchId, // Use primary batch
                        ReceiptId = primaryReceipt.ReceiptId, // Use primary receipt
                        PaymentMethod = SelectedPaymentMethod, // Set the payment method from UI selection
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM",
                        AdvanceDeductionAmount = growerSelection.DeductFromThisTransaction // Store the deduction amount for later application
                    };
                    
                    distributionItems.Add(distributionItem);
                    
                    // Store all receipts for detailed audit tracking
                    distributionItem.ReceiptContributions = growerReceipts;
                }

                // Create a new PaymentDistribution object
                var distribution = new PaymentDistribution
                {
                    DistributionNumber = $"DIST-{DateTime.Now:yyyyMMdd-HHmmss}",
                    DistributionDate = DateTime.UtcNow,
                    DistributionType = "ByGrower",
                    PaymentMethod = "Cheque",
                    TotalAmount = distributionItems.Sum(d => d.Amount),
                    TotalGrowers = distributionItems.Count,
                    TotalBatches = SelectedBatchIds.Count,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM",
                    Items = distributionItems
                };

                // Generate the payment distribution (includes batch status updates in the same transaction)
                var result = await _paymentDistributionService.GenerateCompletePaymentDistributionAsync(
                    distribution, 
                    App.CurrentUser?.Username ?? "SYSTEM",
                    SelectedBatchIds);

                if (result != null)
                {
                    // Note: Batch status updates are already handled inside GenerateCompletePaymentDistributionAsync
                    // in a single transaction to ensure atomicity of all operations
                    
                    await _dialogService.ShowMessageBoxAsync(
                        $"Successfully generated payments for {selectedGrowers.Count} selected growers!\n\n" +
                        $"Total Amount: {selectedGrowers.Sum(g => Math.Max(0, g.ConsolidatedAmount - g.DeductFromThisTransaction)):C}\n" +
                        $"Batches Processed: {SelectedBatchIds.Count}\n\n" +
                        $"You can now go to Enhanced Cheque Preparation to print the cheques.",
                        "Selected Payments Generated");

                    // Refresh the data
                    await RefreshAsync();
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error generating selected payments: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Check if selected payments can be generated
        /// </summary>
        private bool CanGenerateSelectedPayments()
        {
            return GrowerSelections.Any(g => g.IsSelectedForPayment) && !IsBusy;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get all receipts for a grower across multiple batches
        /// </summary>
        private async Task<List<ReceiptContribution>> GetGrowerReceiptsAcrossBatchesAsync(int growerId, List<int> batchIds)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            r.ReceiptId,
                            rpa.PaymentBatchId,
                            rpa.AmountPaid AS Amount,
                            pb.BatchNumber,
                            r.ReceiptDate
                        FROM Receipts r
                        INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        INNER JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        WHERE r.GrowerId = @GrowerId 
                        AND rpa.PaymentBatchId IN @BatchIds
                        ORDER BY r.ReceiptId, rpa.PaymentBatchId";
                    
                    var receipts = await connection.QueryAsync<dynamic>(sql, new { GrowerId = growerId, BatchIds = batchIds });
                    
                    var contributions = new List<ReceiptContribution>();
                    foreach (var receipt in receipts)
                    {
                        contributions.Add(new ReceiptContribution
                        {
                            ReceiptId = receipt.ReceiptId,
                            PaymentBatchId = receipt.PaymentBatchId,
                            Amount = receipt.Amount,
                            BatchNumber = receipt.BatchNumber,
                            ReceiptDate = receipt.ReceiptDate
                        });
                    }
                    
                    return contributions;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipts for grower {growerId}: {ex.Message}", ex);
                return new List<ReceiptContribution>();
            }
        }

        /// <summary>
        /// Get the primary ReceiptId for a grower from a specific batch
        /// </summary>
        private async Task<int> GetPrimaryReceiptIdForGrowerAsync(int growerId, int batchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT TOP 1 r.ReceiptId
                        FROM Receipts r
                        INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        WHERE r.GrowerId = @GrowerId 
                        AND rpa.PaymentBatchId = @BatchId
                        ORDER BY r.ReceiptId";
                    
                    var receiptId = await connection.QueryFirstOrDefaultAsync<int?>(sql, new { GrowerId = growerId, BatchId = batchId });
                    
                    if (receiptId.HasValue)
                    {
                        return receiptId.Value;
                    }
                    
                    // If no receipt found, return 0 as fallback
                    Logger.Info($"No ReceiptId found for GrowerId {growerId} in BatchId {batchId}, using 0 as fallback");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting primary ReceiptId for grower {growerId}: {ex.Message}", ex);
                return 0; // Fallback value
            }
        }

        #endregion

        #region Property Change Handlers

        /// <summary>
        /// Handle property changes on grower selections
        /// </summary>
        private async void OnGrowerSelectionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GrowerPaymentSelection.IsSelectedForPayment))
            {
                // Reload advance deductions when selection changes
                await LoadOutstandingAdvancesAsync();
            }
            else if (e.PropertyName == nameof(GrowerPaymentSelection.DeductFromThisTransaction) ||
                     e.PropertyName == nameof(GrowerPaymentSelection.NetPaymentAmount))
            {
                // Recalculate statistics when deduction amounts change
                await CalculateStatisticsAsync();
            }
        }

        #endregion

        #region Advance Deduction Methods

        /// <summary>
        /// Load outstanding advances for selected growers
        /// </summary>
        private async Task LoadOutstandingAdvancesAsync()
        {
            try
            {
                if (!GrowerSelections.Any())
                {
                    AdvanceDeductions?.Clear();
                    GrowerAdvanceInfos?.Clear();
                    HasAdvanceDeductions = false;
                    return;
                }

                var growerIds = GrowerSelections.Select(gs => gs.GrowerId).ToList();
                var advanceInfos = new List<GrowerAdvanceInfo>();

                foreach (var growerId in growerIds)
                {
                    var advances = await _unifiedAdvanceService.GetOutstandingAdvancesAsync(growerId);
                    if (advances.Any())
                    {
                        var growerInfo = new GrowerAdvanceInfo
                        {
                            GrowerId = growerId,
                            GrowerNumber = advances.First().GrowerNumber,
                            GrowerName = advances.First().GrowerName,
                            HasOutstandingAdvances = true,
                            CanModifyDeductions = true
                        };

                        foreach (var advance in advances)
                        {
                            growerInfo.AddOutstandingAdvance(advance);
                        }

                        advanceInfos.Add(growerInfo);
                    }
                }

                GrowerAdvanceInfos = new ObservableCollection<GrowerAdvanceInfo>(advanceInfos);
                HasAdvanceDeductions = advanceInfos.Any();
                ShowAdvanceDeductions = HasAdvanceDeductions;
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error loading outstanding advances: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Calculate suggested deductions for selected growers
        /// </summary>
        private async Task CalculateSuggestedDeductionsAsync()
        {
            try
            {
                Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Starting with {GrowerAdvanceInfos?.Count ?? 0} grower advance infos");
                
                if (!GrowerAdvanceInfos.Any())
                {
                    Infrastructure.Logging.Logger.Info("CalculateSuggestedDeductionsAsync: No grower advance infos found");
                    return;
                }

                var deductionItems = new List<AdvanceDeductionItem>();

                foreach (var growerInfo in GrowerAdvanceInfos)
                {
                    Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Processing grower {growerInfo.GrowerId} ({growerInfo.GrowerName})");
                    
                    var paymentAmount = GrowerSelections
                        .Where(gs => gs.GrowerId == growerInfo.GrowerId)
                        .Sum(gs => gs.TotalAmount);

                    Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Payment amount for grower {growerInfo.GrowerId}: {paymentAmount:C}");

                    if (paymentAmount > 0)
                    {
                        Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Calling CalculateSuggestedDeductionsAsync for grower {growerInfo.GrowerId}");
                        var suggestions = await _unifiedAdvanceService.CalculateSuggestedDeductionsAsync(growerInfo.GrowerId, paymentAmount);
                        Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Got {suggestions?.Count ?? 0} suggestions for grower {growerInfo.GrowerId}");
                        
                        foreach (var suggestion in suggestions)
                        {
                            var advance = growerInfo.OutstandingAdvances.FirstOrDefault(a => a.AdvanceChequeId == suggestion.AdvanceChequeId);
                            if (advance != null)
                            {
                                Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Creating deduction item for advance {suggestion.AdvanceChequeId}, amount: {suggestion.DeductionAmount:C}");
                                
                                var deductionItem = new AdvanceDeductionItem
                                {
                                    AdvanceChequeId = suggestion.AdvanceChequeId,
                                    AdvanceDate = advance.AdvanceDate.ToString("MMM dd, yyyy"),
                                    OriginalAmount = advance.OriginalAdvanceAmount,
                                    RemainingAmount = advance.CurrentAdvanceAmount,
                                    SuggestedDeduction = suggestion.DeductionAmount,
                                    ActualDeduction = suggestion.DeductionAmount,
                                    CanModify = true,
                                    Status = "Active",
                                    Reason = "Auto-calculated"
                                };

                                deductionItems.Add(deductionItem);
                            }
                            else
                            {
                                Infrastructure.Logging.Logger.Warn($"CalculateSuggestedDeductionsAsync: No matching advance found for suggestion {suggestion.AdvanceChequeId}");
                            }
                        }
                    }
                    else
                    {
                        Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Payment amount is 0 for grower {growerInfo.GrowerId}, skipping");
                    }
                }

                Infrastructure.Logging.Logger.Info($"CalculateSuggestedDeductionsAsync: Created {deductionItems.Count} deduction items");
                AdvanceDeductions = new ObservableCollection<AdvanceDeductionItem>(deductionItems);
                UpdateDeductionTotals();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"CalculateSuggestedDeductionsAsync: Error calculating suggested deductions: {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error calculating suggested deductions: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Update deduction totals and remaining payment amounts
        /// </summary>
        private void UpdateDeductionTotals()
        {
            TotalDeductionAmount = AdvanceDeductions?.Sum(d => d.ActualDeduction) ?? 0;
            TotalRemainingPayment = GrowerSelections.Sum(gs => gs.TotalAmount) - TotalDeductionAmount;
            
            OnPropertyChanged(nameof(TotalDeductionAmount));
            OnPropertyChanged(nameof(TotalRemainingPayment));
        }

        /// <summary>
        /// Show advance deductions dialog
        /// </summary>
        private async Task ShowAdvanceDeductionsAsync()
        {
            try
            {
                if (!HasAdvanceDeductions)
                {
                    await _dialogService.ShowMessageBoxAsync("No outstanding advances found for selected growers.", "No Advances");
                    return;
                }

                // Ensure we have the latest advance data before showing the dialog
                await LoadOutstandingAdvancesAsync();
                
                if (!HasAdvanceDeductions)
                {
                    await _dialogService.ShowMessageBoxAsync("No outstanding advances found for selected growers.", "No Advances");
                    return;
                }

                // Calculate suggested deductions if we don't have them yet
                if (!AdvanceDeductions?.Any() == true)
                {
                    await CalculateSuggestedDeductionsAsync();
                }

                // Create and show advance deduction dialog
                var dialog = new AdvanceDeductionDialog();
                dialog.DataContext = this;
                
                var result = await _dialogService.ShowDialogAsync<AdvanceDeductionDialog>(dialog);
                if (result == true)
                {
                    await CalculateSuggestedDeductionsAsync();
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error showing advance deductions: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Modify specific deduction amount
        /// </summary>
        private async Task ModifyDeductionAsync(object parameter)
        {
            try
            {
                if (parameter is AdvanceDeductionItem deductionItem)
                {
                    // Show modification dialog for specific deduction
                    var dialog = new AdvanceDeductionDialog();
                    dialog.DataContext = this;
                    dialog.SelectedDeduction = deductionItem;
                    
                    var result = await _dialogService.ShowDialogAsync<AdvanceDeductionDialog>(dialog);
                    if (result == true)
                    {
                        UpdateDeductionTotals();
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error modifying deduction: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Refresh outstanding advances
        /// </summary>
        private async Task RefreshAdvancesAsync()
        {
            try
            {
                await LoadOutstandingAdvancesAsync();
                if (HasAdvanceDeductions)
                {
                    await CalculateSuggestedDeductionsAsync();
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error refreshing advances: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Apply deductions to distribution items
        /// </summary>
        private async Task ApplyDeductionsAsync()
        {
            try
            {
                if (AdvanceDeductions?.Any() != true)
                {
                    await _dialogService.ShowMessageBoxAsync("No deductions to apply.", "No Deductions");
                    return;
                }

                // Validate all deductions
                var invalidDeductions = AdvanceDeductions.Where(d => !d.IsValidDeduction).ToList();
                if (invalidDeductions.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Some deduction amounts are invalid. Please correct them before applying.", "Invalid Deductions");
                    return;
                }

                // Apply deductions to distribution items
                foreach (var deduction in AdvanceDeductions)
                {
                    var growerSelection = GrowerSelections.FirstOrDefault(gs => 
                        gs.GrowerId == GrowerAdvanceInfos.FirstOrDefault(gai => 
                            gai.OutstandingAdvances.Any(oa => oa.AdvanceChequeId == deduction.AdvanceChequeId))?.GrowerId);

                    if (growerSelection != null)
                    {
                        // Update the grower selection with deduction amount
                        growerSelection.AdvanceDeductionAmount = deduction.ActualDeduction;
                        // Note: NetPaymentAmount is now computed automatically based on ConsolidatedAmount and DeductFromThisTransaction
                    }
                }

                await _dialogService.ShowMessageBoxAsync("Deductions applied successfully.", "Success");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error applying deductions: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Clear all deductions
        /// </summary>
        private async Task ClearDeductionsAsync()
        {
            try
            {
                AdvanceDeductions?.Clear();
                TotalDeductionAmount = 0;
                TotalRemainingPayment = GrowerSelections.Sum(gs => gs.TotalAmount);
                
                // Reset grower selections
                foreach (var growerSelection in GrowerSelections)
                {
                    growerSelection.AdvanceDeductionAmount = 0;
                    growerSelection.DeductFromThisTransaction = 0;
                    // Note: NetPaymentAmount is now computed automatically
                }

                await _dialogService.ShowMessageBoxAsync("All deductions cleared.", "Success");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error clearing deductions: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Check if advance deductions can be shown
        /// </summary>
        private bool CanShowAdvanceDeductions()
        {
            return HasAdvanceDeductions && GrowerSelections.Any();
        }

        /// <summary>
        /// Check if deduction can be modified
        /// </summary>
        private bool CanModifyDeduction(object parameter)
        {
            return parameter is AdvanceDeductionItem deductionItem && deductionItem.CanModify;
        }

        /// <summary>
        /// Check if deductions can be applied
        /// </summary>
        private bool CanApplyDeductions()
        {
            return AdvanceDeductions?.Any() == true && AdvanceDeductions.All(d => d.IsValidDeduction);
        }

        /// <summary>
        /// Show deductions for a specific grower
        /// </summary>
        private async Task ShowGrowerDeductionsAsync(object parameter)
        {
            try
            {
                if (parameter is GrowerPaymentSelection growerSelection)
                {
                    if (!growerSelection.HasOutstandingAdvances)
                    {
                        await _dialogService.ShowMessageBoxAsync("This grower has no outstanding advances.", "No Advances");
                        return;
                    }

                    // Get outstanding advances for this specific grower
                    var outstandingAdvances = await _unifiedAdvanceService.GetOutstandingAdvancesAsync(growerSelection.GrowerId);
                    
                    if (!outstandingAdvances.Any())
                    {
                        await _dialogService.ShowMessageBoxAsync("No outstanding advances found for this grower.", "No Advances");
                        return;
                    }

                    // Create and show grower-specific deduction dialog
                    var dialog = new GrowerDeductionDialog();
                    dialog.DataContext = new GrowerDeductionViewModel(growerSelection, outstandingAdvances.ToList());
                    
                    var result = await _dialogService.ShowDialogAsync<GrowerDeductionDialog>(dialog);
                    if (result == true)
                    {
                        // Update the grower selection with new deduction amounts
                        var viewModel = dialog.DataContext as GrowerDeductionViewModel;
                        if (viewModel != null)
                        {
                            growerSelection.DeductFromThisTransaction = viewModel.DeductFromThisTransaction;
                            growerSelection.RemainingDeductions = viewModel.RemainingDeductions;
                            // Note: NetPaymentAmount is now computed automatically based on ConsolidatedAmount and DeductFromThisTransaction
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error showing grower deductions: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Show detailed payment information for a specific grower
        /// </summary>
        private async Task ShowGrowerPaymentDetailsAsync(object parameter)
        {
            try
            {
                if (parameter is GrowerPaymentSelection growerSelection)
                {
                    var viewModel = new GrowerPaymentDetailsViewModel(
                        growerSelection,
                        SelectedBatchIds,
                        _crossBatchPaymentService,
                        _unifiedAdvanceService,
                        _paymentBatchService,
                        _receiptService);

                    var dialog = new Views.GrowerPaymentDetailsDialog
                    {
                        DataContext = viewModel,
                        Owner = System.Windows.Application.Current.MainWindow
                    };

                    // Load data before showing dialog
                    await viewModel.LoadDataAsync();

                    dialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error showing grower payment details: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Check if grower deductions can be shown
        /// </summary>
        private bool CanShowGrowerDeductions(object parameter)
        {
            return parameter is GrowerPaymentSelection growerSelection && growerSelection.HasOutstandingAdvances && !IsBusy;
        }

        /// <summary>
        /// Check if grower payment details can be shown
        /// </summary>
        private bool CanShowGrowerPaymentDetails(object parameter)
        {
            return parameter is GrowerPaymentSelection && !IsBusy;
        }

        /// <summary>
        /// Update the SelectAllBatches checkbox state based on current batch selections
        /// </summary>
        private void UpdateSelectAllBatchesState()
        {
            if (!AvailableBatches.Any())
            {
                _selectAllBatches = false;
                return;
            }

            var selectedCount = AvailableBatches.Count(b => b.IsSelected);
            var totalCount = AvailableBatches.Count;

            if (selectedCount == 0)
            {
                _selectAllBatches = false;
            }
            else if (selectedCount == totalCount)
            {
                _selectAllBatches = true;
            }
            else
            {
                // Some but not all selected - indeterminate state, keep current value
                // In WPF, we'll treat this as unchecked to allow easy "select all" action
                _selectAllBatches = false;
            }

            OnPropertyChangedInternal(nameof(SelectAllBatches));
        }

        #endregion
    }
}
