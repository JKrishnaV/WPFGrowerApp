using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing a request to void a payment
    /// </summary>
    public class PaymentVoidRequest : INotifyPropertyChanged
    {
        private string _entityType;
        private int _entityId;
        private string _reason;
        private string _voidedBy;
        private DateTime _voidedAt;
        private bool _reverseDeductions;
        private bool _restoreBatchStatus;

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

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public string VoidedBy
        {
            get => _voidedBy;
            set => SetProperty(ref _voidedBy, value);
        }

        public DateTime VoidedAt
        {
            get => _voidedAt;
            set => SetProperty(ref _voidedAt, value);
        }

        public bool ReverseDeductions
        {
            get => _reverseDeductions;
            set => SetProperty(ref _reverseDeductions, value);
        }

        public bool RestoreBatchStatus
        {
            get => _restoreBatchStatus;
            set => SetProperty(ref _restoreBatchStatus, value);
        }

        // Display properties
        public string EntityDisplay => $"{EntityType} #{EntityId}";
        public string DateDisplay => VoidedAt.ToString("MMM dd, yyyy HH:mm");

        public PaymentVoidRequest()
        {
            VoidedAt = DateTime.Now;
            ReverseDeductions = true;
            RestoreBatchStatus = true;
        }

        public PaymentVoidRequest(string entityType, int entityId, string reason, string voidedBy) : this()
        {
            EntityType = entityType;
            EntityId = entityId;
            Reason = reason;
            VoidedBy = voidedBy;
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
