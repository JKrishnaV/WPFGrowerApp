using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// UI model for advance deduction items in payment distribution
    /// </summary>
    public class AdvanceDeductionItem : INotifyPropertyChanged
    {
        private int _advanceChequeId;
        private string _advanceDate;
        private decimal _originalAmount;
        private decimal _remainingAmount;
        private decimal _suggestedDeduction;
        private decimal _actualDeduction;
        private bool _canModify;
        private bool _isSelected;
        private string _status;
        private string _reason;

        public int AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        public string AdvanceDate
        {
            get => _advanceDate;
            set => SetProperty(ref _advanceDate, value);
        }

        public decimal OriginalAmount
        {
            get => _originalAmount;
            set => SetProperty(ref _originalAmount, value);
        }

        public decimal RemainingAmount
        {
            get => _remainingAmount;
            set => SetProperty(ref _remainingAmount, value);
        }

        public decimal SuggestedDeduction
        {
            get => _suggestedDeduction;
            set => SetProperty(ref _suggestedDeduction, value);
        }

        public decimal ActualDeduction
        {
            get => _actualDeduction;
            set
            {
                if (SetProperty(ref _actualDeduction, value))
                {
                    OnPropertyChanged(nameof(IsModified));
                    OnPropertyChanged(nameof(ActualDeductionDisplay));
                    OnPropertyChanged(nameof(RemainingAfterDeduction));
                    OnPropertyChanged(nameof(RemainingAfterDeductionDisplay));
                    OnPropertyChanged(nameof(IsValidDeduction));
                    OnPropertyChanged(nameof(ValidationMessage));
                }
            }
        }

        public bool CanModify
        {
            get => _canModify;
            set => SetProperty(ref _canModify, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        // Computed properties
        public bool IsModified => Math.Abs(ActualDeduction - SuggestedDeduction) > 0.01m;
        public decimal RemainingAfterDeduction => RemainingAmount - ActualDeduction;
        public bool IsValidDeduction => ActualDeduction >= 0 && ActualDeduction <= RemainingAmount;
        public bool IsFullyDeducted => ActualDeduction >= RemainingAmount;
        public decimal DeductionPercentage => RemainingAmount > 0 ? (ActualDeduction / RemainingAmount) * 100 : 0;

        // Display properties
        public string OriginalAmountDisplay => OriginalAmount.ToString("C");
        public string RemainingAmountDisplay => RemainingAmount.ToString("C");
        public string SuggestedDeductionDisplay => SuggestedDeduction.ToString("C");
        public string ActualDeductionDisplay => ActualDeduction.ToString("C");
        public string RemainingAfterDeductionDisplay => RemainingAfterDeduction.ToString("C");
        public string DeductionPercentageDisplay => $"{DeductionPercentage:F1}%";
        public string StatusDisplay => Status ?? "Active";
        public string ValidationMessage => IsValidDeduction ? string.Empty : "Deduction amount exceeds remaining balance";

        // Status indicators
        public bool ShowWarning => !IsValidDeduction;
        public bool ShowSuccess => IsValidDeduction && ActualDeduction > 0;
        public bool ShowInfo => IsValidDeduction && ActualDeduction == 0;

        public void ResetToSuggested()
        {
            ActualDeduction = SuggestedDeduction;
        }

        public void SetToRemaining()
        {
            ActualDeduction = RemainingAmount;
        }

        public void ClearDeduction()
        {
            ActualDeduction = 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
