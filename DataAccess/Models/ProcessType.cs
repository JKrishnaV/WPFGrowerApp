using System;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    public class ProcessType : INotifyPropertyChanged
    {
        private int _processTypeId;
        private string _processTypeName = string.Empty;
        private string _description = string.Empty;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.Now;
        private string _createdBy = string.Empty;
        private DateTime? _modifiedAt;
        private string _modifiedBy = string.Empty;
        private DateTime? _deletedAt;
        private string _deletedBy = string.Empty;

        public int ProcessTypeId
        {
            get => _processTypeId;
            set => SetProperty(ref _processTypeId, value);
        }

        public string ProcessTypeName
        {
            get => _processTypeName;
            set => SetProperty(ref _processTypeName, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
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

        public string ModifiedBy
        {
            get => _modifiedBy;
            set => SetProperty(ref _modifiedBy, value);
        }

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set => SetProperty(ref _deletedAt, value);
        }

        public string DeletedBy
        {
            get => _deletedBy;
            set => SetProperty(ref _deletedBy, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, string propertyName = "")
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
