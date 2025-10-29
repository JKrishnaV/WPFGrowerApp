using System.Windows.Controls;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views.Reports
{
    /// <summary>
    /// Interaction logic for PaymentSummaryReportView.xaml
    /// </summary>
    public partial class PaymentSummaryReportView : UserControl
    {
        public PaymentSummaryReportView()
        {
            InitializeComponent();
            
            // Set focus to the main control for keyboard shortcuts
            Loaded += (s, e) => Focus();
        }
    }
}
