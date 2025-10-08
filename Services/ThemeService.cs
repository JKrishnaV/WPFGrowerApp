using System;
using System.Windows;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    public class ThemeService : IThemeService
    {
        private bool _isDarkTheme;
        private readonly PaletteHelper _paletteHelper;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    ApplyMaterialDesignTheme(value);
                    ThemeChanged?.Invoke(this, EventArgs.Empty);
                    Logger.Info($"Theme changed to: {(value ? "Dark" : "Light")}");
                }
            }
        }

        public event EventHandler ThemeChanged;

        public ThemeService()
        {
            // Default to light theme
            _isDarkTheme = false;
            _paletteHelper = new PaletteHelper();
        }

        public void ApplyDarkTheme()
        {
            IsDarkTheme = true;
        }

        public void ApplyLightTheme()
        {
            IsDarkTheme = false;
        }

        public void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
        }

        private void ApplyMaterialDesignTheme(bool isDark)
        {
            try
            {
                var theme = _paletteHelper.GetTheme();
                
                // Set base theme (Light or Dark)
                theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
                
                _paletteHelper.SetTheme(theme);
                
                Logger.Info($"MaterialDesign theme applied: {(isDark ? "Dark" : "Light")}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error applying MaterialDesign theme: {ex.Message}", ex);
            }
        }
    }
}

