using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Services;

namespace WPFGrowerApp.Views.Reports
{
    /// <summary>
    /// Interaction logic for PaymentSummaryReportTestView.xaml
    /// </summary>
    public partial class PaymentSummaryReportTestView : UserControl
    {
        private PaymentSummaryReportTestDataGenerator _testDataGenerator;
        private PaymentSummaryReportValidationService _validationService;
        private PaymentSummaryReport _currentTestReport;

        public PaymentSummaryReportTestView()
        {
            InitializeComponent();
            
            _testDataGenerator = new PaymentSummaryReportTestDataGenerator();
            _validationService = new PaymentSummaryReportValidationService();
            
            UpdateStatus("Ready for testing");
        }

        #region Event Handlers

        private async void GenerateTestDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Generating test data...");
                
                var growerCount = int.TryParse(GrowerCountTextBox.Text, out int count) ? count : 50;
                var monthsBack = int.TryParse(MonthsBackTextBox.Text, out int months) ? months : 12;
                
                var scenario = TestScenarioComboBox.SelectedItem as ComboBoxItem;
                var scenarioText = scenario?.Content?.ToString() ?? "Normal Data";
                
                var stopwatch = Stopwatch.StartNew();
                
                // Generate test data based on scenario
                _currentTestReport = scenarioText switch
                {
                    "Edge Cases" => _testDataGenerator.GenerateEdgeCaseTestData(),
                    "Large Dataset" => _testDataGenerator.GenerateTestReport(1000, monthsBack),
                    "Empty Dataset" => new PaymentSummaryReport
                    {
                        ReportDate = DateTime.Now,
                        PeriodStart = DateTime.Now.AddMonths(-monthsBack),
                        PeriodEnd = DateTime.Now,
                        ReportTitle = "Empty Test Report",
                        GeneratedBy = "TestDataGenerator",
                        ReportDescription = "Empty dataset for testing",
                        GrowerDetails = new List<GrowerPaymentDetail>(),
                        PaymentDistribution = new List<WPFGrowerApp.DataAccess.Models.PaymentDistributionChart>(),
                        MonthlyTrends = new List<WPFGrowerApp.DataAccess.Models.MonthlyTrendChart>(),
                        TopPerformers = new List<WPFGrowerApp.DataAccess.Models.GrowerPerformanceChart>()
                    },
                    _ => _testDataGenerator.GenerateTestReport(growerCount, monthsBack)
                };
                
                stopwatch.Stop();
                
                // Update UI with test data
                UpdateTestDataSummary(_currentTestReport);
                TestDataGrid.ItemsSource = _currentTestReport.GrowerDetails;
                
