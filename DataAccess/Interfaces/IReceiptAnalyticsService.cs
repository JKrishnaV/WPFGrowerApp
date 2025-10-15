using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using ProductBreakdown = WPFGrowerApp.DataAccess.Models.ProductBreakdown;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for receipt analytics operations
    /// </summary>
    public interface IReceiptAnalyticsService
    {
        /// <summary>
        /// Get comprehensive receipt analytics for a date range
        /// </summary>
        /// <param name="startDate">Start date for analytics</param>
        /// <param name="endDate">End date for analytics</param>
        /// <returns>Receipt analytics data</returns>
        Task<ReceiptAnalytics> GetReceiptAnalyticsAsync(DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Calculate receipt statistics for a list of receipts
        /// </summary>
        /// <param name="receipts">List of receipts to analyze</param>
        /// <returns>Receipt statistics</returns>
        Task<ReceiptAnalytics> CalculateReceiptStatisticsAsync(List<Receipt> receipts);

        /// <summary>
        /// Get receipt trends over time
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Trend data</returns>
        Task<ReceiptTrends> GetReceiptTrendsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get grower-specific receipt analytics
        /// </summary>
        /// <param name="growerId">Grower ID</param>
        /// <param name="cropYear">Crop year</param>
        /// <returns>Grower analytics</returns>
        Task<GrowerReceiptAnalytics> GetGrowerReceiptAnalyticsAsync(int growerId, int cropYear);

        /// <summary>
        /// Get product-specific receipt analytics
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <param name="cropYear">Crop year</param>
        /// <returns>Product analytics</returns>
        Task<ProductReceiptAnalytics> GetProductReceiptAnalyticsAsync(int productId, int cropYear);

        /// <summary>
        /// Get depot-specific receipt analytics
        /// </summary>
        /// <param name="depotId">Depot ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Depot analytics</returns>
        Task<DepotReceiptAnalytics> GetDepotReceiptAnalyticsAsync(int depotId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get quality metrics for receipts
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Quality metrics</returns>
        Task<QualityMetrics> GetQualityMetricsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get weight distribution analysis
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Weight distribution data</returns>
        Task<WeightDistribution> GetWeightDistributionAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get seasonal trends for receipts
        /// </summary>
        /// <param name="year">Year to analyze</param>
        /// <returns>Seasonal trends</returns>
        Task<SeasonalTrends> GetSeasonalTrendsAsync(int year);

        /// <summary>
        /// Get comparative analytics between periods
        /// </summary>
        /// <param name="period1Start">First period start</param>
        /// <param name="period1End">First period end</param>
        /// <param name="period2Start">Second period start</param>
        /// <param name="period2End">Second period end</param>
        /// <returns>Comparative analytics</returns>
        Task<ComparativeAnalytics> GetComparativeAnalyticsAsync(
            DateTime period1Start, DateTime period1End,
            DateTime period2Start, DateTime period2End);
    }

    /// <summary>
    /// Receipt trends data
    /// </summary>
    public class ReceiptTrends
    {
        public List<DailyTrend> DailyTrends { get; set; } = new List<DailyTrend>();
        public List<WeeklyTrend> WeeklyTrends { get; set; } = new List<WeeklyTrend>();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();
        public decimal GrowthRate { get; set; }
        public string TrendDirection { get; set; } = string.Empty; // "Increasing", "Decreasing", "Stable"
    }

    /// <summary>
    /// Grower receipt analytics
    /// </summary>
    public class GrowerReceiptAnalytics
    {
        public int GrowerId { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public string GrowerNumber { get; set; } = string.Empty;
        public int TotalReceipts { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageValue { get; set; }
        public decimal QualityScore { get; set; }
        public int QualityIssues { get; set; }
        public List<ProductBreakdown> ProductBreakdown { get; set; } = new List<ProductBreakdown>();
        public List<MonthlyPerformance> MonthlyPerformance { get; set; } = new List<MonthlyPerformance>();
    }

    /// <summary>
    /// Product receipt analytics
    /// </summary>
    public class ProductReceiptAnalytics
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalReceipts { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public decimal AverageDockPercentage { get; set; }
        public decimal QualityScore { get; set; }
        public List<GrowerBreakdown> GrowerBreakdown { get; set; } = new List<GrowerBreakdown>();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();
    }

    /// <summary>
    /// Depot receipt analytics
    /// </summary>
    public class DepotReceiptAnalytics
    {
        public int DepotId { get; set; }
        public string DepotName { get; set; } = string.Empty;
        public int TotalReceipts { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public int UniqueGrowers { get; set; }
        public List<ProductBreakdown> ProductBreakdown { get; set; } = new List<ProductBreakdown>();
        public List<GrowerBreakdown> TopGrowers { get; set; } = new List<GrowerBreakdown>();
    }

    /// <summary>
    /// Quality metrics
    /// </summary>
    public class QualityMetrics
    {
        public int TotalReceipts { get; set; }
        public int QualityCheckedReceipts { get; set; }
        public decimal QualityCheckRate { get; set; }
        public decimal AverageGrade { get; set; }
        public int Grade1Count { get; set; }
        public int Grade2Count { get; set; }
        public int Grade3Count { get; set; }
        public int QualityIssues { get; set; }
        public decimal QualityScore { get; set; }
    }

    /// <summary>
    /// Weight distribution analysis
    /// </summary>
    public class WeightDistribution
    {
        public decimal MinWeight { get; set; }
        public decimal MaxWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public decimal MedianWeight { get; set; }
        public decimal StandardDeviation { get; set; }
        public List<WeightRange> WeightRanges { get; set; } = new List<WeightRange>();
    }

    /// <summary>
    /// Seasonal trends
    /// </summary>
    public class SeasonalTrends
    {
        public int Year { get; set; }
        public List<SeasonalData> SeasonalData { get; set; } = new List<SeasonalData>();
        public string PeakSeason { get; set; } = string.Empty;
        public string LowSeason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Comparative analytics between two periods
    /// </summary>
    public class ComparativeAnalytics
    {
        public PeriodAnalytics Period1 { get; set; } = new PeriodAnalytics();
        public PeriodAnalytics Period2 { get; set; } = new PeriodAnalytics();
        public ComparisonMetrics Comparison { get; set; } = new ComparisonMetrics();
    }

    /// <summary>
    /// Period analytics
    /// </summary>
    public class PeriodAnalytics
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalReceipts { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public decimal TotalValue { get; set; }
        public int UniqueGrowers { get; set; }
        public decimal QualityScore { get; set; }
    }

    /// <summary>
    /// Comparison metrics
    /// </summary>
    public class ComparisonMetrics
    {
        public decimal ReceiptCountChange { get; set; }
        public decimal WeightChange { get; set; }
        public decimal AverageWeightChange { get; set; }
        public decimal ValueChange { get; set; }
        public decimal GrowerCountChange { get; set; }
        public decimal QualityScoreChange { get; set; }
        public string OverallTrend { get; set; } = string.Empty; // "Improving", "Declining", "Stable"
    }


    /// <summary>
    /// Grower breakdown
    /// </summary>
    public class GrowerBreakdown
    {
        public int GrowerId { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public string GrowerNumber { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Monthly performance
    /// </summary>
    public class MonthlyPerformance
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public decimal QualityScore { get; set; }
    }

    /// <summary>
    /// Weight range
    /// </summary>
    public class WeightRange
    {
        public decimal MinWeight { get; set; }
        public decimal MaxWeight { get; set; }
        public int ReceiptCount { get; set; }
        public decimal Percentage { get; set; }
        public string RangeLabel { get; set; } = string.Empty;
    }

    /// <summary>
    /// Seasonal data
    /// </summary>
    public class SeasonalData
    {
        public string Season { get; set; } = string.Empty;
        public int ReceiptCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal AverageWeight { get; set; }
        public decimal Percentage { get; set; }
    }
}
