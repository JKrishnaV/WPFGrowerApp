using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class PaymentSettings : INotifyPropertyChanged
    {
        private bool _allowVoidAfterPosted = true;
        private bool _allowVoidAfterFinalized = false;
        private bool _autoRevertBatchOnVoid = true;
        private bool _requireVoidConfirmation = true;
        private string _voidConfirmationMessage = "This will void the receipt and revert the batch to Draft status. Continue?";

        public bool AllowVoidAfterPosted
        {
            get => _allowVoidAfterPosted;
            set => SetProperty(ref _allowVoidAfterPosted, value);
        }

        public bool AllowVoidAfterFinalized
        {
            get => _allowVoidAfterFinalized;
            set => SetProperty(ref _allowVoidAfterFinalized, value);
        }

        public bool AutoRevertBatchOnVoid
        {
            get => _autoRevertBatchOnVoid;
            set => SetProperty(ref _autoRevertBatchOnVoid, value);
        }

        public bool RequireVoidConfirmation
        {
            get => _requireVoidConfirmation;
            set => SetProperty(ref _requireVoidConfirmation, value);
        }

        public string VoidConfirmationMessage
        {
            get => _voidConfirmationMessage;
            set => SetProperty(ref _voidConfirmationMessage, value);
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
