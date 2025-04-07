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
        private PostBatch _lastRunBatch;
        private List<string> _lastRunErrors;

        // Collections for ListBox ItemsSources
        private ObservableCollection<Product> _products;
        private ObservableCollection<Process> _processes;
        private ObservableCollection<PayGroup> _payGroups;
        private ObservableCollection<GrowerInfo> _allGrowers;
        private ICollectionView _filteredGrowersView; // View for filtering

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
            Products = new ObservableCollection<Product>();
            Processes = new ObservableCollection<Process>();
            PayGroups = new ObservableCollection<PayGroup>();
            AllGrowers = new ObservableCollection<GrowerInfo>(); // Initialize the backing collection
            OnHoldGrowers = new ObservableCollection<GrowerInfo>();

            // Setup filtered view for growers
            _filteredGrowersView = CollectionViewSource.GetDefaultView(AllGrowers);
            _filteredGrowersView.Filter = FilterGrowers;

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

         // Source Collections for ListBoxes
         public ObservableCollection<Product> Products { get => _products; set => SetProperty(ref _products, value); }
         public ObservableCollection<Process> Processes { get => _processes; set => SetProperty(ref _processes, value); }
         public ObservableCollection<PayGroup> PayGroups { get => _payGroups; set => SetProperty(ref _payGroups, value); }
         // Expose the filtered view for binding
         public ICollectionView FilteredGrowers => _filteredGrowersView;
         // Keep the original collection for loading data
         public ObservableCollection<GrowerInfo> AllGrowers { get => _allGrowers; set => SetProperty(ref _allGrowers, value); }


         // Properties for On Hold List
         public ObservableCollection<GrowerInfo> OnHoldGrowers { get => _onHoldGrowers; private set => SetProperty(ref _onHoldGrowers, value); }
         public bool IsLoadingOnHoldGrowers { get => _isLoadingOnHoldGrowers; private set => SetProperty(ref _isLoadingOnHoldGrowers, value); }


        public bool IsRunning
        {
            get => _isRunning;
            private set { SetProperty(ref _isRunning, value); ((RelayCommand)StartPaymentRunCommand).RaiseCanExecuteChanged(); }
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


        // Commands
        public ICommand LoadFiltersCommand => new RelayCommand(async o => await LoadFiltersAsync());
        public ICommand StartPaymentRunCommand => new RelayCommand(async o => await StartPaymentRunAsync(), o => !IsRunning);

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

        // Method to load ListBox data sources
        private async Task LoadFiltersAsync()
        {
            try
            {
                // Load Products
                var productList = await _productService.GetAllProductsAsync();
                 Products.Clear();
                 foreach (var p in productList) Products.Add(p);

                // Load Processes
                var processList = await _processService.GetAllProcessesAsync();
                 Processes.Clear();
                 foreach (var p in processList) Processes.Add(p);

                // Load PayGroups
                var payGroupList = await _payGroupService.GetPayGroupsAsync();
                 PayGroups.Clear();
                 foreach (var pg in payGroupList) PayGroups.Add(pg);

                // Load Growers for Exclude ListBox
                var growerList = await _growerService.GetAllGrowersBasicInfoAsync();
                 AllGrowers.Clear(); // Clear the backing collection
                 foreach (var g in growerList) AllGrowers.Add(g);
                 _filteredGrowersView?.Refresh(); // Refresh the view after loading

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
