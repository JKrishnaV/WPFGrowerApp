using System;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    public class PriceScheduleLock : INotifyPropertyChanged
    {
        private int _priceScheduleLockId;
        private int _priceScheduleId;
        private int _paymentTypeId;
        private int _paymentBatchId;
        private DateTime _lockedAt;
        private string _lockedBy = string.Empty;
        private DateTime _createdAt;
        private string _createdBy = "SYSTEM";
        private DateTime? _modifiedAt;
        private string _modifiedBy = string.Empty;
        private DateTime? _deletedAt;
        private string _deletedBy = string.Empty;

        public int PriceScheduleLockId
        {
            get => _priceScheduleLockId;
            set
            {
                if (_priceScheduleLockId != value)
                {
                    _priceScheduleLockId = value;
                    OnPropertyChanged(nameof(PriceScheduleLockId));
                }
            }
        }

        public int PriceScheduleId
        {
            get => _priceScheduleId;
            set
            {
                if (_priceScheduleId != value)
                {
                    _priceScheduleId = value;
                    OnPropertyChanged(nameof(PriceScheduleId));
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
                    OnPropertyChanged(nameof(PaymentTypeId));
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
                    OnPropertyChanged(nameof(PaymentBatchId));
                }
            }
        }

        public DateTime LockedAt
        {
            get => _lockedAt;
            set
            {
                if (_lockedAt != value)
                {
                    _lockedAt = value;
                    OnPropertyChanged(nameof(LockedAt));
                }
            }
        }

        public string LockedBy
        {
            get => _lockedBy;
            set
            {
                if (_lockedBy != value)
                {
                    _lockedBy = value ?? string.Empty;
                    OnPropertyChanged(nameof(LockedBy));
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
                    OnPropertyChanged(nameof(CreatedAt));
                }
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                if (_createdBy != value)
                {
                    _createdBy = value ?? "SYSTEM";
                    OnPropertyChanged(nameof(CreatedBy));
                }
            }
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set
            {
                if (_modifiedAt != value)
                {
                    _modifiedAt = value;
                    OnPropertyChanged(nameof(ModifiedAt));
                }
            }
        }

        public string ModifiedBy
        {
            get => _modifiedBy;
            set
            {
                if (_modifiedBy != value)
                {
                    _modifiedBy = value ?? string.Empty;
                    OnPropertyChanged(nameof(ModifiedBy));
                }
            }
        }

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set
            {
                if (_deletedAt != value)
                {
                    _deletedAt = value;
                    OnPropertyChanged(nameof(DeletedAt));
                }
            }
        }

        public string DeletedBy
        {
            get => _deletedBy;
            set
            {
                if (_deletedBy != value)
                {
                    _deletedBy = value ?? string.Empty;
                    OnPropertyChanged(nameof(DeletedBy));
                }
            }
        }

        // Navigation properties (optional, for future use)
        public PriceSchedule? PriceSchedule { get; set; }
        public PaymentType? PaymentType { get; set; }
        public PaymentBatch? PaymentBatch { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
