using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    public partial class PriceEntryViewModel : ViewModelBase
    {
        private readonly IPriceService _priceService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IDialogService _dialogService;
    private readonly Price? _originalPrice;
        private readonly bool _isEditMode;

        [ObservableProperty]
        private string _windowTitle;

        // Product and Process
        [ObservableProperty]
        private ObservableCollection<Product> _products;

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private ObservableCollection<Process> _processes;

    [ObservableProperty]
    private Process? _selectedProcess;

        [ObservableProperty]
        private DateTime _effectiveDate;

        // Time Premium
        [ObservableProperty]
        private bool _timePremiumEnabled;

        [ObservableProperty]
        private string _premiumTime;

        [ObservableProperty]
        private decimal _canadianPremium;

        // Price Level Tab Selection
        [ObservableProperty]
        private int _selectedTabIndex;

        // Level 1 Canadian Prices
        [ObservableProperty]
        private decimal _cL1G1A1;
        [ObservableProperty]
        private decimal _cL1G1A2;
        [ObservableProperty]
        private decimal _cL1G1A3;
        [ObservableProperty]
        private decimal _cL1G1FN;

        [ObservableProperty]
        private decimal _cL1G2A1;
        [ObservableProperty]
        private decimal _cL1G2A2;
        [ObservableProperty]
        private decimal _cL1G2A3;
        [ObservableProperty]
        private decimal _cL1G2FN;

        [ObservableProperty]
        private decimal _cL1G3A1;
        [ObservableProperty]
        private decimal _cL1G3A2;
        [ObservableProperty]
        private decimal _cL1G3A3;
        [ObservableProperty]
        private decimal _cL1G3FN;

        // Level 2 Canadian Prices
        [ObservableProperty]
        private decimal _cL2G1A1;
        [ObservableProperty]
        private decimal _cL2G1A2;
        [ObservableProperty]
        private decimal _cL2G1A3;
        [ObservableProperty]
        private decimal _cL2G1FN;

        [ObservableProperty]
        private decimal _cL2G2A1;
        [ObservableProperty]
        private decimal _cL2G2A2;
        [ObservableProperty]
        private decimal _cL2G2A3;
        [ObservableProperty]
        private decimal _cL2G2FN;

        [ObservableProperty]
        private decimal _cL2G3A1;
        [ObservableProperty]
        private decimal _cL2G3A2;
        [ObservableProperty]
        private decimal _cL2G3A3;
        [ObservableProperty]
        private decimal _cL2G3FN;

        // Level 3 Canadian Prices
        [ObservableProperty]
        private decimal _cL3G1A1;
        [ObservableProperty]
        private decimal _cL3G1A2;
        [ObservableProperty]
        private decimal _cL3G1A3;
        [ObservableProperty]
        private decimal _cL3G1FN;

        [ObservableProperty]
        private decimal _cL3G2A1;
        [ObservableProperty]
        private decimal _cL3G2A2;
        [ObservableProperty]
        private decimal _cL3G2A3;
        [ObservableProperty]
        private decimal _cL3G2FN;

        [ObservableProperty]
        private decimal _cL3G3A1;
        [ObservableProperty]
        private decimal _cL3G3A2;
        [ObservableProperty]
        private decimal _cL3G3A3;
        [ObservableProperty]
        private decimal _cL3G3FN;

        [ObservableProperty]
        private bool _isSaving;

        [ObservableProperty]
        private bool _isReadOnly;

        // Validation error flags for red border styling (not ObservableProperty to allow ref)
        
        // Level 1 Grade 1 errors
        private bool _hasL1G1A1Error, _hasL1G1A2Error, _hasL1G1A3Error, _hasL1G1FNError;
        public bool HasL1G1A1Error { get => _hasL1G1A1Error; set => SetProperty(ref _hasL1G1A1Error, value); }
        public bool HasL1G1A2Error { get => _hasL1G1A2Error; set => SetProperty(ref _hasL1G1A2Error, value); }
        public bool HasL1G1A3Error { get => _hasL1G1A3Error; set => SetProperty(ref _hasL1G1A3Error, value); }
        public bool HasL1G1FNError { get => _hasL1G1FNError; set => SetProperty(ref _hasL1G1FNError, value); }

        // Level 1 Grade 2 errors
        private bool _hasL1G2A1Error, _hasL1G2A2Error, _hasL1G2A3Error, _hasL1G2FNError;
        public bool HasL1G2A1Error { get => _hasL1G2A1Error; set => SetProperty(ref _hasL1G2A1Error, value); }
        public bool HasL1G2A2Error { get => _hasL1G2A2Error; set => SetProperty(ref _hasL1G2A2Error, value); }
        public bool HasL1G2A3Error { get => _hasL1G2A3Error; set => SetProperty(ref _hasL1G2A3Error, value); }
        public bool HasL1G2FNError { get => _hasL1G2FNError; set => SetProperty(ref _hasL1G2FNError, value); }

        // Level 1 Grade 3 errors
        private bool _hasL1G3A1Error, _hasL1G3A2Error, _hasL1G3A3Error, _hasL1G3FNError;
        public bool HasL1G3A1Error { get => _hasL1G3A1Error; set => SetProperty(ref _hasL1G3A1Error, value); }
        public bool HasL1G3A2Error { get => _hasL1G3A2Error; set => SetProperty(ref _hasL1G3A2Error, value); }
        public bool HasL1G3A3Error { get => _hasL1G3A3Error; set => SetProperty(ref _hasL1G3A3Error, value); }
        public bool HasL1G3FNError { get => _hasL1G3FNError; set => SetProperty(ref _hasL1G3FNError, value); }

        // Level 2 Grade 1 errors
        private bool _hasL2G1A1Error, _hasL2G1A2Error, _hasL2G1A3Error, _hasL2G1FNError;
        public bool HasL2G1A1Error { get => _hasL2G1A1Error; set => SetProperty(ref _hasL2G1A1Error, value); }
        public bool HasL2G1A2Error { get => _hasL2G1A2Error; set => SetProperty(ref _hasL2G1A2Error, value); }
        public bool HasL2G1A3Error { get => _hasL2G1A3Error; set => SetProperty(ref _hasL2G1A3Error, value); }
        public bool HasL2G1FNError { get => _hasL2G1FNError; set => SetProperty(ref _hasL2G1FNError, value); }

        // Level 2 Grade 2 errors
        private bool _hasL2G2A1Error, _hasL2G2A2Error, _hasL2G2A3Error, _hasL2G2FNError;
        public bool HasL2G2A1Error { get => _hasL2G2A1Error; set => SetProperty(ref _hasL2G2A1Error, value); }
        public bool HasL2G2A2Error { get => _hasL2G2A2Error; set => SetProperty(ref _hasL2G2A2Error, value); }
        public bool HasL2G2A3Error { get => _hasL2G2A3Error; set => SetProperty(ref _hasL2G2A3Error, value); }
        public bool HasL2G2FNError { get => _hasL2G2FNError; set => SetProperty(ref _hasL2G2FNError, value); }

        // Level 2 Grade 3 errors
        private bool _hasL2G3A1Error, _hasL2G3A2Error, _hasL2G3A3Error, _hasL2G3FNError;
        public bool HasL2G3A1Error { get => _hasL2G3A1Error; set => SetProperty(ref _hasL2G3A1Error, value); }
        public bool HasL2G3A2Error { get => _hasL2G3A2Error; set => SetProperty(ref _hasL2G3A2Error, value); }
        public bool HasL2G3A3Error { get => _hasL2G3A3Error; set => SetProperty(ref _hasL2G3A3Error, value); }
        public bool HasL2G3FNError { get => _hasL2G3FNError; set => SetProperty(ref _hasL2G3FNError, value); }

        // Level 3 Grade 1 errors
        private bool _hasL3G1A1Error, _hasL3G1A2Error, _hasL3G1A3Error, _hasL3G1FNError;
        public bool HasL3G1A1Error { get => _hasL3G1A1Error; set => SetProperty(ref _hasL3G1A1Error, value); }
        public bool HasL3G1A2Error { get => _hasL3G1A2Error; set => SetProperty(ref _hasL3G1A2Error, value); }
        public bool HasL3G1A3Error { get => _hasL3G1A3Error; set => SetProperty(ref _hasL3G1A3Error, value); }
        public bool HasL3G1FNError { get => _hasL3G1FNError; set => SetProperty(ref _hasL3G1FNError, value); }

        // Level 3 Grade 2 errors
        private bool _hasL3G2A1Error, _hasL3G2A2Error, _hasL3G2A3Error, _hasL3G2FNError;
        public bool HasL3G2A1Error { get => _hasL3G2A1Error; set => SetProperty(ref _hasL3G2A1Error, value); }
        public bool HasL3G2A2Error { get => _hasL3G2A2Error; set => SetProperty(ref _hasL3G2A2Error, value); }
        public bool HasL3G2A3Error { get => _hasL3G2A3Error; set => SetProperty(ref _hasL3G2A3Error, value); }
        public bool HasL3G2FNError { get => _hasL3G2FNError; set => SetProperty(ref _hasL3G2FNError, value); }

        // Level 3 Grade 3 errors
        private bool _hasL3G3A1Error, _hasL3G3A2Error, _hasL3G3A3Error, _hasL3G3FNError;
        public bool HasL3G3A1Error { get => _hasL3G3A1Error; set => SetProperty(ref _hasL3G3A1Error, value); }
        public bool HasL3G3A2Error { get => _hasL3G3A2Error; set => SetProperty(ref _hasL3G3A2Error, value); }
        public bool HasL3G3A3Error { get => _hasL3G3A3Error; set => SetProperty(ref _hasL3G3A3Error, value); }
        public bool HasL3G3FNError { get => _hasL3G3FNError; set => SetProperty(ref _hasL3G3FNError, value); }

        public bool DialogResult { get; private set; }

        // Public methods for real-time validation from XAML LostFocus events
        [RelayCommand]
        public void ValidateL1G1() => ValidateLevelGradeRealtime(1, 1);
        
        [RelayCommand]
        public void ValidateL1G2() => ValidateLevelGradeRealtime(1, 2);
        
        [RelayCommand]
        public void ValidateL1G3() => ValidateLevelGradeRealtime(1, 3);
        
        [RelayCommand]
        public void ValidateL2G1() => ValidateLevelGradeRealtime(2, 1);
        
        [RelayCommand]
        public void ValidateL2G2() => ValidateLevelGradeRealtime(2, 2);
        
        [RelayCommand]
        public void ValidateL2G3() => ValidateLevelGradeRealtime(2, 3);
        
        [RelayCommand]
        public void ValidateL3G1() => ValidateLevelGradeRealtime(3, 1);
        
        [RelayCommand]
        public void ValidateL3G2() => ValidateLevelGradeRealtime(3, 2);
        
        [RelayCommand]
        public void ValidateL3G3() => ValidateLevelGradeRealtime(3, 3);

        public PriceEntryViewModel(
            IPriceService priceService,
            IProductService productService,
            IProcessService processService,
            IDialogService dialogService,
            Price? priceToEdit = null,
            bool isReadOnly = false)
        {
            _priceService = priceService;
            _productService = productService;
            _processService = processService;
            _dialogService = dialogService;
            _originalPrice = priceToEdit;
            _isEditMode = priceToEdit != null;
            _isReadOnly = isReadOnly;

            WindowTitle = isReadOnly 
                ? $"View Price - ID# {priceToEdit?.PriceID} (Read-Only)" 
                : (_isEditMode && priceToEdit != null ? $"Edit Price - ID# {priceToEdit.PriceID}" : "Add New Price");
            EffectiveDate = DateTime.Today;
            PremiumTime = "10:10";
            SelectedTabIndex = 0;

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Load products and processes
                var products = await _productService.GetAllProductsAsync();
                var processes = await _processService.GetAllProcessesAsync();

                Infrastructure.Logging.Logger.Info($"Loaded {products.Count()} products and {processes.Count()} processes");
                
                Products = new ObservableCollection<Product>(products);
                Processes = new ObservableCollection<Process>(processes);
                
                Infrastructure.Logging.Logger.Info($"ObservableCollections created - Products: {Products.Count}, Processes: {Processes.Count}");

                // If editing, populate fields
                if (_isEditMode && _originalPrice != null)
                {
                    LoadPriceData(_originalPrice);
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error initializing price entry", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading data: {ex.Message}", "Error");
            }
        }

        private void LoadPriceData(Price price)
        {
            Infrastructure.Logging.Logger.Info($"Loading price data - Product: '{price.Product}', Process: '{price.Process}'");
            
            // Set product and process
            if (int.TryParse(price.Product, out int prodId))
            {
                SelectedProduct = Products?.FirstOrDefault(p => p.ProductId == prodId) ?? Products?.FirstOrDefault()!;
                Infrastructure.Logging.Logger.Info($"Selected product by ID {prodId}: {SelectedProduct?.ProductId} - {SelectedProduct?.Description}");
            }
            else
            {
                SelectedProduct = Products?.FirstOrDefault()!;
                Infrastructure.Logging.Logger.Info($"Selected first product: {SelectedProduct?.ProductId} - {SelectedProduct?.Description}");
            }
            
            // Match by process code instead of ID since Price.Process contains the code (e.g., "Bluecrop")
            if (!string.IsNullOrEmpty(price.Process))
            {
                SelectedProcess = Processes?.FirstOrDefault(p => p.ProcessCode == price.Process) ?? Processes?.FirstOrDefault()!;
                Infrastructure.Logging.Logger.Info($"Selected process by code '{price.Process}': {SelectedProcess?.ProcessId} - {SelectedProcess?.ProcessCode} - {SelectedProcess?.Description}");
            }
            else
            {
                SelectedProcess = Processes?.FirstOrDefault()!;
                Infrastructure.Logging.Logger.Info($"Selected first process: {SelectedProcess?.ProcessId} - {SelectedProcess?.ProcessCode} - {SelectedProcess?.Description}");
            }
            EffectiveDate = price.From;

            // Time premium
            TimePremiumEnabled = price.TimePrem;
            PremiumTime = price.Time ?? "10:10";
            CanadianPremium = price.CPremium;

            // Level 1 prices
            CL1G1A1 = price.CL1G1A1;
            CL1G1A2 = price.CL1G1A2;
            CL1G1A3 = price.CL1G1A3;
            CL1G1FN = price.CL1G1FN;
            CL1G2A1 = price.CL1G2A1;
            CL1G2A2 = price.CL1G2A2;
            CL1G2A3 = price.CL1G2A3;
            CL1G2FN = price.CL1G2FN;
            CL1G3A1 = price.CL1G3A1;
            CL1G3A2 = price.CL1G3A2;
            CL1G3A3 = price.CL1G3A3;
            CL1G3FN = price.CL1G3FN;

            // Level 2 prices
            CL2G1A1 = price.CL2G1A1;
            CL2G1A2 = price.CL2G1A2;
            CL2G1A3 = price.CL2G1A3;
            CL2G1FN = price.CL2G1FN;
            CL2G2A1 = price.CL2G2A1;
            CL2G2A2 = price.CL2G2A2;
            CL2G2A3 = price.CL2G2A3;
            CL2G2FN = price.CL2G2FN;
            CL2G3A1 = price.CL2G3A1;
            CL2G3A2 = price.CL2G3A2;
            CL2G3A3 = price.CL2G3A3;
            CL2G3FN = price.CL2G3FN;

            // Level 3 prices
            CL3G1A1 = price.CL3G1A1;
            CL3G1A2 = price.CL3G1A2;
            CL3G1A3 = price.CL3G1A3;
            CL3G1FN = price.CL3G1FN;
            CL3G2A1 = price.CL3G2A1;
            CL3G2A2 = price.CL3G2A2;
            CL3G2A3 = price.CL3G2A3;
            CL3G2FN = price.CL3G2FN;
            CL3G3A1 = price.CL3G3A1;
            CL3G3A2 = price.CL3G3A2;
            CL3G3A3 = price.CL3G3A3;
            CL3G3FN = price.CL3G3FN;
        }

        [RelayCommand]
        private async Task Save()
        {
            if (!ValidateInput())
                return;

            IsSaving = true;

            try
            {
                var price = _isEditMode ? _originalPrice : new Price();

                // Basic info
                price.Product = SelectedProduct != null ? SelectedProduct.ProductId.ToString() : string.Empty;
                price.Process = SelectedProcess != null ? SelectedProcess.ProcessId.ToString() : string.Empty;
                price.From = EffectiveDate;

                // Time premium
                price.TimePrem = TimePremiumEnabled;
                price.Time = TimePremiumEnabled ? PremiumTime : string.Empty;
                price.CPremium = TimePremiumEnabled ? CanadianPremium : 0;
                price.UPremium = 0; // Not used

                // Level 1 Canadian prices
                price.CL1G1A1 = CL1G1A1;
                price.CL1G1A2 = CL1G1A2;
                price.CL1G1A3 = CL1G1A3;
                price.CL1G1FN = CL1G1FN;
                price.CL1G2A1 = CL1G2A1;
                price.CL1G2A2 = CL1G2A2;
                price.CL1G2A3 = CL1G2A3;
                price.CL1G2FN = CL1G2FN;
                price.CL1G3A1 = CL1G3A1;
                price.CL1G3A2 = CL1G3A2;
                price.CL1G3A3 = CL1G3A3;
                price.CL1G3FN = CL1G3FN;

                // Level 2 Canadian prices
                price.CL2G1A1 = CL2G1A1;
                price.CL2G1A2 = CL2G1A2;
                price.CL2G1A3 = CL2G1A3;
                price.CL2G1FN = CL2G1FN;
                price.CL2G2A1 = CL2G2A1;
                price.CL2G2A2 = CL2G2A2;
                price.CL2G2A3 = CL2G2A3;
                price.CL2G2FN = CL2G2FN;
                price.CL2G3A1 = CL2G3A1;
                price.CL2G3A2 = CL2G3A2;
                price.CL2G3A3 = CL2G3A3;
                price.CL2G3FN = CL2G3FN;

                // Level 3 Canadian prices
                price.CL3G1A1 = CL3G1A1;
                price.CL3G1A2 = CL3G1A2;
                price.CL3G1A3 = CL3G1A3;
                price.CL3G1FN = CL3G1FN;
                price.CL3G2A1 = CL3G2A1;
                price.CL3G2A2 = CL3G2A2;
                price.CL3G2A3 = CL3G2A3;
                price.CL3G2FN = CL3G2FN;
                price.CL3G3A1 = CL3G3A1;
                price.CL3G3A2 = CL3G3A2;
                price.CL3G3A3 = CL3G3A3;
                price.CL3G3FN = CL3G3FN;

                // Set US prices to 0 (will be removed eventually)
                price.UL1G1A1 = price.UL1G1A2 = price.UL1G1A3 = price.UL1G1FN = 0;
                price.UL1G2A1 = price.UL1G2A2 = price.UL1G2A3 = price.UL1G2FN = 0;
                price.UL1G3A1 = price.UL1G3A2 = price.UL1G3A3 = price.UL1G3FN = 0;
                price.UL2G1A1 = price.UL2G1A2 = price.UL2G1A3 = price.UL2G1FN = 0;
                price.UL2G2A1 = price.UL2G2A2 = price.UL2G2A3 = price.UL2G2FN = 0;
                price.UL2G3A1 = price.UL2G3A2 = price.UL2G3A3 = price.UL2G3FN = 0;
                price.UL3G1A1 = price.UL3G1A2 = price.UL3G1A3 = price.UL3G1FN = 0;
                price.UL3G2A1 = price.UL3G2A2 = price.UL3G2A3 = price.UL3G2FN = 0;
                price.UL3G3A1 = price.UL3G3A2 = price.UL3G3A3 = price.UL3G3FN = 0;

                if (_isEditMode)
                {
                    await _priceService.UpdateAsync(price);
                }
                else
                {
                    await _priceService.CreateAsync(price);
                }

                // Show success message
                await _dialogService.ShowMessageBoxAsync(
                    $"Price {(_isEditMode ? "updated" : "created")} successfully!", 
                    "Success");

                DialogResult = true;
                Infrastructure.Logging.Logger.Info($"Price {(_isEditMode ? "updated" : "created")} successfully");
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error saving price", ex);
                await _dialogService.ShowMessageBoxAsync($"Error saving price: {ex.Message}", "Error");
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
        }

        private bool ValidateInput()
        {
            // Validate product and process
            if (SelectedProduct == null)
            {
                _dialogService.ShowMessageBoxAsync("Please select a product.", "Validation Error");
                return false;
            }

            if (SelectedProcess == null)
            {
                _dialogService.ShowMessageBoxAsync("Please select a process type.", "Validation Error");
                return false;
            }

            // Validate effective date
            if (EffectiveDate == default)
            {
                _dialogService.ShowMessageBoxAsync("Please enter a valid effective date.", "Validation Error");
                return false;
            }

            // Validate no negative prices
            var allPrices = new[]
            {
                CL1G1A1, CL1G1A2, CL1G1A3, CL1G1FN,
                CL1G2A1, CL1G2A2, CL1G2A3, CL1G2FN,
                CL1G3A1, CL1G3A2, CL1G3A3, CL1G3FN,
                CL2G1A1, CL2G1A2, CL2G1A3, CL2G1FN,
                CL2G2A1, CL2G2A2, CL2G2A3, CL2G2FN,
                CL2G3A1, CL2G3A2, CL2G3A3, CL2G3FN,
                CL3G1A1, CL3G1A2, CL3G1A3, CL3G1FN,
                CL3G2A1, CL3G2A2, CL3G2A3, CL3G2FN,
                CL3G3A1, CL3G3A2, CL3G3A3, CL3G3FN
            };

            if (allPrices.Any(p => p < 0))
            {
                _dialogService.ShowMessageBoxAsync("Prices cannot be negative.", "Validation Error");
                return false;
            }

            // Validate Final price must be >= Highest cumulative advance for each level/grade
            // Business logic: Advances are cumulative (A2 includes A1, A3 includes A1+A2)
            // Final payment cannot be less than the highest cumulative advance payment
            var validationErrors = new List<string>();

            // Clear all error flags first
            // Level 1
            HasL1G1A1Error = HasL1G1A2Error = HasL1G1A3Error = HasL1G1FNError = false;
            HasL1G2A1Error = HasL1G2A2Error = HasL1G2A3Error = HasL1G2FNError = false;
            HasL1G3A1Error = HasL1G3A2Error = HasL1G3A3Error = HasL1G3FNError = false;
            
            // Level 2
            HasL2G1A1Error = HasL2G1A2Error = HasL2G1A3Error = HasL2G1FNError = false;
            HasL2G2A1Error = HasL2G2A2Error = HasL2G2A3Error = HasL2G2FNError = false;
            HasL2G3A1Error = HasL2G3A2Error = HasL2G3A3Error = HasL2G3FNError = false;
            
            // Level 3
            HasL3G1A1Error = HasL3G1A2Error = HasL3G1A3Error = HasL3G1FNError = false;
            HasL3G2A1Error = HasL3G2A2Error = HasL3G2A3Error = HasL3G2FNError = false;
            HasL3G3A1Error = HasL3G3A2Error = HasL3G3A3Error = HasL3G3FNError = false;

            // Level 1
            ValidateFinalPrice(1, 1, CL1G1A1, CL1G1A2, CL1G1A3, CL1G1FN, validationErrors, 
                ref _hasL1G1A1Error, ref _hasL1G1A2Error, ref _hasL1G1A3Error, ref _hasL1G1FNError);
            ValidateFinalPrice(1, 2, CL1G2A1, CL1G2A2, CL1G2A3, CL1G2FN, validationErrors, 
                ref _hasL1G2A1Error, ref _hasL1G2A2Error, ref _hasL1G2A3Error, ref _hasL1G2FNError);
            ValidateFinalPrice(1, 3, CL1G3A1, CL1G3A2, CL1G3A3, CL1G3FN, validationErrors, 
                ref _hasL1G3A1Error, ref _hasL1G3A2Error, ref _hasL1G3A3Error, ref _hasL1G3FNError);

            // Level 2
            ValidateFinalPrice(2, 1, CL2G1A1, CL2G1A2, CL2G1A3, CL2G1FN, validationErrors, 
                ref _hasL2G1A1Error, ref _hasL2G1A2Error, ref _hasL2G1A3Error, ref _hasL2G1FNError);
            ValidateFinalPrice(2, 2, CL2G2A1, CL2G2A2, CL2G2A3, CL2G2FN, validationErrors, 
                ref _hasL2G2A1Error, ref _hasL2G2A2Error, ref _hasL2G2A3Error, ref _hasL2G2FNError);
            ValidateFinalPrice(2, 3, CL2G3A1, CL2G3A2, CL2G3A3, CL2G3FN, validationErrors, 
                ref _hasL2G3A1Error, ref _hasL2G3A2Error, ref _hasL2G3A3Error, ref _hasL2G3FNError);

            // Level 3
            ValidateFinalPrice(3, 1, CL3G1A1, CL3G1A2, CL3G1A3, CL3G1FN, validationErrors, 
                ref _hasL3G1A1Error, ref _hasL3G1A2Error, ref _hasL3G1A3Error, ref _hasL3G1FNError);
            ValidateFinalPrice(3, 2, CL3G2A1, CL3G2A2, CL3G2A3, CL3G2FN, validationErrors, 
                ref _hasL3G2A1Error, ref _hasL3G2A2Error, ref _hasL3G2A3Error, ref _hasL3G2FNError);
            ValidateFinalPrice(3, 3, CL3G3A1, CL3G3A2, CL3G3A3, CL3G3FN, validationErrors, 
                ref _hasL3G3A1Error, ref _hasL3G3A2Error, ref _hasL3G3A3Error, ref _hasL3G3FNError);

            // Trigger property change notifications for all error flags
            // Level 1
            OnPropertyChanged(nameof(HasL1G1A1Error)); OnPropertyChanged(nameof(HasL1G1A2Error)); OnPropertyChanged(nameof(HasL1G1A3Error)); OnPropertyChanged(nameof(HasL1G1FNError));
            OnPropertyChanged(nameof(HasL1G2A1Error)); OnPropertyChanged(nameof(HasL1G2A2Error)); OnPropertyChanged(nameof(HasL1G2A3Error)); OnPropertyChanged(nameof(HasL1G2FNError));
            OnPropertyChanged(nameof(HasL1G3A1Error)); OnPropertyChanged(nameof(HasL1G3A2Error)); OnPropertyChanged(nameof(HasL1G3A3Error)); OnPropertyChanged(nameof(HasL1G3FNError));
            
            // Level 2
            OnPropertyChanged(nameof(HasL2G1A1Error)); OnPropertyChanged(nameof(HasL2G1A2Error)); OnPropertyChanged(nameof(HasL2G1A3Error)); OnPropertyChanged(nameof(HasL2G1FNError));
            OnPropertyChanged(nameof(HasL2G2A1Error)); OnPropertyChanged(nameof(HasL2G2A2Error)); OnPropertyChanged(nameof(HasL2G2A3Error)); OnPropertyChanged(nameof(HasL2G2FNError));
            OnPropertyChanged(nameof(HasL2G3A1Error)); OnPropertyChanged(nameof(HasL2G3A2Error)); OnPropertyChanged(nameof(HasL2G3A3Error)); OnPropertyChanged(nameof(HasL2G3FNError));
            
            // Level 3
            OnPropertyChanged(nameof(HasL3G1A1Error)); OnPropertyChanged(nameof(HasL3G1A2Error)); OnPropertyChanged(nameof(HasL3G1A3Error)); OnPropertyChanged(nameof(HasL3G1FNError));
            OnPropertyChanged(nameof(HasL3G2A1Error)); OnPropertyChanged(nameof(HasL3G2A2Error)); OnPropertyChanged(nameof(HasL3G2A3Error)); OnPropertyChanged(nameof(HasL3G2FNError));
            OnPropertyChanged(nameof(HasL3G3A1Error)); OnPropertyChanged(nameof(HasL3G3A2Error)); OnPropertyChanged(nameof(HasL3G3A3Error)); OnPropertyChanged(nameof(HasL3G3FNError));

            if (validationErrors.Any())
            {
                _dialogService.ShowMessageBoxAsync(
                    $"Final price validation errors:\n\n{string.Join("\n", validationErrors)}",
                    "Validation Error");
                return false;
            }

            // Validate time premium
            if (TimePremiumEnabled)
            {
                if (string.IsNullOrWhiteSpace(PremiumTime))
                {
                    _dialogService.ShowMessageBoxAsync("Please enter a time for the premium (e.g., 10:10).", "Validation Error");
                    return false;
                }

                if (CanadianPremium < 0)
                {
                    _dialogService.ShowMessageBoxAsync("Premium amount cannot be negative.", "Validation Error");
                    return false;
                }
            }

            return true;
        }

        private void ValidateFinalPrice(int level, int grade, decimal adv1, decimal adv2, decimal adv3, decimal final, 
            List<string> errors, ref bool hasA1Error, ref bool hasA2Error, ref bool hasA3Error, ref bool hasFinalError)
        {
            // Reset all error flags for this level/grade
            hasA1Error = hasA2Error = hasA3Error = hasFinalError = false;
            
            // Skip validation if all values are 0 (not set)
            if (adv1 == 0 && adv2 == 0 && adv3 == 0 && final == 0)
            {
                return;
            }

            // Validate cumulative progression
            // A2 cannot be less than A1 (if A2 is provided and A1 is provided)
            if (adv1 > 0 && adv2 > 0 && adv2 < adv1)
            {
                errors.Add($"Level {level}, Grade {grade}: Advance 2 (${adv2:F2}) cannot be less than Advance 1 (${adv1:F2})");
                hasA2Error = true;
            }

            // A3 cannot be less than A2 (if A3 is provided and A2 is provided)
            if (adv2 > 0 && adv3 > 0 && adv3 < adv2)
            {
                errors.Add($"Level {level}, Grade {grade}: Advance 3 (${adv3:F2}) cannot be less than Advance 2 (${adv2:F2})");
                hasA3Error = true;
            }

            // Final price validation: Final >= max(A1, A2, A3)
            var maxAdvance = Math.Max(Math.Max(adv1, adv2), adv3);
            
            if (maxAdvance > 0 && final < maxAdvance)
            {
                errors.Add($"Level {level}, Grade {grade}: Highest cumulative advance (${maxAdvance:F2}) exceeds final price (${final:F2})");
                hasFinalError = true;
            }
        }

        // Real-time validation methods for individual field validation
        private void ValidateLevelGradeRealtime(int level, int grade)
        {
            // Get the current values for this level/grade
            decimal adv1, adv2, adv3, final;
            bool hasA1Error, hasA2Error, hasA3Error, hasFinalError;

            switch (level)
            {
                case 1:
                    switch (grade)
                    {
                        case 1:
                            adv1 = CL1G1A1; adv2 = CL1G1A2; adv3 = CL1G1A3; final = CL1G1FN;
                            hasA1Error = _hasL1G1A1Error; hasA2Error = _hasL1G1A2Error; hasA3Error = _hasL1G1A3Error; hasFinalError = _hasL1G1FNError;
                            break;
                        case 2:
                            adv1 = CL1G2A1; adv2 = CL1G2A2; adv3 = CL1G2A3; final = CL1G2FN;
                            hasA1Error = _hasL1G2A1Error; hasA2Error = _hasL1G2A2Error; hasA3Error = _hasL1G2A3Error; hasFinalError = _hasL1G2FNError;
                            break;
                        case 3:
                            adv1 = CL1G3A1; adv2 = CL1G3A2; adv3 = CL1G3A3; final = CL1G3FN;
                            hasA1Error = _hasL1G3A1Error; hasA2Error = _hasL1G3A2Error; hasA3Error = _hasL1G3A3Error; hasFinalError = _hasL1G3FNError;
                            break;
                        default: return;
                    }
                    break;
                case 2:
                    switch (grade)
                    {
                        case 1:
                            adv1 = CL2G1A1; adv2 = CL2G1A2; adv3 = CL2G1A3; final = CL2G1FN;
                            hasA1Error = _hasL2G1A1Error; hasA2Error = _hasL2G1A2Error; hasA3Error = _hasL2G1A3Error; hasFinalError = _hasL2G1FNError;
                            break;
                        case 2:
                            adv1 = CL2G2A1; adv2 = CL2G2A2; adv3 = CL2G2A3; final = CL2G2FN;
                            hasA1Error = _hasL2G2A1Error; hasA2Error = _hasL2G2A2Error; hasA3Error = _hasL2G2A3Error; hasFinalError = _hasL2G2FNError;
                            break;
                        case 3:
                            adv1 = CL2G3A1; adv2 = CL2G3A2; adv3 = CL2G3A3; final = CL2G3FN;
                            hasA1Error = _hasL2G3A1Error; hasA2Error = _hasL2G3A2Error; hasA3Error = _hasL2G3A3Error; hasFinalError = _hasL2G3FNError;
                            break;
                        default: return;
                    }
                    break;
                case 3:
                    switch (grade)
                    {
                        case 1:
                            adv1 = CL3G1A1; adv2 = CL3G1A2; adv3 = CL3G1A3; final = CL3G1FN;
                            hasA1Error = _hasL3G1A1Error; hasA2Error = _hasL3G1A2Error; hasA3Error = _hasL3G1A3Error; hasFinalError = _hasL3G1FNError;
                            break;
                        case 2:
                            adv1 = CL3G2A1; adv2 = CL3G2A2; adv3 = CL3G2A3; final = CL3G2FN;
                            hasA1Error = _hasL3G2A1Error; hasA2Error = _hasL3G2A2Error; hasA3Error = _hasL3G2A3Error; hasFinalError = _hasL3G2FNError;
                            break;
                        case 3:
                            adv1 = CL3G3A1; adv2 = CL3G3A2; adv3 = CL3G3A3; final = CL3G3FN;
                            hasA1Error = _hasL3G3A1Error; hasA2Error = _hasL3G3A2Error; hasA3Error = _hasL3G3A3Error; hasFinalError = _hasL3G3FNError;
                            break;
                        default: return;
                    }
                    break;
                default: return;
            }

            // Perform real-time validation (without error messages, just set flags)
            var errors = new List<string>();
            ValidateFinalPrice(level, grade, adv1, adv2, adv3, final, errors, ref hasA1Error, ref hasA2Error, ref hasA3Error, ref hasFinalError);

            // Update the error flags and trigger property change notifications
            UpdateErrorFlags(level, grade, hasA1Error, hasA2Error, hasA3Error, hasFinalError);
        }

        private void UpdateErrorFlags(int level, int grade, bool hasA1Error, bool hasA2Error, bool hasA3Error, bool hasFinalError)
        {
            switch (level)
            {
                case 1:
                    switch (grade)
                    {
                        case 1:
                            HasL1G1A1Error = hasA1Error; HasL1G1A2Error = hasA2Error; HasL1G1A3Error = hasA3Error; HasL1G1FNError = hasFinalError;
                            break;
                        case 2:
                            HasL1G2A1Error = hasA1Error; HasL1G2A2Error = hasA2Error; HasL1G2A3Error = hasA3Error; HasL1G2FNError = hasFinalError;
                            break;
                        case 3:
                            HasL1G3A1Error = hasA1Error; HasL1G3A2Error = hasA2Error; HasL1G3A3Error = hasA3Error; HasL1G3FNError = hasFinalError;
                            break;
                    }
                    break;
                case 2:
                    switch (grade)
                    {
                        case 1:
                            HasL2G1A1Error = hasA1Error; HasL2G1A2Error = hasA2Error; HasL2G1A3Error = hasA3Error; HasL2G1FNError = hasFinalError;
                            break;
                        case 2:
                            HasL2G2A1Error = hasA1Error; HasL2G2A2Error = hasA2Error; HasL2G2A3Error = hasA3Error; HasL2G2FNError = hasFinalError;
                            break;
                        case 3:
                            HasL2G3A1Error = hasA1Error; HasL2G3A2Error = hasA2Error; HasL2G3A3Error = hasA3Error; HasL2G3FNError = hasFinalError;
                            break;
                    }
                    break;
                case 3:
                    switch (grade)
                    {
                        case 1:
                            HasL3G1A1Error = hasA1Error; HasL3G1A2Error = hasA2Error; HasL3G1A3Error = hasA3Error; HasL3G1FNError = hasFinalError;
                            break;
                        case 2:
                            HasL3G2A1Error = hasA1Error; HasL3G2A2Error = hasA2Error; HasL3G2A3Error = hasA3Error; HasL3G2FNError = hasFinalError;
                            break;
                        case 3:
                            HasL3G3A1Error = hasA1Error; HasL3G3A2Error = hasA2Error; HasL3G3A3Error = hasA3Error; HasL3G3FNError = hasFinalError;
                            break;
                    }
                    break;
            }
        }
    }
}
