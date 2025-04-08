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

namespace WPFGrowerApp.ViewModels
{
    public class SettingsHostViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;
        private ViewModelBase _currentSettingViewModel;
        private SettingsNavigationItem _selectedSetting;

        public ObservableCollection<SettingsNavigationItem> SettingsOptions { get; }

        public SettingsHostViewModel(IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Logger.Info("Initializing SettingsHostViewModel");

            SettingsOptions = new ObservableCollection<SettingsNavigationItem>();

            // Always add Change Password option
            SettingsOptions.Add(new SettingsNavigationItem("Change Password", typeof(ChangePasswordViewModel), PackIconKind.Key));

            // Only add User Management if user is admin
            if (App.CurrentUser?.IsAdmin == true)
            {
                SettingsOptions.Add(new SettingsNavigationItem("Manage Users", typeof(UserManagementViewModel), PackIconKind.AccountMultiple));
            }

            // Add other options
            SettingsOptions.Add(new SettingsNavigationItem("Products", typeof(ProductViewModel), PackIconKind.PackageVariant));
            SettingsOptions.Add(new SettingsNavigationItem("Process Types", typeof(ProcessViewModel), PackIconKind.Cog));
            SettingsOptions.Add(new SettingsNavigationItem("Depots", typeof(DepotViewModel), PackIconKind.Store));

            Logger.Info($"Created {SettingsOptions.Count} navigation items");

            // Select the first item by default
            SelectedSetting = SettingsOptions.FirstOrDefault();
            if (SelectedSetting != null)
            {
                Logger.Info($"Selected default setting: {SelectedSetting.DisplayName}");
            }
        }

        public ViewModelBase CurrentSettingViewModel
        {
            get => _currentSettingViewModel;
            private set
            {
                if (SetProperty(ref _currentSettingViewModel, value))
                {
                    Logger.Info($"CurrentSettingViewModel changed to: {value?.GetType().Name ?? "null"}");
                }
            }
        }

        public SettingsNavigationItem SelectedSetting
        {
            get => _selectedSetting;
            set
            {
                if (value != null)
                {
                    Logger.Info($"Attempting to navigate to: {value.DisplayName}");
                }
                if (SetProperty(ref _selectedSetting, value) && value != null)
                {
                    NavigateToSetting(value.ViewModelType);
                }
            }
        }

        private async void NavigateToSetting(Type viewModelType)
        {
            if (viewModelType == null)
            {
                Logger.Warn("NavigateToSetting called with null viewModelType");
                return;
            }

            try
            {
                Logger.Info($"Attempting to resolve ViewModel of type: {viewModelType.Name}");
                var resolvedViewModel = _serviceProvider.GetRequiredService(viewModelType) as ViewModelBase;
                
                if (resolvedViewModel == null)
                {
                    Logger.Error($"Failed to resolve ViewModel of type {viewModelType.Name} as ViewModelBase");
                    return;
                }

                Logger.Info($"Successfully resolved ViewModel: {resolvedViewModel.GetType().Name}");
                CurrentSettingViewModel = resolvedViewModel;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error($"Unauthorized access to {viewModelType.Name}", ex);
                await _dialogService.ShowMessageBoxAsync("You do not have permission to access this feature.", "Access Denied");
                // Reset selection to previous item
                SelectedSetting = SettingsOptions.FirstOrDefault(x => x.ViewModelType != viewModelType);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to setting ViewModel {viewModelType.Name}", ex);
                await _dialogService.ShowMessageBoxAsync("An error occurred while loading the selected feature.", "Error");
            }
        }
    }
}
