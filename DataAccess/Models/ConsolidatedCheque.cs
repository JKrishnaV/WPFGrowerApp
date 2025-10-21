using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Model representing a consolidated cheque linking multiple payment batches
    /// </summary>
    public class ConsolidatedCheque : INotifyPropertyChanged
    {
        private int _consolidatedChequeId;
        private int _chequeId;
        private int _paymentBatchId;
        private decimal _amount;
        private DateTime _createdAt;
        private string _createdBy;

        // Navigation properties
        private Cheque _cheque;
        private PaymentBatch _paymentBatch;

        public int ConsolidatedChequeId
        {
            get => _consolidatedChequeId;
            set => SetProperty(ref _consolidatedChequeId, value);
        }

        public int ChequeId
        {
            get => _chequeId;
            set => SetProperty(ref _chequeId, value);
        }

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
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

        // Navigation properties
        public Cheque Cheque
        {
            get => _cheque;
            set => SetProperty(ref _cheque, value);
        }

        public PaymentBatch PaymentBatch
        {
            get => _paymentBatch;
            set => SetProperty(ref _paymentBatch, value);
        }

        // Display properties
        public string AmountDisplay => Amount.ToString("C");
        public string DateDisplay => CreatedAt.ToString("MMM dd, yyyy");
        public string BatchNumber => PaymentBatch?.BatchNumber ?? "N/A";
        public string ChequeNumber => Cheque?.ChequeNumber ?? "N/A";

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
