using Microsoft.Extensions.DependencyInjection; // For IServiceProvider
using System; 
using System.Threading.Tasks; 
using System.Windows.Input; 
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services; // Added for IDialogService
using WPFGrowerApp.Views; // Still needed for GrowerSearchView in DialogService implementation (can be removed if DialogService is refactored)

namespace WPFGrowerApp.ViewModels
{
    // Ensure class definition is present and correct
    public class MainViewModel : ViewModelBase 
    {
        private object _currentViewModel;
        private bool _isNavigating; 
        private readonly IServiceProvider _serviceProvider; 
        private readonly IDialogService _dialogService; 

        // Inject IServiceProvider and IDialogService
        public MainViewModel(IServiceProvider serviceProvider, IDialogService dialogService) 
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); 

            // Initialize commands
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute, CanNavigate);
            NavigateToGrowersCommand = new RelayCommand(NavigateToGrowersExecuteAsync, CanNavigate); 
            NavigateToImportCommand = new RelayCommand(NavigateToImportExecute, CanNavigate);
            NavigateToReportsCommand = new RelayCommand(NavigateToReportsExecute, CanNavigate);
            NavigateToInventoryCommand = new RelayCommand(NavigateToInventoryExecute, CanNavigate); // Added
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsExecute, CanNavigate); // Added

            // Set default view model to Dashboard
            NavigateToDashboardExecute(null); 
        }

        // --- Navigation Commands ---
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToGrowersCommand { get; }
        public ICommand NavigateToImportCommand { get; }
        public ICommand NavigateToReportsCommand { get; }
        public ICommand NavigateToInventoryCommand { get; } // Added
        public ICommand NavigateToSettingsCommand { get; } // Added

        private bool CanNavigate(object parameter) => !_isNavigating;

        private void NavigateToDashboardExecute(object parameter)
        {
            try
            {
                CurrentViewModel = _serviceProvider.GetRequiredService<DashboardViewModel>(); 
            }
            catch (Exception ex)
            {
                 Infrastructure.Logging.Logger.Error("Error navigating to Dashboard", ex);
                 _dialogService.ShowMessageBox($"Error navigating to Dashboard: {ex.Message}", "Navigation Error");
            }
        }

        private async Task NavigateToGrowersExecuteAsync(object parameter)
        {
            if (!CanNavigate(parameter)) return; // Prevent re-entry

            _isNavigating = true;
            ((RelayCommand)NavigateToGrowersCommand).RaiseCanExecuteChanged(); 

            try
            {
                // Use Dialog Service
                var (dialogResult, selectedGrowerNumber) = _dialogService.ShowGrowerSearchDialog();

                if (dialogResult == true && selectedGrowerNumber.HasValue)
                {
                    // Resolve ViewModel from DI container
                    var growerViewModel = _serviceProvider.GetRequiredService<GrowerViewModel>(); 
                    
                    // Call InitializeAsync after getting the instance
                    await growerViewModel.InitializeAsync(); 

                    if (selectedGrowerNumber.Value != 0) // Load only if not creating new
                    {
                       await growerViewModel.LoadGrowerAsync(selectedGrowerNumber.Value);
                    }
                    CurrentViewModel = growerViewModel;
                }
            }
            catch (System.Exception ex)
            {
                 Infrastructure.Logging.Logger.Error("Error during grower navigation", ex);
                 _dialogService.ShowMessageBox($"Error navigating to grower: {ex.Message}", "Navigation Error");
            }
            finally
            {
                _isNavigating = false;
                 ((RelayCommand)NavigateToGrowersCommand).RaiseCanExecuteChanged();
            }
        }

        private void NavigateToImportExecute(object parameter)
        {
             if (!CanNavigate(parameter)) return; 
            try
            {
                var importViewModel = _serviceProvider.GetRequiredService<ImportViewModel>();
                // TODO: Does ImportViewModel need an InitializeAsync? If so, make this method async.
                CurrentViewModel = importViewModel;
            }
            catch (Exception ex)
            {
                 Infrastructure.Logging.Logger.Error("Error navigating to Import", ex);
                 _dialogService.ShowMessageBox($"Error navigating to Import: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToReportsExecute(object parameter)
        {
             if (!CanNavigate(parameter)) return; 
            try
            {
                var reportsViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
                // TODO: Does ReportsViewModel need an InitializeAsync? If so, make this method async.
                CurrentViewModel = reportsViewModel;
            }
            catch (Exception ex)
            {
                 Infrastructure.Logging.Logger.Error("Error navigating to Reports", ex);
                 _dialogService.ShowMessageBox($"Error navigating to Reports: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToInventoryExecute(object parameter)
        {
             if (!CanNavigate(parameter)) return; 
            try
            {
                CurrentViewModel = _serviceProvider.GetRequiredService<InventoryViewModel>();
            }
            catch (Exception ex)
            {
                 Infrastructure.Logging.Logger.Error("Error navigating to Inventory", ex);
                 _dialogService.ShowMessageBox($"Error navigating to Inventory: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToSettingsExecute(object parameter)
        {
             if (!CanNavigate(parameter)) return; 
            try
            {
                CurrentViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
            }
            catch (Exception ex)
            {
                 Infrastructure.Logging.Logger.Error("Error navigating to Settings", ex);
                 _dialogService.ShowMessageBox($"Error navigating to Settings: {ex.Message}", "Navigation Error");
            }
        }
        // --- End Navigation Commands ---


        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (_currentViewModel != value)
                {
                    _currentViewModel = value;
                    OnPropertyChanged(); // Assuming OnPropertyChanged is in ViewModelBase
                }
            }
        }
    } // End of MainViewModel class
} // End of namespace
