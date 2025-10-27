using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Model representing a deduction from an advance cheque
    /// </summary>
    public class AdvanceDeduction : INotifyPropertyChanged
    {
        private int _deductionId;
        private int _advanceChequeId;
        private int? _chequeId;
        private int _paymentBatchId;
        private decimal _deductionAmount;
        private DateTime _deductionDate;
        private string _transactionType;
        private string _status;
        private bool _isVoided;
        private DateTime? _voidedAt;
        private string _voidedBy;
        private string _voidReason;
        private string _createdBy;
        private DateTime _createdAt;
        private string _modifiedBy;
        private DateTime? _modifiedAt;
        private DateTime? _deletedAt;
        private string _deletedBy;
        private string _deletedReason;
        
        // Professional accounting fields
        private int _fiscalYear;
        private string _accountingPeriod;
        private string _glAccountCode;
        private string _costCenter;
        private int _growerId;
        private string _growerNumber;
        private decimal? _originalAdvanceAmount;
        private decimal? _remainingAdvanceAmount;
        private int? _batchSequence;
        private int? _processingOrder;
        private string _systemVersion;

        // Navigation properties
        private AdvanceCheque _advanceCheque;
        private PaymentBatch _paymentBatch;

        public int DeductionId
        {
            get => _deductionId;
            set => SetProperty(ref _deductionId, value);
        }

        public int AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        public int? ChequeId
        {
            get => _chequeId;
            set => SetProperty(ref _chequeId, value);
        }

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public decimal DeductionAmount
        {
            get => _deductionAmount;
            set => SetProperty(ref _deductionAmount, value);
        }

        public DateTime DeductionDate
        {
            get => _deductionDate;
            set => SetProperty(ref _deductionDate, value);
        }

        public string CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string TransactionType
        {
            get => _transactionType;
            set => SetProperty(ref _transactionType, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool IsVoided
        {
            get => _isVoided;
            set => SetProperty(ref _isVoided, value);
        }

        public DateTime? VoidedAt
        {
            get => _voidedAt;
            set => SetProperty(ref _voidedAt, value);
        }

        public string VoidedBy
        {
            get => _voidedBy;
            set => SetProperty(ref _voidedBy, value);
        }

        public string VoidReason
        {
            get => _voidReason;
            set => SetProperty(ref _voidReason, value);
        }

        public string ModifiedBy
        {
            get => _modifiedBy;
            set => SetProperty(ref _modifiedBy, value);
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set => SetProperty(ref _modifiedAt, value);
        }

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set => SetProperty(ref _deletedAt, value);
        }

        public string DeletedBy
        {
            get => _deletedBy;
            set => SetProperty(ref _deletedBy, value);
        }

        public string DeletedReason
        {
            get => _deletedReason;
            set => SetProperty(ref _deletedReason, value);
        }

        // Professional accounting fields
        public int FiscalYear
        {
            get => _fiscalYear;
            set => SetProperty(ref _fiscalYear, value);
        }

        public string AccountingPeriod
        {
            get => _accountingPeriod;
            set => SetProperty(ref _accountingPeriod, value);
        }

        public string GLAccountCode
        {
            get => _glAccountCode;
            set => SetProperty(ref _glAccountCode, value);
        }

        public string CostCenter
        {
            get => _costCenter;
            set => SetProperty(ref _costCenter, value);
        }

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public decimal? OriginalAdvanceAmount
        {
            get => _originalAdvanceAmount;
            set => SetProperty(ref _originalAdvanceAmount, value);
        }

        public decimal? RemainingAdvanceAmount
        {
            get => _remainingAdvanceAmount;
            set => SetProperty(ref _remainingAdvanceAmount, value);
        }

        public int? BatchSequence
        {
            get => _batchSequence;
            set => SetProperty(ref _batchSequence, value);
        }

        public int? ProcessingOrder
        {
            get => _processingOrder;
            set => SetProperty(ref _processingOrder, value);
        }

        public string SystemVersion
        {
            get => _systemVersion;
            set => SetProperty(ref _systemVersion, value);
        }

        // Navigation properties
        public AdvanceCheque AdvanceCheque
        {
            get => _advanceCheque;
            set => SetProperty(ref _advanceCheque, value);
        }

        public PaymentBatch PaymentBatch
        {
            get => _paymentBatch;
            set => SetProperty(ref _paymentBatch, value);
        }

        // Computed properties
        public bool IsActive => Status == "Active" && !IsVoided;
        public bool IsVoidedStatus => Status == "Voided" || IsVoided;
        public bool IsReversed => Status == "Reversed";
        public bool IsDeleted => DeletedAt.HasValue;
        public bool HasCheque => ChequeId.HasValue;
        public bool IsFullyDeducted => TransactionType == "FullDeduction";
        public string StatusDisplay => IsVoidedStatus ? "Voided" : (IsReversed ? "Reversed" : Status ?? "Active");
        public string TransactionTypeDisplay => TransactionType ?? "Deduction";

        // Display properties
        public string AmountDisplay => DeductionAmount.ToString("C");
        public string DateDisplay => DeductionDate.ToString("MMM dd, yyyy");
        public string VoidedDateDisplay => VoidedAt?.ToString("MMM dd, yyyy") ?? "N/A";
        public string BatchNumber => PaymentBatch?.BatchNumber ?? "N/A";
        public string GrowerName => AdvanceCheque?.GrowerName ?? "Unknown";
        public string ChequeIdDisplay => ChequeId?.ToString() ?? "N/A";
        public string OriginalAmountDisplay => OriginalAdvanceAmount?.ToString("C") ?? "N/A";
        public string RemainingAmountDisplay => RemainingAdvanceAmount?.ToString("C") ?? "N/A";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
