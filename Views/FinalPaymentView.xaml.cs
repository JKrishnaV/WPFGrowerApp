using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for FinalPaymentView.xaml
    /// </summary>
    public partial class FinalPaymentView : UserControl
    {
        public FinalPaymentView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Auto-focus to enable keyboard shortcuts immediately
            Loaded += (s, e) => Focus();
        }
    }
}

