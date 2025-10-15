using System;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    public class ProductBreakdown : INotifyPropertyChanged
    {
        private int _productId;
        private string? _productName;
        private decimal _totalWeight;
        private decimal _totalValue;
        private int _receiptCount;
        private decimal _averageWeight;
        private decimal _averageValue;

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string? ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set => SetProperty(ref _totalWeight, value);
        }

        public decimal TotalValue
        {
            get => _totalValue;
            set => SetProperty(ref _totalValue, value);
        }

        public int ReceiptCount
        {
            get => _receiptCount;
            set => SetProperty(ref _receiptCount, value);
        }

        public decimal AverageWeight
        {
            get => _averageWeight;
            set => SetProperty(ref _averageWeight, value);
        }

        public decimal AverageValue
        {
            get => _averageValue;
            set => SetProperty(ref _averageValue, value);
        }

        public decimal Percentage { get; set; }
        public decimal Amount { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
