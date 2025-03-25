using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using AspNetCore.Reporting;
using System.Text;
using System.Text.RegularExpressions;
using WPFGrowerApp.DataAccess;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using MessageBox = System.Windows.MessageBox;
using WebBrowser = System.Windows.Controls.WebBrowser;
using mshtml;

namespace WPFGrowerApp.Reports
{
    public partial class ReportViewerWindow : Window
    {
        private readonly ReportService _reportService;
        private readonly ReportDataManager _reportDataManager;
        private readonly DatabaseService _databaseService;
        private WebBrowser _reportViewer;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private string _currentReportPath;
        private byte[] _currentReportData;

        public ReportViewerWindow(DatabaseService databaseService)
        {
            InitializeComponent();
            
            // Initialize encoding for reports
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _databaseService = databaseService;
            _reportDataManager = new ReportDataManager(databaseService);
            _reportService = new ReportService();
            _reportViewer = reportContainer;

            cboReportType.Items.Add("Grower Summary");
            cboReportType.Items.Add("Grower Details");
            cboReportType.Items.Add("Financial Summary");
            cboReportType.SelectedIndex = 0;

            cboReportType.SelectionChanged += CboReportType_SelectionChanged;
            Loaded += ReportViewerWindow_Loaded;

            // Initialize navigation controls
            UpdateNavigationControls();
            _reportViewer.LoadCompleted += ReportViewer_LoadCompleted;
        }

        private void UpdateNavigationControls()
        {
            txtCurrentPage.Text = _currentPage.ToString();
            txtTotalPages.Text = _totalPages.ToString();
            btnFirstPage.IsEnabled = _currentPage > 1;
            btnPreviousPage.IsEnabled = _currentPage > 1;
            btnNextPage.IsEnabled = _currentPage < _totalPages;
            btnLastPage.IsEnabled = _currentPage < _totalPages;
        }

