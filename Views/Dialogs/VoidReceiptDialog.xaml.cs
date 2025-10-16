using System.Windows.Controls;
using WPFGrowerApp.ViewModels.Dialogs;

namespace WPFGrowerApp.Views.Dialogs
{
    public partial class VoidReceiptDialog : UserControl
    {
        public VoidReceiptDialog(string receiptNumber)
        {
            InitializeComponent();
            DataContext = new VoidReceiptDialogViewModel(
                App.ServiceProvider.GetService(typeof(WPFGrowerApp.DataAccess.Interfaces.IReceiptVoidService)) as WPFGrowerApp.DataAccess.Interfaces.IReceiptVoidService,
                receiptNumber);
        }
    }
}
