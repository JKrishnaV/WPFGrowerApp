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

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Receipt List view
    /// </summary>
    public class ReceiptListViewModel : ViewModelBase, IDisposable
    {
        #region Private Fields

        private readonly IReceiptService _receiptService;
        private readonly IReceiptExportService _exportService;
        private readonly IReceiptValidationService _validationService;
        private readonly IReceiptAnalyticsService _analyticsService;
        private readonly IDialogService _dialogService;
        private readonly IGrowerService _growerService;
        private readonly IProductService _productService;
        private readonly IDepotService _depotService;
        private readonly IProcessService _processService;
        private readonly IPriceClassService _priceClassService;
        private readonly IHelpContentProvider _helpContentProvider;

        private ObservableCollection<Receipt> _receipts = new ObservableCollection<Receipt>();
        private Receipt? _selectedReceipt;
        private ObservableCollection<Receipt> _selectedReceipts = new ObservableCollection<Receipt>();
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        // Filter properties
        private string _searchText = string.Empty;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private bool _showVoided;
        private Product? _selectedProduct;
        private Depot? _selectedDepot;
        private int? _selectedGrade;
        private string? _selectedCreatedBy;
        private bool? _isQualityChecked;

        // Statistics properties
        private int _totalReceipts;
        private decimal _totalGrossWeight;
        private decimal _totalNetWeight;
        private decimal _totalFinalWeight;
        private decimal _averageDockPercentage;

        // Pagination properties
        private int _currentPage = 1;
        private int _pageSize = 100;
        private int _totalPages;
        private int _totalRecords;

        // Lookup collections
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private ObservableCollection<Depot> _depots = new ObservableCollection<Depot>();
        private ObservableCollection<int> _grades = new ObservableCollection<int> { 1, 2, 3 };
        private ObservableCollection<string> _createdByUsers = new ObservableCollection<string>();
        private DateTime _lastUpdated = DateTime.Now;
        
        // Performance optimization fields
        private System.Windows.Threading.DispatcherTimer _searchDebounceTimer;
        private System.Windows.Threading.DispatcherTimer _filterDebounceTimer;

        #endregion

        #region Constructor

        public ReceiptListViewModel(
            IReceiptService receiptService,
            IReceiptExportService exportService,
            IReceiptValidationService validationService,
            IReceiptAnalyticsService analyticsService,
            IDialogService dialogService,
            IGrowerService growerService,
            IProductService productService,
            IDepotService depotService,
            IProcessService processService,
            IPriceClassService priceClassService,
            IHelpContentProvider helpContentProvider)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _priceClassService = priceClassService ?? throw new ArgumentNullException(nameof(priceClassService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));

            // Initialize commands
            LoadReceiptsCommand = new RelayCommand(async p => await LoadReceiptsAsync(), p => !IsLoading);
            RefreshCommand = new RelayCommand(async p => await LoadReceiptsAsync(), p => !IsLoading);
            SearchCommand = new RelayCommand(async p => await LoadReceiptsAsync(), p => !IsLoading);
            ClearFiltersCommand = new RelayCommand(p => ClearFilters());
            AddReceiptCommand = new RelayCommand(async p => await AddReceiptAsync(), p => !IsLoading);
            EditReceiptCommand = new RelayCommand(async p => await EditReceiptAsync(), p => !IsLoading && SelectedReceipt != null);
            ViewReceiptCommand = new RelayCommand(async p => await ViewReceiptAsync(), p => !IsLoading && SelectedReceipt != null);
            DuplicateReceiptCommand = new RelayCommand(async p => await DuplicateReceiptAsync(), p => !IsLoading && SelectedReceipt != null);
            VoidReceiptCommand = new RelayCommand(async p => await VoidReceiptAsync(), p => !IsLoading && SelectedReceipt != null && SelectedReceipt.CanVoid);
            DeleteReceiptCommand = new RelayCommand(async p => await DeleteReceiptAsync(), p => !IsLoading && SelectedReceipt != null && SelectedReceipt.CanDelete);
            PrintReceiptCommand = new RelayCommand(async p => await PrintReceiptAsync(), p => !IsLoading && SelectedReceipt != null);
            ExportCommand = new RelayCommand(async p => await ExportReceiptsAsync(), p => !IsLoading);
            BulkVoidCommand = new RelayCommand(async p => await BulkVoidReceiptsAsync(), p => !IsLoading && HasSelectedReceipts);
            BulkExportCommand = new RelayCommand(async p => await BulkExportReceiptsAsync(), p => !IsLoading && HasSelectedReceipts);

            // Pagination commands
            FirstPageCommand = new RelayCommand(p => GoToFirstPage(), p => CanGoToFirstPage);
            PreviousPageCommand = new RelayCommand(p => GoToPreviousPage(), p => CanGoToPreviousPage);
            NextPageCommand = new RelayCommand(p => GoToNextPage(), p => CanGoToNextPage);
            LastPageCommand = new RelayCommand(p => GoToLastPage(), p => CanGoToLastPage);

            // Navigation commands
            NavigateToDashboardCommand = new RelayCommand(p => NavigateToDashboard());

            // Initialize date range to current month
            var now = DateTime.Now;
            StartDate = new DateTime(now.Year, now.Month, 1);
            EndDate = now;

            // Initialize debounce timers for performance
            InitializeDebounceTimers();

            // Load initial data
            _ = Task.Run(async () => await InitializeAsync());
        }

        #endregion

        #region Properties

        public ObservableCollection<Receipt> Receipts
        {
            get => _receipts;
            set => SetProperty(ref _receipts, value);
        }

        public Receipt? SelectedReceipt
        {
            get => _selectedReceipt;
            set
            {
                SetProperty(ref _selectedReceipt, value);
                OnPropertyChanged(nameof(HasSelectedReceipt));
                OnPropertyChanged(nameof(CanVoidSelectedReceipt));
                OnPropertyChanged(nameof(CanDeleteSelectedReceipt));
            }
        }

        public ObservableCollection<Receipt> SelectedReceipts
        {
            get => _selectedReceipts;
            set
            {
                SetProperty(ref _selectedReceipts, value);
                OnPropertyChanged(nameof(HasSelectedReceipts));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                OnPropertyChanged(nameof(CanLoadData));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Filter Properties
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public bool ShowVoided
        {
            get => _showVoided;
            set => SetProperty(ref _showVoided, value);
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public Depot? SelectedDepot
        {
            get => _selectedDepot;
            set => SetProperty(ref _selectedDepot, value);
        }

        public int? SelectedGrade
        {
            get => _selectedGrade;
            set => SetProperty(ref _selectedGrade, value);
        }

        public string? SelectedCreatedBy
        {
            get => _selectedCreatedBy;
            set => SetProperty(ref _selectedCreatedBy, value);
        }

        public bool? IsQualityChecked
        {
            get => _isQualityChecked;
            set => SetProperty(ref _isQualityChecked, value);
        }

        // Statistics Properties
        public int TotalReceipts
        {
            get => _totalReceipts;
            set => SetProperty(ref _totalReceipts, value);
        }

        public decimal TotalGrossWeight
        {
            get => _totalGrossWeight;
            set => SetProperty(ref _totalGrossWeight, value);
        }

        public decimal TotalNetWeight
        {
            get => _totalNetWeight;
            set => SetProperty(ref _totalNetWeight, value);
        }

        public decimal TotalFinalWeight
        {
            get => _totalFinalWeight;
            set => SetProperty(ref _totalFinalWeight, value);
        }

        public decimal AverageDockPercentage
        {
            get => _averageDockPercentage;
            set => SetProperty(ref _averageDockPercentage, value);
        }

        // Pagination Properties
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                SetProperty(ref _currentPage, value);
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(CurrentPageInfo));
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set 
            {
                if (SetProperty(ref _pageSize, value))
                {
                    Logger.Info($"PageSize changed from {_pageSize} to {value}");
                    // Reset to first page when page size changes
                    CurrentPage = 1;
                    // Reload data with new page size
                    _ = Task.Run(async () => await LoadReceiptsAsync());
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                SetProperty(ref _totalPages, value);
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set
            {
                SetProperty(ref _totalRecords, value);
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }

        // Lookup Collections
        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Depot> Depots
        {
            get => _depots;
            set => SetProperty(ref _depots, value);
        }

        public ObservableCollection<int> Grades
        {
            get => _grades;
            set => SetProperty(ref _grades, value);
        }

        public ObservableCollection<string> CreatedByUsers
        {
            get => _createdByUsers;
            set => SetProperty(ref _createdByUsers, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        // Computed Properties
        public bool HasSelectedReceipt => SelectedReceipt != null;
        public bool CanVoidSelectedReceipt => SelectedReceipt?.CanVoid ?? false;
        public bool CanDeleteSelectedReceipt => SelectedReceipt?.CanDelete ?? false;
        public bool HasSelectedReceipts => SelectedReceipts.Count > 0;
        public bool CanLoadData => !IsLoading;

        public bool CanGoToFirstPage => CurrentPage > 1;
        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;
        public bool CanGoToLastPage => CurrentPage < TotalPages;

        public string CurrentPageInfo => $"Page {CurrentPage} of {TotalPages}";
        public string PaginationInfo 
        {
            get
            {
                if (TotalRecords == 0)
                    return "Showing 0 of 0 records";
                
                var startRecord = ((CurrentPage - 1) * PageSize) + 1;
                var endRecord = Math.Min(CurrentPage * PageSize, TotalRecords);
                return $"Showing {startRecord}-{endRecord} of {TotalRecords} records";
            }
        }

        #endregion

        #region Performance Optimization Methods

        private void InitializeDebounceTimers()
        {
            // Initialize search debounce timer (300ms delay)
            _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchDebounceTimer.Tick += OnSearchDebounceTimerTick;

            // Initialize filter debounce timer (500ms delay)
            _filterDebounceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _filterDebounceTimer.Tick += OnFilterDebounceTimerTick;

            // Setup property change handlers for debounced operations
            PropertyChanged += OnPropertyChangedDebounced;
        }

        private void OnPropertyChangedDebounced(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SearchText):
                    // Debounce search text changes
                    _searchDebounceTimer?.Stop();
                    _searchDebounceTimer?.Start();
                    break;

                case nameof(StartDate):
                case nameof(EndDate):
                case nameof(SelectedProduct):
                case nameof(SelectedDepot):
                case nameof(SelectedGrade):
                case nameof(SelectedCreatedBy):
                case nameof(IsQualityChecked):
                case nameof(ShowVoided):
                    // Debounce filter changes
                    _filterDebounceTimer?.Stop();
                    _filterDebounceTimer?.Start();
                    break;
            }
        }

        private async void OnSearchDebounceTimerTick(object sender, EventArgs e)
        {
            _searchDebounceTimer?.Stop();
            await LoadReceiptsAsync();
        }

        private async void OnFilterDebounceTimerTick(object sender, EventArgs e)
        {
            _filterDebounceTimer?.Stop();
            await LoadReceiptsAsync();
        }

        private void CleanupDebounceTimers()
        {
            _searchDebounceTimer?.Stop();
            _filterDebounceTimer?.Stop();
            _searchDebounceTimer = null;
            _filterDebounceTimer = null;
        }

        // Implement IDisposable for proper cleanup
        public void Dispose()
        {
            CleanupDebounceTimers();
        }

        #endregion

        #region Commands

        public ICommand LoadReceiptsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AddReceiptCommand { get; }
        public ICommand EditReceiptCommand { get; }
        public ICommand ViewReceiptCommand { get; }
        public ICommand DuplicateReceiptCommand { get; }
        public ICommand VoidReceiptCommand { get; }
        public ICommand DeleteReceiptCommand { get; }
        public ICommand PrintReceiptCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand BulkVoidCommand { get; }
        public ICommand BulkExportCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }

        #endregion

        #region Public Methods

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Initializing...";

                // Load lookup data
                await LoadLookupDataAsync();

                // Load receipts
                await LoadReceiptsAsync();

                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing ReceiptListViewModel", ex);
                StatusMessage = "Error initializing";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadLookupDataAsync()
        {
            try
            {
                // Load products
                var products = await _productService.GetAllProductsAsync();
                Products.Clear();
                foreach (var product in products.Where(p => !p.IsDeleted))
                {
                    Products.Add(product);
                }

                // Load depots
                var depots = await _depotService.GetAllDepotsAsync();
                Depots.Clear();
                foreach (var depot in depots.Where(d => !d.IsDeleted))
                {
                    Depots.Add(depot);
                }

                // Load created by users (this would typically come from a user service)
                CreatedByUsers.Clear();
                CreatedByUsers.Add("All Users");
                // TODO: Load actual users from user service
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading lookup data", ex);
            }
        }

        private async Task LoadReceiptsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading receipts...";

                var filters = new ReceiptFilters
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    SearchText = SearchText,
                    ShowVoided = ShowVoided,
                    ProductId = SelectedProduct?.ProductId,
                    DepotId = SelectedDepot?.DepotId,
                    Grade = SelectedGrade,
                    CreatedBy = SelectedCreatedBy,
                    IsQualityChecked = IsQualityChecked,
                    PageNumber = CurrentPage,
                    PageSize = PageSize,
                    SortBy = "ReceiptDate",
                    SortDescending = true
                };

                var (receipts, totalCount) = await _receiptService.GetReceiptsWithFiltersAndCountAsync(filters);
                
                // Performance optimization: Use bulk operations instead of individual Add calls
                var newReceipts = new ObservableCollection<Receipt>();
                foreach (var receipt in receipts)
                {
                    newReceipts.Add(receipt);
                }
                
                // Replace entire collection at once (more efficient than Clear + Add)
                Receipts = newReceipts;

                // Update pagination info with actual total count
                TotalRecords = totalCount;
                TotalPages = (int)Math.Ceiling((double)TotalRecords / PageSize);
                LastUpdated = DateTime.Now;

                // Force property change notifications for pagination
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(CurrentPageInfo));
                OnPropertyChanged(nameof(PaginationInfo));

                // Refresh command states
                ((RelayCommand)FirstPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)PreviousPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)NextPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)LastPageCommand)?.RaiseCanExecuteChanged();

                // Add comprehensive logging
                Logger.Info($"=== RECEIPT LOADING DEBUG ===");
                Logger.Info($"Records returned from service: {receipts.Count}");
                Logger.Info($"Total count from service: {totalCount}");
                Logger.Info($"Receipts in collection: {Receipts.Count}");
                Logger.Info($"PageSize: {PageSize}");
                Logger.Info($"CurrentPage: {CurrentPage}");
                Logger.Info($"TotalPages: {TotalPages}");
                Logger.Info($"TotalRecords: {TotalRecords}");
                Logger.Info($"PaginationInfo: {PaginationInfo}");
                Logger.Info($"CanGoToNextPage: {CanGoToNextPage}");
                Logger.Info($"CanGoToPreviousPage: {CanGoToPreviousPage}");
                Logger.Info($"CanGoToFirstPage: {CanGoToFirstPage}");
                Logger.Info($"CanGoToLastPage: {CanGoToLastPage}");
                Logger.Info($"=== END DEBUG ===");

                // Calculate statistics asynchronously without blocking UI
                _ = Task.Run(async () => await CalculateStatisticsAsync());

                StatusMessage = $"Loaded {Receipts.Count} receipts";
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading receipts", ex);
                StatusMessage = "Error loading receipts";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task CalculateStatisticsAsync()
        {
            try
            {
                var analytics = await _analyticsService.GetReceiptAnalyticsAsync(StartDate, EndDate);
                
                Logger.Info($"=== STATISTICS CALCULATION ===");
                Logger.Info($"Analytics TotalReceipts: {analytics.TotalReceipts}");
                Logger.Info($"Analytics TotalGrossWeight: {analytics.TotalGrossWeight}");
                Logger.Info($"Analytics TotalNetWeight: {analytics.TotalNetWeight}");
                Logger.Info($"Analytics TotalFinalWeight: {analytics.TotalFinalWeight}");
                Logger.Info($"Analytics AverageDockPercentage: {analytics.AverageDockPercentage}");
                Logger.Info($"Current Receipts in collection: {Receipts.Count}");
                Logger.Info($"Current TotalRecords: {TotalRecords}");
                Logger.Info($"=== END STATISTICS ===");
                
                TotalReceipts = analytics.TotalReceipts;
                TotalGrossWeight = analytics.TotalGrossWeight;
                TotalNetWeight = analytics.TotalNetWeight;
                TotalFinalWeight = analytics.TotalFinalWeight;
                AverageDockPercentage = analytics.AverageDockPercentage;
            }
            catch (Exception ex)
            {
                Logger.Error("Error calculating statistics", ex);
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;
            ShowVoided = false;
            SelectedProduct = null;
            SelectedDepot = null;
            SelectedGrade = null;
            SelectedCreatedBy = null;
            IsQualityChecked = null;
            CurrentPage = 1;
        }

        private async Task AddReceiptAsync()
        {
            try
            {
                // Navigate to detail view in add mode
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    var detailViewModel = new ReceiptDetailViewModel(
                        _receiptService,
                        _exportService,
                        _validationService,
                        _dialogService,
                        _growerService,
                        _productService,
                        _depotService,
                        _processService,
                        _priceClassService,
                        _helpContentProvider,
                        null, // null for new receipt
                        false); // false = edit mode for new receipts

                    await detailViewModel.InitializeAsync();
                    mainViewModel.ReceiptListViewModel = this; // Store current list state
                    mainViewModel.CurrentView = detailViewModel;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error adding receipt", ex);
                StatusMessage = "Error adding receipt";
            }
        }

        private async Task EditReceiptAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                // Navigate to detail view in edit mode
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    var detailViewModel = new ReceiptDetailViewModel(
                        _receiptService,
                        _exportService,
                        _validationService,
                        _dialogService,
                        _growerService,
                        _productService,
                        _depotService,
                        _processService,
                        _priceClassService,
                        _helpContentProvider,
                        SelectedReceipt,
                        false); // false = edit mode

                    await detailViewModel.InitializeAsync();
                    mainViewModel.ReceiptListViewModel = this; // Store current list state
                    mainViewModel.CurrentView = detailViewModel;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error editing receipt", ex);
                StatusMessage = "Error editing receipt";
            }
        }

        private async Task ViewReceiptAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                // Navigate to detail view in view mode
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    var detailViewModel = new ReceiptDetailViewModel(
                        _receiptService,
                        _exportService,
                        _validationService,
                        _dialogService,
                        _growerService,
                        _productService,
                        _depotService,
                        _processService,
                        _priceClassService,
                        _helpContentProvider,
                        SelectedReceipt,
                        true); // true = view mode

                    await detailViewModel.InitializeAsync();
                    mainViewModel.ReceiptListViewModel = this; // Store current list state
                    mainViewModel.CurrentView = detailViewModel;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing receipt", ex);
                StatusMessage = "Error viewing receipt";
            }
        }

        private async Task DuplicateReceiptAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Duplicating receipt...";

                var duplicatedReceipt = await _receiptService.DuplicateReceiptAsync(SelectedReceipt.ReceiptId, "CurrentUser");
                
                if (duplicatedReceipt != null)
                {
                    StatusMessage = "Receipt duplicated successfully";
                    await LoadReceiptsAsync(); // Refresh the list
                }
                else
                {
                    StatusMessage = "Error duplicating receipt";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error duplicating receipt", ex);
                StatusMessage = "Error duplicating receipt";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task VoidReceiptAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                var result = await _dialogService.ShowInputDialogAsync("Void Receipt", "Enter reason for voiding:", "Receipt voided by user");
                
                if (result != null)
                {
                    IsLoading = true;
                    StatusMessage = "Voiding receipt...";

                    var success = await _receiptService.VoidReceiptAsync(SelectedReceipt.ReceiptNumber, result);
                    
                    if (success)
                    {
                        StatusMessage = "Receipt voided successfully";
                        await LoadReceiptsAsync(); // Refresh the list
                    }
                    else
                    {
                        StatusMessage = "Error voiding receipt";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error voiding receipt", ex);
                StatusMessage = "Error voiding receipt";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteReceiptAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                var result = await _dialogService.ShowConfirmationDialogAsync("Delete Receipt", 
                    $"Are you sure you want to delete receipt {SelectedReceipt.ReceiptNumber}? This action cannot be undone.");
                
                if (result == true)
                {
                    IsLoading = true;
                    StatusMessage = "Deleting receipt...";

                    var success = await _receiptService.DeleteReceiptAsync(SelectedReceipt.ReceiptNumber);
                    
                    if (success)
                    {
                        StatusMessage = "Receipt deleted successfully";
                        await LoadReceiptsAsync(); // Refresh the list
                    }
                    else
                    {
                        StatusMessage = "Error deleting receipt";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error deleting receipt", ex);
                StatusMessage = "Error deleting receipt";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PrintReceiptAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Generating print preview...";

                var pdfBytes = await _exportService.GenerateReceiptPrintPreviewAsync(SelectedReceipt.ReceiptId);
                
                if (pdfBytes.Length > 0)
                {
                    // TODO: Implement print functionality
                    StatusMessage = "Print preview generated";
                }
                else
                {
                    StatusMessage = "Error generating print preview";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error printing receipt", ex);
                StatusMessage = "Error printing receipt";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportReceiptsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Exporting receipts...";

                var fileName = $"Receipts_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var receiptIds = Receipts.Select(r => r.ReceiptId).ToList();
                var success = await _exportService.ExportMultipleReceiptsToExcelAsync(receiptIds, filePath);
                
                if (success)
                {
                    StatusMessage = $"Receipts exported to {fileName}";
                }
                else
                {
                    StatusMessage = "Error exporting receipts";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting receipts", ex);
                StatusMessage = "Error exporting receipts";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BulkVoidReceiptsAsync()
        {
            if (SelectedReceipts.Count == 0) return;

            try
            {
                var result = await _dialogService.ShowInputDialogAsync("Bulk Void Receipts", 
                    $"Enter reason for voiding {SelectedReceipts.Count} receipts:", "Bulk voided by user");
                
                if (result != null)
                {
                    IsLoading = true;
                    StatusMessage = "Voiding receipts...";

                    var receiptIds = SelectedReceipts.Select(r => r.ReceiptId).ToList();
                    var success = await _receiptService.BulkVoidReceiptsAsync(receiptIds, result, "CurrentUser");
                    
                    if (success)
                    {
                        StatusMessage = $"{SelectedReceipts.Count} receipts voided successfully";
                        await LoadReceiptsAsync(); // Refresh the list
                    }
                    else
                    {
                        StatusMessage = "Error voiding receipts";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error bulk voiding receipts", ex);
                StatusMessage = "Error voiding receipts";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task BulkExportReceiptsAsync()
        {
            if (SelectedReceipts.Count == 0) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Exporting selected receipts...";

                var fileName = $"SelectedReceipts_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var receiptIds = SelectedReceipts.Select(r => r.ReceiptId).ToList();
                var success = await _exportService.ExportMultipleReceiptsToExcelAsync(receiptIds, filePath);
                
                if (success)
                {
                    StatusMessage = $"Selected receipts exported to {fileName}";
                }
                else
                {
                    StatusMessage = "Error exporting selected receipts";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error bulk exporting receipts", ex);
                StatusMessage = "Error exporting receipts";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void GoToFirstPage()
        {
            Logger.Info($"Navigating to first page. Current: {CurrentPage}, TotalPages: {TotalPages}");
            CurrentPage = 1;
            _ = Task.Run(async () => await LoadReceiptsAsync());
        }

        private void GoToPreviousPage()
        {
            if (CurrentPage > 1)
            {
                Logger.Info($"Navigating to previous page. Current: {CurrentPage}, TotalPages: {TotalPages}");
                CurrentPage--;
                _ = Task.Run(async () => await LoadReceiptsAsync());
            }
        }

        private void GoToNextPage()
        {
            if (CurrentPage < TotalPages)
            {
                Logger.Info($"Navigating to next page. Current: {CurrentPage}, TotalPages: {TotalPages}");
                CurrentPage++;
                _ = Task.Run(async () => await LoadReceiptsAsync());
            }
        }

        private void GoToLastPage()
        {
            Logger.Info($"Navigating to last page. Current: {CurrentPage}, TotalPages: {TotalPages}");
            CurrentPage = TotalPages;
            _ = Task.Run(async () => await LoadReceiptsAsync());
        }

        private void NavigateToDashboard()
        {
            if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.NavigateToDashboardCommand.Execute(null);
            }
        }

        #endregion
    }
}
