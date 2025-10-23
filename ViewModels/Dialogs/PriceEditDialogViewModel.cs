using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    public class PriceEditDialogViewModel : ViewModelBase, IDataErrorInfo
    {
        private Price _priceData;
        private readonly Price? _originalPrice;
        private readonly IDialogService _dialogService;
        private readonly bool _isEditMode;
        private readonly bool _isReadOnly;

        private string _title;
        private Product? _selectedProduct;
        private Process? _selectedProcess;
        private DateTime _effectiveDate;
        private bool _timePremiumEnabled;
        private string _premiumTime;
        private decimal _canadianPremium;
        private bool _hasUnsavedChanges;
        private bool _showWarningBanner;
        private int _selectedTabIndex;

        // 36 price properties (3 levels × 3 grades × 4 payment types)
        // Level 1
        private decimal _cL1G1A1, _cL1G1A2, _cL1G1A3, _cL1G1FN;
        private decimal _cL1G2A1, _cL1G2A2, _cL1G2A3, _cL1G2FN;
        private decimal _cL1G3A1, _cL1G3A2, _cL1G3A3, _cL1G3FN;
        // Level 2
        private decimal _cL2G1A1, _cL2G1A2, _cL2G1A3, _cL2G1FN;
        private decimal _cL2G2A1, _cL2G2A2, _cL2G2A3, _cL2G2FN;
        private decimal _cL2G3A1, _cL2G3A2, _cL2G3A3, _cL2G3FN;
        // Level 3
        private decimal _cL3G1A1, _cL3G1A2, _cL3G1A3, _cL3G1FN;
        private decimal _cL3G2A1, _cL3G2A2, _cL3G2A3, _cL3G2FN;
        private decimal _cL3G3A1, _cL3G3A2, _cL3G3A3, _cL3G3FN;

        // Validation error flags (36 flags)
        private bool _hasL1G1A1Error, _hasL1G1A2Error, _hasL1G1A3Error, _hasL1G1FNError;
        private bool _hasL1G2A1Error, _hasL1G2A2Error, _hasL1G2A3Error, _hasL1G2FNError;
        private bool _hasL1G3A1Error, _hasL1G3A2Error, _hasL1G3A3Error, _hasL1G3FNError;
        private bool _hasL2G1A1Error, _hasL2G1A2Error, _hasL2G1A3Error, _hasL2G1FNError;
        private bool _hasL2G2A1Error, _hasL2G2A2Error, _hasL2G2A3Error, _hasL2G2FNError;
        private bool _hasL2G3A1Error, _hasL2G3A2Error, _hasL2G3A3Error, _hasL2G3FNError;
        private bool _hasL3G1A1Error, _hasL3G1A2Error, _hasL3G1A3Error, _hasL3G1FNError;
        private bool _hasL3G2A1Error, _hasL3G2A2Error, _hasL3G2A3Error, _hasL3G2FNError;
        private bool _hasL3G3A1Error, _hasL3G3A2Error, _hasL3G3A3Error, _hasL3G3FNError;

        public Price PriceData
        {
            get => _priceData;
            set => SetProperty(ref _priceData, value);
        }

        public bool IsEditMode => _isEditMode;
        public bool IsReadOnly => _isReadOnly;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public List<Product> Products { get; }
        public List<Process> Processes { get; }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public Process? SelectedProcess
        {
            get => _selectedProcess;
            set => SetProperty(ref _selectedProcess, value);
        }

        public DateTime EffectiveDate
        {
            get => _effectiveDate;
            set => SetProperty(ref _effectiveDate, value);
        }

        public bool TimePremiumEnabled
        {
            get => _timePremiumEnabled;
            set => SetProperty(ref _timePremiumEnabled, value);
        }

        public string PremiumTime
        {
            get => _premiumTime;
            set => SetProperty(ref _premiumTime, value);
        }

        public decimal CanadianPremium
        {
            get => _canadianPremium;
            set => SetProperty(ref _canadianPremium, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public bool ShowWarningBanner
        {
            get => _showWarningBanner;
            set => SetProperty(ref _showWarningBanner, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public bool WasSaved { get; private set; } = false;

        // Level 1 Properties
        public decimal CL1G1A1 { get => _cL1G1A1; set => SetProperty(ref _cL1G1A1, value); }
        public decimal CL1G1A2 { get => _cL1G1A2; set => SetProperty(ref _cL1G1A2, value); }
        public decimal CL1G1A3 { get => _cL1G1A3; set => SetProperty(ref _cL1G1A3, value); }
        public decimal CL1G1FN { get => _cL1G1FN; set => SetProperty(ref _cL1G1FN, value); }
        public decimal CL1G2A1 { get => _cL1G2A1; set => SetProperty(ref _cL1G2A1, value); }
        public decimal CL1G2A2 { get => _cL1G2A2; set => SetProperty(ref _cL1G2A2, value); }
        public decimal CL1G2A3 { get => _cL1G2A3; set => SetProperty(ref _cL1G2A3, value); }
        public decimal CL1G2FN { get => _cL1G2FN; set => SetProperty(ref _cL1G2FN, value); }
        public decimal CL1G3A1 { get => _cL1G3A1; set => SetProperty(ref _cL1G3A1, value); }
        public decimal CL1G3A2 { get => _cL1G3A2; set => SetProperty(ref _cL1G3A2, value); }
        public decimal CL1G3A3 { get => _cL1G3A3; set => SetProperty(ref _cL1G3A3, value); }
        public decimal CL1G3FN { get => _cL1G3FN; set => SetProperty(ref _cL1G3FN, value); }

        // Level 2 Properties
        public decimal CL2G1A1 { get => _cL2G1A1; set => SetProperty(ref _cL2G1A1, value); }
        public decimal CL2G1A2 { get => _cL2G1A2; set => SetProperty(ref _cL2G1A2, value); }
        public decimal CL2G1A3 { get => _cL2G1A3; set => SetProperty(ref _cL2G1A3, value); }
        public decimal CL2G1FN { get => _cL2G1FN; set => SetProperty(ref _cL2G1FN, value); }
        public decimal CL2G2A1 { get => _cL2G2A1; set => SetProperty(ref _cL2G2A1, value); }
        public decimal CL2G2A2 { get => _cL2G2A2; set => SetProperty(ref _cL2G2A2, value); }
        public decimal CL2G2A3 { get => _cL2G2A3; set => SetProperty(ref _cL2G2A3, value); }
        public decimal CL2G2FN { get => _cL2G2FN; set => SetProperty(ref _cL2G2FN, value); }
        public decimal CL2G3A1 { get => _cL2G3A1; set => SetProperty(ref _cL2G3A1, value); }
        public decimal CL2G3A2 { get => _cL2G3A2; set => SetProperty(ref _cL2G3A2, value); }
        public decimal CL2G3A3 { get => _cL2G3A3; set => SetProperty(ref _cL2G3A3, value); }
        public decimal CL2G3FN { get => _cL2G3FN; set => SetProperty(ref _cL2G3FN, value); }

        // Level 3 Properties
        public decimal CL3G1A1 { get => _cL3G1A1; set => SetProperty(ref _cL3G1A1, value); }
        public decimal CL3G1A2 { get => _cL3G1A2; set => SetProperty(ref _cL3G1A2, value); }
        public decimal CL3G1A3 { get => _cL3G1A3; set => SetProperty(ref _cL3G1A3, value); }
        public decimal CL3G1FN { get => _cL3G1FN; set => SetProperty(ref _cL3G1FN, value); }
        public decimal CL3G2A1 { get => _cL3G2A1; set => SetProperty(ref _cL3G2A1, value); }
        public decimal CL3G2A2 { get => _cL3G2A2; set => SetProperty(ref _cL3G2A2, value); }
        public decimal CL3G2A3 { get => _cL3G2A3; set => SetProperty(ref _cL3G2A3, value); }
        public decimal CL3G2FN { get => _cL3G2FN; set => SetProperty(ref _cL3G2FN, value); }
        public decimal CL3G3A1 { get => _cL3G3A1; set => SetProperty(ref _cL3G3A1, value); }
        public decimal CL3G3A2 { get => _cL3G3A2; set => SetProperty(ref _cL3G3A2, value); }
        public decimal CL3G3A3 { get => _cL3G3A3; set => SetProperty(ref _cL3G3A3, value); }
        public decimal CL3G3FN { get => _cL3G3FN; set => SetProperty(ref _cL3G3FN, value); }

        // Error flags properties (needed for binding)
        public bool HasL1G1A1Error { get => _hasL1G1A1Error; set => SetProperty(ref _hasL1G1A1Error, value); }
        public bool HasL1G1A2Error { get => _hasL1G1A2Error; set => SetProperty(ref _hasL1G1A2Error, value); }
        public bool HasL1G1A3Error { get => _hasL1G1A3Error; set => SetProperty(ref _hasL1G1A3Error, value); }
        public bool HasL1G1FNError { get => _hasL1G1FNError; set => SetProperty(ref _hasL1G1FNError, value); }
        public bool HasL1G2A1Error { get => _hasL1G2A1Error; set => SetProperty(ref _hasL1G2A1Error, value); }
        public bool HasL1G2A2Error { get => _hasL1G2A2Error; set => SetProperty(ref _hasL1G2A2Error, value); }
        public bool HasL1G2A3Error { get => _hasL1G2A3Error; set => SetProperty(ref _hasL1G2A3Error, value); }
        public bool HasL1G2FNError { get => _hasL1G2FNError; set => SetProperty(ref _hasL1G2FNError, value); }
        public bool HasL1G3A1Error { get => _hasL1G3A1Error; set => SetProperty(ref _hasL1G3A1Error, value); }
        public bool HasL1G3A2Error { get => _hasL1G3A2Error; set => SetProperty(ref _hasL1G3A2Error, value); }
        public bool HasL1G3A3Error { get => _hasL1G3A3Error; set => SetProperty(ref _hasL1G3A3Error, value); }
        public bool HasL1G3FNError { get => _hasL1G3FNError; set => SetProperty(ref _hasL1G3FNError, value); }

        public bool HasL2G1A1Error { get => _hasL2G1A1Error; set => SetProperty(ref _hasL2G1A1Error, value); }
        public bool HasL2G1A2Error { get => _hasL2G1A2Error; set => SetProperty(ref _hasL2G1A2Error, value); }
        public bool HasL2G1A3Error { get => _hasL2G1A3Error; set => SetProperty(ref _hasL2G1A3Error, value); }
        public bool HasL2G1FNError { get => _hasL2G1FNError; set => SetProperty(ref _hasL2G1FNError, value); }
        public bool HasL2G2A1Error { get => _hasL2G2A1Error; set => SetProperty(ref _hasL2G2A1Error, value); }
        public bool HasL2G2A2Error { get => _hasL2G2A2Error; set => SetProperty(ref _hasL2G2A2Error, value); }
        public bool HasL2G2A3Error { get => _hasL2G2A3Error; set => SetProperty(ref _hasL2G2A3Error, value); }
        public bool HasL2G2FNError { get => _hasL2G2FNError; set => SetProperty(ref _hasL2G2FNError, value); }
        public bool HasL2G3A1Error { get => _hasL2G3A1Error; set => SetProperty(ref _hasL2G3A1Error, value); }
        public bool HasL2G3A2Error { get => _hasL2G3A2Error; set => SetProperty(ref _hasL2G3A2Error, value); }
        public bool HasL2G3A3Error { get => _hasL2G3A3Error; set => SetProperty(ref _hasL2G3A3Error, value); }
        public bool HasL2G3FNError { get => _hasL2G3FNError; set => SetProperty(ref _hasL2G3FNError, value); }

        public bool HasL3G1A1Error { get => _hasL3G1A1Error; set => SetProperty(ref _hasL3G1A1Error, value); }
        public bool HasL3G1A2Error { get => _hasL3G1A2Error; set => SetProperty(ref _hasL3G1A2Error, value); }
        public bool HasL3G1A3Error { get => _hasL3G1A3Error; set => SetProperty(ref _hasL3G1A3Error, value); }
        public bool HasL3G1FNError { get => _hasL3G1FNError; set => SetProperty(ref _hasL3G1FNError, value); }
        public bool HasL3G2A1Error { get => _hasL3G2A1Error; set => SetProperty(ref _hasL3G2A1Error, value); }
        public bool HasL3G2A2Error { get => _hasL3G2A2Error; set => SetProperty(ref _hasL3G2A2Error, value); }
        public bool HasL3G2A3Error { get => _hasL3G2A3Error; set => SetProperty(ref _hasL3G2A3Error, value); }
        public bool HasL3G2FNError { get => _hasL3G2FNError; set => SetProperty(ref _hasL3G2FNError, value); }
        public bool HasL3G3A1Error { get => _hasL3G3A1Error; set => SetProperty(ref _hasL3G3A1Error, value); }
        public bool HasL3G3A2Error { get => _hasL3G3A2Error; set => SetProperty(ref _hasL3G3A2Error, value); }
        public bool HasL3G3A3Error { get => _hasL3G3A3Error; set => SetProperty(ref _hasL3G3A3Error, value); }
        public bool HasL3G3FNError { get => _hasL3G3FNError; set => SetProperty(ref _hasL3G3FNError, value); }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ConfirmDiscardCommand { get; }
        public ICommand CancelDiscardCommand { get; }

        public PriceEditDialogViewModel(
            Price? price,
            List<Product> products,
            List<Process> processes,
            IDialogService dialogService,
            bool isReadOnly)
        {
            _dialogService = dialogService;
            Products = products ?? new List<Product>();
            Processes = processes ?? new List<Process>();
            _isReadOnly = isReadOnly;

            if (price == null)
            {
                // Add Mode
                _priceData = new Price();
                _isEditMode = false;
                Title = "Add New Price";
                EffectiveDate = DateTime.Today;
                PremiumTime = "10:10";
                SelectedTabIndex = 0;
                _originalPrice = null;
            }
            else
            {
                // Edit/View Mode
                _priceData = ClonePrice(price);
                _originalPrice = ClonePrice(price);
                _isEditMode = true;
                Title = isReadOnly ? $"View Price - ID# {price.PriceID} (Read-Only)" : $"Edit Price - ID# {price.PriceID}";
                LoadPriceData(price);
            }

            // Subscribe to property changes to detect modifications
            PropertyChanged += OnPropertyChanged;

            // Initialize commands
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            ConfirmDiscardCommand = new RelayCommand(ConfirmDiscard);
            CancelDiscardCommand = new RelayCommand(CancelDiscard);
        }

        private void LoadPriceData(Price price)
        {
            // Set product and process
            SelectedProduct = Products.FirstOrDefault(p => p.ProductId == price.ProductId);
            SelectedProcess = Processes.FirstOrDefault(p => p.ProcessId == price.ProcessId);
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

            SelectedTabIndex = 0;
        }

        private Price ClonePrice(Price price)
        {
            return new Price
            {
                PriceID = price.PriceID,
                ProductId = price.ProductId,
                Product = price.Product,
                ProcessId = price.ProcessId,
                Process = price.Process,
                From = price.From,
                TimePrem = price.TimePrem,
                Time = price.Time,
                CPremium = price.CPremium,
                UPremium = price.UPremium,
                CL1G1A1 = price.CL1G1A1,
                CL1G1A2 = price.CL1G1A2,
                CL1G1A3 = price.CL1G1A3,
                CL1G1FN = price.CL1G1FN,
                CL1G2A1 = price.CL1G2A1,
                CL1G2A2 = price.CL1G2A2,
                CL1G2A3 = price.CL1G2A3,
                CL1G2FN = price.CL1G2FN,
                CL1G3A1 = price.CL1G3A1,
                CL1G3A2 = price.CL1G3A2,
                CL1G3A3 = price.CL1G3A3,
                CL1G3FN = price.CL1G3FN,
                CL2G1A1 = price.CL2G1A1,
                CL2G1A2 = price.CL2G1A2,
                CL2G1A3 = price.CL2G1A3,
                CL2G1FN = price.CL2G1FN,
                CL2G2A1 = price.CL2G2A1,
                CL2G2A2 = price.CL2G2A2,
                CL2G2A3 = price.CL2G2A3,
                CL2G2FN = price.CL2G2FN,
                CL2G3A1 = price.CL2G3A1,
                CL2G3A2 = price.CL2G3A2,
                CL2G3A3 = price.CL2G3A3,
                CL2G3FN = price.CL2G3FN,
                CL3G1A1 = price.CL3G1A1,
                CL3G1A2 = price.CL3G1A2,
                CL3G1A3 = price.CL3G1A3,
                CL3G1FN = price.CL3G1FN,
                CL3G2A1 = price.CL3G2A1,
                CL3G2A2 = price.CL3G2A2,
                CL3G2A3 = price.CL3G2A3,
                CL3G2FN = price.CL3G2FN,
                CL3G3A1 = price.CL3G3A1,
                CL3G3A2 = price.CL3G3A2,
                CL3G3A3 = price.CL3G3A3,
                CL3G3FN = price.CL3G3FN,
                Adv1Used = price.Adv1Used,
                Adv2Used = price.Adv2Used,
                Adv3Used = price.Adv3Used,
                FinUsed = price.FinUsed
            };
        }

        private void Save(object parameter)
        {
            if (IsReadOnly)
                return;

            // Perform final validation
            if (!IsValid())
            {
                return;
            }

            // Update the price object with form values
            PriceData.ProductId = SelectedProduct?.ProductId ?? 0;
            PriceData.Product = SelectedProduct?.ProductCode ?? string.Empty;
            PriceData.ProcessId = SelectedProcess?.ProcessId ?? 0;
            PriceData.Process = SelectedProcess?.ProcessCode ?? string.Empty;
            PriceData.From = EffectiveDate;

            // Time premium
            PriceData.TimePrem = TimePremiumEnabled;
            PriceData.Time = TimePremiumEnabled ? PremiumTime : string.Empty;
            PriceData.CPremium = TimePremiumEnabled ? CanadianPremium : 0;
            PriceData.UPremium = 0; // Not used

            // Level 1 Canadian prices
            PriceData.CL1G1A1 = CL1G1A1;
            PriceData.CL1G1A2 = CL1G1A2;
            PriceData.CL1G1A3 = CL1G1A3;
            PriceData.CL1G1FN = CL1G1FN;
            PriceData.CL1G2A1 = CL1G2A1;
            PriceData.CL1G2A2 = CL1G2A2;
            PriceData.CL1G2A3 = CL1G2A3;
            PriceData.CL1G2FN = CL1G2FN;
            PriceData.CL1G3A1 = CL1G3A1;
            PriceData.CL1G3A2 = CL1G3A2;
            PriceData.CL1G3A3 = CL1G3A3;
            PriceData.CL1G3FN = CL1G3FN;

            // Level 2 Canadian prices
            PriceData.CL2G1A1 = CL2G1A1;
            PriceData.CL2G1A2 = CL2G1A2;
            PriceData.CL2G1A3 = CL2G1A3;
            PriceData.CL2G1FN = CL2G1FN;
            PriceData.CL2G2A1 = CL2G2A1;
            PriceData.CL2G2A2 = CL2G2A2;
            PriceData.CL2G2A3 = CL2G2A3;
            PriceData.CL2G2FN = CL2G2FN;
            PriceData.CL2G3A1 = CL2G3A1;
            PriceData.CL2G3A2 = CL2G3A2;
            PriceData.CL2G3A3 = CL2G3A3;
            PriceData.CL2G3FN = CL2G3FN;

            // Level 3 Canadian prices
            PriceData.CL3G1A1 = CL3G1A1;
            PriceData.CL3G1A2 = CL3G1A2;
            PriceData.CL3G1A3 = CL3G1A3;
            PriceData.CL3G1FN = CL3G1FN;
            PriceData.CL3G2A1 = CL3G2A1;
            PriceData.CL3G2A2 = CL3G2A2;
            PriceData.CL3G2A3 = CL3G2A3;
            PriceData.CL3G2FN = CL3G2FN;
            PriceData.CL3G3A1 = CL3G3A1;
            PriceData.CL3G3A2 = CL3G3A2;
            PriceData.CL3G3A3 = CL3G3A3;
            PriceData.CL3G3FN = CL3G3FN;

            // Set US prices to 0 (will be removed eventually)
            PriceData.UL1G1A1 = PriceData.UL1G1A2 = PriceData.UL1G1A3 = PriceData.UL1G1FN = 0;
            PriceData.UL1G2A1 = PriceData.UL1G2A2 = PriceData.UL1G2A3 = PriceData.UL1G2FN = 0;
            PriceData.UL1G3A1 = PriceData.UL1G3A2 = PriceData.UL1G3A3 = PriceData.UL1G3FN = 0;
            PriceData.UL2G1A1 = PriceData.UL2G1A2 = PriceData.UL2G1A3 = PriceData.UL2G1FN = 0;
            PriceData.UL2G2A1 = PriceData.UL2G2A2 = PriceData.UL2G2A3 = PriceData.UL2G2FN = 0;
            PriceData.UL2G3A1 = PriceData.UL2G3A2 = PriceData.UL2G3A3 = PriceData.UL2G3FN = 0;
            PriceData.UL3G1A1 = PriceData.UL3G1A2 = PriceData.UL3G1A3 = PriceData.UL3G1FN = 0;
            PriceData.UL3G2A1 = PriceData.UL3G2A2 = PriceData.UL3G2A3 = PriceData.UL3G2FN = 0;
            PriceData.UL3G3A1 = PriceData.UL3G3A2 = PriceData.UL3G3A3 = PriceData.UL3G3FN = 0;

            HasUnsavedChanges = false; // Clear unsaved changes flag
            WasSaved = true;

            // Close the dialog with true indicating success/save
            DialogHost.CloseDialogCommand.Execute(true, null);
        }

        private bool CanSave(object parameter)
        {
            return !IsReadOnly && IsValid();
        }

        private bool IsValid()
        {
            // Check required fields
            if (SelectedProduct == null || SelectedProduct.ProductId == 0)
                return false;

            if (SelectedProcess == null || SelectedProcess.ProcessId == 0)
                return false;

            if (EffectiveDate == default)
                return false;

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
                return false;

            // Validate time premium if enabled
            if (TimePremiumEnabled)
            {
                if (string.IsNullOrWhiteSpace(PremiumTime))
                    return false;

                if (CanadianPremium < 0)
                    return false;
            }

            // Validate price progression (A2 >= A1, A3 >= A2, Final >= max(A1,A2,A3))
            // This is done per level/grade combination
            bool hasAnyError = ValidatePriceProgression();

            return !hasAnyError;
        }

        private bool ValidatePriceProgression()
        {
            bool hasAnyError = false;

            // Reset all error flags
            ResetAllErrorFlags();

            // Validate Level 1 Grade 1
            if (ValidateLevelGrade(1, 1, CL1G1A1, CL1G1A2, CL1G1A3, CL1G1FN,
                ref _hasL1G1A1Error, ref _hasL1G1A2Error, ref _hasL1G1A3Error, ref _hasL1G1FNError))
                hasAnyError = true;

            // Validate Level 1 Grade 2
            if (ValidateLevelGrade(1, 2, CL1G2A1, CL1G2A2, CL1G2A3, CL1G2FN,
                ref _hasL1G2A1Error, ref _hasL1G2A2Error, ref _hasL1G2A3Error, ref _hasL1G2FNError))
                hasAnyError = true;

            // Validate Level 1 Grade 3
            if (ValidateLevelGrade(1, 3, CL1G3A1, CL1G3A2, CL1G3A3, CL1G3FN,
                ref _hasL1G3A1Error, ref _hasL1G3A2Error, ref _hasL1G3A3Error, ref _hasL1G3FNError))
                hasAnyError = true;

            // Validate Level 2 Grade 1
            if (ValidateLevelGrade(2, 1, CL2G1A1, CL2G1A2, CL2G1A3, CL2G1FN,
                ref _hasL2G1A1Error, ref _hasL2G1A2Error, ref _hasL2G1A3Error, ref _hasL2G1FNError))
                hasAnyError = true;

            // Validate Level 2 Grade 2
            if (ValidateLevelGrade(2, 2, CL2G2A1, CL2G2A2, CL2G2A3, CL2G2FN,
                ref _hasL2G2A1Error, ref _hasL2G2A2Error, ref _hasL2G2A3Error, ref _hasL2G2FNError))
                hasAnyError = true;

            // Validate Level 2 Grade 3
            if (ValidateLevelGrade(2, 3, CL2G3A1, CL2G3A2, CL2G3A3, CL2G3FN,
                ref _hasL2G3A1Error, ref _hasL2G3A2Error, ref _hasL2G3A3Error, ref _hasL2G3FNError))
                hasAnyError = true;

            // Validate Level 3 Grade 1
            if (ValidateLevelGrade(3, 1, CL3G1A1, CL3G1A2, CL3G1A3, CL3G1FN,
                ref _hasL3G1A1Error, ref _hasL3G1A2Error, ref _hasL3G1A3Error, ref _hasL3G1FNError))
                hasAnyError = true;

            // Validate Level 3 Grade 2
            if (ValidateLevelGrade(3, 2, CL3G2A1, CL3G2A2, CL3G2A3, CL3G2FN,
                ref _hasL3G2A1Error, ref _hasL3G2A2Error, ref _hasL3G2A3Error, ref _hasL3G2FNError))
                hasAnyError = true;

            // Validate Level 3 Grade 3
            if (ValidateLevelGrade(3, 3, CL3G3A1, CL3G3A2, CL3G3A3, CL3G3FN,
                ref _hasL3G3A1Error, ref _hasL3G3A2Error, ref _hasL3G3A3Error, ref _hasL3G3FNError))
                hasAnyError = true;

            // Notify all error flag changes
            NotifyAllErrorFlags();

            return hasAnyError;
        }

        private bool ValidateLevelGrade(int level, int grade, decimal a1, decimal a2, decimal a3, decimal final,
            ref bool hasA1Error, ref bool hasA2Error, ref bool hasA3Error, ref bool hasFinalError)
        {
            hasA1Error = hasA2Error = hasA3Error = hasFinalError = false;

            // Skip validation if all values are 0 (not set)
            if (a1 == 0 && a2 == 0 && a3 == 0 && final == 0)
            {
                return false;
            }

            bool hasError = false;

            // A2 cannot be less than A1 (if both are provided)
            if (a1 > 0 && a2 > 0 && a2 < a1)
            {
                hasA2Error = true;
                hasError = true;
            }

            // A3 cannot be less than A2 (if both are provided)
            if (a2 > 0 && a3 > 0 && a3 < a2)
            {
                hasA3Error = true;
                hasError = true;
            }

            // Final price validation: Final >= max(A1, A2, A3)
            var maxAdvance = Math.Max(Math.Max(a1, a2), a3);

            if (maxAdvance > 0 && final < maxAdvance)
            {
                hasFinalError = true;
                hasError = true;
            }

            return hasError;
        }

        private void ResetAllErrorFlags()
        {
            _hasL1G1A1Error = _hasL1G1A2Error = _hasL1G1A3Error = _hasL1G1FNError = false;
            _hasL1G2A1Error = _hasL1G2A2Error = _hasL1G2A3Error = _hasL1G2FNError = false;
            _hasL1G3A1Error = _hasL1G3A2Error = _hasL1G3A3Error = _hasL1G3FNError = false;
            _hasL2G1A1Error = _hasL2G1A2Error = _hasL2G1A3Error = _hasL2G1FNError = false;
            _hasL2G2A1Error = _hasL2G2A2Error = _hasL2G2A3Error = _hasL2G2FNError = false;
            _hasL2G3A1Error = _hasL2G3A2Error = _hasL2G3A3Error = _hasL2G3FNError = false;
            _hasL3G1A1Error = _hasL3G1A2Error = _hasL3G1A3Error = _hasL3G1FNError = false;
            _hasL3G2A1Error = _hasL3G2A2Error = _hasL3G2A3Error = _hasL3G2FNError = false;
            _hasL3G3A1Error = _hasL3G3A2Error = _hasL3G3A3Error = _hasL3G3FNError = false;
        }

        private void NotifyAllErrorFlags()
        {
            // Level 1
            OnPropertyChanged(nameof(HasL1G1A1Error));
            OnPropertyChanged(nameof(HasL1G1A2Error));
            OnPropertyChanged(nameof(HasL1G1A3Error));
            OnPropertyChanged(nameof(HasL1G1FNError));
            OnPropertyChanged(nameof(HasL1G2A1Error));
            OnPropertyChanged(nameof(HasL1G2A2Error));
            OnPropertyChanged(nameof(HasL1G2A3Error));
            OnPropertyChanged(nameof(HasL1G2FNError));
            OnPropertyChanged(nameof(HasL1G3A1Error));
            OnPropertyChanged(nameof(HasL1G3A2Error));
            OnPropertyChanged(nameof(HasL1G3A3Error));
            OnPropertyChanged(nameof(HasL1G3FNError));

            // Level 2
            OnPropertyChanged(nameof(HasL2G1A1Error));
            OnPropertyChanged(nameof(HasL2G1A2Error));
            OnPropertyChanged(nameof(HasL2G1A3Error));
            OnPropertyChanged(nameof(HasL2G1FNError));
            OnPropertyChanged(nameof(HasL2G2A1Error));
            OnPropertyChanged(nameof(HasL2G2A2Error));
            OnPropertyChanged(nameof(HasL2G2A3Error));
            OnPropertyChanged(nameof(HasL2G2FNError));
            OnPropertyChanged(nameof(HasL2G3A1Error));
            OnPropertyChanged(nameof(HasL2G3A2Error));
            OnPropertyChanged(nameof(HasL2G3A3Error));
            OnPropertyChanged(nameof(HasL2G3FNError));

            // Level 3
            OnPropertyChanged(nameof(HasL3G1A1Error));
            OnPropertyChanged(nameof(HasL3G1A2Error));
            OnPropertyChanged(nameof(HasL3G1A3Error));
            OnPropertyChanged(nameof(HasL3G1FNError));
            OnPropertyChanged(nameof(HasL3G2A1Error));
            OnPropertyChanged(nameof(HasL3G2A2Error));
            OnPropertyChanged(nameof(HasL3G2A3Error));
            OnPropertyChanged(nameof(HasL3G2FNError));
            OnPropertyChanged(nameof(HasL3G3A1Error));
            OnPropertyChanged(nameof(HasL3G3A2Error));
            OnPropertyChanged(nameof(HasL3G3A3Error));
            OnPropertyChanged(nameof(HasL3G3FNError));
        }

        private void Cancel(object parameter)
        {
            if (HasUnsavedChanges)
            {
                // Show warning banner when trying to cancel with unsaved changes
                ShowWarningBanner = true;
                return;
            }

            // No unsaved changes, close immediately
            HasUnsavedChanges = false;
            WasSaved = false;
            DialogHost.CloseDialogCommand.Execute(false, null);
        }

        private void ConfirmDiscard(object parameter)
        {
            // User confirmed they want to discard changes
            HasUnsavedChanges = false;
            ShowWarningBanner = false;
            WasSaved = false;
            DialogHost.CloseDialogCommand.Execute(false, null);
        }

        private void CancelDiscard(object parameter)
        {
            // User decided not to discard changes, hide warning banner
            ShowWarningBanner = false;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Don't track changes to HasUnsavedChanges or ShowWarningBanner itself
            if (e.PropertyName != nameof(HasUnsavedChanges) && e.PropertyName != nameof(ShowWarningBanner))
            {
                CheckForUnsavedChanges();
                // Refresh Save button state when any property changes
                if (SaveCommand is RelayCommand relayCommand)
                {
                    relayCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private void CheckForUnsavedChanges()
        {
            if (_originalPrice == null)
            {
                // For new prices, check if any field has content
                HasUnsavedChanges = SelectedProduct != null ||
                                   SelectedProcess != null ||
                                   EffectiveDate != DateTime.Today ||
                                   CL1G1A1 != 0 || CL1G1A2 != 0 || CL1G1A3 != 0 || CL1G1FN != 0 ||
                                   CL1G2A1 != 0 || CL1G2A2 != 0 || CL1G2A3 != 0 || CL1G2FN != 0 ||
                                   CL1G3A1 != 0 || CL1G3A2 != 0 || CL1G3A3 != 0 || CL1G3FN != 0;
                return;
            }

            // For existing prices, compare with original values
            HasUnsavedChanges =
                SelectedProduct?.ProductId != _originalPrice.ProductId ||
                SelectedProcess?.ProcessId != _originalPrice.ProcessId ||
                EffectiveDate != _originalPrice.From ||
                TimePremiumEnabled != _originalPrice.TimePrem ||
                PremiumTime != _originalPrice.Time ||
                CanadianPremium != _originalPrice.CPremium ||
                CL1G1A1 != _originalPrice.CL1G1A1 || CL1G1A2 != _originalPrice.CL1G1A2 ||
                CL1G1A3 != _originalPrice.CL1G1A3 || CL1G1FN != _originalPrice.CL1G1FN ||
                CL1G2A1 != _originalPrice.CL1G2A1 || CL1G2A2 != _originalPrice.CL1G2A2 ||
                CL1G2A3 != _originalPrice.CL1G2A3 || CL1G2FN != _originalPrice.CL1G2FN ||
                CL1G3A1 != _originalPrice.CL1G3A1 || CL1G3A2 != _originalPrice.CL1G3A2 ||
                CL1G3A3 != _originalPrice.CL1G3A3 || CL1G3FN != _originalPrice.CL1G3FN ||
                CL2G1A1 != _originalPrice.CL2G1A1 || CL2G1A2 != _originalPrice.CL2G1A2 ||
                CL2G1A3 != _originalPrice.CL2G1A3 || CL2G1FN != _originalPrice.CL2G1FN ||
                CL2G2A1 != _originalPrice.CL2G2A1 || CL2G2A2 != _originalPrice.CL2G2A2 ||
                CL2G2A3 != _originalPrice.CL2G2A3 || CL2G2FN != _originalPrice.CL2G2FN ||
                CL2G3A1 != _originalPrice.CL2G3A1 || CL2G3A2 != _originalPrice.CL2G3A2 ||
                CL2G3A3 != _originalPrice.CL2G3A3 || CL2G3FN != _originalPrice.CL2G3FN ||
                CL3G1A1 != _originalPrice.CL3G1A1 || CL3G1A2 != _originalPrice.CL3G1A2 ||
                CL3G1A3 != _originalPrice.CL3G1A3 || CL3G1FN != _originalPrice.CL3G1FN ||
                CL3G2A1 != _originalPrice.CL3G2A1 || CL3G2A2 != _originalPrice.CL3G2A2 ||
                CL3G2A3 != _originalPrice.CL3G2A3 || CL3G2FN != _originalPrice.CL3G2FN ||
                CL3G3A1 != _originalPrice.CL3G3A1 || CL3G3A2 != _originalPrice.CL3G3A2 ||
                CL3G3A3 != _originalPrice.CL3G3A3 || CL3G3FN != _originalPrice.CL3G3FN;
        }

        public bool HandleDialogClosing()
        {
            // Prevent closing if there are unsaved changes
            if (HasUnsavedChanges)
            {
                return false; // Prevent closing
            }

            HasUnsavedChanges = false;
            return true; // Allow closing
        }

        // IDataErrorInfo Implementation
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                // For simplicity, we're relying on the ValidatePriceProgression method
                // This could be expanded to provide field-specific error messages if needed
                return null;
            }
        }
    }
}

