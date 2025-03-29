using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Receipt : INotifyPropertyChanged
    {
        private string _depot;
        private string _product;
        private decimal _receiptNumber;
        private decimal _growerNumber;
        private decimal _gross;
        private decimal _tare;
        private decimal _net;
        private decimal _grade;
        private string _process;
        private DateTime _date;
        private decimal _dayUniq;
        private decimal _impBatch;
        private decimal _finBatch;
        private decimal _dockPercent;
        private bool _isVoid;
        private decimal _thePrice;
        private decimal _priceSource;
        private string _prNote1;
        private string _npNote1;
        private string _fromField;
        private bool _imported;
        private string _containerErrors;

        public string Depot
        {
            get => _depot;
            set => SetProperty(ref _depot, value);
        }

        public string Product
        {
            get => _product;
            set => SetProperty(ref _product, value);
        }

        public decimal ReceiptNumber
        {
            get => _receiptNumber;
            set => SetProperty(ref _receiptNumber, value);
        }

        public decimal GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public decimal Gross
        {
            get => _gross;
            set => SetProperty(ref _gross, value);
        }

        public decimal Tare
        {
            get => _tare;
            set => SetProperty(ref _tare, value);
        }

        public decimal Net
        {
            get => _net;
            set => SetProperty(ref _net, value);
        }

        public decimal Grade
        {
            get => _grade;
            set => SetProperty(ref _grade, value);
        }

        public string Process
        {
            get => _process;
            set => SetProperty(ref _process, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public decimal DayUniq
        {
            get => _dayUniq;
            set => SetProperty(ref _dayUniq, value);
        }

        public decimal ImpBatch
        {
            get => _impBatch;
            set => SetProperty(ref _impBatch, value);
        }

        public decimal FinBatch
        {
            get => _finBatch;
            set => SetProperty(ref _finBatch, value);
        }

        public decimal DockPercent
        {
            get => _dockPercent;
            set => SetProperty(ref _dockPercent, value);
        }

        public bool IsVoid
        {
            get => _isVoid;
            set => SetProperty(ref _isVoid, value);
        }

        public decimal ThePrice
        {
            get => _thePrice;
            set => SetProperty(ref _thePrice, value);
        }

        public decimal PriceSource
        {
            get => _priceSource;
            set => SetProperty(ref _priceSource, value);
        }

        public string PrNote1
        {
            get => _prNote1;
            set => SetProperty(ref _prNote1, value);
        }

        public string NpNote1
        {
            get => _npNote1;
            set => SetProperty(ref _npNote1, value);
        }

        public string FromField
        {
            get => _fromField;
            set => SetProperty(ref _fromField, value);
        }

        public bool Imported
        {
            get => _imported;
            set => SetProperty(ref _imported, value);
        }

        public string ContainerErrors
        {
            get => _containerErrors;
            set => SetProperty(ref _containerErrors, value);
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