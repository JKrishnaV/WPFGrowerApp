using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class PaymentDistribution : INotifyPropertyChanged
    {
        private int _distributionId;
        private string _distributionNumber;
        private DateTime _distributionDate;
        private string _distributionType; // ByGrower, ByBatch, AllPending
        private string _paymentMethod; // Cheque, Electronic, Both
        private decimal _totalAmount;
        private int _totalGrowers;
        private int _totalBatches;
        private string _status; // Draft, Generated, Processed, Voided
        private DateTime _createdAt;
        private string _createdBy;
        private DateTime? _processedAt;
        private string _processedBy;
        private List<PaymentDistributionItem> _items = new();

        public int DistributionId
        {
            get => _distributionId;
            set => SetProperty(ref _distributionId, value);
        }

        public string DistributionNumber
        {
            get => _distributionNumber;
            set => SetProperty(ref _distributionNumber, value);
        }

        public DateTime DistributionDate
        {
            get => _distributionDate;
            set => SetProperty(ref _distributionDate, value);
        }

        public string DistributionType
        {
            get => _distributionType;
            set => SetProperty(ref _distributionType, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public int TotalGrowers
        {
            get => _totalGrowers;
            set => SetProperty(ref _totalGrowers, value);
        }

        public int TotalBatches
        {
            get => _totalBatches;
            set => SetProperty(ref _totalBatches, value);
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

        public List<PaymentDistributionItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
