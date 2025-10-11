using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment method (Cheque, EFT, Wire Transfer, etc.)
    /// </summary>
    public class PaymentMethod : INotifyPropertyChanged
    {
        private int _paymentMethodId;
        private string _methodCode;
        private string _methodName;
        private string _description;
        private bool _requiresBankInfo;
        private bool _allowForAdvances;
        private bool _allowForFinal;
        private bool _isActive;
        private int _displayOrder;
        private DateTime _createdAt;
        private string _createdBy;
        private DateTime? _modifiedAt;
        private string _modifiedBy;

        public int PaymentMethodId
        {
            get => _paymentMethodId;
            set => SetProperty(ref _paymentMethodId, value);
        }

        public string MethodCode
        {
            get => _methodCode;
            set => SetProperty(ref _methodCode, value);
        }

        public string MethodName
        {
            get => _methodName;
            set => SetProperty(ref _methodName, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool RequiresBankInfo
        {
            get => _requiresBankInfo;
            set => SetProperty(ref _requiresBankInfo, value);
        }

        public bool AllowForAdvances
        {
            get => _allowForAdvances;
            set => SetProperty(ref _allowForAdvances, value);
        }

        public bool AllowForFinal
        {
            get => _allowForFinal;
            set => SetProperty(ref _allowForFinal, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public int DisplayOrder
        {
            get => _displayOrder;
            set => SetProperty(ref _displayOrder, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set => SetProperty(ref _modifiedAt, value);
        }

        public string ModifiedBy
        {
            get => _modifiedBy;
            set => SetProperty(ref _modifiedBy, value);
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

