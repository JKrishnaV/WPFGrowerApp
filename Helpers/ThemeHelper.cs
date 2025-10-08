using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.Helpers
{
    /// <summary>
    /// Helper class to easily apply theme support to any UserControl
    /// </summary>
    public static class ThemeHelper
    {
        /// <summary>
        /// Enables theme support for a UserControl
        /// Call this in the UserControl's constructor after InitializeComponent()
        /// </summary>
        /// <param name="userControl">The UserControl to enable theme support for</param>
        public static void EnableThemeSupport(UserControl userControl)
        {
            if (userControl == null) return;

            var themeService = App.ServiceProvider?.GetService<IThemeService>();
            if (themeService == null) return;

            // Subscribe to theme changes
            themeService.ThemeChanged += (sender, e) => ApplyTheme(userControl, themeService.IsDarkTheme);

            // Apply initial theme
            ApplyTheme(userControl, themeService.IsDarkTheme);
        }

        /// <summary>
        /// Applies the specified theme to a UserControl's resources
        /// </summary>
        /// <param name="userControl">The UserControl to apply theme to</param>
        /// <param name="isDark">True for dark theme, false for light theme</param>
        public static void ApplyTheme(UserControl userControl, bool isDark)
        {
            if (userControl?.Resources == null) return;

            var resources = userControl.Resources;

            // Get the application-level theme resources
            var appResources = Application.Current.Resources;

            try
            {
                if (isDark)
                {
                    // Apply dark theme
                    UpdateResource(resources, appResources, "ThemeCardBackground", "DarkCardBackground");
                    UpdateResource(resources, appResources, "ThemeCardBorder", "DarkCardBorder");
                    UpdateResource(resources, appResources, "ThemeCardHoverBackground", "DarkCardHoverBackground");
                    UpdateResource(resources, appResources, "ThemeCardHoverBorder", "DarkCardHoverBorder");
                    UpdateResource(resources, appResources, "ThemeTextPrimary", "DarkTextPrimary");
                    UpdateResource(resources, appResources, "ThemeTextSecondary", "DarkTextSecondary");
                    UpdateResource(resources, appResources, "ThemeAccentColor", "DarkAccent");
                    UpdateResource(resources, appResources, "ThemeHeaderBackground", "DarkHeaderBackground");
                    UpdateResource(resources, appResources, "ThemeMainBackground", "DarkMainBackground");
                    UpdateResource(resources, appResources, "ThemeInputBackground", "DarkInputBackground");
                    UpdateResource(resources, appResources, "ThemeInputBorder", "DarkInputBorder");

                    // Update background if control has it set
                    if (userControl.Background != null || appResources.Contains("DarkMainBackground"))
                    {
                        userControl.Background = appResources["DarkMainBackground"] as System.Windows.Media.Brush;
                    }
                }
                else
                {
                    // Apply light theme
                    UpdateResource(resources, appResources, "ThemeCardBackground", "LightCardBackground");
                    UpdateResource(resources, appResources, "ThemeCardBorder", "LightCardBorder");
                    UpdateResource(resources, appResources, "ThemeCardHoverBackground", "LightCardHoverBackground");
                    UpdateResource(resources, appResources, "ThemeCardHoverBorder", "LightCardHoverBorder");
                    UpdateResource(resources, appResources, "ThemeTextPrimary", "LightTextPrimary");
                    UpdateResource(resources, appResources, "ThemeTextSecondary", "LightTextSecondary");
                    UpdateResource(resources, appResources, "ThemeAccentColor", "LightAccent");
                    UpdateResource(resources, appResources, "ThemeHeaderBackground", "LightHeaderBackground");
                    UpdateResource(resources, appResources, "ThemeMainBackground", "LightMainBackground");
                    UpdateResource(resources, appResources, "ThemeInputBackground", "LightInputBackground");
                    UpdateResource(resources, appResources, "ThemeInputBorder", "LightInputBorder");

                    // Update background if control has it set
                    if (userControl.Background != null || appResources.Contains("LightMainBackground"))
                    {
                        userControl.Background = appResources["LightMainBackground"] as System.Windows.Media.Brush;
                    }
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error applying theme to {userControl.GetType().Name}", ex);
            }
        }

        /// <summary>
        /// Updates a resource in the control's resource dictionary
        /// </summary>
        private static void UpdateResource(ResourceDictionary controlResources, ResourceDictionary appResources, string targetKey, string sourceKey)
        {
            if (appResources.Contains(sourceKey))
            {
                controlResources[targetKey] = appResources[sourceKey];
            }
        }
    }
}