                UpdateStatus($"Test data generated successfully in {stopwatch.ElapsedMilliseconds}ms. Generated {_currentTestReport.TotalGrowers} growers.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error generating test data: {ex.Message}");
                MessageBox.Show($"Error generating test data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ValidateDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTestReport == null)
            {
                MessageBox.Show("Please generate test data first.", "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                UpdateStatus("Validating test data...");
                
                var stopwatch = Stopwatch.StartNew();
                var validationResult = _validationService.ValidateReport(_currentTestReport);
                stopwatch.Stop();
                
                // Update validation results
                UpdateValidationResults(validationResult);
                
                var statusMessage = validationResult.IsValid 
                    ? $"Validation completed successfully in {stopwatch.ElapsedMilliseconds}ms. No errors found."
                    : $"Validation completed in {stopwatch.ElapsedMilliseconds}ms. Found {validationResult.Errors.Count} errors and {validationResult.Warnings.Count} warnings.";
                
                UpdateStatus(statusMessage);
                
                if (!validationResult.IsValid)
                {
                    MessageBox.Show($"Validation found {validationResult.Errors.Count} errors and {validationResult.Warnings.Count} warnings. Check the Validation Results tab for details.", 
                                   "Validation Issues", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error validating data: {ex.Message}");
                MessageBox.Show($"Error validating data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RunPerformanceTestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Running performance test...");
                
                var datasetSize = int.Parse((PerformanceDatasetSizeComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "500");
                var testType = (PerformanceTestTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Data Generation";
                
                var stopwatch = Stopwatch.StartNew();
                var results = new List<string>();
                
                switch (testType)
                {
                    case "Data Generation":
                        results.AddRange(await RunDataGenerationPerformanceTest(datasetSize));
                        break;
                    case "Report Generation":
                        results.AddRange(await RunReportGenerationPerformanceTest(datasetSize));
                        break;
                    case "Export Test":
                        results.AddRange(await RunExportPerformanceTest(datasetSize));
                        break;
                    case "Validation Test":
                        results.AddRange(await RunValidationPerformanceTest(datasetSize));
                        break;
                }
                
                stopwatch.Stop();
                
                var performanceResults = string.Join("\n", results);
                PerformanceResultsTextBlock.Text = $"Performance Test Results ({testType})\n" +
                                                 $"Dataset Size: {datasetSize} growers\n" +
                                                 $"Total Time: {stopwatch.ElapsedMilliseconds}ms\n" +
                                                 $"Average Time per Grower: {(double)stopwatch.ElapsedMilliseconds / datasetSize:F2}ms\n\n" +
                                                 performanceResults;
                
                UpdateStatus($"Performance test completed in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error running performance test: {ex.Message}");
                MessageBox.Show($"Error running performance test: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Performance Test Methods

        private async Task<List<string>> RunDataGenerationPerformanceTest(int datasetSize)
        {
            var results = new List<string>();
            var iterations = 3;
            
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                var testReport = _testDataGenerator.GenerateTestReport(datasetSize, 12);
                stopwatch.Stop();
                
                results.Add($"Iteration {i + 1}: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            return results;
        }

        private async Task<List<string>> RunReportGenerationPerformanceTest(int datasetSize)
        {
            var results = new List<string>();
            var iterations = 3;
            
            for (int i = 0; i < iterations; i++)
            {
                var testReport = _testDataGenerator.GenerateTestReport(datasetSize, 12);
                
                var stopwatch = Stopwatch.StartNew();
                // Simulate report generation processing
                await Task.Delay(100); // Simulate processing time
                stopwatch.Stop();
                
                results.Add($"Iteration {i + 1}: {stopwatch.ElapsedMilliseconds}ms");
            }
            
            return results;
        }

        private async Task<List<string>> RunExportPerformanceTest(int datasetSize)
        {
            var results = new List<string>();
            var testReport = _testDataGenerator.GenerateTestReport(datasetSize, 12);
            var exportService = new ReportExportService();
            
            var formats = new[] { "PDF", "Excel", "CSV", "Word" };
            
            foreach (var format in formats)
            {
                var stopwatch = Stopwatch.StartNew();
                
                var exportOptions = new ExportOptions
                {
                    ExportFormat = format,
                    IncludeCharts = true,
                    IncludeSummaryStatistics = true,
                    IncludeDetailedData = true
                };
                exportOptions.SetDefaultFileName($"PerformanceTest_{format}_{DateTime.Now:yyyyMMdd_HHmmss}");
                
                try
                {
                    switch (format)
                    {
                        case "PDF":
                            await exportService.ExportToPdfAsync(testReport, exportOptions);
                            break;
                        case "Excel":
                            await exportService.ExportToExcelAsync(testReport, exportOptions);
                            break;
                        case "CSV":
                            await exportService.ExportToCsvAsync(testReport, exportOptions);
                            break;
                        case "Word":
                            await exportService.ExportToWordAsync(testReport, exportOptions);
                            break;
                    }
                    
                    stopwatch.Stop();
                    results.Add($"{format} Export: {stopwatch.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    results.Add($"{format} Export: Failed - {ex.Message}");
                }
            }
            
            return results;
        }

        private async Task<List<string>> RunValidationPerformanceTest(int datasetSize)
        {
            var results = new List<string>();
            var iterations = 3;
            
            for (int i = 0; i < iterations; i++)
            {
                var testReport = _testDataGenerator.GenerateTestReport(datasetSize, 12);
                
                var stopwatch = Stopwatch.StartNew();
                var validationResult = _validationService.ValidateReport(testReport);
                stopwatch.Stop();
                
                results.Add($"Iteration {i + 1}: {stopwatch.ElapsedMilliseconds}ms ({validationResult.Errors.Count} errors, {validationResult.Warnings.Count} warnings)");
            }
            
            return results;
        }

        #endregion

        #region Helper Methods

        private void UpdateTestDataSummary(PaymentSummaryReport report)
        {
            TestGrowersCount.Text = report.TotalGrowers.ToString("N0");
            TestReceiptsCount.Text = report.TotalReceipts.ToString("N0");
            TestPaymentsTotal.Text = report.TotalPaymentsMade.ToString("C0");
            TestWeightTotal.Text = report.TotalWeight.ToString("N0") + " lbs";
        }

        private void UpdateValidationResults(WPFGrowerApp.DataAccess.Services.ValidationResult result)
        {
            ValidationErrorsCount.Text = result.Errors.Count.ToString();
            ValidationWarningsCount.Text = result.Warnings.Count.ToString();
            
            ErrorsListBox.ItemsSource = result.Errors;
            WarningsListBox.ItemsSource = result.Warnings;
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }

        #endregion
    }
}
