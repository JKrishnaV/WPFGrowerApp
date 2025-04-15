using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Grower : INotifyPropertyChanged
    {
        private decimal _growerNumber;
        private string _chequeName;
        private string _growerName;
        private string _address;
        private string _city;
        private string _prov;
        private string _postal;
        private string _phone;
        private decimal _acres;
        private string _notes = string.Empty;
        private string _contract;
        private char _currency = 'C';
        private int _contractLimit;
        private string _payGroup = "1";
        private bool _onHold;
        private string _phone2;
        private string _phoneAdditional1;
        private string _otherNames;
        private string _phoneAdditional2;
        private int _lyFresh;
        private int _lyOther;
        private string _certified;
        private bool _chargeGST;
        private int _priceLevel = 1;

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

        public string Prov
        {
            get => _prov;
            set
            {
                if (_prov != value)
                {
                    _prov = value;
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
            get => _notes ?? string.Empty;
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

        public string PayGroup
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
        public string Phone2
        {
            get => _phone2;
            set
            {
                if (_phone2 != value)
                {
                    _phone2 = value;
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

        public int PriceLevel
        {
            get => _priceLevel;
            set
            {
                if (_priceLevel != value)
                {
                    _priceLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
