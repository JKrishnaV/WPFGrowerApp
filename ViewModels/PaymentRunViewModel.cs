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
using System.Windows.Data; // For ICollectionView
using System.Collections.Specialized; // For INotifyCollectionChanged
using WPFGrowerApp.Models; // Added for AdvanceOption, FilterPreset, and FilterItem

// Need this for the PdfViewerWindow call later
using WPFGrowerApp.Views;
// Need this for the PaymentTestRunReportViewModel call later
using WPFGrowerApp.ViewModels; // Assuming PaymentTestRunReportViewModel is here now
// Need this for MemoryStream
using System.IO;
// Add using for Bold Reports (Found in code-behind)
using BoldReports.Windows;


namespace WPFGrowerApp.ViewModels
{
    public class PaymentRunViewModel : ViewModelBase, IProgress<string>
    {
        private readonly IPaymentService _paymentService;
        private readonly IDialogService _dialogService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IPayGroupService _payGroupService;
        private readonly IGrowerService _growerService;
        
        // Phase 1 - New services
        private readonly IPaymentBatchManagementService _batchManagementService;
        private readonly IChequeGenerationService _chequeGenerationService;
        private readonly IChequePrintingService _chequePrintingService;
        private readonly IStatementPrintingService _statementPrintingService;

        private int _advanceNumber = 1;
        private DateTime _paymentDate = DateTime.Today;
        private DateTime _cutoffDate = DateTime.Today;
        private DateTime? _receiptDateFrom; // NEW: Receipt date range start
        private int _cropYear = DateTime.Today.Year;
        private bool _isRunning;
        private string? _statusMessage;
        private ObservableCollection<string>? _runLog;
        private PaymentBatch? _lastRunBatch; // For actual run
        private List<string>? _lastRunErrors; // For actual run
        private TestRunResult? _latestTestRunResult; // For test run
        
        // Phase 1 - Enhanced properties
        private PaymentBatch? _currentBatch;
        private ObservableCollection<Cheque>? _generatedCheques;
        private bool _chequesGenerated;
        private bool _chequesPrinted;
        private bool _statementsGenerated;

        // --- Collections ---
        // Original backing collections for all items
        private ObservableCollection<Product> _allProducts;
        private ObservableCollection<Process> _allProcesses;
        private ObservableCollection<PayGroup> _allPayGroups;
        private ObservableCollection<GrowerInfo> _allGrowers;

        // ICollectionView wrappers for filtering ListBoxes
        private ICollectionView _filteredProductsView;
        private ICollectionView _filteredProcessesView;
        private ICollectionView _filteredPayGroupsView;
        private ICollectionView _filteredGrowersView;

        // Collection for Advance Number ComboBox
        public ObservableCollection<AdvanceOption> AdvanceOptions { get; } = new ObservableCollection<AdvanceOption>();

        // Collection for Crop Year ComboBox
        public ObservableCollection<int> CropYears { get; } = new ObservableCollection<int>();

        // Search Text for Grower Filter
        private string _growerSearchText;
        public string GrowerSearchText
        {
            get => _growerSearchText;
            set
            {
                if (SetProperty(ref _growerSearchText, value))
                {
                    _filteredGrowersView?.Refresh(); // Refresh the filtered view
                    FilteredGrowerFilterItems?.Refresh(); // Refresh the FilterItem view
                }
            }
        }

        private string _productSearchText;
        public string ProductSearchText
        {
            get => _productSearchText;
            set
            {
                if (SetProperty(ref _productSearchText, value))
                {
                    _filteredProductsView?.Refresh();
                }
            }
        }

        private string _processSearchText;
        public string ProcessSearchText
        {
            get => _processSearchText;
            set
            {
                if (SetProperty(ref _processSearchText, value))
                {
                    _filteredProcessesView?.Refresh();
                }
            }
        }

        private string _payGroupSearchText;
        public string PayGroupSearchText
        {
            get => _payGroupSearchText;
            set
            {
                if (SetProperty(ref _payGroupSearchText, value))
                {
                    _filteredPayGroupsView?.Refresh();
                    FilteredPayGroupFilterItems?.Refresh(); // Refresh the FilterItem view
                }
            }
        }
        // --- End Search Text Properties ---

        // NEW: Enhanced Filter Properties
        private decimal? _minimumWeight;
        private bool _showUnpayableReceipts;
        private bool _includePreviouslyPaid;
        private bool _showAdvancedOptions;
        
        // Process Class Filter
        private bool _includeFresh = true;
        private bool _includeProcessed = true;
        private bool _includeJuice = true;
        private bool _includeOther = true;
        
        // Grade Filter
        private bool _includeGrade1 = true;
        private bool _includeGrade2 = true;
        private bool _includeGrade3 = true;
        private bool _includeOtherGrade = true;
        
        // Preview/Summary Properties
        private int _previewReceiptCount;
        private int _previewGrowerCount;
        private decimal _previewEstimatedTotal;
        private int _previewSkippedCount;
        private string _previewSummaryText = "Click 'Preview Count' to see payment summary";
        
        // Filter Presets
        private ObservableCollection<FilterPreset> _filterPresets = new ObservableCollection<FilterPreset>();
        private FilterPreset? _selectedPreset;

        // Collections to hold selected items from ListBoxes (LEGACY - for backward compatibility)
        public ObservableCollection<Product> SelectedProducts { get; private set; } = new ObservableCollection<Product>();
        public ObservableCollection<Process> SelectedProcesses { get; private set; } = new ObservableCollection<Process>();
        public ObservableCollection<PayGroup> SelectedExcludePayGroups { get; private set; } = new ObservableCollection<PayGroup>();
        public ObservableCollection<GrowerInfo> SelectedExcludeGrowers { get; private set; } = new ObservableCollection<GrowerInfo>();

        // NEW: FilterItem collections for checkbox-based selection
        public ObservableCollection<FilterItem<Product>> ProductFilterItems { get; private set; } = new ObservableCollection<FilterItem<Product>>();
        public ObservableCollection<FilterItem<Process>> ProcessFilterItems { get; private set; } = new ObservableCollection<FilterItem<Process>>();
        public ObservableCollection<FilterItem<GrowerInfo>> GrowerFilterItems { get; private set; } = new ObservableCollection<FilterItem<GrowerInfo>>();
        public ObservableCollection<FilterItem<PayGroup>> PayGroupFilterItems { get; private set; } = new ObservableCollection<FilterItem<PayGroup>>();

        // Filtered Collections for Search
        public ICollectionView FilteredGrowerFilterItems { get; }
        public ICollectionView FilteredPayGroupFilterItems { get; }

        // On Hold List properties
        private ObservableCollection<GrowerInfo>? _onHoldGrowers;
        private bool _isLoadingOnHoldGrowers;


        public PaymentRunViewModel(
            IPaymentService paymentService,
            IDialogService dialogService,
            IProductService productService,
            IProcessService processService,
            IPayGroupService payGroupService,
            IGrowerService growerService,
            IPaymentBatchManagementService batchManagementService,
            IChequeGenerationService chequeGenerationService,
            IChequePrintingService chequePrintingService,
            IStatementPrintingService statementPrintingService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _batchManagementService = batchManagementService ?? throw new ArgumentNullException(nameof(batchManagementService));
            _chequeGenerationService = chequeGenerationService ?? throw new ArgumentNullException(nameof(chequeGenerationService));
            _chequePrintingService = chequePrintingService ?? throw new ArgumentNullException(nameof(chequePrintingService));
            _statementPrintingService = statementPrintingService ?? throw new ArgumentNullException(nameof(statementPrintingService));

            _growerSearchText = string.Empty;
            _productSearchText = string.Empty;
            _processSearchText = string.Empty;
            _payGroupSearchText = string.Empty;

            RunLog = new ObservableCollection<string>();
            LastRunErrors = new List<string>();

            // Initialize backing collections
            _allProducts = new ObservableCollection<Product>();
            _allProcesses = new ObservableCollection<Process>();
            _allPayGroups = new ObservableCollection<PayGroup>();
            _allGrowers = new ObservableCollection<GrowerInfo>();
            OnHoldGrowers = new ObservableCollection<GrowerInfo>();

            // Setup filtered views
            _filteredProductsView = CollectionViewSource.GetDefaultView(_allProducts);
            _filteredProductsView.Filter = FilterProducts; // Assign filter predicate
            _filteredProcessesView = CollectionViewSource.GetDefaultView(_allProcesses);
            _filteredProcessesView.Filter = FilterProcesses; // Assign filter predicate
            _filteredPayGroupsView = CollectionViewSource.GetDefaultView(_allPayGroups);
            _filteredPayGroupsView.Filter = FilterPayGroups; // Assign filter predicate
            _filteredGrowersView = CollectionViewSource.GetDefaultView(_allGrowers);
            _filteredGrowersView.Filter = FilterGrowers; // Existing filter

            // Setup filtered views for FilterItem collections
            FilteredGrowerFilterItems = CollectionViewSource.GetDefaultView(GrowerFilterItems);
            FilteredGrowerFilterItems.Filter = FilterGrowerFilterItems;
            FilteredPayGroupFilterItems = CollectionViewSource.GetDefaultView(PayGroupFilterItems);
            FilteredPayGroupFilterItems.Filter = FilterPayGroupFilterItems;

            // Populate Advance Options
            AdvanceOptions.Add(new AdvanceOption { Display = "First Advance Payment", Value = 1 });
            AdvanceOptions.Add(new AdvanceOption { Display = "Second Advance Payment", Value = 2 });
            AdvanceOptions.Add(new AdvanceOption { Display = "Third Advance Payment", Value = 3 });

            // Populate Crop Years
            int currentYear = DateTime.Today.Year;
            CropYears.Add(currentYear);
            CropYears.Add(currentYear - 1);
            CropYears.Add(currentYear - 2);
            CropYears.Add(2022); // Add 2022 for test data
            _cropYear = currentYear; // Set default
            _receiptDateFrom = null; // Initialize receipt date from

            // Add listeners for selection changes
            SelectedProducts.CollectionChanged += SelectedFilters_CollectionChanged;
            SelectedProcesses.CollectionChanged += SelectedFilters_CollectionChanged;
            SelectedExcludePayGroups.CollectionChanged += SelectedFilters_CollectionChanged;
            SelectedExcludeGrowers.CollectionChanged += SelectedFilters_CollectionChanged;


            // Load initial data
            _ = LoadFiltersAsync();
            _ = UpdateOnHoldGrowersAsync(); // Initial load
        }

