using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Information about a grower's advance status for payment processing
    /// </summary>
    public class GrowerAdvanceInfo : INotifyPropertyChanged
    {
        private int _growerId;
        private string _growerNumber;
        private string _growerName;
        private decimal _paymentAmount;
        private decimal _suggestedDeductionAmount;
        private decimal _actualDeductionAmount;
        private decimal _remainingPaymentAmount;
        private List<AdvanceCheque> _outstandingAdvances;
        private List<AdvanceDeduction> _suggestedDeductions;
        private bool _hasOutstandingAdvances;
        private bool _isFullyDeducted;
        private bool _canModifyDeductions;

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

        public string GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set => SetProperty(ref _paymentAmount, value);
        }

        public decimal SuggestedDeductionAmount
        {
            get => _suggestedDeductionAmount;
            set => SetProperty(ref _suggestedDeductionAmount, value);
        }

        public decimal ActualDeductionAmount
        {
            get => _actualDeductionAmount;
            set => SetProperty(ref _actualDeductionAmount, value);
        }

        public decimal RemainingPaymentAmount
        {
            get => _remainingPaymentAmount;
            set => SetProperty(ref _remainingPaymentAmount, value);
        }

        public List<AdvanceCheque> OutstandingAdvances
        {
            get => _outstandingAdvances ?? (_outstandingAdvances = new List<AdvanceCheque>());
            set => SetProperty(ref _outstandingAdvances, value);
        }

        public List<AdvanceDeduction> SuggestedDeductions
        {
            get => _suggestedDeductions ?? (_suggestedDeductions = new List<AdvanceDeduction>());
            set => SetProperty(ref _suggestedDeductions, value);
        }

        public bool HasOutstandingAdvances
        {
            get => _hasOutstandingAdvances;
            set => SetProperty(ref _hasOutstandingAdvances, value);
        }

        public bool IsFullyDeducted
        {
            get => _isFullyDeducted;
            set => SetProperty(ref _isFullyDeducted, value);
        }

        public bool CanModifyDeductions
        {
            get => _canModifyDeductions;
            set => SetProperty(ref _canModifyDeductions, value);
        }

        // Computed properties
        public decimal TotalOutstandingAdvances => OutstandingAdvances.Sum(a => a.CurrentAdvanceAmount);
        public bool IsPartiallyDeducted => ActualDeductionAmount > 0 && RemainingPaymentAmount > 0;
        public decimal DeductionPercentage => PaymentAmount > 0 ? (ActualDeductionAmount / PaymentAmount) * 100 : 0;
        public bool WillGenerateCheque => RemainingPaymentAmount > 0;
        public bool WillSkipPrint => IsFullyDeducted || RemainingPaymentAmount <= 0;

        // Display properties
        public string PaymentAmountDisplay => PaymentAmount.ToString("C");
        public string SuggestedDeductionDisplay => SuggestedDeductionAmount.ToString("C");
        public string ActualDeductionDisplay => ActualDeductionAmount.ToString("C");
        public string RemainingPaymentDisplay => RemainingPaymentAmount.ToString("C");
        public string TotalOutstandingDisplay => TotalOutstandingAdvances.ToString("C");
        public string DeductionPercentageDisplay => $"{DeductionPercentage:F1}%";
        public string StatusDisplay => IsFullyDeducted ? "Fully Deducted" : (IsPartiallyDeducted ? "Partially Deducted" : "No Deductions");
        public string ChequeStatusDisplay => WillGenerateCheque ? "Will Generate Cheque" : "No Cheque (Fully Deducted)";

        public void AddOutstandingAdvance(AdvanceCheque advance)
        {
            OutstandingAdvances.Add(advance);
            HasOutstandingAdvances = OutstandingAdvances.Count > 0;
            OnPropertyChanged(nameof(TotalOutstandingAdvances));
            OnPropertyChanged(nameof(TotalOutstandingDisplay));
        }

        public void AddSuggestedDeduction(AdvanceDeduction deduction)
        {
            SuggestedDeductions.Add(deduction);
            OnPropertyChanged(nameof(SuggestedDeductions));
        }

        public void UpdateDeductionAmounts()
        {
            SuggestedDeductionAmount = SuggestedDeductions.Sum(d => d.DeductionAmount);
            ActualDeductionAmount = SuggestedDeductionAmount; // Default to suggested
            RemainingPaymentAmount = PaymentAmount - ActualDeductionAmount;
            IsFullyDeducted = RemainingPaymentAmount <= 0;
            
            OnPropertyChanged(nameof(IsPartiallyDeducted));
            OnPropertyChanged(nameof(WillGenerateCheque));
            OnPropertyChanged(nameof(WillSkipPrint));
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(ChequeStatusDisplay));
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
