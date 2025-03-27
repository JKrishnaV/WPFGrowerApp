using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class BankRec : INotifyPropertyChanged
    {
        private DateTime _acctDate;
        private DateTime? _dateDone;
        private string _note;
        private decimal _amount;
        private DateTime? _qaddDate;
        private string _qaddTime;
        private string _qaddOp;
        private DateTime? _qedDate;
        private string _qedTime;
        private string _qedOp;
        private DateTime? _qdelDate;
        private string _qdelTime;
        private string _qdelOp;

        public DateTime AcctDate
        {
            get => _acctDate;
            set
            {
                if (_acctDate != value)
                {
                    _acctDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? DateDone
        {
            get => _dateDone;
            set
            {
                if (_dateDone != value)
                {
                    _dateDone = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Note
        {
            get => _note;
            set
            {
                if (_note != value)
                {
                    _note = value;
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