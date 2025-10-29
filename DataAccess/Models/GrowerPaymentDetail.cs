using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents detailed payment information for an individual grower.
    /// Contains comprehensive financial data, receipt details, and performance metrics.
    /// </summary>
    public class GrowerPaymentDetail : INotifyPropertyChanged
    {
        // ======================================================================
        // GROWER INFORMATION
        // ======================================================================
        
        private int _growerId;
        private string _growerNumber = string.Empty;
        private string _fullName = string.Empty;
        private string _checkPayeeName = string.Empty;
        private string _city = string.Empty;
        private string _province = string.Empty;
        private string _phoneNumber = string.Empty;
        private string _email = string.Empty;
        private string _address = string.Empty;
        private string _postalCode = string.Empty;

        public int GrowerId
        {
            get => _growerId;
            set
            {
                if (_growerId != value)
                {
                    _growerId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set
            {
                if (_growerNumber != value)
                {
                    _growerNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CheckPayeeName
        {
            get => _checkPayeeName;
            set
            {
                if (_checkPayeeName != value)
                {
                    _checkPayeeName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string City
        {
            get => _city;
            set
            {
                if (_city != value)
                {
                    _city = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Province
        {
            get => _province;
            set
            {
                if (_province != value)
                {
                    _province = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (_phoneNumber != value)
                {
                    _phoneNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                if (_address != value)
                {
                    _address = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PostalCode
        {
            get => _postalCode;
            set
            {
                if (_postalCode != value)
                {
                    _postalCode = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // FINANCIAL SUMMARY
        // ======================================================================
        
        private decimal _totalReceiptsValue;
        private decimal _advance1Paid;
        private decimal _advance2Paid;
        private decimal _advance3Paid;
        private decimal _finalPaymentPaid;
        private decimal _totalDeductions;
        private decimal _outstandingBalance;
        private decimal _premiumAmount;

        public decimal TotalReceiptsValue
        {
            get => _totalReceiptsValue;
            set
            {
                if (_totalReceiptsValue != value)
                {
                    _totalReceiptsValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance1Paid
        {
            get => _advance1Paid;
            set
            {
                if (_advance1Paid != value)
                {
                    _advance1Paid = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance2Paid
        {
            get => _advance2Paid;
            set
            {
                if (_advance2Paid != value)
                {
                    _advance2Paid = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance3Paid
        {
            get => _advance3Paid;
            set
            {
                if (_advance3Paid != value)
                {
                    _advance3Paid = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal FinalPaymentPaid
        {
            get => _finalPaymentPaid;
            set
            {
                if (_finalPaymentPaid != value)
                {
                    _finalPaymentPaid = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalDeductions
        {
            get => _totalDeductions;
            set
            {
                if (_totalDeductions != value)
                {
                    _totalDeductions = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal OutstandingBalance
        {
            get => _outstandingBalance;
            set
            {
                if (_outstandingBalance != value)
                {
                    _outstandingBalance = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal PremiumAmount
        {
            get => _premiumAmount;
            set
            {
                if (_premiumAmount != value)
                {
                    _premiumAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // RECEIPT DETAILS
        // ======================================================================
        
        private int _totalReceipts;
        private decimal _totalWeight;
        private DateTime? _firstReceiptDate;
        private DateTime? _lastReceiptDate;
        private DateTime? _lastPaymentDate;
        private decimal _averageReceiptValue;
        private decimal _averageReceiptWeight;

        public int TotalReceipts
        {
            get => _totalReceipts;
            set
            {
                if (_totalReceipts != value)
                {
                    _totalReceipts = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set
            {
                if (_totalWeight != value)
                {
                    _totalWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? FirstReceiptDate
        {
            get => _firstReceiptDate;
            set
            {
                if (_firstReceiptDate != value)
                {
                    _firstReceiptDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? LastReceiptDate
        {
            get => _lastReceiptDate;
            set
            {
                if (_lastReceiptDate != value)
                {
                    _lastReceiptDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? LastPaymentDate
        {
            get => _lastPaymentDate;
            set
            {
                if (_lastPaymentDate != value)
                {
                    _lastPaymentDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AverageReceiptValue
        {
            get => _averageReceiptValue;
            set
            {
                if (_averageReceiptValue != value)
                {
                    _averageReceiptValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AverageReceiptWeight
        {
            get => _averageReceiptWeight;
            set
            {
                if (_averageReceiptWeight != value)
                {
                    _averageReceiptWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // STATUS INFORMATION
        // ======================================================================
        
        private string _paymentStatus = string.Empty;
        private bool _isOnHold;
        private string _paymentMethod = string.Empty;
        private string _currencyCode = "CAD";
        private bool _isActive;
        private string _paymentGroupName = string.Empty;

        public string PaymentStatus
        {
            get => _paymentStatus;
            set
            {
                if (_paymentStatus != value)
                {
                    _paymentStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOnHold
        {
            get => _isOnHold;
            set
            {
                if (_isOnHold != value)
                {
                    _isOnHold = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod != value)
                {
                    _paymentMethod = value;
                    OnPropertyChanged();
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
                    _currencyCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PaymentGroupName
        {
            get => _paymentGroupName;
            set
            {
                if (_paymentGroupName != value)
                {
                    _paymentGroupName = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // PERFORMANCE METRICS
        // ======================================================================
        
        private decimal _averagePaymentPerReceipt;
        private int _daysSinceLastPayment;
        private decimal _paymentCompletionPercentage;
        private int _daysSinceLastReceipt;
        private decimal _paymentVelocity; // Payments per month

        public decimal AveragePaymentPerReceipt
        {
            get => _averagePaymentPerReceipt;
            set
            {
                if (_averagePaymentPerReceipt != value)
                {
                    _averagePaymentPerReceipt = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DaysSinceLastPayment
        {
            get => _daysSinceLastPayment;
            set
            {
                if (_daysSinceLastPayment != value)
                {
                    _daysSinceLastPayment = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal PaymentCompletionPercentage
        {
            get => _paymentCompletionPercentage;
            set
            {
                if (_paymentCompletionPercentage != value)
                {
                    _paymentCompletionPercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        public int DaysSinceLastReceipt
        {
            get => _daysSinceLastReceipt;
            set
            {
                if (_daysSinceLastReceipt != value)
                {
                    _daysSinceLastReceipt = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal PaymentVelocity
        {
            get => _paymentVelocity;
            set
            {
                if (_paymentVelocity != value)
                {
                    _paymentVelocity = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // CALCULATED PROPERTIES
        // ======================================================================
        
        public decimal TotalPaymentsMade => Advance1Paid + Advance2Paid + Advance3Paid + FinalPaymentPaid;
        
        public decimal TotalAdvancesPaid => Advance1Paid + Advance2Paid + Advance3Paid;
        
        public bool HasOutstandingBalance => OutstandingBalance > 0;
        
        public bool IsPaymentComplete => OutstandingBalance <= 0 && TotalReceiptsValue > 0;
        
        public string PaymentStatusDisplay => IsPaymentComplete ? "Complete" : 
                                             HasOutstandingBalance ? "Outstanding" : 
                                             TotalPaymentsMade > 0 ? "Partial" : "Pending";

        // ======================================================================
        // DISPLAY FORMATTING
        // ======================================================================
        
        public string TotalReceiptsValueDisplay => $"{TotalReceiptsValue:C2}";
        public string TotalPaymentsMadeDisplay => $"{TotalPaymentsMade:C2}";
        public string OutstandingBalanceDisplay => $"{OutstandingBalance:C2}";
        public string TotalWeightDisplay => $"{TotalWeight:N2} lbs";
        public string AverageReceiptValueDisplay => $"{AverageReceiptValue:C2}";
        public string AverageReceiptWeightDisplay => $"{AverageReceiptWeight:N2} lbs";
        
        public string Advance1PaidDisplay => $"{Advance1Paid:C2}";
        public string Advance2PaidDisplay => $"{Advance2Paid:C2}";
        public string Advance3PaidDisplay => $"{Advance3Paid:C2}";
        public string FinalPaymentPaidDisplay => $"{FinalPaymentPaidDisplay:C2}";
        public string TotalDeductionsDisplay => $"{TotalDeductions:C2}";
        public string PremiumAmountDisplay => $"{PremiumAmount:C2}";

        public string FirstReceiptDateDisplay => FirstReceiptDate?.ToString("yyyy-MM-dd") ?? "N/A";
        public string LastReceiptDateDisplay => LastReceiptDate?.ToString("yyyy-MM-dd") ?? "N/A";
        public string LastPaymentDateDisplay => LastPaymentDate?.ToString("yyyy-MM-dd") ?? "N/A";

        public string FullAddressDisplay => $"{Address}, {City}, {Province} {PostalCode}".Trim(',', ' ');
        
        public string ContactInfoDisplay => $"{PhoneNumber} | {Email}".Trim('|', ' ');

        // ======================================================================
        // INotifyPropertyChanged Implementation
        // ======================================================================
        
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
