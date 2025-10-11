using Microsoft.Extensions.DependencyInjection; // For IServiceProvider
using System;
using System.Threading.Tasks;
using System.Windows; // Added for Application.Current
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
        private bool _isMenuOpen = true; // Default to open
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;
        private readonly IThemeService _themeService;
        
        // Store current PaymentBatchViewModel instance to preserve state during navigation
        private PaymentBatchViewModel _paymentBatchViewModel;

        // Inject IServiceProvider, IDialogService, and IThemeService
        public MainViewModel(IServiceProvider serviceProvider, IDialogService dialogService, IThemeService themeService) 
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            // Initialize commands using the NavigateToAsync helper
            NavigateToDashboardCommand = new RelayCommand(async p => await NavigateToAsync<DashboardViewModel>("Dashboard", p), CanNavigate); // Use async lambda
            NavigateToGrowersCommand = new RelayCommand(NavigateToGrowersExecuteAsync, CanNavigate); // Keep separate async logic
            NavigateToReceiptsCommand = new RelayCommand(NavigateToReceiptsExecuteAsync, CanNavigate); // Added Receipts navigation
            NavigateToImportCommand = new RelayCommand(async p => await NavigateToAsync<ImportViewModel>("Import", p), CanNavigate); // Use async lambda
            // Update Reports command to navigate to the new ReportsHostViewModel
            NavigateToReportsCommand = new RelayCommand(async p => await NavigateToAsync<ReportsHostViewModel>("Reports", p), CanNavigate); // Use async lambda
            NavigateToInventoryCommand = new RelayCommand(async p => await NavigateToAsync<InventoryViewModel>("Inventory", p), CanNavigate); // Use async lambda
            NavigateToPaymentRunCommand = new RelayCommand(async p => await NavigateToAsync<PaymentRunViewModel>("Payment Run", p), CanNavigate); // Use async lambda
            // Update Settings command to navigate to the new SettingsHostViewModel
            NavigateToSettingsCommand = new RelayCommand(async p => await NavigateToAsync<SettingsHostViewModel>("Settings", p), CanNavigate); // Use async lambda
            
            // Phase 1 - New payment management navigation commands
            NavigateToPaymentBatchesCommand = new RelayCommand(async p => await NavigateToAsync<PaymentBatchViewModel>("Payment Batches", p), CanNavigate);
            NavigateToChequeManagementCommand = new RelayCommand(async p => await NavigateToAsync<ChequeManagementViewModel>("Cheque Management", p), CanNavigate);
            NavigateToFinalPaymentCommand = new RelayCommand(async p => await NavigateToAsync<FinalPaymentViewModel>("Final Payment", p), CanNavigate);

            // Set default view model to Dashboard
            _ = NavigateToAsync<DashboardViewModel>("Dashboard"); // Call async method, discard task

            // Initialize ToggleMenuCommand
            ToggleMenuCommand = new RelayCommand(ToggleMenuExecute);

            // Initialize LogoutCommand
            LogoutCommand = new RelayCommand(async p => await LogoutExecuteAsync(), CanLogout);

            // Initialize ToggleThemeCommand
            ToggleThemeCommand = new RelayCommand(ToggleThemeExecute);
            
            // Initialize NavigateToChangePasswordCommand
            NavigateToChangePasswordCommand = new RelayCommand(async p => await NavigateToAsync<ChangePasswordViewModel>("Change Password", p), CanNavigate);
        }

        // --- Menu State ---
        public bool IsMenuOpen
        {
            get => _isMenuOpen;
            set => SetProperty(ref _isMenuOpen, value);
        }

        public ICommand ToggleMenuCommand { get; }

        private void ToggleMenuExecute(object parameter)
        {
            IsMenuOpen = !IsMenuOpen;
        }

        // --- Theme Toggle ---
        public bool IsDarkTheme
        {
            get => _themeService.IsDarkTheme;
            set
            {
                if (_themeService.IsDarkTheme != value)
                {
                    _themeService.IsDarkTheme = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ToggleThemeCommand { get; }

        private void ToggleThemeExecute(object parameter)
        {
            _themeService.ToggleTheme();
            OnPropertyChanged(nameof(IsDarkTheme));
        }

        // --- Navigation Helper ---
        // Changed to async Task
        private async Task NavigateToAsync<TViewModel>(string viewName, object? parameter = null) where TViewModel : ViewModelBase 
        {
            if (!CanNavigate(parameter)) return;

            try
            {
                var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
                
                // Store PaymentBatchViewModel instance for state preservation
                if (viewModel is PaymentBatchViewModel paymentBatchVm)
                {
                    // Reuse existing instance if available, otherwise use the new one
                    if (_paymentBatchViewModel != null)
                    {
                        CurrentViewModel = _paymentBatchViewModel;
                    }
                    else
                    {
                        _paymentBatchViewModel = paymentBatchVm;
                        CurrentViewModel = paymentBatchVm;
                    }
                }
                else
                {
                    CurrentViewModel = viewModel;
                }
                
                IsMenuOpen = false; // Close menu after navigation
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
        public ICommand NavigateToReceiptsCommand { get; } // Added Receipts
        public ICommand NavigateToImportCommand { get; }
        public ICommand NavigateToReportsCommand { get; }
        public ICommand NavigateToInventoryCommand { get; }
        public ICommand NavigateToPaymentRunCommand { get; } // Added
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand NavigateToChangePasswordCommand { get; } // Added
        public ICommand LogoutCommand { get; } // Added
        
        // Phase 1 - Payment management navigation
        public ICommand NavigateToPaymentBatchesCommand { get; }
        public ICommand NavigateToChequeManagementCommand { get; }
        public ICommand NavigateToFinalPaymentCommand { get; }

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
                var result = _dialogService.ShowGrowerSearchDialog();

                if (result.DialogResult == true && !string.IsNullOrEmpty(result.SelectedGrowerNumber))
                {
                    // Resolve ViewModel from DI container
                    var growerViewModel = _serviceProvider.GetRequiredService<GrowerViewModel>();

                    // Call InitializeAsync after getting the instance
                    await growerViewModel.InitializeAsync();

                    if (result.SelectedGrowerNumber != "0") // Load only if not creating new
                        await growerViewModel.LoadGrowerAsync(result.SelectedGrowerNumber);
                    CurrentViewModel = growerViewModel;
                    IsMenuOpen = false; // Close menu after navigation
                }
                // If dialog is cancelled or no grower selected, leave menu state as is
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

        private async Task NavigateToReceiptsExecuteAsync(object? parameter) // Added Receipts navigation
        {
            Infrastructure.Logging.Logger.Info("NavigateToReceiptsExecuteAsync - Starting receipt navigation");
            
            if (!CanNavigate(parameter))
            {
                Infrastructure.Logging.Logger.Warn("NavigateToReceiptsExecuteAsync - Cannot navigate (already navigating)");
                return;
            }

            _isNavigating = true;
            ((RelayCommand)NavigateToReceiptsCommand).RaiseCanExecuteChanged();

            try
            {
                Infrastructure.Logging.Logger.Info("NavigateToReceiptsExecuteAsync - Resolving ReceiptViewModel from DI container");
                // Resolve ViewModel from DI container
                var receiptViewModel = _serviceProvider.GetRequiredService<ReceiptViewModel>();

                Infrastructure.Logging.Logger.Info("NavigateToReceiptsExecuteAsync - Calling ReceiptViewModel.InitializeAsync");
                // Call InitializeAsync to load receipts
                await receiptViewModel.InitializeAsync();

                Infrastructure.Logging.Logger.Info("NavigateToReceiptsExecuteAsync - Setting CurrentViewModel to ReceiptViewModel");
                CurrentViewModel = receiptViewModel;
                IsMenuOpen = false; // Close menu after navigation
                
                Infrastructure.Logging.Logger.Info("NavigateToReceiptsExecuteAsync - Receipt navigation completed successfully");
            }
            catch (System.Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error during receipt navigation", ex);
                await _dialogService.ShowMessageBoxAsync($"Error navigating to receipts: {ex.Message}", "Navigation Error");
            }
            finally
            {
                _isNavigating = false;
                ((RelayCommand)NavigateToReceiptsCommand).RaiseCanExecuteChanged();
            }
        }

        // Removed NavigateToImportExecute - Handled by NavigateTo<TViewModel> in command initialization
        // Removed NavigateToReportsExecute - Handled by NavigateTo<TViewModel> in command initialization
        // Removed NavigateToInventoryExecute - Handled by NavigateTo<TViewModel> in command initialization
        // Removed NavigateToSettingsExecute - Handled by NavigateTo<TViewModel> in command initialization

        // --- Logout Logic ---
        private bool CanLogout(object? parameter) => true; // Always allow logout

        private async Task LogoutExecuteAsync()
        {
            bool confirmLogout = await _dialogService.ShowConfirmationDialogAsync("Confirm Logout", "Are you sure you want to log out?");
            if (confirmLogout)
            {
                try
                {
                    // Get the current main window
                    var mainWindow = Application.Current.MainWindow;

                    // Resolve and show the login view
                    var loginView = _serviceProvider.GetRequiredService<LoginView>();
                    loginView.WindowStartupLocation = WindowStartupLocation.CenterScreen; // Ensure it's centered
                    loginView.Show();

                    // Close the main window
                    mainWindow?.Close();
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.Logger.Error("Error during logout process.", ex);
                    await _dialogService.ShowMessageBoxAsync($"An error occurred during logout: {ex.Message}", "Logout Error");
                }
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
        
        // Public property to access/store PaymentBatchViewModel for state preservation
        public PaymentBatchViewModel PaymentBatchViewModel
        {
            get => _paymentBatchViewModel;
            set => SetProperty(ref _paymentBatchViewModel, value);
        }
        
        // Alias property for compatibility with detail view navigation
        public object CurrentView
        {
            get => CurrentViewModel;
            set => CurrentViewModel = value;
        }

        // --- Header Features ---
        private string _searchText = string.Empty;
        private int _notificationCount = 0;
        private string _currentUserName = "Admin User";
        private string _currentUserInitials = "AU";

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public int NotificationCount
        {
            get => _notificationCount;
            set
            {
                SetProperty(ref _notificationCount, value);
                OnPropertyChanged(nameof(HasNotifications));
            }
        }

        public bool HasNotifications => NotificationCount > 0;

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public string CurrentUserInitials
        {
            get => _currentUserInitials;
            set => SetProperty(ref _currentUserInitials, value);
        }

        // --- End Header Features ---

    } // End of MainViewModel class
} // End of namespace
