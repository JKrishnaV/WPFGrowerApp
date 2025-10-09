using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PaymentBatchListView.xaml
    /// </summary>
    public partial class PaymentBatchListView : UserControl
    {
        public PaymentBatchListView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Ensure view is focusable for keyboard shortcuts
            Loaded += (s, e) => Focus();
        }
    }
}


