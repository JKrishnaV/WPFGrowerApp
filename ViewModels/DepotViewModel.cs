using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services; // For IDialogService

namespace WPFGrowerApp.ViewModels
{
    public class DepotViewModel : ViewModelBase
    {
        private readonly IDepotService _depotService;
        private readonly IDialogService _dialogService;
        private ObservableCollection<Depot> _depots;
        private Depot _selectedDepot;
        private bool _isEditing;
        private bool _isLoading;

        public ObservableCollection<Depot> Depots
        {
            get => _depots;
            set => SetProperty(ref _depots, value);
        }

        public Depot SelectedDepot
        {
            get => _selectedDepot;
            set
            {
                bool depotSelected = value != null;
                if (SetProperty(ref _selectedDepot, value))
                {
                    IsEditing = depotSelected; // Set IsEditing based on selection
                }
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            private set // Controlled internally
            {
                if (SetProperty(ref _isEditing, value))
                {
                    // Raise CanExecuteChanged for relevant commands
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)NewCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoadDepotsCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public DepotViewModel(IDepotService depotService, IDialogService dialogService)
        {
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Depots = new ObservableCollection<Depot>();

            LoadDepotsCommand = new RelayCommand(async (param) => await LoadDepotsAsync(param), (param) => true);
            NewCommand = new RelayCommand(AddNewDepot, CanAddNew);
            SaveCommand = new RelayCommand(async (param) => await SaveDepotAsync(param), CanSaveCancelDelete);
            DeleteCommand = new RelayCommand(async (param) => await DeleteDepotAsync(param), CanSaveCancelDelete);
            CancelCommand = new RelayCommand(CancelEdit, CanSaveCancelDelete);

            // Load depots on initialization
            _ = LoadDepotsAsync(null);
        }

        private async Task LoadDepotsAsync(object parameter)
        {
            IsLoading = true;
            try
            {
                var depots = await _depotService.GetAllDepotsAsync();
                Depots = new ObservableCollection<Depot>(depots.OrderBy(d => d.DepotId)); // Order by ID
                SelectedDepot = null; // Clear selection after loading
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error loading depots.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error loading depots: {ex.Message}", "Error"); // Use async
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddNewDepot(object parameter)
        {
            SelectedDepot = new Depot(); // Create a new, empty depot object
        }

        // CanExecute for New button
        private bool CanAddNew(object parameter)
        {
            return SelectedDepot == null;
        }

        // Combined CanExecute for Save, Cancel, Delete
        private bool CanSaveCancelDelete(object parameter)
        {
            return SelectedDepot != null;
        }

        private async Task SaveDepotAsync(object parameter)
        {
            if (SelectedDepot == null) return;

          // Basic Validation
          if (SelectedDepot.DepotId <= 0)
          {
              await _dialogService?.ShowMessageBoxAsync("Depot ID must be a positive integer.", "Validation Error"); // Use async
              return;
          }
          if (string.IsNullOrWhiteSpace(SelectedDepot.DepotCode) || SelectedDepot.DepotCode.Length > 8)
          {
              await _dialogService?.ShowMessageBoxAsync("Depot Code cannot be empty and must be 8 characters or less.", "Validation Error"); // Use async
              return;
          }
          if (string.IsNullOrWhiteSpace(SelectedDepot.DepotName) || SelectedDepot.DepotName.Length > 12)
          {
              await _dialogService?.ShowMessageBoxAsync("Depot Name cannot be empty and must be 12 characters or less.", "Validation Error"); // Use async
              return;
          }

            // --- Start Validation ---
            Depot existingDepotById = null; // Declare variable outside the try block
            try
            {
                existingDepotById = await _depotService.GetDepotByIdAsync(SelectedDepot.DepotId); // Assign value inside
                var allDepots = (await _depotService.GetAllDepotsAsync()).ToList(); // Materialize for multiple checks

                // Check if adding a new depot
                if (existingDepotById == null)
                {
                    // Check if ID already exists (should be caught by GetDepotByIdAsync, but double-check)
                    // This check is technically redundant if GetDepotByIdAsync works correctly, but safe to keep.
                    if (allDepots.Any(d => d.DepotId == SelectedDepot.DepotId))
                    {
                        await _dialogService?.ShowMessageBoxAsync($"Depot ID '{SelectedDepot.DepotId}' already exists.", "Validation Error"); // Use async
                        return;
                    }
                    // Check if Name already exists
                    if (allDepots.Any(d => d.DepotName.Equals(SelectedDepot.DepotName, StringComparison.OrdinalIgnoreCase)))
                    {
                        await _dialogService?.ShowMessageBoxAsync($"Depot Name '{SelectedDepot.DepotName}' already exists.", "Validation Error"); // Use async
                        return;
                    }
                }
                else // Check if updating an existing depot
                {
                    // Check if Name already exists for a *different* depot
                    if (allDepots.Any(d => d.DepotId != SelectedDepot.DepotId &&
                                           d.DepotName.Equals(SelectedDepot.DepotName, StringComparison.OrdinalIgnoreCase)))
                    {
                        await _dialogService?.ShowMessageBoxAsync($"Depot Name '{SelectedDepot.DepotName}' is already used by another depot.", "Validation Error"); // Use async
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                 Infrastructure.Logging.Logger.Error($"Error during validation check for depot {SelectedDepot.DepotId}.", ex);
                await _dialogService?.ShowMessageBoxAsync($"An error occurred during validation: {ex.Message}", "Error"); // Use async
                return; // Stop if validation check fails
            }
            finally
            {
                 // No IsLoading changes here, validation should be quick
            }
            // --- End Validation ---


            IsLoading = true;
            bool success = false;
            try
            {
                // Determine if it's an Add or Update (using the existingDepotById variable declared above)

                if (existingDepotById == null)
                {
                    // Add new depot
                    success = await _depotService.AddDepotAsync(SelectedDepot);
                    if(success) await _dialogService?.ShowMessageBoxAsync("Depot added successfully.", "Success"); // Use async
                }
                else
                {
                    // Update existing depot
                    success = await _depotService.UpdateDepotAsync(SelectedDepot);
                     if(success) await _dialogService?.ShowMessageBoxAsync("Depot updated successfully.", "Success"); // Use async
                }

                if (success)
                {
                    await LoadDepotsAsync(null); // Reload the list
                }
                else
                {
                     await _dialogService?.ShowMessageBoxAsync("Failed to save the depot.", "Error"); // Use async
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error saving depot {SelectedDepot.DepotId}.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error saving depot: {ex.Message}", "Error"); // Use async
            }
             finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteDepotAsync(object parameter)
        {
             // Infrastructure.Logging.Logger.Info($"DeleteDepotAsync called for DepotId: {SelectedDepot?.DepotId ?? "null"}"); // Removed log
             if (!CanSaveCancelDelete(parameter)) return;

            bool confirm = false; // Default to false
            try
            {
                 // Infrastructure.Logging.Logger.Info($"Showing confirmation dialog for DepotId: {SelectedDepot.DepotId}"); // Removed log
                 if (_dialogService != null) // Check if service is not null before calling
                 {
                    confirm = await _dialogService.ShowConfirmationDialogAsync($"Are you sure you want to delete depot '{SelectedDepot.DepotName}' ({SelectedDepot.DepotId})?", "Confirm Delete");
                 }
                 // 'confirm' remains false if _dialogService was null
                 // Infrastructure.Logging.Logger.Info($"Confirmation dialog returned: {confirm}"); // Removed log
            }
            catch (Exception dialogEx)
            {
                 Infrastructure.Logging.Logger.Error($"Error showing confirmation dialog: {dialogEx.Message}", dialogEx);
                 await _dialogService?.ShowMessageBoxAsync($"An error occurred showing the confirmation dialog: {dialogEx.Message}", "Dialog Error");
                 return; // Stop if dialog fails
            }

            if (!confirm)
            {
                 // Infrastructure.Logging.Logger.Info("Deletion cancelled by user."); // Removed log
                 return; // Stop if user didn't confirm
            }

            IsLoading = true;
            try
            {
                // Infrastructure.Logging.Logger.Info($"Calling _depotService.DeleteDepotAsync for DepotId: {SelectedDepot.DepotId}"); // Removed log
                bool success = await _depotService.DeleteDepotAsync(SelectedDepot.DepotId, App.CurrentUser?.Username ?? "SYSTEM");

                if (success)
                {
                    await _dialogService?.ShowMessageBoxAsync("Depot deleted successfully.", "Success"); // Use async
                    await LoadDepotsAsync(null); // Reload list
                }
                else
                {
                    await _dialogService?.ShowMessageBoxAsync("Failed to delete the depot.", "Error"); // Use async
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error deleting depot {SelectedDepot.DepotId}.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error deleting depot: {ex.Message}", "Error"); // Use async
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit(object parameter)
        {
            if (!CanSaveCancelDelete(parameter)) return;

            SelectedDepot = null;
        }
    }
}
