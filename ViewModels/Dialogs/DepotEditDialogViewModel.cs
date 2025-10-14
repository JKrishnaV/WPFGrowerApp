using System;
using System.ComponentModel;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using MaterialDesignThemes.Wpf;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    public class DepotEditDialogViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly Depot _originalDepot;
        private readonly IDialogService _dialogService;
        
        private string _title = string.Empty;
        private string _depotCode = string.Empty;
        private string _depotName = string.Empty;
        private string _address = string.Empty;
        private string _city = string.Empty;
        private string _province = string.Empty;
        private string _postalCode = string.Empty;
        private string _phoneNumber = string.Empty;
        private int? _displayOrder;
        private bool _isActive = true;
        private bool _isEditMode;
        private bool _isReadOnly;
        private bool _hasUnsavedChanges;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string DepotCode
        {
            get => _depotCode;
            set
            {
                if (SetProperty(ref _depotCode, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string DepotName
        {
            get => _depotName;
            set
            {
                if (SetProperty(ref _depotName, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (SetProperty(ref _city, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string Province
        {
            get => _province;
            set
            {
                if (SetProperty(ref _province, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string PostalCode
        {
            get => _postalCode;
            set
            {
                if (SetProperty(ref _postalCode, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public int? DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (SetProperty(ref _displayOrder, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public string StatusText => IsActive ? "Active" : "Inactive";

        public Depot DepotData { get; set; } = new Depot();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public DepotEditDialogViewModel(Depot originalDepot, bool isReadOnly, IDialogService dialogService)
        {
            _originalDepot = originalDepot ?? throw new ArgumentNullException(nameof(originalDepot));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Initialize properties
            _isEditMode = originalDepot.DepotId > 0;
            _isReadOnly = isReadOnly;
            _title = isReadOnly ? "View Depot" : (_isEditMode ? "Edit Depot" : "Add Depot");

            // Set form values
            _depotCode = originalDepot.DepotCode ?? string.Empty;
            _depotName = originalDepot.DepotName ?? string.Empty;
            _address = originalDepot.Address ?? string.Empty;
            _city = originalDepot.City ?? string.Empty;
            _province = originalDepot.Province ?? string.Empty;
            _postalCode = originalDepot.PostalCode ?? string.Empty;
            _phoneNumber = originalDepot.PhoneNumber ?? string.Empty;
            _displayOrder = originalDepot.DisplayOrder;
            _isActive = originalDepot.IsActive;

            // Initialize commands
            SaveCommand = new RelayCommand((param) => Save(), (param) => CanSave());
            CancelCommand = new RelayCommand((param) => Cancel());
        }

        private bool CanSave()
        {
            return !IsReadOnly && !string.IsNullOrWhiteSpace(DepotName) && !string.IsNullOrWhiteSpace(DepotCode);
        }

        private void Save()
        {
            if (IsReadOnly) return;

            // Update the original depot object
            _originalDepot.DepotCode = DepotCode;
            _originalDepot.DepotName = DepotName;
            _originalDepot.Address = Address;
            _originalDepot.City = City;
            _originalDepot.Province = Province;
            _originalDepot.PostalCode = PostalCode;
            _originalDepot.PhoneNumber = PhoneNumber;
            _originalDepot.DisplayOrder = DisplayOrder;
            _originalDepot.IsActive = IsActive;

            // Copy to DepotData for the parent view
            DepotData.DepotId = _originalDepot.DepotId;
            DepotData.DepotCode = DepotCode;
            DepotData.DepotName = DepotName;
            DepotData.Address = Address;
            DepotData.City = City;
            DepotData.Province = Province;
            DepotData.PostalCode = PostalCode;
            DepotData.PhoneNumber = PhoneNumber;
            DepotData.DisplayOrder = DisplayOrder;
            DepotData.IsActive = IsActive;

            DialogHost.Close("RootDialogHost", true);
        }

        private void Cancel()
        {
            DialogHost.Close("RootDialogHost", false);
        }

        #region IDataErrorInfo Implementation

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(DepotCode):
                        if (string.IsNullOrWhiteSpace(DepotCode))
                            return "Depot Code is required.";
                        if (DepotCode.Length > 10)
                            return "Depot Code cannot exceed 10 characters.";
                        break;

                    case nameof(DepotName):
                        if (string.IsNullOrWhiteSpace(DepotName))
                            return "Depot Name is required.";
                        if (DepotName.Length > 100)
                            return "Depot Name cannot exceed 100 characters.";
                        break;

                    case nameof(Address):
                        if (!string.IsNullOrWhiteSpace(Address) && Address.Length > 200)
                            return "Address cannot exceed 200 characters.";
                        break;

                    case nameof(City):
                        if (!string.IsNullOrWhiteSpace(City) && City.Length > 50)
                            return "City cannot exceed 50 characters.";
                        break;

                    case nameof(Province):
                        if (!string.IsNullOrWhiteSpace(Province) && Province.Length > 2)
                            return "Province cannot exceed 2 characters.";
                        break;

                    case nameof(PostalCode):
                        if (!string.IsNullOrWhiteSpace(PostalCode) && PostalCode.Length > 10)
                            return "Postal Code cannot exceed 10 characters.";
                        break;

                    case nameof(PhoneNumber):
                        if (!string.IsNullOrWhiteSpace(PhoneNumber) && PhoneNumber.Length > 20)
                            return "Phone Number cannot exceed 20 characters.";
                        break;
                }

                return string.Empty;
            }
        }

        #endregion
    }
}