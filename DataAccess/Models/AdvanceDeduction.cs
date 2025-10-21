using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Model representing a deduction from an advance cheque
    /// </summary>
    public class AdvanceDeduction : INotifyPropertyChanged
    {
        private int _deductionId;
        private int _advanceChequeId;
        private int _paymentBatchId;
        private decimal _deductionAmount;
        private DateTime _deductionDate;
        private string _createdBy;
        private DateTime _createdAt;

        // Navigation properties
        private AdvanceCheque _advanceCheque;
        private PaymentBatch _paymentBatch;

        public int DeductionId
        {
            get => _deductionId;
            set => SetProperty(ref _deductionId, value);
        }

        public int AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        public int PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        public decimal DeductionAmount
        {
            get => _deductionAmount;
            set => SetProperty(ref _deductionAmount, value);
        }

        public DateTime DeductionDate
        {
            get => _deductionDate;
            set => SetProperty(ref _deductionDate, value);
        }

        public string CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        // Navigation properties
        public AdvanceCheque AdvanceCheque
        {
            get => _advanceCheque;
            set => SetProperty(ref _advanceCheque, value);
        }

        public PaymentBatch PaymentBatch
        {
            get => _paymentBatch;
            set => SetProperty(ref _paymentBatch, value);
        }

        // Display properties
        public string AmountDisplay => DeductionAmount.ToString("C");
        public string DateDisplay => DeductionDate.ToString("MMM dd, yyyy");
        public string BatchNumber => PaymentBatch?.BatchNumber ?? "N/A";
        public string GrowerName => AdvanceCheque?.GrowerName ?? "Unknown";

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
