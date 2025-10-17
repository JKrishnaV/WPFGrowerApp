using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents cheque delivery tracking information.
    /// Tracks the delivery status and details of cheques sent to growers.
    /// </summary>
    public class ChequeDelivery : INotifyPropertyChanged
    {
        private int _deliveryId;
        private int _chequeId;
        private DateTime _mailedDate;
        private string? _trackingNumber;
        private string _deliveryMethod = string.Empty;
        private string _status = string.Empty;
        private DateTime? _deliveredDate;
        private string? _deliveredBy;
        private string? _receivedBy;
        private string? _notes;
        private DateTime _createdAt;
        private string _createdBy = string.Empty;
        private DateTime? _modifiedAt;
        private string? _modifiedBy;

        // Navigation properties
        private Cheque? _cheque;

        public int DeliveryId
        {
            get => _deliveryId;
            set => SetProperty(ref _deliveryId, value);
        }

        public int ChequeId
        {
            get => _chequeId;
            set => SetProperty(ref _chequeId, value);
        }

        public DateTime MailedDate
        {
            get => _mailedDate;
            set => SetProperty(ref _mailedDate, value);
        }

        public string? TrackingNumber
        {
            get => _trackingNumber;
            set => SetProperty(ref _trackingNumber, value);
        }

        public string DeliveryMethod
        {
            get => _deliveryMethod;
            set => SetProperty(ref _deliveryMethod, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime? DeliveredDate
        {
            get => _deliveredDate;
            set => SetProperty(ref _deliveredDate, value);
        }

        public string? DeliveredBy
        {
            get => _deliveredBy;
            set => SetProperty(ref _deliveredBy, value);
        }

        public string? ReceivedBy
        {
            get => _receivedBy;
            set => SetProperty(ref _receivedBy, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set => SetProperty(ref _modifiedAt, value);
        }

        public string? ModifiedBy
        {
            get => _modifiedBy;
            set => SetProperty(ref _modifiedBy, value);
        }

        // Navigation property
        public Cheque? Cheque
        {
            get => _cheque;
            set => SetProperty(ref _cheque, value);
        }


        // Helper properties
        public bool CanBeDelivered => Status == "Mailed" || Status == "In Transit";
        public bool CanBeReturned => Status == "Mailed" || Status == "In Transit" || Status == "Delivered";
        public bool IsOverdue => Status == "Mailed" && DateTime.Now.Subtract(MailedDate).Days > 7;

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
