using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PaymentManagementHubView.xaml
    /// </summary>
    public partial class PaymentManagementHubView : UserControl
    {
        public PaymentManagementHubView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }
    }
}
