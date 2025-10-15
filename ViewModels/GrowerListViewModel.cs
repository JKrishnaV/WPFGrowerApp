using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the grower list view following ProductView/ProcessView pattern.
    /// Displays growers in a searchable, filterable list with statistics.
    /// </summary>
    public class GrowerListViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private GrowerManagementHostViewModel _parentHost;

        // Collections
        private ObservableCollection<GrowerSearchResult> _growers;
        private ObservableCollection<GrowerSearchResult> _filteredGrowers;
        
        // Filters
        private string _searchText = string.Empty;
        private string _selectedProvince = "All";
        private string _selectedPaymentGroup = "All";
        private string _selectedStatus = "All";
        
        // UI State
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private string _lastUpdated = string.Empty;
        private GrowerSearchResult _selectedGrower;

        // Statistics
        private int _totalGrowers;
        private int _activeGrowers;
        private int _onHoldGrowers;

        public GrowerListViewModel(
            IGrowerService growerService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));

            // Initialize collections
            Growers = new ObservableCollection<GrowerSearchResult>();
            FilteredGrowers = new ObservableCollection<GrowerSearchResult>();

            // Initialize filter options
            InitializeFilterOptions();

            // Initialize commands
            SearchCommand = new RelayCommand(ExecuteSearchAsync);
            ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
            AddNewGrowerCommand = new RelayCommand(ExecuteAddNewGrower);
            EditGrowerCommand = new RelayCommand(ExecuteEditGrower, CanExecuteEditGrower);
            ViewGrowerCommand = new RelayCommand(ExecuteViewGrower, CanExecuteViewGrower);
            DeleteGrowerCommand = new RelayCommand(ExecuteDeleteGrower, CanExecuteDeleteGrower);
            RefreshCommand = new RelayCommand(ExecuteRefreshAsync);
            ExportCommand = new RelayCommand(ExecuteExport);
            ShowHelpCommand = new RelayCommand(ExecuteShowHelp);

            // Initialize data
            _ = InitializeAsync();
        }

        #region Properties

        public ObservableCollection<GrowerSearchResult> Growers
        {
            get => _growers;
            set
            {
                if (_growers != value)
                {
                    _growers = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<GrowerSearchResult> FilteredGrowers
        {
            get => _filteredGrowers;
            set
            {
                if (_filteredGrowers != value)
                {
                    _filteredGrowers = value;
                    OnPropertyChanged();
                    UpdateRecordCount();
                }
            }
        }

        public GrowerSearchResult SelectedGrower
        {
            get => _selectedGrower;
            set
            {
                if (_selectedGrower != value)
                {
                    _selectedGrower = value;
                    OnPropertyChanged();
                    UpdateCommandStates();
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public string SelectedProvince
        {
            get => _selectedProvince;
            set
            {
                if (_selectedProvince != value)
                {
                    _selectedProvince = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public string SelectedPaymentGroup
        {
            get => _selectedPaymentGroup;
            set
            {
                if (_selectedPaymentGroup != value)
                {
                    _selectedPaymentGroup = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

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

        public string LastUpdated
        {
            get => _lastUpdated;
            set
            {
                if (_lastUpdated != value)
                {
                    _lastUpdated = value;
                    OnPropertyChanged();
                }
            }
        }

        // Statistics
        public int TotalGrowers
        {
            get => _totalGrowers;
            set
            {
                if (_totalGrowers != value)
                {
                    _totalGrowers = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ActiveGrowers
        {
            get => _activeGrowers;
            set
            {
                if (_activeGrowers != value)
                {
                    _activeGrowers = value;
                    OnPropertyChanged();
                }
            }
        }

        public int OnHoldGrowers
        {
            get => _onHoldGrowers;
            set
            {
                if (_onHoldGrowers != value)
                {
                    _onHoldGrowers = value;
                    OnPropertyChanged();
                }
            }
        }

        public string RecordCount => FilteredGrowers.Count.ToString();

        #endregion

        #region Filter Options

        public ObservableCollection<string> ProvinceOptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> PaymentGroupOptions { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>();

        #endregion

        #region Commands

        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand AddNewGrowerCommand { get; }
        public ICommand EditGrowerCommand { get; }
        public ICommand ViewGrowerCommand { get; }
        public ICommand DeleteGrowerCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportCommand { get; }
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
        /// Navigates to the dashboard (delegated to parent host).
        /// </summary>
        public void NavigateToDashboard()
        {
            // This would be handled by the parent host or MainViewModel
            Logger.Info("Navigate to dashboard requested from grower list view");
        }

        #endregion

        #region Private Methods

        private void InitializeFilterOptions()
        {
            // Status options
            StatusOptions.Clear();
            StatusOptions.Add("All");
            StatusOptions.Add("Active");
            StatusOptions.Add("On Hold");
            StatusOptions.Add("Inactive");

            // Province options (will be populated from database)
            ProvinceOptions.Clear();
            ProvinceOptions.Add("All");

            // Payment group options (will be populated from database)
            PaymentGroupOptions.Clear();
            PaymentGroupOptions.Add("All");
        }

        private async Task InitializeAsync()
        {
            await LoadGrowersAsync();
            await LoadFilterOptionsAsync();
        }

        private async Task LoadGrowersAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading growers...";

                var growers = await _growerService.GetAllGrowersForListAsync();
                
                Growers.Clear();
                foreach (var grower in growers)
                {
                    // Set status display
                    if (grower.IsOnHold)
                        grower.Status = "On Hold";
                    else if (grower.IsActive)
                        grower.Status = "Active";
                    else
                        grower.Status = "Inactive";

                    Growers.Add(grower);
                }

                ApplyFilters();
                await LoadStatisticsAsync();
                StatusMessage = "Growers loaded successfully";
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading growers";
                Logger.Error("Error loading growers in GrowerListViewModel", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading growers: {ex.Message}", "Loading Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                TotalGrowers = await _growerService.GetTotalGrowersCountAsync();
                ActiveGrowers = await _growerService.GetActiveGrowersCountAsync();
                OnHoldGrowers = await _growerService.GetOnHoldGrowersCountAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading grower statistics", ex);
                // Don't show error to user for statistics - just log it
            }
        }

        private async Task LoadFilterOptionsAsync()
        {
            try
            {
                // Load provinces
                var provinces = await _growerService.GetUniqueProvincesAsync();
                foreach (var province in provinces.OrderBy(p => p))
                {
                    if (!ProvinceOptions.Contains(province))
                        ProvinceOptions.Add(province);
                }

                // TODO: Load payment groups when service is available
                // For now, we'll use placeholder data
                PaymentGroupOptions.Add("STD");
                PaymentGroupOptions.Add("PREM");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading filter options", ex);
                // Don't show error to user - just log it
            }
        }

        private void ApplyFilters()
        {
            var filtered = Growers.AsEnumerable();

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(g => 
                    g.GrowerName?.ToLower().Contains(searchLower) == true ||
                    g.GrowerNumber.ToString().Contains(searchLower) ||
                    g.ChequeName?.ToLower().Contains(searchLower) == true ||
                    g.City?.ToLower().Contains(searchLower) == true ||
                    g.Phone?.Contains(searchLower) == true ||
                    g.Email?.ToLower().Contains(searchLower) == true);
            }

            // Apply province filter
            if (SelectedProvince != "All")
            {
                filtered = filtered.Where(g => g.Province == SelectedProvince);
            }

            // Apply payment group filter
            if (SelectedPaymentGroup != "All")
            {
                filtered = filtered.Where(g => g.PaymentGroupCode == SelectedPaymentGroup);
            }

            // Apply status filter
            if (SelectedStatus != "All")
            {
                filtered = filtered.Where(g => g.Status == SelectedStatus);
            }

            FilteredGrowers.Clear();
            foreach (var grower in filtered)
            {
                FilteredGrowers.Add(grower);
            }

            UpdateRecordCount();
        }

        private void UpdateRecordCount()
        {
            OnPropertyChanged(nameof(RecordCount));
        }

        private void UpdateCommandStates()
        {
            (EditGrowerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ViewGrowerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteGrowerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteSearchAsync(object parameter)
        {
            ApplyFilters();
            StatusMessage = $"Search completed. Found {RecordCount} growers.";
        }

        private void ExecuteClearFilters(object parameter)
        {
            SearchText = string.Empty;
            SelectedProvince = "All";
            SelectedPaymentGroup = "All";
            SelectedStatus = "All";
            ApplyFilters();
            StatusMessage = "Filters cleared";
        }

        private void ExecuteAddNewGrower(object parameter)
        {
            _parentHost?.NavigateToDetail(null, true); // null = new grower, true = edit mode
        }

        private bool CanExecuteEditGrower(object parameter)
        {
            return SelectedGrower != null;
        }

        private void ExecuteEditGrower(object parameter)
        {
            if (SelectedGrower != null)
            {
                _parentHost?.NavigateToDetail(SelectedGrower.GrowerId, true);
            }
        }

        private bool CanExecuteViewGrower(object parameter)
        {
            return SelectedGrower != null;
        }

        private void ExecuteViewGrower(object parameter)
        {
            if (SelectedGrower != null)
            {
                _parentHost?.NavigateToDetail(SelectedGrower.GrowerId, false); // false = view mode
            }
        }

        private bool CanExecuteDeleteGrower(object parameter)
        {
            return SelectedGrower != null;
        }

        private async Task ExecuteDeleteGrower(object parameter)
        {
            if (SelectedGrower == null) return;

            bool result = await _dialogService.ShowConfirmationDialogAsync(
                $"Are you sure you want to delete grower {SelectedGrower.GrowerName}?\n\nThis action cannot be undone.",
                "Confirm Delete");

            if (result)
            {
                try
                {
                    await _growerService.DeleteGrowerAsync(SelectedGrower.GrowerId);
                    await LoadGrowersAsync(); // Reload the list
                    StatusMessage = $"Grower {SelectedGrower.GrowerName} deleted successfully";
                }
                catch (Exception ex)
                {
                    StatusMessage = "Error deleting grower";
                    Logger.Error("Error deleting grower in GrowerListViewModel", ex);
                    await _dialogService.ShowMessageBoxAsync($"Error deleting grower: {ex.Message}", "Delete Error");
                }
            }
        }

        private async Task ExecuteRefreshAsync(object parameter)
        {
            await LoadGrowersAsync();
        }

        private void ExecuteExport(object parameter)
        {
            // TODO: Implement export functionality
            StatusMessage = "Export functionality not yet implemented";
        }

        private void ExecuteShowHelp(object parameter)
        {
            // TODO: Implement help functionality
            StatusMessage = "Help functionality not yet implemented";
        }

        #endregion
    }
}
