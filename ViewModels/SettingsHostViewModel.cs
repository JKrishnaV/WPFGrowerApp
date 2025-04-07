using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Models; // Assuming SettingsNavigationItem will be created here

namespace WPFGrowerApp.ViewModels
{
    public class SettingsHostViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private ViewModelBase _currentSettingViewModel;
        private SettingsNavigationItem _selectedSetting;

        public ObservableCollection<SettingsNavigationItem> SettingsOptions { get; }

        public SettingsHostViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            SettingsOptions = new ObservableCollection<SettingsNavigationItem>
            {
                new SettingsNavigationItem("Change Password", typeof(ChangePasswordViewModel)),
                new SettingsNavigationItem("Products", typeof(ProductViewModel)) 
                // Add more settings here as needed
            };

            // Select the first item by default
            SelectedSetting = SettingsOptions.FirstOrDefault(); 
        }

        public ViewModelBase CurrentSettingViewModel
        {
            get => _currentSettingViewModel;
            private set => SetProperty(ref _currentSettingViewModel, value);
        }

        public SettingsNavigationItem SelectedSetting
        {
            get => _selectedSetting;
            set
            {
                if (SetProperty(ref _selectedSetting, value) && value != null)
                {
                    NavigateToSetting(value.ViewModelType);
                }
            }
        }

        private void NavigateToSetting(Type viewModelType)
        {
            if (viewModelType == null) return;

            try
            {
                // Resolve the specific setting ViewModel from the service provider
                var resolvedViewModel = _serviceProvider.GetRequiredService(viewModelType) as ViewModelBase;
                CurrentSettingViewModel = resolvedViewModel;
                
                // Optional: Call an Initialize method if needed
                // if (resolvedViewModel is IInitializable vm) { await vm.InitializeAsync(); }
            }
            catch (Exception ex)
            {
                // Log error
                Infrastructure.Logging.Logger.Error($"Error navigating to setting ViewModel {viewModelType.Name}", ex);
                // Optionally show an error message to the user via a dialog service
            }
        }
    }
}
