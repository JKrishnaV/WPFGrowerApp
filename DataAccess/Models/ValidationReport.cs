using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Comprehensive validation report
    /// </summary>
    public class ValidationReport : INotifyPropertyChanged
    {
        private AdvanceValidationResult _advanceBalanceValidation;
        private AdvanceValidationResult _deductionTotalValidation;
        private AdvanceValidationResult _orphanedDeductionValidation;
        private bool _overallIsValid;
        private DateTime _generatedAt;
        private string _generatedBy;

        public AdvanceValidationResult AdvanceBalanceValidation
        {
            get => _advanceBalanceValidation ?? (_advanceBalanceValidation = new AdvanceValidationResult());
            set => SetProperty(ref _advanceBalanceValidation, value);
        }

        public AdvanceValidationResult DeductionTotalValidation
        {
            get => _deductionTotalValidation ?? (_deductionTotalValidation = new AdvanceValidationResult());
            set => SetProperty(ref _deductionTotalValidation, value);
        }

        public AdvanceValidationResult OrphanedDeductionValidation
        {
            get => _orphanedDeductionValidation ?? (_orphanedDeductionValidation = new AdvanceValidationResult());
            set => SetProperty(ref _orphanedDeductionValidation, value);
        }

        public bool OverallIsValid
        {
            get => _overallIsValid;
            set => SetProperty(ref _overallIsValid, value);
        }

        public DateTime GeneratedAt
        {
            get => _generatedAt;
            set => SetProperty(ref _generatedAt, value);
        }

        public string GeneratedBy
        {
            get => _generatedBy ?? string.Empty;
            set => SetProperty(ref _generatedBy, value);
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
