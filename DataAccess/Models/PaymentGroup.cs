using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment group for organizing growers.
    /// </summary>
    public class PaymentGroup : INotifyPropertyChanged
    {
        private int _paymentGroupId;
        private string _groupCode = string.Empty;
        private string _groupName = string.Empty;
        private string _description = string.Empty;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.Now;
        private string _createdBy = string.Empty;
        private DateTime? _modifiedAt;
        private string _modifiedBy = string.Empty;

        public int PaymentGroupId
        {
            get => _paymentGroupId;
            set
            {
                if (_paymentGroupId != value)
                {
                    _paymentGroupId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GroupCode
        {
            get => _groupCode;
            set
            {
                if (_groupCode != value)
                {
                    _groupCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GroupName
        {
            get => _groupName;
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
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

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
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

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                if (_createdBy != value)
                {
                    _createdBy = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    _modifiedBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"{GroupCode} - {GroupName}";
        }
    }
}
