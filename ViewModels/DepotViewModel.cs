using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.Views;

namespace WPFGrowerApp.ViewModels
{
    public class DepotViewModel : ViewModelBase
    {
        private readonly IDepotService _depotService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IServiceProvider _serviceProvider;
        
        private ObservableCollection<Depot> _depots;
        private ObservableCollection<Depot> _filteredDepots;
        private Depot _selectedDepot;
        private string _searchText = string.Empty;
        private string _statusMessage = "Ready";
        private string _lastUpdated = string.Empty;
        private bool _isLoading;
        private bool _isDialogOpen;

        public ObservableCollection<Depot> Depots
        {
            get => _depots;
            set => SetProperty(ref _depots, value);
        }

        public ObservableCollection<Depot> FilteredDepots
        {
            get => _filteredDepots;
            set => SetProperty(ref _filteredDepots, value);
        }

        public Depot SelectedDepot
        {
            get => _selectedDepot;
            set => SetProperty(ref _selectedDepot, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterDepots();
                }
            }
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

        public string LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        // Commands
        public ICommand AddDepotCommand { get; }
        public ICommand EditDepotCommand { get; }
        public ICommand ViewDepotCommand { get; }
        public ICommand DeleteDepotCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public DepotViewModel(
            IDepotService depotService, 
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IServiceProvider serviceProvider)
        {
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            Depots = new ObservableCollection<Depot>();
            FilteredDepots = new ObservableCollection<Depot>();

            // Initialize commands
            AddDepotCommand = new RelayCommand(async (param) => await AddDepotAsync());
            EditDepotCommand = new RelayCommand(async (param) => await EditDepotAsync(param as Depot));
            ViewDepotCommand = new RelayCommand(async (param) => await ViewDepotAsync(param as Depot));
            DeleteDepotCommand = new RelayCommand(async (param) => await DeleteDepotAsync(param as Depot));
            RefreshCommand = new RelayCommand(async (param) => await RefreshAsync());
            SearchCommand = new RelayCommand((param) => FilterDepots());
            ClearFiltersCommand = new RelayCommand((param) => ClearFilters());
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsExecute);
            ShowHelpCommand = new RelayCommand((param) => ShowHelpExecute());

            // Initialize data
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadDepotsAsync();
        }

        private async Task LoadDepotsAsync()
        {
            IsLoading = true;
            StatusMessage = "Loading depots...";
            
            try
            {
                var depots = await _depotService.GetAllDepotsAsync();
                Depots = new ObservableCollection<Depot>(depots.OrderBy(d => d.DepotName));
                FilterDepots();
                LastUpdated = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
                StatusMessage = "Ready";
                
                Logger.Info($"Loaded {Depots.Count} depots");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading depots.", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading depots: {ex.Message}", "Error");
                StatusMessage = "Error loading depots";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }

        private void FilterDepots()
        {
            var baseFilter = Depots.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                baseFilter = baseFilter.Where(d =>
                    (d.DepotName?.ToLower().Contains(searchLower) ?? false) ||
                    (d.DepotCode?.ToLower().Contains(searchLower) ?? false) ||
                    (d.City?.ToLower().Contains(searchLower) ?? false) ||
                    (d.Province?.ToLower().Contains(searchLower) ?? false) ||
                    (d.PhoneNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (d.DepotId.ToString().Contains(searchLower))
                );
            }

            FilteredDepots = new ObservableCollection<Depot>(baseFilter);
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusMessage = "Ready";
        }

