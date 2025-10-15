using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the grower detail/edit view with tabbed form layout.
    /// Handles grower creation, editing, and viewing with validation.
    /// </summary>
    public class GrowerDetailViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IPayGroupService _payGroupService;
        private readonly IPaymentMethodService _paymentMethodService;
        private readonly IDepotService _depotService;
        private readonly IPriceClassService _priceClassService;
        private GrowerManagementHostViewModel? _parentHost;

        // Grower data
        private Grower? _currentGrower;
        private Grower? _originalGrower;
        private GrowerStatistics? _growerStatistics;

        // Mode flags
        private bool _isEditMode;
        private bool _isNewGrower;
        private bool _isReadOnly;
        private bool _isDirty;

        // Lookup collections
        private ObservableCollection<PaymentGroup> _paymentGroups;
        private ObservableCollection<PaymentMethod> _paymentMethods;
        private ObservableCollection<Depot> _depots;
        private ObservableCollection<PriceClass> _priceClasses;
        private List<string> _provinces;
        private List<string> _currencyCodes;

        // History data
        private ObservableCollection<Receipt> _recentReceipts;
        private ObservableCollection<Payment> _recentPayments;

        // Validation
        private Dictionary<string, string> _validationErrors;
        private bool _isValidating;

        // UI State
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private int _selectedTabIndex;
        private bool _hasUnsavedChanges;

        public GrowerDetailViewModel(
            IGrowerService growerService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IPayGroupService payGroupService,
            IPaymentMethodService paymentMethodService,
            IDepotService depotService,
            IPriceClassService priceClassService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _paymentMethodService = paymentMethodService ?? throw new ArgumentNullException(nameof(paymentMethodService));
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _priceClassService = priceClassService ?? throw new ArgumentNullException(nameof(priceClassService));

            // Initialize collections
            PaymentGroups = new ObservableCollection<PaymentGroup>();
            PaymentMethods = new ObservableCollection<PaymentMethod>();
            Depots = new ObservableCollection<Depot>();
            PriceClasses = new ObservableCollection<PriceClass>();
            RecentReceipts = new ObservableCollection<Receipt>();
            RecentPayments = new ObservableCollection<Payment>();
            _validationErrors = new Dictionary<string, string>();

            // Initialize static data
            Provinces = ProvinceHelper.GetCanadianProvinces();
            CurrencyCodes = new List<string> { "CAD", "USD" };

            // Initialize commands
            SaveCommand = new RelayCommand(ExecuteSaveAsync, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            EditCommand = new RelayCommand(ExecuteEdit, CanExecuteEdit);
            DeleteCommand = new RelayCommand(ExecuteDeleteAsync, CanExecuteDelete);
            RefreshCommand = new RelayCommand(ExecuteRefreshAsync);
            ShowHelpCommand = new RelayCommand(ExecuteShowHelp);

            // Initialize data
            _ = InitializeAsync();
        }

        #region Properties

        public Grower CurrentGrower
        {
            get => _currentGrower;
            set
            {
                if (_currentGrower != value)
                {
                    if (_currentGrower != null)
                        _currentGrower.PropertyChanged -= OnGrowerPropertyChanged;

                    _currentGrower = value;

                    if (_currentGrower != null)
                        _currentGrower.PropertyChanged += OnGrowerPropertyChanged;

                    OnPropertyChanged();
                    UpdateCommandStates();
                }
            }
        }

        public GrowerStatistics GrowerStatistics
        {
            get => _growerStatistics;
            set
            {
                if (_growerStatistics != value)
                {
                    _growerStatistics = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (_isEditMode != value)
                {
                    _isEditMode = value;
                    OnPropertyChanged();
                    UpdateCommandStates();
                }
            }
        }

        public bool IsNewGrower
        {
            get => _isNewGrower;
            set
            {
                if (_isNewGrower != value)
                {
                    _isNewGrower = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly != value)
                {
                    _isReadOnly = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsEditable));
                    UpdateCommandStates();
                }
            }
        }

        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    OnPropertyChanged();
                    (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsEditable => !IsReadOnly && (IsEditMode || IsNewGrower);

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                    
                    // Load history data when history tab is selected
                    if (value == 3 && CurrentGrower?.GrowerId > 0) // Tab 4 (History) is index 3
                    {
                        _ = LoadHistoryDataAsync();
                    }
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                var baseTitle = IsNewGrower ? "New Grower" : 
                               CurrentGrower?.GrowerId > 0 ? $"Grower #{CurrentGrower.GrowerNumber} - {CurrentGrower.FullName}" : 
                               "Grower Details";
                
                if (HasUnsavedChanges)
                    return $"{baseTitle} *";
                
                return baseTitle;
            }
        }

        // Lookup collections
        public ObservableCollection<PaymentGroup> PaymentGroups { get; }
        public ObservableCollection<PaymentMethod> PaymentMethods { get; }
        public ObservableCollection<Depot> Depots { get; }
        public ObservableCollection<PriceClass> PriceClasses { get; }
        public List<string> Provinces { get; }
        public List<string> CurrencyCodes { get; }

        // History data
        public ObservableCollection<Receipt> RecentReceipts { get; }
        public ObservableCollection<Payment> RecentPayments { get; }

        // Validation
        public Dictionary<string, string> ValidationErrors
        {
            get => _validationErrors;
            set
            {
                if (_validationErrors != value)
                {
                    _validationErrors = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasValidationErrors));
                }
            }
        }

        public bool HasValidationErrors => ValidationErrors?.Any() == true;

        public bool IsValidating
        {
            get => _isValidating;
            set
            {
                if (_isValidating != value)
                {
                    _isValidating = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                if (_hasUnsavedChanges != value)
                {
                    _hasUnsavedChanges = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowHelpCommand { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the parent host for navigation purposes.
        /// </summary>
        public void SetParentHost(GrowerManagementHostViewModel parentHost)
        {
            _parentHost = parentHost;
        }

        /// <summary>
        /// Creates a new grower.
        /// </summary>
        public async Task CreateNewGrowerAsync()
        {
            CurrentGrower = new Grower
            {
                CurrencyCode = "CAD",
                IsActive = true,
                IsOnHold = false,
                ChargeGST = true,
                CreatedAt = DateTime.Now,
                CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
            };

            _originalGrower = CurrentGrower.Clone();
            IsNewGrower = true;
            IsEditMode = true;
            IsReadOnly = false;
            IsDirty = false;
            ValidationErrors.Clear();

            // Load price classes filtered by currency
            await LoadPriceClassesByCurrencyAsync();

            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(IsEditable));
        }

        /// <summary>
        /// Loads a grower for viewing or editing.
        /// </summary>
        public async Task LoadGrowerAsync(int growerId, bool editMode)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading grower...";

                var grower = await _growerService.GetGrowerByIdAsync(growerId);
                if (grower == null)
                {
                    StatusMessage = "Grower not found";
                    await _dialogService.ShowMessageBoxAsync("Grower not found.", "Error");
                    return;
                }

                CurrentGrower = grower;
                _originalGrower = grower.Clone();
                IsNewGrower = false;
                IsEditMode = editMode;
                IsReadOnly = !editMode;
                IsDirty = false;
                ValidationErrors.Clear();

                // Load price classes filtered by currency
                await LoadPriceClassesByCurrencyAsync();

                // Notify UI of property changes
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(IsEditable));
                StatusMessage = "Grower loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading grower";
                Logger.Error("Error loading grower in GrowerDetailViewModel", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading grower: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            await LoadLookupDataAsync();
        }

        private async Task LoadLookupDataAsync()
        {
            try
            {
                Logger.Info("Starting to load lookup data in GrowerDetailViewModel");

                // Load Payment Groups
                try
                {
                    var paymentGroups = await _payGroupService.GetAllPaymentGroupsAsync();
                    PaymentGroups.Clear();
                    foreach (var group in paymentGroups)
                    {
                        PaymentGroups.Add(group);
                    }
                    Logger.Info($"Loaded {PaymentGroups.Count} payment groups");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading payment groups", ex);
                    StatusMessage = "Error loading payment groups";
                }

                // Load Payment Methods
                try
                {
                    var paymentMethods = await _paymentMethodService.GetAllPaymentMethodsAsync();
                    PaymentMethods.Clear();
                    foreach (var method in paymentMethods)
                    {
                        PaymentMethods.Add(method);
                        Logger.Info($"Payment Method: ID={method.PaymentMethodId}, Name={method.MethodName}, Active={method.IsActive}");
                    }
                    Logger.Info($"Loaded {PaymentMethods.Count} payment methods");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading payment methods", ex);
                    StatusMessage = "Error loading payment methods";
                }

                // Load Depots
                try
                {
                    var depots = await _depotService.GetAllDepotsAsync();
                    Depots.Clear();
                    foreach (var depot in depots)
                    {
                        Depots.Add(depot);
                    }
                    Logger.Info($"Loaded {Depots.Count} depots");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading depots", ex);
                    StatusMessage = "Error loading depots";
                }

                // Load Price Classes (will be filtered by currency when grower is loaded)
                try
                {
                    var priceClasses = await _priceClassService.GetAllPriceClassesAsync();
                    PriceClasses.Clear();
                    foreach (var priceClass in priceClasses)
                    {
                        PriceClasses.Add(priceClass);
                        Logger.Info($"Price Class: ID={priceClass.PriceClassId}, Code={priceClass.ClassCode}, Name={priceClass.ClassName}, Active={priceClass.IsActive}");
                    }
                    Logger.Info($"Loaded {PriceClasses.Count} price classes");
                }
                catch (Exception ex)
                {
                    Logger.Error("Error loading price classes", ex);
                    StatusMessage = "Error loading price classes";
                }

                Logger.Info("Completed loading lookup data in GrowerDetailViewModel");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading lookup data in GrowerDetailViewModel", ex);
                StatusMessage = "Error loading lookup data";
            }
        }

        private async Task LoadHistoryDataAsync()
        {
            if (CurrentGrower?.GrowerId <= 0) return;

            try
            {
                IsLoading = true;
                StatusMessage = "Loading grower history...";

                // Load statistics
                GrowerStatistics = await _growerService.GetGrowerStatisticsAsync(CurrentGrower!.GrowerId);

                // Load recent receipts
                var receipts = await _growerService.GetGrowerRecentReceiptsAsync(CurrentGrower!.GrowerId, 10);
                RecentReceipts.Clear();
                foreach (var receipt in receipts)
                {
                    RecentReceipts.Add(receipt);
                }

                // Load recent payments
                var payments = await _growerService.GetGrowerRecentPaymentsAsync(CurrentGrower!.GrowerId, 10);
                RecentPayments.Clear();
                foreach (var payment in payments)
                {
                    RecentPayments.Add(payment);
                }

                StatusMessage = "History loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading history";
                Logger.Error("Error loading grower history in GrowerDetailViewModel", ex);
                // Don't show error to user - just log it
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void OnGrowerPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_originalGrower != null && CurrentGrower != null)
            {
                IsDirty = CurrentGrower.HasChanges(_originalGrower);
                HasUnsavedChanges = IsDirty;
            }

            // Trigger real-time validation for specific fields
            if (e.PropertyName != null)
            {
                _ = ValidateFieldAsync(e.PropertyName);
            }

            // If currency changed, reload price classes
            if (e.PropertyName == nameof(CurrentGrower.CurrencyCode))
            {
                _ = LoadPriceClassesByCurrencyAsync();
            }
        }

        private void UpdateCommandStates()
        {
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private async Task<bool> ValidateGrowerAsync()
        {
            try
            {
                IsValidating = true;
                var errors = await _growerService.ValidateGrowerAsync(CurrentGrower);
                ValidationErrors = errors;
                return !errors.Any();
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating grower in GrowerDetailViewModel", ex);
                StatusMessage = "Error validating grower data";
                return false;
            }
            finally
            {
                IsValidating = false;
            }
        }

        private async Task ValidateFieldAsync(string propertyName)
        {
            if (CurrentGrower == null || IsValidating) return;

            try
            {
                IsValidating = true;
                var errors = await _growerService.ValidateGrowerAsync(CurrentGrower);
                
                // Update only the specific field error
                var currentErrors = new Dictionary<string, string>(ValidationErrors);
                if (errors.ContainsKey(propertyName))
                {
                    currentErrors[propertyName] = errors[propertyName];
                }
                else
                {
                    currentErrors.Remove(propertyName);
                }
                
                ValidationErrors = currentErrors;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating field {propertyName} in GrowerDetailViewModel", ex);
            }
            finally
            {
                IsValidating = false;
            }
        }

        public async Task<bool> CanNavigateAwayAsync()
        {
            if (!HasUnsavedChanges) return true;

            try
            {
                var result = await _dialogService.ShowConfirmationDialogAsync(
                    "You have unsaved changes. Are you sure you want to leave without saving?",
                    "Unsaved Changes");

                if (result)
                {
                    HasUnsavedChanges = false;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error showing navigation confirmation dialog", ex);
                return false;
            }
        }

        private async Task LoadPriceClassesByCurrencyAsync()
        {
            if (CurrentGrower?.CurrencyCode == null) 
            {
                Logger.Warn("CurrentGrower or CurrencyCode is null, skipping currency-based price class loading");
                return;
            }

            try
            {
                Logger.Info($"Loading price classes for currency: {CurrentGrower.CurrencyCode}");
                
                var priceClasses = await _priceClassService.GetPriceClassesByCurrencyAsync(CurrentGrower.CurrencyCode);
                PriceClasses.Clear();
                foreach (var priceClass in priceClasses)
                {
                    PriceClasses.Add(priceClass);
                }
                
                Logger.Info($"Loaded {PriceClasses.Count} price classes for currency {CurrentGrower.CurrencyCode}");
                
                // If no price classes found, load all active ones as fallback
                if (PriceClasses.Count == 0)
                {
                    Logger.Warn($"No price classes found for currency {CurrentGrower.CurrencyCode}, loading all active price classes as fallback");
                    var allPriceClasses = await _priceClassService.GetAllPriceClassesAsync();
                    PriceClasses.Clear();
                    foreach (var priceClass in allPriceClasses)
                    {
                        PriceClasses.Add(priceClass);
                    }
                    Logger.Info($"Loaded {PriceClasses.Count} price classes as fallback");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading price classes for currency {CurrentGrower.CurrencyCode}", ex);
                StatusMessage = "Error loading price classes";
                
                // Fallback: load all price classes
                try
                {
                    Logger.Info("Attempting to load all price classes as fallback");
                    var allPriceClasses = await _priceClassService.GetAllPriceClassesAsync();
                    PriceClasses.Clear();
                    foreach (var priceClass in allPriceClasses)
                    {
                        PriceClasses.Add(priceClass);
                    }
                    Logger.Info($"Loaded {PriceClasses.Count} price classes as fallback");
                }
                catch (Exception fallbackEx)
                {
                    Logger.Error("Error loading all price classes as fallback", fallbackEx);
                }
            }
        }

        #endregion

        #region Command Implementations

        private bool CanExecuteSave(object parameter)
        {
            return IsDirty && (IsEditMode || IsNewGrower) && !HasValidationErrors;
        }

        private async Task ExecuteSaveAsync(object parameter)
        {
            try
            {
                // Validate before saving
                var isValid = await ValidateGrowerAsync();
                if (!isValid)
                {
                    StatusMessage = "Please fix validation errors before saving";
                    await _dialogService.ShowMessageBoxAsync(
                        "Please fix the validation errors before saving. Check the highlighted fields for details.",
                        "Validation Errors");
                    return;
                }

                IsLoading = true;
                StatusMessage = IsNewGrower ? "Creating grower..." : "Saving grower...";

                // Use a timeout to prevent hanging
                using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    if (IsNewGrower)
                    {
                        var newGrowerId = await _growerService.CreateGrowerAsync(CurrentGrower);
                        CurrentGrower.GrowerId = newGrowerId;
                        IsNewGrower = false;
                        Logger.Info($"Successfully created new grower with ID: {newGrowerId}");
                    }
                    else
                    {
                        var success = await _growerService.UpdateGrowerAsync(CurrentGrower);
                        if (!success)
                        {
                            throw new InvalidOperationException("Failed to update grower. The record may have been modified by another user.");
                        }
                        Logger.Info($"Successfully updated grower with ID: {CurrentGrower.GrowerId}");
                    }
                }

                _originalGrower = CurrentGrower.Clone();
                IsDirty = false;
                HasUnsavedChanges = false;
                IsEditMode = false;
                IsReadOnly = true;

                OnPropertyChanged(nameof(WindowTitle));
                StatusMessage = "Grower saved successfully";

                // Show success message
                await _dialogService.ShowMessageBoxAsync("Grower saved successfully.", "Save Success");

                // Navigate back to list
                _parentHost?.NavigateBackToList();
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                StatusMessage = "Save operation timed out";
                Logger.Error("Save operation timed out in GrowerDetailViewModel");
                await _dialogService.ShowMessageBoxAsync(
                    "The save operation timed out. Please try again. If the problem persists, contact support.",
                    "Save Timeout");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error saving grower";
                Logger.Error("Error saving grower in GrowerDetailViewModel", ex);
                
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nDetails: {ex.InnerException.Message}";
                }
                
                await _dialogService.ShowMessageBoxAsync(
                    $"Error saving grower:\n\n{errorMessage}\n\nPlease try again or contact support if the problem persists.",
                    "Save Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteCancel(object parameter)
        {
            try
            {
                var canNavigate = await CanNavigateAwayAsync();
                if (canNavigate)
                {
                    _parentHost?.NavigateBackToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in ExecuteCancel", ex);
                StatusMessage = "Error canceling operation";
            }
        }

        private bool CanExecuteEdit(object parameter)
        {
            return !IsNewGrower && !IsEditMode && !IsReadOnly;
        }

        private void ExecuteEdit(object parameter)
        {
            IsEditMode = true;
            IsReadOnly = false;
            StatusMessage = "Edit mode enabled";
        }

        private bool CanExecuteDelete(object parameter)
        {
            return !IsNewGrower && CurrentGrower?.GrowerId > 0;
        }

        private async Task ExecuteDeleteAsync(object parameter)
        {
            if (CurrentGrower == null) return;

            bool result = await _dialogService.ShowConfirmationDialogAsync(
                $"Are you sure you want to delete grower {CurrentGrower.FullName}?\n\nThis action cannot be undone.",
                "Confirm Delete");

            if (result)
            {
                try
                {
                    IsLoading = true;
                    StatusMessage = "Deleting grower...";

                    await _growerService.DeleteGrowerAsync(CurrentGrower.GrowerId);
                    StatusMessage = "Grower deleted successfully";

                    // Navigate back to list
                    _parentHost?.NavigateBackToList();
                }
                catch (Exception ex)
                {
                    StatusMessage = "Error deleting grower";
                    Logger.Error("Error deleting grower in GrowerDetailViewModel", ex);
                    await _dialogService.ShowMessageBoxAsync($"Error deleting grower: {ex.Message}", "Delete Error");
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private async Task ExecuteRefreshAsync(object parameter)
        {
            if (CurrentGrower?.GrowerId > 0)
            {
                await LoadGrowerAsync(CurrentGrower.GrowerId, IsEditMode);
            }
        }

        private void ExecuteShowHelp(object parameter)
        {
            // TODO: Implement help functionality
            StatusMessage = "Help functionality not yet implemented";
        }

        #endregion
    }
}
