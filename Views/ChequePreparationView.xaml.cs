using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ChequePreparationView.xaml
    /// </summary>
    public partial class ChequePreparationView : UserControl
    {
        public ChequePreparationView()
        {
            InitializeComponent();
            Loaded += (s, e) => Focus(); // Enable immediate keyboard shortcuts
            Loaded += ChequePreparationView_Loaded;
        }

        private void ChequePreparationView_Loaded(object sender, RoutedEventArgs e)
        {
            // Ensure DataGrid selection works properly
            if (ChequesDataGrid != null)
            {
                ChequesDataGrid.SelectionChanged += ChequesDataGrid_SelectionChanged;
            }
        }

        private void ChequesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection changes to ensure proper binding
            if (sender is DataGrid dataGrid && DataContext is ChequePreparationViewModel viewModel)
            {
                if (dataGrid.SelectedItem is WPFGrowerApp.DataAccess.Models.Cheque selectedCheque)
                {
                    viewModel.SelectedCheque = selectedCheque;
                }
                else
                {
                    viewModel.SelectedCheque = null;
                }
            }
        }

        private void ChequesDataGrid_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Handle mouse clicks on the DataGrid to ensure row selection works
            if (sender is DataGrid dataGrid)
            {
                // Let the DataGrid handle the click for row selection
                // This ensures that clicking anywhere on a row selects it
            }
        }
    }
}