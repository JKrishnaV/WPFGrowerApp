using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Summary model for grower-level payment information in a batch
    /// </summary>
    public class GrowerPaymentSummary : INotifyPropertyChanged
    {
        private int _growerId;
        private string _growerNumber;
        private string _growerName;
        private int _receiptCount;
        private decimal _totalWeight;
        private decimal _totalAmount;
        private string _chequeNumber;
        private bool _isOnHold;
        private int? _paymentMethodId;
        private string _paymentMethodName;

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public string GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public int ReceiptCount
        {
            get => _receiptCount;
            set => SetProperty(ref _receiptCount, value);
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set => SetProperty(ref _totalWeight, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public string ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        public bool IsOnHold
        {
            get => _isOnHold;
            set => SetProperty(ref _isOnHold, value);
        }

        public int? PaymentMethodId
        {
            get => _paymentMethodId;
            set => SetProperty(ref _paymentMethodId, value);
        }

        public string PaymentMethodName
        {
            get => _paymentMethodName;
            set => SetProperty(ref _paymentMethodName, value);
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

