using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment record for grower statistics and history.
    /// </summary>
    public class Payment : INotifyPropertyChanged
    {
        private int _paymentId;
        private int _paymentBatchId;
        private int _growerId;
        private decimal _amount;
        private DateTime _paymentDate;
        private int _paymentTypeId;
        private string _status = string.Empty;
        private DateTime _createdAt;

        public int PaymentId
        {
            get => _paymentId;
            set
            {
                if (_paymentId != value)
                {
                    _paymentId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set
            {
                if (_paymentBatchId != value)
                {
                    _paymentBatchId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GrowerId
        {
            get => _growerId;
            set
            {
                if (_growerId != value)
                {
                    _growerId = value;
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

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set
            {
                if (_paymentDate != value)
                {
                    _paymentDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PaymentTypeId
        {
            get => _paymentTypeId;
            set
            {
                if (_paymentTypeId != value)
                {
                    _paymentTypeId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
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
