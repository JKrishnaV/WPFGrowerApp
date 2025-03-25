using System.Windows;
using System.Windows.Controls;

namespace WPFGrowerApp.Views.Reports
{
    public partial class ReportsView : UserControl
    {
        public ReportsView()
        {
            InitializeComponent();
        }

        private void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PieChartView == null || DetailReportView == null)
                return;

            string selectedItem = (ReportTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty;

            if (selectedItem.StartsWith("Pie Chart"))
            {
                PieChartView.Visibility = Visibility.Visible;
                DetailReportView.Visibility = Visibility.Collapsed;

                // Update the selected report type in the pie chart view model
                if (PieChartView.DataContext is ViewModels.ReportsViewModel viewModel)
                {
                    if (selectedItem.Contains("Province"))
                        viewModel.SelectedReportType = "Province Distribution";
                    else if (selectedItem.Contains("Price Level"))
                        viewModel.SelectedReportType = "Price Level Distribution";
                    else if (selectedItem.Contains("Pay Group"))
                        viewModel.SelectedReportType = "Pay Group Distribution";
                }
            }
            else
            {
                PieChartView.Visibility = Visibility.Collapsed;
                DetailReportView.Visibility = Visibility.Visible;
            }
        }
    }
}
