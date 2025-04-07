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
                Infrastructure.Logging.Logger.Error("Error loading processes.", ex);
                _dialogService?.ShowMessageBox($"Error loading processes: {ex.Message}", "Error");
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
            if (string.IsNullOrWhiteSpace(SelectedProcess.ProcessId) || SelectedProcess.ProcessId.Length > 2)
            {
                 _dialogService?.ShowMessageBox("Process ID cannot be empty and must be 1 or 2 characters.", "Validation Error");
                 return;
            }
             if (string.IsNullOrWhiteSpace(SelectedProcess.Description))
            {
                 _dialogService?.ShowMessageBox("Description cannot be empty.", "Validation Error");
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
                    if(success) _dialogService?.ShowMessageBox("Process added successfully.", "Success");
                }
                else
                {
                    // Update existing process
                    success = await _processService.UpdateProcessAsync(SelectedProcess);
                     if(success) _dialogService?.ShowMessageBox("Process updated successfully.", "Success");
                }

                if (success)
                {
                    await LoadProcessesAsync(null); // Reload the list to show changes & clear selection
                }
                else
                {
                     _dialogService?.ShowMessageBox("Failed to save the process.", "Error");
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error saving process {SelectedProcess.ProcessId}.", ex);
                _dialogService?.ShowMessageBox($"Error saving process: {ex.Message}", "Error");
            }
             finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteProcessAsync(object parameter)
        {
             if (!CanSaveCancelDelete(parameter)) return; 

            var confirm = _dialogService?.ShowConfirmationDialog($"Are you sure you want to delete process '{SelectedProcess.Description}' ({SelectedProcess.ProcessId})?", "Confirm Delete");
            if (confirm != true) return; 

            IsLoading = true;
            try
            {
                bool success = await _processService.DeleteProcessAsync(SelectedProcess.ProcessId, App.CurrentUser?.Username ?? "SYSTEM"); 

                if (success)
                {
                    _dialogService?.ShowMessageBox("Process deleted successfully.", "Success");
                    await LoadProcessesAsync(null); // Reload list
                }
                else
                {
                    _dialogService?.ShowMessageBox("Failed to delete the process.", "Error");
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error deleting process {SelectedProcess.ProcessId}.", ex);
                _dialogService?.ShowMessageBox($"Error deleting process: {ex.Message}", "Error");
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
