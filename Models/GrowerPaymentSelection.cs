using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing a grower's payment method selection in the distribution view
    /// </summary>
    public class GrowerPaymentSelection : INotifyPropertyChanged
    {
        private int _growerId;
        private string _growerName;
        private string _growerNumber;
        private ChequePaymentType _selectedPaymentType;
        private decimal _regularAmount;
        private decimal _consolidatedAmount;
        private bool _canBeConsolidated;
        private bool _hasOutstandingAdvances;
        private decimal _outstandingAdvances;
        private string _recommendedPaymentType;
        private string _reason;
        private bool _isSelectedForPayment = true; // Default to checked
        private decimal _advanceDeductionAmount;
        private decimal _deductFromThisTransaction;
        private decimal _remainingDeductions;

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public string GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public ChequePaymentType SelectedPaymentType
        {
            get => _selectedPaymentType;
            set => SetProperty(ref _selectedPaymentType, value);
        }

        public decimal RegularAmount
        {
            get => _regularAmount;
            set => SetProperty(ref _regularAmount, value);
        }

        public decimal ConsolidatedAmount
        {
            get => _consolidatedAmount;
            set 
            {
                if (SetProperty(ref _consolidatedAmount, value))
                {
                    // Trigger property change notification for calculated properties
                    OnPropertyChanged(nameof(NetPaymentAmount));
                    OnPropertyChanged(nameof(NetConsolidatedAmountDisplay));
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public bool CanBeConsolidated
        {
            get => _canBeConsolidated;
            set => SetProperty(ref _canBeConsolidated, value);
        }

        public bool HasOutstandingAdvances
        {
            get => _hasOutstandingAdvances;
            set => SetProperty(ref _hasOutstandingAdvances, value);
        }

        public decimal OutstandingAdvances
        {
            get => _outstandingAdvances;
            set 
            {
                if (SetProperty(ref _outstandingAdvances, value))
                {
                    // Initialize deduction amounts when outstanding advances are set
                    if (value > 0)
                    {
                        // Default to deducting the full amount if it's less than or equal to gross amount
                        DeductFromThisTransaction = Math.Min(value, ConsolidatedAmount);
                        RemainingDeductions = value - DeductFromThisTransaction;
                    }
                    else
                    {
                        DeductFromThisTransaction = 0;
                        RemainingDeductions = 0;
                    }
                }
            }
        }

        public string RecommendedPaymentType
        {
            get => _recommendedPaymentType;
            set => SetProperty(ref _recommendedPaymentType, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public bool IsSelectedForPayment
        {
            get => _isSelectedForPayment;
            set => SetProperty(ref _isSelectedForPayment, value);
        }

        public decimal AdvanceDeductionAmount
        {
            get => _advanceDeductionAmount;
            set => SetProperty(ref _advanceDeductionAmount, value);
        }

        public decimal NetPaymentAmount
        {
            get => ConsolidatedAmount - DeductFromThisTransaction;
        }

        public decimal DeductFromThisTransaction
        {
            get => _deductFromThisTransaction;
            set 
            {
                if (SetProperty(ref _deductFromThisTransaction, value))
                {
                    // Validate the deduction amount - cannot be negative and cannot exceed gross amount
                    if (value < 0)
                        _deductFromThisTransaction = 0;
                    else
                    {
                        // Cap at the minimum of OutstandingAdvances and ConsolidatedAmount
                        // This prevents deducting more than the gross payment amount
                        var maxAllowed = Math.Min(OutstandingAdvances, ConsolidatedAmount);
                        if (value > maxAllowed)
                            _deductFromThisTransaction = maxAllowed;
                    }
                    
                    // Update remaining deductions (how much is left after this transaction)
                    RemainingDeductions = OutstandingAdvances - _deductFromThisTransaction;
                    
                    // Trigger property change notifications
                    OnPropertyChanged(nameof(NetPaymentAmount));
                    OnPropertyChanged(nameof(NetConsolidatedAmountDisplay));
                    OnPropertyChanged(nameof(StatusDisplay));
                }
            }
        }

        public decimal RemainingDeductions
        {
            get => _remainingDeductions;
            set 
            {
                if (SetProperty(ref _remainingDeductions, value))
                {
                    // Trigger property change notification for display property
                    OnPropertyChanged(nameof(RemainingDeductionsDisplay));
                }
            }
        }

        // Computed properties
        public string GrowerDisplay => $"{GrowerNumber} - {GrowerName}";
        public string RegularAmountDisplay => RegularAmount.ToString("C");
        public string ConsolidatedAmountDisplay => ConsolidatedAmount.ToString("C");
        public string OutstandingAdvancesDisplay => OutstandingAdvances.ToString("C");
        public string DeductFromThisTransactionDisplay => DeductFromThisTransaction.ToString("C");
        public string RemainingDeductionsDisplay => RemainingDeductions.ToString("C");
        public decimal NetRegularAmount => RegularAmount - OutstandingAdvances;
        public decimal NetConsolidatedAmount => ConsolidatedAmount - OutstandingAdvances;
        
        // Properties for proper advance deduction logic (cap deduction at payment amount)
        public decimal ActualDeductionAmount => Math.Min(ConsolidatedAmount, OutstandingAdvances);
        public decimal ActualNetAmount => ConsolidatedAmount - ActualDeductionAmount;
        public decimal RemainingAdvanceAmount => Math.Max(0, OutstandingAdvances - ConsolidatedAmount);
        public decimal TotalAmount => NetConsolidatedAmount + OutstandingAdvances;
        public decimal GrossTotalAmount => NetConsolidatedAmount + OutstandingAdvances;
        public string NetRegularAmountDisplay => NetRegularAmount.ToString("C");
        public string NetConsolidatedAmountDisplay => NetPaymentAmount.ToString("C");
        public string GrossTotalAmountDisplay => GrossTotalAmount.ToString("C");
        public bool IsConsolidated => SelectedPaymentType == ChequePaymentType.Distribution;
        public bool IsRegular => SelectedPaymentType == ChequePaymentType.Regular;
        public bool HasExcessiveAdvances => OutstandingAdvances > ConsolidatedAmount;
        public decimal ExcessAdvanceAmount => Math.Max(0, OutstandingAdvances - ConsolidatedAmount);
        public string PaymentTypeDisplay => GetPaymentTypeDisplay();
        public string StatusDisplay => GetStatusDisplay();

        public GrowerPaymentSelection()
        {
            SelectedPaymentType = ChequePaymentType.Regular;
            RecommendedPaymentType = "Regular";
            DeductFromThisTransaction = 0;
            RemainingDeductions = 0;
        }

        public GrowerPaymentSelection(int growerId, string growerName, string growerNumber) : this()
        {
            GrowerId = growerId;
            GrowerName = growerName;
            GrowerNumber = growerNumber;
        }

        public void SetRecommendedPaymentType()
        {
            if (CanBeConsolidated && ConsolidatedAmount > RegularAmount)
            {
                RecommendedPaymentType = "Consolidated";
                Reason = "Higher amount available through consolidation";
            }
            else if (HasOutstandingAdvances)
            {
                RecommendedPaymentType = "Regular";
                Reason = "Has outstanding advances that will be deducted";
            }
            else
            {
                RecommendedPaymentType = "Regular";
                Reason = "Standard batch payment";
            }
        }

        public void ApplyRecommendation()
        {
            if (RecommendedPaymentType == "Consolidated")
                SelectedPaymentType = ChequePaymentType.Distribution;
            else
                SelectedPaymentType = ChequePaymentType.Regular;
        }

        private string GetPaymentTypeDisplay()
        {
            return SelectedPaymentType switch
            {
                ChequePaymentType.Regular => "Regular Batch",
                ChequePaymentType.Distribution => "Consolidated",
                _ => "Unknown"
            };
        }

        private string GetStatusDisplay()
        {
            if (HasOutstandingAdvances)
            {
                if (DeductFromThisTransaction > ConsolidatedAmount)
                {
                    var excess = DeductFromThisTransaction - ConsolidatedAmount;
                    return $"▲ EXCESSIVE DEDUCTION: {excess:C} over gross";
                }
                else if (RemainingDeductions > 0)
                {
                    return $"Outstanding Advances: {OutstandingAdvances:C} (Deducting {DeductFromThisTransaction:C}, {RemainingDeductions:C} remaining)";
                }
                else
                {
                    return $"All advances deducted: {OutstandingAdvances:C}";
                }
            }
            
            if (CanBeConsolidated)
                return "Can be consolidated";
            
            return "Standard payment";
        }

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
