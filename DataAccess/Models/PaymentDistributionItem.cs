using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class PaymentDistributionItem : INotifyPropertyChanged
    {
        private int _itemId;
        private int _distributionId;
        private int _growerId;
        private string _growerName;
        private string _growerNumber;
        private int? _paymentBatchId;
        private int? _receiptId;
        private string _batchNumber;
        private decimal _amount;
        private string _paymentMethod; // Cheque, Electronic
        private string _status; // Pending, Generated, Processed, Voided
        private DateTime _createdAt;
        private string _createdBy;
        private DateTime? _processedAt;
        private string _processedBy;

        public int ItemId
        {
            get => _itemId;
            set => SetProperty(ref _itemId, value);
        }

        public int DistributionId
        {
            get => _distributionId;
            set => SetProperty(ref _distributionId, value);
        }

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public string GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public int? PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public int? ReceiptId
        {
            get => _receiptId;
            set => SetProperty(ref _receiptId, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
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

        public DateTime? ProcessedAt
        {
            get => _processedAt;
            set => SetProperty(ref _processedAt, value);
        }

        public string ProcessedBy
        {
            get => _processedBy;
            set => SetProperty(ref _processedBy, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
