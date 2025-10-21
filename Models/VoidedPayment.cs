using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing a voided payment
    /// </summary>
    public class VoidedPayment : INotifyPropertyChanged
    {
        private string _entityType;
        private int _entityId;
        private DateTime _voidedAt;
        private string _voidedBy;
        private string _reason;
        private decimal _amount;

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

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        // Display properties
        public string EntityDisplay => $"{EntityType} #{EntityId}";
        public string AmountDisplay => Amount.ToString("C");
        public string DateDisplay => VoidedAt.ToString("MMM dd, yyyy HH:mm");
        public string TypeDisplay => GetTypeDisplay();
        public string StatusDisplay => "Voided";

        public VoidedPayment()
        {
        }

        public VoidedPayment(string entityType, int entityId, DateTime voidedAt, string voidedBy, string reason, decimal amount)
        {
            EntityType = entityType;
            EntityId = entityId;
            VoidedAt = voidedAt;
            VoidedBy = voidedBy;
            Reason = reason;
            Amount = amount;
        }

        private string GetTypeDisplay()
        {
            return EntityType switch
            {
                "Regular" => "Regular Payment",
                "Advance" => "Advance Cheque",
                "Consolidated" => "Consolidated Payment",
                _ => EntityType
            };
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
