using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using WPFGrowerApp.ViewModels.Dialogs;
using WPFGrowerApp.Views;

namespace WPFGrowerApp.ViewModels
{
    public class ProcessViewModel : ViewModelBase
    {
        private readonly IProcessService _processService;
        private readonly IDialogService _dialogService; 
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IServiceProvider _serviceProvider;

        // Collections
        private ObservableCollection<Process> _processes;
        private ObservableCollection<Process> _filteredProcesses;
        
        // Filters
        private string _searchText = string.Empty;
        private string _statusFilter = "All";
        
        // UI State
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private string _lastUpdated = string.Empty;
        private Process? _selectedProcess;
        private bool _isDialogOpen = false;

        public ObservableCollection<Process> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        public ObservableCollection<Process> FilteredProcesses
        {
            get => _filteredProcesses;
            set => SetProperty(ref _filteredProcesses, value);
        }

        public Process? SelectedProcess
        {
            get => _selectedProcess;
            set => SetProperty(ref _selectedProcess, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterProcesses();
                }
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    FilterProcesses();
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
        public ICommand AddProcessCommand { get; }
        public ICommand EditProcessCommand { get; }
        public ICommand ViewProcessCommand { get; }
        public ICommand DeleteProcessCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public ProcessViewModel(
            IProcessService processService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IServiceProvider serviceProvider)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); 
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize collections
            _processes = new ObservableCollection<Process>();
            _filteredProcesses = new ObservableCollection<Process>();

