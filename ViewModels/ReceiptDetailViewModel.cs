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
    /// ViewModel for the Receipt Detail view
    /// </summary>
    public class ReceiptDetailViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly IReceiptService _receiptService;
        private readonly IReceiptExportService _exportService;
        private readonly IReceiptValidationService _validationService;
        private readonly IDialogService _dialogService;
        private readonly IGrowerService _growerService;
        private readonly IProductService _productService;
        private readonly IDepotService _depotService;
        private readonly IProcessService _processService;
        private readonly IPriceClassService _priceClassService;
        private readonly IHelpContentProvider _helpContentProvider;

        private Receipt? _currentReceipt;
        private ReceiptDetailDto? _receiptDetail;
        private bool _isEditMode;
        private bool _isNewReceipt;
        private bool _isLoading;
        private bool _isExporting;
        private string _statusMessage = string.Empty;
        private DateTime _lastSaved = DateTime.MinValue;

        // Validation
        private Models.ValidationResult _validationResult = new Models.ValidationResult();
        private ObservableCollection<string> _validationErrors = new ObservableCollection<string>();

        // Lookup collections
        private ObservableCollection<Grower> _growers = new ObservableCollection<Grower>();
        private ObservableCollection<Product> _products = new ObservableCollection<Product>();
        private ObservableCollection<Process> _processes = new ObservableCollection<Process>();
        private ObservableCollection<ProcessType> _processTypes = new ObservableCollection<ProcessType>();
        private ObservableCollection<Variety> _varieties = new ObservableCollection<Variety>();
        private ObservableCollection<Depot> _depots = new ObservableCollection<Depot>();
        private ObservableCollection<PriceClass> _priceClasses = new ObservableCollection<PriceClass>();
        private ObservableCollection<int> _grades = new ObservableCollection<int> { 1, 2, 3 };

        // Selected items
        private Grower? _selectedGrower;
        private Product? _selectedProduct;
        private Process? _selectedProcess;
        private ProcessType? _selectedProcessType;
        private Variety? _selectedVariety;
        private Depot? _selectedDepot;
        private PriceClass? _selectedPriceClass;

        // Tab data
        private ObservableCollection<ReceiptPaymentAllocation> _paymentAllocations = new ObservableCollection<ReceiptPaymentAllocation>();
        private ObservableCollection<ReceiptAuditEntry> _auditHistory = new ObservableCollection<ReceiptAuditEntry>();
        private ObservableCollection<Receipt> _relatedReceipts = new ObservableCollection<Receipt>();
        private int _selectedTabIndex = 0;

        // Search and filter properties
        private string _paymentSearchText = string.Empty;
        private ObservableCollection<string> _auditFilterOptions = new ObservableCollection<string>();
        private string? _selectedAuditFilter;

        #endregion

        #region Constructor

        public ReceiptDetailViewModel(
            IReceiptService receiptService,
            IReceiptExportService exportService,
            IReceiptValidationService validationService,
            IDialogService dialogService,
            IGrowerService growerService,
            IProductService productService,
            IDepotService depotService,
            IProcessService processService,
            IPriceClassService priceClassService,
            IHelpContentProvider helpContentProvider,
            Receipt? existingReceipt = null,
            bool isViewMode = false)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _priceClassService = priceClassService ?? throw new ArgumentNullException(nameof(priceClassService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));

            _currentReceipt = existingReceipt;
            _isNewReceipt = existingReceipt == null;
            _isEditMode = _isNewReceipt && !isViewMode; // New receipts start in edit mode unless view mode
            _isExporting = false; // Initialize to false

            // Initialize commands
            ToggleEditModeCommand = new RelayCommand(p => ToggleEditMode(), p => !IsLoading);
            SaveCommand = new RelayCommand(async p => await SaveAsync(), p => CanSave);
            CancelCommand = new RelayCommand(p => CancelEdit(), p => IsEditMode);
            DuplicateReceiptCommand = new RelayCommand(async p => await DuplicateReceiptAsync(), p => !IsLoading && !IsNewReceipt);
            DeleteReceiptCommand = new RelayCommand(async p => await DeleteReceiptAsync(), p => CanDelete);
            PrintReceiptCommand = new RelayCommand(async p => await PrintReceiptAsync(), p => !IsLoading && !IsNewReceipt);
            ExportToPdfCommand = new RelayCommand(async p => await ExportToPdfAsync(), p => !IsLoading && !IsNewReceipt);
            ExportToExcelCommand = new RelayCommand(async p => await ExportToExcelAsync(), p => !IsLoading && !IsNewReceipt);
            QualityCheckCommand = new RelayCommand(async p => await QualityCheckAsync(), p => CanQualityCheck);
            CalculateWeightsCommand = new RelayCommand(p => CalculateWeights(), p => IsEditMode);
            RefreshCommand = new RelayCommand(async p => await RefreshAsync(), p => !IsLoading);
            NavigateBackCommand = new RelayCommand(p => NavigateBack());
            NavigateToDashboardCommand = new RelayCommand(p => NavigateToDashboard());
            ShowHelpCommand = new RelayCommand(p => ShowHelpExecute());
            RefreshPaymentAllocationsCommand = new RelayCommand(async p => await RefreshPaymentAllocationsAsync());
            RefreshAuditHistoryCommand = new RelayCommand(async p => await RefreshAuditHistoryAsync());

            // Initialize new receipt with defaults
            if (_isNewReceipt)
            {
                InitializeNewReceipt();
            }
        }

        #endregion

        #region Properties

        public Receipt? CurrentReceipt
        {
            get => _currentReceipt;
            set
            {
                SetProperty(ref _currentReceipt, value);
                OnPropertyChanged(nameof(ReceiptNumber));
                OnPropertyChanged(nameof(ReceiptDate));
                OnPropertyChanged(nameof(ReceiptTime));
                OnPropertyChanged(nameof(GrossWeight));
                OnPropertyChanged(nameof(TareWeight));
                OnPropertyChanged(nameof(NetWeight));
                OnPropertyChanged(nameof(DockPercentage));
                OnPropertyChanged(nameof(DockWeight));
                OnPropertyChanged(nameof(FinalWeight));
                OnPropertyChanged(nameof(Grade));
                OnPropertyChanged(nameof(IsVoided));
                OnPropertyChanged(nameof(VoidedReason));
                OnPropertyChanged(nameof(VoidedBy));
                OnPropertyChanged(nameof(VoidedDisplay));
                OnPropertyChanged(nameof(CanDelete));
                OnPropertyChanged(nameof(CanQualityCheck));
            }
        }

        public ReceiptDetailDto? ReceiptDetail
        {
            get => _receiptDetail;
            set => SetProperty(ref _receiptDetail, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                SetProperty(ref _isEditMode, value);
                OnPropertyChanged(nameof(EditModeButtonText));
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool IsNewReceipt
        {
            get => _isNewReceipt;
            set => SetProperty(ref _isNewReceipt, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                OnPropertyChanged(nameof(CanSave));
            }
        }

        public bool IsExporting
        {
            get => _isExporting;
            set => SetProperty(ref _isExporting, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public DateTime LastSaved
        {
            get => _lastSaved;
            set
            {
                SetProperty(ref _lastSaved, value);
                OnPropertyChanged(nameof(LastSavedDisplay));
            }
        }

        // Receipt Properties
        public string ReceiptNumber
        {
            get => CurrentReceipt?.ReceiptNumber ?? string.Empty;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.ReceiptNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime ReceiptDate
        {
            get => CurrentReceipt?.ReceiptDate ?? DateTime.Today;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.ReceiptDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public TimeSpan ReceiptTime
        {
            get => CurrentReceipt?.ReceiptTime ?? TimeSpan.Zero;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.ReceiptTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal GrossWeight
        {
            get => CurrentReceipt?.GrossWeight ?? 0;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.GrossWeight = value;
                    OnPropertyChanged();
                    CalculateWeights(); // Auto-calculate when changed
                }
            }
        }

        public decimal TareWeight
        {
            get => CurrentReceipt?.TareWeight ?? 0;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.TareWeight = value;
                    OnPropertyChanged();
                    CalculateWeights(); // Auto-calculate when changed
                }
            }
        }

        public decimal NetWeight
        {
            get => CurrentReceipt?.NetWeight ?? 0;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.NetWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal DockPercentage
        {
            get => CurrentReceipt?.DockPercentage ?? 0;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.DockPercentage = value;
                    OnPropertyChanged();
                    CalculateWeights(); // Auto-calculate when changed
                }
            }
        }

        public decimal DockWeight
        {
            get => CurrentReceipt?.DockWeight ?? 0;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.DockWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal FinalWeight
        {
            get => CurrentReceipt?.FinalWeight ?? 0;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.FinalWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Grade
        {
            get => CurrentReceipt?.Grade ?? 1;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.Grade = (byte)value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsVoided
        {
            get => CurrentReceipt?.IsVoided ?? false;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.IsVoided = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? VoidedReason
        {
            get => CurrentReceipt?.VoidedReason;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.VoidedReason = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? VoidedBy
        {
            get => CurrentReceipt?.VoidedBy;
            set
            {
                if (CurrentReceipt != null)
                {
                    CurrentReceipt.VoidedBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VoidedDisplay
        {
            get => CurrentReceipt?.VoidedDisplay ?? "Not voided";
        }

        // Lookup Collections
        public ObservableCollection<Grower> Growers
        {
            get => _growers;
            set => SetProperty(ref _growers, value);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Process> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        public ObservableCollection<ProcessType> ProcessTypes
        {
            get => _processTypes;
            set => SetProperty(ref _processTypes, value);
        }

        public ObservableCollection<Variety> Varieties
        {
            get => _varieties;
            set => SetProperty(ref _varieties, value);
        }

        public ObservableCollection<Depot> Depots
        {
            get => _depots;
            set => SetProperty(ref _depots, value);
        }

        public ObservableCollection<PriceClass> PriceClasses
        {
            get => _priceClasses;
            set => SetProperty(ref _priceClasses, value);
        }

        public ObservableCollection<int> Grades
        {
            get => _grades;
            set => SetProperty(ref _grades, value);
        }

        // Selected Items
        public Grower? SelectedGrower
        {
            get => _selectedGrower;
            set
            {
                SetProperty(ref _selectedGrower, value);
                if (CurrentReceipt != null && value != null)
                {
                    CurrentReceipt.GrowerId = value.GrowerId;
                }
                OnPropertyChanged(nameof(HasSelectedGrower));
            }
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (CurrentReceipt != null && value != null)
                {
                    CurrentReceipt.ProductId = value.ProductId;
                }
            }
        }

        public Process? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                SetProperty(ref _selectedProcess, value);
                if (CurrentReceipt != null && value != null)
                {
                    CurrentReceipt.ProcessId = value.ProcessId;
                }
            }
        }

        public ProcessType? SelectedProcessType
        {
            get => _selectedProcessType;
            set
            {
                SetProperty(ref _selectedProcessType, value);
                if (CurrentReceipt != null && value != null)
                {
                    CurrentReceipt.ProcessTypeId = value.ProcessTypeId;
                }
            }
        }

        public Variety? SelectedVariety
        {
            get => _selectedVariety;
            set
            {
                SetProperty(ref _selectedVariety, value);
                if (CurrentReceipt != null && value != null)
                {
                    CurrentReceipt.VarietyId = value.VarietyId;
                }
            }
        }

        public Depot? SelectedDepot
        {
            get => _selectedDepot;
            set
            {
                SetProperty(ref _selectedDepot, value);
                if (CurrentReceipt != null && value != null)
                {
                    CurrentReceipt.DepotId = value.DepotId;
                }
            }
        }

        public PriceClass? SelectedPriceClass
        {
            get => _selectedPriceClass;
            set
            {
                SetProperty(ref _selectedPriceClass, value);
                if (CurrentReceipt != null && value != null)
                {
                    CurrentReceipt.PriceClassId = value.PriceClassId;
                }
            }
        }

        // Tab Data
        public ObservableCollection<ReceiptPaymentAllocation> PaymentAllocations
        {
            get => _paymentAllocations;
            set => SetProperty(ref _paymentAllocations, value);
        }

        public ObservableCollection<ReceiptAuditEntry> AuditHistory
        {
            get => _auditHistory;
            set => SetProperty(ref _auditHistory, value);
        }

        public ObservableCollection<Receipt> RelatedReceipts
        {
            get => _relatedReceipts;
            set => SetProperty(ref _relatedReceipts, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        // Search and filter properties
        public string PaymentSearchText
        {
            get => _paymentSearchText;
            set
            {
                if (SetProperty(ref _paymentSearchText, value))
                {
                    FilterPaymentAllocations();
                }
            }
        }

        public ObservableCollection<string> AuditFilterOptions
        {
            get => _auditFilterOptions;
            set => SetProperty(ref _auditFilterOptions, value);
        }

        public string? SelectedAuditFilter
        {
            get => _selectedAuditFilter;
            set
            {
                if (SetProperty(ref _selectedAuditFilter, value))
                {
                    FilterAuditHistory();
                }
            }
        }

        // Validation Properties
        public Models.ValidationResult ValidationResult
        {
            get => _validationResult;
            set
            {
                SetProperty(ref _validationResult, value);
                OnPropertyChanged(nameof(HasValidationErrors));
                OnPropertyChanged(nameof(ValidationErrorSummary));
            }
        }

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        // Computed Properties
        public bool HasSelectedGrower => SelectedGrower != null;
        public bool CanSave => IsEditMode && !IsLoading && !HasValidationErrors;
        public bool CanDelete => CurrentReceipt?.CanDelete ?? false;
        public bool CanQualityCheck => CurrentReceipt?.CanQualityCheck ?? false;
        public bool HasValidationErrors => ValidationResult.HasErrors;
        public bool HasPaymentAllocations => PaymentAllocations.Count > 0;
        public bool HasAuditHistory => AuditHistory.Count > 0;
        public bool HasRelatedReceipts => RelatedReceipts.Count > 0;
        public bool IsReceiptNumberReadOnly => !IsNewReceipt; // Receipt numbers are auto-generated and read-only for existing receipts

        public string EditModeButtonText => IsEditMode ? "View Mode" : "Edit Mode";
        public string ValidationErrorSummary => ValidationResult.ErrorSummary;
        public string ReceiptDateTimeDisplay => $"{ReceiptDate:yyyy-MM-dd} {ReceiptTime:HH:mm}";
        public string StatusDisplay => IsVoided ? "Voided" : "Active";
        public string QualityStatusDisplay => CurrentReceipt?.QualityStatusDisplay ?? "Pending";
        public string PaymentStatusDisplay => "Unpaid"; // TODO: Implement payment status logic
        public string WeightSummary => $"Gross: {GrossWeight:N2} | Net: {NetWeight:N2} | Final: {FinalWeight:N2}";
        public string DockSummary => $"Dock: {DockPercentage:N2}% ({DockWeight:N2} lbs)";
        public string CreatedDisplay => CurrentReceipt?.CreatedDisplay ?? "Not created";
        public string ModifiedDisplay => CurrentReceipt?.ModifiedDisplay ?? "Never";
        public string QualityCheckedDisplay => CurrentReceipt?.QualityCheckedDisplay ?? "Not checked";
        public string LastSavedDisplay => LastSaved > DateTime.MinValue ? $"Last saved: {LastSaved:yyyy-MM-dd HH:mm}" : "Not saved";
        public string ImportBatchDisplay => CurrentReceipt?.ImportBatchId?.ToString() ?? "Not assigned";

        #endregion

        #region Commands

        public ICommand ToggleEditModeCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DuplicateReceiptCommand { get; }
        public ICommand DeleteReceiptCommand { get; }
        public ICommand PrintReceiptCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand ExportToExcelCommand { get; }
        public ICommand QualityCheckCommand { get; }
        public ICommand CalculateWeightsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NavigateBackCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand RefreshPaymentAllocationsCommand { get; }
        public ICommand RefreshAuditHistoryCommand { get; }

        #endregion

        #region Public Methods

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading receipt details...";

                // Load lookup data
                await LoadLookupDataAsync();

                // Load receipt detail if not new
                if (!IsNewReceipt && CurrentReceipt != null)
                {
                    await LoadReceiptDetailAsync();
                }

                // Load tab data
                await LoadTabDataAsync();

                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing ReceiptDetailViewModel", ex);
                StatusMessage = "Error loading receipt details";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private void InitializeNewReceipt()
        {
            CurrentReceipt = new Receipt
            {
                ReceiptDate = DateTime.Today,
                ReceiptTime = DateTime.Now.TimeOfDay,
                Grade = 1,
                CreatedAt = DateTime.Now,
                CreatedBy = "CurrentUser" // TODO: Get from current user context
            };
        }

        private async Task LoadLookupDataAsync()
        {
            try
            {
                // Load growers
                var growers = await _growerService.GetAllGrowersAsync();
                Growers.Clear();
                foreach (var grower in growers.Where(g => !g.DeletedAt.HasValue))
                {
                    Growers.Add(grower);
                }

                // Load products
                var products = await _productService.GetAllProductsAsync();
                Products.Clear();
                foreach (var product in products.Where(p => !p.DeletedAt.HasValue))
                {
                    Products.Add(product);
                }

                // Load depots
                var depots = await _depotService.GetAllDepotsAsync();
                Depots.Clear();
                foreach (var depot in depots.Where(d => !d.DeletedAt.HasValue))
                {
                    Depots.Add(depot);
                }

                // Load processes
                var processes = await _processService.GetAllProcessesAsync();
                Processes.Clear();
                foreach (var process in processes.Where(p => !p.DeletedAt.HasValue))
                {
                    Processes.Add(process);
                }

                // Load price classes
                var priceClasses = await _priceClassService.GetAllPriceClassesAsync();
                PriceClasses.Clear();
                foreach (var priceClass in priceClasses.Where(pc => !pc.IsDeleted))
                {
                    PriceClasses.Add(priceClass);
                }

                // TODO: Load other lookup data (process types, varieties)
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading lookup data", ex);
            }
        }

        private async Task LoadReceiptDetailAsync()
        {
            try
            {
                if (CurrentReceipt == null) return;

                ReceiptDetail = await _receiptService.GetReceiptDetailAsync(CurrentReceipt.ReceiptId);
                
                if (ReceiptDetail != null)
                {
                    // Set selected items based on detail data
                    SelectedGrower = Growers.FirstOrDefault(g => g.GrowerId == ReceiptDetail.GrowerId);
                    SelectedProduct = Products.FirstOrDefault(p => p.ProductId == ReceiptDetail.ProductId);
                    SelectedDepot = Depots.FirstOrDefault(d => d.DepotId == ReceiptDetail.DepotId);
                    SelectedProcess = Processes.FirstOrDefault(p => p.ProcessId == ReceiptDetail.ProcessId);
                    SelectedPriceClass = PriceClasses.FirstOrDefault(pc => pc.PriceClassId == ReceiptDetail.PriceClassId);
                    // TODO: Set other selected items (process types, varieties)
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading receipt detail", ex);
            }
        }

        private async Task LoadTabDataAsync()
        {
            try
            {
                if (CurrentReceipt == null) return;

                // Load payment allocations
                try
                {
                    var allocations = await _receiptService.GetReceiptPaymentAllocationsAsync(CurrentReceipt.ReceiptId);
                    PaymentAllocations.Clear();
                    foreach (var allocation in allocations)
                    {
                        PaymentAllocations.Add(allocation);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Could not load payment allocations", ex);
                    // Continue with other data loading
                }

                // Load audit history
                try
                {
                    var auditHistory = await _receiptService.GetReceiptAuditHistoryAsync(CurrentReceipt.ReceiptId);
                    AuditHistory.Clear();
                    foreach (var entry in auditHistory)
                    {
                        AuditHistory.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Could not load audit history - ReceiptAuditTrail table may not exist", ex);
                    // Continue without audit history - this is not critical for basic functionality
                }

                // Load related receipts
                try
                {
                    var relatedReceipts = await _receiptService.GetRelatedReceiptsAsync(CurrentReceipt.ReceiptId);
                    RelatedReceipts.Clear();
                    foreach (var receipt in relatedReceipts)
                    {
                        RelatedReceipts.Add(receipt);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Could not load related receipts", ex);
                    // Continue without related receipts
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading tab data", ex);
            }
        }

        private void ToggleEditMode()
        {
            IsEditMode = !IsEditMode;
            StatusMessage = IsEditMode ? "Edit mode enabled" : "View mode enabled";
        }

        private async Task SaveAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Validating receipt...";

                // Validate receipt
                if (CurrentReceipt != null)
                {
                    ValidationResult = await _validationService.ValidateReceiptDataAsync(CurrentReceipt);
                    
                    if (ValidationResult.HasErrors)
                    {
                        UpdateValidationErrors();
                        StatusMessage = "Validation errors found. Please correct them before saving.";
                        return;
                    }
                }

                StatusMessage = "Saving receipt...";

                // Save receipt
                var savedReceipt = await _receiptService.SaveReceiptAsync(CurrentReceipt!);
                
                if (savedReceipt != null)
                {
                    CurrentReceipt = savedReceipt;
                    IsNewReceipt = false;
                    LastSaved = DateTime.Now;
                    StatusMessage = "Receipt saved successfully";
                    
                    // Refresh detail data
                    await LoadReceiptDetailAsync();
                }
                else
                {
                    StatusMessage = "Error saving receipt";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving receipt", ex);
                StatusMessage = "Error saving receipt";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit()
        {
            if (IsNewReceipt)
            {
                // Navigate back to list
                NavigateBack();
            }
            else
            {
                // Reload original data
                _ = Task.Run(async () => await RefreshAsync());
                IsEditMode = false;
                StatusMessage = "Edit cancelled";
            }
        }

        private async Task DuplicateReceiptAsync()
        {
            if (CurrentReceipt == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Duplicating receipt...";

                var duplicatedReceipt = await _receiptService.DuplicateReceiptAsync(CurrentReceipt.ReceiptId, "CurrentUser");
                
                if (duplicatedReceipt != null)
                {
                    CurrentReceipt = duplicatedReceipt;
                    IsNewReceipt = true;
                    IsEditMode = true;
                    StatusMessage = "Receipt duplicated successfully";
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

        private async Task DeleteReceiptAsync()
        {
            if (CurrentReceipt == null) return;

            try
            {
                var result = await _dialogService.ShowConfirmationDialogAsync("Delete Receipt", 
                    $"Are you sure you want to delete receipt {CurrentReceipt.ReceiptNumber}? This action cannot be undone.");
                
                if (result == true)
                {
                    IsLoading = true;
                    StatusMessage = "Deleting receipt...";

                    var success = await _receiptService.DeleteReceiptAsync(CurrentReceipt.ReceiptNumber ?? string.Empty);
                    
                    if (success)
                    {
                        StatusMessage = "Receipt deleted successfully";
                        NavigateBack();
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
            if (CurrentReceipt == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Generating print preview...";

                var pdfBytes = await _exportService.GenerateReceiptPrintPreviewAsync(CurrentReceipt.ReceiptId);
                
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

        private async Task ExportToPdfAsync()
        {
            if (CurrentReceipt == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Exporting to PDF...";

                var fileName = $"Receipt_{CurrentReceipt.ReceiptNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var success = await _exportService.ExportReceiptToPdfAsync(CurrentReceipt.ReceiptId, filePath);
                
                if (success)
                {
                    StatusMessage = $"Receipt exported to {fileName}";
                }
                else
                {
                    StatusMessage = "Error exporting receipt to PDF";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting receipt to PDF", ex);
                StatusMessage = "Error exporting receipt to PDF";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportToExcelAsync()
        {
            if (CurrentReceipt == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Exporting to Excel...";

                var fileName = $"Receipt_{CurrentReceipt.ReceiptNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                var success = await _exportService.ExportReceiptToExcelAsync(CurrentReceipt.ReceiptId, filePath);
                
                if (success)
                {
                    StatusMessage = $"Receipt exported to {fileName}";
                }
                else
                {
                    StatusMessage = "Error exporting receipt to Excel";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error exporting receipt to Excel", ex);
                StatusMessage = "Error exporting receipt to Excel";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task QualityCheckAsync()
        {
            if (CurrentReceipt == null) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Marking as quality checked...";

                var success = await _receiptService.MarkQualityCheckedAsync(CurrentReceipt.ReceiptId, "CurrentUser");
                
                if (success)
                {
                    StatusMessage = "Receipt marked as quality checked";
                    await RefreshAsync(); // Refresh to show updated data
                }
                else
                {
                    StatusMessage = "Error marking receipt as quality checked";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error quality checking receipt", ex);
                StatusMessage = "Error quality checking receipt";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculateWeights()
        {
            if (CurrentReceipt == null) return;

            try
            {
                // Calculate net weight
                var netWeight = GrossWeight - TareWeight;
                NetWeight = netWeight;

                // Calculate dock weight
                var dockWeight = netWeight * (DockPercentage / 100);
                DockWeight = dockWeight;

                // Calculate final weight
                var finalWeight = netWeight - dockWeight;
                FinalWeight = finalWeight;

                StatusMessage = "Weights calculated";
            }
            catch (Exception ex)
            {
                Logger.Error("Error calculating weights", ex);
                StatusMessage = "Error calculating weights";
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Refreshing receipt details...";

                if (!IsNewReceipt && CurrentReceipt != null)
                {
                    await LoadReceiptDetailAsync();
                    await LoadTabDataAsync();
                }

                StatusMessage = "Receipt details refreshed";
            }
            catch (Exception ex)
            {
                Logger.Error("Error refreshing receipt details", ex);
                StatusMessage = "Error refreshing receipt details";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateValidationErrors()
        {
            ValidationErrors.Clear();
            foreach (var error in ValidationResult.Errors)
            {
                ValidationErrors.Add($"{error.FieldName}: {error.Message}");
            }
        }

        private void NavigateBack()
        {
            if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                if (mainViewModel.ReceiptListViewModel != null)
                {
                    mainViewModel.CurrentView = mainViewModel.ReceiptListViewModel; // Restore previous list view state
                }
                else
                {
                    // Fallback: create new list view
                    mainViewModel.NavigateToReceiptsCommand.Execute(null);
                }
            }
        }

        private void NavigateToDashboard()
        {
            if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.NavigateToDashboardCommand.Execute(null);
            }
        }

        private async void ShowHelpExecute()
        {
            try
            {
                var helpContent = _helpContentProvider.GetHelpContent("ReceiptDetailView");
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
                await _dialogService.ShowMessageBoxAsync("Unable to show help at this time.", "Help");
            }
        }

        private async Task RefreshPaymentAllocationsAsync()
        {
            try
            {
                if (CurrentReceipt == null) return;

                var allocations = await _receiptService.GetReceiptPaymentAllocationsAsync(CurrentReceipt.ReceiptId);
                PaymentAllocations.Clear();
                foreach (var allocation in allocations)
                {
                    PaymentAllocations.Add(allocation);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error refreshing payment allocations", ex);
            }
        }

        private async Task RefreshAuditHistoryAsync()
        {
            try
            {
                if (CurrentReceipt == null) return;

                var auditHistory = await _receiptService.GetReceiptAuditHistoryAsync(CurrentReceipt.ReceiptId);
                AuditHistory.Clear();
                foreach (var entry in auditHistory)
                {
                    AuditHistory.Add(entry);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Could not refresh audit history - ReceiptAuditTrail table may not exist", ex);
                StatusMessage = "Audit history not available - ReceiptAuditTrail table missing";
            }
        }

        private void FilterPaymentAllocations()
        {
            // TODO: Implement payment allocation filtering based on PaymentSearchText
            // This would filter the PaymentAllocations collection
        }

        private void FilterAuditHistory()
        {
            // TODO: Implement audit history filtering based on SelectedAuditFilter
            // This would filter the AuditHistory collection
        }

        #endregion
    }
}
