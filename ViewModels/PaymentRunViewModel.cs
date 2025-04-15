using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using System.Windows.Data; // For ICollectionView
using System.Collections.Specialized; // For INotifyCollectionChanged
using WPFGrowerApp.Models; // Added for AdvanceOption

// Need this for the PdfViewerWindow call later
using WPFGrowerApp.Views;
// Need this for the PaymentTestRunReportViewModel call later
using WPFGrowerApp.ViewModels; // Assuming PaymentTestRunReportViewModel is here now
// Need this for MemoryStream
using System.IO;
// Add using for Bold Reports (Found in code-behind)
using BoldReports.Windows;


namespace WPFGrowerApp.ViewModels
{
    public class PaymentRunViewModel : ViewModelBase, IProgress<string>
    {
        private readonly IPaymentService _paymentService;
        private readonly IDialogService _dialogService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IPayGroupService _payGroupService;
        private readonly IGrowerService _growerService;

        private int _advanceNumber = 1;
        private DateTime _paymentDate = DateTime.Today;
        private DateTime _cutoffDate = DateTime.Today;
        private int _cropYear = DateTime.Today.Year;
        private bool _isRunning;
        private string _statusMessage;
        private ObservableCollection<string> _runLog;
        private PostBatch _lastRunBatch; // For actual run
        private List<string> _lastRunErrors; // For actual run
        private TestRunResult _latestTestRunResult; // For test run

        // --- Collections ---
        // Original backing collections for all items
        private ObservableCollection<Product> _allProducts;
        private ObservableCollection<Process> _allProcesses;
        private ObservableCollection<PayGroup> _allPayGroups;
        private ObservableCollection<GrowerInfo> _allGrowers;

        // ICollectionView wrappers for filtering ListBoxes
        private ICollectionView _filteredProductsView;
        private ICollectionView _filteredProcessesView;
        private ICollectionView _filteredPayGroupsView;
        private ICollectionView _filteredGrowersView;

        // Collection for Advance Number ComboBox
        public ObservableCollection<AdvanceOption> AdvanceOptions { get; } = new ObservableCollection<AdvanceOption>();

        // Collection for Crop Year ComboBox
        public ObservableCollection<int> CropYears { get; } = new ObservableCollection<int>();

        // Search Text for Grower Filter
        private string _growerSearchText;
        public string GrowerSearchText
        {
            get => _growerSearchText;
            set
            {
                if (SetProperty(ref _growerSearchText, value))
                {
                    _filteredGrowersView?.Refresh(); // Refresh the filtered view
                }
            }
        }

        private string _productSearchText;
        public string ProductSearchText
        {
            get => _productSearchText;
            set
            {
                if (SetProperty(ref _productSearchText, value))
                {
                    _filteredProductsView?.Refresh();
                }
            }
        }

        private string _processSearchText;
        public string ProcessSearchText
        {
            get => _processSearchText;
            set
            {
                if (SetProperty(ref _processSearchText, value))
                {
                    _filteredProcessesView?.Refresh();
                }
            }
        }

        private string _payGroupSearchText;
        public string PayGroupSearchText
        {
            get => _payGroupSearchText;
            set
            {
                if (SetProperty(ref _payGroupSearchText, value))
                {
                    _filteredPayGroupsView?.Refresh();
                }
            }
        }
        // --- End Search Text Properties ---


        // Collections to hold selected items from ListBoxes
        public ObservableCollection<Product> SelectedProducts { get; private set; } = new ObservableCollection<Product>();
        public ObservableCollection<Process> SelectedProcesses { get; private set; } = new ObservableCollection<Process>();
        public ObservableCollection<PayGroup> SelectedExcludePayGroups { get; private set; } = new ObservableCollection<PayGroup>();
        public ObservableCollection<GrowerInfo> SelectedExcludeGrowers { get; private set; } = new ObservableCollection<GrowerInfo>();

        // On Hold List properties
        private ObservableCollection<GrowerInfo> _onHoldGrowers;
        private bool _isLoadingOnHoldGrowers;


