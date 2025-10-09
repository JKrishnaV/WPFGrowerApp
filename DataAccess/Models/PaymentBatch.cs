using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment batch in the modern database schema.
    /// Tracks batch processing for advance payments (ADV1, ADV2, ADV3, FINAL).
    /// Matches the PaymentBatches table structure with clean, modern field names.
    /// </summary>
    public class PaymentBatch : INotifyPropertyChanged
    {
        // ======================================================================
        // PRIMARY IDENTIFICATION
        // ======================================================================
        
        private int _paymentBatchId;
        private string _batchNumber = string.Empty;
        private int _paymentTypeId;
        private DateTime _batchDate;

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

        // ======================================================================
        // BATCH TOTALS
        // ======================================================================
        
        private decimal? _totalAmount;
        private int? _totalGrowers;
        private int? _totalReceipts;

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

        // ======================================================================
        // STATUS & PROCESSING
        // ======================================================================
        
        private string _status = "Draft";
        private string? _notes;
        private DateTime? _processedAt;
        private string? _processedBy;

        /// <summary>
        /// Batch status: Draft, Posted, Finalized, Voided
        /// </summary>
        public string Status
        {
            get => _status ?? "Draft";
            set => SetProperty(ref _status, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
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

        // ======================================================================
        // AUDIT TRAIL
        // ======================================================================
        
        private DateTime _createdAt;
        private string? _createdBy;
        private DateTime? _modifiedAt;
        private string? _modifiedBy;
        private DateTime? _deletedAt;
        private string? _deletedBy;

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string? CreatedBy
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
        // NAVIGATION PROPERTIES (Not mapped to database)
        // ======================================================================
        
        private string? _paymentTypeName;
        private int? _cropYear;
        private DateTime? _cutoffDate;
        private string? _filterPayGroup;
        private int? _filterGrower;

        /// <summary>
        /// Payment type name for display (e.g., "Advance 1", "Advance 2", "Final Payment")
        /// </summary>
        public string? PaymentTypeName
        {
            get => _paymentTypeName;
            set => SetProperty(ref _paymentTypeName, value);
        }

        /// <summary>
        /// Crop year for this payment batch (for filtering/grouping)
        /// </summary>
        public int? CropYear
        {
            get => _cropYear;
            set => SetProperty(ref _cropYear, value);
        }

        /// <summary>
        /// Include receipts up to this date (used for test run parameters)
        /// </summary>
        public DateTime? CutoffDate
        {
            get => _cutoffDate;
            set => SetProperty(ref _cutoffDate, value);
        }

        /// <summary>
        /// Optional: Filter to specific pay group (used for test run parameters)
        /// </summary>
        public string? FilterPayGroup
        {
            get => _filterPayGroup;
            set => SetProperty(ref _filterPayGroup, value);
        }

        /// <summary>
        /// Optional: Filter to specific grower (used for test run parameters)
        /// </summary>
        public int? FilterGrower
        {
            get => _filterGrower;
            set => SetProperty(ref _filterGrower, value);
        }

        // ======================================================================
        // COMPUTED PROPERTIES
        // ======================================================================

        /// <summary>
        /// Is this batch soft-deleted?
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;

        /// <summary>
        /// Is this batch posted (finalized)?
        /// </summary>
        public bool IsPosted => Status == "Posted" || Status == "Finalized";

        /// <summary>
        /// Can this batch be edited?
        /// </summary>
        public bool CanBeEdited => Status == "Draft" && !IsDeleted;

        /// <summary>
        /// Can this batch be posted?
        /// </summary>
        public bool CanBePosted => Status == "Draft" && !IsDeleted && TotalGrowers > 0;

        /// <summary>
        /// Can this batch be voided?
        /// </summary>
        public bool CanBeVoided => IsPosted && !IsDeleted;

        /// <summary>
        /// Display name for the batch
        /// </summary>
        public string DisplayName => $"Batch {BatchNumber} - {PaymentTypeName} ({BatchDate:yyyy-MM-dd})";

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
            
            // Update dependent computed properties
            if (propertyName == nameof(Status) || propertyName == nameof(DeletedAt) || propertyName == nameof(TotalGrowers))
            {
                OnPropertyChanged(nameof(IsPosted));
                OnPropertyChanged(nameof(CanBeEdited));
                OnPropertyChanged(nameof(CanBePosted));
                OnPropertyChanged(nameof(CanBeVoided));
            }
            
            return true;
        }
    }
}
