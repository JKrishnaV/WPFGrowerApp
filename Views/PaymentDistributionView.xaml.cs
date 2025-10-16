using System.Windows.Controls;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    public partial class PaymentDistributionView : UserControl
    {
        public PaymentDistributionView()
        {
            InitializeComponent();
            DataContext = new PaymentDistributionViewModel(
                App.ServiceProvider.GetService(typeof(WPFGrowerApp.DataAccess.Interfaces.IPaymentDistributionService)) as WPFGrowerApp.DataAccess.Interfaces.IPaymentDistributionService,
                App.ServiceProvider.GetService(typeof(WPFGrowerApp.DataAccess.Interfaces.IPaymentBatchService)) as WPFGrowerApp.DataAccess.Interfaces.IPaymentBatchService);
        }
    }
}
