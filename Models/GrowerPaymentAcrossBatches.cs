using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing a grower's payment across multiple batches
    /// </summary>
    public class GrowerPaymentAcrossBatches : INotifyPropertyChanged
    {
        private int _batchId;
        private string _batchNumber;
        private DateTime _batchDate;
        private decimal _amount;
        private string _status;

        public int BatchId
        {
            get => _batchId;
            set => SetProperty(ref _batchId, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public DateTime BatchDate
        {
            get => _batchDate;
            set => SetProperty(ref _batchDate, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        // Display properties
        public string AmountDisplay => Amount.ToString("C");
        public string DateDisplay => BatchDate.ToString("MMM dd, yyyy");
        public string BatchDisplay => $"{BatchNumber} ({AmountDisplay})";

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
