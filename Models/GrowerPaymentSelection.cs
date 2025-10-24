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
            set => SetProperty(ref _consolidatedAmount, value);
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
            set => SetProperty(ref _outstandingAdvances, value);
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

        // Computed properties
        public string GrowerDisplay => $"{GrowerNumber} - {GrowerName}";
        public string RegularAmountDisplay => RegularAmount.ToString("C");
        public string ConsolidatedAmountDisplay => ConsolidatedAmount.ToString("C");
        public string OutstandingAdvancesDisplay => OutstandingAdvances.ToString("C");
        public decimal NetRegularAmount => RegularAmount - OutstandingAdvances;
        public decimal NetConsolidatedAmount => ConsolidatedAmount - OutstandingAdvances;
        public decimal GrossTotalAmount => NetConsolidatedAmount + OutstandingAdvances;
        public string NetRegularAmountDisplay => NetRegularAmount.ToString("C");
        public string NetConsolidatedAmountDisplay => NetConsolidatedAmount.ToString("C");
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
            if (HasExcessiveAdvances)
                return $"⚠️ EXCESSIVE ADVANCES: {ExcessAdvanceAmount:C} over gross";
            
            if (HasOutstandingAdvances)
                return $"Outstanding Advances: {OutstandingAdvancesDisplay}";
            
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
