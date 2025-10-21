using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Model representing an audit log entry for payment changes
    /// </summary>
    public class PaymentAuditLog : INotifyPropertyChanged
    {
        private int _auditLogId;
        private string _entityType;
        private int _entityId;
        private string _action;
        private string _oldValues;
        private string _newValues;
        private string _changedBy;
        private DateTime _changedAt;
        private string _reason;

        public int AuditLogId
        {
            get => _auditLogId;
            set => SetProperty(ref _auditLogId, value);
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

        public string Action
        {
            get => _action;
            set => SetProperty(ref _action, value);
        }

        public string OldValues
        {
            get => _oldValues;
            set => SetProperty(ref _oldValues, value);
        }

        public string NewValues
        {
            get => _newValues;
            set => SetProperty(ref _newValues, value);
        }

        public string ChangedBy
        {
            get => _changedBy;
            set => SetProperty(ref _changedBy, value);
        }

        public DateTime ChangedAt
        {
            get => _changedAt;
            set => SetProperty(ref _changedAt, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        // Display properties
        public string DateDisplay => ChangedAt.ToString("MMM dd, yyyy HH:mm");
        public string ActionDisplay => Action;
        public string EntityDisplay => $"{EntityType} #{EntityId}";

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
