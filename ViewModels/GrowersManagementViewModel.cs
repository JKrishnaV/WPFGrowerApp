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
    public class GrowersManagementViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        private readonly IDialogService _dialogService;
        
        private ObservableCollection<GrowerSearchResult> _allGrowers;
        private ObservableCollection<GrowerSearchResult> _filteredGrowers;
        private GrowerSearchResult _selectedGrower;
        private string _searchText;
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private string _recordCount = "0";
        private string _lastUpdated;

        // Filter properties
        private DateRangeOption _selectedDateRange;
        private StatusOption _selectedStatus;
        private LocationOption _selectedLocation;

        public GrowersManagementViewModel(
            IGrowerService growerService, 
            IDialogService dialogService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            AllGrowers = new ObservableCollection<GrowerSearchResult>();
            FilteredGrowers = new ObservableCollection<GrowerSearchResult>();
            
            // Initialize filter options
            InitializeFilterOptions();
            
            // Initialize commands
            SearchCommand = new RelayCommand(ExecuteSearchAsync);
            ClearSearchCommand = new RelayCommand(ExecuteClearSearch);
            AddNewGrowerCommand = new RelayCommand(ExecuteAddNewGrower);
            EditGrowerCommand = new RelayCommand(ExecuteEditGrower, CanExecuteEditGrower);
            DeleteGrowerCommand = new RelayCommand(ExecuteDeleteGrower, CanExecuteDeleteGrower);
            ViewGrowerDetailsCommand = new RelayCommand(ExecuteViewGrowerDetails, CanExecuteViewGrowerDetails);
            ExportGrowersCommand = new RelayCommand(ExecuteExportGrowers);
            RefreshCommand = new RelayCommand(ExecuteRefreshAsync);
            NavigateToDashboardCommand = new RelayCommand(ExecuteNavigateToDashboard);

            // Set initial filter selections
            SelectedDateRange = DateRangeOptions.FirstOrDefault();
            SelectedStatus = StatusOptions.FirstOrDefault();
            SelectedLocation = LocationOptions.FirstOrDefault();

            // Initialize data
            _ = InitializeAsync();
        }

        #region Properties

        public ObservableCollection<GrowerSearchResult> AllGrowers
        {
            get => _allGrowers;
            set
            {
                if (_allGrowers != value)
                {
                    _allGrowers = value;
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

        public string RecordCount
        {
            get => _recordCount;
            set
            {
                if (_recordCount != value)
                {
                    _recordCount = value;
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

        #endregion

        #region Filter Properties

        public ObservableCollection<DateRangeOption> DateRangeOptions { get; } = new ObservableCollection<DateRangeOption>();
        public ObservableCollection<StatusOption> StatusOptions { get; } = new ObservableCollection<StatusOption>();
        public ObservableCollection<LocationOption> LocationOptions { get; } = new ObservableCollection<LocationOption>();

        public DateRangeOption SelectedDateRange
        {
            get => _selectedDateRange;
            set
            {
                if (_selectedDateRange != value)
                {
                    _selectedDateRange = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        public StatusOption SelectedStatus
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

        public LocationOption SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (_selectedLocation != value)
                {
                    _selectedLocation = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand AddNewGrowerCommand { get; }
        public ICommand EditGrowerCommand { get; }
        public ICommand DeleteGrowerCommand { get; }
        public ICommand ViewGrowerDetailsCommand { get; }
        public ICommand ExportGrowersCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }

        #endregion

        #region Private Methods

        private void InitializeFilterOptions()
        {
            // Date range options
            DateRangeOptions.Add(new DateRangeOption { DisplayName = "All Time", Value = "all" });
            DateRangeOptions.Add(new DateRangeOption { DisplayName = "Last 30 Days", Value = "30" });
            DateRangeOptions.Add(new DateRangeOption { DisplayName = "Last 90 Days", Value = "90" });
            DateRangeOptions.Add(new DateRangeOption { DisplayName = "This Year", Value = "year" });

            // Status options
            StatusOptions.Add(new StatusOption { DisplayName = "All Status", Value = "all" });
            StatusOptions.Add(new StatusOption { DisplayName = "Active", Value = "active" });
            StatusOptions.Add(new StatusOption { DisplayName = "Inactive", Value = "inactive" });
            StatusOptions.Add(new StatusOption { DisplayName = "On Hold", Value = "hold" });

            // Location options
            LocationOptions.Add(new LocationOption { DisplayName = "All Locations", Value = "all" });
            LocationOptions.Add(new LocationOption { DisplayName = "BC", Value = "BC" });
            LocationOptions.Add(new LocationOption { DisplayName = "AB", Value = "AB" });
            LocationOptions.Add(new LocationOption { DisplayName = "ON", Value = "ON" });
            LocationOptions.Add(new LocationOption { DisplayName = "Other", Value = "other" });
        }

        private async Task InitializeAsync()
        {
            await LoadGrowersAsync();
        }

        private async Task LoadGrowersAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading growers...";

                var growers = await _growerService.GetAllGrowersAsync();
                
                AllGrowers.Clear();
                foreach (var grower in growers)
                {
                    // Set Status based on IsOnHold property
                    grower.IsActive = !grower.IsOnHold;
                    grower.Status = grower.IsOnHold ? "On Hold" : "Active";
                    AllGrowers.Add(grower);
                }

                ApplyFilters();
                StatusMessage = "Growers loaded successfully";
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading growers";
                Logger.Error("Error loading growers in GrowersManagementViewModel", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading growers: {ex.Message}", "Loading Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = AllGrowers.AsEnumerable();

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(g => 
                    g.GrowerName?.ToLower().Contains(searchLower) == true ||
                    g.GrowerNumber.ToString().Contains(searchLower) ||
                    g.ChequeName?.ToLower().Contains(searchLower) == true ||
                    g.City?.ToLower().Contains(searchLower) == true);
            }

            // Apply status filter
            if (SelectedStatus?.Value != "all")
            {
                filtered = filtered.Where(g => g.Status?.Equals(SelectedStatus.Value, StringComparison.OrdinalIgnoreCase) == true);
            }

            // Apply location filter
            if (SelectedLocation?.Value != "all")
            {
                filtered = filtered.Where(g => 
                {
                    // This would need to be implemented based on actual location data
                    // For now, we'll use a simple province check
                    return SelectedLocation.Value == "other" || 
                           g.City?.Contains(SelectedLocation.Value) == true;
                });
            }

            FilteredGrowers.Clear();
            foreach (var grower in filtered)
            {
                FilteredGrowers.Add(grower);
            }
        }

        private void UpdateRecordCount()
        {
            RecordCount = FilteredGrowers.Count.ToString();
        }

        private void UpdateCommandStates()
        {
            (EditGrowerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteGrowerCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ViewGrowerDetailsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Command Implementations

        private async Task ExecuteSearchAsync(object parameter)
        {
            ApplyFilters();
            StatusMessage = $"Search completed. Found {RecordCount} growers.";
        }

        private void ExecuteClearSearch(object parameter)
        {
            SearchText = string.Empty;
            SelectedDateRange = DateRangeOptions.FirstOrDefault();
            SelectedStatus = StatusOptions.FirstOrDefault();
            SelectedLocation = LocationOptions.FirstOrDefault();
            ApplyFilters();
        }

        private void ExecuteAddNewGrower(object parameter)
        {
            // This will be handled by showing the grower search dialog with a "new" option
            // For now, show a message
            StatusMessage = "Add new grower functionality - opens grower edit dialog with new grower";
            // TODO: Implement navigation to grower edit view with "0" as grower number
        }

        private bool CanExecuteEditGrower(object parameter)
        {
            return SelectedGrower != null;
        }

        private void ExecuteEditGrower(object parameter)
        {
            if (SelectedGrower != null)
            {
                StatusMessage = $"Opening grower {SelectedGrower.GrowerNumber} for editing...";
                // TODO: Implement navigation to grower edit view with selected grower number
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
                $"Are you sure you want to delete grower {SelectedGrower.GrowerName}?",
                "Confirm Delete");

            if (result)
            {
                try
                {
                    // Mark grower as on hold instead of deleting
                    var grower = await _growerService.GetGrowerByNumberAsync(SelectedGrower.GrowerNumber.ToString());
                    if (grower != null)
                    {
                        grower.OnHold = true;
                        await _growerService.SaveGrowerAsync(grower);
                        await LoadGrowersAsync();
                        StatusMessage = $"Grower {SelectedGrower.GrowerName} placed on hold successfully";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = "Error placing grower on hold";
                    Logger.Error("Error placing grower on hold in GrowersManagementViewModel", ex);
                    await _dialogService.ShowMessageBoxAsync($"Error placing grower on hold: {ex.Message}", "Delete Error");
                }
            }
        }

        private bool CanExecuteViewGrowerDetails(object parameter)
        {
            return SelectedGrower != null;
        }

        private void ExecuteViewGrowerDetails(object parameter)
        {
            if (SelectedGrower != null)
            {
                StatusMessage = $"Viewing details for grower {SelectedGrower.GrowerNumber}...";
                // TODO: Implement navigation to grower view with selected grower number
            }
        }

        private void ExecuteExportGrowers(object parameter)
        {
            // TODO: Implement export functionality
            StatusMessage = "Export functionality not yet implemented";
        }

        private async Task ExecuteRefreshAsync(object parameter)
        {
            await LoadGrowersAsync();
        }

        private void ExecuteNavigateToDashboard(object parameter)
        {
            StatusMessage = "Navigation to dashboard...";
            // TODO: Implement navigation to dashboard
            // This would typically be handled by the MainViewModel
        }

        #endregion
    }

    #region Filter Option Classes

    public class DateRangeOption
    {
        public string DisplayName { get; set; }
        public string Value { get; set; }
    }

    public class StatusOption
    {
        public string DisplayName { get; set; }
        public string Value { get; set; }
    }

    public class LocationOption
    {
        public string DisplayName { get; set; }
        public string Value { get; set; }
    }

    #endregion
}
