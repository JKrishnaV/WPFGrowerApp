using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents summary statistics for payment processing dashboard.
    /// Provides overview of payment statuses across all distributions.
    /// </summary>
    public class PaymentStatusSummary : INotifyPropertyChanged
    {
        private int _totalDistributions;
        private int _totalCheques;
        private int _totalElectronicPayments;
        private int _chequesGenerated;
        private int _chequesPrinted;
        private int _chequesIssued;
        private int _chequesCleared;
        private int _paymentsInTransit;
        private int _paymentsReturned;
        private decimal _totalAmount;
        private decimal _pendingAmount;
        private int _overdueDeliveries;
        private int _exceptions;
        private int _electronicPaymentsGenerated;
        private int _electronicPaymentsProcessed;
        private int _electronicPaymentsFailed;

        public int TotalDistributions
        {
            get => _totalDistributions;
            set => SetProperty(ref _totalDistributions, value);
        }

        public int TotalCheques
        {
            get => _totalCheques;
            set => SetProperty(ref _totalCheques, value);
        }

        public int TotalElectronicPayments
        {
            get => _totalElectronicPayments;
            set => SetProperty(ref _totalElectronicPayments, value);
        }

        public int ChequesGenerated
        {
            get => _chequesGenerated;
            set => SetProperty(ref _chequesGenerated, value);
        }

        public int ChequesPrinted
        {
            get => _chequesPrinted;
            set => SetProperty(ref _chequesPrinted, value);
        }

        public int ChequesIssued
        {
            get => _chequesIssued;
            set => SetProperty(ref _chequesIssued, value);
        }

        public int ChequesCleared
        {
            get => _chequesCleared;
            set => SetProperty(ref _chequesCleared, value);
        }

        public int PaymentsInTransit
        {
            get => _paymentsInTransit;
            set => SetProperty(ref _paymentsInTransit, value);
        }

        public int PaymentsReturned
        {
            get => _paymentsReturned;
            set => SetProperty(ref _paymentsReturned, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public decimal PendingAmount
        {
            get => _pendingAmount;
            set => SetProperty(ref _pendingAmount, value);
        }

        public int OverdueDeliveries
        {
            get => _overdueDeliveries;
            set => SetProperty(ref _overdueDeliveries, value);
        }

        public int Exceptions
        {
            get => _exceptions;
            set => SetProperty(ref _exceptions, value);
        }

        public int ElectronicPaymentsGenerated
        {
            get => _electronicPaymentsGenerated;
            set => SetProperty(ref _electronicPaymentsGenerated, value);
        }

        public int ElectronicPaymentsProcessed
        {
            get => _electronicPaymentsProcessed;
            set => SetProperty(ref _electronicPaymentsProcessed, value);
        }

        public int ElectronicPaymentsFailed
        {
            get => _electronicPaymentsFailed;
            set => SetProperty(ref _electronicPaymentsFailed, value);
        }

        // Calculated properties
        public decimal ProcessedAmount => _totalAmount - _pendingAmount;
        public double ProcessingPercentage => _totalAmount > 0 ? (double)(_totalAmount - _pendingAmount) / (double)_totalAmount * 100 : 0;
        public bool HasOverdueDeliveries => _overdueDeliveries > 0;
        public bool HasExceptions => _exceptions > 0;
        public string StatusSummary => GetStatusSummary();

        private string GetStatusSummary()
        {
            if (_exceptions > 0)
                return $"⚠️ {_exceptions} exceptions require attention";
            if (_overdueDeliveries > 0)
                return $"📦 {_overdueDeliveries} overdue deliveries";
            if (_pendingAmount > 0)
                return $"💰 ${_pendingAmount:N2} pending payments";
            return "✅ All payments processed";
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
