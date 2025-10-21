using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Model representing the complete audit trail for a cheque including advance deductions
    /// </summary>
    public class ChequeAuditTrail : INotifyPropertyChanged
    {
        private int _chequeId;
        private string _chequeNumber;
        private decimal _chequeAmount;
        private DateTime _chequeDate;
        private string _chequeStatus;
        private string _growerName;
        private string _growerNumber;
        private decimal _distributionAmount;
        private string _distributionNumber;
        private DateTime _distributionDate;
        private int? _deductionId;
        private decimal? _deductionAmount;
        private DateTime? _deductionDate;
        private int? _advanceChequeId;
        private decimal? _advanceAmount;
        private DateTime? _advanceDate;
        private string _advanceStatus;

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

        public decimal ChequeAmount
        {
            get => _chequeAmount;
            set => SetProperty(ref _chequeAmount, value);
        }

        public DateTime ChequeDate
        {
            get => _chequeDate;
            set => SetProperty(ref _chequeDate, value);
        }

        public string ChequeStatus
        {
            get => _chequeStatus;
            set => SetProperty(ref _chequeStatus, value);
        }

        public string GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public decimal DistributionAmount
        {
            get => _distributionAmount;
            set => SetProperty(ref _distributionAmount, value);
        }

        public string DistributionNumber
        {
            get => _distributionNumber;
            set => SetProperty(ref _distributionNumber, value);
        }

        public DateTime DistributionDate
        {
            get => _distributionDate;
            set => SetProperty(ref _distributionDate, value);
        }

        public int? DeductionId
        {
            get => _deductionId;
            set => SetProperty(ref _deductionId, value);
        }

        public decimal? DeductionAmount
        {
            get => _deductionAmount;
            set => SetProperty(ref _deductionAmount, value);
        }

        public DateTime? DeductionDate
        {
            get => _deductionDate;
            set => SetProperty(ref _deductionDate, value);
        }

        public int? AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        public decimal? AdvanceAmount
        {
            get => _advanceAmount;
            set => SetProperty(ref _advanceAmount, value);
        }

        public DateTime? AdvanceDate
        {
            get => _advanceDate;
            set => SetProperty(ref _advanceDate, value);
        }

        public string AdvanceStatus
        {
            get => _advanceStatus;
            set => SetProperty(ref _advanceStatus, value);
        }

        // Computed properties
        public string ChequeAmountDisplay => ChequeAmount.ToString("C");
        public string DistributionAmountDisplay => DistributionAmount.ToString("C");
        public string DeductionAmountDisplay => DeductionAmount?.ToString("C") ?? "N/A";
        public string AdvanceAmountDisplay => AdvanceAmount?.ToString("C") ?? "N/A";
        public string ChequeDateDisplay => ChequeDate.ToString("yyyy-MM-dd");
        public string DistributionDateDisplay => DistributionDate.ToString("yyyy-MM-dd");
        public string DeductionDateDisplay => DeductionDate?.ToString("yyyy-MM-dd") ?? "N/A";
        public string AdvanceDateDisplay => AdvanceDate?.ToString("yyyy-MM-dd") ?? "N/A";

        // Helper properties
        public bool HasAdvanceDeduction => DeductionId.HasValue;
        public decimal NetAmount => ChequeAmount;
        public string AuditSummary => $"Cheque: {ChequeNumber}, Amount: {ChequeAmountDisplay}, Grower: {GrowerName}";

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