        private async Task AddDepotAsync()
        {
            if (_isDialogOpen) return;

            try
            {
                _isDialogOpen = true;
                var newDepot = new Depot();
                var dialogViewModel = new Dialogs.DepotEditDialogViewModel(newDepot, false, _dialogService);
                var result = await DialogHost.Show(new DepotEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.DepotData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Adding depot...";
                    
                    try
                    {
                        bool success = await _depotService.AddDepotAsync(dialogViewModel.DepotData);
                        
                        if (success)
                        {
                            Logger.Info($"AddDepotAsync: Successfully added depot {dialogViewModel.DepotData.DepotId}");
                            await _dialogService.ShowMessageBoxAsync("Depot added successfully.", "Success");
                            await LoadDepotsAsync();
                        }
                        else
                        {
                            Logger.Error($"AddDepotAsync: Failed to add depot {dialogViewModel.DepotData.DepotId}. AddDepotAsync returned false - no rows were affected by the add operation.");
                            await _dialogService.ShowMessageBoxAsync("Failed to add the depot.", "Error");
                            StatusMessage = "Failed to add depot";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error adding depot {dialogViewModel.DepotData.DepotId}.", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error adding depot: {ex.Message}", "Error");
                        StatusMessage = "Error adding depot";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task ViewDepotAsync(Depot? depot)
        {
            if (depot == null) return;
            if (_isDialogOpen) return;

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new Dialogs.DepotEditDialogViewModel(depot, true, _dialogService);
                await DialogHost.Show(new DepotEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task EditDepotAsync(Depot? depot)
        {
            if (depot == null) return;
            if (_isDialogOpen) return;

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new Dialogs.DepotEditDialogViewModel(depot, false, _dialogService);
                var result = await DialogHost.Show(new DepotEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.DepotData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Updating depot...";
                    
                    try
                    {
                        bool success = await _depotService.UpdateDepotAsync(dialogViewModel.DepotData);
                        
                        if (success)
                        {
                            Logger.Info($"EditDepotAsync: Successfully updated depot {dialogViewModel.DepotData.DepotId}");
                            await _dialogService.ShowMessageBoxAsync("Depot updated successfully.", "Success");
                            await LoadDepotsAsync();
                        }
                        else
                        {
                            Logger.Error($"EditDepotAsync: Failed to update depot {dialogViewModel.DepotData.DepotId}. UpdateDepotAsync returned false - no rows were affected by the update operation.");
                            await _dialogService.ShowMessageBoxAsync("Failed to update the depot.", "Error");
                            StatusMessage = "Failed to update depot";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error updating depot {dialogViewModel.DepotData.DepotId}.", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error updating depot: {ex.Message}", "Error");
                        StatusMessage = "Error updating depot";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task DeleteDepotAsync(Depot? depot)
        {
            if (depot == null) return;

            var confirm = await _dialogService.ShowConfirmationDialogAsync(
                $"Are you sure you want to delete depot '{depot.DepotName}' ({depot.DepotId})?", 
                "Confirm Delete");
            
            if (confirm != true) return;

            IsLoading = true;
            StatusMessage = "Deleting depot...";
            
            try
            {
                bool success = await _depotService.DeleteDepotAsync(depot.DepotId, App.CurrentUser?.Username ?? "SYSTEM");

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync("Depot deleted successfully.", "Success");
                    await LoadDepotsAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to delete the depot.", "Error");
                    StatusMessage = "Failed to delete depot";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting depot {depot.DepotId}.", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting depot: {ex.Message}", "Error");
                StatusMessage = "Error deleting depot";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void NavigateToDashboardExecute(object? parameter)
        {
            try
            {
                Logger.Info("NavigateToDashboardExecute called - navigating to Dashboard");
                
                // Get the MainViewModel instance that's bound to the UI
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    Logger.Info($"MainViewModel instance: {mainViewModel.GetHashCode()}");
                    
                    // Navigate to Dashboard using the MainViewModel's navigation system
                    await mainViewModel.NavigateToAsync<DashboardViewModel>("Dashboard");
                    
                    Logger.Info($"NavigateToDashboardExecute completed successfully. CurrentViewModel: {mainViewModel.CurrentViewModel?.GetType().Name}");
                }
                else
                {
                    Logger.Error("Could not access MainViewModel from MainWindow DataContext");
                    await _dialogService.ShowMessageBoxAsync("Navigation error: Could not access main application.", "Navigation Error");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Dashboard", ex);
                await _dialogService.ShowMessageBoxAsync($"Error navigating to Dashboard: {ex.Message}", "Navigation Error");
            }
        }

        private async void NavigateToSettingsExecute(object? parameter)
        {
            try
            {
                Logger.Info("NavigateToSettingsExecute called - navigating to Settings");
                
                // Get the MainViewModel instance that's bound to the UI
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    Logger.Info($"MainViewModel instance: {mainViewModel.GetHashCode()}");
                    
                    // Navigate to Settings using the MainViewModel's navigation system
                    await mainViewModel.NavigateToAsync<SettingsHostViewModel>("Settings");
                    
                    Logger.Info($"NavigateToSettingsExecute completed successfully. CurrentViewModel: {mainViewModel.CurrentViewModel?.GetType().Name}");
                }
                else
                {
                    Logger.Error("Could not access MainViewModel from MainWindow DataContext");
                    await _dialogService.ShowMessageBoxAsync("Navigation error: Could not access main application.", "Navigation Error");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Settings", ex);
                await _dialogService.ShowMessageBoxAsync($"Error navigating to Settings: {ex.Message}", "Navigation Error");
            }
        }

        private void ShowHelpExecute()
        {
            var helpContent = _helpContentProvider.GetHelpContent("DepotView");
            _dialogService.ShowMessageBoxAsync(helpContent.Content, helpContent.Title);
        }
    }
}