        private async void ReportViewer_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            try
            {
                // Wait for the document to be ready
                await Task.Delay(100);
                
                if (_reportViewer?.Document == null)
                {
                    return;
                }

                // Get the document
                var doc = (IHTMLDocument2)_reportViewer.Document;
                if (doc?.body == null)
                {
                    return;
                }

                // Apply zoom level
                ApplyZoomLevel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in load completed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyZoomLevel()
        {
            try
            {
                if (_reportViewer?.Document == null)
                {
                    return;
                }

                // Get the document
                var doc = (IHTMLDocument2)_reportViewer.Document;
                if (doc?.body == null)
                {
                    return;
                }

                var zoom = cboZoom?.SelectedItem as ComboBoxItem;
                if (zoom == null)
                {
                    return;
                }

                string zoomLevel = zoom.Content?.ToString() ?? "100%";
                
                // Add CSS styles for better report display
                var style = doc.createStyleSheet();
                if (style != null)
                {
                    style.cssText = @"
                        body { margin: 0; padding: 0; }
                        .page { margin: 10px auto; box-shadow: 0 0 10px rgba(0,0,0,0.1); }
                    ";
                }

                if (zoomLevel == "Page Width")
                {
                    doc.body.style.setAttribute("width", "100%", 0);
                    doc.body.style.setAttribute("zoom", "100%", 0);
                }
                else if (zoomLevel == "Whole Page")
                {
                    doc.body.style.setAttribute("zoom", "100%", 0);
                    doc.body.style.setAttribute("margin", "0", 0);
                    doc.body.style.setAttribute("padding", "0", 0);
                }
                else
                {
                    if (zoomLevel.EndsWith("%"))
                    {
                        zoomLevel = zoomLevel.TrimEnd('%');
                    }
                    if (int.TryParse(zoomLevel, out int zoomPercent))
                    {
                        doc.body.style.setAttribute("zoom", $"{zoomPercent}%", 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying zoom: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReportViewerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async void CboReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            try
            {
                string reportPath = string.Empty;
                string reportName = string.Empty;
                object reportData = null;

                switch (cboReportType.SelectedItem?.ToString() ?? "")
                {
                    case "Grower Summary":
                        reportPath = "Reports/GrowerSummary.rdlc";
                        reportName = "GrowerDataSet";
                        reportData = await _reportDataManager.GetGrowerSummaryDataAsync();
                        break;
                    
                    case "Grower Details":
                        reportPath = "Reports/GrowerDetails.rdlc";
                        reportName = "GrowerDetailsDataSet";
                        reportData = await _reportDataManager.GetGrowerDetailsDataAsync("1");
                        break;
                    
                    case "Financial Summary":
                        reportPath = "Reports/FinancialSummary.rdlc";
                        reportName = "FinancialDataSet";
                        reportData = await _reportDataManager.GetFinancialSummaryDataAsync();
                        break;
                }

                if (!File.Exists(reportPath))
                {
                    MessageBox.Show($"Report file not found: {reportPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _currentReportPath = reportPath;

                using (var report = _reportService.LoadReport(reportPath))
                {
                    report.AddDataSource(new ReportDataSource(reportName, reportData));
                    var result = report.Execute(RenderType.Html);
                    _currentReportData = result.MainStream;
                    
                    // Create a temporary HTML file with proper styling
                    var tempFile = Path.GetTempFileName() + ".html";
                    var htmlContent = Encoding.UTF8.GetString(result.MainStream);
                    
                    // Add CSS for better page display
                    var styledHtml = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ margin: 0; padding: 20px; }}
        .page {{ 
            margin: 10px auto;
            padding: 20px;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
            background: white;
        }}
        @media print {{
            .page {{ 
                margin: 0;
                padding: 0;
                box-shadow: none;
            }}
        }}
    </style>
</head>
<body>
{htmlContent}
</body>
</html>";
                    
                    File.WriteAllText(tempFile, styledHtml, Encoding.UTF8);
                    
                    // Navigate to the temporary file
                    _reportViewer.Navigate(new Uri(tempFile));

                    // Reset to first page when loading new report
                    _currentPage = 1;
                    _totalPages = CalculateTotalPages(result.MainStream);
                    UpdateNavigationControls();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int CalculateTotalPages(byte[] reportData)
        {
            try
            {
                string htmlContent = Encoding.UTF8.GetString(reportData);
                var pageBreaks = Regex.Split(htmlContent, "page-break-after", RegexOptions.IgnoreCase);
                return Math.Max(1, pageBreaks.Length);
            }
            catch
            {
                return 1;
            }
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            NavigateToPage();
        }

        private void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                NavigateToPage();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                NavigateToPage();
            }
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            NavigateToPage();
        }

        private void NavigateToPage()
        {
            UpdateNavigationControls();
            try
            {
                if (_reportViewer?.Document == null)
                {
                    return;
                }

                var doc = (IHTMLDocument2)_reportViewer.Document;
                if (doc == null)
                {
                    return;
                }

                var body = doc.body;
                if (body == null)
                {
                    return;
                }

                var pages = doc.all.tags("div") as IHTMLElementCollection;
                if (pages == null)
                {
                    return;
                }

                for (int i = 0; i < pages.length; i++)
                {
                    var page = pages.item(i, i) as IHTMLElement;
                    if (page?.className == "page")
                    {
                        page.style.display = (i + 1 == _currentPage) ? "block" : "none";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error navigating to page: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ZoomLevel_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyZoomLevel();
        }

        private void NumberValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadReportAsync();
        }

        private async void ExportToPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    DefaultExt = ".pdf",
                    Filter = "PDF Files (*.pdf)|*.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var report = _reportService.LoadReport(_currentReportPath))
                    {
                        report.AddDataSource(new ReportDataSource(GetCurrentDataSetName(), await GetCurrentReportData()));
                        var result = report.Execute(RenderType.Pdf);
                        File.WriteAllBytes(saveFileDialog.FileName, result.MainStream);
                        MessageBox.Show("Report exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    DefaultExt = ".xlsx",
                    Filter = "Excel Files (*.xlsx)|*.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var report = _reportService.LoadReport(_currentReportPath))
                    {
                        report.AddDataSource(new ReportDataSource(GetCurrentDataSetName(), await GetCurrentReportData()));
                        var result = report.Execute(RenderType.Excel);
                        File.WriteAllBytes(saveFileDialog.FileName, result.MainStream);
                        MessageBox.Show("Report exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EmailReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Email functionality will be implemented in a future update.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string GetCurrentDataSetName()
        {
            switch (cboReportType.SelectedItem?.ToString() ?? "")
            {
                case "Grower Summary":
                    return "GrowerDataSet";
                case "Grower Details":
                    return "GrowerDetailsDataSet";
                case "Financial Summary":
                    return "FinancialDataSet";
                default:
                    throw new InvalidOperationException("Invalid report type selected");
            }
        }

        private async Task<object> GetCurrentReportData()
        {
            switch (cboReportType.SelectedItem?.ToString() ?? "")
            {
                case "Grower Summary":
                    return await _reportDataManager.GetGrowerSummaryDataAsync();
                case "Grower Details":
                    return await _reportDataManager.GetGrowerDetailsDataAsync("1");
                case "Financial Summary":
                    return await _reportDataManager.GetFinancialSummaryDataAsync();
                default:
                    throw new InvalidOperationException("Invalid report type selected");
            }
        }
    }
} 