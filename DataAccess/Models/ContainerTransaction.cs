using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a container transaction associated with a receipt.
    /// Tracks containers brought IN or taken OUT during receipt processing.
    /// Replaces the old IN1-IN20, OUT1-OUT20 denormalized column structure.
    /// </summary>
    public class ContainerTransaction : INotifyPropertyChanged
    {
        private int _containerTransactionId;
        private int _receiptId;
        private int _containerId;
        private string _direction = string.Empty;
        private int _quantity;
        private DateTime _createdAt;
        private string _createdBy = string.Empty;
        
        // Navigation properties
        private string? _containerCode;
        private string? _containerName;
        private decimal? _tareWeight;

        public int ContainerTransactionId
        {
            get => _containerTransactionId;
            set => SetProperty(ref _containerTransactionId, value);
        }

        public int ReceiptId
        {
            get => _receiptId;
            set => SetProperty(ref _receiptId, value);
        }

        public int ContainerId
        {
            get => _containerId;
            set => SetProperty(ref _containerId, value);
        }

        /// <summary>
        /// Direction of container movement: "IN" (brought in) or "OUT" (taken out)
        /// </summary>
        public string Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        /// <summary>
        /// Number of containers in this transaction
        /// </summary>
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
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

        // Additional Audit Columns
        private DateTime? _modifiedAt;
        private string? _modifiedBy;
        private DateTime? _deletedAt;
        private string? _deletedBy;

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

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set => SetProperty(ref _deletedAt, value);
        }

        public string? DeletedBy
        {
            get => _deletedBy;
            set => SetProperty(ref _deletedBy, value);
        }

        /// <summary>
        /// Returns true if the record is soft-deleted
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;

        // Navigation properties for display purposes
        public string? ContainerCode
        {
            get => _containerCode;
            set => SetProperty(ref _containerCode, value);
        }

        public string? ContainerName
        {
            get => _containerName;
            set => SetProperty(ref _containerName, value);
        }

        public decimal? TareWeight
        {
            get => _tareWeight;
            set => SetProperty(ref _tareWeight, value);
        }

        // Computed property for display
        public string DisplayText => $"{ContainerCode ?? ContainerId.ToString()}: {Quantity} {Direction}";

        public event PropertyChangedEventHandler? PropertyChanged;

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
