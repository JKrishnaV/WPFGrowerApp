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

            // Initialize commands using the NavigateToAsync helper
            NavigateToDashboardCommand = new RelayCommand(async p => await NavigateToAsync<DashboardViewModel>("Dashboard", p), CanNavigate); // Use async lambda
            NavigateToGrowersCommand = new RelayCommand(NavigateToGrowersExecuteAsync, CanNavigate); // Keep separate async logic
            NavigateToImportCommand = new RelayCommand(async p => await NavigateToAsync<ImportViewModel>("Import", p), CanNavigate); // Use async lambda
            NavigateToReportsCommand = new RelayCommand(async p => await NavigateToAsync<ReportsViewModel>("Reports", p), CanNavigate); // Use async lambda
            NavigateToInventoryCommand = new RelayCommand(async p => await NavigateToAsync<InventoryViewModel>("Inventory", p), CanNavigate); // Use async lambda
            NavigateToPaymentRunCommand = new RelayCommand(async p => await NavigateToAsync<PaymentRunViewModel>("Payment Run", p), CanNavigate); // Use async lambda
            // Update Settings command to navigate to the new SettingsHostViewModel
            NavigateToSettingsCommand = new RelayCommand(async p => await NavigateToAsync<SettingsHostViewModel>("Settings", p), CanNavigate); // Use async lambda

            // Set default view model to Dashboard
            _ = NavigateToAsync<DashboardViewModel>("Dashboard"); // Call async method, discard task
        }

        // --- Navigation Helper ---
        // Changed to async Task
        private async Task NavigateToAsync<TViewModel>(string viewName, object? parameter = null) where TViewModel : ViewModelBase 
        {
            if (!CanNavigate(parameter)) return;

            try
            {
                CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();
                // TODO: Consider if InitializeAsync pattern is needed for other ViewModels like Import/Reports
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error navigating to {viewName}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error navigating to {viewName}: {ex.Message}", "Navigation Error"); // Use async
            }
        }

        // --- Navigation Commands ---
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToGrowersCommand { get; }
        public ICommand NavigateToImportCommand { get; }
        public ICommand NavigateToReportsCommand { get; }
        public ICommand NavigateToInventoryCommand { get; }
        public ICommand NavigateToPaymentRunCommand { get; } // Added
        public ICommand NavigateToSettingsCommand { get; }

        private bool CanNavigate(object? parameter) => !_isNavigating; // Changed parameter to object?

        // Removed NavigateToDashboardExecute - Handled by NavigateTo<TViewModel>

        private async Task NavigateToGrowersExecuteAsync(object? parameter) // Changed parameter to object?
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
                 await _dialogService.ShowMessageBoxAsync($"Error navigating to grower: {ex.Message}", "Navigation Error"); // Use async
            }
            finally
            {
                _isNavigating = false;
                 ((RelayCommand)NavigateToGrowersCommand).RaiseCanExecuteChanged();
            }
        }

        // Removed NavigateToImportExecute - Handled by NavigateTo<TViewModel> in command initialization
        // Removed NavigateToReportsExecute - Handled by NavigateTo<TViewModel> in command initialization
        // Removed NavigateToInventoryExecute - Handled by NavigateTo<TViewModel> in command initialization
        // Removed NavigateToSettingsExecute - Handled by NavigateTo<TViewModel> in command initialization

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
