using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model for analytics summary data of a payment batch
    /// </summary>
    public class PaymentBatchAnalytics : INotifyPropertyChanged
    {
        private decimal _averagePaymentPerGrower;
        private decimal _largestPayment;
        private string _paymentRange = string.Empty;
        private decimal _totalWeight;
        private int _anomalyCount;
        private string _comparisonNote = string.Empty;
        private List<PaymentDistributionBucket> _paymentDistribution = new();
        private List<ProductBreakdown> _productBreakdown = new();
        private List<PaymentRangeBucket> _paymentRangeBuckets = new();

        // Computed properties for empty state logic
        public bool HasPaymentDistributionData => PaymentDistribution?.Count > 1;
        public bool HasPaymentRangeData => PaymentRangeBuckets?.Count > 1;
        public bool HasProductBreakdownData => ProductBreakdown?.Count > 1;

        public decimal AveragePaymentPerGrower
        {
            get => _averagePaymentPerGrower;
            set => SetProperty(ref _averagePaymentPerGrower, value);
        }

        public decimal LargestPayment
        {
            get => _largestPayment;
            set => SetProperty(ref _largestPayment, value);
        }

        public string PaymentRange
        {
            get => _paymentRange;
            set => SetProperty(ref _paymentRange, value);
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set => SetProperty(ref _totalWeight, value);
        }

        public int AnomalyCount
        {
            get => _anomalyCount;
            set => SetProperty(ref _anomalyCount, value);
        }

        public string ComparisonNote
        {
            get => _comparisonNote;
            set => SetProperty(ref _comparisonNote, value);
        }

        public List<PaymentDistributionBucket> PaymentDistribution
        {
            get => _paymentDistribution;
            set => SetProperty(ref _paymentDistribution, value);
        }

        public List<ProductBreakdown> ProductBreakdown
        {
            get => _productBreakdown;
            set => SetProperty(ref _productBreakdown, value);
        }

        public List<PaymentRangeBucket> PaymentRangeBuckets
        {
            get => _paymentRangeBuckets;
            set => SetProperty(ref _paymentRangeBuckets, value);
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    /// <summary>
    /// Represents a payment distribution bucket for charting
    /// </summary>
    public class PaymentDistributionBucket
    {
        public string Range { get; set; } = string.Empty;
        public int GrowerCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Represents product breakdown data
    /// </summary>
    public class ProductBreakdown
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal Amount { get; set; }
        public int ReceiptCount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Represents payment range buckets for grower distribution
    /// </summary>
    public class PaymentRangeBucket
    {
        public string RangeLabel { get; set; } = string.Empty;
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public int GrowerCount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Represents comparison data with previous batches
    /// </summary>
    public class BatchComparison
    {
        public decimal? PreviousAveragePayment { get; set; }
        public decimal? PreviousTotalAmount { get; set; }
        public int? PreviousGrowerCount { get; set; }
        public decimal? PreviousTotalWeight { get; set; }
        public decimal CurrentAveragePayment { get; set; }
        public decimal CurrentTotalAmount { get; set; }
        public int CurrentGrowerCount { get; set; }
        public decimal CurrentTotalWeight { get; set; }
        public decimal PaymentChangePercentage { get; set; }
        public decimal AmountChangePercentage { get; set; }
        public decimal GrowerChangePercentage { get; set; }
        public decimal WeightChangePercentage { get; set; }
    }
}
