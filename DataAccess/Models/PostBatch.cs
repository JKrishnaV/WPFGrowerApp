using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a posting batch record (from PostBat table).
    /// </summary>
    public class PostBatch : INotifyPropertyChanged
    {
        private decimal _postBat;
        private DateTime _date;
        private DateTime _cutoff;
        private string _postType;
        private DateTime? _qaddDate;
        private string _qaddTime;
        private string _qaddOp;
        private DateTime? _qedDate;
        private string _qedTime;
        private string _qedOp;
        private DateTime? _qdelDate;
        private string _qdelTime;
        private string _qdelOp;

        // Corresponds to POST_BAT column
        public decimal PostBat
        {
            get => _postBat;
            set => SetProperty(ref _postBat, value);
        }

        // Corresponds to DATE column
        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        // Corresponds to CUTOFF column
        public DateTime Cutoff
        {
            get => _cutoff;
            set => SetProperty(ref _cutoff, value);
        }

        // Corresponds to POST_TYPE column
        public string PostType
        {
            get => _postType;
            set => SetProperty(ref _postType, value);
        }

        // Audit Fields (nullable DateTime for dates)
        public DateTime? QaddDate { get => _qaddDate; set => SetProperty(ref _qaddDate, value); }
        public string QaddTime { get => _qaddTime; set => SetProperty(ref _qaddTime, value); }
        public string QaddOp { get => _qaddOp; set => SetProperty(ref _qaddOp, value); }
        public DateTime? QedDate { get => _qedDate; set => SetProperty(ref _qedDate, value); }
        public string QedTime { get => _qedTime; set => SetProperty(ref _qedTime, value); }
        public string QedOp { get => _qedOp; set => SetProperty(ref _qedOp, value); }
        public DateTime? QdelDate { get => _qdelDate; set => SetProperty(ref _qdelDate, value); }
        public string QdelTime { get => _qdelTime; set => SetProperty(ref _qdelTime, value); }
        public string QdelOp { get => _qdelOp; set => SetProperty(ref _qdelOp, value); }


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
