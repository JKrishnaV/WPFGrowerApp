using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for DashboardView.xaml
    /// </summary>
    public partial class DashboardView : UserControl
    {
        private readonly IThemeService _themeService;

        public DashboardView()
        {
            InitializeComponent();

            // Get theme service from DI
            _themeService = App.ServiceProvider?.GetService<IThemeService>();
            
            if (_themeService != null)
            {
                // Subscribe to theme changes
                _themeService.ThemeChanged += OnThemeChanged;
                
                // Apply initial theme
                ApplyTheme(_themeService.IsDarkTheme);
            }
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyTheme(_themeService.IsDarkTheme);
        }

        private void ApplyTheme(bool isDark)
        {
            var resources = this.Resources;

            // Update dynamic resources
            if (isDark)
            {
                resources["CardBackground"] = resources["DarkCardBackground"];
                resources["CardBorder"] = resources["DarkCardBorder"];
                resources["CardHoverBackground"] = resources["DarkCardHoverBackground"];
                resources["CardHoverBorder"] = resources["DarkCardHoverBorder"];
                resources["TextPrimary"] = resources["DarkTextPrimary"];
                resources["TextSecondary"] = resources["DarkTextSecondary"];
                resources["AccentColor"] = resources["DarkAccent"];
                resources["HeaderBackground"] = resources["DarkHeaderBackground"];
                this.Background = resources["DarkMainBackground"] as SolidColorBrush;
            }
            else
            {
                resources["CardBackground"] = resources["LightCardBackground"];
                resources["CardBorder"] = resources["LightCardBorder"];
                resources["CardHoverBackground"] = resources["LightCardHoverBackground"];
                resources["CardHoverBorder"] = resources["LightCardHoverBorder"];
                resources["TextPrimary"] = resources["LightTextPrimary"];
                resources["TextSecondary"] = resources["LightTextSecondary"];
                resources["AccentColor"] = resources["LightAccent"];
                resources["HeaderBackground"] = resources["LightHeaderBackground"];
                this.Background = resources["LightMainBackground"] as SolidColorBrush;
            }
        }
    }
}
