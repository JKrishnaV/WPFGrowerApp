using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        
        private string _status = "Issued";
        private DateTime? _clearedDate;
        private DateTime? _voidedDate;
        private string? _voidedReason;

        /// <summary>
        /// Cheque status: Generated, Issued, Cleared, Voided, Stopped
        /// </summary>
        public string Status
        {
            get => _status ?? "Issued";
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
        private string? _voidedBy;

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

        public string? VoidedBy
        {
            get => _voidedBy;
            set => SetProperty(ref _voidedBy, value);
        }

        // ======================================================================
        // NAVIGATION PROPERTIES (Not mapped to database)
        // ======================================================================
        
        private string? _growerName;
        private string? _seriesCode;
        private string? _paymentTypeName;

        /// <summary>
        /// Grower name for display purposes
        /// </summary>
        public string? GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
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
        public bool IsCleared => Status == "Cleared";

        /// <summary>
        /// Can this cheque be voided? (Only generated and issued cheques can be voided)
        /// </summary>
        public bool CanBeVoided => Status == "Generated" || Status == "Issued";

        /// <summary>
        /// Full cheque number for display (e.g., "A-1234" or "CHQ-20251015-231733336-469")
        /// </summary>
        public string DisplayChequeNumber => !string.IsNullOrEmpty(SeriesCode) && !ChequeNumber.StartsWith("CHQ-") 
            ? $"{SeriesCode}-{ChequeNumber}" 
            : ChequeNumber;

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