        // Filter logic for the Grower ListBox
        private bool FilterGrowers(object item)
        {
            if (string.IsNullOrEmpty(GrowerSearchText))
            {
                return true; // No filter applied
            }

            if (item is GrowerInfo grower)
            {
                // Check if name or number contains the search text (case-insensitive)
                return (grower.Name?.IndexOf(GrowerSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (grower.GrowerNumber.ToString().IndexOf(GrowerSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return false;
        }

        // Filter logic for Products
        private bool FilterProducts(object item)
        {
            if (string.IsNullOrEmpty(ProductSearchText)) return true;
            if (item is Product product)
            {
                return (product.ProductCode?.IndexOf(ProductSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (product.Description?.IndexOf(ProductSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }

        // Filter logic for Processes
        private bool FilterProcesses(object item)
        {
            if (string.IsNullOrEmpty(ProcessSearchText)) return true;
            if (item is Process process)
            {
                return (process.ProcessCode?.IndexOf(ProcessSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (process.Description?.IndexOf(ProcessSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }

        // Filter logic for PayGroups
        private bool FilterPayGroups(object item)
        {
            if (string.IsNullOrEmpty(PayGroupSearchText)) return true;
            if (item is PayGroup payGroup)
            {
                return (payGroup.GroupCode?.IndexOf(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (payGroup.Description?.IndexOf(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }

        // Filter logic for GrowerFilterItems
        private bool FilterGrowerFilterItems(object item)
        {
            if (string.IsNullOrEmpty(GrowerSearchText))
            {
                return true; // No filter applied
            }

            if (item is FilterItem<GrowerInfo> growerItem)
            {
                var grower = growerItem.Item;
                // Check if name or number contains the search text (case-insensitive)
                return (grower.Name?.IndexOf(GrowerSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (grower.GrowerNumber.ToString().IndexOf(GrowerSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return false;
        }

        // Filter logic for PayGroupFilterItems
        private bool FilterPayGroupFilterItems(object item)
        {
            if (string.IsNullOrEmpty(PayGroupSearchText)) return true;
            if (item is FilterItem<PayGroup> payGroupItem)
            {
                var payGroup = payGroupItem.Item;
                return (payGroup.GroupCode?.IndexOf(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (payGroup.Description?.IndexOf(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }


        // Properties for UI Binding
        public int AdvanceNumber
        {
            get => _advanceNumber;
            set { SetProperty(ref _advanceNumber, value); }
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set { SetProperty(ref _paymentDate, value); }
        }

        public DateTime CutoffDate
        {
            get => _cutoffDate;
             set { SetProperty(ref _cutoffDate, value); }
        }

        // NEW: Receipt Date Range
        public DateTime? ReceiptDateFrom
        {
            get => _receiptDateFrom;
            set => SetProperty(ref _receiptDateFrom, value);
        }

        public int CropYear
        {
            get => _cropYear;
            set { SetProperty(ref _cropYear, value); }
        }

         // Expose the filtered views for binding
         public ICollectionView FilteredProducts => _filteredProductsView;
         public ICollectionView FilteredProcesses => _filteredProcessesView;
         public ICollectionView FilteredPayGroups => _filteredPayGroupsView;
         public ICollectionView FilteredGrowers => _filteredGrowersView; // Existing

         // Keep original collections accessible if needed internally, but UI binds to filtered views
         // public ObservableCollection<Product> AllProducts { get => _allProducts; } // Example if needed
         // public ObservableCollection<Process> AllProcesses { get => _allProcesses; }
         // public ObservableCollection<PayGroup> AllPayGroups { get => _allPayGroups; }
         // public ObservableCollection<GrowerInfo> AllGrowers { get => _allGrowers; }


         // Properties for On Hold List
         public ObservableCollection<GrowerInfo> OnHoldGrowers { get => _onHoldGrowers ?? new ObservableCollection<GrowerInfo>(); private set => SetProperty(ref _onHoldGrowers, value); }
         public bool IsLoadingOnHoldGrowers { get => _isLoadingOnHoldGrowers; private set => SetProperty(ref _isLoadingOnHoldGrowers, value); }

        // NEW: Enhanced Filter Properties
        public decimal? MinimumWeight
        {
            get => _minimumWeight;
            set => SetProperty(ref _minimumWeight, value);
        }

        public bool ShowUnpayableReceipts
        {
            get => _showUnpayableReceipts;
            set => SetProperty(ref _showUnpayableReceipts, value);
        }

        public bool IncludePreviouslyPaid
        {
            get => _includePreviouslyPaid;
            set => SetProperty(ref _includePreviouslyPaid, value);
        }

        public bool ShowAdvancedOptions
        {
            get => _showAdvancedOptions;
            set => SetProperty(ref _showAdvancedOptions, value);
        }


        // Process Class Filter Properties
        public bool IncludeFresh
        {
            get => _includeFresh;
            set => SetProperty(ref _includeFresh, value);
        }

        public bool IncludeProcessed
        {
            get => _includeProcessed;
            set => SetProperty(ref _includeProcessed, value);
        }

        public bool IncludeJuice
        {
            get => _includeJuice;
            set => SetProperty(ref _includeJuice, value);
        }

        public bool IncludeOther
        {
            get => _includeOther;
            set => SetProperty(ref _includeOther, value);
        }

        // Grade Filter Properties
        public bool IncludeGrade1
        {
            get => _includeGrade1;
            set => SetProperty(ref _includeGrade1, value);
        }

        public bool IncludeGrade2
        {
            get => _includeGrade2;
            set => SetProperty(ref _includeGrade2, value);
        }

        public bool IncludeGrade3
        {
            get => _includeGrade3;
            set => SetProperty(ref _includeGrade3, value);
        }

        public bool IncludeOtherGrade
        {
            get => _includeOtherGrade;
            set => SetProperty(ref _includeOtherGrade, value);
        }

        // Preview/Summary Properties
        public int PreviewReceiptCount
        {
            get => _previewReceiptCount;
            private set => SetProperty(ref _previewReceiptCount, value);
        }

        public int PreviewGrowerCount
        {
            get => _previewGrowerCount;
            private set => SetProperty(ref _previewGrowerCount, value);
        }

        public decimal PreviewEstimatedTotal
        {
            get => _previewEstimatedTotal;
            private set => SetProperty(ref _previewEstimatedTotal, value);
        }

        public int PreviewSkippedCount
        {
            get => _previewSkippedCount;
            private set => SetProperty(ref _previewSkippedCount, value);
        }

        public string PreviewSummaryText
        {
            get => _previewSummaryText;
            private set => SetProperty(ref _previewSummaryText, value);
        }

        // Filter Presets
        public ObservableCollection<FilterPreset> FilterPresets => _filterPresets;

        public FilterPreset? SelectedPreset
        {
            get => _selectedPreset;
            set => SetProperty(ref _selectedPreset, value);
        }

        // NEW: Selection Count Properties
        public int SelectedProductsCount => ProductFilterItems.Count(item => item.IsSelected);
        public int SelectedProcessesCount => ProcessFilterItems.Count(item => item.IsSelected);
        public int SelectedGrowersCount => GrowerFilterItems.Count(item => item.IsSelected);
        public int SelectedPayGroupsCount => PayGroupFilterItems.Count(item => item.IsSelected);

        public string SelectedProductsSummary => $"{SelectedProductsCount} of {ProductFilterItems.Count} selected";
        public string SelectedProcessesSummary => $"{SelectedProcessesCount} of {ProcessFilterItems.Count} selected";
        public string SelectedGrowersSummary => $"{SelectedGrowersCount} of {GrowerFilterItems.Count} selected";
        public string SelectedPayGroupsSummary => $"{SelectedPayGroupsCount} of {PayGroupFilterItems.Count} selected";



        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    // Ensure all commands update their CanExecute status
                    ((RelayCommand)StartPaymentRunCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)TestRunCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ViewBoldReportCommand).RaiseCanExecuteChanged(); // Add new command here
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage ?? string.Empty;
            private set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> RunLog
        {
            get => _runLog ?? new ObservableCollection<string>();
            private set => SetProperty(ref _runLog, value);
        }

        // Property to hold selected Run Log items (used by the View)
        public List<string> SelectedRunLogItems { get; set; } = new List<string>();

        public PaymentBatch? LastRunBatch
        {
            get => _lastRunBatch;
            private set => SetProperty(ref _lastRunBatch, value);
        }

        public List<string> LastRunErrors
        {
            get => _lastRunErrors ?? new List<string>();
            private set => SetProperty(ref _lastRunErrors, value);
        }

        public TestRunResult? LatestTestRunResult
        {
            get => _latestTestRunResult;
            private set => SetProperty(ref _latestTestRunResult, value);
        }

        // Phase 1 - Enhanced properties
        public PaymentBatch? CurrentBatch
        {
            get => _currentBatch;
            private set => SetProperty(ref _currentBatch, value);
        }

        public ObservableCollection<Cheque> GeneratedCheques
        {
            get => _generatedCheques ??= new ObservableCollection<Cheque>();
            private set => SetProperty(ref _generatedCheques, value);
        }

        public bool ChequesGenerated
        {
            get => _chequesGenerated;
            private set => SetProperty(ref _chequesGenerated, value);
        }

        public bool ChequesPrinted
        {
            get => _chequesPrinted;
            private set => SetProperty(ref _chequesPrinted, value);
        }

        public bool StatementsGenerated
        {
            get => _statementsGenerated;
            private set => SetProperty(ref _statementsGenerated, value);
        }

        // Can Execute properties for commands
        public bool CanPostBatch => LatestTestRunResult != null && CurrentBatch == null && !IsRunning;
        public bool CanGenerateCheques => CurrentBatch != null && !ChequesGenerated && !IsRunning;
        public bool CanPrintCheques => ChequesGenerated && !IsRunning;
        public bool CanPrintStatements => CurrentBatch != null && !IsRunning;


        // Commands - Basic Operations
        public ICommand LoadFiltersCommand => new RelayCommand(async o => await LoadFiltersAsync());
        public ICommand StartPaymentRunCommand => new RelayCommand(async o => await StartPaymentRunAsync(), o => !IsRunning);
        public ICommand TestRunCommand => new RelayCommand(async o => await PerformTestRunAsync(), o => !IsRunning); // Shows Dialog Report
        public ICommand ViewBoldReportCommand => new RelayCommand(async o => await PerformViewBoldReportAsync(), o => !IsRunning); // Shows Bold Report Viewer
        
        // Commands - Phase 1 Enhancements (NEW)
        public ICommand PostBatchCommand => new RelayCommand(async o => await PostBatchAsync(), o => CanPostBatch);
        public ICommand GenerateChequesCommand => new RelayCommand(async o => await GenerateChequesAsync(), o => CanGenerateCheques);
        public ICommand PrintChequesCommand => new RelayCommand(async o => await PrintChequesAsync(), o => CanPrintCheques);
        public ICommand PrintStatementsCommand => new RelayCommand(async o => await PrintStatementsAsync(), o => CanPrintStatements);
        public ICommand ViewBatchDetailsCommand => new RelayCommand(async o => await ViewBatchDetailsAsync(), o => CurrentBatch != null);

        // NEW: Enhanced Filter Commands
        public ICommand PreviewCountCommand => new RelayCommand(async o => await PreviewPaymentCountAsync(), o => !IsRunning);
        public ICommand SavePresetCommand => new RelayCommand(async o => await SaveFilterPresetAsync(), o => !IsRunning);
        public ICommand LoadPresetCommand => new RelayCommand(async o => await LoadFilterPresetAsync(), o => !IsRunning && SelectedPreset != null);
        public ICommand DeletePresetCommand => new RelayCommand(async o => await DeleteFilterPresetAsync(), o => !IsRunning && SelectedPreset != null);
        public ICommand ToggleAdvancedOptionsCommand => new RelayCommand(o => ToggleAdvancedOptions());

        // NEW: Checkbox-based Filter Commands
        public ICommand SelectAllProductsCommand => new RelayCommand(o => SelectAllProducts());
        public ICommand ClearAllProductsCommand => new RelayCommand(o => ClearAllProducts());
        public ICommand SelectAllProcessesCommand => new RelayCommand(o => SelectAllProcesses());
        public ICommand ClearAllProcessesCommand => new RelayCommand(o => ClearAllProcesses());
        public ICommand SelectAllGrowersCommand => new RelayCommand(o => SelectAllGrowers());
        public ICommand ClearAllGrowersCommand => new RelayCommand(o => ClearAllGrowers());
        public ICommand SelectAllPayGroupsCommand => new RelayCommand(o => SelectAllPayGroups());
        public ICommand ClearAllPayGroupsCommand => new RelayCommand(o => ClearAllPayGroups());
        
        // Navigation Commands
        public ICommand NavigateToDashboardCommand => new RelayCommand(NavigateToDashboardExecute);
        public ICommand NavigateToPaymentManagementCommand => new RelayCommand(NavigateToPaymentManagementExecute);
        
        // Run Log Commands
        public ICommand CopyRunLogCommand => new RelayCommand(o => CopyRunLogToClipboard());
        public ICommand CopySelectedRunLogCommand => new RelayCommand(o => CopySelectedRunLogToClipboard());


        // IProgress<string> implementation
        public void Report(string value)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                RunLog.Add($"{DateTime.Now:HH:mm:ss} - {value}");
                if (RunLog.Count > 200) RunLog.RemoveAt(0);
            });
        }

        private async Task StartPaymentRunAsync()
        {
            IsRunning = true;
            RunLog.Clear();
            LastRunErrors.Clear();
            LastRunBatch = null;
            StatusMessage = "Creating payment draft...";
            Report($"Creating Advance {AdvanceNumber} payment draft...");
            Report($"Parameters: PaymentDate={PaymentDate:d}, CutoffDate={CutoffDate:d}, CropYear={CropYear}");

            // Prepare lists of IDs from selected items
            var selectedProductIds = ProductFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProductId).ToList();
            var selectedProcessIds = ProcessFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProcessId).ToList();
            var selectedExcludeGrowerIds = SelectedGrowersCount == GrowerFilterItems.Count ? new List<int>() : 
                GrowerFilterItems.Where(g => !g.IsSelected).Select(g => g.Item.GrowerId).ToList();
            var selectedExcludePayGroupIds = SelectedPayGroupsCount == PayGroupFilterItems.Count ? new List<string>() : 
                PayGroupFilterItems.Where(pg => !pg.IsSelected).Select(pg => pg.Item.GroupCode).ToList();

            // Log selected filters
            Report($"Filtering by Products: {(selectedProductIds.Any() ? string.Join(",", selectedProductIds) : "All")}");
            Report($"Filtering by Processes: {(selectedProcessIds.Any() ? string.Join(",", selectedProcessIds) : "All")}");
            Report($"Excluding Growers: {(selectedExcludeGrowerIds.Any() ? string.Join(",", selectedExcludeGrowerIds) : "None")}");
            Report($"Excluding PayGroups: {(selectedExcludePayGroupIds.Any() ? string.Join(",", selectedExcludePayGroupIds) : "None")}");

            try
            {
                // NEW: Only create draft and calculate preview - with validation
                (bool success, List<string> errors, PaymentBatch? createdBatch, TestRunResult? previewResult, PaymentValidationResult? validationResult) = 
                    await _paymentService.CreatePaymentDraftAsync(
                    AdvanceNumber,
                    PaymentDate,
                    CutoffDate,
                    CropYear,
                    selectedExcludeGrowerIds,
                    selectedExcludePayGroupIds,
                    selectedProductIds,
                    selectedProcessIds,
                    this);

                LastRunBatch = createdBatch;
                LastRunErrors = errors ?? new List<string>();

                // Check if validation issues require user confirmation
                if (validationResult != null && (validationResult.HasErrors || validationResult.HasWarnings))
                {
                    // Build detailed message for user
                    var confirmMessage = BuildValidationConfirmationMessage(validationResult);
                    
                    // Ask user if they want to proceed despite issues
                    var userConfirmed = await _dialogService.ShowConfirmationAsync(
                        confirmMessage,
                        "Payment Validation Issues Detected");
                    
                    if (userConfirmed != true)
                    {
                        StatusMessage = "Payment draft creation cancelled by user due to validation issues.";
                        Report("User cancelled draft creation due to validation issues.");
                        IsRunning = false;
                        return;
                    }
                    
                    // User confirmed - proceed with database writes
                    Report("User confirmed to proceed despite validation issues. Creating draft...");
                    
                    // Ensure previewResult is not null before proceeding
                    if (previewResult == null)
                    {
                        StatusMessage = "Payment draft creation failed - no calculation results.";
                        Report("ERROR: No calculation results available.");
                        IsRunning = false;
                        return;
                    }
                    
                    // Call a new method that skips validation and creates the batch
                    (success, errors, createdBatch, previewResult) = 
                        await _paymentService.CreatePaymentDraftConfirmedAsync(
                            AdvanceNumber, PaymentDate, CutoffDate, CropYear,
                            selectedExcludeGrowerIds, selectedExcludePayGroupIds,
                            selectedProductIds, selectedProcessIds, 
                            previewResult, // Pass existing calculation
                            this);
                    
                    LastRunBatch = createdBatch;
                    LastRunErrors = errors ?? new List<string>();
                }

                if (success && createdBatch != null)
                {
                    StatusMessage = $"Payment draft created successfully: Batch {createdBatch.PaymentBatchId}";
                    Report($"Payment draft created: {createdBatch.BatchNumber}");
                    Report($"Status: {createdBatch.Status} - Ready for review and approval");
                    
                    // Show preview summary
                    if (previewResult != null)
                    {
                        var summary = $"Payment Draft Created Successfully!\n\n" +
                                    $"Batch Number: {createdBatch.BatchNumber}\n" +
                                    $"Status: Draft (Ready for Review)\n\n" +
                                    $"Preview Summary:\n" +
                                    $"• Growers: {previewResult.GrowerPayments.Count}\n" +
                                    $"• Total Amount: ${previewResult.GrowerPayments.Sum(gp => gp.TotalCalculatedPayment):N2}\n" +
                                    $"• Receipts: {previewResult.GrowerPayments.Sum(gp => gp.ReceiptCount)}\n\n" +
                                    $"Next Steps:\n" +
                                    $"1. Go to Payment Batches to review the draft\n" +
                                    $"2. Approve the batch to post it\n" +
                                    $"3. Process payments to finalize";
                        
                        await _dialogService.ShowMessageBoxAsync(summary, "Draft Created");
                    }
                }
                else
                {
                    StatusMessage = $"Payment run completed with errors for Batch {createdBatch?.PaymentBatchId}. Check log.";
                    Report("Payment run finished with errors:");
                    foreach(var error in LastRunErrors)
                    {
                        Report($"ERROR: {error}");
                    }
                     await _dialogService.ShowMessageBoxAsync($"The payment run encountered errors. Please review the run log.\nBatch ID: {createdBatch?.PaymentBatchId}", "Payment Run Errors"); // Use async
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Payment run failed with a critical error.";
                Report($"CRITICAL ERROR: {ex.Message}");
                Logger.Error($"Critical error during payment run execution", ex);
                await _dialogService.ShowMessageBoxAsync($"A critical error occurred: {ex.Message}", "Payment Run Failed"); // Use async
            }
            finally
            {
                IsRunning = false;
            }
        }

        // Method to run test and show results in Dialog
        private async Task PerformTestRunAsync()
        {
            IsRunning = true;
            RunLog.Clear();
            // Don't clear LastRunErrors/LastRunBatch as they belong to the actual run
            LatestTestRunResult = null; // Clear previous test result
            StatusMessage = "Starting test run simulation...";
            Report($"Initiating Advance {AdvanceNumber} test run simulation...");
            Report($"Parameters: PaymentDate={PaymentDate:d}, CutoffDate={CutoffDate:d}, CropYear={CropYear}");

            // Prepare lists of IDs from selected items (same as actual run)
            var selectedProductIds = ProductFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProductId).ToList();
            var selectedProcessIds = ProcessFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProcessId).ToList();
            var selectedExcludeGrowerIds = SelectedGrowersCount == GrowerFilterItems.Count ? new List<int>() : 
                GrowerFilterItems.Where(g => !g.IsSelected).Select(g => g.Item.GrowerId).ToList();
            var selectedExcludePayGroupIds = SelectedPayGroupsCount == PayGroupFilterItems.Count ? new List<string>() : 
                PayGroupFilterItems.Where(pg => !pg.IsSelected).Select(pg => pg.Item.GroupCode).ToList();

            // Log selected filters
            Report($"Filtering by Products: {(selectedProductIds.Any() ? string.Join(",", selectedProductIds) : "All")}");
            Report($"Filtering by Processes: {(selectedProcessIds.Any() ? string.Join(",", selectedProcessIds) : "All")}");
            Report($"Excluding Growers: {(selectedExcludeGrowerIds.Any() ? string.Join(",", selectedExcludeGrowerIds) : "None")}");
            Report($"Excluding PayGroups: {(selectedExcludePayGroupIds.Any() ? string.Join(",", selectedExcludePayGroupIds) : "None")}");

            // Prepare parameters object including descriptions
            var parameters = new TestRunInputParameters
            {
                AdvanceNumber = AdvanceNumber,
                PaymentDate = PaymentDate,
                CutoffDate = CutoffDate,
                CropYear = CropYear,
                ExcludeGrowerIds = selectedExcludeGrowerIds,
                ExcludePayGroupIds = selectedExcludePayGroupIds,
                ProductIds = selectedProductIds,
                ProcessIds = selectedProcessIds,
                // Populate descriptions
                ProductDescriptions = SelectedProducts.Select(p => $"{p.ProductId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ProcessDescriptions = SelectedProcesses.Select(p => $"{p.ProcessId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ExcludedGrowerDescriptions = SelectedExcludeGrowers.Select(g => $"{g.GrowerNumber} - {g.Name}".TrimStart(' ', '-')).ToList(),
                ExcludedPayGroupDescriptions = SelectedExcludePayGroups.Select(pg => $"{pg.GroupCode} - {pg.Description}".TrimStart(' ', '-')).ToList()
            };


            try
            {
                // Call the service method with the parameters object
                var testResult = await _paymentService.PerformAdvancePaymentTestRunAsync(
                    parameters.AdvanceNumber,
                    parameters.PaymentDate,
                    parameters.CutoffDate,
                    parameters.CropYear,
                    parameters.ExcludeGrowerIds,
                    parameters.ExcludePayGroupIds,
                    parameters.ProductIds,
                    parameters.ProcessIds,
                    this); // Pass progress reporter

                // Store the result which now includes the input parameters with descriptions
                LatestTestRunResult = testResult;
                // Update the parameters in the result object just in case the service didn't preserve the exact instance
                // (though our current service implementation does)
                if (LatestTestRunResult != null) LatestTestRunResult.InputParameters = parameters;

                if (testResult.HasAnyErrors)
                {
                    StatusMessage = "Test run simulation completed with calculation errors. Check log.";
                    Report("Test run simulation finished with errors:");
                    // Log general errors
                    foreach(var error in testResult.GeneralErrors)
                    {
                        Report($"ERROR: {error}");
                    }
                    // Log grower/receipt specific errors
                    foreach(var gp in testResult.GrowerPayments.Where(g => g.HasErrors))
                    {
                         foreach(var errorMsg in gp.ErrorMessages)
                         {
                             Report($"ERROR (Grower {gp.GrowerNumber}): {errorMsg}");
                         }
                    }
                    await _dialogService.ShowMessageBoxAsync("The test run simulation encountered errors during calculation. Please review the run log.", "Test Run Errors");
                }

                if (!testResult.GrowerPayments.Any())
                {
                     StatusMessage = "Test run simulation completed. No eligible growers/receipts found.";
                     Report("Test run simulation finished: No eligible growers/receipts found.");
                     await _dialogService.ShowMessageBoxAsync("Test run simulation completed, but no eligible growers or receipts were found based on the selected criteria.", "Test Run Complete - No Results");
                }
                else
                {
                    StatusMessage = $"Test run simulation completed successfully. {testResult.GrowerPayments.Count} growers processed.";
                    Report("Test run simulation finished successfully.");

                    // Show the Report in a dedicated Window instead of a dialog
                    if (LatestTestRunResult != null)
                    {
                        var reportViewModel = new PaymentTestRunReportViewModel(LatestTestRunResult);
                        var reportWindow = new PaymentTestRunReportWindow
                        {
                            DataContext = reportViewModel,
                        };
                        reportWindow.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Test run simulation failed with a critical error.";
                Report($"CRITICAL ERROR during test run: {ex.Message}");
                Logger.Error($"Critical error during test run execution", ex);
                await _dialogService.ShowMessageBoxAsync($"A critical error occurred during the test run simulation: {ex.Message}", "Test Run Failed");
            }
            finally
            {
                IsRunning = false;
            }
        }

        // Method to run test and show results in PDF Viewer Window - REMOVED
        /*
        private async Task PerformViewTestReportAsync()
        {
            // ... (Code removed) ...
        }
        */

        // Method to run test and show results in Bold Report Viewer Window
        private async Task PerformViewBoldReportAsync()
        {
            IsRunning = true;
            RunLog.Clear();
            LatestTestRunResult = null;
            StatusMessage = "Starting test run for report viewer...";
            Report($"Initiating Advance {AdvanceNumber} test run for report viewer...");
            Report($"Parameters: PaymentDate={PaymentDate:d}, CutoffDate={CutoffDate:d}, CropYear={CropYear}");

            // Prepare lists of IDs from selected items (same as actual run)
            var selectedProductIds = ProductFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProductId).ToList();
            var selectedProcessIds = ProcessFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProcessId).ToList();
            var selectedExcludeGrowerIds = SelectedGrowersCount == GrowerFilterItems.Count ? new List<int>() : 
                GrowerFilterItems.Where(g => !g.IsSelected).Select(g => g.Item.GrowerId).ToList();
            var selectedExcludePayGroupIds = SelectedPayGroupsCount == PayGroupFilterItems.Count ? new List<string>() : 
                PayGroupFilterItems.Where(pg => !pg.IsSelected).Select(pg => pg.Item.GroupCode).ToList();

            // Log selected filters
            Report($"Filtering by Products: {(selectedProductIds.Any() ? string.Join(",", selectedProductIds) : "All")}");
            Report($"Filtering by Processes: {(selectedProcessIds.Any() ? string.Join(",", selectedProcessIds) : "All")}");
            Report($"Excluding Growers: {(selectedExcludeGrowerIds.Any() ? string.Join(",", selectedExcludeGrowerIds) : "None")}");
            Report($"Excluding PayGroups: {(selectedExcludePayGroupIds.Any() ? string.Join(",", selectedExcludePayGroupIds) : "None")}");

            // Prepare parameters object including descriptions
            var parameters = new TestRunInputParameters
            {
                AdvanceNumber = AdvanceNumber,
                PaymentDate = PaymentDate,
                CutoffDate = CutoffDate,
                CropYear = CropYear,
                ExcludeGrowerIds = selectedExcludeGrowerIds,
                ExcludePayGroupIds = selectedExcludePayGroupIds,
                ProductIds = selectedProductIds,
                ProcessIds = selectedProcessIds,
                ProductDescriptions = SelectedProducts.Select(p => $"{p.ProductId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ProcessDescriptions = SelectedProcesses.Select(p => $"{p.ProcessId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ExcludedGrowerDescriptions = SelectedExcludeGrowers.Select(g => $"{g.GrowerNumber} - {g.Name}".TrimStart(' ', '-')).ToList(),
                ExcludedPayGroupDescriptions = SelectedExcludePayGroups.Select(pg => $"{pg.GroupCode} - {pg.Description}".TrimStart(' ', '-')).ToList()
            };

            System.IO.Stream? reportStream = null;

            try
            {
                // Call the service method
                var testResult = await _paymentService.PerformAdvancePaymentTestRunAsync(
                    parameters.AdvanceNumber, parameters.PaymentDate, parameters.CutoffDate, parameters.CropYear,
                    parameters.ExcludeGrowerIds, parameters.ExcludePayGroupIds, parameters.ProductIds, parameters.ProcessIds, this);

                LatestTestRunResult = testResult;
                if (LatestTestRunResult != null) LatestTestRunResult.InputParameters = parameters;

                if (testResult.HasAnyErrors)
                {
                    StatusMessage = "Test run completed with calculation errors. Cannot show report viewer.";
                    Report("Test run finished with errors:");
                    foreach(var error in testResult.GeneralErrors) { Report($"ERROR: {error}"); }
                    foreach(var gp in testResult.GrowerPayments.Where(g => g.HasErrors)) {
                         foreach(var errorMsg in gp.ErrorMessages) { Report($"ERROR (Grower {gp.GrowerNumber}): {errorMsg}"); }
                    }
                    await _dialogService.ShowMessageBoxAsync("The test run encountered errors during calculation. Report viewer cannot be shown.", "Test Run Errors");
                }
                else if (!testResult.GrowerPayments.Any())
                {
                     StatusMessage = "Test run completed. No eligible growers/receipts found.";
                     Report("Test run finished: No eligible growers/receipts found.");
                     await _dialogService.ShowMessageBoxAsync("Test run completed, but no eligible growers or receipts were found. Report viewer not shown.", "Test Run Complete - No Results");
                }
                else
                {
                    StatusMessage = $"Test run simulation completed successfully. Preparing report viewer...";
                    Report("Test run simulation finished successfully. Preparing report viewer...");

                    // Create DataTable for the report
                    // Need access to the helper method, temporarily create instance of other VM
                    // TODO: Refactor DataTable creation into a shared service/helper
                    if (LatestTestRunResult != null)
                    {
                        var tempReportViewModel = new PaymentTestRunReportViewModel(LatestTestRunResult);
                        System.Data.DataTable reportDataTable = tempReportViewModel.CreateDataTableFromGrowerPayments(); // Assuming this is made public/internal

                        // Prepare data sources for Bold Reports Viewer
                        // Use the correct ReportDataSource class from BoldReports.Windows
                        var reportDataSource = new ReportDataSource
                        {
                             Name = "GrowerPayments", // MUST match the DataSet name in RDL
                             Value = reportDataTable
                        };
                        var dataSources = new List<ReportDataSource> { reportDataSource };


                        // Prepare parameters for Bold Reports Viewer
                        // Use ReportParameter from BoldReports.Windows namespace
                        var reportParameters = new List<ReportParameter>();
                        reportParameters.Add(new ReportParameter() { Name = "ParamAdvanceNumber", Values = new List<string>() { parameters.AdvanceNumber.ToString() } });
                        reportParameters.Add(new ReportParameter() { Name = "ParamPaymentDate", Values = new List<string>() { parameters.PaymentDate.ToString("d") } }); // Format date
                        reportParameters.Add(new ReportParameter() { Name = "ParamCutoffDate", Values = new List<string>() { parameters.CutoffDate.ToString("d") } }); // Format date
                        reportParameters.Add(new ReportParameter() { Name = "ParamCropYear", Values = new List<string>() { parameters.CropYear.ToString() } });
                        reportParameters.Add(new ReportParameter() { Name = "ParamProductsFilter", Values = new List<string>() { parameters.ProductDescriptions.Any() ? string.Join(", ", parameters.ProductDescriptions) : "All" } });
                        reportParameters.Add(new ReportParameter() { Name = "ParamProcessesFilter", Values = new List<string>() { parameters.ProcessDescriptions.Any() ? string.Join(", ", parameters.ProcessDescriptions) : "All" } });
                        reportParameters.Add(new ReportParameter() { Name = "ParamExcludedGrowersFilter", Values = new List<string>() { parameters.ExcludedGrowerDescriptions.Any() ? string.Join(", ", parameters.ExcludedGrowerDescriptions) : "None" } });
                        reportParameters.Add(new ReportParameter() { Name = "ParamExcludedPayGroupsFilter", Values = new List<string>() { parameters.ExcludedPayGroupDescriptions.Any() ? string.Join(", ", parameters.ExcludedPayGroupDescriptions) : "None" } });


                        // --- Load Report from Stream ---
                        try
                        {
                            // Get the current assembly
                            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                            // Standard resource name (AssemblyName.Folder.File.Extension)
                            string resourceName = "WPFGrowerApp.Reports.PaymentTestRunReport.rdlc";
                            reportStream = assembly.GetManifestResourceStream(resourceName);

                            if (reportStream == null)
                            {
                                // Attempt with just Folder.File.Extension (sometimes works if default namespace differs unexpectedly)
                                resourceName = "Reports.PaymentTestRunReport.rdlc";
                                reportStream = assembly.GetManifestResourceStream(resourceName);
                            }

                            if (reportStream == null)
                            {
                                throw new Exception($"Embedded report resource not found. Tried paths: WPFGrowerApp.Reports.PaymentTestRunReport.rdlc and Reports.PaymentTestRunReport.rdlc");
                            }
                        }
                        catch (Exception streamEx)
                        {
                             Report($"CRITICAL ERROR: Failed to load embedded report stream: {streamEx.Message}");
                             await _dialogService.ShowMessageBoxAsync($"Could not load the embedded report resource: {streamEx.Message}", "Report Load Error");
                             IsRunning = false; // Ensure IsRunning is reset
                             return; // Stop execution
                        }
                        // --- End Load Report from Stream ---


                        if (reportStream != null)
                        {
                            // Create the ViewModel for the viewer window, passing the stream, data sources, and parameters
                            var viewerViewModel = new BoldReportViewerViewModel(reportStream, dataSources, reportParameters);
                            viewerViewModel.ReportTitle = $"Payment Test Run - {DateTime.Now:g}";

                            // Show the Bold Report Viewer Window
                            var reportViewerWindow = new Views.BoldReportViewerWindow();
                            reportViewerWindow.DataContext = viewerViewModel;
                            reportViewerWindow.Show(); // Show as a non-modal window

                            StatusMessage = "Report viewer displayed.";
                            Report("Report viewer displayed.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Test run for report viewer failed with a critical error.";
                Report($"CRITICAL ERROR during report viewer test run: {ex.Message}");
                Logger.Error($"Critical error during report viewer test run execution", ex);
                await _dialogService.ShowMessageBoxAsync($"A critical error occurred: {ex.Message}", "Test Run Failed");
            }
            finally
            {
                IsRunning = false;
            }
        }


        // Method to load ListBox data sources
        private async Task LoadFiltersAsync()
        {
            try
            {
                // Load Products into the backing collection
                var productList = await _productService.GetAllProductsAsync();
                 _allProducts.Clear();
                 foreach (var p in productList) _allProducts.Add(p);
                 _filteredProductsView?.Refresh(); // Refresh the view

                // Load Processes into the backing collection
                var processList = await _processService.GetAllProcessesAsync();
                 _allProcesses.Clear();
                 foreach (var p in processList) _allProcesses.Add(p);
                 _filteredProcessesView?.Refresh(); // Refresh the view

                // Load PayGroups into the backing collection
                var payGroupList = await _payGroupService.GetAllPayGroupsAsync();
                 _allPayGroups.Clear();
                 foreach (var pg in payGroupList) _allPayGroups.Add(pg);
                 _filteredPayGroupsView?.Refresh(); // Refresh the view

                // Load Growers into the backing collection
                var growerList = await _growerService.GetAllGrowersBasicInfoAsync();
                 _allGrowers.Clear();
                 foreach (var g in growerList) _allGrowers.Add(g);
                 _filteredGrowersView?.Refresh(); // Refresh the view

                // Initialize the new FilterItem collections for checkbox-based selection
                InitializeFilterItems();
                
                // Subscribe to PropertyChanged events to keep legacy collections in sync
                SubscribeToFilterItemChanges();

                // Ensure UI gets updated after a small delay to allow all data to be processed
                await Task.Delay(100);
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(SelectedProductsCount));
                    OnPropertyChanged(nameof(SelectedProcessesCount));
                    OnPropertyChanged(nameof(SelectedGrowersCount));
                    OnPropertyChanged(nameof(SelectedPayGroupsCount));
                    OnPropertyChanged(nameof(SelectedProductsSummary));
                    OnPropertyChanged(nameof(SelectedProcessesSummary));
                    OnPropertyChanged(nameof(SelectedGrowersSummary));
                    OnPropertyChanged(nameof(SelectedPayGroupsSummary));
                });

            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filter data for Payment Run", ex);
                await _dialogService.ShowMessageBoxAsync("Error loading filter options.", "Load Error"); // Use async
            }
        }

         // Method to update the On Hold Growers list based on current filters
        private async Task UpdateOnHoldGrowersAsync()
        {
            IsLoadingOnHoldGrowers = true;

            try
            {
                var onHold = await _growerService.GetOnHoldGrowersAsync();

                // Apply client-side filtering based on Exclude options if needed
                var excludedGrowerIds = SelectedGrowersCount == GrowerFilterItems.Count ? new List<int>() : 
                    GrowerFilterItems.Where(g => !g.IsSelected).Select(g => g.Item.GrowerId).ToList();
                if (excludedGrowerIds.Any())
                {
                    onHold = onHold.Where(g => !excludedGrowerIds.Contains(g.GrowerId)).ToList();
                }
                // TODO: Add filtering for Products, Processes, PayGroups if required by business logic

                OnHoldGrowers.Clear();
                if (onHold != null)
                {
                    foreach(var g in onHold)
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

        // Handler for selection changes in filter lists
        private void SelectedFilters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Re-calculate the OnHoldGrowers list whenever a filter selection changes
           // _ = UpdateOnHoldGrowersAsync();
        }

        // ==============================================================
        // PHASE 1 - NEW COMMAND IMPLEMENTATIONS
        // ==============================================================

        /// <summary>
        /// Post the payment batch (create batch, generate cheques, post to accounts)
        /// </summary>
        private async Task PostBatchAsync()
        {
            try
            {
                if (LatestTestRunResult == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please run a test first before posting.", "Test Required");
                    return;
                }

                // Confirm with user
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will:\n" +
                    $"• Create payment batch\n" +
                    $"• Generate {LatestTestRunResult.GrowerPayments.Count} cheques\n" +
                    $"• Post ${LatestTestRunResult.GrowerPayments.Sum(gp => gp.TotalCalculatedPayment):N2} to accounts\n\n" +
                    $"Continue?",
                    $"Post Advance {AdvanceNumber} Payment Batch?");

                if (confirm != true)
                    return;

                IsRunning = true;
                StatusMessage = "Posting payment batch...";
                Report("Starting batch posting...");

                try
                {
                    // 1. Create payment batch
                    Report("Creating payment batch...");
                    var paymentTypeId = AdvanceNumber; // Assuming PaymentTypeId matches advance number
                    CurrentBatch = await _batchManagementService.CreatePaymentBatchAsync(
                        paymentTypeId: paymentTypeId,
                        batchDate: PaymentDate,
                        cropYear: CropYear,
                        cutoffDate: CutoffDate,
                        notes: $"Advance {AdvanceNumber} - {LatestTestRunResult.GrowerPayments.Count} growers",
                        createdBy: App.CurrentUser?.Username);

                    Report($"✓ Created batch: {CurrentBatch.BatchNumber}");

                    // 2. Run the actual payment processing (posts to accounts, creates allocations)
                    Report("Processing payments to accounts...");
                    
                    var selectedProductIds = ProductFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProductId).ToList();
                    var selectedProcessIds = ProcessFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProcessId).ToList();
                    var selectedExcludeGrowerIds = SelectedGrowersCount == GrowerFilterItems.Count ? new List<int>() : 
                        GrowerFilterItems.Where(g => !g.IsSelected).Select(g => g.Item.GrowerId).ToList();
                    var selectedExcludePayGroupIds = SelectedPayGroupsCount == PayGroupFilterItems.Count ? new List<string>() : 
                        PayGroupFilterItems.Where(pg => !pg.IsSelected).Select(pg => pg.Item.GroupCode).ToList();

                    var (success, errors, postedBatch) = await _paymentService.ProcessAdvancePaymentRunAsync(
                        AdvanceNumber,
                        PaymentDate,
                        CutoffDate,
                        CropYear,
                        selectedExcludeGrowerIds,
                        selectedExcludePayGroupIds,
                        selectedProductIds,
                        selectedProcessIds,
                        this);

                    if (!success || errors.Any())
                    {
                        Report("⚠ Payment processing completed with errors");
                        foreach (var error in errors)
                        {
                            Report($"  ERROR: {error}");
                        }
                        
                        await _dialogService.ShowMessageBoxAsync(
                            $"Payment processing completed with {errors.Count} error(s). Check the run log for details.",
                            "Posting Completed with Errors");
                    }
                    else
                    {
                        Report("✓ Successfully posted payments to accounts");
                        StatusMessage = $"Batch {CurrentBatch.BatchNumber} posted successfully";
                        
                        await _dialogService.ShowMessageBoxAsync(
                            $"Payment batch posted successfully!\n\n" +
                            $"Batch: {CurrentBatch.BatchNumber}\n" +
                            $"Growers: {LatestTestRunResult.GrowerPayments.Count}\n" +
                            $"Total Amount: ${LatestTestRunResult.GrowerPayments.Sum(gp => gp.TotalCalculatedPayment):N2}",
                            "Batch Posted");
                    }
                }
                catch (Exception ex)
                {
                    Report($"✗ ERROR: {ex.Message}");
                    Logger.Error("Error posting batch", ex);
                    await _dialogService.ShowMessageBoxAsync($"Error posting batch: {ex.Message}", "Error");
                    throw;
                }
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Generate cheques for the current batch
        /// </summary>
        private async Task GenerateChequesAsync()
        {
            try
            {
                if (CurrentBatch == null || LatestTestRunResult == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch available. Please post a batch first.", "No Batch");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Generating cheques...";
                Report("Generating cheques for batch...");

                // Convert test result to GrowerPaymentAmount list
                var growerPayments = LatestTestRunResult.GrowerPayments
                    .Select(gp => new GrowerPaymentAmount
                    {
                        GrowerId = int.TryParse(gp.GrowerNumber, out var id) ? id : 0,
                        GrowerName = gp.GrowerName,
                        PaymentAmount = gp.TotalCalculatedPayment,
                        IsOnHold = gp.IsOnHold
                    }).ToList();

                // Generate cheques
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
                StatusMessage = $"Generated {cheques.Count} cheques";

                await _dialogService.ShowMessageBoxAsync(
                    $"Successfully generated {cheques.Count} cheques!\n\n" +
                    $"Cheque range: {cheques.Min(c => c.ChequeNumber)} - {cheques.Max(c => c.ChequeNumber)}",
                    "Cheques Generated");
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error generating cheques", ex);
                await _dialogService.ShowMessageBoxAsync($"Error generating cheques: {ex.Message}", "Error");
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
                    await _dialogService.ShowMessageBoxAsync("No cheques to print. Please generate cheques first.", "No Cheques");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Printing cheques...";
                Report($"Printing {GeneratedCheques.Count} cheques...");

                // Print all cheques
                var printedCount = await _chequePrintingService.PrintChequesAsync(GeneratedCheques.ToList());

                if (printedCount > 0)
                {
                    ChequesPrinted = true;
                    Report($"✓ Printed {printedCount} cheques");
                    StatusMessage = $"Printed {printedCount} cheques successfully";

                    await _dialogService.ShowMessageBoxAsync(
                        $"Successfully printed {printedCount} cheques!",
                        "Printing Complete");
                }
                else
                {
                    Report("Printing cancelled by user");
                }
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error printing cheques", ex);
                await _dialogService.ShowMessageBoxAsync($"Error printing cheques: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Print advance statements for all growers in batch
        /// </summary>
        private async Task PrintStatementsAsync()
        {
            try
            {
                if (CurrentBatch == null || LatestTestRunResult == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch available to print statements for.", "No Batch");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Printing statements...";
                Report($"Printing statements for {LatestTestRunResult.GrowerPayments.Count} growers...");

                // Print statements
                var printedCount = await _statementPrintingService.PrintBatchStatementsAsync(
                    LatestTestRunResult.GrowerPayments,
                    AdvanceNumber,
                    CurrentBatch.PaymentBatchId);

                if (printedCount > 0)
                {
                    StatementsGenerated = true;
                    Report($"✓ Printed {printedCount} statements");
                    StatusMessage = $"Printed {printedCount} statements successfully";

                    await _dialogService.ShowMessageBoxAsync(
                        $"Successfully printed {printedCount} advance statements!",
                        "Printing Complete");
                }
                else
                {
                    Report("Statement printing cancelled by user");
                }
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error printing statements", ex);
                await _dialogService.ShowMessageBoxAsync($"Error printing statements: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// View details of the current payment batch
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

                // Get batch summary
                var summary = await _batchManagementService.GetBatchSummaryAsync(CurrentBatch.PaymentBatchId);

                // Display summary
                var message = $"Batch Details:\n\n" +
                             $"Batch Number: {summary.BatchNumber}\n" +
                             $"Payment Type: {summary.PaymentTypeName}\n" +
                             $"Batch Date: {summary.BatchDate:yyyy-MM-dd}\n" +
                             $"Status: {summary.Status}\n\n" +
                             $"Growers: {summary.TotalGrowers}\n" +
                             $"Receipts: {summary.TotalReceipts}\n" +
                             $"Amount: ${summary.TotalAmount:N2}\n" +
                             $"Cheques Generated: {summary.ChequesGenerated}\n\n" +
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
                await _dialogService.ShowMessageBoxAsync($"Error loading batch details: {ex.Message}", "Error");
            }
        }

        #region Enhanced Filter Commands

        /// <summary>
        /// Preview how many receipts will be included in the payment run
        /// </summary>
        private async Task PreviewPaymentCountAsync()
        {
            try
            {
                IsRunning = true;
                StatusMessage = "Calculating payment preview...";
                Report("Running preview calculation...");

                // Get current filter selections
                // For exclude parameters: pass empty list when all items are selected (meaning include all)
                // For include parameters: pass the selected IDs
                var selectedExcludeGrowerIds = SelectedGrowersCount == GrowerFilterItems.Count ? new List<int>() : 
                    GrowerFilterItems.Where(g => !g.IsSelected).Select(g => g.Item.GrowerId).ToList();
                var selectedExcludePayGroupIds = SelectedPayGroupsCount == PayGroupFilterItems.Count ? new List<string>() : 
                    PayGroupFilterItems.Where(pg => !pg.IsSelected).Select(pg => pg.Item.GroupCode).ToList();
                var selectedProductIds = ProductFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProductId).ToList();
                var selectedProcessIds = ProcessFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProcessId).ToList();

                // Run test calculation to get preview data
                var testResult = await _paymentService.PerformAdvancePaymentTestRunAsync(
                    AdvanceNumber,
                    PaymentDate,
                    CutoffDate,
                    CropYear,
                    selectedExcludeGrowerIds,
                    selectedExcludePayGroupIds,
                    selectedProductIds,
                    selectedProcessIds,
                    this);

                // Update preview properties
                PreviewReceiptCount = testResult.GrowerPayments.Sum(gp => gp.ReceiptCount);
                PreviewGrowerCount = testResult.GrowerPayments.Count;
                PreviewEstimatedTotal = testResult.GrowerPayments.Sum(gp => gp.TotalCalculatedPayment);
                PreviewSkippedCount = testResult.GrowerPayments.Sum(gp => gp.ReceiptDetails.Count(rd => !string.IsNullOrEmpty(rd.ErrorMessage)));

                // Update summary text
                PreviewSummaryText = $"Receipts: {PreviewReceiptCount} | Growers: {PreviewGrowerCount} | Est. Total: ${PreviewEstimatedTotal:N2}";
                if (PreviewSkippedCount > 0)
                {
                    PreviewSummaryText += $" | Skipped: {PreviewSkippedCount}";
                }

                Report($"✓ Preview complete: {PreviewReceiptCount} receipts, {PreviewGrowerCount} growers, ${PreviewEstimatedTotal:N2}");
                StatusMessage = $"Preview: {PreviewReceiptCount} receipts, ${PreviewEstimatedTotal:N2}";

                await _dialogService.ShowMessageBoxAsync(
                    $"Payment Preview:\n\n" +
                    $"Receipts: {PreviewReceiptCount}\n" +
                    $"Growers: {PreviewGrowerCount}\n" +
                    $"Estimated Total: ${PreviewEstimatedTotal:N2}\n" +
                    $"Skipped (no price): {PreviewSkippedCount}",
                    "Payment Preview");
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error in preview calculation", ex);
                await _dialogService.ShowMessageBoxAsync($"Error calculating preview: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Save current filter settings as a preset
        /// </summary>
        private async Task SaveFilterPresetAsync()
        {
            try
            {
                // Prompt user for preset name
                var presetName = await _dialogService.ShowInputDialogAsync(
                    "Enter a name for this filter preset:",
                    "Save Filter Preset",
                    null,
                    "e.g., Weekly Blueberry Payment");

                if (string.IsNullOrWhiteSpace(presetName))
                    return;

                // Create preset from current settings
                var preset = new FilterPreset
                {
                    Name = presetName.Trim(),
                    Description = $"Saved on {DateTime.Now:yyyy-MM-dd HH:mm}",
                    CreatedBy = "Current User", // TODO: Get actual user
                    CreatedAt = DateTime.Now,

                    // Basic Parameters
                    AdvanceNumber = AdvanceNumber,
                    PaymentDate = PaymentDate,
                    CutoffDate = CutoffDate,
                    ReceiptDateFrom = ReceiptDateFrom,
                    CropYear = CropYear,

                    // Enhanced Filters
                    MinimumWeight = MinimumWeight,
                    ShowUnpayableReceipts = ShowUnpayableReceipts,
                    IncludePreviouslyPaid = IncludePreviouslyPaid,

                    // Filter Modes - Removed as radio buttons are no longer used

                    // Process Class Filters
                    IncludeFresh = IncludeFresh,
                    IncludeProcessed = IncludeProcessed,
                    IncludeJuice = IncludeJuice,
                    IncludeOther = IncludeOther,

                    // Grade Filters
                    IncludeGrade1 = IncludeGrade1,
                    IncludeGrade2 = IncludeGrade2,
                    IncludeGrade3 = IncludeGrade3,
                    IncludeOtherGrade = IncludeOtherGrade,

                    // Selected Items
                    SelectedProductIds = ProductFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProductId).ToList(),
                    SelectedProcessIds = ProcessFilterItems.Where(p => p.IsSelected).Select(p => p.Item.ProcessId).ToList(),
                    SelectedGrowerIds = GrowerFilterItems.Where(g => g.IsSelected).Select(g => g.Item.GrowerId).ToList(),
                    SelectedPayGroupIds = PayGroupFilterItems.Where(pg => pg.IsSelected).Select(pg => pg.Item.GroupCode).ToList()
                };

                // Add to collection
                FilterPresets.Add(preset);

                Report($"✓ Saved filter preset: {presetName}");
                StatusMessage = $"Saved preset: {presetName}";

                await _dialogService.ShowMessageBoxAsync(
                    $"Filter preset '{presetName}' saved successfully!",
                    "Preset Saved");
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error saving filter preset", ex);
                await _dialogService.ShowMessageBoxAsync($"Error saving preset: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Load selected filter preset
        /// </summary>
        private async Task LoadFilterPresetAsync()
        {
            try
            {
                if (SelectedPreset == null)
                    return;

                // Confirm loading preset
                var result = await _dialogService.ShowConfirmationDialogAsync(
                    $"Load filter preset '{SelectedPreset.Name}'?\n\nThis will replace your current filter settings.",
                    "Load Preset");

                if (!result)
                    return;

                // Load preset values
                AdvanceNumber = SelectedPreset.AdvanceNumber;
                PaymentDate = SelectedPreset.PaymentDate;
                CutoffDate = SelectedPreset.CutoffDate;
                ReceiptDateFrom = SelectedPreset.ReceiptDateFrom;
                CropYear = SelectedPreset.CropYear;

                // Enhanced Filters
                MinimumWeight = SelectedPreset.MinimumWeight;
                ShowUnpayableReceipts = SelectedPreset.ShowUnpayableReceipts;
                IncludePreviouslyPaid = SelectedPreset.IncludePreviouslyPaid;

                // Filter Modes - Removed as radio buttons are no longer used

                // Process Class Filters
                IncludeFresh = SelectedPreset.IncludeFresh;
                IncludeProcessed = SelectedPreset.IncludeProcessed;
                IncludeJuice = SelectedPreset.IncludeJuice;
                IncludeOther = SelectedPreset.IncludeOther;

                // Grade Filters
                IncludeGrade1 = SelectedPreset.IncludeGrade1;
                IncludeGrade2 = SelectedPreset.IncludeGrade2;
                IncludeGrade3 = SelectedPreset.IncludeGrade3;
                IncludeOtherGrade = SelectedPreset.IncludeOtherGrade;

                // Load selected items (this is more complex as we need to match by ID)
                await LoadPresetSelectedItemsAsync(SelectedPreset);

                Report($"✓ Loaded filter preset: {SelectedPreset.Name}");
                StatusMessage = $"Loaded preset: {SelectedPreset.Name}";

                await _dialogService.ShowMessageBoxAsync(
                    $"Filter preset '{SelectedPreset.Name}' loaded successfully!",
                    "Preset Loaded");
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error loading filter preset", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading preset: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Delete selected filter preset
        /// </summary>
        private async Task DeleteFilterPresetAsync()
        {
            try
            {
                if (SelectedPreset == null)
                    return;

                // Confirm deletion
                var result = await _dialogService.ShowConfirmationDialogAsync(
                    $"Delete filter preset '{SelectedPreset.Name}'?\n\nThis action cannot be undone.",
                    "Delete Preset");

                if (!result)
                    return;

                var presetName = SelectedPreset.Name;
                FilterPresets.Remove(SelectedPreset);
                SelectedPreset = null;

                Report($"✓ Deleted filter preset: {presetName}");
                StatusMessage = $"Deleted preset: {presetName}";

                await _dialogService.ShowMessageBoxAsync(
                    $"Filter preset '{presetName}' deleted successfully!",
                    "Preset Deleted");
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error deleting filter preset", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting preset: {ex.Message}", "Error");
            }
        }

        /// <summary>
        /// Toggle the advanced options visibility
        /// </summary>
        private void ToggleAdvancedOptions()
        {
            ShowAdvancedOptions = !ShowAdvancedOptions;
        }

        /// <summary>
        /// Helper method to load selected items from a preset
        /// </summary>
        private Task LoadPresetSelectedItemsAsync(FilterPreset preset)
        {
            // Clear current selections
            SelectedProducts.Clear();
            SelectedProcesses.Clear();
            SelectedExcludeGrowers.Clear();
            SelectedExcludePayGroups.Clear();

            // Load products
            foreach (var productId in preset.SelectedProductIds)
            {
                var product = _allProducts.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                    SelectedProducts.Add(product);
            }

            // Load processes
            foreach (var processId in preset.SelectedProcessIds)
            {
                var process = _allProcesses.FirstOrDefault(p => p.ProcessId == processId);
                if (process != null)
                    SelectedProcesses.Add(process);
            }

            // Load growers
            foreach (var growerId in preset.SelectedGrowerIds)
            {
                var grower = _allGrowers.FirstOrDefault(g => (int)g.GrowerNumber == growerId);
                if (grower != null)
                    SelectedExcludeGrowers.Add(grower);
            }

            // Load pay groups
            foreach (var payGroupId in preset.SelectedPayGroupIds)
            {
                var payGroup = _allPayGroups.FirstOrDefault(pg => pg.GroupCode == payGroupId);
                if (payGroup != null)
                    SelectedExcludePayGroups.Add(payGroup);
            }
            
            return Task.CompletedTask;
        }

        #endregion

        #region Checkbox-based Filter Commands

        /// <summary>
        /// Select all products
        /// </summary>
        private void SelectAllProducts()
        {
            foreach (var item in ProductFilterItems)
            {
                item.IsSelected = true;
            }
            UpdateSelectedProductsFromFilterItems();
            OnPropertyChanged(nameof(SelectedProductsCount));
            OnPropertyChanged(nameof(SelectedProductsSummary));
        }

        /// <summary>
        /// Clear all product selections
        /// </summary>
        private void ClearAllProducts()
        {
            foreach (var item in ProductFilterItems)
            {
                item.IsSelected = false;
            }
            UpdateSelectedProductsFromFilterItems();
            OnPropertyChanged(nameof(SelectedProductsCount));
            OnPropertyChanged(nameof(SelectedProductsSummary));
        }

        /// <summary>
        /// Select all processes
        /// </summary>
        private void SelectAllProcesses()
        {
            foreach (var item in ProcessFilterItems)
            {
                item.IsSelected = true;
            }
            UpdateSelectedProcessesFromFilterItems();
            OnPropertyChanged(nameof(SelectedProcessesCount));
            OnPropertyChanged(nameof(SelectedProcessesSummary));
        }

        /// <summary>
        /// Clear all process selections
        /// </summary>
        private void ClearAllProcesses()
        {
            foreach (var item in ProcessFilterItems)
            {
                item.IsSelected = false;
            }
            UpdateSelectedProcessesFromFilterItems();
            OnPropertyChanged(nameof(SelectedProcessesCount));
            OnPropertyChanged(nameof(SelectedProcessesSummary));
        }

        /// <summary>
        /// Select all growers
        /// </summary>
        private void SelectAllGrowers()
        {
            foreach (var item in GrowerFilterItems)
            {
                item.IsSelected = true;
            }
            UpdateSelectedGrowersFromFilterItems();
            OnPropertyChanged(nameof(SelectedGrowersCount));
            OnPropertyChanged(nameof(SelectedGrowersSummary));
        }

        /// <summary>
        /// Clear all grower selections
        /// </summary>
        private void ClearAllGrowers()
        {
            foreach (var item in GrowerFilterItems)
            {
                item.IsSelected = false;
            }
            UpdateSelectedGrowersFromFilterItems();
            OnPropertyChanged(nameof(SelectedGrowersCount));
            OnPropertyChanged(nameof(SelectedGrowersSummary));
        }

        /// <summary>
        /// Select all pay groups
        /// </summary>
        private void SelectAllPayGroups()
        {
            foreach (var item in PayGroupFilterItems)
            {
                item.IsSelected = true;
            }
            UpdateSelectedPayGroupsFromFilterItems();
            OnPropertyChanged(nameof(SelectedPayGroupsCount));
            OnPropertyChanged(nameof(SelectedPayGroupsSummary));
        }

        /// <summary>
        /// Clear all pay group selections
        /// </summary>
        private void ClearAllPayGroups()
        {
            foreach (var item in PayGroupFilterItems)
            {
                item.IsSelected = false;
            }
            UpdateSelectedPayGroupsFromFilterItems();
            OnPropertyChanged(nameof(SelectedPayGroupsCount));
            OnPropertyChanged(nameof(SelectedPayGroupsSummary));
        }

        /// <summary>
        /// Copy all Run Log entries to clipboard
        /// </summary>
        private void CopyRunLogToClipboard()
        {
            try
            {
                if (RunLog?.Count > 0)
                {
                    string logText = string.Join(Environment.NewLine, RunLog);
                    System.Windows.Clipboard.SetText(logText);
                    StatusMessage = $"Copied {RunLog.Count} log entries to clipboard";
                }
                else
                {
                    StatusMessage = "No log entries to copy";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to copy log entries: {ex.Message}";
            }
        }

        /// <summary>
        /// Copy selected Run Log entries to clipboard
        /// </summary>
        private void CopySelectedRunLogToClipboard()
        {
            try
            {
                var selectedItems = SelectedRunLogItems;
                if (selectedItems?.Count > 0)
                {
                    string logText = string.Join(Environment.NewLine, selectedItems);
                    System.Windows.Clipboard.SetText(logText);
                    StatusMessage = $"Copied {selectedItems.Count} selected log entries to clipboard";
                }
                else
                {
                    StatusMessage = "No log entries selected to copy";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to copy selected log entries: {ex.Message}";
            }
        }


        #endregion

        #region Helper Methods for FilterItem Synchronization

        /// <summary>
        /// Update SelectedProducts collection from ProductFilterItems
        /// </summary>
        private void UpdateSelectedProductsFromFilterItems()
        {
            SelectedProducts.Clear();
            foreach (var item in ProductFilterItems.Where(fi => fi.IsSelected))
            {
                SelectedProducts.Add(item.Item);
            }
        }

        /// <summary>
        /// Update SelectedProcesses collection from ProcessFilterItems
        /// </summary>
        private void UpdateSelectedProcessesFromFilterItems()
        {
            SelectedProcesses.Clear();
            foreach (var item in ProcessFilterItems.Where(fi => fi.IsSelected))
            {
                SelectedProcesses.Add(item.Item);
            }
        }

        /// <summary>
        /// Update SelectedExcludeGrowers collection from GrowerFilterItems
        /// </summary>
        private void UpdateSelectedGrowersFromFilterItems()
        {
            SelectedExcludeGrowers.Clear();
            foreach (var item in GrowerFilterItems.Where(fi => fi.IsSelected))
            {
                SelectedExcludeGrowers.Add(item.Item);
            }
        }

        /// <summary>
        /// Update SelectedExcludePayGroups collection from PayGroupFilterItems
        /// </summary>
        private void UpdateSelectedPayGroupsFromFilterItems()
        {
            SelectedExcludePayGroups.Clear();
            foreach (var item in PayGroupFilterItems.Where(fi => fi.IsSelected))
            {
                SelectedExcludePayGroups.Add(item.Item);
            }
        }

        /// <summary>
        /// Initialize FilterItem collections from the original collections
        /// All items are selected by default on page load
        /// </summary>
        private void InitializeFilterItems()
        {
            // Initialize Product Filter Items - Select all by default
            ProductFilterItems.Clear();
            foreach (var product in _allProducts)
            {
                var displayText = $"{product.ProductId} - {product.Description}";
                ProductFilterItems.Add(new FilterItem<Product>(product, displayText, true)); // Always selected by default
            }

            // Initialize Process Filter Items - Select all by default
            ProcessFilterItems.Clear();
            foreach (var process in _allProcesses)
            {
                var displayText = $"{process.ProcessId} - {process.Description}";
                ProcessFilterItems.Add(new FilterItem<Process>(process, displayText, true)); // Always selected by default
            }

            // Initialize Grower Filter Items - Select all by default
            GrowerFilterItems.Clear();
            foreach (var grower in _allGrowers)
            {
                var displayText = $"{grower.GrowerNumber} - {grower.Name}";
                GrowerFilterItems.Add(new FilterItem<GrowerInfo>(grower, displayText, true)); // Always selected by default
            }

            // Initialize PayGroup Filter Items - Select all by default
            PayGroupFilterItems.Clear();
            foreach (var payGroup in _allPayGroups)
            {
                var displayText = $"{payGroup.GroupCode} - {payGroup.Description}";
                PayGroupFilterItems.Add(new FilterItem<PayGroup>(payGroup, displayText, true)); // Always selected by default
            }

            // Update the legacy collections to reflect all items being selected
            UpdateSelectedProductsFromFilterItems();
            UpdateSelectedProcessesFromFilterItems();
            UpdateSelectedGrowersFromFilterItems();
            UpdateSelectedPayGroupsFromFilterItems();

        }

        #endregion

        #region Event Subscription for FilterItem Changes

        /// <summary>
        /// Subscribe to PropertyChanged events on FilterItems to keep legacy collections in sync
        /// </summary>
        private void SubscribeToFilterItemChanges()
        {
            foreach (var item in ProductFilterItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FilterItem<Product>.IsSelected))
                    {
                        UpdateSelectedProductsFromFilterItems();
                        OnPropertyChanged(nameof(SelectedProductsCount));
                        OnPropertyChanged(nameof(SelectedProductsSummary));
                    }
                };
            }

            foreach (var item in ProcessFilterItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FilterItem<Process>.IsSelected))
                    {
                        UpdateSelectedProcessesFromFilterItems();
                        OnPropertyChanged(nameof(SelectedProcessesCount));
                        OnPropertyChanged(nameof(SelectedProcessesSummary));
                    }
                };
            }

            foreach (var item in GrowerFilterItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FilterItem<GrowerInfo>.IsSelected))
                    {
                        UpdateSelectedGrowersFromFilterItems();
                        OnPropertyChanged(nameof(SelectedGrowersCount));
                        OnPropertyChanged(nameof(SelectedGrowersSummary));
                    }
                };
            }

            foreach (var item in PayGroupFilterItems)
            {
                item.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(FilterItem<PayGroup>.IsSelected))
                    {
                        UpdateSelectedPayGroupsFromFilterItems();
                        OnPropertyChanged(nameof(SelectedPayGroupsCount));
                        OnPropertyChanged(nameof(SelectedPayGroupsSummary));
                    }
                };
            }
        }

        /// <summary>
        /// Builds a detailed validation confirmation message for user review
        /// </summary>
        private string BuildValidationConfirmationMessage(PaymentValidationResult validation)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Payment validation found the following issues:\n");
            
            if (validation.HasErrors)
            {
                sb.AppendLine($"ERRORS ({validation.Errors.Count}):");
                var errorGroups = validation.Errors.GroupBy(e => e.IssueType);
                foreach (var group in errorGroups)
                {
                    sb.AppendLine($"  • {group.Key}: {group.Count()} receipt(s)");
                }
                sb.AppendLine();
            }
            
            if (validation.HasWarnings)
            {
                sb.AppendLine($"WARNINGS ({validation.Warnings.Count}):");
                var warningGroups = validation.Warnings.GroupBy(w => w.IssueType);
                foreach (var group in warningGroups)
                {
                    sb.AppendLine($"  • {group.Key}: {group.Count()} receipt(s)");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine($"Summary:");
            sb.AppendLine($"  • Total Receipts: {validation.TotalReceipts}");
            sb.AppendLine($"  • Valid Receipts: {validation.ValidReceipts}");
            sb.AppendLine($"  • Invalid Receipts: {validation.InvalidReceipts}");
            sb.AppendLine();
            sb.AppendLine("Do you want to proceed with creating the payment draft?");
            sb.AppendLine("(Only valid receipts will be included in the payment.)");
            
            return sb.ToString();
        }

        #endregion

        #region Navigation Methods

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
                StatusMessage = $"Error navigating to Dashboard: {ex.Message}";
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
                StatusMessage = $"Error navigating to Payment Management: {ex.Message}";
            }
        }

        #endregion

    }
}
