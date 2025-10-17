using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents payment exceptions that require attention during reconciliation.
    /// Tracks missing payments, duplicates, and other discrepancies.
    /// </summary>
    public class PaymentException : INotifyPropertyChanged
    {
        private int _exceptionId;
        private int _distributionId;
        private string _exceptionType = string.Empty;
        private string _description = string.Empty;
        private string _severity = string.Empty;
        private string _status = string.Empty;
        private string? _resolution;
        private string? _resolvedBy;
        private DateTime? _resolvedAt;
        private DateTime _createdAt;
        private string _createdBy = string.Empty;
        private int? _chequeId;
        private int? _electronicPaymentId;

        public int ExceptionId
        {
            get => _exceptionId;
            set => SetProperty(ref _exceptionId, value);
        }

        public int DistributionId
        {
            get => _distributionId;
            set => SetProperty(ref _distributionId, value);
        }

        public string ExceptionType
        {
            get => _exceptionType;
            set => SetProperty(ref _exceptionType, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Severity
        {
            get => _severity;
            set => SetProperty(ref _severity, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string? Resolution
        {
            get => _resolution;
            set => SetProperty(ref _resolution, value);
        }

        public string? ResolvedBy
        {
            get => _resolvedBy;
            set => SetProperty(ref _resolvedBy, value);
        }

        public DateTime? ResolvedAt
        {
            get => _resolvedAt;
            set => SetProperty(ref _resolvedAt, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public int PaymentDistributionId
        {
            get => _distributionId;
            set => SetProperty(ref _distributionId, value);
        }



        // Helper properties
        public bool IsResolved => _status == "Resolved";
        public bool IsHighSeverity => _severity == "High";
        public bool IsOverdue => !IsResolved && DateTime.Now.Subtract(_createdAt).Days > 3;
        public string AgeDisplay => GetAgeDisplay();

        private string GetAgeDisplay()
        {
            var age = DateTime.Now.Subtract(_createdAt);
            if (age.TotalDays >= 1)
                return $"{(int)age.TotalDays} days";
            if (age.TotalHours >= 1)
                return $"{(int)age.TotalHours} hours";
            return $"{(int)age.TotalMinutes} minutes";
        }

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
