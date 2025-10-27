using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Summary model for advance deduction reporting
    /// </summary>
    public class AdvanceDeductionSummary : INotifyPropertyChanged
    {
        private int _growerId;
        private string _growerNumber;
        private string _growerName;
        private decimal _totalOriginalAdvances;
        private decimal _totalOutstandingAdvances;
        private decimal _totalDeductedAmount;
        private decimal _totalVoidedAmount;
        private int _activeAdvanceCount;
        private int _totalDeductionCount;
        private int _voidedDeductionCount;
        private DateTime? _lastDeductionDate;
        private DateTime? _lastAdvanceDate;
        private List<AdvanceCheque> _activeAdvances;
        private List<AdvanceDeduction> _recentDeductions;

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber ?? string.Empty;
            set => SetProperty(ref _growerNumber, value);
        }

        public string GrowerName
        {
            get => _growerName ?? string.Empty;
            set => SetProperty(ref _growerName, value);
        }

        public decimal TotalOriginalAdvances
        {
            get => _totalOriginalAdvances;
            set => SetProperty(ref _totalOriginalAdvances, value);
        }

        public decimal TotalOutstandingAdvances
        {
            get => _totalOutstandingAdvances;
            set => SetProperty(ref _totalOutstandingAdvances, value);
        }

        public decimal TotalDeductedAmount
        {
            get => _totalDeductedAmount;
            set => SetProperty(ref _totalDeductedAmount, value);
        }

        public decimal TotalVoidedAmount
        {
            get => _totalVoidedAmount;
            set => SetProperty(ref _totalVoidedAmount, value);
        }

        public int ActiveAdvanceCount
        {
            get => _activeAdvanceCount;
            set => SetProperty(ref _activeAdvanceCount, value);
        }

        public int TotalDeductionCount
        {
            get => _totalDeductionCount;
            set => SetProperty(ref _totalDeductionCount, value);
        }

        public int VoidedDeductionCount
        {
            get => _voidedDeductionCount;
            set => SetProperty(ref _voidedDeductionCount, value);
        }

        public DateTime? LastDeductionDate
        {
            get => _lastDeductionDate;
            set => SetProperty(ref _lastDeductionDate, value);
        }

        public DateTime? LastAdvanceDate
        {
            get => _lastAdvanceDate;
            set => SetProperty(ref _lastAdvanceDate, value);
        }

        public List<AdvanceCheque> ActiveAdvances
        {
            get => _activeAdvances ?? (_activeAdvances = new List<AdvanceCheque>());
            set => SetProperty(ref _activeAdvances, value);
        }

        public List<AdvanceDeduction> RecentDeductions
        {
            get => _recentDeductions ?? (_recentDeductions = new List<AdvanceDeduction>());
            set => SetProperty(ref _recentDeductions, value);
        }

        // Computed properties
        public decimal TotalDeductedPercentage => TotalOriginalAdvances > 0 ? (TotalDeductedAmount / TotalOriginalAdvances) * 100 : 0;
        public decimal NetOutstandingBalance => TotalOutstandingAdvances - TotalVoidedAmount;
        public bool HasOutstandingAdvances => TotalOutstandingAdvances > 0;
        public bool HasActiveAdvances => ActiveAdvanceCount > 0;
        public bool HasRecentActivity => LastDeductionDate.HasValue && LastDeductionDate.Value > DateTime.Now.AddDays(-30);
        public decimal AverageAdvanceAmount => ActiveAdvanceCount > 0 ? TotalOutstandingAdvances / ActiveAdvanceCount : 0;

        // Display properties
        public string TotalOriginalAdvancesDisplay => TotalOriginalAdvances.ToString("C");
        public string TotalOutstandingAdvancesDisplay => TotalOutstandingAdvances.ToString("C");
        public string TotalDeductedAmountDisplay => TotalDeductedAmount.ToString("C");
        public string TotalVoidedAmountDisplay => TotalVoidedAmount.ToString("C");
        public string NetOutstandingBalanceDisplay => NetOutstandingBalance.ToString("C");
        public string TotalDeductedPercentageDisplay => $"{TotalDeductedPercentage:F1}%";
        public string AverageAdvanceAmountDisplay => AverageAdvanceAmount.ToString("C");
        public string LastDeductionDateDisplay => LastDeductionDate?.ToString("MMM dd, yyyy") ?? "Never";
        public string LastAdvanceDateDisplay => LastAdvanceDate?.ToString("MMM dd, yyyy") ?? "Never";
        public string GrowerDisplay => $"{GrowerNumber} - {GrowerName}";
        public string SummaryDisplay => $"Advances: {ActiveAdvanceCount}, Outstanding: {TotalOutstandingAdvancesDisplay}, Deducted: {TotalDeductedAmountDisplay}";

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