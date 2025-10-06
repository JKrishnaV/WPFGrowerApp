using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    public class ReceiptEntryViewModel : ViewModelBase
    {
        private readonly IReceiptService _receiptService;
        private readonly IGrowerService _growerService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IDepotService _depotService;
        private readonly IDialogService _dialogService;

        private Receipt _currentReceipt = null!;
        private bool _isEditMode;
        private bool _isLoading;
        private bool _isSaving;
        private string _statusMessage = string.Empty;

        // Lookup collections
        private ObservableCollection<GrowerSearchResult> _growers = new();
        private ObservableCollection<Product> _products = new();
        private ObservableCollection<Process> _processes = new();
        private ObservableCollection<Depot> _depots = new();

        // Selected items
        private GrowerSearchResult? _selectedGrower;
        private Product? _selectedProduct;
        private Process? _selectedProcess;
        private Depot? _selectedDepot;

        // Dialog result
        private bool? _dialogResult;

        public ReceiptEntryViewModel(
            IReceiptService receiptService,
            IGrowerService growerService,
            IProductService productService,
            IProcessService processService,
            IDepotService depotService,
            IDialogService dialogService,
            Receipt? existingReceipt = null)
        {
            Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel - Constructor starting. IsEditMode: {existingReceipt != null}");
            
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Initialize commands
            SaveCommand = new RelayCommand(async p => await SaveAsync(), p => CanSave());
            CancelCommand = new RelayCommand(Cancel);
            CalculateWeightsCommand = new RelayCommand(p => CalculateWeights());

            // Set edit mode and current receipt
            _isEditMode = existingReceipt != null;
            if (_isEditMode && existingReceipt != null)
            {
                Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel - Edit mode: Receipt #{existingReceipt.ReceiptNumberModern}");
                Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel - Receipt Date: {existingReceipt.ReceiptDate}, Time: {existingReceipt.ReceiptTime}");
                if (existingReceipt.ReceiptTime == default(TimeSpan))
                {
                    Infrastructure.Logging.Logger.Warn($"ReceiptEntryViewModel - ReceiptTime is default (zero) for Receipt #{existingReceipt.ReceiptNumberModern}");
                }
                else
                {
                    Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel - ReceiptTime loaded: {existingReceipt.ReceiptTime}");
                }
                CurrentReceipt = existingReceipt;
                AttachReceiptEventHandlers();
                OnPropertyChanged(nameof(CurrentReceipt));
                OnPropertyChanged(nameof(ReceiptDateTime));
            }
            else
            {
                Infrastructure.Logging.Logger.Info("ReceiptEntryViewModel - Add mode: Creating new receipt");
                // Create new receipt with defaults
                CurrentReceipt = new Receipt
                {
                    ReceiptDate = DateTime.Now.Date,
                    ReceiptTime = DateTime.Now.TimeOfDay,
                    GradeModern = 1,
                    DockPercentage = 0
                };
                AttachReceiptEventHandlers();
                OnPropertyChanged(nameof(ReceiptDateTime));
            }
            
            Infrastructure.Logging.Logger.Info("ReceiptEntryViewModel - Constructor completed");
        }

        public async Task InitializeAsync()
        {
            Infrastructure.Logging.Logger.Info("ReceiptEntryViewModel.InitializeAsync - Starting initialization");
            IsLoading = true;
            StatusMessage = "Loading...";

            try
            {
                // Load all lookup data
                Infrastructure.Logging.Logger.Info("ReceiptEntryViewModel.InitializeAsync - Loading lookup data (Growers, Products, Processes, Depots)");
                var growersTask = _growerService.GetAllGrowersAsync();
                var productsTask = _productService.GetAllProductsAsync();
                var processesTask = _processService.GetAllProcessesAsync();
                var depotsTask = _depotService.GetAllDepotsAsync();

                await Task.WhenAll(growersTask, productsTask, processesTask, depotsTask);

                // Populate collections
                Growers.Clear();
                foreach (var grower in (await growersTask).OrderBy(g => g.GrowerName))
                {
                    Growers.Add(grower);
                }
                Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Loaded {Growers.Count} growers");

                Products.Clear();
                foreach (var product in (await productsTask).OrderBy(p => p.Description))
                {
                    Products.Add(product);
                }
                Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Loaded {Products.Count} products");

                Processes.Clear();
                foreach (var process in (await processesTask).OrderBy(p => p.Description))
                {
                    Processes.Add(process);
                }
                Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Loaded {Processes.Count} processes");

                Depots.Clear();
                foreach (var depot in (await depotsTask).OrderBy(d => d.DepotName))
                {
                    Depots.Add(depot);
                }
                Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Loaded {Depots.Count} depots");

                // If edit mode, select the current items
                if (IsEditMode)
                {
                    Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Edit mode: Selecting current items for Receipt #{CurrentReceipt.ReceiptNumberModern}");
                    Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Receipt.GrowerId={CurrentReceipt.GrowerId}, Receipt.ReceiptTime={CurrentReceipt.ReceiptTime}, Receipt.GradeModern={CurrentReceipt.GradeModern}");
                    Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Growers.Count={Growers.Count}, First grower: {Growers.FirstOrDefault()?.GrowerNumber} - {Growers.FirstOrDefault()?.GrowerName}");
                    
                    // Match by GrowerId (not GrowerNumber which is a different field)
                    SelectedGrower = Growers.FirstOrDefault(g => g.GrowerId == CurrentReceipt.GrowerId);
                    
                    // Compare IDs as strings - Receipt stores ints, so convert for comparison
                    var productIdStr = CurrentReceipt.ProductId.ToString();
                    var processIdStr = CurrentReceipt.ProcessId.ToString();
                    var depotIdStr = CurrentReceipt.DepotId.ToString();
                    
                    SelectedProduct = Products.FirstOrDefault(p => p.ProductId == productIdStr);
                    SelectedProcess = Processes.FirstOrDefault(p => p.ProcessId == processIdStr);
                    SelectedDepot = Depots.FirstOrDefault(d => d.DepotId == depotIdStr);
                    
                    Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Edit mode selection - GrowerId={CurrentReceipt.GrowerId}, ProductId={productIdStr}, ProcessId={processIdStr}, DepotId={depotIdStr}");
                    Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel.InitializeAsync - Selected: Grower={SelectedGrower?.GrowerName} (GrowerNumber={SelectedGrower?.GrowerNumber}, GrowerId={SelectedGrower?.GrowerId}), Product={SelectedProduct?.Description}, Process={SelectedProcess?.Description}, Depot={SelectedDepot?.DepotName}");
                    
                    // Force UI updates
                    OnPropertyChanged(nameof(SelectedGrower));
                    OnPropertyChanged(nameof(SelectedProduct));
                    OnPropertyChanged(nameof(SelectedProcess));
                    OnPropertyChanged(nameof(SelectedDepot));
                    OnPropertyChanged(nameof(CurrentReceipt));
                }

                StatusMessage = "Ready";
                Infrastructure.Logging.Logger.Info("ReceiptEntryViewModel.InitializeAsync - Initialization completed successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error loading data: {ex.Message}", "Load Error");
                Infrastructure.Logging.Logger.Error("ReceiptEntryViewModel.InitializeAsync - Error loading data", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Combined Date + Time property for TimePicker (MaterialDesign TimePicker expects DateTime?)
        public DateTime? ReceiptDateTime
        {
            get
            {
                if (CurrentReceipt == null) return null;
                return CurrentReceipt.ReceiptDate.Date + CurrentReceipt.ReceiptTime;
            }
            set
            {
                if (CurrentReceipt == null || !value.HasValue) return;
                var dt = value.Value;
                bool changed = false;
                if (CurrentReceipt.ReceiptDate != dt.Date)
                {
                    CurrentReceipt.ReceiptDate = dt.Date;
                    changed = true;
                }
                if (CurrentReceipt.ReceiptTime != dt.TimeOfDay)
                {
                    CurrentReceipt.ReceiptTime = dt.TimeOfDay;
                    changed = true;
                }
                if (changed)
                {
                    Infrastructure.Logging.Logger.Info($"ReceiptEntryViewModel - ReceiptDateTime updated -> {dt:yyyy-MM-dd HH:mm:ss}");
                    OnPropertyChanged(nameof(ReceiptDateTime));
                    OnPropertyChanged(nameof(CurrentReceipt));
                }
            }
        }

        private void AttachReceiptEventHandlers()
        {
            if (CurrentReceipt != null)
            {
                CurrentReceipt.PropertyChanged -= CurrentReceiptOnPropertyChanged;
                CurrentReceipt.PropertyChanged += CurrentReceiptOnPropertyChanged;
            }
        }

        private void CurrentReceiptOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Receipt.ReceiptDate) || e.PropertyName == nameof(Receipt.ReceiptTime))
            {
                OnPropertyChanged(nameof(ReceiptDateTime));
            }
        }

        #region Properties

        public Receipt CurrentReceipt
        {
            get => _currentReceipt;
            set => SetProperty(ref _currentReceipt, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string WindowTitle => IsEditMode ? $"Edit Receipt - {CurrentReceipt?.ReceiptNumberModern}" : "Add New Receipt";

        // Lookup collections
        public ObservableCollection<GrowerSearchResult> Growers
        {
            get => _growers;
            set => SetProperty(ref _growers, value);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Process> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        public ObservableCollection<Depot> Depots
        {
            get => _depots;
            set => SetProperty(ref _depots, value);
        }

        // Selected items
        public GrowerSearchResult SelectedGrower
        {
            get => _selectedGrower;
            set
            {
                if (SetProperty(ref _selectedGrower, value))
                {
                    if (value != null)
                    {
                        CurrentReceipt.GrowerId = value.GrowerId;  // Use GrowerId, not GrowerNumber
                    }
                    OnPropertyChanged(nameof(SelectedGrower));
                }
            }
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (SetProperty(ref _selectedProduct, value) && value != null)
                {
                    CurrentReceipt.ProductId = int.Parse(value.ProductId); // Parse string back to int
                }
            }
        }

        public Process SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (SetProperty(ref _selectedProcess, value) && value != null)
                {
                    CurrentReceipt.ProcessId = int.Parse(value.ProcessId); // Parse string back to int
                }
            }
        }

        public Depot SelectedDepot
        {
            get => _selectedDepot;
            set
            {
                if (SetProperty(ref _selectedDepot, value) && value != null)
                {
                    CurrentReceipt.DepotId = int.Parse(value.DepotId); // Parse string back to int
                }
            }
        }

        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CalculateWeightsCommand { get; }

        #endregion

        #region Methods

        private bool CanSave()
        {
            return !IsSaving &&
                   SelectedGrower != null &&
                   SelectedProduct != null &&
                   SelectedProcess != null &&
                   SelectedDepot != null &&
                   CurrentReceipt.GrossWeight > 0;
        }

        private async Task SaveAsync()
        {
            if (!CanSave()) return;

            IsSaving = true;
            StatusMessage = "Saving receipt...";

            try
            {
                // Calculate weights before saving
                CalculateWeights();

                // Validate receipt
                var isValid = await _receiptService.ValidateReceiptAsync(CurrentReceipt);
                if (!isValid)
                {
                    StatusMessage = "Receipt validation failed";
                    await _dialogService.ShowMessageBoxAsync("Please check all required fields and try again.", "Validation Error");
                    return;
                }

                // Save receipt
                var savedReceipt = await _receiptService.SaveReceiptAsync(CurrentReceipt);

                if (savedReceipt != null)
                {
                    StatusMessage = "Receipt saved successfully";
                    DialogResult = true;
                }
                else
                {
                    StatusMessage = "Failed to save receipt";
                    await _dialogService.ShowMessageBoxAsync("Failed to save receipt.", "Save Error");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving receipt: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error saving receipt: {ex.Message}", "Save Error");
                Infrastructure.Logging.Logger.Error("Error saving receipt in ReceiptEntryViewModel", ex);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void Cancel(object parameter)
        {
            DialogResult = false;
        }

        private void CalculateWeights()
        {
            // Calculate Net Weight = Gross - Tare
            CurrentReceipt.NetWeight = CurrentReceipt.GrossWeight - CurrentReceipt.TareWeight;

            // Calculate Dock Weight = Net * (DockPercentage / 100)
            CurrentReceipt.DockWeight = CurrentReceipt.NetWeight * (CurrentReceipt.DockPercentage / 100);

            // Calculate Final Weight = Net - Dock
            CurrentReceipt.FinalWeight = CurrentReceipt.NetWeight - CurrentReceipt.DockWeight;

            // Update legacy properties for compatibility
            CurrentReceipt.Gross = CurrentReceipt.GrossWeight;
            CurrentReceipt.Tare = CurrentReceipt.TareWeight;
            CurrentReceipt.Net = CurrentReceipt.FinalWeight; // Legacy Net = modern FinalWeight
            CurrentReceipt.DockPercent = CurrentReceipt.DockPercentage;

            OnPropertyChanged(nameof(CurrentReceipt));
        }

        #endregion
    }
}