        public PaymentRunViewModel(
            IPaymentService paymentService,
            IDialogService dialogService,
            IProductService productService,
            IProcessService processService,
            IPayGroupService payGroupService,
            IGrowerService growerService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));

            RunLog = new ObservableCollection<string>();
            LastRunErrors = new List<string>();

            // Initialize backing collections
            _allProducts = new ObservableCollection<Product>();
            _allProcesses = new ObservableCollection<Process>();
            _allPayGroups = new ObservableCollection<PayGroup>();
            _allGrowers = new ObservableCollection<GrowerInfo>();
            OnHoldGrowers = new ObservableCollection<GrowerInfo>();

            // Setup filtered views
            _filteredProductsView = CollectionViewSource.GetDefaultView(_allProducts);
            _filteredProductsView.Filter = FilterProducts; // Assign filter predicate
            _filteredProcessesView = CollectionViewSource.GetDefaultView(_allProcesses);
            _filteredProcessesView.Filter = FilterProcesses; // Assign filter predicate
            _filteredPayGroupsView = CollectionViewSource.GetDefaultView(_allPayGroups);
            _filteredPayGroupsView.Filter = FilterPayGroups; // Assign filter predicate
            _filteredGrowersView = CollectionViewSource.GetDefaultView(_allGrowers);
            _filteredGrowersView.Filter = FilterGrowers; // Existing filter

            // Populate Advance Options
            AdvanceOptions.Add(new AdvanceOption { Display = "First Advance Payment", Value = 1 });
            AdvanceOptions.Add(new AdvanceOption { Display = "Second Advance Payment", Value = 2 });
            AdvanceOptions.Add(new AdvanceOption { Display = "Third Advance Payment", Value = 3 });

            // Populate Crop Years
            int currentYear = DateTime.Today.Year;
            CropYears.Add(currentYear);
            CropYears.Add(currentYear - 1);
            CropYears.Add(currentYear - 2);
            _cropYear = currentYear; // Set default

            // Add listeners for selection changes
            SelectedProducts.CollectionChanged += SelectedFilters_CollectionChanged;
            SelectedProcesses.CollectionChanged += SelectedFilters_CollectionChanged;
            SelectedExcludePayGroups.CollectionChanged += SelectedFilters_CollectionChanged;
            SelectedExcludeGrowers.CollectionChanged += SelectedFilters_CollectionChanged;


