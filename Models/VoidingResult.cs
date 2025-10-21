using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing the result of a voiding operation
    /// </summary>
    public class VoidingResult : INotifyPropertyChanged
    {
        private bool _success;
        private string _message;
        private string _entityType;
        private int _entityId;
        private DateTime _voidedAt;
        private string _voidedBy;
        private List<string> _warnings;
        private List<string> _errors;
        private decimal _amountReversed;
        private bool _deductionsReversed;
        private bool _batchStatusRestored;

        public bool Success
        {
            get => _success;
            set => SetProperty(ref _success, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string EntityType
        {
            get => _entityType;
            set => SetProperty(ref _entityType, value);
        }

        public int EntityId
        {
            get => _entityId;
            set => SetProperty(ref _entityId, value);
        }

        public DateTime VoidedAt
        {
            get => _voidedAt;
            set => SetProperty(ref _voidedAt, value);
        }

        public string VoidedBy
        {
            get => _voidedBy;
            set => SetProperty(ref _voidedBy, value);
        }

        public List<string> Warnings
        {
            get => _warnings;
            set => SetProperty(ref _warnings, value);
        }

        public List<string> Errors
        {
            get => _errors;
            set => SetProperty(ref _errors, value);
        }

        public decimal AmountReversed
        {
            get => _amountReversed;
            set => SetProperty(ref _amountReversed, value);
        }

        public bool DeductionsReversed
        {
            get => _deductionsReversed;
            set => SetProperty(ref _deductionsReversed, value);
        }

        public bool BatchStatusRestored
        {
            get => _batchStatusRestored;
            set => SetProperty(ref _batchStatusRestored, value);
        }

        // Computed properties
        public string EntityDisplay => $"{EntityType} #{EntityId}";
        public string DateDisplay => VoidedAt.ToString("MMM dd, yyyy HH:mm");
        public string AmountReversedDisplay => AmountReversed.ToString("C");
        public bool HasWarnings => Warnings?.Count > 0;
        public bool HasErrors => Errors?.Count > 0;
        public string StatusDisplay => Success ? "Success" : "Failed";

        public VoidingResult()
        {
            Warnings = new List<string>();
            Errors = new List<string>();
            VoidedAt = DateTime.Now;
        }

        public VoidingResult(bool success, string message) : this()
        {
            Success = success;
            Message = message;
        }

        public void AddWarning(string warning)
        {
            if (string.IsNullOrEmpty(warning)) return;
            Warnings.Add(warning);
        }

        public void AddError(string error)
        {
            if (string.IsNullOrEmpty(error)) return;
            Errors.Add(error);
        }

        public void AddWarnings(IEnumerable<string> warnings)
        {
            if (warnings == null) return;
            foreach (var warning in warnings)
                AddWarning(warning);
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            if (errors == null) return;
            foreach (var error in errors)
                AddError(error);
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
