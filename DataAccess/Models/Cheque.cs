using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment cheque in the modern database schema.
    /// Matches the Cheques table structure with clean, modern field names.
    /// </summary>
    public class Cheque : INotifyPropertyChanged
    {
        // ======================================================================
        // PRIMARY IDENTIFICATION
        // ======================================================================
        
        private int _chequeId;
        private int _chequeSeriesId;
        private string _chequeNumber = string.Empty;
        private int _fiscalYear;

        public int ChequeId
        {
            get => _chequeId;
            set => SetProperty(ref _chequeId, value);
        }

        public int ChequeSeriesId
        {
            get => _chequeSeriesId;
            set => SetProperty(ref _chequeSeriesId, value);
        }

        public string ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        public int FiscalYear
        {
            get => _fiscalYear;
            set => SetProperty(ref _fiscalYear, value);
        }

        // ======================================================================
        // PAYMENT DETAILS
        // ======================================================================
        
        private int _growerId;
        private int? _paymentBatchId;
        private DateTime _chequeDate;
        private decimal _chequeAmount;

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public int? PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public DateTime ChequeDate
        {
            get => _chequeDate;
            set => SetProperty(ref _chequeDate, value);
        }

        public decimal ChequeAmount
        {
            get => _chequeAmount;
            set => SetProperty(ref _chequeAmount, value);
        }

        // ======================================================================
        // CURRENCY & PAYEE
        // ======================================================================
        
        private string _currencyCode = "CAD";
        private decimal _exchangeRate = 1.0m;
        private string? _payeeName;
        private string? _memo;

        public string CurrencyCode
        {
            get => _currencyCode ?? "CAD";
            set => SetProperty(ref _currencyCode, value);
        }

        public decimal ExchangeRate
        {
            get => _exchangeRate;
            set => SetProperty(ref _exchangeRate, value);
        }

        public string? PayeeName
        {
            get => _payeeName;
            set => SetProperty(ref _payeeName, value);
        }

        public string? Memo
        {
            get => _memo;
            set => SetProperty(ref _memo, value);
        }

        // ======================================================================
        // STATUS & CLEARING
        // ======================================================================
        
        private string _status = "Generated";
        private DateTime? _clearedDate;
        private DateTime? _voidedDate;
        private string? _voidedReason;

        /// <summary>
        /// Cheque status: Generated, Printed, Delivered, Voided, Stopped
        /// </summary>
        public string Status
        {
            get => _status ?? "Generated";
            set => SetProperty(ref _status, value);
        }

        public DateTime? ClearedDate
        {
            get => _clearedDate;
            set => SetProperty(ref _clearedDate, value);
        }

        public DateTime? VoidedDate
        {
            get => _voidedDate;
            set => SetProperty(ref _voidedDate, value);
        }

        public string? VoidedReason
        {
            get => _voidedReason;
            set => SetProperty(ref _voidedReason, value);
        }

        // ======================================================================
        // AUDIT TRAIL
        // ======================================================================
        
        private DateTime _createdAt;
        private string? _createdBy;
        private DateTime? _printedAt;
        private string? _printedBy;
        private DateTime? _issuedDate;
        private string? _issuedBy;
        private DateTime? _printedDate;
        private string? _voidedBy;
        private DateTime? _deliveredAt;
        private string? _deliveredBy;
        private string? _deliveryMethod;

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

        /// <summary>
        /// When the cheque was actually printed (added for tracking)
        /// </summary>
        public DateTime? PrintedAt
        {
            get => _printedAt;
            set => SetProperty(ref _printedAt, value);
        }

        public string? PrintedBy
        {
            get => _printedBy;
            set => SetProperty(ref _printedBy, value);
        }

        /// <summary>
        /// When the cheque was issued (added for tracking)
        /// </summary>
        public DateTime? IssuedDate
        {
            get => _issuedDate;
            set => SetProperty(ref _issuedDate, value);
        }

        public string? IssuedBy
        {
            get => _issuedBy;
            set => SetProperty(ref _issuedBy, value);
        }

        /// <summary>
        /// When the cheque was printed (added for tracking)
        /// </summary>
        public DateTime? PrintedDate
        {
            get => _printedDate;
            set => SetProperty(ref _printedDate, value);
        }

        public string? VoidedBy
        {
            get => _voidedBy;
            set => SetProperty(ref _voidedBy, value);
        }

        /// <summary>
        /// When the cheque was delivered to the grower
        /// </summary>
        public DateTime? DeliveredAt
        {
            get => _deliveredAt;
            set => SetProperty(ref _deliveredAt, value);
        }

        /// <summary>
        /// Who delivered the cheque
        /// </summary>
        public string? DeliveredBy
        {
            get => _deliveredBy;
            set => SetProperty(ref _deliveredBy, value);
        }

        /// <summary>
        /// How the cheque was delivered (Mail, Pickup, Courier, etc.)
        /// </summary>
        public string? DeliveryMethod
        {
            get => _deliveryMethod;
            set => SetProperty(ref _deliveryMethod, value);
        }

        // ======================================================================
        // UNIFIED CHEQUE SYSTEM PROPERTIES
        // ======================================================================
        
        private bool _isConsolidated;
        private string? _consolidatedFromBatches;
        private bool _isAdvanceCheque;
        private int? _advanceChequeId;

        /// <summary>
        /// Is this a consolidated cheque from multiple batches?
        /// </summary>
        public bool IsConsolidated
        {
            get => _isConsolidated;
            set => SetProperty(ref _isConsolidated, value);
        }

        /// <summary>
        /// JSON array of batch IDs that were consolidated into this cheque
        /// </summary>
        public string? ConsolidatedFromBatches
        {
            get => _consolidatedFromBatches;
            set => SetProperty(ref _consolidatedFromBatches, value);
        }

        /// <summary>
        /// Is this an advance cheque?
        /// </summary>
        public bool IsAdvanceCheque
        {
            get => _isAdvanceCheque;
            set => SetProperty(ref _isAdvanceCheque, value);
        }

        /// <summary>
        /// Reference to the advance cheque that generated this cheque
        /// </summary>
        public int? AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        // ======================================================================
        // NAVIGATION PROPERTIES (Not mapped to database)
        // ======================================================================
        
        private string? _growerName;
        private string? _growerNumber;
        private string? _batchNumber;
        private string? _seriesCode;
        private string? _paymentTypeName;
        private bool _isSelected;
        private ChequeSeries? _chequeSeries;

        /// <summary>
        /// Grower name for display purposes
        /// </summary>
        public string? GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        /// <summary>
        /// Grower number for display purposes
        /// </summary>
        public string? GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        /// <summary>
        /// Batch number for display purposes
        /// </summary>
        public string? BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        /// <summary>
        /// Cheque series code for display (e.g., "A", "B", "MAIN")
        /// </summary>
        public string? SeriesCode
        {
            get => _seriesCode;
            set => SetProperty(ref _seriesCode, value);
        }

        /// <summary>
        /// Payment type name for display (e.g., "Advance 1", "Final Payment")
        /// </summary>
        public string? PaymentTypeName
        {
            get => _paymentTypeName;
            set => SetProperty(ref _paymentTypeName, value);
        }

        // ======================================================================
        // COMPUTED PROPERTIES
        // ======================================================================

        /// <summary>
        /// Is this cheque voided?
        /// </summary>
        public bool IsVoided => Status == "Voided";

        /// <summary>
        /// Is this cheque cleared?
        /// </summary>
        public bool IsCleared => Status == "Delivered";

        /// <summary>
        /// Can this cheque be voided? (Only generated and issued cheques can be voided)
        /// </summary>
        public bool CanBeVoided => Status == "Generated" || Status == "Printed";

        /// <summary>
        /// Can this cheque be printed?
        /// </summary>
        public bool CanBePrinted => Status == "Generated";

        /// <summary>
        /// Can this cheque be issued?
        /// </summary>
        public bool CanBeIssued => Status == "Printed";

        /// <summary>
        /// Get the payment type for this cheque
        /// </summary>
        public string PaymentType => IsAdvanceCheque ? "Advance" : IsConsolidated ? "Consolidated" : "Regular";

        /// <summary>
        /// Full cheque number for display (e.g., "A-1234" or "CHQ-20251015-231733336-469")
        /// </summary>
        public string DisplayChequeNumber => !string.IsNullOrEmpty(SeriesCode) && !ChequeNumber.StartsWith("CHQ-") 
            ? $"{SeriesCode}-{ChequeNumber}" 
            : ChequeNumber;

        /// <summary>
        /// For UI selection purposes
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // Navigation properties
        public ChequeSeries? ChequeSeries
        {
            get => _chequeSeries;
            set => SetProperty(ref _chequeSeries, value);
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
            // Add diagnostic logging to see if SetProperty is called for any property change
            Logger.Info($"Cheque.SetProperty: Property '{propertyName}' changed to '{value}' for ChequeId={ChequeId}");
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
