using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Cheque : INotifyPropertyChanged
    {
        private string _series;
        private decimal _chequeNumber;
        private decimal _growerNumber;
        private DateTime _date;
        private decimal _amount;
        private decimal _year;
        private string _chequeType;
        private bool _void;
        private DateTime? _dateClear;
        private bool _isCleared;
        private string _currency;
        private DateTime? _qaddDate;
        private string _qaddTime;
        private string _qaddOp;
        private DateTime? _qedDate;
        private string _qedTime;
        private string _qedOp;
        private DateTime? _qdelDate;
        private string _qdelTime;
        private string _qdelOp;

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

        public decimal ChequeNumber
        {
            get => _chequeNumber;
            set
            {
                if (_chequeNumber != value)
                {
                    _chequeNumber = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public decimal Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    _amount = value;
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

        public string ChequeType
        {
            get => _chequeType;
            set
            {
                if (_chequeType != value)
                {
                    _chequeType = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Void
        {
            get => _void;
            set
            {
                if (_void != value)
                {
                    _void = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? DateClear
        {
            get => _dateClear;
            set
            {
                if (_dateClear != value)
                {
                    _dateClear = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCleared
        {
            get => _isCleared;
            set
            {
                if (_isCleared != value)
                {
                    _isCleared = value;
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

        public DateTime? QaddDate
        {
            get => _qaddDate;
            set
            {
                if (_qaddDate != value)
                {
                    _qaddDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string QaddTime
        {
            get => _qaddTime;
            set
            {
                if (_qaddTime != value)
                {
                    _qaddTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string QaddOp
        {
            get => _qaddOp;
            set
            {
                if (_qaddOp != value)
                {
                    _qaddOp = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? QedDate
        {
            get => _qedDate;
            set
            {
                if (_qedDate != value)
                {
                    _qedDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string QedTime
        {
            get => _qedTime;
            set
            {
                if (_qedTime != value)
                {
                    _qedTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string QedOp
        {
            get => _qedOp;
            set
            {
                if (_qedOp != value)
                {
                    _qedOp = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? QdelDate
        {
            get => _qdelDate;
            set
            {
                if (_qdelDate != value)
                {
                    _qdelDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string QdelTime
        {
            get => _qdelTime;
            set
            {
                if (_qdelTime != value)
                {
                    _qdelTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string QdelOp
        {
            get => _qdelOp;
            set
            {
                if (_qdelOp != value)
                {
                    _qdelOp = value;
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