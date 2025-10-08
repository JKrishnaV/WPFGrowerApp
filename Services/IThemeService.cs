using System;

namespace WPFGrowerApp.Services
{
    public interface IThemeService
    {
        /// <summary>
        /// Gets or sets whether dark theme is enabled
        /// </summary>
        bool IsDarkTheme { get; set; }

        /// <summary>
        /// Event raised when theme changes
        /// </summary>
        event EventHandler ThemeChanged;

        /// <summary>
        /// Applies the dark theme to the Dashboard view
        /// </summary>
        void ApplyDarkTheme();

        /// <summary>
        /// Applies the light theme to the Dashboard view
        /// </summary>
        void ApplyLightTheme();

        /// <summary>
        /// Toggles between dark and light themes
        /// </summary>
        void ToggleTheme();
    }
}

