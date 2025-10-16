using System;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    public class GrowerAccount : INotifyPropertyChanged
    {
        private int _accountId;
        private int _growerId;
        private DateTime _transactionDate;
        private string _transactionType = string.Empty;
        private string _description = string.Empty;
        private decimal _debitAmount;
        private decimal _creditAmount;
        private int? _paymentBatchId;
        private int? _receiptId;
        private int? _chequeId;
        private string _currencyCode = "CAD";
        private decimal _exchangeRate = 1.0m;
        private DateTime _createdAt;
        private string _createdBy = string.Empty;
        private DateTime? _modifiedAt;
        private string _modifiedBy = string.Empty;
        private DateTime? _deletedAt;
        private string _deletedBy = string.Empty;

        public int AccountId
        {
            get => _accountId;
            set
            {
                if (_accountId != value)
                {
                    _accountId = value;
                    OnPropertyChanged(nameof(AccountId));
                }
            }
        }

        public int GrowerId
        {
            get => _growerId;
            set
            {
                if (_growerId != value)
                {
                    _growerId = value;
                    OnPropertyChanged(nameof(GrowerId));
                }
            }
        }

        public DateTime TransactionDate
        {
            get => _transactionDate;
            set
            {
                if (_transactionDate != value)
                {
                    _transactionDate = value;
                    OnPropertyChanged(nameof(TransactionDate));
                }
            }
        }

        public string TransactionType
        {
            get => _transactionType;
            set
            {
                if (_transactionType != value)
                {
                    _transactionType = value ?? string.Empty;
                    OnPropertyChanged(nameof(TransactionType));
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value ?? string.Empty;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public decimal DebitAmount
        {
            get => _debitAmount;
            set
            {
                if (_debitAmount != value)
                {
                    _debitAmount = value;
                    OnPropertyChanged(nameof(DebitAmount));
                }
            }
        }

        public decimal CreditAmount
        {
            get => _creditAmount;
            set
            {
                if (_creditAmount != value)
                {
                    _creditAmount = value;
                    OnPropertyChanged(nameof(CreditAmount));
                }
            }
        }

        public int? PaymentBatchId
        {
            get => _paymentBatchId;
            set
            {
                if (_paymentBatchId != value)
                {
                    _paymentBatchId = value;
                    OnPropertyChanged(nameof(PaymentBatchId));
                }
            }
        }

        public int? ReceiptId
        {
            get => _receiptId;
            set
            {
                if (_receiptId != value)
                {
                    _receiptId = value;
                    OnPropertyChanged(nameof(ReceiptId));
                }
            }
        }

        public int? ChequeId
        {
            get => _chequeId;
            set
            {
                if (_chequeId != value)
                {
                    _chequeId = value;
                    OnPropertyChanged(nameof(ChequeId));
                }
            }
        }

        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                if (_currencyCode != value)
                {
                    _currencyCode = value ?? "CAD";
                    OnPropertyChanged(nameof(CurrencyCode));
                }
            }
        }

        public decimal ExchangeRate
        {
            get => _exchangeRate;
            set
            {
                if (_exchangeRate != value)
                {
                    _exchangeRate = value;
                    OnPropertyChanged(nameof(ExchangeRate));
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    OnPropertyChanged(nameof(CreatedAt));
                }
            }
        }

        public string CreatedBy
        {
            get => _createdBy;
            set
            {
                if (_createdBy != value)
                {
                    _createdBy = value ?? string.Empty;
                    OnPropertyChanged(nameof(CreatedBy));
                }
            }
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set
            {
                if (_modifiedAt != value)
                {
                    _modifiedAt = value;
                    OnPropertyChanged(nameof(ModifiedAt));
                }
            }
        }

        public string ModifiedBy
        {
            get => _modifiedBy;
            set
            {
                if (_modifiedBy != value)
                {
                    _modifiedBy = value ?? string.Empty;
                    OnPropertyChanged(nameof(ModifiedBy));
                }
            }
        }

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set
            {
                if (_deletedAt != value)
                {
                    _deletedAt = value;
                    OnPropertyChanged(nameof(DeletedAt));
                }
            }
        }

        public string DeletedBy
        {
            get => _deletedBy;
            set
            {
                if (_deletedBy != value)
                {
                    _deletedBy = value ?? string.Empty;
                    OnPropertyChanged(nameof(DeletedBy));
                }
            }
        }

        // Navigation properties (optional, for future use)
        public Grower? Grower { get; set; }
        public PaymentBatch? PaymentBatch { get; set; }
        public Receipt? Receipt { get; set; }
        public Cheque? Cheque { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
