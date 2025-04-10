using System;
using System.Windows;
using WPFGrowerApp.ViewModels;
using System.Linq;
using BoldReports.UI.Xaml;
using BoldReports.Windows;
// TODO: Add correct using for ReportDataSourceInfo if found
// using BoldReports.Windows.DataSource; 

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for BoldReportViewerWindow.xaml
    /// </summary>
    public partial class BoldReportViewerWindow : Window
    {
        public BoldReportViewerWindow()
        {
            InitializeComponent();
        }

        private void ReportViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // Check for ReportStream instead of ReportPath
            if (DataContext is BoldReportViewerViewModel viewModel &&
                viewModel.ReportStream != null && // Check if stream exists
                viewModel.ReportDataSources != null &&
                viewModel.ReportDataSources.Any())
            {
                try
                {
                    // Load report from stream
                    reportViewer.LoadReport(viewModel.ReportStream);

                    // Clear existing data sources
                    reportViewer.DataSources.Clear();
                    
                    // Add each data source
                    foreach (var dataSource in viewModel.ReportDataSources)
                    {
                        if (dataSource is ReportDataSource reportDataSource)
                        {
                            reportViewer.DataSources.Add(reportDataSource);
                        }
                    }

                    // Set parameters if they exist
                    if (viewModel.ReportParameters != null && viewModel.ReportParameters.Any())
                    {
                        reportViewer.SetParameters(viewModel.ReportParameters);
                    }

                    reportViewer.RefreshReport();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load report: {ex.Message}", "Report Load Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // Dispose the stream after loading (important!)
                finally
                {
                    viewModel.ReportStream?.Dispose();
                }
            }
            // Update checks to use ReportStream
            else if (DataContext is BoldReportViewerViewModel vmCheck &&
                     vmCheck.ReportStream == null)
            {
                MessageBox.Show("Report stream is not set in the ViewModel.", "Report Load Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if (DataContext is BoldReportViewerViewModel vmCheck2 &&
                    (vmCheck2.ReportDataSources == null || !vmCheck2.ReportDataSources.Any()))
            {
                MessageBox.Show("Report data sources are not set in the ViewModel.", "Report Load Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // If needed, add methods here to interact with the viewer, 
        // although ideally interaction is handled via ViewModel bindings.
    }
}
