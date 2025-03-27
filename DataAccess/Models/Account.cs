using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Account : INotifyPropertyChanged
    {
        private decimal _number;
        private DateTime _date;
        private string _type;
        private string _class;
        private string _product;
        private string _process;
        private decimal _grade;
        private decimal _lbs;
        private decimal _unitPrice;
        private decimal _dollars;
        private string _description;
        private string _series;
        private decimal _cheque;
        private string _tSeries;
        private decimal _tCheque;
        private decimal _year;
        private decimal _acctUnique;
        private string _currency;
        private bool _chgGst;
        private decimal _gstRate;
        private decimal _gstEst;
        private decimal _nonGstEst;
        private decimal _advNo;
        private decimal _advBat;
        private decimal _finBat;

        public decimal Number
        {
            get => _number;
            set
            {
                if (_number != value)
                {
                    _number = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                if (_date != value)
                {
                    _date = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Class
        {
            get => _class;
            set
            {
                if (_class != value)
                {
                    _class = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Product
        {
            get => _product;
            set
            {
                if (_product != value)
                {
                    _product = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Process
        {
            get => _process;
            set
            {
                if (_process != value)
                {
                    _process = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Grade
        {
            get => _grade;
            set
            {
                if (_grade != value)
                {
                    _grade = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Lbs
        {
            get => _lbs;
            set
            {
                if (_lbs != value)
                {
                    _lbs = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice != value)
                {
                    _unitPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Dollars
        {
            get => _dollars;
            set
            {
                if (_dollars != value)
                {
                    _dollars = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Series
        {
            get => _series;
            set
            {
                if (_series != value)
                {
                    _series = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Cheque
        {
            get => _cheque;
            set
            {
                if (_cheque != value)
                {
                    _cheque = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TSeries
        {
            get => _tSeries;
            set
            {
                if (_tSeries != value)
                {
                    _tSeries = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TCheque
        {
            get => _tCheque;
            set
            {
                if (_tCheque != value)
                {
                    _tCheque = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Year
        {
            get => _year;
            set
            {
                if (_year != value)
                {
                    _year = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AcctUnique
        {
            get => _acctUnique;
            set
            {
                if (_acctUnique != value)
                {
                    _acctUnique = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Currency
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

        public bool ChgGst
        {
            get => _chgGst;
            set
            {
                if (_chgGst != value)
                {
                    _chgGst = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal GstRate
        {
            get => _gstRate;
            set
            {
                if (_gstRate != value)
                {
                    _gstRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal GstEst
        {
            get => _gstEst;
            set
            {
                if (_gstEst != value)
                {
                    _gstEst = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal NonGstEst
        {
            get => _nonGstEst;
            set
            {
                if (_nonGstEst != value)
                {
                    _nonGstEst = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AdvNo
        {
            get => _advNo;
            set
            {
                if (_advNo != value)
                {
                    _advNo = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AdvBat
        {
            get => _advBat;
            set
            {
                if (_advBat != value)
                {
                    _advBat = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal FinBat
        {
            get => _finBat;
            set
            {
                if (_finBat != value)
                {
                    _finBat = value;
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