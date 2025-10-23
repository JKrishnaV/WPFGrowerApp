using System.Windows;
using WPFGrowerApp.ViewModels.Dialogs;

namespace WPFGrowerApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for InvoiceStyleChequeCalculationDialogView.xaml
    /// </summary>
    public partial class InvoiceStyleChequeCalculationDialogView : Window
    {
        public InvoiceStyleChequeCalculationDialogView()
        {
            InitializeComponent();
        }

        public InvoiceStyleChequeCalculationDialogView(ChequeCalculationDialogViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
