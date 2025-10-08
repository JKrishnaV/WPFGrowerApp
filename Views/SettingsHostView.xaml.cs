using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for SettingsHostView.xaml
    /// </summary>
    public partial class SettingsHostView : UserControl
    {
        public SettingsHostView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }
    }
}
