using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Represents details about a receipt that was skipped during payment calculation
    /// </summary>
    public class SkippedReceiptDetail : INotifyPropertyChanged
    {
        private decimal _receiptNumber;
        private DateTime _receiptDate;
        private string _growerNumber = string.Empty;
        private string _growerName = string.Empty;
        private string _product = string.Empty;
        private string _process = string.Empty;
        private decimal _netWeight;
        private string _errorMessage = string.Empty;
        private string _reason = string.Empty;

        public decimal ReceiptNumber
        {
            get => _receiptNumber;
            set => SetProperty(ref _receiptNumber, value);
        }

        public DateTime ReceiptDate
        {
            get => _receiptDate;
            set => SetProperty(ref _receiptDate, value);
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

        public string Product
        {
            get => _product;
            set => SetProperty(ref _product, value);
        }

        public string Process
        {
            get => _process;
            set => SetProperty(ref _process, value);
        }

        public decimal NetWeight
        {
            get => _netWeight;
            set => SetProperty(ref _netWeight, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public string DisplayText => $"Receipt {ReceiptNumber} - {GrowerName} ({GrowerNumber}): {Reason}";
        public string DetailedText => $"Receipt: {ReceiptNumber}\nGrower: {GrowerName} ({GrowerNumber})\nProduct: {Product}\nProcess: {Process}\nWeight: {NetWeight:N2} lbs\nDate: {ReceiptDate:yyyy-MM-dd}\nReason: {Reason}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

