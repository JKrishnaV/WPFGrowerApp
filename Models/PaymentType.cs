using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Enumeration of payment types in the unified cheque system
    /// </summary>
    public enum ChequePaymentType
    {
        /// <summary>
        /// Regular batch payment
        /// </summary>
        Regular = 0,

        /// <summary>
        /// Advance cheque payment
        /// </summary>
        Advance = 1,

        /// <summary>
        /// Distribution payment (replaces consolidated payments)
        /// </summary>
        Distribution = 2

        // Note: Consolidated payments have been replaced by payment distributions
        // Distribution payments are now handled as regular payments with IsFromDistribution flag
    }

    /// <summary>
    /// Model representing a payment type with display information
    /// </summary>
    public class PaymentTypeInfo : INotifyPropertyChanged
    {
        private ChequePaymentType _type;
        private string _displayName;
        private string _description;
        private string _icon;

        public ChequePaymentType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
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
