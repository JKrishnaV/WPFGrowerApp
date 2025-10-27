using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Models; 
using MaterialDesignThemes.Wpf; // Added for PackIconKind
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using WPFGrowerApp.Views;
using System.Windows;

namespace WPFGrowerApp.ViewModels
{
    public class SettingsHostViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;

        // Direct navigation commands for dashboard-style cards
        public ICommand NavigateToChangePasswordCommand { get; }
        public ICommand NavigateToUserManagementCommand { get; }
        public ICommand NavigateToProductsCommand { get; }
        public ICommand NavigateToProcessTypesCommand { get; }
        public ICommand NavigateToPricingCommand { get; }
        public ICommand NavigateToDepotsCommand { get; }
        public ICommand NavigateToPaymentGroupsCommand { get; }
        public ICommand NavigateToContainerTypesCommand { get; }
        public ICommand NavigateToAppearanceCommand { get; }

        // Navigation event for main window navigation
        public static event Action<Type, string> NavigationRequested;

        public SettingsHostViewModel(IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Logger.Info("Initializing SettingsHostViewModel with dashboard-style navigation");

            // Initialize direct navigation commands
            NavigateToChangePasswordCommand = new RelayCommand(_ => NavigateToSetting<ChangePasswordViewModel>());
            NavigateToUserManagementCommand = new RelayCommand(_ => NavigateToSetting<UserManagementViewModel>());
            NavigateToProductsCommand = new RelayCommand(_ => NavigateToSetting<ProductViewModel>());
            NavigateToProcessTypesCommand = new RelayCommand(_ => NavigateToSetting<ProcessViewModel>());
            NavigateToPricingCommand = new RelayCommand(_ => NavigateToSetting<PriceViewModel>());
            NavigateToDepotsCommand = new RelayCommand(_ => NavigateToSetting<DepotViewModel>());
            NavigateToPaymentGroupsCommand = new RelayCommand(_ => NavigateToSetting<PaymentGroupViewModel>());
            NavigateToContainerTypesCommand = new RelayCommand(_ => NavigateToSetting<ContainerViewModel>());
            NavigateToAppearanceCommand = new RelayCommand(_ => NavigateToSetting<AppearanceSettingsViewModel>());

            Logger.Info("SettingsHostViewModel initialized with dashboard-style navigation commands");
        }

        private async void NavigateToSetting<T>() where T : ViewModelBase
        {
            try
            {
                Logger.Info($"Attempting to navigate to setting: {typeof(T).Name}");
                
                // Use the NavigationRequested event to navigate in the main window
                NavigationRequested?.Invoke(typeof(T), GetSettingTitle<T>());
                
                Logger.Info($"Successfully requested navigation to: {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to setting {typeof(T).Name}", ex);
                await _dialogService.ShowMessageBoxAsync($"An error occurred while loading {typeof(T).Name}.", "Error");
            }
        }

        private string GetSettingTitle<T>() where T : ViewModelBase
        {
            return typeof(T).Name switch
            {
                nameof(ChangePasswordViewModel) => "Change Password",
                nameof(UserManagementViewModel) => "User Management",
                nameof(ProductViewModel) => "Products",
                nameof(ProcessViewModel) => "Process Types",
                nameof(PriceViewModel) => "Pricing",
                nameof(DepotViewModel) => "Depots",
                nameof(PaymentGroupViewModel) => "Payment Groups",
                nameof(ContainerViewModel) => "Container Types",
                nameof(AppearanceSettingsViewModel) => "Appearance",
                _ => typeof(T).Name.Replace("ViewModel", "")
            };
        }
    }
}
