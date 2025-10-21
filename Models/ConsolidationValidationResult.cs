using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing the result of consolidation validation
    /// </summary>
    public class ConsolidationValidationResult : INotifyPropertyChanged
    {
        private bool _isValid;
        private List<string> _warnings;
        private List<string> _errors;
        private string _message;

        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
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

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        // Computed properties
        public bool HasWarnings => Warnings?.Count > 0;
        public bool HasErrors => Errors?.Count > 0;
        public string StatusDisplay => IsValid ? "Valid" : "Invalid";
        public string SummaryDisplay => GetSummaryDisplay();

        public ConsolidationValidationResult()
        {
            Warnings = new List<string>();
            Errors = new List<string>();
            IsValid = true;
        }

        public ConsolidationValidationResult(bool isValid, string message) : this()
        {
            IsValid = isValid;
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
            IsValid = false;
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

        private string GetSummaryDisplay()
        {
            if (HasErrors)
                return $"Invalid: {Errors.Count} error{(Errors.Count != 1 ? "s" : "")}";
            
            if (HasWarnings)
                return $"Valid with {Warnings.Count} warning{(Warnings.Count != 1 ? "s" : "")}";
            
            return "Valid";
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
