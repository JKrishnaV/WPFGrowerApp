
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services;
using WPFGrowerApp.Properties;

namespace WPFGrowerApp.ViewModels
{
    public class AppearanceSettingsViewModel : ViewModelBase
    {
        private readonly IUISettingsService _uiSettingsService;
        private double _fontScalingFactor;
        private bool _rememberPassword;

        public AppearanceSettingsViewModel(IUISettingsService uiSettingsService)
        {
            _uiSettingsService = uiSettingsService;

            _fontScalingFactor = _uiSettingsService.GetFontScalingFactor();
            _rememberPassword = Settings.Default.RememberPassword;

            ApplyChangesCommand = new RelayCommand(o => ApplyChanges());
        }

        public double FontScalingFactor
        {
            get => _fontScalingFactor;
            set => SetProperty(ref _fontScalingFactor, value);
        }

        public bool RememberPassword
        {
            get => _rememberPassword;
            set => SetProperty(ref _rememberPassword, value);
        }

        public ICommand ApplyChangesCommand { get; }

        private void ApplyChanges()
        {
            _uiSettingsService.SaveFontScalingFactor(FontScalingFactor);
            App.UpdateScaledFontSizes(FontScalingFactor);
            
            // Save remember password setting
            Settings.Default.RememberPassword = RememberPassword;
            Settings.Default.Save();
        }
    }
}
