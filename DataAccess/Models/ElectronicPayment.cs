using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents an electronic payment record.
    /// Tracks individual electronic payments for growers.
    /// </summary>
    public class ElectronicPayment : INotifyPropertyChanged
    {
        private int _electronicPaymentId;
        private int _paymentBatchId;
        private int _growerId;
        private decimal _amount;
        private DateTime _paymentDate;
        private string _status = string.Empty;
        private DateTime _createdAt;
        private string _createdBy = string.Empty;
        private string _paymentMethod = string.Empty;
        private string _referenceNumber = string.Empty;
        private DateTime? _processedAt;
        private string? _processedBy;
        private string? _growerName;
        private bool _isSelected;

        public int ElectronicPaymentId
        {
            get => _electronicPaymentId;
            set => SetProperty(ref _electronicPaymentId, value);
        }

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
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

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public string ReferenceNumber
        {
            get => _referenceNumber;
            set => SetProperty(ref _referenceNumber, value);
        }

        public DateTime? ProcessedAt
        {
            get => _processedAt;
            set => SetProperty(ref _processedAt, value);
        }

        public string? ProcessedBy
        {
            get => _processedBy;
            set => SetProperty(ref _processedBy, value);
        }

        // Navigation properties
        public string? GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        // UI properties
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // Helper properties
        public bool IsProcessed => _status == "Processed";
        public bool IsPending => _status == "Generated" || _status == "Pending";
        public string StatusDisplay => GetStatusDisplay();

        private string GetStatusDisplay()
        {
            return Status switch
            {
                "Generated" => "Ready for Processing",
                "Pending" => "Pending Bank Processing",
                "Processed" => "Processed by Bank",
                "Failed" => "Processing Failed",
                _ => Status
            };
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
