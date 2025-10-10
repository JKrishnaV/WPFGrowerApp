using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.ViewModels;

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

        private void BatchesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Handle double-click to view batch details
            // Check if the click was on an actual row (not on empty space or header)
            var row = ItemsControl.ContainerFromElement((DataGrid)sender,
                e.OriginalSource as DependencyObject) as DataGridRow;

            if (row != null && row.Item != null && DataContext is PaymentBatchViewModel viewModel)
            {
                // Execute the ViewBatchCommand
                viewModel.ViewBatchCommand.Execute(null);
            }
        }
    }
}


