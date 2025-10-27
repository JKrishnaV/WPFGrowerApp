using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Result object for advance deduction operations
    /// </summary>
    public class DeductionResult : INotifyPropertyChanged
    {
        private bool _isSuccessful;
        private decimal _totalDeductedAmount;
        private int _deductionCount;
        private List<AdvanceDeduction> _deductions;
        private List<string> _warnings;
        private List<string> _errors;
        private string _message;
        private bool _isDeductionFullyApplied;
        private decimal _remainingPaymentAmount;

        public bool IsSuccessful
        {
            get => _isSuccessful;
            set => SetProperty(ref _isSuccessful, value);
        }

        public decimal TotalDeductedAmount
        {
            get => _totalDeductedAmount;
            set => SetProperty(ref _totalDeductedAmount, value);
        }

        public int DeductionCount
        {
            get => _deductionCount;
            set => SetProperty(ref _deductionCount, value);
        }

        public List<AdvanceDeduction> Deductions
        {
            get => _deductions ?? (_deductions = new List<AdvanceDeduction>());
            set => SetProperty(ref _deductions, value);
        }

        public List<string> Warnings
        {
            get => _warnings ?? (_warnings = new List<string>());
            set => SetProperty(ref _warnings, value);
        }

        public List<string> Errors
        {
            get => _errors ?? (_errors = new List<string>());
            set => SetProperty(ref _errors, value);
        }

        public string Message
        {
            get => _message ?? string.Empty;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// Indicates whether the entire deduction amount was successfully applied across advances.
        /// Note: This does NOT mean the payment was absorbed - use RemainingPaymentAmount to check that.
        /// </summary>
        public bool IsDeductionFullyApplied
        {
            get => _isDeductionFullyApplied;
            set => SetProperty(ref _isDeductionFullyApplied, value);
        }

        /// <summary>
        /// Legacy property - use IsDeductionFullyApplied instead
        /// </summary>
        [Obsolete("Use IsDeductionFullyApplied instead")]
        public bool HasFullyAbsorbedPayment
        {
            get => _isDeductionFullyApplied;
            set => _isDeductionFullyApplied = value;
        }

        public decimal RemainingPaymentAmount
        {
            get => _remainingPaymentAmount;
            set => SetProperty(ref _remainingPaymentAmount, value);
        }

        // Computed properties
        public bool HasWarnings => Warnings.Count > 0;
        public bool HasErrors => Errors.Count > 0;
        public decimal NetPaymentAmount => RemainingPaymentAmount;
        public bool IsFullyDeducted => RemainingPaymentAmount <= 0;

        // Display properties
        public string TotalDeductedAmountDisplay => TotalDeductedAmount.ToString("C");
        public string RemainingPaymentAmountDisplay => RemainingPaymentAmount.ToString("C");
        public string NetPaymentAmountDisplay => NetPaymentAmount.ToString("C");
        public string StatusDisplay => IsSuccessful ? "Success" : "Failed";
        public string SummaryDisplay => $"Deductions: {DeductionCount}, Total: {TotalDeductedAmountDisplay}, Remaining: {RemainingPaymentAmountDisplay}";

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