using System;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    public class ThemeService : IThemeService
    {
        private bool _isDarkTheme;

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
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
    }
}

