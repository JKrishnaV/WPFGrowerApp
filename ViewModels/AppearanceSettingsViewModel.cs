
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    public class AppearanceSettingsViewModel : ViewModelBase
    {
        private readonly IUISettingsService _uiSettingsService;
        private double _fontScalingFactor;

        public AppearanceSettingsViewModel(IUISettingsService uiSettingsService)
        {
            _uiSettingsService = uiSettingsService;

            _fontScalingFactor = _uiSettingsService.GetFontScalingFactor();

            ApplyChangesCommand = new RelayCommand(o => ApplyChanges());
        }

        public double FontScalingFactor
        {
            get => _fontScalingFactor;
            set => SetProperty(ref _fontScalingFactor, value);
        }

        public ICommand ApplyChangesCommand { get; }

        private void ApplyChanges()
        {
            _uiSettingsService.SaveFontScalingFactor(FontScalingFactor);
            App.UpdateScaledFontSizes(FontScalingFactor);
        }
    }
}
