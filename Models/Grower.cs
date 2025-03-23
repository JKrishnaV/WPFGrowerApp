using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace WPFGrowerApp.Models
{
    public class Grower : INotifyPropertyChanged, IDataErrorInfo
    {
        private decimal _growerNumber;
        private string _chequeName;
        private string _growerName;
        private string _address;
        private string _city;
        private string _postal;
        private string _phone;
        private decimal _acres;
        private string _notes;
        private string _contract;
        private char _currency = 'C';
        private int _contractLimit;
        private int _payGroup = 1;
        private bool _onHold;
        private string _phoneAdditional1;
        private string _otherNames;
        private string _phoneAdditional2;
        private int _lyFresh;
        private int _lyOther;
        private string _certified;
        private bool _chargeGST;
        
        // Dictionary to store validation errors
        private Dictionary<string, string> _validationErrors = new Dictionary<string, string>();

        public decimal GrowerNumber
        {
            get => _growerNumber;
            set
            {
                if (_growerNumber != value)
                {
                    _growerNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ChequeName
        {
            get => _chequeName;
            set
            {
                if (_chequeName != value)
                {
                    _chequeName = value;
                    OnPropertyChanged();
                    ValidateChequeName();
                }
            }
        }

        public string GrowerName
        {
            get => _growerName;
            set
            {
                if (_growerName != value)
                {
                    _growerName = value;
                    OnPropertyChanged();
                    ValidateGrowerName();
                }
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Postal
        {
            get => _postal;
            set
            {
                if (_postal != value)
                {
                    _postal = value;
                    OnPropertyChanged();
                    ValidatePostalCode();
                }
            }
        }

        public string Phone
        {
            get => _phone;
            set
            {
                if (_phone != value)
                {
                    _phone = value;
                    OnPropertyChanged();
                    ValidatePhoneNumber();
                }
            }
        }

        public decimal Acres
        {
            get => _acres;
            set
            {
                if (_acres != value)
                {
                    _acres = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Contract
        {
            get => _contract;
            set
            {
                if (_contract != value)
                {
                    _contract = value;
                    OnPropertyChanged();
                }
            }
        }

        public char Currency
        {
            get => _currency;
            set
            {
                if (_currency != value)
                {
                    _currency = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ContractLimit
        {
            get => _contractLimit;
            set
            {
                if (_contractLimit != value)
                {
                    _contractLimit = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PayGroup
        {
            get => _payGroup;
            set
            {
                if (_payGroup != value)
                {
                    _payGroup = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool OnHold
        {
            get => _onHold;
            set
            {
                if (_onHold != value)
                {
                    _onHold = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PhoneAdditional1
        {
            get => _phoneAdditional1;
            set
            {
                if (_phoneAdditional1 != value)
                {
                    _phoneAdditional1 = value;
                    OnPropertyChanged();
                }
            }
        }

        public string OtherNames
        {
            get => _otherNames;
            set
            {
                if (_otherNames != value)
                {
                    _otherNames = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PhoneAdditional2
        {
            get => _phoneAdditional2;
            set
            {
                if (_phoneAdditional2 != value)
                {
                    _phoneAdditional2 = value;
                    OnPropertyChanged();
                }
            }
        }

        public int LYFresh
        {
            get => _lyFresh;
            set
            {
                if (_lyFresh != value)
                {
                    _lyFresh = value;
                    OnPropertyChanged();
                }
            }
        }

        public int LYOther
        {
            get => _lyOther;
            set
            {
                if (_lyOther != value)
                {
                    _lyOther = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Certified
        {
            get => _certified;
            set
            {
                if (_certified != value)
                {
                    _certified = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ChargeGST
        {
            get => _chargeGST;
            set
            {
                if (_chargeGST != value)
                {
                    _chargeGST = value;
                    OnPropertyChanged();
                }
            }
        }

        // Validation methods
        private void ValidateGrowerName()
        {
            if (string.IsNullOrWhiteSpace(GrowerName))
            {
                _validationErrors["GrowerName"] = "Grower Name is required.";
            }
            else if (GrowerName.Length < 3)
            {
                _validationErrors["GrowerName"] = "Grower Name must be at least 3 characters.";
            }
            else
            {
                _validationErrors.Remove("GrowerName");
            }
            
            OnPropertyChanged(nameof(Error));
        }

        private void ValidateChequeName()
        {
            if (string.IsNullOrWhiteSpace(ChequeName))
            {
                _validationErrors["ChequeName"] = "Cheque Name is required.";
            }
            else if (ChequeName.Length < 3)
            {
                _validationErrors["ChequeName"] = "Cheque Name must be at least 3 characters.";
            }
            else
            {
                _validationErrors.Remove("ChequeName");
            }
            
            OnPropertyChanged(nameof(Error));
        }

        private void ValidatePostalCode()
        {
            if (!string.IsNullOrWhiteSpace(Postal))
            {
                // Canadian postal code format: A1A 1A1
                var canadianPattern = @"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$";
                
                // US ZIP code format: 12345 or 12345-6789
                var usPattern = @"^\d{5}(-\d{4})?$";
                
                if (!Regex.IsMatch(Postal, canadianPattern) && !Regex.IsMatch(Postal, usPattern))
                {
                    _validationErrors["Postal"] = "Invalid postal code format. Use Canadian (A1A 1A1) or US (12345 or 12345-6789) format.";
                }
                else
                {
                    _validationErrors.Remove("Postal");
                }
            }
            else
            {
                _validationErrors.Remove("Postal");
            }
            
            OnPropertyChanged(nameof(Error));
        }

        private void ValidatePhoneNumber()
        {
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                // Remove any non-digit characters for validation
                var digitsOnly = new string(Phone.Where(char.IsDigit).ToArray());
                
                if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
                {
                    _validationErrors["Phone"] = "Phone number must have between 10 and 15 digits.";
                }
                else
                {
                    _validationErrors.Remove("Phone");
                }
            }
            else
            {
                _validationErrors.Remove("Phone");
            }
            
            OnPropertyChanged(nameof(Error));
        }

        // IDataErrorInfo implementation
        public string Error => string.Join(Environment.NewLine, _validationErrors.Values);

        public string this[string columnName]
        {
            get
            {
                _validationErrors.TryGetValue(columnName, out string error);
                return error;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
