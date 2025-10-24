using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing an outstanding advance breakdown for consolidated payments
    /// </summary>
    public class AdvanceBreakdown : INotifyPropertyChanged
    {
        private int _advanceChequeId;
        private string _chequeNumber;
        private decimal _amount;
        private DateTime _advanceDate;
        private string _status;

        public int AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        public string ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public DateTime AdvanceDate
        {
            get => _advanceDate;
            set => SetProperty(ref _advanceDate, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        // Display properties
        public string AmountDisplay => Amount.ToString("C");
        public string DateDisplay => AdvanceDate.ToString("MMM dd, yyyy");
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
