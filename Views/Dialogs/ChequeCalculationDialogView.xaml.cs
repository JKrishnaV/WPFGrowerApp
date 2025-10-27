using System.Windows;
using WPFGrowerApp.ViewModels.Dialogs;

namespace WPFGrowerApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ChequeCalculationDialogView.xaml
    /// </summary>
    public partial class ChequeCalculationDialogView : Window
    {
        public ChequeCalculationDialogView()
        {
            InitializeComponent();
        }

        public ChequeCalculationDialogView(ChequeCalculationDialogViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }
    }
}
