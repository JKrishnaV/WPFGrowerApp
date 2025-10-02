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
        private readonly Price _originalPrice;
        private readonly bool _isEditMode;

        [ObservableProperty]
        private string _windowTitle;

        // Product and Process
        [ObservableProperty]
        private ObservableCollection<Product> _products;

        [ObservableProperty]
        private Product _selectedProduct;

        [ObservableProperty]
        private ObservableCollection<Process> _processes;

        [ObservableProperty]
        private Process _selectedProcess;

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
        private bool _hasL1G1FNError;
        public bool HasL1G1FNError
        {
            get => _hasL1G1FNError;
            set => SetProperty(ref _hasL1G1FNError, value);
        }

        private bool _hasL1G2FNError;
        public bool HasL1G2FNError
        {
            get => _hasL1G2FNError;
            set => SetProperty(ref _hasL1G2FNError, value);
        }

        private bool _hasL1G3FNError;
        public bool HasL1G3FNError
        {
            get => _hasL1G3FNError;
            set => SetProperty(ref _hasL1G3FNError, value);
        }

        private bool _hasL2G1FNError;
        public bool HasL2G1FNError
        {
            get => _hasL2G1FNError;
            set => SetProperty(ref _hasL2G1FNError, value);
        }

        private bool _hasL2G2FNError;
        public bool HasL2G2FNError
        {
            get => _hasL2G2FNError;
            set => SetProperty(ref _hasL2G2FNError, value);
        }

        private bool _hasL2G3FNError;
        public bool HasL2G3FNError
        {
            get => _hasL2G3FNError;
            set => SetProperty(ref _hasL2G3FNError, value);
        }

        private bool _hasL3G1FNError;
        public bool HasL3G1FNError
        {
            get => _hasL3G1FNError;
            set => SetProperty(ref _hasL3G1FNError, value);
        }

        private bool _hasL3G2FNError;
        public bool HasL3G2FNError
        {
            get => _hasL3G2FNError;
            set => SetProperty(ref _hasL3G2FNError, value);
        }

        private bool _hasL3G3FNError;
        public bool HasL3G3FNError
        {
            get => _hasL3G3FNError;
            set => SetProperty(ref _hasL3G3FNError, value);
        }

        public bool DialogResult { get; private set; }

        public PriceEntryViewModel(
            IPriceService priceService,
            IProductService productService,
            IProcessService processService,
            IDialogService dialogService,
            Price priceToEdit = null,
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
                : (_isEditMode ? $"Edit Price - ID# {priceToEdit.PriceID}" : "Add New Price");
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

                Products = new ObservableCollection<Product>(products);
                Processes = new ObservableCollection<Process>(processes);

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
            // Set product and process
            SelectedProduct = Products?.FirstOrDefault(p => p.ProductId == price.Product);
            SelectedProcess = Processes?.FirstOrDefault(p => p.ProcessId == price.Process);
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
                price.Product = SelectedProduct?.ProductId;
                price.Process = SelectedProcess?.ProcessId;
                price.From = EffectiveDate;

                // Time premium
                price.TimePrem = TimePremiumEnabled;
                price.Time = TimePremiumEnabled ? PremiumTime : null;
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

            // Validate Final price must be >= Sum of advances for each level/grade
            // Business logic: Final payment cannot be less than the sum of advance payments
            var validationErrors = new List<string>();

            // Clear all error flags first
            HasL1G1FNError = HasL1G2FNError = HasL1G3FNError = false;
            HasL2G1FNError = HasL2G2FNError = HasL2G3FNError = false;
            HasL3G1FNError = HasL3G2FNError = HasL3G3FNError = false;

            // Level 1
            ValidateFinalPrice(1, 1, CL1G1A1, CL1G1A2, CL1G1A3, CL1G1FN, validationErrors, ref _hasL1G1FNError);
            ValidateFinalPrice(1, 2, CL1G2A1, CL1G2A2, CL1G2A3, CL1G2FN, validationErrors, ref _hasL1G2FNError);
            ValidateFinalPrice(1, 3, CL1G3A1, CL1G3A2, CL1G3A3, CL1G3FN, validationErrors, ref _hasL1G3FNError);

            // Level 2
            ValidateFinalPrice(2, 1, CL2G1A1, CL2G1A2, CL2G1A3, CL2G1FN, validationErrors, ref _hasL2G1FNError);
            ValidateFinalPrice(2, 2, CL2G2A1, CL2G2A2, CL2G2A3, CL2G2FN, validationErrors, ref _hasL2G2FNError);
            ValidateFinalPrice(2, 3, CL2G3A1, CL2G3A2, CL2G3A3, CL2G3FN, validationErrors, ref _hasL2G3FNError);

            // Level 3
            ValidateFinalPrice(3, 1, CL3G1A1, CL3G1A2, CL3G1A3, CL3G1FN, validationErrors, ref _hasL3G1FNError);
            ValidateFinalPrice(3, 2, CL3G2A1, CL3G2A2, CL3G2A3, CL3G2FN, validationErrors, ref _hasL3G2FNError);
            ValidateFinalPrice(3, 3, CL3G3A1, CL3G3A2, CL3G3A3, CL3G3FN, validationErrors, ref _hasL3G3FNError);

            // Trigger property change notifications for error flags
            OnPropertyChanged(nameof(HasL1G1FNError));
            OnPropertyChanged(nameof(HasL1G2FNError));
            OnPropertyChanged(nameof(HasL1G3FNError));
            OnPropertyChanged(nameof(HasL2G1FNError));
            OnPropertyChanged(nameof(HasL2G2FNError));
            OnPropertyChanged(nameof(HasL2G3FNError));
            OnPropertyChanged(nameof(HasL3G1FNError));
            OnPropertyChanged(nameof(HasL3G2FNError));
            OnPropertyChanged(nameof(HasL3G3FNError));

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

        private void ValidateFinalPrice(int level, int grade, decimal adv1, decimal adv2, decimal adv3, decimal final, List<string> errors, ref bool hasError)
        {
            // Skip validation if all values are 0 (not set)
            if (adv1 == 0 && adv2 == 0 && adv3 == 0 && final == 0)
            {
                hasError = false;
                return;
            }

            var sumAdvances = adv1 + adv2 + adv3;
            
            // Final price should be greater than or equal to sum of advances
            // You cannot have the final payment be less than what's already been paid in advances
            if (sumAdvances > 0 && final < sumAdvances)
            {
                errors.Add($"Level {level}, Grade {grade}: Sum of advances (${sumAdvances:F2}) exceeds final price (${final:F2})");
                hasError = true;
            }
            else
            {
                hasError = false;
            }
        }
    }
}
