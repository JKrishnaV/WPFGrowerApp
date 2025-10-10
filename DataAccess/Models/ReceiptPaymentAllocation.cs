using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment allocation to a receipt.
    /// Links receipts to payment batches and tracks what was paid.
    /// Matches the ReceiptPaymentAllocations table in the modern database.
    /// </summary>
    public class ReceiptPaymentAllocation : INotifyPropertyChanged
    {
        // ======================================================================
        // PRIMARY IDENTIFICATION
        // ======================================================================
        
        private int _allocationId;
        private int _receiptId;
        private int _paymentBatchId;
        private int _paymentTypeId;

        public int AllocationId
        {
            get => _allocationId;
            set => SetProperty(ref _allocationId, value);
        }

        public int ReceiptId
        {
            get => _receiptId;
            set => SetProperty(ref _receiptId, value);
        }

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public int PaymentTypeId
        {
            get => _paymentTypeId;
            set => SetProperty(ref _paymentTypeId, value);
        }

        // ======================================================================
        // PRICING & PAYMENT DETAILS
        // ======================================================================
        
        private int _priceScheduleId;
        private decimal _pricePerPound;
        private decimal _quantityPaid;
        private decimal _amountPaid;

        public int PriceScheduleId
        {
            get => _priceScheduleId;
            set => SetProperty(ref _priceScheduleId, value);
        }

        public decimal PricePerPound
        {
            get => _pricePerPound;
            set => SetProperty(ref _pricePerPound, value);
        }

        public decimal QuantityPaid
        {
            get => _quantityPaid;
            set => SetProperty(ref _quantityPaid, value);
        }

        public decimal AmountPaid
        {
            get => _amountPaid;
            set => SetProperty(ref _amountPaid, value);
        }

        // ======================================================================
        // AUDIT TRAIL
        // ======================================================================
        
        private DateTime _allocatedAt;
        private string _status = "Pending";
        private DateTime? _modifiedAt;
        private string? _modifiedBy;

        public DateTime AllocatedAt
        {
            get => _allocatedAt;
            set => SetProperty(ref _allocatedAt, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set => SetProperty(ref _modifiedAt, value);
        }

        public string? ModifiedBy
        {
            get => _modifiedBy;
            set => SetProperty(ref _modifiedBy, value);
        }

        // ======================================================================
        // NAVIGATION PROPERTIES (Not mapped to database)
        // ======================================================================
        
        private string? _receiptNumber;
        private string? _growerName;
        private string? _paymentTypeName;
        private string? _batchNumber;

        public string? ReceiptNumber
        {
            get => _receiptNumber;
            set => SetProperty(ref _receiptNumber, value);
        }

        public string? GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public string? PaymentTypeName
        {
            get => _paymentTypeName;
            set => SetProperty(ref _paymentTypeName, value);
        }

        public string? BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        // ======================================================================
        // INOTIFYPROPERTYCHANGED IMPLEMENTATION
        // ======================================================================

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
