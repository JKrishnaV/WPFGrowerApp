using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using WPFGrowerApp.Models;
using System.Windows.Data;
using System.Collections.Specialized;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for processing final (year-end) payments
    /// Similar to PaymentRunViewModel but for final payments after all advances
    /// </summary>
    public class FinalPaymentViewModel : ViewModelBase, IProgress<string>
    {
        private readonly IPaymentService _paymentService;
        private readonly IDialogService _dialogService;
        private readonly IPayGroupService _payGroupService;
        private readonly IGrowerService _growerService;
        private readonly IPaymentBatchManagementService _batchManagementService;
        private readonly IChequeGenerationService _chequeGenerationService;
        private readonly IChequePrintingService _chequePrintingService;
        private readonly IStatementPrintingService _statementPrintingService;
        private readonly IHelpContentProvider _helpContentProvider;

        // Properties
        private DateTime _paymentDate = DateTime.Today;
        private DateTime _cutoffDate = DateTime.Today;
        private int _cropYear = DateTime.Today.Year;
        private bool _isRunning;
        private string? _statusMessage;
        private ObservableCollection<string>? _runLog;
        
        // Collections
        private ObservableCollection<PayGroup> _allPayGroups;
        private ObservableCollection<GrowerInfo> _allGrowers;
        private ICollectionView _filteredPayGroupsView;
        private ICollectionView _filteredGrowersView;
        
        // Filters
        private string _payGroupSearchText = string.Empty;
        private string _growerSearchText = string.Empty;
        public ObservableCollection<PayGroup> SelectedExcludePayGroups { get; private set; } = new ObservableCollection<PayGroup>();
        public ObservableCollection<GrowerInfo> SelectedExcludeGrowers { get; private set; } = new ObservableCollection<GrowerInfo>();
        
        // Results
        private List<GrowerFinalPayment>? _finalPaymentResults;
        private PaymentBatch? _currentBatch;
        private ObservableCollection<Cheque>? _generatedCheques;
        private bool _chequesGenerated;
        private bool _chequesPrinted;
        private bool _statementsGenerated;
        
        // On Hold List
        private ObservableCollection<GrowerInfo>? _onHoldGrowers;
        private bool _isLoadingOnHoldGrowers;

        // Public Properties
        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
        }

        public DateTime CutoffDate
        {
            get => _cutoffDate;
            set => SetProperty(ref _cutoffDate, value);
        }

        public int CropYear
        {
            get => _cropYear;
            set => SetProperty(ref _cropYear, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        public string? StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> RunLog
        {
            get => _runLog ??= new ObservableCollection<string>();
            set => SetProperty(ref _runLog, value);
        }

        public List<GrowerFinalPayment> FinalPaymentResults
        {
            get => _finalPaymentResults ?? new List<GrowerFinalPayment>();
            set => SetProperty(ref _finalPaymentResults, value);
        }

        public PaymentBatch? CurrentBatch
        {
            get => _currentBatch;
            set => SetProperty(ref _currentBatch, value);
        }

        public ObservableCollection<Cheque> GeneratedCheques
        {
            get => _generatedCheques ??= new ObservableCollection<Cheque>();
            set => SetProperty(ref _generatedCheques, value);
        }

        public bool ChequesGenerated
        {
            get => _chequesGenerated;
            set => SetProperty(ref _chequesGenerated, value);
        }

        public bool ChequesPrinted
        {
            get => _chequesPrinted;
            set => SetProperty(ref _chequesPrinted, value);
        }

        public bool StatementsGenerated
        {
            get => _statementsGenerated;
            set => SetProperty(ref _statementsGenerated, value);
        }

        public ObservableCollection<GrowerInfo> OnHoldGrowers
        {
            get => _onHoldGrowers ??= new ObservableCollection<GrowerInfo>();
            set => SetProperty(ref _onHoldGrowers, value);
        }

        public bool IsLoadingOnHoldGrowers
        {
            get => _isLoadingOnHoldGrowers;
            set => SetProperty(ref _isLoadingOnHoldGrowers, value);
        }

        // Filtered views for ListBoxes
        public ICollectionView FilteredPayGroupsView => _filteredPayGroupsView;
        public ICollectionView FilteredGrowersView => _filteredGrowersView;

        public string PayGroupSearchText
        {
            get => _payGroupSearchText;
            set
            {
                if (SetProperty(ref _payGroupSearchText, value))
                {
                    _filteredPayGroupsView?.Refresh();
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
                    _filteredGrowersView?.Refresh();
                }
            }
        }

        // Crop Year collection
        public ObservableCollection<int> CropYears { get; } = new ObservableCollection<int>();

        // Can Execute properties
        public bool CanCalculateFinal => !IsRunning;
        public bool CanPostBatch => FinalPaymentResults != null && FinalPaymentResults.Any() && CurrentBatch == null && !IsRunning;
        public bool CanGenerateCheques => CurrentBatch != null && !ChequesGenerated && !IsRunning;
        public bool CanPrintCheques => ChequesGenerated && !IsRunning;
        public bool CanPrintStatements => CurrentBatch != null && !IsRunning;

        // Commands
        public ICommand LoadFiltersCommand { get; }
        public ICommand CalculateFinalPaymentsCommand { get; }
        public ICommand PostBatchCommand { get; }
        public ICommand GenerateChequesCommand { get; }
        public ICommand PrintChequesCommand { get; }
        public ICommand PrintStatementsCommand { get; }
        public ICommand ViewBatchDetailsCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand RefreshCommand { get; }

        public FinalPaymentViewModel(
            IPaymentService paymentService,
            IDialogService dialogService,
            IPayGroupService payGroupService,
            IGrowerService growerService,
            IPaymentBatchManagementService batchManagementService,
            IChequeGenerationService chequeGenerationService,
            IChequePrintingService chequePrintingService,
            IStatementPrintingService statementPrintingService,
            IHelpContentProvider helpContentProvider)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _batchManagementService = batchManagementService ?? throw new ArgumentNullException(nameof(batchManagementService));
            _chequeGenerationService = chequeGenerationService ?? throw new ArgumentNullException(nameof(chequeGenerationService));
            _chequePrintingService = chequePrintingService ?? throw new ArgumentNullException(nameof(chequePrintingService));
            _statementPrintingService = statementPrintingService ?? throw new ArgumentNullException(nameof(statementPrintingService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));

            // Initialize collections
            _allPayGroups = new ObservableCollection<PayGroup>();
            _allGrowers = new ObservableCollection<GrowerInfo>();

            // Set up filtered views
            _filteredPayGroupsView = CollectionViewSource.GetDefaultView(_allPayGroups);
            _filteredPayGroupsView.Filter = FilterPayGroups;

            _filteredGrowersView = CollectionViewSource.GetDefaultView(_allGrowers);
            _filteredGrowersView.Filter = FilterGrowers;

            // Initialize commands
            LoadFiltersCommand = new RelayCommand(async o => await LoadFiltersAsync());
            CalculateFinalPaymentsCommand = new RelayCommand(async o => await CalculateFinalPaymentsAsync(), o => CanCalculateFinal);
            PostBatchCommand = new RelayCommand(async o => await PostBatchAsync(), o => CanPostBatch);
            GenerateChequesCommand = new RelayCommand(async o => await GenerateChequesAsync(), o => CanGenerateCheques);
            PrintChequesCommand = new RelayCommand(async o => await PrintChequesAsync(), o => CanPrintCheques);
            PrintStatementsCommand = new RelayCommand(async o => await PrintStatementsAsync(), o => CanPrintStatements);
            ViewBatchDetailsCommand = new RelayCommand(async o => await ViewBatchDetailsAsync(), o => CurrentBatch != null);
            ShowHelpCommand = new RelayCommand(o => ShowHelp());
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());

            // Initialize crop years
            for (int i = 0; i < 5; i++)
            {
                CropYears.Add(DateTime.Today.Year - i);
            }

            // Set default year
            CropYear = DateTime.Today.Year;

            // Load initial data
            _ = InitializeAsync();
        }

        // ==============================================================
        // INITIALIZATION
        // ==============================================================

        private async Task InitializeAsync()
        {
            await LoadFiltersAsync();
            await UpdateOnHoldGrowersAsync();
        }

        private async Task LoadFiltersAsync()
        {
            try
            {
                // Load PayGroups
                var payGroupList = await _payGroupService.GetAllPayGroupsAsync();
                _allPayGroups.Clear();
                foreach (var pg in payGroupList)
                {
                    _allPayGroups.Add(pg);
                }
                _filteredPayGroupsView?.Refresh();

                // Load Growers
                var growerList = await _growerService.GetAllGrowersBasicInfoAsync();
                _allGrowers.Clear();
                foreach (var g in growerList)
                {
                    _allGrowers.Add(g);
                }
                _filteredGrowersView?.Refresh();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filter data for Final Payment", ex);
                await _dialogService.ShowMessageBoxAsync("Error loading filter options.", "Load Error");
            }
        }

        private async Task UpdateOnHoldGrowersAsync()
        {
            IsLoadingOnHoldGrowers = true;

            try
            {
                var onHold = await _growerService.GetOnHoldGrowersAsync();

                // Apply client-side filtering
                var excludedGrowerIds = SelectedExcludeGrowers.Select(g => (int)g.GrowerNumber).Where(num => num != 0).ToList();
                if (excludedGrowerIds.Any())
                {
                    onHold = onHold.Where(g => !excludedGrowerIds.Contains((int)g.GrowerNumber)).ToList();
                }

                OnHoldGrowers.Clear();
                if (onHold != null)
                {
                    foreach (var g in onHold)
                    {
                        OnHoldGrowers.Add(g);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update On Hold Growers list", ex);
            }
            finally
            {
                IsLoadingOnHoldGrowers = false;
            }
        }

        // Filter predicates
        private bool FilterPayGroups(object obj)
        {
            if (obj is not PayGroup payGroup) return false;
            if (string.IsNullOrWhiteSpace(PayGroupSearchText)) return true;
            return payGroup.GroupName?.Contains(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool FilterGrowers(object obj)
        {
            if (obj is not GrowerInfo grower) return false;
            if (string.IsNullOrWhiteSpace(GrowerSearchText)) return true;
            return grower.Name?.Contains(GrowerSearchText, StringComparison.OrdinalIgnoreCase) == true ||
                   grower.GrowerNumber.ToString().Contains(GrowerSearchText);
        }

        // ==============================================================
        // COMMAND IMPLEMENTATIONS
        // ==============================================================

        /// <summary>
        /// Calculate final payments for all eligible growers
        /// </summary>
        private async Task CalculateFinalPaymentsAsync()
        {
            try
            {
                IsRunning = true;
                RunLog.Clear();
                StatusMessage = "Calculating final payments...";
                Report("Starting final payment calculation...");
                Report($"Parameters: PaymentDate={PaymentDate:d}, CropYear={CropYear}");

                // Confirm with user
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will calculate the final (year-end) payment for all growers.\n\n" +
                    $"Crop Year: {CropYear}\n" +
                    $"Payment Date: {PaymentDate:yyyy-MM-dd}\n\n" +
                    $"This calculation shows what each grower is owed after all advances.\n\n" +
                    $"Continue?",
                    "Calculate Final Payments?");

                if (confirm != true)
                {
                    IsRunning = false;
                    return;
                }

                Report("Calculating final payments for all growers...");

                // Get excluded growers and pay groups
                var excludedGrowerIds = SelectedExcludeGrowers.Select(g => (int)g.GrowerNumber).Where(num => num != 0).ToList();
                var excludedPayGroupIds = SelectedExcludePayGroups.Select(pg => pg.GroupCode).ToList();

                // TODO: Implement CalculateFinalPaymentBatchAsync in PaymentCalculationService
                // For now, show message that this feature is in development
                Report("⚠ Final payment calculation logic needs to be implemented");
                Report("  This would calculate: Total receipts - Advances - Deductions = Final Payment");
                
                await _dialogService.ShowMessageBoxAsync(
                    "Final payment calculation is being implemented.\n\n" +
                    "This will calculate:\n" +
                    "• Total receipt value at final prices\n" +
                    "• Minus all advances already paid\n" +
                    "• Minus all deductions\n" +
                    "• Equals final payment due\n\n" +
                    "Please check back soon!",
                    "Feature In Development");

                StatusMessage = "Final payment calculation - feature in development";
            }
            catch (Exception ex)
            {
                Logger.Error("Error calculating final payments", ex);
                Report($"✗ ERROR: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Post the final payment batch
        /// </summary>
        private async Task PostBatchAsync()
        {
            try
            {
                if (FinalPaymentResults == null || !FinalPaymentResults.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please calculate final payments first.", "Calculation Required");
                    return;
                }

                // Confirm with user
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will create the final (year-end) payment batch.\n\n" +
                    $"Growers: {FinalPaymentResults.Count}\n" +
                    $"Total Payment: ${FinalPaymentResults.Sum(g => g.NetPayment):N2}\n\n" +
                    $"Continue?",
                    "Post Final Payment Batch?");

                if (confirm != true)
                    return;

                IsRunning = true;
                StatusMessage = "Posting final payment batch...";
                Report("Creating final payment batch...");

                // Create payment batch (PaymentTypeId = 4 for FINAL)
                CurrentBatch = await _batchManagementService.CreatePaymentBatchAsync(
                    paymentTypeId: 4, // FINAL payment type
                    batchDate: PaymentDate,
                    cropYear: CropYear,
                    cutoffDate: CutoffDate,
                    notes: $"Final Payment - {FinalPaymentResults.Count} growers",
                    createdBy: App.CurrentUser?.Username);

                Report($"✓ Created batch: {CurrentBatch.BatchNumber}");

                // TODO: Post final payments to accounts
                Report("⚠ Final payment posting logic needs to be implemented");
                
                await _dialogService.ShowMessageBoxAsync(
                    $"Batch created: {CurrentBatch.BatchNumber}\n\n" +
                    $"Final payment posting logic is being implemented.\n" +
                    $"Please check back soon!",
                    "Feature In Development");

                StatusMessage = $"Batch {CurrentBatch.BatchNumber} created";
            }
            catch (Exception ex)
            {
                Logger.Error("Error posting final batch", ex);
                Report($"✗ ERROR: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Generate cheques for final payment
        /// </summary>
        private async Task GenerateChequesAsync()
        {
            try
            {
                if (CurrentBatch == null || FinalPaymentResults == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch available. Please post batch first.", "No Batch");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Generating final payment cheques...";
                Report("Generating cheques...");

                // Convert results to GrowerPaymentAmount list
                var growerPayments = FinalPaymentResults
                    .Where(gp => !gp.IsOnHold && gp.NetPayment > 0)
                    .Select(gp => new GrowerPaymentAmount
                    {
                        GrowerId = gp.GrowerNumber,
                        GrowerName = gp.GrowerName,
                        PaymentAmount = gp.NetPayment,
                        Memo = $"Final Payment {CropYear}",
                        IsOnHold = gp.IsOnHold
                    }).ToList();

                var cheques = await _chequeGenerationService.GenerateChequesForBatchAsync(
                    CurrentBatch.PaymentBatchId,
                    growerPayments);

                GeneratedCheques.Clear();
                foreach (var cheque in cheques)
                {
                    GeneratedCheques.Add(cheque);
                }

                ChequesGenerated = true;
                Report($"✓ Generated {cheques.Count} cheques");
                StatusMessage = $"Generated {cheques.Count} final payment cheques";

                await _dialogService.ShowMessageBoxAsync(
                    $"Successfully generated {cheques.Count} final payment cheques!\n\n" +
                    $"Cheque range: {cheques.Min(c => c.ChequeNumber)} - {cheques.Max(c => c.ChequeNumber)}",
                    "Cheques Generated");
            }
            catch (Exception ex)
            {
                Logger.Error("Error generating final payment cheques", ex);
                Report($"✗ ERROR: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Print generated cheques
        /// </summary>
        private async Task PrintChequesAsync()
        {
            try
            {
                if (!GeneratedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("No cheques to print.", "No Cheques");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Printing final payment cheques...";
                Report($"Printing {GeneratedCheques.Count} cheques...");

                var printedCount = await _chequePrintingService.PrintChequesAsync(GeneratedCheques.ToList());

                if (printedCount > 0)
                {
                    ChequesPrinted = true;
                    Report($"✓ Printed {printedCount} cheques");
                    StatusMessage = $"Printed {printedCount} cheques successfully";

                    await _dialogService.ShowMessageBoxAsync(
                        $"Successfully printed {printedCount} final payment cheques!",
                        "Printing Complete");
                }
                else
                {
                    Report("Printing cancelled by user");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error printing cheques", ex);
                Report($"✗ ERROR: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Print year-end statements
        /// </summary>
        private async Task PrintStatementsAsync()
        {
            try
            {
                if (CurrentBatch == null || FinalPaymentResults == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch available.", "No Batch");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Printing year-end statements...";
                Report($"Printing statements for {FinalPaymentResults.Count} growers...");

                // Print year-end statements
                int printedCount = 0;
                foreach (var finalPayment in FinalPaymentResults.Where(g => !g.IsOnHold))
                {
                    var printed = await _statementPrintingService.PrintYearEndStatementAsync(finalPayment);
                    if (printed) printedCount++;
                }

                if (printedCount > 0)
                {
                    StatementsGenerated = true;
                    Report($"✓ Printed {printedCount} statements");
                    StatusMessage = $"Printed {printedCount} year-end statements";

                    await _dialogService.ShowMessageBoxAsync(
                        $"Successfully printed {printedCount} year-end statements!",
                        "Printing Complete");
                }
                else
                {
                    Report("Statement printing cancelled");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error printing statements", ex);
                Report($"✗ ERROR: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync($"Error: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// View batch details
        /// </summary>
        private async Task ViewBatchDetailsAsync()
        {
            try
            {
                if (CurrentBatch == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch selected.", "No Batch");
                    return;
                }

                var summary = await _batchManagementService.GetBatchSummaryAsync(CurrentBatch.PaymentBatchId);

                var message = $"Final Payment Batch Details:\n\n" +
                             $"Batch Number: {summary.BatchNumber}\n" +
                             $"Crop Year: {summary.CropYear}\n" +
                             $"Batch Date: {summary.BatchDate:yyyy-MM-dd}\n" +
                             $"Status: {summary.Status}\n\n" +
                             $"Growers: {summary.TotalGrowers}\n" +
                             $"Total Amount: ${summary.TotalAmount:N2}\n" +
                             $"Cheques: {summary.ChequesGenerated}\n\n" +
                             $"Created: {summary.CreatedAt:g} by {summary.CreatedBy}";

                if (summary.PostedAt.HasValue)
                {
                    message += $"\nPosted: {summary.PostedAt:g} by {summary.PostedBy}";
                }

                await _dialogService.ShowMessageBoxAsync(message, "Batch Details");
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing batch details", ex);
                await _dialogService.ShowMessageBoxAsync($"Error: {ex.Message}", "Error");
            }
        }

        private async Task RefreshAsync()
        {
            await LoadFiltersAsync();
            await UpdateOnHoldGrowersAsync();
        }

        private async void ShowHelp()
        {
            var helpContent = _helpContentProvider.GetHelpContent("FinalPayment");
            await _dialogService.ShowMessageBoxAsync(helpContent?.Content ?? "Help content not available.", "Final Payment Help");
        }

        // IProgress<string> implementation
        public void Report(string value)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                RunLog.Add($"{DateTime.Now:HH:mm:ss} - {value}");
                if (RunLog.Count > 200) RunLog.RemoveAt(0);
            });
        }
    }
}

