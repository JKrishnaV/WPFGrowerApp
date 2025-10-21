using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing a grower's details across multiple batches
    /// </summary>
    public class GrowerBatchDetails : INotifyPropertyChanged
    {
        private int _growerId;
        private string _growerNumber;
        private string _growerName;
        private int _batchCount;
        private string _batchNumbers;

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public string GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public int BatchCount
        {
            get => _batchCount;
            set => SetProperty(ref _batchCount, value);
        }

        public string BatchNumbers
        {
            get => _batchNumbers;
            set => SetProperty(ref _batchNumbers, value);
        }

        // Display properties
        public string GrowerDisplay => $"{GrowerNumber} - {GrowerName}";
        public string BatchCountDisplay => $"{BatchCount} batch{(BatchCount != 1 ? "es" : "")}";
        public string StatusDisplay => GetStatusDisplay();

        public GrowerBatchDetails()
        {
        }

        public GrowerBatchDetails(int growerId, string growerNumber, string growerName, int batchCount, string batchNumbers)
        {
            GrowerId = growerId;
            GrowerNumber = growerNumber;
            GrowerName = growerName;
            BatchCount = batchCount;
            BatchNumbers = batchNumbers;
        }

        private string GetStatusDisplay()
        {
            if (BatchCount > 3)
                return "High consolidation potential";
            else if (BatchCount > 1)
                return "Can be consolidated";
            else
                return "Single batch";
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
