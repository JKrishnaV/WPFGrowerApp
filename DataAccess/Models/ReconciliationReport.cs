using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a reconciliation report for payment distributions.
    /// Compares expected vs actual payments and identifies discrepancies.
    /// </summary>
    public class ReconciliationReport : INotifyPropertyChanged
    {
        private int _reportId;
        private int _distributionId;
        private string _distributionNumber = string.Empty;
        private DateTime _reportDate;
        private decimal _expectedAmount;
        private decimal _actualAmount;
        private decimal _variance;
        private int _expectedPayments;
        private int _actualPayments;
        private int _missingPayments;
        private int _duplicatePayments;
        private List<PaymentException> _exceptions = new();
        private string _status = string.Empty;
        private string _generatedBy = string.Empty;

        public int DistributionId
        {
            get => _distributionId;
            set => SetProperty(ref _distributionId, value);
        }

        public string DistributionNumber
        {
            get => _distributionNumber;
            set => SetProperty(ref _distributionNumber, value);
        }

        public DateTime ReportDate
        {
            get => _reportDate;
            set => SetProperty(ref _reportDate, value);
        }

        public decimal ExpectedAmount
        {
            get => _expectedAmount;
            set => SetProperty(ref _expectedAmount, value);
        }

        public decimal ActualAmount
        {
            get => _actualAmount;
            set => SetProperty(ref _actualAmount, value);
        }

        public decimal Variance
        {
            get => _variance;
            set => SetProperty(ref _variance, value);
        }

        public int ExpectedPayments
        {
            get => _expectedPayments;
            set => SetProperty(ref _expectedPayments, value);
        }

        public int ActualPayments
        {
            get => _actualPayments;
            set => SetProperty(ref _actualPayments, value);
        }

        public int MissingPayments
        {
            get => _missingPayments;
            set => SetProperty(ref _missingPayments, value);
        }

        public int DuplicatePayments
        {
            get => _duplicatePayments;
            set => SetProperty(ref _duplicatePayments, value);
        }

        public List<PaymentException> Exceptions
        {
            get => _exceptions;
            set => SetProperty(ref _exceptions, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string GeneratedBy
        {
            get => _generatedBy;
            set => SetProperty(ref _generatedBy, value);
        }

        public int ReportId
        {
            get => _reportId;
            set => SetProperty(ref _reportId, value);
        }

        public decimal Difference
        {
            get => _variance;
            set => SetProperty(ref _variance, value);
        }

        public int PaymentDistributionId
        {
            get => _distributionId;
            set => SetProperty(ref _distributionId, value);
        }

        public DateTime GeneratedAt
        {
            get => _reportDate;
            set => SetProperty(ref _reportDate, value);
        }

        // Calculated properties
        public bool IsBalanced => Math.Abs(_variance) < 0.01m;
        public bool HasExceptions => _exceptions.Count > 0;
        public string VarianceDisplay => _variance >= 0 ? $"+${_variance:N2}" : $"-${Math.Abs(_variance):N2}";
        public double CompletionPercentage => _expectedPayments > 0 ? (double)_actualPayments / _expectedPayments * 100 : 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
