using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a cheque series for organizing cheques by type or purpose.
    /// </summary>
    public class ChequeSeries : INotifyPropertyChanged
    {
        private int _seriesId;
        private string _seriesName = string.Empty;
        private string _seriesCode = string.Empty;
        private string _description = string.Empty;
        private bool _isActive = true;
        private DateTime _createdAt;
        private string _createdBy = string.Empty;

        public int SeriesId
        {
            get => _seriesId;
            set => SetProperty(ref _seriesId, value);
        }

        public string SeriesName
        {
            get => _seriesName;
            set => SetProperty(ref _seriesName, value);
        }

        public string SeriesCode
        {
            get => _seriesCode;
            set => SetProperty(ref _seriesCode, value);
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
