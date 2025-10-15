using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Analytics data for receipts
    /// </summary>
    public class ReceiptAnalytics : INotifyPropertyChanged
    {
        // Basic Statistics
        public int TotalReceipts { get; set; }
        public int ActiveReceipts { get; set; }
        public int VoidedReceipts { get; set; }
        public int QualityCheckedReceipts { get; set; }
        public int PaidReceipts { get; set; }

        // Weight Statistics
        public decimal TotalGrossWeight { get; set; }
        public decimal TotalNetWeight { get; set; }
        public decimal TotalFinalWeight { get; set; }
        public decimal TotalDockWeight { get; set; }
        public decimal AverageGrossWeight { get; set; }
        public decimal AverageNetWeight { get; set; }
        public decimal AverageFinalWeight { get; set; }
        public decimal AverageDockPercentage { get; set; }

        // Grade Distribution
        public int Grade1Count { get; set; }
        public int Grade2Count { get; set; }
        public int Grade3Count { get; set; }
        public decimal Grade1Percentage { get; set; }
        public decimal Grade2Percentage { get; set; }
        public decimal Grade3Percentage { get; set; }

        // Top Performers
        public List<TopGrower> TopGrowers { get; set; } = new List<TopGrower>();
        public List<TopProduct> TopProducts { get; set; } = new List<TopProduct>();
        public List<TopDepot> TopDepots { get; set; } = new List<TopDepot>();

        // Trends
        public List<DailyTrend> DailyTrends { get; set; } = new List<DailyTrend>();
        public List<WeeklyTrend> WeeklyTrends { get; set; } = new List<WeeklyTrend>();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();

        // Quality Metrics
        public decimal QualityCheckRate { get; set; }
        public decimal AverageQualityScore { get; set; }
        public int QualityIssuesCount { get; set; }

        // Payment Metrics
        public decimal TotalPaymentAmount { get; set; }
        public decimal AveragePaymentAmount { get; set; }
        public decimal PaymentRate { get; set; }

        // Display Properties
        public string TotalReceiptsDisplay => $"{TotalReceipts:N0}";
        public string TotalGrossWeightDisplay => $"{TotalGrossWeight:N2} lbs";
        public string TotalNetWeightDisplay => $"{TotalNetWeight:N2} lbs";
        public string TotalFinalWeightDisplay => $"{TotalFinalWeight:N2} lbs";
        public string AverageDockPercentageDisplay => $"{AverageDockPercentage:N2}%";
        public string QualityCheckRateDisplay => $"{QualityCheckRate:N1}%";
        public string PaymentRateDisplay => $"{PaymentRate:N1}%";
        public string TotalPaymentAmountDisplay => $"{TotalPaymentAmount:C2}";

        // Helper Properties
        public bool HasData => TotalReceipts > 0;
        public bool HasQualityData => QualityCheckedReceipts > 0;
        public bool HasPaymentData => PaidReceipts > 0;
        public bool HasTrendData => DailyTrends.Count > 0 || WeeklyTrends.Count > 0 || MonthlyTrends.Count > 0;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Top grower by receipt count or weight
    /// </summary>
    public class TopGrower
    {
        public int GrowerId { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public string GrowerNumber { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageWeight { get; set; }
        public string DisplayName => $"{GrowerNumber} - {GrowerName}";
    }

    /// <summary>
    /// Top product by receipt count or weight
    /// </summary>
    public class TopProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public string DisplayName => $"{ProductName} ({ProductDescription})";
    }

    /// <summary>
    /// Top depot by receipt count or weight
    /// </summary>
    public class TopDepot
    {
        public int DepotId { get; set; }
        public string DepotName { get; set; } = string.Empty;
        public string DepotLocation { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public string DisplayName => $"{DepotName} - {DepotLocation}";
    }

    /// <summary>
    /// Daily trend data
    /// </summary>
    public class DailyTrend
    {
        public DateTime Date { get; set; }
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public string DateDisplay => Date.ToString("MMM dd");
    }

    /// <summary>
    /// Weekly trend data
    /// </summary>
    public class WeeklyTrend
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public string WeekDisplay => $"{WeekStart:MMM dd} - {WeekEnd:MMM dd}";
    }

    /// <summary>
    /// Monthly trend data
    /// </summary>
    public class MonthlyTrend
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public string MonthDisplay => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }
}
