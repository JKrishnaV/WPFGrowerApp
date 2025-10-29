using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views.Reports
{
    public partial class ReportsView : UserControl
    {
        private ReportsViewModel _viewModel;

        public ReportsView()
        {
            InitializeComponent();
            this.DataContextChanged += ReportsView_DataContextChanged;
        }

        private void ReportsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = e.NewValue as ReportsViewModel;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                // Update the view based on the current SelectedReportType
                UpdateReportVisibility(_viewModel.SelectedReportType);
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ReportsViewModel.SelectedReportType))
            {
                UpdateReportVisibility(_viewModel.SelectedReportType);
            }
        }

        private void ReportTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PieChartView == null || DetailReportView == null || PaymentSummaryView == null || PaymentSummaryTestView == null)
                return;

            string selectedItem = (ReportTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty;

            // Update the selected report type in the view model
            if (_viewModel != null)
            {
                if (selectedItem.StartsWith("Pie Chart"))
                {
                    if (selectedItem.Contains("Province"))
                        _viewModel.SelectedReportType = "Province Distribution";
                    else if (selectedItem.Contains("Price Level"))
                        _viewModel.SelectedReportType = "Price Level Distribution";
                    else if (selectedItem.Contains("Pay Group"))
                        _viewModel.SelectedReportType = "Pay Group Distribution";
                }
                else if (selectedItem == "Grower Detail Report")
                {
                    _viewModel.SelectedReportType = "Grower Detail Report";
                }
                else if (selectedItem == "Payment Summary Report")
                {
                    _viewModel.SelectedReportType = "Payment Summary Report";
                }
                else if (selectedItem == "Payment Summary Test & Validation")
                {
                    _viewModel.SelectedReportType = "Payment Summary Test & Validation";
                }
            }
        }

        private void UpdateReportVisibility(string selectedReportType)
        {
            if (PieChartView == null || DetailReportView == null || PaymentSummaryView == null || PaymentSummaryTestView == null)
                return;

            // Hide all views first
            PieChartView.Visibility = Visibility.Collapsed;
            DetailReportView.Visibility = Visibility.Collapsed;
            PaymentSummaryView.Visibility = Visibility.Collapsed;
            PaymentSummaryTestView.Visibility = Visibility.Collapsed;

            // Show the appropriate view based on the selected report type
            if (selectedReportType == "Province Distribution" || 
                selectedReportType == "Price Level Distribution" || 
                selectedReportType == "Pay Group Distribution")
            {
                PieChartView.Visibility = Visibility.Visible;
            }
            else if (selectedReportType == "Grower Detail Report")
            {
                DetailReportView.Visibility = Visibility.Visible;
            }
            else if (selectedReportType == "Payment Summary Report")
            {
                PaymentSummaryView.Visibility = Visibility.Visible;
            }
            else if (selectedReportType == "Payment Summary Test & Validation")
            {
                PaymentSummaryTestView.Visibility = Visibility.Visible;
            }
        }
    }
}
