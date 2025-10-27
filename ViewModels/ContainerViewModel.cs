using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.ViewModels.Dialogs;
using WPFGrowerApp.Views.Dialogs;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for managing container types (Containers table).
    /// Provides CRUD operations for container type definitions.
    /// </summary>
    public class ContainerViewModel : ViewModelBase
    {
        private readonly IContainerTypeService _containerService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IServiceProvider _serviceProvider;
        
        private ObservableCollection<ContainerType> _containers = new();
        private ObservableCollection<ContainerType> _filteredContainers = new();
        private ContainerType? _selectedContainer;
        private string _searchText = string.Empty;
        private string _statusFilter = "All";
        private string _statusMessage = "Ready";
        private string _lastUpdated = string.Empty;
        private bool _isLoading;
        private bool _isDialogOpen;

        public ObservableCollection<ContainerType> Containers
        {
            get => _containers;
            set => SetProperty(ref _containers, value);
        }

        public ObservableCollection<ContainerType> FilteredContainers
        {
            get => _filteredContainers;
            set => SetProperty(ref _filteredContainers, value);
        }

        public ContainerType? SelectedContainer
        {
            get => _selectedContainer;
            set => SetProperty(ref _selectedContainer, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterContainers();
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
                    FilterContainers();
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
        public ICommand AddContainerCommand { get; }
        public ICommand EditContainerCommand { get; }
        public ICommand ViewContainerCommand { get; }
        public ICommand DeleteContainerCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public ContainerViewModel(
            IContainerTypeService containerService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IServiceProvider serviceProvider)
        {
            _containerService = containerService ?? throw new ArgumentNullException(nameof(containerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize commands
            AddContainerCommand = new RelayCommand(async _ => await AddContainerAsync());
            EditContainerCommand = new RelayCommand(async param => await EditContainerAsync(param as ContainerType), param => param != null);
            ViewContainerCommand = new RelayCommand(async param => await ViewContainerAsync(param as ContainerType), param => param != null);
            DeleteContainerCommand = new RelayCommand(async param => await DeleteContainerAsync(param as ContainerType), param => param != null);
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            SearchCommand = new RelayCommand(_ => FilterContainers());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            NavigateToDashboardCommand = new RelayCommand(_ => NavigateToDashboard());
            NavigateToSettingsCommand = new RelayCommand(_ => NavigateToSettings());
            ShowHelpCommand = new RelayCommand(_ => ShowHelp());

            // Load data
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            try
            {
                Logger.Info("ContainerViewModel: Starting initialization");
                IsLoading = true;
                StatusMessage = "Loading containers...";
                await LoadContainersAsync();
                Logger.Info($"ContainerViewModel: Loaded {Containers.Count} containers, {FilteredContainers.Count} filtered");
                StatusMessage = "Ready";
                LastUpdated = DateTime.Now.ToString("MMM dd, yyyy HH:mm:ss");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error initializing ContainerViewModel: {ex.Message}", ex);
                StatusMessage = "Error loading containers";
            }
            finally
            {
                IsLoading = false;
                Logger.Info("ContainerViewModel: Initialization completed");
            }
        }

        private async Task LoadContainersAsync()
        {
            try
            {
                Logger.Info("ContainerViewModel: Starting to load containers from service");
                var containers = await _containerService.GetAllAsync();
                Logger.Info($"ContainerViewModel: Retrieved {containers.Count()} containers from service");
                
                Containers.Clear();
                
                foreach (var container in containers.OrderBy(c => c.DisplayOrder ?? 999).ThenBy(c => c.ContainerCode))
                {
                    Containers.Add(container);
                }

                Logger.Info($"ContainerViewModel: Added {Containers.Count} containers to collection");

                // Update filtered containers
                FilterContainers();
                Logger.Info($"ContainerViewModel: Filtered containers count: {FilteredContainers.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading containers: {ex.Message}", ex);
                throw;
            }
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }

        private void FilterContainers()
        {
            var baseFilter = Containers.AsEnumerable();

            // Apply status filter
            switch (StatusFilter)
            {
                case "Active":
                    baseFilter = baseFilter.Where(c => c.IsActive);
                    break;
                case "Inactive":
                    baseFilter = baseFilter.Where(c => !c.IsActive);
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
                baseFilter = baseFilter.Where(c =>
                    (c.ContainerName?.ToLower().Contains(searchLower) ?? false) ||
                    (c.ContainerCode?.ToLower().Contains(searchLower) ?? false) ||
                    (c.ContainerId.ToString().Contains(searchLower)) ||
                    (c.Value?.ToString().Contains(searchLower) ?? false) ||
                    (c.TareWeight?.ToString().Contains(searchLower) ?? false)
                );
            }

            FilteredContainers = new ObservableCollection<ContainerType>(baseFilter);
            StatusMessage = $"Showing {FilteredContainers.Count} of {Containers.Count} containers";
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusFilter = "All";
            StatusMessage = "Ready";
        }

        private async Task AddContainerAsync()
        {
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var newContainer = new ContainerType
                {
                    ContainerId = 0,
                    ContainerCode = string.Empty,
                    ContainerName = string.Empty,
                    TareWeight = 0,
                    Value = 0,
                    IsActive = true,
                    DisplayOrder = null,
                    CreatedAt = DateTime.Now,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                };

                var dialogViewModel = new ContainerEditDialogViewModel(newContainer, false, _dialogService);
                var result = await DialogHost.Show(new ContainerEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.ContainerData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Adding container...";
                    
                    try
                    {
                        bool success = await _containerService.CreateAsync(dialogViewModel.ContainerData, App.CurrentUser?.Username ?? "SYSTEM");
                        
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Container added successfully.", "Success");
                            await LoadContainersAsync();
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Failed to add the container.", "Error");
                            StatusMessage = "Failed to add container";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error adding container: {ex.Message}", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error adding container: {ex.Message}", "Error");
                        StatusMessage = "Error adding container";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding container: {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync("Error", $"Failed to add container: {ex.Message}");
                StatusMessage = "Error adding container";
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task ViewContainerAsync(ContainerType? container)
        {
            if (container == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new ContainerEditDialogViewModel(container, true, _dialogService);
                await DialogHost.Show(new ContainerEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error viewing container: {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync("Error", $"Failed to view container: {ex.Message}");
                StatusMessage = "Error viewing container";
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task EditContainerAsync(ContainerType? container)
        {
            if (container == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new ContainerEditDialogViewModel(container, false, _dialogService);
                var result = await DialogHost.Show(new ContainerEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.ContainerData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Updating container...";
                    
                    try
                    {
                        bool success = await _containerService.UpdateAsync(dialogViewModel.ContainerData, App.CurrentUser?.Username ?? "SYSTEM");
                        
                        if (success)
                        {
                            Logger.Info($"EditContainerAsync: Successfully updated container {dialogViewModel.ContainerData.ContainerId}");
                            await _dialogService.ShowMessageBoxAsync("Container updated successfully.", "Success");
                            await LoadContainersAsync();
                        }
                        else
                        {
                            Logger.Warn($"EditContainerAsync: Failed to update container {dialogViewModel.ContainerData.ContainerId}");
                            await _dialogService.ShowMessageBoxAsync("Failed to update the container.", "Error");
                            StatusMessage = "Failed to update container";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error updating container {dialogViewModel.ContainerData.ContainerId}: {ex.Message}", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error updating container: {ex.Message}", "Error");
                        StatusMessage = "Error updating container";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error editing container: {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync("Error", $"Failed to edit container: {ex.Message}");
                StatusMessage = "Error editing container";
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task DeleteContainerAsync(ContainerType container)
        {
            if (container == null) return;

            try
            {
                // Check if container can be deleted
                var canDelete = await _containerService.CanDeleteAsync(container.ContainerId);
                if (!canDelete)
                {
                    var usageCount = await _containerService.GetUsageCountAsync(container.ContainerId);
                    await _dialogService.ShowMessageBoxAsync(
                        $"Cannot delete container '{container.ContainerName}' because it is used in {usageCount} receipt(s).\n\n" +
                        "You can mark it as 'Inactive' instead by editing the container.",
                        "Container In Use");
                    return;
                }

                var confirm = await _dialogService.ShowConfirmationDialogAsync(
                    $"Are you sure you want to delete container '{container.ContainerName}' ({container.ContainerCode})?", 
                    "Confirm Delete");
                
                if (confirm != true) return;

                IsLoading = true;
                StatusMessage = "Deleting container...";
                
                try
                {
                    bool success = await _containerService.DeleteAsync(container.ContainerId, App.CurrentUser?.Username ?? "SYSTEM");

                    if (success)
                    {
                        await _dialogService.ShowMessageBoxAsync("Container deleted successfully.", "Success");
                        await LoadContainersAsync();
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync("Failed to delete the container.", "Error");
                        StatusMessage = "Failed to delete container";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error deleting container: {ex.Message}", ex);
                    await _dialogService.ShowMessageBoxAsync($"Error deleting container: {ex.Message}", "Error");
                    StatusMessage = "Error deleting container";
                }
                finally
                {
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting container: {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting container: {ex.Message}", "Error");
                StatusMessage = "Error deleting container";
            }
        }

        private void NavigateToDashboard()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Navigate to dashboard - implementation depends on MainWindow structure
                    Logger.Info("Navigate to Dashboard requested");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to dashboard: {ex.Message}", ex);
                StatusMessage = "Error navigating to dashboard";
            }
        }

        private void NavigateToSettings()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Navigate to settings - implementation depends on MainWindow structure
                    Logger.Info("Navigate to Settings requested");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to settings: {ex.Message}", ex);
                StatusMessage = "Error navigating to settings";
            }
        }

        private async void ShowHelp()
        {
            try
            {
                await _dialogService.ShowMessageBoxAsync(
                    "Container Types Help\n\n" +
                    "• Use the search box to find containers by name, code, ID, value, or tare weight\n" +
                    "• Use the status filter to show All, Active, or Inactive containers\n" +
                    "• Click 'Add New' to create a new container type\n" +
                    "• Double-click a row to edit the container\n" +
                    "• Right-click for additional options like delete\n" +
                    "• Use F5 to refresh the data\n" +
                    "• Use F1 to show this help\n\n" +
                    "Container Types are used to track physical containers (flats, lugs, pallets) that growers use to ship berries.", 
                    "Container Types Help");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error showing help: {ex.Message}", ex);
                StatusMessage = "Error showing help";
            }
        }
    }
}