            // Load initial data
            _ = LoadFiltersAsync();
            _ = UpdateOnHoldGrowersAsync(); // Initial load
        }

        // Filter logic for the Grower ListBox
        private bool FilterGrowers(object item)
        {
            if (string.IsNullOrEmpty(GrowerSearchText))
            {
                return true; // No filter applied
            }

            if (item is GrowerInfo grower)
            {
                // Check if name or number contains the search text (case-insensitive)
                return (grower.Name?.IndexOf(GrowerSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (grower.GrowerNumber.ToString().IndexOf(GrowerSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return false;
        }

        // Filter logic for Products
        private bool FilterProducts(object item)
        {
            if (string.IsNullOrEmpty(ProductSearchText)) return true;
            if (item is Product product)
            {
                return (product.ProductId?.IndexOf(ProductSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (product.Description?.IndexOf(ProductSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }

        // Filter logic for Processes
        private bool FilterProcesses(object item)
        {
            if (string.IsNullOrEmpty(ProcessSearchText)) return true;
            if (item is Process process)
            {
                return (process.ProcessId?.IndexOf(ProcessSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (process.Description?.IndexOf(ProcessSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }

        // Filter logic for PayGroups
        private bool FilterPayGroups(object item)
        {
            if (string.IsNullOrEmpty(PayGroupSearchText)) return true;
            if (item is PayGroup payGroup)
            {
                return (payGroup.PayGroupId?.IndexOf(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (payGroup.Description?.IndexOf(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }


        // Properties for UI Binding
        public int AdvanceNumber
        {
            get => _advanceNumber;
            set { SetProperty(ref _advanceNumber, value); }
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set { SetProperty(ref _paymentDate, value); }
        }

        public DateTime CutoffDate
        {
            get => _cutoffDate;
             set { SetProperty(ref _cutoffDate, value); }
        }

        public int CropYear
        {
            get => _cropYear;
            set { SetProperty(ref _cropYear, value); }
        }

         // Expose the filtered views for binding
         public ICollectionView FilteredProducts => _filteredProductsView;
         public ICollectionView FilteredProcesses => _filteredProcessesView;
         public ICollectionView FilteredPayGroups => _filteredPayGroupsView;
         public ICollectionView FilteredGrowers => _filteredGrowersView; // Existing

         // Keep original collections accessible if needed internally, but UI binds to filtered views
         // public ObservableCollection<Product> AllProducts { get => _allProducts; } // Example if needed
         // public ObservableCollection<Process> AllProcesses { get => _allProcesses; }
         // public ObservableCollection<PayGroup> AllPayGroups { get => _allPayGroups; }
         // public ObservableCollection<GrowerInfo> AllGrowers { get => _allGrowers; }


         // Properties for On Hold List
         public ObservableCollection<GrowerInfo> OnHoldGrowers { get => _onHoldGrowers; private set => SetProperty(ref _onHoldGrowers, value); }
         public bool IsLoadingOnHoldGrowers { get => _isLoadingOnHoldGrowers; private set => SetProperty(ref _isLoadingOnHoldGrowers, value); }


        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    // Ensure all commands update their CanExecute status
                    ((RelayCommand)StartPaymentRunCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)TestRunCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ViewBoldReportCommand).RaiseCanExecuteChanged(); // Add new command here
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> RunLog
        {
            get => _runLog;
            private set => SetProperty(ref _runLog, value);
        }

         public PostBatch LastRunBatch
        {
            get => _lastRunBatch;
            private set => SetProperty(ref _lastRunBatch, value);
        }

        public List<string> LastRunErrors
        {
            get => _lastRunErrors;
            private set => SetProperty(ref _lastRunErrors, value);
        }

        public TestRunResult LatestTestRunResult
        {
            get => _latestTestRunResult;
            private set => SetProperty(ref _latestTestRunResult, value);
        }


        // Commands
        public ICommand LoadFiltersCommand => new RelayCommand(async o => await LoadFiltersAsync());
        public ICommand StartPaymentRunCommand => new RelayCommand(async o => await StartPaymentRunAsync(), o => !IsRunning);
        public ICommand TestRunCommand => new RelayCommand(async o => await PerformTestRunAsync(), o => !IsRunning); // Shows Dialog Report
        public ICommand ViewBoldReportCommand => new RelayCommand(async o => await PerformViewBoldReportAsync(), o => !IsRunning); // Shows Bold Report Viewer


        // IProgress<string> implementation
        public void Report(string value)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                RunLog.Add($"{DateTime.Now:HH:mm:ss} - {value}");
                if (RunLog.Count > 200) RunLog.RemoveAt(0);
            });
        }

        private async Task StartPaymentRunAsync()
        {
            IsRunning = true;
            RunLog.Clear();
            LastRunErrors.Clear();
            LastRunBatch = null;
            StatusMessage = "Starting payment run...";
            Report($"Initiating Advance {AdvanceNumber} payment run...");
            Report($"Parameters: PaymentDate={PaymentDate:d}, CutoffDate={CutoffDate:d}, CropYear={CropYear}");

            // Prepare lists of IDs from selected items
            var selectedProductIds = SelectedProducts.Select(p => p.ProductId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            var selectedProcessIds = SelectedProcesses.Select(p => p.ProcessId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            var selectedExcludeGrowerIds = SelectedExcludeGrowers.Select(g => g.GrowerNumber).Where(num => num != 0).ToList();
            var selectedExcludePayGroupIds = SelectedExcludePayGroups.Select(pg => pg.PayGroupId).Where(id => !string.IsNullOrEmpty(id)).ToList();

            // Log selected filters
            Report($"Filtering by Products: {(selectedProductIds.Any() ? string.Join(",", selectedProductIds) : "All")}");
            Report($"Filtering by Processes: {(selectedProcessIds.Any() ? string.Join(",", selectedProcessIds) : "All")}");
            Report($"Excluding Growers: {(selectedExcludeGrowerIds.Any() ? string.Join(",", selectedExcludeGrowerIds) : "None")}");
            Report($"Excluding PayGroups: {(selectedExcludePayGroupIds.Any() ? string.Join(",", selectedExcludePayGroupIds) : "None")}");

            try
            {
                 // TODO: Update ProcessAdvancePaymentRunAsync signature in IPaymentService and PaymentService
                 // The following line will cause compile errors until the service is updated.
                var (success, errors, createdBatch) = await _paymentService.ProcessAdvancePaymentRunAsync(
                    AdvanceNumber,
                    PaymentDate,
                    CutoffDate,
                    CropYear,
                    // Removed the two null placeholders for includeGrowerId/includePayGroup
                    selectedExcludeGrowerIds, // Pass list of excluded grower IDs
                    selectedExcludePayGroupIds, // Pass list of excluded paygroup IDs
                    selectedProductIds, // Pass list of product IDs (assuming this means *include* only these)
                    selectedProcessIds, // Pass list of process IDs (assuming this means *include* only these)
                    this);

                LastRunBatch = createdBatch;
                LastRunErrors = errors ?? new List<string>();

                if (success)
                {
                    StatusMessage = $"Payment run completed successfully for Batch {createdBatch?.PostBat}.";
                    Report("Payment run finished successfully.");
                    await _dialogService.ShowMessageBoxAsync($"Advance {AdvanceNumber} payment run completed successfully for Batch {createdBatch?.PostBat}.", "Payment Run Complete"); // Use async
                }
                else
                {
                    StatusMessage = $"Payment run completed with errors for Batch {createdBatch?.PostBat}. Check log.";
                    Report("Payment run finished with errors:");
                    foreach(var error in LastRunErrors)
                    {
                        Report($"ERROR: {error}");
                    }
                     await _dialogService.ShowMessageBoxAsync($"The payment run encountered errors. Please review the run log.\nBatch ID: {createdBatch?.PostBat}", "Payment Run Errors"); // Use async
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Payment run failed with a critical error.";
                Report($"CRITICAL ERROR: {ex.Message}");
                Logger.Error($"Critical error during payment run execution", ex);
                await _dialogService.ShowMessageBoxAsync($"A critical error occurred: {ex.Message}", "Payment Run Failed"); // Use async
            }
            finally
            {
                IsRunning = false;
            }
        }

        // Method to run test and show results in Dialog
        private async Task PerformTestRunAsync()
        {
            IsRunning = true;
            RunLog.Clear();
            // Don't clear LastRunErrors/LastRunBatch as they belong to the actual run
            LatestTestRunResult = null; // Clear previous test result
            StatusMessage = "Starting test run simulation...";
            Report($"Initiating Advance {AdvanceNumber} test run simulation...");
            Report($"Parameters: PaymentDate={PaymentDate:d}, CutoffDate={CutoffDate:d}, CropYear={CropYear}");

            // Prepare lists of IDs from selected items (same as actual run)
            var selectedProductIds = SelectedProducts.Select(p => p.ProductId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            var selectedProcessIds = SelectedProcesses.Select(p => p.ProcessId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            var selectedExcludeGrowerIds = SelectedExcludeGrowers.Select(g => g.GrowerNumber).Where(num => num != 0).ToList();
            var selectedExcludePayGroupIds = SelectedExcludePayGroups.Select(pg => pg.PayGroupId).Where(id => !string.IsNullOrEmpty(id)).ToList();

            // Log selected filters
            Report($"Filtering by Products: {(selectedProductIds.Any() ? string.Join(",", selectedProductIds) : "All")}");
            Report($"Filtering by Processes: {(selectedProcessIds.Any() ? string.Join(",", selectedProcessIds) : "All")}");
            Report($"Excluding Growers: {(selectedExcludeGrowerIds.Any() ? string.Join(",", selectedExcludeGrowerIds) : "None")}");
            Report($"Excluding PayGroups: {(selectedExcludePayGroupIds.Any() ? string.Join(",", selectedExcludePayGroupIds) : "None")}");

            // Prepare parameters object including descriptions
            var parameters = new TestRunInputParameters
            {
                AdvanceNumber = AdvanceNumber,
                PaymentDate = PaymentDate,
                CutoffDate = CutoffDate,
                CropYear = CropYear,
                ExcludeGrowerIds = selectedExcludeGrowerIds,
                ExcludePayGroupIds = selectedExcludePayGroupIds,
                ProductIds = selectedProductIds,
                ProcessIds = selectedProcessIds,
                // Populate descriptions
                ProductDescriptions = SelectedProducts.Select(p => $"{p.ProductId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ProcessDescriptions = SelectedProcesses.Select(p => $"{p.ProcessId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ExcludedGrowerDescriptions = SelectedExcludeGrowers.Select(g => $"{g.GrowerNumber} - {g.Name}".TrimStart(' ', '-')).ToList(),
                ExcludedPayGroupDescriptions = SelectedExcludePayGroups.Select(pg => $"{pg.PayGroupId} - {pg.Description}".TrimStart(' ', '-')).ToList()
            };


            try
            {
                // Call the service method with the parameters object
                var testResult = await _paymentService.PerformAdvancePaymentTestRunAsync(
                    parameters.AdvanceNumber,
                    parameters.PaymentDate,
                    parameters.CutoffDate,
                    parameters.CropYear,
                    parameters.ExcludeGrowerIds,
                    parameters.ExcludePayGroupIds,
                    parameters.ProductIds,
                    parameters.ProcessIds,
                    this); // Pass progress reporter

                // Store the result which now includes the input parameters with descriptions
                LatestTestRunResult = testResult;
                // Update the parameters in the result object just in case the service didn't preserve the exact instance
                // (though our current service implementation does)
                if (LatestTestRunResult != null) LatestTestRunResult.InputParameters = parameters;

                if (testResult.HasAnyErrors)
                {
                    StatusMessage = "Test run simulation completed with calculation errors. Check log.";
                    Report("Test run simulation finished with errors:");
                    // Log general errors
                    foreach(var error in testResult.GeneralErrors)
                    {
                        Report($"ERROR: {error}");
                    }
                    // Log grower/receipt specific errors
                    foreach(var gp in testResult.GrowerPayments.Where(g => g.HasErrors))
                    {
                         foreach(var errorMsg in gp.ErrorMessages)
                         {
                             Report($"ERROR (Grower {gp.GrowerNumber}): {errorMsg}");
                         }
                    }
                    await _dialogService.ShowMessageBoxAsync("The test run simulation encountered errors during calculation. Please review the run log.", "Test Run Errors");
                }
                else if (!testResult.GrowerPayments.Any())
                {
                     StatusMessage = "Test run simulation completed. No eligible growers/receipts found.";
                     Report("Test run simulation finished: No eligible growers/receipts found.");
                     await _dialogService.ShowMessageBoxAsync("Test run simulation completed, but no eligible growers or receipts were found based on the selected criteria.", "Test Run Complete - No Results");
                }
                else
                {
                    StatusMessage = $"Test run simulation completed successfully. {testResult.GrowerPayments.Count} growers processed.";
                    Report("Test run simulation finished successfully.");

                    // Show the Report in a dedicated Window instead of a dialog
                    var reportViewModel = new PaymentTestRunReportViewModel(LatestTestRunResult);
                    var reportWindow = new PaymentTestRunReportWindow
                    {
                        DataContext = reportViewModel,
                        // Optional: Set owner if you want modal-like behavior relative to the main window
                        // Owner = System.Windows.Application.Current.MainWindow 
                    };
                    reportWindow.Show(); // Show as a non-modal window
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Test run simulation failed with a critical error.";
                Report($"CRITICAL ERROR during test run: {ex.Message}");
                Logger.Error($"Critical error during test run execution", ex);
                await _dialogService.ShowMessageBoxAsync($"A critical error occurred during the test run simulation: {ex.Message}", "Test Run Failed");
            }
            finally
            {
                IsRunning = false;
            }
        }

        // Method to run test and show results in PDF Viewer Window - REMOVED
        /*
        private async Task PerformViewTestReportAsync()
        {
            // ... (Code removed) ...
        }
        */

        // Method to run test and show results in Bold Report Viewer Window
        private async Task PerformViewBoldReportAsync()
        {
            IsRunning = true;
            RunLog.Clear();
            LatestTestRunResult = null;
            StatusMessage = "Starting test run for report viewer...";
            Report($"Initiating Advance {AdvanceNumber} test run for report viewer...");
            Report($"Parameters: PaymentDate={PaymentDate:d}, CutoffDate={CutoffDate:d}, CropYear={CropYear}");

            // Prepare lists of IDs from selected items (same as actual run)
            var selectedProductIds = SelectedProducts.Select(p => p.ProductId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            var selectedProcessIds = SelectedProcesses.Select(p => p.ProcessId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            var selectedExcludeGrowerIds = SelectedExcludeGrowers.Select(g => g.GrowerNumber).Where(num => num != 0).ToList();
            var selectedExcludePayGroupIds = SelectedExcludePayGroups.Select(pg => pg.PayGroupId).Where(id => !string.IsNullOrEmpty(id)).ToList();

            // Log selected filters
            Report($"Filtering by Products: {(selectedProductIds.Any() ? string.Join(",", selectedProductIds) : "All")}");
            Report($"Filtering by Processes: {(selectedProcessIds.Any() ? string.Join(",", selectedProcessIds) : "All")}");
            Report($"Excluding Growers: {(selectedExcludeGrowerIds.Any() ? string.Join(",", selectedExcludeGrowerIds) : "None")}");
            Report($"Excluding PayGroups: {(selectedExcludePayGroupIds.Any() ? string.Join(",", selectedExcludePayGroupIds) : "None")}");

            // Prepare parameters object including descriptions
            var parameters = new TestRunInputParameters
            {
                AdvanceNumber = AdvanceNumber,
                PaymentDate = PaymentDate,
                CutoffDate = CutoffDate,
                CropYear = CropYear,
                ExcludeGrowerIds = selectedExcludeGrowerIds,
                ExcludePayGroupIds = selectedExcludePayGroupIds,
                ProductIds = selectedProductIds,
                ProcessIds = selectedProcessIds,
                ProductDescriptions = SelectedProducts.Select(p => $"{p.ProductId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ProcessDescriptions = SelectedProcesses.Select(p => $"{p.ProcessId} - {p.Description}".TrimStart(' ', '-')).ToList(),
                ExcludedGrowerDescriptions = SelectedExcludeGrowers.Select(g => $"{g.GrowerNumber} - {g.Name}".TrimStart(' ', '-')).ToList(),
                ExcludedPayGroupDescriptions = SelectedExcludePayGroups.Select(pg => $"{pg.PayGroupId} - {pg.Description}".TrimStart(' ', '-')).ToList()
            };

            try
            {
                // Call the service method
                var testResult = await _paymentService.PerformAdvancePaymentTestRunAsync(
                    parameters.AdvanceNumber, parameters.PaymentDate, parameters.CutoffDate, parameters.CropYear,
                    parameters.ExcludeGrowerIds, parameters.ExcludePayGroupIds, parameters.ProductIds, parameters.ProcessIds, this);

                LatestTestRunResult = testResult;
                if (LatestTestRunResult != null) LatestTestRunResult.InputParameters = parameters;

                if (testResult.HasAnyErrors)
                {
                    StatusMessage = "Test run completed with calculation errors. Cannot show report viewer.";
                    Report("Test run finished with errors:");
                    foreach(var error in testResult.GeneralErrors) { Report($"ERROR: {error}"); }
                    foreach(var gp in testResult.GrowerPayments.Where(g => g.HasErrors)) {
                         foreach(var errorMsg in gp.ErrorMessages) { Report($"ERROR (Grower {gp.GrowerNumber}): {errorMsg}"); }
                    }
                    await _dialogService.ShowMessageBoxAsync("The test run encountered errors during calculation. Report viewer cannot be shown.", "Test Run Errors");
                }
                else if (!testResult.GrowerPayments.Any())
                {
                     StatusMessage = "Test run completed. No eligible growers/receipts found.";
                     Report("Test run finished: No eligible growers/receipts found.");
                     await _dialogService.ShowMessageBoxAsync("Test run completed, but no eligible growers or receipts were found. Report viewer not shown.", "Test Run Complete - No Results");
                }
                else
                {
                    StatusMessage = $"Test run simulation completed successfully. Preparing report viewer...";
                    Report("Test run simulation finished successfully. Preparing report viewer...");

                    // Create DataTable for the report
                    // Need access to the helper method, temporarily create instance of other VM
                    // TODO: Refactor DataTable creation into a shared service/helper
                    var tempReportViewModel = new PaymentTestRunReportViewModel(LatestTestRunResult);
                    System.Data.DataTable reportDataTable = tempReportViewModel.CreateDataTableFromGrowerPayments(); // Assuming this is made public/internal

                    // Prepare data sources for Bold Reports Viewer
                    // Use the correct ReportDataSource class from BoldReports.Windows
                    var reportDataSource = new ReportDataSource
                    {
                         Name = "GrowerPayments", // MUST match the DataSet name in RDL
                         Value = reportDataTable
                    };
                    var dataSources = new List<ReportDataSource> { reportDataSource };


                    // Prepare parameters for Bold Reports Viewer
                    // Use ReportParameter from BoldReports.Windows namespace
                    var reportParameters = new List<ReportParameter>();
                    reportParameters.Add(new ReportParameter() { Name = "ParamAdvanceNumber", Values = new List<string>() { parameters.AdvanceNumber.ToString() } });
                    reportParameters.Add(new ReportParameter() { Name = "ParamPaymentDate", Values = new List<string>() { parameters.PaymentDate.ToString("d") } }); // Format date
                    reportParameters.Add(new ReportParameter() { Name = "ParamCutoffDate", Values = new List<string>() { parameters.CutoffDate.ToString("d") } }); // Format date
                    reportParameters.Add(new ReportParameter() { Name = "ParamCropYear", Values = new List<string>() { parameters.CropYear.ToString() } });
                    reportParameters.Add(new ReportParameter() { Name = "ParamProductsFilter", Values = new List<string>() { parameters.ProductDescriptions.Any() ? string.Join(", ", parameters.ProductDescriptions) : "All" } });
                    reportParameters.Add(new ReportParameter() { Name = "ParamProcessesFilter", Values = new List<string>() { parameters.ProcessDescriptions.Any() ? string.Join(", ", parameters.ProcessDescriptions) : "All" } });
                    reportParameters.Add(new ReportParameter() { Name = "ParamExcludedGrowersFilter", Values = new List<string>() { parameters.ExcludedGrowerDescriptions.Any() ? string.Join(", ", parameters.ExcludedGrowerDescriptions) : "None" } });
                    reportParameters.Add(new ReportParameter() { Name = "ParamExcludedPayGroupsFilter", Values = new List<string>() { parameters.ExcludedPayGroupDescriptions.Any() ? string.Join(", ", parameters.ExcludedPayGroupDescriptions) : "None" } });


                    // --- Load Report from Stream ---
                    System.IO.Stream reportStream = null;
                    try
                    {
                        // Get the current assembly
                        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        // Standard resource name (AssemblyName.Folder.File.Extension)
                        string resourceName = "WPFGrowerApp.Reports.PaymentTestRunReport.rdlc";
                        reportStream = assembly.GetManifestResourceStream(resourceName);

                        if (reportStream == null)
                        {
                            // Attempt with just Folder.File.Extension (sometimes works if default namespace differs unexpectedly)
                            resourceName = "Reports.PaymentTestRunReport.rdlc";
                            reportStream = assembly.GetManifestResourceStream(resourceName);
                        }

                        if (reportStream == null)
                        {
                            throw new Exception($"Embedded report resource not found. Tried paths: WPFGrowerApp.Reports.PaymentTestRunReport.rdlc and Reports.PaymentTestRunReport.rdlc");
                        }
                    }
                    catch (Exception streamEx)
                    {
                         Report($"CRITICAL ERROR: Failed to load embedded report stream: {streamEx.Message}");
                         await _dialogService.ShowMessageBoxAsync($"Could not load the embedded report resource: {streamEx.Message}", "Report Load Error");
                         IsRunning = false; // Ensure IsRunning is reset
                         return; // Stop execution
                    }
                    // --- End Load Report from Stream ---


                    // Create the ViewModel for the viewer window, passing the stream, data sources, and parameters
                    var viewerViewModel = new BoldReportViewerViewModel(reportStream, dataSources, reportParameters);
                    viewerViewModel.ReportTitle = $"Payment Test Run - {DateTime.Now:g}";

                    // Show the Bold Report Viewer Window
                    var reportViewerWindow = new Views.BoldReportViewerWindow();
                    reportViewerWindow.DataContext = viewerViewModel;
                    // Set owner if desired for modality/behavior
                    // reportViewerWindow.Owner = Application.Current.MainWindow;
                    reportViewerWindow.Show(); // Show as a non-modal window

                    StatusMessage = "Report viewer displayed.";
                    Report("Report viewer displayed.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Test run for report viewer failed with a critical error.";
                Report($"CRITICAL ERROR during report viewer test run: {ex.Message}");
                Logger.Error($"Critical error during report viewer test run execution", ex);
                await _dialogService.ShowMessageBoxAsync($"A critical error occurred: {ex.Message}", "Test Run Failed");
            }
            finally
            {
                IsRunning = false;
            }
        }


        // Method to load ListBox data sources
        private async Task LoadFiltersAsync()
        {
            try
            {
                // Load Products into the backing collection
                var productList = await _productService.GetAllProductsAsync();
                 _allProducts.Clear();
                 foreach (var p in productList) _allProducts.Add(p);
                 _filteredProductsView?.Refresh(); // Refresh the view

                // Load Processes into the backing collection
                var processList = await _processService.GetAllProcessesAsync();
                 _allProcesses.Clear();
                 foreach (var p in processList) _allProcesses.Add(p);
                 _filteredProcessesView?.Refresh(); // Refresh the view

                // Load PayGroups into the backing collection
                var payGroupList = await _payGroupService.GetAllPayGroupsAsync();
                 _allPayGroups.Clear();
                 foreach (var pg in payGroupList) _allPayGroups.Add(pg);
                 _filteredPayGroupsView?.Refresh(); // Refresh the view

                // Load Growers into the backing collection
                var growerList = await _growerService.GetAllGrowersBasicInfoAsync();
                 _allGrowers.Clear();
                 foreach (var g in growerList) _allGrowers.Add(g);
                 _filteredGrowersView?.Refresh(); // Refresh the view

            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filter data for Payment Run", ex);
                await _dialogService.ShowMessageBoxAsync("Error loading filter options.", "Load Error"); // Use async
            }
        }

         // Method to update the On Hold Growers list based on current filters
        private async Task UpdateOnHoldGrowersAsync()
        {
            IsLoadingOnHoldGrowers = true;

            try
            {
                var onHold = await _growerService.GetOnHoldGrowersAsync();

                // Apply client-side filtering based on Exclude options if needed
                var excludedGrowerIds = SelectedExcludeGrowers.Select(g => g.GrowerNumber).Where(num => num != 0).ToList();
                if (excludedGrowerIds.Any())
                {
                    onHold = onHold.Where(g => !excludedGrowerIds.Contains(g.GrowerNumber)).ToList();
                }
                // TODO: Add filtering for Products, Processes, PayGroups if required by business logic

                OnHoldGrowers.Clear();
                if (onHold != null)
                {
                    foreach(var g in onHold)
                    {
                        OnHoldGrowers.Add(g);
                    }
                }
            }
            catch (Exception ex)
            {
                 Logger.Error("Failed to update On Hold Growers list", ex);
            }
            finally
            {
                IsLoadingOnHoldGrowers = false;
            }
        }

        // Handler for selection changes in filter lists
        private void SelectedFilters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Re-calculate the OnHoldGrowers list whenever a filter selection changes
           // _ = UpdateOnHoldGrowersAsync();
        }

    }
}
