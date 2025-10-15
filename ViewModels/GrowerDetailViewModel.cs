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
        private GrowerManagementHostViewModel _parentHost;

        // Grower data
        private Grower _currentGrower;
        private Grower _originalGrower;
        private GrowerStatistics _growerStatistics;

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

        // UI State
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private int _selectedTabIndex;

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
                if (IsNewGrower)
                    return "New Grower";
                if (CurrentGrower?.GrowerId > 0)
                    return $"Grower #{CurrentGrower.GrowerNumber} - {CurrentGrower.FullName}";
                return "Grower Details";
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
        public void CreateNewGrower()
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
                // Load Payment Groups
                var paymentGroups = await _payGroupService.GetAllPaymentGroupsAsync();
                PaymentGroups.Clear();
                foreach (var group in paymentGroups)
                {
                    PaymentGroups.Add(group);
                }

                // Load Payment Methods
                var paymentMethods = await _paymentMethodService.GetAllPaymentMethodsAsync();
                PaymentMethods.Clear();
                foreach (var method in paymentMethods)
                {
                    PaymentMethods.Add(method);
                }

                // Load Depots
                var depots = await _depotService.GetAllDepotsAsync();
                Depots.Clear();
                foreach (var depot in depots)
                {
                    Depots.Add(depot);
                }

                // Load Price Classes
                var priceClasses = await _priceClassService.GetAllPriceClassesAsync();
                PriceClasses.Clear();
                foreach (var priceClass in priceClasses)
                {
                    PriceClasses.Add(priceClass);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading lookup data in GrowerDetailViewModel", ex);
                // Don't show error to user - just log it
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
                GrowerStatistics = await _growerService.GetGrowerStatisticsAsync(CurrentGrower.GrowerId);

                // Load recent receipts
                var receipts = await _growerService.GetGrowerRecentReceiptsAsync(CurrentGrower.GrowerId, 10);
                RecentReceipts.Clear();
                foreach (var receipt in receipts)
                {
                    RecentReceipts.Add(receipt);
                }

                // Load recent payments
                var payments = await _growerService.GetGrowerRecentPaymentsAsync(CurrentGrower.GrowerId, 10);
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

        private void OnGrowerPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_originalGrower != null && CurrentGrower != null)
            {
                IsDirty = CurrentGrower.HasChanges(_originalGrower);
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
                var errors = await _growerService.ValidateGrowerAsync(CurrentGrower);
                ValidationErrors = errors;
                return !errors.Any();
            }
            catch (Exception ex)
            {
                Logger.Error("Error validating grower in GrowerDetailViewModel", ex);
                return false;
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
                    return;
                }

                IsLoading = true;
                StatusMessage = IsNewGrower ? "Creating grower..." : "Saving grower...";

                if (IsNewGrower)
                {
                    var newGrowerId = await _growerService.CreateGrowerAsync(CurrentGrower);
                    CurrentGrower.GrowerId = newGrowerId;
                    IsNewGrower = false;
                }
                else
                {
                    await _growerService.UpdateGrowerAsync(CurrentGrower);
                }

                _originalGrower = CurrentGrower.Clone();
                IsDirty = false;
                IsEditMode = false;
                IsReadOnly = true;

                OnPropertyChanged(nameof(WindowTitle));
                StatusMessage = "Grower saved successfully";

                // Show success message
                await _dialogService.ShowMessageBoxAsync("Grower saved successfully.", "Save Success");

                // Navigate back to list
                _parentHost?.NavigateBackToList();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error saving grower";
                Logger.Error("Error saving grower in GrowerDetailViewModel", ex);
                await _dialogService.ShowMessageBoxAsync($"Error saving grower: {ex.Message}", "Save Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteCancel(object parameter)
        {
            if (IsDirty)
            {
                var result = _dialogService.ShowConfirmationDialogAsync(
                    "You have unsaved changes. Are you sure you want to cancel?",
                    "Confirm Cancel").Result;

                if (!result)
                    return;
            }

            _parentHost?.NavigateBackToList();
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
