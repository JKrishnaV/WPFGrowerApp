using System;
using System.ComponentModel;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    public class ProductEditDialogViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly Product _originalProduct;
        private readonly IDialogService _dialogService;
        private readonly bool _isEditMode;
        private readonly bool _isReadOnly;

        private string _title;
        private string _productCode;
        private string _description;
        private string _shortDescription;
        private int? _category;
        private string _variety;
        private decimal _deduct;
        private bool _chargeGst;
        private bool _hasUnsavedChanges;

        public Product ProductData { get; private set; }

        public bool IsEditMode => _isEditMode;
        public bool IsReadOnly => _isReadOnly;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string ProductCode
        {
            get => _productCode;
            set
            {
                if (SetProperty(ref _productCode, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public string ShortDescription
        {
            get => _shortDescription;
            set
            {
                if (SetProperty(ref _shortDescription, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public int? Category
        {
            get => _category;
            set
            {
                if (SetProperty(ref _category, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string Variety
        {
            get => _variety;
            set
            {
                if (SetProperty(ref _variety, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public decimal Deduct
        {
            get => _deduct;
            set
            {
                if (SetProperty(ref _deduct, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public bool ChargeGst
        {
            get => _chargeGst;
            set
            {
                if (SetProperty(ref _chargeGst, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ProductEditDialogViewModel(Product product, bool isReadOnly, IDialogService dialogService)
        {
            _originalProduct = product ?? throw new ArgumentNullException(nameof(product));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _isEditMode = product.ProductId > 0;
            _isReadOnly = isReadOnly;

            // Set title based on mode
            Title = isReadOnly ? "View Product" : (_isEditMode ? "Edit Product" : "Add New Product");

            // Initialize properties from product
            _productCode = product.ProductCode ?? string.Empty;
            _description = product.Description ?? string.Empty;
            _shortDescription = product.ShortDescription ?? string.Empty;
            _category = product.Category;
            _variety = product.Variety ?? string.Empty;
            _deduct = product.Deduct;
            _chargeGst = product.ChargeGst;

            _hasUnsavedChanges = false;

            // Initialize commands
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel, param => true);
        }

        private bool CanSave(object parameter)
        {
            // Can save if not in read-only mode and there are no validation errors
            return !IsReadOnly && string.IsNullOrEmpty(Error);
        }

        private void Save(object parameter)
        {
            if (!CanSave(parameter))
                return;

            // Update the product data
            ProductData = new Product
            {
                ProductId = _originalProduct.ProductId,
                ProductCode = ProductCode?.Trim(),
                Description = Description?.Trim(),
                ShortDescription = ShortDescription?.Trim(),
                Category = Category,
                Variety = Variety?.Trim(),
                Deduct = Deduct,
                ChargeGst = ChargeGst
            };

            // Close dialog with success result
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(true, null);
        }

        private void Cancel(object parameter)
        {
            if (_hasUnsavedChanges && !IsReadOnly)
            {
                // Could add confirmation dialog here if needed
            }

            // Close dialog with cancel result
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(false, null);
        }

        #region IDataErrorInfo Implementation

        public string Error
        {
            get
            {
                // Return first error found
                if (!string.IsNullOrWhiteSpace(this[nameof(ProductCode)]))
                    return this[nameof(ProductCode)];
                if (!string.IsNullOrWhiteSpace(this[nameof(Description)]))
                    return this[nameof(Description)];
                if (!string.IsNullOrWhiteSpace(this[nameof(ShortDescription)]))
                    return this[nameof(ShortDescription)];
                if (!string.IsNullOrWhiteSpace(this[nameof(Variety)]))
                    return this[nameof(Variety)];
                if (!string.IsNullOrWhiteSpace(this[nameof(Deduct)]))
                    return this[nameof(Deduct)];

                return string.Empty;
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;

                switch (columnName)
                {
                    case nameof(ProductCode):
                        if (string.IsNullOrWhiteSpace(ProductCode))
                            error = "Product Code is required.";
                        else if (ProductCode.Length > 2)
                            error = "Product Code must be 2 characters or less.";
                        break;

                    case nameof(Description):
                        if (string.IsNullOrWhiteSpace(Description))
                            error = "Description is required.";
                        break;

                    case nameof(ShortDescription):
                        if (!string.IsNullOrWhiteSpace(ShortDescription) && ShortDescription.Length > 4)
                            error = "Short Description must be 4 characters or less.";
                        break;

                    case nameof(Variety):
                        if (!string.IsNullOrWhiteSpace(Variety) && Variety.Length > 8)
                            error = "Variety must be 8 characters or less.";
                        break;

                    case nameof(Deduct):
                        if (Deduct < 0)
                            error = "Deduct amount cannot be negative.";
                        break;
                }

                return error;
            }
        }

        #endregion
    }
}

