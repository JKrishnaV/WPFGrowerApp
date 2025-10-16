using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class VoidReceiptResult : INotifyPropertyChanged
    {
        private bool _success;
        private bool _requiresConfirmation;
        private string _warningMessage;
        private bool _batchReverted;
        private string _batchNumber;
        private int _paymentBatchId;
        private decimal _amountVoided;
        private List<string> _affectedGrowers = new();
        private string _errorMessage;

        public bool Success
        {
            get => _success;
            set => SetProperty(ref _success, value);
        }

        public bool RequiresConfirmation
        {
            get => _requiresConfirmation;
            set => SetProperty(ref _requiresConfirmation, value);
        }

        public string WarningMessage
        {
            get => _warningMessage;
            set => SetProperty(ref _warningMessage, value);
        }

        public bool BatchReverted
        {
            get => _batchReverted;
            set => SetProperty(ref _batchReverted, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public decimal AmountVoided
        {
            get => _amountVoided;
            set => SetProperty(ref _amountVoided, value);
        }

        public List<string> AffectedGrowers
        {
            get => _affectedGrowers;
            set => SetProperty(ref _affectedGrowers, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
