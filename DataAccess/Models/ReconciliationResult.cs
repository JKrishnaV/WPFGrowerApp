using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Result of a reconciliation operation
    /// </summary>
    public class ReconciliationResult : INotifyPropertyChanged
    {
        private bool _isSuccessful;
        private int _fixedCount;
        private List<string> _errors;
        private string _message;

        public bool IsSuccessful
        {
            get => _isSuccessful;
            set => SetProperty(ref _isSuccessful, value);
        }

        public int FixedCount
        {
            get => _fixedCount;
            set => SetProperty(ref _fixedCount, value);
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
