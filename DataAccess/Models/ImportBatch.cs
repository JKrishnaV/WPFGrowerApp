using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class ImportBatch : INotifyPropertyChanged
    {
        private int _importBatchId; // Modern primary key
        private decimal _impBatch;
        private DateTime _date;
        private DateTime _dataDate;
        private decimal _noTrans;
        private decimal _lowId;
        private decimal _highId;
        private decimal _lowReceipt;
        private decimal _highReceipt;
        private DateTime _lowDate;
        private DateTime _highDate;
        private decimal _voids;
        private string _depot;
        private string _impFile;
        private decimal _uniqImbat;
        private decimal _receipts;

        // Modern property - ImportBatchId (primary key in ImportBatches table)
        public int ImportBatchId
        {
            get => _importBatchId;
            set => SetProperty(ref _importBatchId, value);
        }

        public decimal ImpBatch
        {
            get => _impBatch;
            set => SetProperty(ref _impBatch, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public DateTime DataDate
        {
            get => _dataDate;
            set => SetProperty(ref _dataDate, value);
        }

        public decimal NoTrans
        {
            get => _noTrans;
            set => SetProperty(ref _noTrans, value);
        }

        public decimal LowId
        {
            get => _lowId;
            set => SetProperty(ref _lowId, value);
        }

        public decimal HighId
        {
            get => _highId;
            set => SetProperty(ref _highId, value);
        }

        public decimal LowReceipt
        {
            get => _lowReceipt;
            set => SetProperty(ref _lowReceipt, value);
        }

        public decimal HighReceipt
        {
            get => _highReceipt;
            set => SetProperty(ref _highReceipt, value);
        }

        public DateTime LowDate
        {
            get => _lowDate;
            set => SetProperty(ref _lowDate, value);
        }

        public DateTime HighDate
        {
            get => _highDate;
            set => SetProperty(ref _highDate, value);
        }

        public decimal Voids
        {
            get => _voids;
            set => SetProperty(ref _voids, value);
        }

        public string Depot
        {
            get => _depot;
            set => SetProperty(ref _depot, value);
        }

        public string ImpFile
        {
            get => _impFile;
            set => SetProperty(ref _impFile, value);
        }

        public decimal UniqImbat
        {
            get => _uniqImbat;
            set => SetProperty(ref _uniqImbat, value);
        }

        public decimal Receipts
        {
            get => _receipts;
            set => SetProperty(ref _receipts, value);
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