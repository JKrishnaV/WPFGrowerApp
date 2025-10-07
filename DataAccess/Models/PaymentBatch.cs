using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment batch record from PaymentBatches table.
    /// Tracks batch processing for advance payments (ADV1, ADV2, ADV3, FINAL).
    /// </summary>
    public class PaymentBatch : INotifyPropertyChanged
    {
        private int _paymentBatchId;
        private string _batchNumber;
        private int _paymentTypeId;
        private DateTime _batchDate;
        private decimal? _totalAmount;
        private int? _totalGrowers;
        private int? _totalReceipts;
        private string _status;
        private string _notes;
        private DateTime? _processedAt;
        private string _processedBy;
        private DateTime _createdAt;
        private string _createdBy;

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public int PaymentTypeId
        {
            get => _paymentTypeId;
            set => SetProperty(ref _paymentTypeId, value);
        }

        public DateTime BatchDate
        {
            get => _batchDate;
            set => SetProperty(ref _batchDate, value);
        }

        public decimal? TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public int? TotalGrowers
        {
            get => _totalGrowers;
            set => SetProperty(ref _totalGrowers, value);
        }

        public int? TotalReceipts
        {
            get => _totalReceipts;
            set => SetProperty(ref _totalReceipts, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