            // Initialize commands
            AddProcessCommand = new RelayCommand(async o => await AddProcessAsync());
            EditProcessCommand = new RelayCommand(async o => await EditProcessAsync(o as Process));
            ViewProcessCommand = new RelayCommand(async o => await ViewProcessAsync(o as Process));
            DeleteProcessCommand = new RelayCommand(async o => await DeleteProcessAsync(o as Process));
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());
            SearchCommand = new RelayCommand(o => FilterProcesses());
            ClearFiltersCommand = new RelayCommand(o => ClearFilters());
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsExecute);
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);

            // Load data
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                Logger.Info("ProcessViewModel: Starting initialization");
                IsLoading = true;
                StatusMessage = "Loading process types...";

                // Load processes
                await LoadProcessesAsync();
                Logger.Info($"ProcessViewModel: Loaded {Processes.Count} process types, {FilteredProcesses.Count} filtered");

                StatusMessage = "Ready";
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing ProcessViewModel", ex);
                StatusMessage = "Error loading data";
                await _dialogService.ShowMessageBoxAsync($"Error loading process types: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
                Logger.Info("ProcessViewModel: Initialization completed");
            }
        }

        private async Task LoadProcessesAsync()
        {
            try
            {
                Logger.Info("ProcessViewModel: Starting to load process types from service");
                var processes = await _processService.GetAllProcessesAsync();
                Logger.Info($"ProcessViewModel: Retrieved {processes.Count()} process types from service");
                
                Processes.Clear();
                
                foreach (var process in processes.OrderBy(p => p.ProcessName))
                {
                    Processes.Add(process);
                }

                Logger.Info($"ProcessViewModel: Added {Processes.Count} process types to collection");

                // Update filtered processes
                FilterProcesses();
                Logger.Info($"ProcessViewModel: Filtered process types count: {FilteredProcesses.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading process types", ex);
                throw;
            }
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }

        private void FilterProcesses()
        {
            var baseFilter = Processes.AsEnumerable();

            // Apply status filter
            switch (StatusFilter)
            {
                case "Active":
                    baseFilter = baseFilter.Where(p => p.IsActive);
                    break;
                case "Inactive":
                    baseFilter = baseFilter.Where(p => !p.IsActive);
                    break;
                case "All":
                default:
                    // No status filtering - show all records
                    break;
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                baseFilter = baseFilter.Where(p =>
                    (p.ProcessName?.ToLower().Contains(searchLower) ?? false) ||
                    (p.Description?.ToLower().Contains(searchLower) ?? false) ||
                    (p.ProcessCode?.ToLower().Contains(searchLower) ?? false) ||
                    (p.ProcessId.ToString().Contains(searchLower)) ||
                    (p.ProcessClass.ToString().Contains(searchLower))
                );
            }

            FilteredProcesses = new ObservableCollection<Process>(baseFilter);
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusFilter = "All";
            StatusMessage = "Ready";
        }

        private async Task AddProcessAsync()
        {
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var newProcess = new Process
                {
                    ProcessId = 0,
                    ProcessCode = string.Empty,
                    ProcessName = string.Empty,
                    Description = string.Empty,
                    IsActive = true,
                    DefaultGrade = 0,
                    ProcessClass = 0
                };

                var dialogViewModel = new ProcessEditDialogViewModel(newProcess, false, _dialogService);
                var result = await DialogHost.Show(new ProcessEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.ProcessData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Adding process type...";
                    
                    try
                    {
                        bool success = await _processService.AddProcessAsync(dialogViewModel.ProcessData);
                        
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Process type added successfully.", "Success");
                            await LoadProcessesAsync();
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Failed to add the process type.", "Error");
                            StatusMessage = "Failed to add process type";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error adding process type.", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error adding process type: {ex.Message}", "Error");
                        StatusMessage = "Error adding process type";
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

        private async Task ViewProcessAsync(Process? process)
        {
            if (process == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new ProcessEditDialogViewModel(process, true, _dialogService);
                await DialogHost.Show(new ProcessEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task EditProcessAsync(Process? process)
        {
            if (process == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new ProcessEditDialogViewModel(process, false, _dialogService);
                var result = await DialogHost.Show(new ProcessEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.ProcessData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Updating process type...";
                    
                    try
                    {
                        bool success = await _processService.UpdateProcessAsync(dialogViewModel.ProcessData);
                        
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Process type updated successfully.", "Success");
                            await LoadProcessesAsync();
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Failed to update the process type.", "Error");
                            StatusMessage = "Failed to update process type";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error updating process type {dialogViewModel.ProcessData.ProcessId}.", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error updating process type: {ex.Message}", "Error");
                        StatusMessage = "Error updating process type";
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

        private async Task DeleteProcessAsync(Process? process)
        {
            if (process == null) return;

            var confirm = await _dialogService.ShowConfirmationDialogAsync(
                $"Are you sure you want to delete process type '{process.ProcessName}' ({process.ProcessId})?", 
                "Confirm Delete");
            
            if (confirm != true) return;

            IsLoading = true;
            StatusMessage = "Deleting process type...";
            
            try
            {
                bool success = await _processService.DeleteProcessAsync(process.ProcessId, App.CurrentUser?.Username ?? "SYSTEM");

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync("Process type deleted successfully.", "Success");
                    await LoadProcessesAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to delete the process type.", "Error");
                    StatusMessage = "Failed to delete process type";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting process type {process.ProcessId}.", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting process type: {ex.Message}", "Error");
                StatusMessage = "Error deleting process type";
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

        private void ShowHelpExecute(object? parameter)
        {
            var helpMessage = @"Process Types Management Help

Keyboard Shortcuts:
- F5: Refresh process types list
- F1: Show this help

Actions:
- Add New: Create a new process type
- View: View process type details (read-only)
- Edit: Modify process type information
- Delete: Remove process type from system

Search:
- Type in search box to filter process types
- Search looks in Process Name, Description, Code, ID, and Process Class fields
- Press Enter or click Search button
- Click Clear to reset search

Process Type Fields:
- ID: Unique identifier (numeric)
- Process Name: Process type name (required)
- Description: Process type description (optional)
- Code: Process code (up to 10 characters)
- Default Grade: Default grade (1-3)
- Process Class: Process classification (1-4)
- Is Active: Whether the process is active
- Display Order: Order for UI display
- Grade Names: Custom names for grades 1-3";

            _dialogService?.ShowMessageBoxAsync(helpMessage, "Help");
        }
    }
}