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
        
        // Phase 1 - New services
        private readonly IPaymentBatchManagementService _batchManagementService;
        private readonly IChequeGenerationService _chequeGenerationService;
        private readonly IChequePrintingService _chequePrintingService;
        private readonly IStatementPrintingService _statementPrintingService;

        private int _advanceNumber = 1;
        private DateTime _paymentDate = DateTime.Today;
        private DateTime _cutoffDate = DateTime.Today;
        private int _cropYear = DateTime.Today.Year;
        private bool _isRunning;
        private string? _statusMessage;
        private ObservableCollection<string>? _runLog;
        private PaymentBatch? _lastRunBatch; // For actual run
        private List<string>? _lastRunErrors; // For actual run
        private TestRunResult? _latestTestRunResult; // For test run
        
        // Phase 1 - Enhanced properties
        private PaymentBatch? _currentBatch;
        private ObservableCollection<Cheque>? _generatedCheques;
        private bool _chequesGenerated;
        private bool _chequesPrinted;
        private bool _statementsGenerated;

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
        private ObservableCollection<GrowerInfo>? _onHoldGrowers;
        private bool _isLoadingOnHoldGrowers;


        public PaymentRunViewModel(
            IPaymentService paymentService,
            IDialogService dialogService,
            IProductService productService,
            IProcessService processService,
            IPayGroupService payGroupService,
            IGrowerService growerService,
            IPaymentBatchManagementService batchManagementService,
            IChequeGenerationService chequeGenerationService,
            IChequePrintingService chequePrintingService,
            IStatementPrintingService statementPrintingService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _batchManagementService = batchManagementService ?? throw new ArgumentNullException(nameof(batchManagementService));
            _chequeGenerationService = chequeGenerationService ?? throw new ArgumentNullException(nameof(chequeGenerationService));
            _chequePrintingService = chequePrintingService ?? throw new ArgumentNullException(nameof(chequePrintingService));
            _statementPrintingService = statementPrintingService ?? throw new ArgumentNullException(nameof(statementPrintingService));

            _growerSearchText = string.Empty;
            _productSearchText = string.Empty;
            _processSearchText = string.Empty;
            _payGroupSearchText = string.Empty;

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
            CropYears.Add(2022); // Add 2022 for test data
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
                return (product.ProductCode?.IndexOf(ProductSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
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
                return (process.ProcessCode?.IndexOf(ProcessSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
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
                return (payGroup.GroupCode?.IndexOf(PayGroupSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
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
         public ObservableCollection<GrowerInfo> OnHoldGrowers { get => _onHoldGrowers ?? new ObservableCollection<GrowerInfo>(); private set => SetProperty(ref _onHoldGrowers, value); }
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
            get => _statusMessage ?? string.Empty;
            private set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> RunLog
        {
            get => _runLog ?? new ObservableCollection<string>();
            private set => SetProperty(ref _runLog, value);
        }

        public PaymentBatch? LastRunBatch
        {
            get => _lastRunBatch;
            private set => SetProperty(ref _lastRunBatch, value);
        }

        public List<string> LastRunErrors
        {
            get => _lastRunErrors ?? new List<string>();
            private set => SetProperty(ref _lastRunErrors, value);
        }

        public TestRunResult? LatestTestRunResult
        {
            get => _latestTestRunResult;
            private set => SetProperty(ref _latestTestRunResult, value);
        }

        // Phase 1 - Enhanced properties
        public PaymentBatch? CurrentBatch
        {
            get => _currentBatch;
            private set => SetProperty(ref _currentBatch, value);
        }

        public ObservableCollection<Cheque> GeneratedCheques
        {
            get => _generatedCheques ??= new ObservableCollection<Cheque>();
            private set => SetProperty(ref _generatedCheques, value);
        }

        public bool ChequesGenerated
        {
            get => _chequesGenerated;
            private set => SetProperty(ref _chequesGenerated, value);
        }

        public bool ChequesPrinted
        {
            get => _chequesPrinted;
            private set => SetProperty(ref _chequesPrinted, value);
        }

        public bool StatementsGenerated
        {
            get => _statementsGenerated;
            private set => SetProperty(ref _statementsGenerated, value);
        }

        // Can Execute properties for commands
        public bool CanPostBatch => LatestTestRunResult != null && CurrentBatch == null && !IsRunning;
        public bool CanGenerateCheques => CurrentBatch != null && !ChequesGenerated && !IsRunning;
        public bool CanPrintCheques => ChequesGenerated && !IsRunning;
        public bool CanPrintStatements => CurrentBatch != null && !IsRunning;


        // Commands - Basic Operations
        public ICommand LoadFiltersCommand => new RelayCommand(async o => await LoadFiltersAsync());
        public ICommand StartPaymentRunCommand => new RelayCommand(async o => await StartPaymentRunAsync(), o => !IsRunning);
        public ICommand TestRunCommand => new RelayCommand(async o => await PerformTestRunAsync(), o => !IsRunning); // Shows Dialog Report
        public ICommand ViewBoldReportCommand => new RelayCommand(async o => await PerformViewBoldReportAsync(), o => !IsRunning); // Shows Bold Report Viewer
        
        // Commands - Phase 1 Enhancements (NEW)
        public ICommand PostBatchCommand => new RelayCommand(async o => await PostBatchAsync(), o => CanPostBatch);
        public ICommand GenerateChequesCommand => new RelayCommand(async o => await GenerateChequesAsync(), o => CanGenerateCheques);
        public ICommand PrintChequesCommand => new RelayCommand(async o => await PrintChequesAsync(), o => CanPrintCheques);
        public ICommand PrintStatementsCommand => new RelayCommand(async o => await PrintStatementsAsync(), o => CanPrintStatements);
        public ICommand ViewBatchDetailsCommand => new RelayCommand(async o => await ViewBatchDetailsAsync(), o => CurrentBatch != null);


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
            var selectedProductIds = SelectedProducts.Select(p => p.ProductId).ToList();
            var selectedProcessIds = SelectedProcesses.Select(p => p.ProcessId).ToList();
            var selectedExcludeGrowerIds = SelectedExcludeGrowers.Select(g => (int)g.GrowerNumber).Where(num => num != 0).ToList();
            var selectedExcludePayGroupIds = SelectedExcludePayGroups.Select(pg => pg.PayGroupId).ToList();

            // Log selected filters
            Report($"Filtering by Products: {(selectedProductIds.Any() ? string.Join(",", selectedProductIds) : "All")}");
            Report($"Filtering by Processes: {(selectedProcessIds.Any() ? string.Join(",", selectedProcessIds) : "All")}");
            Report($"Excluding Growers: {(selectedExcludeGrowerIds.Any() ? string.Join(",", selectedExcludeGrowerIds) : "None")}");
            Report($"Excluding PayGroups: {(selectedExcludePayGroupIds.Any() ? string.Join(",", selectedExcludePayGroupIds) : "None")}");

            try
            {
                 // ProcessAdvancePaymentRunAsync signature updated to use PaymentBatch and List<int>
                (bool success, List<string> errors, PaymentBatch createdBatch) = 
                    await _paymentService.ProcessAdvancePaymentRunAsync(
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
                    StatusMessage = $"Payment run completed successfully for Batch {createdBatch?.PaymentBatchId}.";
                    Report("Payment run finished successfully.");
                    await _dialogService.ShowMessageBoxAsync($"Advance {AdvanceNumber} payment run completed successfully for Batch {createdBatch?.PaymentBatchId}.", "Payment Run Complete"); // Use async
                }
                else
                {
                    StatusMessage = $"Payment run completed with errors for Batch {createdBatch?.PaymentBatchId}. Check log.";
                    Report("Payment run finished with errors:");
                    foreach(var error in LastRunErrors)
                    {
                        Report($"ERROR: {error}");
                    }
                     await _dialogService.ShowMessageBoxAsync($"The payment run encountered errors. Please review the run log.\nBatch ID: {createdBatch?.PaymentBatchId}", "Payment Run Errors"); // Use async
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
            var selectedProductIds = SelectedProducts.Select(p => p.ProductId).ToList();
            var selectedProcessIds = SelectedProcesses.Select(p => p.ProcessId).ToList();
            var selectedExcludeGrowerIds = SelectedExcludeGrowers.Select(g => (int)g.GrowerNumber).Where(num => num != 0).ToList();
            var selectedExcludePayGroupIds = SelectedExcludePayGroups.Select(pg => pg.PayGroupId).ToList();

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

                if (!testResult.GrowerPayments.Any())
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
                    if (LatestTestRunResult != null)
                    {
                        var reportViewModel = new PaymentTestRunReportViewModel(LatestTestRunResult);
                        var reportWindow = new PaymentTestRunReportWindow
                        {
                            DataContext = reportViewModel,
                        };
                        reportWindow.Show();
                    }
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
            var selectedProductIds = SelectedProducts.Select(p => p.ProductId).ToList();
            var selectedProcessIds = SelectedProcesses.Select(p => p.ProcessId).ToList();
            var selectedExcludeGrowerIds = SelectedExcludeGrowers.Select(g => (int)g.GrowerNumber).Where(num => num != 0).ToList();
            var selectedExcludePayGroupIds = SelectedExcludePayGroups.Select(pg => pg.PayGroupId).ToList();

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

            System.IO.Stream? reportStream = null;

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
                    if (LatestTestRunResult != null)
                    {
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


                        if (reportStream != null)
                        {
                            // Create the ViewModel for the viewer window, passing the stream, data sources, and parameters
                            var viewerViewModel = new BoldReportViewerViewModel(reportStream, dataSources, reportParameters);
                            viewerViewModel.ReportTitle = $"Payment Test Run - {DateTime.Now:g}";

                            // Show the Bold Report Viewer Window
                            var reportViewerWindow = new Views.BoldReportViewerWindow();
                            reportViewerWindow.DataContext = viewerViewModel;
                            reportViewerWindow.Show(); // Show as a non-modal window

                            StatusMessage = "Report viewer displayed.";
                            Report("Report viewer displayed.");
                        }
                    }
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
                var excludedGrowerIds = SelectedExcludeGrowers.Select(g => (int)g.GrowerNumber).Where(num => num != 0).ToList();
                if (excludedGrowerIds.Any())
                {
                    onHold = onHold.Where(g => !excludedGrowerIds.Contains((int)g.GrowerNumber)).ToList();
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
        private void SelectedFilters_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Re-calculate the OnHoldGrowers list whenever a filter selection changes
           // _ = UpdateOnHoldGrowersAsync();
        }

        // ==============================================================
        // PHASE 1 - NEW COMMAND IMPLEMENTATIONS
        // ==============================================================

        /// <summary>
        /// Post the payment batch (create batch, generate cheques, post to accounts)
        /// </summary>
        private async Task PostBatchAsync()
        {
            try
            {
                if (LatestTestRunResult == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please run a test first before posting.", "Test Required");
                    return;
                }

                // Confirm with user
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will:\n" +
                    $"• Create payment batch\n" +
                    $"• Generate {LatestTestRunResult.GrowerPayments.Count} cheques\n" +
                    $"• Post ${LatestTestRunResult.GrowerPayments.Sum(gp => gp.TotalCalculatedPayment):N2} to accounts\n\n" +
                    $"Continue?",
                    $"Post Advance {AdvanceNumber} Payment Batch?");

                if (confirm != true)
                    return;

                IsRunning = true;
                StatusMessage = "Posting payment batch...";
                Report("Starting batch posting...");

                try
                {
                    // 1. Create payment batch
                    Report("Creating payment batch...");
                    var paymentTypeId = AdvanceNumber; // Assuming PaymentTypeId matches advance number
                    CurrentBatch = await _batchManagementService.CreatePaymentBatchAsync(
                        paymentTypeId: paymentTypeId,
                        batchDate: PaymentDate,
                        cropYear: CropYear,
                        cutoffDate: CutoffDate,
                        notes: $"Advance {AdvanceNumber} - {LatestTestRunResult.GrowerPayments.Count} growers",
                        createdBy: App.CurrentUser?.Username);

                    Report($"✓ Created batch: {CurrentBatch.BatchNumber}");

                    // 2. Run the actual payment processing (posts to accounts, creates allocations)
                    Report("Processing payments to accounts...");
                    
                    var selectedProductIds = SelectedProducts.Select(p => p.ProductId).ToList();
                    var selectedProcessIds = SelectedProcesses.Select(p => p.ProcessId).ToList();
                    var selectedExcludeGrowerIds = SelectedExcludeGrowers.Select(g => (int)g.GrowerNumber).Where(num => num != 0).ToList();
                    var selectedExcludePayGroupIds = SelectedExcludePayGroups.Select(pg => pg.PayGroupId).ToList();

                    var (success, errors, postedBatch) = await _paymentService.ProcessAdvancePaymentRunAsync(
                        AdvanceNumber,
                        PaymentDate,
                        CutoffDate,
                        CropYear,
                        selectedExcludeGrowerIds,
                        selectedExcludePayGroupIds,
                        selectedProductIds,
                        selectedProcessIds,
                        this);

                    if (!success || errors.Any())
                    {
                        Report("⚠ Payment processing completed with errors");
                        foreach (var error in errors)
                        {
                            Report($"  ERROR: {error}");
                        }
                        
                        await _dialogService.ShowMessageBoxAsync(
                            $"Payment processing completed with {errors.Count} error(s). Check the run log for details.",
                            "Posting Completed with Errors");
                    }
                    else
                    {
                        Report("✓ Successfully posted payments to accounts");
                        StatusMessage = $"Batch {CurrentBatch.BatchNumber} posted successfully";
                        
                        await _dialogService.ShowMessageBoxAsync(
                            $"Payment batch posted successfully!\n\n" +
                            $"Batch: {CurrentBatch.BatchNumber}\n" +
                            $"Growers: {LatestTestRunResult.GrowerPayments.Count}\n" +
                            $"Total Amount: ${LatestTestRunResult.GrowerPayments.Sum(gp => gp.TotalCalculatedPayment):N2}",
                            "Batch Posted");
                    }
                }
                catch (Exception ex)
                {
                    Report($"✗ ERROR: {ex.Message}");
                    Logger.Error("Error posting batch", ex);
                    await _dialogService.ShowMessageBoxAsync($"Error posting batch: {ex.Message}", "Error");
                    throw;
                }
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Generate cheques for the current batch
        /// </summary>
        private async Task GenerateChequesAsync()
        {
            try
            {
                if (CurrentBatch == null || LatestTestRunResult == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch available. Please post a batch first.", "No Batch");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Generating cheques...";
                Report("Generating cheques for batch...");

                // Convert test result to GrowerPaymentAmount list
                var growerPayments = LatestTestRunResult.GrowerPayments
                    .Select(gp => new GrowerPaymentAmount
                    {
                        GrowerId = int.TryParse(gp.GrowerNumber, out var id) ? id : 0,
                        GrowerName = gp.GrowerName,
                        PaymentAmount = gp.TotalCalculatedPayment,
                        IsOnHold = gp.IsOnHold
                    }).ToList();

                // Generate cheques
                var cheques = await _chequeGenerationService.GenerateChequesForBatchAsync(
                    CurrentBatch.PaymentBatchId,
                    growerPayments);

                GeneratedCheques.Clear();
                foreach (var cheque in cheques)
                {
                    GeneratedCheques.Add(cheque);
                }

                ChequesGenerated = true;
                Report($"✓ Generated {cheques.Count} cheques");
                StatusMessage = $"Generated {cheques.Count} cheques";

                await _dialogService.ShowMessageBoxAsync(
                    $"Successfully generated {cheques.Count} cheques!\n\n" +
                    $"Cheque range: {cheques.Min(c => c.ChequeNumber)} - {cheques.Max(c => c.ChequeNumber)}",
                    "Cheques Generated");
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error generating cheques", ex);
                await _dialogService.ShowMessageBoxAsync($"Error generating cheques: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Print generated cheques
        /// </summary>
        private async Task PrintChequesAsync()
        {
            try
            {
                if (!GeneratedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("No cheques to print. Please generate cheques first.", "No Cheques");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Printing cheques...";
                Report($"Printing {GeneratedCheques.Count} cheques...");

                // Print all cheques
                var printedCount = await _chequePrintingService.PrintChequesAsync(GeneratedCheques.ToList());

                if (printedCount > 0)
                {
                    ChequesPrinted = true;
                    Report($"✓ Printed {printedCount} cheques");
                    StatusMessage = $"Printed {printedCount} cheques successfully";

                    await _dialogService.ShowMessageBoxAsync(
                        $"Successfully printed {printedCount} cheques!",
                        "Printing Complete");
                }
                else
                {
                    Report("Printing cancelled by user");
                }
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error printing cheques", ex);
                await _dialogService.ShowMessageBoxAsync($"Error printing cheques: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// Print advance statements for all growers in batch
        /// </summary>
        private async Task PrintStatementsAsync()
        {
            try
            {
                if (CurrentBatch == null || LatestTestRunResult == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch available to print statements for.", "No Batch");
                    return;
                }

                IsRunning = true;
                StatusMessage = "Printing statements...";
                Report($"Printing statements for {LatestTestRunResult.GrowerPayments.Count} growers...");

                // Print statements
                var printedCount = await _statementPrintingService.PrintBatchStatementsAsync(
                    LatestTestRunResult.GrowerPayments,
                    AdvanceNumber,
                    CurrentBatch.PaymentBatchId);

                if (printedCount > 0)
                {
                    StatementsGenerated = true;
                    Report($"✓ Printed {printedCount} statements");
                    StatusMessage = $"Printed {printedCount} statements successfully";

                    await _dialogService.ShowMessageBoxAsync(
                        $"Successfully printed {printedCount} advance statements!",
                        "Printing Complete");
                }
                else
                {
                    Report("Statement printing cancelled by user");
                }
            }
            catch (Exception ex)
            {
                Report($"✗ ERROR: {ex.Message}");
                Logger.Error("Error printing statements", ex);
                await _dialogService.ShowMessageBoxAsync($"Error printing statements: {ex.Message}", "Error");
            }
            finally
            {
                IsRunning = false;
            }
        }

        /// <summary>
        /// View details of the current payment batch
        /// </summary>
        private async Task ViewBatchDetailsAsync()
        {
            try
            {
                if (CurrentBatch == null)
                {
                    await _dialogService.ShowMessageBoxAsync("No batch selected.", "No Batch");
                    return;
                }

                // Get batch summary
                var summary = await _batchManagementService.GetBatchSummaryAsync(CurrentBatch.PaymentBatchId);

                // Display summary
                var message = $"Batch Details:\n\n" +
                             $"Batch Number: {summary.BatchNumber}\n" +
                             $"Payment Type: {summary.PaymentTypeName}\n" +
                             $"Batch Date: {summary.BatchDate:yyyy-MM-dd}\n" +
                             $"Status: {summary.Status}\n\n" +
                             $"Growers: {summary.TotalGrowers}\n" +
                             $"Receipts: {summary.TotalReceipts}\n" +
                             $"Amount: ${summary.TotalAmount:N2}\n" +
                             $"Cheques Generated: {summary.ChequesGenerated}\n\n" +
                             $"Created: {summary.CreatedAt:g} by {summary.CreatedBy}";

                if (summary.PostedAt.HasValue)
                {
                    message += $"\nPosted: {summary.PostedAt:g} by {summary.PostedBy}";
                }

                await _dialogService.ShowMessageBoxAsync(message, "Batch Details");
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing batch details", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading batch details: {ex.Message}", "Error");
            }
        }

    }
}
