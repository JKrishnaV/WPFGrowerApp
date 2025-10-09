using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ChequeManagementView.xaml
    /// </summary>
    public partial class ChequeManagementView : UserControl
    {
        public ChequeManagementView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Ensure view is focusable for keyboard shortcuts
            Loaded += (s, e) => Focus();
        }
    }
}


