using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerDetailView.xaml
    /// </summary>
    public partial class GrowerDetailView : UserControl
    {
        public GrowerDetailView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Set focus to the UserControl when loaded so keyboard shortcuts work
            Loaded += (s, e) => Focus();
        }
    }
}
