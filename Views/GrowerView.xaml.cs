using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerView.xaml
    /// </summary>
    public partial class GrowerView : UserControl
    {
        public GrowerView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this); // Enable theme support
        }
    }
}
