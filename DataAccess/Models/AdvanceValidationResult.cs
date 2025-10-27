using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Result of an advance deduction validation operation
    /// </summary>
    public class AdvanceValidationResult : INotifyPropertyChanged
    {
        private bool _isValid;
        private List<dynamic> _discrepancies;
        private string _message;

        public bool IsValid
        {
            get => _isValid;
            set => SetProperty(ref _isValid, value);
        }

        public List<dynamic> Discrepancies
        {
            get => _discrepancies ?? (_discrepancies = new List<dynamic>());
            set => SetProperty(ref _discrepancies, value);
        }

        public string Message
        {
            get => _message ?? string.Empty;
            set => SetProperty(ref _message, value);
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
