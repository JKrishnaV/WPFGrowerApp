using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment type (Advance 1, Advance 2, Advance 3, Final, etc.)
    /// Matches the PaymentTypes table in the modern database (verified Oct 9, 2025).
    /// Extensible: Can add ADV4, ADV5, SPECIAL, LOAN by inserting rows.
    /// </summary>
    public class PaymentType : INotifyPropertyChanged
    {
        // ======================================================================
        // PRIMARY IDENTIFICATION
        // ======================================================================
        
        private int _paymentTypeId;
        private string _typeCode;
        private string _typeName;
        private int _sequenceNumber;
        private bool _isFinalPayment;
        private string? _description;
        private int? _displayOrder;
        private bool _isActive;

        public int PaymentTypeId
        {
            get => _paymentTypeId;
            set => SetProperty(ref _paymentTypeId, value);
        }

        /// <summary>
        /// Payment type code: ADV1, ADV2, ADV3, FINAL, SPECIAL, LOAN
        /// </summary>
        public string TypeCode
        {
            get => _typeCode;
            set => SetProperty(ref _typeCode, value);
        }

        public string TypeName
        {
            get => _typeName;
            set => SetProperty(ref _typeName, value);
        }

        /// <summary>
        /// Sequence number determines payment order (1, 2, 3, ..., 99 for FINAL)
        /// </summary>
        public int SequenceNumber
        {
            get => _sequenceNumber;
            set => SetProperty(ref _sequenceNumber, value);
        }

        /// <summary>
        /// True if this is the final payment type
        /// </summary>
        public bool IsFinalPayment
        {
            get => _isFinalPayment;
            set => SetProperty(ref _isFinalPayment, value);
        }

        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public int? DisplayOrder
        {
            get => _displayOrder;
            set => SetProperty(ref _displayOrder, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // ======================================================================
        // AUDIT TRAIL (from actual database schema)
        // ======================================================================

        private DateTime _createdAt;
        private string _createdBy;
        private DateTime? _modifiedAt;
        private string? _modifiedBy;
        private DateTime? _deletedAt;
        private string? _deletedBy;

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

        public string? ModifiedBy
        {
            get => _modifiedBy;
            set => SetProperty(ref _modifiedBy, value);
        }

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set => SetProperty(ref _deletedAt, value);
        }

        public string? DeletedBy
        {
            get => _deletedBy;
            set => SetProperty(ref _deletedBy, value);
        }

        // ======================================================================
        // COMPUTED PROPERTIES
        // ======================================================================

        /// <summary>
        /// Is this an advance payment (ADV1, ADV2, ADV3, ADV4, etc.)?
        /// </summary>
        public bool IsAdvancePayment => TypeCode?.StartsWith("ADV") == true;

        /// <summary>
        /// Is this soft-deleted?
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;

        /// <summary>
        /// Which advance number (1, 2, 3, 4, ...) or 0 for non-advance
        /// Uses SequenceNumber for better extensibility
        /// </summary>
        public int AdvanceNumber => IsAdvancePayment && !IsFinalPayment ? SequenceNumber : 0;

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

