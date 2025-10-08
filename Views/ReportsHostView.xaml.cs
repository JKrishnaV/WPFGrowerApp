using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ReportsHostView.xaml
    /// </summary>
    public partial class ReportsHostView : UserControl
    {
        public ReportsHostView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }
    }
}
