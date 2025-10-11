using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PaymentBatchDetailView.xaml
    /// </summary>
    public partial class PaymentBatchDetailView : UserControl
    {
        public PaymentBatchDetailView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Set focus for keyboard shortcuts
            Loaded += (s, e) => Focus();
        }
    }
}

