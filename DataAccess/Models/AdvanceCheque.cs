using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Model representing an advance cheque issued to a grower
    /// </summary>
    public class AdvanceCheque : INotifyPropertyChanged
    {
        private int _advanceChequeId;
        private int _growerId;
        private decimal _advanceAmount;
        private decimal _originalAdvanceAmount;
        private decimal _currentAdvanceAmount;
        private decimal _totalDeductedAmount;

        private bool _isFullyDeducted;
        private DateTime? _lastDeductionDate;
        private int _deductionCount;
        private DateTime _advanceDate;
        private string? _reason;
        private string? _status;
        private string? _createdBy;
        private DateTime _createdAt;
        private DateTime? _deductedAt;
        private string? _deductedBy;
        private int? _deductedFromBatchId;
        private DateTime? _modifiedAt;
        private string? _modifiedBy;
        private DateTime? _deletedAt;
        private string? _deletedBy;
        private int? _chequeNumber;
        
        // Professional accounting fields
        private int _fiscalYear;
        private string? _accountingPeriod;
        private string? _glAccountCode;
        private string? _costCenter;
        private string? _growerNumber;
        private string? _growerName;
        private string? _systemVersion;

        // Navigation properties
        private Grower? _grower;
        private PaymentBatch? _deductedFromBatch;

        public int AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public decimal AdvanceAmount
        {
            get => _advanceAmount;
            set => SetProperty(ref _advanceAmount, value);
        }

        public decimal OriginalAdvanceAmount
        {
            get => _originalAdvanceAmount;
            set => SetProperty(ref _originalAdvanceAmount, value);
        }

        public decimal CurrentAdvanceAmount
        {
            get => _currentAdvanceAmount;
            set => SetProperty(ref _currentAdvanceAmount, value);
        }

        public decimal TotalDeductedAmount
        {
            get => _totalDeductedAmount;
            set => SetProperty(ref _totalDeductedAmount, value);
        }



        public bool IsFullyDeducted
        {
            get => _isFullyDeducted;
            set => SetProperty(ref _isFullyDeducted, value);
        }

        public DateTime? LastDeductionDate
        {
            get => _lastDeductionDate;
            set => SetProperty(ref _lastDeductionDate, value);
        }

        public int DeductionCount
        {
            get => _deductionCount;
            set => SetProperty(ref _deductionCount, value);
        }

        public DateTime AdvanceDate
        {
            get => _advanceDate;
            set => SetProperty(ref _advanceDate, value);
        }

        public string? Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public string? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string? CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime? DeductedAt
        {
            get => _deductedAt;
            set => SetProperty(ref _deductedAt, value);
        }

        public string? DeductedBy
        {
            get => _deductedBy;
            set => SetProperty(ref _deductedBy, value);
        }

        public int? DeductedFromBatchId
        {
            get => _deductedFromBatchId;
            set => SetProperty(ref _deductedFromBatchId, value);
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

        public int? ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        // Professional accounting fields
        public int FiscalYear
        {
            get => _fiscalYear;
            set => SetProperty(ref _fiscalYear, value);
        }

        public string? AccountingPeriod
        {
            get => _accountingPeriod;
            set => SetProperty(ref _accountingPeriod, value);
        }

        public string? GLAccountCode
        {
            get => _glAccountCode;
            set => SetProperty(ref _glAccountCode, value);
        }

        public string? CostCenter
        {
            get => _costCenter;
            set => SetProperty(ref _costCenter, value);
        }

        public string? GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public string? GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public string? SystemVersion
        {
            get => _systemVersion;
            set => SetProperty(ref _systemVersion, value);
        }

        // Navigation properties
        public Grower? Grower
        {
            get => _grower;
            set => SetProperty(ref _grower, value);
        }

        public PaymentBatch? DeductedFromBatch
        {
            get => _deductedFromBatch;
            set => SetProperty(ref _deductedFromBatch, value);
        }

        // ======================================================================
        // UNIFIED WORKFLOW PROPERTIES (Same as regular cheques)
        // ======================================================================
        
        private DateTime? _printedDate;
        private string? _printedBy;
        private DateTime? _deliveredAt;
        private string? _deliveredBy;
        private string? _deliveryMethod;
        private DateTime? _voidedDate;
        private string? _voidedBy;
        private string? _voidedReason;

        /// <summary>
        /// When the advance cheque was printed
        /// </summary>
        public DateTime? PrintedDate
        {
            get => _printedDate;
            set => SetProperty(ref _printedDate, value);
        }

        /// <summary>
        /// Who printed the advance cheque
        /// </summary>
        public string? PrintedBy
        {
            get => _printedBy;
            set => SetProperty(ref _printedBy, value);
        }

        /// <summary>
        /// When the advance cheque was delivered to the grower
        /// </summary>
        public DateTime? DeliveredAt
        {
            get => _deliveredAt;
            set => SetProperty(ref _deliveredAt, value);
        }

        /// <summary>
        /// Who delivered the advance cheque
        /// </summary>
        public string? DeliveredBy
        {
            get => _deliveredBy;
            set => SetProperty(ref _deliveredBy, value);
        }

        /// <summary>
        /// How the advance cheque was delivered (Mail, Pickup, Courier, etc.)
        /// </summary>
        public string? DeliveryMethod
        {
            get => _deliveryMethod;
            set => SetProperty(ref _deliveryMethod, value);
        }

        /// <summary>
        /// When the advance cheque was voided
        /// </summary>
        public DateTime? VoidedDate
        {
            get => _voidedDate;
            set => SetProperty(ref _voidedDate, value);
        }

        /// <summary>
        /// Who voided the advance cheque
        /// </summary>
        public string? VoidedBy
        {
            get => _voidedBy;
            set => SetProperty(ref _voidedBy, value);
        }

        /// <summary>
        /// Reason for voiding the advance cheque
        /// </summary>
        public string? VoidedReason
        {
            get => _voidedReason;
            set => SetProperty(ref _voidedReason, value);
        }

        // ======================================================================
        // COMPUTED PROPERTIES (Unified with regular cheques)
        // ======================================================================

        /// <summary>
        /// The type of cheque - always "Advance" for advance cheques
        /// </summary>
        public string ChequeType { get; set; } = "Advance";

        /// <summary>
        /// Is this advance cheque voided?
        /// </summary>
        public bool IsVoided => Status == "Voided";

        /// <summary>
        /// Is this advance cheque delivered?
        /// </summary>
        public bool IsDelivered => Status == "Delivered";

        /// <summary>
        /// Is this advance cheque printed?
        /// </summary>
        public bool IsPrinted => Status == "Printed";

        /// <summary>
        /// Is this advance cheque generated?
        /// </summary>
        public bool IsGenerated => Status == "Generated";

        /// <summary>
        /// Can this advance cheque be voided? (Only generated and printed cheques can be voided)
        /// </summary>
        public bool CanBeVoided => Status == "Generated" || Status == "Printed";

        /// <summary>
        /// Can this advance cheque be printed?
        /// </summary>
        public bool CanBePrinted => Status == "Generated";

        /// <summary>
        /// Can this advance cheque be delivered?
        /// </summary>
        public bool CanBeDelivered => Status == "Printed";

        /// <summary>
        /// Can this advance cheque be deducted from regular payments?
        /// </summary>
        public bool CanBeDeducted => Status == "Delivered";

        // Legacy properties for backward compatibility
        public bool IsActive => Status == "Generated";
        public bool IsDeducted => Status == "Delivered";
        public bool IsCancelled => Status == "Voided";

        // Computed properties
        public decimal OutstandingBalance => CurrentAdvanceAmount;
        public decimal PercentageDeducted => OriginalAdvanceAmount > 0 ? (TotalDeductedAmount / OriginalAdvanceAmount) * 100 : 0;
        public bool HasOutstandingBalance => CurrentAdvanceAmount > 0;
        public bool IsPartiallyDeducted => TotalDeductedAmount > 0 && !IsFullyDeducted;
        public decimal CalculatedBalance => OriginalAdvanceAmount - TotalDeductedAmount;

        // Display properties
        public string StatusDisplay => Status ?? "Unknown";
        public string AmountDisplay => AdvanceAmount.ToString("C");
        public string OriginalAmountDisplay => OriginalAdvanceAmount.ToString("C");
        public string CurrentAmountDisplay => CurrentAdvanceAmount.ToString("C");
        public string OutstandingBalanceDisplay => OutstandingBalance.ToString("C");
        public string PercentageDeductedDisplay => $"{PercentageDeducted:F1}%";
        public string DateDisplay => AdvanceDate.ToString("MMM dd, yyyy");
        public string GrowerDisplay => Grower?.FullName ?? $"Grower {GrowerId}";

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
