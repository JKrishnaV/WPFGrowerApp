using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing a consolidated payment across multiple batches
    /// </summary>
    public class ConsolidatedPayment : INotifyPropertyChanged
    {
        private int _growerId;
        private string _growerName;
        private string _growerNumber;
        private List<int> _batchIds;
        private List<BatchBreakdown> _batchBreakdowns;
        private decimal _totalAmount;
        private DateTime _consolidationDate;
        private string _status;
        private bool _canBeConsolidated;
        private List<AdvanceBreakdown> _outstandingAdvances;
        private decimal _netTotal;

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
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

        public List<int> BatchIds
        {
            get => _batchIds;
            set => SetProperty(ref _batchIds, value);
        }

        public List<BatchBreakdown> BatchBreakdowns
        {
            get => _batchBreakdowns;
            set => SetProperty(ref _batchBreakdowns, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public DateTime ConsolidationDate
        {
            get => _consolidationDate;
            set => SetProperty(ref _consolidationDate, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public bool CanBeConsolidated
        {
            get => _canBeConsolidated;
            set => SetProperty(ref _canBeConsolidated, value);
        }

        public List<AdvanceBreakdown> OutstandingAdvances
        {
            get => _outstandingAdvances;
            set => SetProperty(ref _outstandingAdvances, value);
        }

        public decimal NetTotal
        {
            get => _netTotal;
            set => SetProperty(ref _netTotal, value);
        }

        // Computed properties
        public string TotalAmountDisplay => TotalAmount.ToString("C");
        public string DateDisplay => ConsolidationDate.ToString("MMM dd, yyyy");
        public string GrowerDisplay => $"{GrowerNumber} - {GrowerName}";
        public int BatchCount => BatchIds?.Count ?? 0;
        public string BatchCountDisplay => $"{BatchCount} batch{(BatchCount != 1 ? "es" : "")}";
        public string SourceBatchesDisplay => GetSourceBatchesDisplay();
        public string StatusDisplay => Status;
        public int OutstandingAdvancesCount => OutstandingAdvances?.Count ?? 0;
        public string OutstandingAdvancesCountDisplay => $"{OutstandingAdvancesCount} advance{(OutstandingAdvancesCount != 1 ? "s" : "")}";
        public string NetTotalDisplay => NetTotal.ToString("C");

        public ConsolidatedPayment()
        {
            BatchIds = new List<int>();
            BatchBreakdowns = new List<BatchBreakdown>();
            OutstandingAdvances = new List<AdvanceBreakdown>();
            ConsolidationDate = DateTime.Now;
            Status = "Draft";
            CanBeConsolidated = true;
        }

        public void AddBatch(BatchBreakdown batch)
        {
            if (batch == null) return;

            BatchBreakdowns.Add(batch);
            if (!BatchIds.Contains(batch.BatchId))
                BatchIds.Add(batch.BatchId);
            
            UpdateTotalAmount();
        }

        public void RemoveBatch(int batchId)
        {
            BatchIds.Remove(batchId);
            BatchBreakdowns.RemoveAll(b => b.BatchId == batchId);
            UpdateTotalAmount();
        }

        public void UpdateTotalAmount()
        {
            TotalAmount = BatchBreakdowns?.Sum(b => b.Amount) ?? 0;
        }

        private string GetSourceBatchesDisplay()
        {
            if (BatchBreakdowns == null || !BatchBreakdowns.Any())
                return "No batches";

            var batchNumbers = BatchBreakdowns.Select(b => b.BatchNumber).ToList();
            return string.Join(", ", batchNumbers);
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
