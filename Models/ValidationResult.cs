using System.Collections.Generic;
using System.ComponentModel;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult : INotifyPropertyChanged
    {
        private bool _isValid = true;
        private List<ValidationError> _errors = new List<ValidationError>();
        private List<ValidationError> _warnings = new List<ValidationError>();

        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        public List<ValidationError> Errors
        {
            get => _errors;
            set => SetProperty(ref _errors, value);
        }

        public List<ValidationError> Warnings
        {
            get => _warnings;
            set => SetProperty(ref _warnings, value);
        }

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool HasIssues => HasErrors || HasWarnings;

        public string ErrorSummary
        {
            get
            {
                if (!HasErrors) return string.Empty;
                return $"{Errors.Count} error(s) found";
            }
        }

        public string WarningSummary
        {
            get
            {
                if (!HasWarnings) return string.Empty;
                return $"{Warnings.Count} warning(s) found";
            }
        }

        public void AddError(string fieldName, string message)
        {
            Errors.Add(new ValidationError(fieldName, message, ValidationSeverity.Error));
            IsValid = false;
            OnPropertyChanged(nameof(ErrorSummary));
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(HasIssues));
        }

        public void AddWarning(string fieldName, string message)
        {
            Warnings.Add(new ValidationError(fieldName, message, ValidationSeverity.Warning));
            OnPropertyChanged(nameof(WarningSummary));
            OnPropertyChanged(nameof(HasWarnings));
            OnPropertyChanged(nameof(HasIssues));
        }

        public void Clear()
        {
            Errors.Clear();
            Warnings.Clear();
            IsValid = true;
            OnPropertyChanged(nameof(ErrorSummary));
            OnPropertyChanged(nameof(WarningSummary));
            OnPropertyChanged(nameof(HasErrors));
            OnPropertyChanged(nameof(HasWarnings));
            OnPropertyChanged(nameof(HasIssues));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
