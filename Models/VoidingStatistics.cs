using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing voiding statistics
    /// </summary>
    public class VoidingStatistics : INotifyPropertyChanged
    {
        private DateTime? _startDate;
        private DateTime? _endDate;
        private int _totalVoids;
        private int _regularVoids;
        private int _advanceVoids;
        private int _consolidatedVoids;

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public int TotalVoids
        {
            get => _totalVoids;
            set => SetProperty(ref _totalVoids, value);
        }

        public int RegularVoids
        {
            get => _regularVoids;
            set => SetProperty(ref _regularVoids, value);
        }

        public int AdvanceVoids
        {
            get => _advanceVoids;
            set => SetProperty(ref _advanceVoids, value);
        }

        public int ConsolidatedVoids
        {
            get => _consolidatedVoids;
            set => SetProperty(ref _consolidatedVoids, value);
        }

        // Computed properties
        public string DateRangeDisplay => GetDateRangeDisplay();
        public string TotalVoidsDisplay => $"{TotalVoids} void{(TotalVoids != 1 ? "s" : "")}";
        public string RegularVoidsDisplay => $"{RegularVoids} regular void{(RegularVoids != 1 ? "s" : "")}";
        public string AdvanceVoidsDisplay => $"{AdvanceVoids} advance void{(AdvanceVoids != 1 ? "s" : "")}";
        public string ConsolidatedVoidsDisplay => $"{ConsolidatedVoids} consolidated void{(ConsolidatedVoids != 1 ? "s" : "")}";
        public double RegularPercentage => TotalVoids > 0 ? (double)RegularVoids / TotalVoids * 100 : 0;
        public double AdvancePercentage => TotalVoids > 0 ? (double)AdvanceVoids / TotalVoids * 100 : 0;
        public double ConsolidatedPercentage => TotalVoids > 0 ? (double)ConsolidatedVoids / TotalVoids * 100 : 0;

        public VoidingStatistics()
        {
        }

        public VoidingStatistics(DateTime? startDate, DateTime? endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }

        private string GetDateRangeDisplay()
        {
            if (StartDate.HasValue && EndDate.HasValue)
                return $"{StartDate.Value:MMM dd, yyyy} - {EndDate.Value:MMM dd, yyyy}";
            else if (StartDate.HasValue)
                return $"From {StartDate.Value:MMM dd, yyyy}";
            else if (EndDate.HasValue)
                return $"Until {EndDate.Value:MMM dd, yyyy}";
            else
                return "All time";
        }

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
