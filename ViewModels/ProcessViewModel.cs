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
    public class ProcessViewModel : ViewModelBase
    {
        private readonly IProcessService _processService;
        private readonly IDialogService _dialogService; 
        private ObservableCollection<Process> _processes;
        private Process _selectedProcess;
        private bool _isEditing;
        private bool _isLoading;

        public ObservableCollection<Process> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        public Process SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                bool processSelected = value != null;
                if (SetProperty(ref _selectedProcess, value))
                {
                    IsEditing = processSelected; // Set IsEditing based on selection
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

        public ICommand LoadProcessesCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public ProcessViewModel(IProcessService processService, IDialogService dialogService)
        {
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); 

            Processes = new ObservableCollection<Process>();

            LoadProcessesCommand = new RelayCommand(async (param) => await LoadProcessesAsync(param), (param) => true); 
            NewCommand = new RelayCommand(AddNewProcess, CanAddNew); 
            SaveCommand = new RelayCommand(async (param) => await SaveProcessAsync(param), CanSaveCancelDelete); 
            DeleteCommand = new RelayCommand(async (param) => await DeleteProcessAsync(param), CanSaveCancelDelete); 
            CancelCommand = new RelayCommand(CancelEdit, CanSaveCancelDelete); 

            // Load processes on initialization
            _ = LoadProcessesAsync(null); 
        }

        private async Task LoadProcessesAsync(object parameter)
        {
            IsLoading = true;
            try
            {
                var processes = await _processService.GetAllProcessesAsync();
                Processes = new ObservableCollection<Process>(processes.OrderBy(p => p.Description));
                SelectedProcess = null; // Clear selection after loading
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error loading process types.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error loading process types: {ex.Message}", "Error"); // Use async
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void AddNewProcess(object parameter)
        {
            SelectedProcess = new Process(); // Create a new, empty process object
            // IsEditing is set automatically by the SelectedProcess setter
        }

        // CanExecute for New button
        private bool CanAddNew(object parameter)
        {
            // Can add new only if NOT currently editing/viewing a selected process
            return SelectedProcess == null; 
        }

        // Combined CanExecute for Save, Cancel, Delete
        private bool CanSaveCancelDelete(object parameter)
        {
            // Can perform these actions only if a process is selected
            return SelectedProcess != null;
        }

        private async Task SaveProcessAsync(object parameter)
        {
            if (SelectedProcess == null) return; 

          // Basic Validation 
          if (SelectedProcess.ProcessId <= 0)
          {
              await _dialogService?.ShowMessageBoxAsync("Process ID must be a positive integer.", "Validation Error"); // Use async
              return;
          }
          if (string.IsNullOrWhiteSpace(SelectedProcess.ProcessCode) || SelectedProcess.ProcessCode.Length > 8)
          {
              await _dialogService?.ShowMessageBoxAsync("Process Code cannot be empty and must be 8 characters or less.", "Validation Error"); // Use async
              return;
          }
          if (string.IsNullOrWhiteSpace(SelectedProcess.Description) || SelectedProcess.Description.Length > 19)
          {
              await _dialogService?.ShowMessageBoxAsync("Description cannot be empty and must be 19 characters or less.", "Validation Error"); // Use async
              return;
          }
            // Add validation for DefGrade and ProcClass if needed (e.g., range checks)

            IsLoading = true;
            bool success = false;
            try
            {
                // Determine if it's an Add or Update
                var existingProcess = await _processService.GetProcessByIdAsync(SelectedProcess.ProcessId);

                if (existingProcess == null) 
                {
                    // Add new process
                    success = await _processService.AddProcessAsync(SelectedProcess);
                    if(success) await _dialogService?.ShowMessageBoxAsync("Process type added successfully.", "Success"); // Use async
                }
                else
                {
                    // Update existing process
                    success = await _processService.UpdateProcessAsync(SelectedProcess);
                     if(success) await _dialogService?.ShowMessageBoxAsync("Process type updated successfully.", "Success"); // Use async
                }

                if (success)
                {
                    await LoadProcessesAsync(null); // Reload the list to show changes & clear selection
                }
                else
                {
                     await _dialogService?.ShowMessageBoxAsync("Failed to save the process type.", "Error"); // Use async
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error saving process type {SelectedProcess.ProcessId}.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error saving process type: {ex.Message}", "Error"); // Use async
            }
             finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteProcessAsync(object parameter)
        {
             if (!CanSaveCancelDelete(parameter)) return; 

            var confirm = await _dialogService?.ShowConfirmationDialogAsync($"Are you sure you want to delete process type '{SelectedProcess.Description}' ({SelectedProcess.ProcessId})?", "Confirm Delete"); // Use async
            if (confirm != true) return; 

            IsLoading = true;
            try
            {
                bool success = await _processService.DeleteProcessAsync(SelectedProcess.ProcessId, App.CurrentUser?.Username ?? "SYSTEM"); 

                if (success)
                {
                    await _dialogService?.ShowMessageBoxAsync("Process type deleted successfully.", "Success"); // Use async
                    await LoadProcessesAsync(null); // Reload list
                }
                else
                {
                    await _dialogService?.ShowMessageBoxAsync("Failed to delete the process type.", "Error"); // Use async
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error deleting process type {SelectedProcess.ProcessId}.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error deleting process type: {ex.Message}", "Error"); // Use async
            }
             finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit(object parameter)
        {
            if (!CanSaveCancelDelete(parameter)) return; 

            // Simply clear selection, which will trigger IsEditing = false and reload
            SelectedProcess = null; 
        }
    }
}
