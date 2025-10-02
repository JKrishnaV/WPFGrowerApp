using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Infrastructure;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for managing container types (Contain table).
    /// Provides CRUD operations for container type definitions.
    /// </summary>
    public partial class ContainerViewModel : ViewModelBase
    {
        private readonly ContainerTypeService _containerService;
        private readonly IDialogService _dialogService;

        // Store all containers (unfiltered)
        private ObservableCollection<ContainerType> _allContainers = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActiveContainerCount))]
        [NotifyPropertyChangedFor(nameof(InactiveContainerCount))]
        [NotifyPropertyChangedFor(nameof(TotalContainerCount))]
        private ObservableCollection<ContainerType> _containers = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditContainerCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeleteContainerCommand))]
        [NotifyCanExecuteChangedFor(nameof(ToggleActiveCommand))]
        private ContainerType? _selectedContainer;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>
        /// Filters the container list when search text changes.
        /// </summary>
        partial void OnSearchTextChanged(string value)
        {
            ApplyFilter();
        }

        public ContainerViewModel(
            ContainerTypeService containerService,
            IDialogService dialogService)
        {
            Logger.Info("Initializing ContainerViewModel");
            _containerService = containerService ?? throw new ArgumentNullException(nameof(containerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Logger.Info("ContainerViewModel initialized successfully, loading containers...");
            _ = LoadContainersAsync();
        }

        /// <summary>
        /// Gets the current user's username.
        /// </summary>
        private string CurrentUser => App.CurrentUser?.Username ?? "SYSTEM";

        /// <summary>
        /// Loads all container types from the database.
        /// </summary>
        [RelayCommand]
        private async Task LoadContainersAsync()
        {
            try
            {
                Logger.Info("Loading container types from database...");
                IsLoading = true;

                var containers = await _containerService.GetAllAsync();
                _allContainers = new ObservableCollection<ContainerType>(containers);
                Logger.Info($"Successfully loaded {_allContainers.Count} container types");
                
                // Apply current filter
                ApplyFilter();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load container types", ex);
                await _dialogService.ShowMessageBoxAsync(
                    $"Failed to load container types:\n{ex.Message}",
                    "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Applies the current search filter to the container list.
        /// Searches across Container ID, Short Code, and Description.
        /// </summary>
        private void ApplyFilter()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    // No filter - show all containers
                    Containers = new ObservableCollection<ContainerType>(_allContainers);
                }
                else
                {
                    // Filter containers by search text (case-insensitive)
                    var searchLower = SearchText.ToLower();
                    var filtered = _allContainers.Where(c =>
                        c.ContainerId.ToString().Contains(searchLower) ||
                        (c.ShortCode?.ToLower().Contains(searchLower) ?? false) ||
                        (c.Description?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();

                    Containers = new ObservableCollection<ContainerType>(filtered);
                    Logger.Debug($"Filter applied: '{SearchText}' - {Containers.Count} of {_allContainers.Count} containers shown");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error applying container filter", ex);
                // On error, show all containers
                Containers = new ObservableCollection<ContainerType>(_allContainers);
            }
        }

        /// <summary>
        /// Opens the Add Container dialog.
        /// </summary>
        [RelayCommand]
        private void AddContainer()
        {
            var newContainer = new ContainerType
            {
                InUse = true // Default to active
            };

            var viewModel = new ContainerEntryViewModel(
                _containerService,
                _dialogService,
                CurrentUser,
                newContainer,
                isEditMode: false);

            var window = new Views.ContainerEntryWindow
            {
                DataContext = viewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                _ = LoadContainersAsync();
            }
        }

        /// <summary>
        /// Opens the Edit Container dialog for the selected container.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditContainer))]
        private void EditContainer()
        {
            if (SelectedContainer == null) return;

            var viewModel = new ContainerEntryViewModel(
                _containerService,
                _dialogService,
                CurrentUser,
                SelectedContainer,
                isEditMode: true);

            var window = new Views.ContainerEntryWindow
            {
                DataContext = viewModel,
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                _ = LoadContainersAsync();
            }
        }

        private bool CanEditContainer() => SelectedContainer != null;

        /// <summary>
        /// Deletes the selected container type.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanDeleteContainer))]
        private async Task DeleteContainer()
        {
            if (SelectedContainer == null) return;

            try
            {
                // Check if container is in use
                var canDelete = await _containerService.CanDeleteAsync(SelectedContainer.ContainerId);
                if (!canDelete)
                {
                    var usageCount = await _containerService.GetUsageCountAsync(SelectedContainer.ContainerId);
                    await _dialogService.ShowMessageBoxAsync(
                        $"Cannot delete container type '{SelectedContainer.Description}' because it is used in {usageCount} receipt(s).\n\n" +
                        "You can mark it as 'Inactive' instead by toggling the Active status.",
                        "Container In Use");
                    return;
                }

                // Confirm deletion
                var confirmed = await _dialogService.ShowConfirmationDialogAsync(
                    $"Are you sure you want to delete container type:\n\n" +
                    $"[{SelectedContainer.ContainerId}] {SelectedContainer.ShortCode} - {SelectedContainer.Description}?\n\n" +
                    "This action cannot be undone.",
                    "Confirm Delete");

                if (!confirmed) return;

                // Delete the container
                var success = await _containerService.DeleteAsync(SelectedContainer.ContainerId, CurrentUser);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        "Container type deleted successfully!",
                        "Success");

                    await LoadContainersAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync(
                        "Failed to delete container type. Please try again.",
                        "Error");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync(
                    $"Error deleting container type:\n{ex.Message}",
                    "Error");
            }
        }

        private bool CanDeleteContainer() => SelectedContainer != null;

        /// <summary>
        /// Toggles the Active/Inactive status of the selected container.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanToggleActive))]
        private async Task ToggleActive()
        {
            if (SelectedContainer == null) return;

            try
            {
                var newStatus = !SelectedContainer.InUse;
                var statusText = newStatus ? "Active" : "Inactive";

                var confirmed = await _dialogService.ShowConfirmationDialogAsync(
                    $"Mark container type '{SelectedContainer.Description}' as {statusText}?",
                    "Confirm Status Change");

                if (!confirmed) return;

                SelectedContainer.InUse = newStatus;
                var success = await _containerService.UpdateAsync(SelectedContainer, CurrentUser);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Container type marked as {statusText}.",
                        "Success");

                    await LoadContainersAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync(
                        "Failed to update container status.",
                        "Error");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync(
                    $"Error updating container status:\n{ex.Message}",
                    "Error");
            }
        }

        private bool CanToggleActive() => SelectedContainer != null;

        /// <summary>
        /// Refreshes the container list.
        /// </summary>
        [RelayCommand]
        private async Task Refresh()
        {
            await LoadContainersAsync();
        }

        /// <summary>
        /// Gets the count of active containers.
        /// </summary>
        public int ActiveContainerCount => Containers?.Count(c => c.InUse) ?? 0;

        /// <summary>
        /// Gets the count of inactive containers.
        /// </summary>
        public int InactiveContainerCount => Containers?.Count(c => !c.InUse) ?? 0;

        /// <summary>
        /// Gets the total container count.
        /// </summary>
        public int TotalContainerCount => Containers?.Count ?? 0;
    }
}
