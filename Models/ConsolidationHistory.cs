using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing consolidation history for a grower
    /// </summary>
    public class ConsolidationHistory : INotifyPropertyChanged
    {
        private int _chequeId;
        private string _chequeNumber;
        private DateTime _chequeDate;
        private decimal _amount;
        private string _status;
        private string _sourceBatches;
        private int _batchCount;

        public int ChequeId
        {
            get => _chequeId;
            set => SetProperty(ref _chequeId, value);
        }

        public string ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        public DateTime ChequeDate
        {
            get => _chequeDate;
            set => SetProperty(ref _chequeDate, value);
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

        public string SourceBatches
        {
            get => _sourceBatches;
            set => SetProperty(ref _sourceBatches, value);
        }

        public int BatchCount
        {
            get => _batchCount;
            set => SetProperty(ref _batchCount, value);
        }

        // Display properties
        public string AmountDisplay => Amount.ToString("C");
        public string DateDisplay => ChequeDate.ToString("MMM dd, yyyy");
        public string BatchCountDisplay => $"{BatchCount} batch{(BatchCount != 1 ? "es" : "")}";
        public string StatusDisplay => Status;
        public string ChequeDisplay => $"{ChequeNumber} ({AmountDisplay})";

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
