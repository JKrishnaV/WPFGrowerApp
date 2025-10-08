using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for DepotView.xaml
    /// </summary>
    public partial class DepotView : UserControl
    {
        public DepotView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }
    }
}
