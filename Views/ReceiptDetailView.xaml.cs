using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ReceiptDetailView.xaml
    /// </summary>
    public partial class ReceiptDetailView : UserControl
    {
        public ReceiptDetailView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // CRITICAL: Auto-focus for immediate keyboard shortcuts
            Loaded += (s, e) => Focus();
        }
    }
}
