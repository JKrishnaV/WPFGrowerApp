using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for receipt analytics operations
    /// </summary>
    public class ReceiptAnalyticsService : IReceiptAnalyticsService
    {
        private readonly IReceiptService _receiptService;

        public ReceiptAnalyticsService(IReceiptService receiptService)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        }

        public async Task<ReceiptAnalytics> GetReceiptAnalyticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                Logger.Info($"Getting receipt analytics for period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                // Get receipts for the date range
                var receipts = await _receiptService.GetReceiptsAsync(startDate, endDate);
                
                if (!receipts.Any())
                {
                    Logger.Info("No receipts found for analytics period");
                    return new ReceiptAnalytics();
                }

                // Calculate analytics
                var analytics = await CalculateReceiptStatisticsAsync(receipts);

                // Add trend data
                if (startDate.HasValue && endDate.HasValue)
                {
                    var trends = await GetReceiptTrendsAsync(startDate.Value, endDate.Value);
                    analytics.DailyTrends = trends.DailyTrends;
                    analytics.WeeklyTrends = trends.WeeklyTrends;
                    analytics.MonthlyTrends = trends.MonthlyTrends;
                }

                Logger.Info($"Analytics calculated for {receipts.Count} receipts");
                return analytics;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting receipt analytics", ex);
                return new ReceiptAnalytics();
            }
        }

        public async Task<ReceiptAnalytics> CalculateReceiptStatisticsAsync(List<Receipt> receipts)
        {
            try
            {
                Logger.Info($"Calculating statistics for {receipts.Count} receipts");

                var analytics = new ReceiptAnalytics();

                if (!receipts.Any())
                    return analytics;

                // Basic statistics
                analytics.TotalReceipts = receipts.Count;
                analytics.ActiveReceipts = receipts.Count(r => !r.IsVoided);
                analytics.VoidedReceipts = receipts.Count(r => r.IsVoided);
                analytics.QualityCheckedReceipts = receipts.Count(r => r.QualityCheckedAt.HasValue);
                analytics.PaidReceipts = 0; // This would need to be calculated based on payment data - placeholder for now

                // Weight statistics
                analytics.TotalGrossWeight = receipts.Sum(r => r.GrossWeight);
                analytics.TotalNetWeight = receipts.Sum(r => r.NetWeight);
                analytics.TotalFinalWeight = receipts.Sum(r => r.FinalWeight);
                analytics.TotalDockWeight = receipts.Sum(r => r.DockWeight);
                analytics.AverageGrossWeight = (decimal)receipts.Average(r => r.GrossWeight);
                analytics.AverageNetWeight = (decimal)receipts.Average(r => r.NetWeight);
                analytics.AverageFinalWeight = (decimal)receipts.Average(r => r.FinalWeight);
                analytics.AverageDockPercentage = (decimal)receipts.Average(r => r.DockPercentage);

                // Grade distribution
                analytics.Grade1Count = receipts.Count(r => r.Grade == 1);
                analytics.Grade2Count = receipts.Count(r => r.Grade == 2);
                analytics.Grade3Count = receipts.Count(r => r.Grade == 3);
                analytics.Grade1Percentage = analytics.TotalReceipts > 0 ? (decimal)analytics.Grade1Count / analytics.TotalReceipts * 100 : 0;
                analytics.Grade2Percentage = analytics.TotalReceipts > 0 ? (decimal)analytics.Grade2Count / analytics.TotalReceipts * 100 : 0;
                analytics.Grade3Percentage = analytics.TotalReceipts > 0 ? (decimal)analytics.Grade3Count / analytics.TotalReceipts * 100 : 0;

                // Quality metrics
                analytics.QualityCheckRate = analytics.TotalReceipts > 0 ? (decimal)analytics.QualityCheckedReceipts / analytics.TotalReceipts * 100 : 0;
                var qualityCheckedReceipts = receipts.Where(r => r.QualityCheckedAt.HasValue).ToList();
                analytics.AverageQualityScore = qualityCheckedReceipts.Any() ? (decimal)qualityCheckedReceipts.Average(r => r.Grade) : 0;
                analytics.QualityIssuesCount = receipts.Count(r => r.Grade < 2); // Assuming grade < 2 indicates quality issues

                // Payment metrics (placeholder - would need payment data)
                analytics.TotalPaymentAmount = 0; // Would be calculated from payment allocations
                analytics.AveragePaymentAmount = 0;
                analytics.PaymentRate = analytics.TotalReceipts > 0 ? (decimal)analytics.PaidReceipts / analytics.TotalReceipts * 100 : 0;

                // Top performers
                analytics.TopGrowers = CalculateTopGrowers(receipts);
                analytics.TopProducts = CalculateTopProducts(receipts);
                analytics.TopDepots = CalculateTopDepots(receipts);

                Logger.Info($"Statistics calculated successfully");
                return analytics;
            }
            catch (Exception ex)
            {
                Logger.Error("Error calculating receipt statistics", ex);
                return new ReceiptAnalytics();
            }
        }

        public async Task<ReceiptTrends> GetReceiptTrendsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                Logger.Info($"Getting receipt trends from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var trends = new ReceiptTrends();

                // Get receipts for the period
                var receipts = await _receiptService.GetReceiptsAsync(startDate, endDate);
                
                if (!receipts.Any())
                {
                    Logger.Info("No receipts found for trend analysis");
                    return trends;
                }

                // Calculate daily trends
                trends.DailyTrends = CalculateDailyTrends(receipts, startDate, endDate);
                
                // Calculate weekly trends
                trends.WeeklyTrends = CalculateWeeklyTrends(receipts, startDate, endDate);
                
                // Calculate monthly trends
                trends.MonthlyTrends = CalculateMonthlyTrends(receipts, startDate, endDate);

                // Calculate growth rate and trend direction
                if (trends.DailyTrends.Count >= 2)
                {
                    var firstWeek = trends.DailyTrends.Take(7).Sum(t => t.ReceiptCount);
                    var lastWeek = trends.DailyTrends.TakeLast(7).Sum(t => t.ReceiptCount);
                    trends.GrowthRate = firstWeek > 0 ? (decimal)(lastWeek - firstWeek) / firstWeek * 100 : 0;
                    
                    trends.TrendDirection = trends.GrowthRate switch
                    {
                        > 5 => "Increasing",
                        < -5 => "Decreasing",
                        _ => "Stable"
                    };
                }

                Logger.Info($"Trend analysis completed");
                return trends;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting receipt trends", ex);
                return new ReceiptTrends();
            }
        }

        public async Task<GrowerReceiptAnalytics> GetGrowerReceiptAnalyticsAsync(int growerId, int cropYear)
        {
            try
            {
                Logger.Info($"Getting grower analytics for grower {growerId}, crop year {cropYear}");

                // Get receipts for the grower and crop year
                var startDate = new DateTime(cropYear, 1, 1);
                var endDate = new DateTime(cropYear, 12, 31);
                var allReceipts = await _receiptService.GetReceiptsAsync(startDate, endDate);
                var receipts = allReceipts.Where(r => r.GrowerId == growerId).ToList();

                if (!receipts.Any())
                {
                    Logger.Info($"No receipts found for grower {growerId} in crop year {cropYear}");
                    return new GrowerReceiptAnalytics { GrowerId = growerId };
                }

                var analytics = new GrowerReceiptAnalytics
                {
                    GrowerId = growerId,
                    TotalReceipts = receipts.Count,
                    TotalWeight = receipts.Sum(r => r.FinalWeight),
                    AverageWeight = (decimal)receipts.Average(r => r.FinalWeight),
                    QualityScore = (decimal)receipts.Average(r => r.Grade),
                    QualityIssues = receipts.Count(r => r.Grade < 2)
                };

                // Calculate product breakdown
                analytics.ProductBreakdown = CalculateProductBreakdown(receipts);

                // Calculate monthly performance
                analytics.MonthlyPerformance = CalculateMonthlyPerformance(receipts, cropYear);

                Logger.Info($"Grower analytics calculated for {receipts.Count} receipts");
                return analytics;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting grower analytics for grower {growerId}", ex);
                return new GrowerReceiptAnalytics { GrowerId = growerId };
            }
        }

        public async Task<ProductReceiptAnalytics> GetProductReceiptAnalyticsAsync(int productId, int cropYear)
        {
            try
            {
                Logger.Info($"Getting product analytics for product {productId}, crop year {cropYear}");

                // Get receipts for the product and crop year
                var startDate = new DateTime(cropYear, 1, 1);
                var endDate = new DateTime(cropYear, 12, 31);
                var allReceipts = await _receiptService.GetReceiptsAsync(startDate, endDate);
                var receipts = allReceipts.Where(r => r.ProductId == productId).ToList();

                if (!receipts.Any())
                {
                    Logger.Info($"No receipts found for product {productId} in crop year {cropYear}");
                    return new ProductReceiptAnalytics { ProductId = productId };
                }

                var analytics = new ProductReceiptAnalytics
                {
                    ProductId = productId,
                    TotalReceipts = receipts.Count,
                    TotalWeight = receipts.Sum(r => r.FinalWeight),
                    AverageWeight = (decimal)receipts.Average(r => r.FinalWeight),
                    AverageDockPercentage = (decimal)receipts.Average(r => r.DockPercentage),
                    QualityScore = (decimal)receipts.Average(r => r.Grade)
                };

                // Calculate grower breakdown
                analytics.GrowerBreakdown = CalculateGrowerBreakdown(receipts);

                // Calculate monthly trends
                analytics.MonthlyTrends = CalculateMonthlyTrends(receipts, startDate, endDate);

                Logger.Info($"Product analytics calculated for {receipts.Count} receipts");
                return analytics;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting product analytics for product {productId}", ex);
                return new ProductReceiptAnalytics { ProductId = productId };
            }
        }

        public async Task<DepotReceiptAnalytics> GetDepotReceiptAnalyticsAsync(int depotId, DateTime startDate, DateTime endDate)
        {
            try
            {
                Logger.Info($"Getting depot analytics for depot {depotId} from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                // Get receipts for the depot and date range
                var allReceipts = await _receiptService.GetReceiptsAsync(startDate, endDate);
                var receipts = allReceipts.Where(r => r.DepotId == depotId).ToList();

                if (!receipts.Any())
                {
                    Logger.Info($"No receipts found for depot {depotId} in date range");
                    return new DepotReceiptAnalytics { DepotId = depotId };
                }

                var analytics = new DepotReceiptAnalytics
                {
                    DepotId = depotId,
                    TotalReceipts = receipts.Count,
                    TotalWeight = receipts.Sum(r => r.FinalWeight),
                    AverageWeight = (decimal)receipts.Average(r => r.FinalWeight),
                    UniqueGrowers = receipts.Select(r => r.GrowerId).Distinct().Count()
                };

                // Calculate product breakdown
                analytics.ProductBreakdown = CalculateProductBreakdown(receipts);

                // Calculate top growers
                analytics.TopGrowers = CalculateGrowerBreakdown(receipts).Take(10).ToList();

                Logger.Info($"Depot analytics calculated for {receipts.Count} receipts");
                return analytics;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting depot analytics for depot {depotId}", ex);
                return new DepotReceiptAnalytics { DepotId = depotId };
            }
        }

        public async Task<QualityMetrics> GetQualityMetricsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                Logger.Info($"Getting quality metrics from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var receipts = await _receiptService.GetReceiptsAsync(startDate, endDate);
                
                var metrics = new QualityMetrics
                {
                    TotalReceipts = receipts.Count,
                    QualityCheckedReceipts = receipts.Count(r => r.QualityCheckedAt.HasValue),
                    QualityCheckRate = receipts.Count > 0 ? (decimal)receipts.Count(r => r.QualityCheckedAt.HasValue) / receipts.Count * 100 : 0,
                    AverageGrade = receipts.Any() ? (decimal)receipts.Average(r => r.Grade) : 0,
                    Grade1Count = receipts.Count(r => r.Grade == 1),
                    Grade2Count = receipts.Count(r => r.Grade == 2),
                    Grade3Count = receipts.Count(r => r.Grade == 3),
                    QualityIssues = receipts.Count(r => r.Grade < 2),
                    QualityScore = receipts.Any() ? (decimal)receipts.Average(r => r.Grade) : 0
                };

                Logger.Info($"Quality metrics calculated for {receipts.Count} receipts");
                return metrics;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting quality metrics", ex);
                return new QualityMetrics();
            }
        }

        public async Task<WeightDistribution> GetWeightDistributionAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                Logger.Info($"Getting weight distribution from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var receipts = await _receiptService.GetReceiptsAsync(startDate, endDate);
                var weights = receipts.Select(r => r.FinalWeight).ToList();

                if (!weights.Any())
                {
                    Logger.Info("No receipts found for weight distribution analysis");
                    return new WeightDistribution();
                }

                var distribution = new WeightDistribution
                {
                    MinWeight = weights.Min(),
                    MaxWeight = weights.Max(),
                    AverageWeight = weights.Average(),
                    MedianWeight = CalculateMedian(weights),
                    StandardDeviation = CalculateStandardDeviation(weights)
                };

                // Calculate weight ranges
                distribution.WeightRanges = CalculateWeightRanges(weights);

                Logger.Info($"Weight distribution calculated for {weights.Count} receipts");
                return distribution;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting weight distribution", ex);
                return new WeightDistribution();
            }
        }

        public async Task<SeasonalTrends> GetSeasonalTrendsAsync(int year)
        {
            try
            {
                Logger.Info($"Getting seasonal trends for year {year}");

                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);
                var receipts = await _receiptService.GetReceiptsAsync(startDate, endDate);

                var trends = new SeasonalTrends { Year = year };

                if (!receipts.Any())
                {
                    Logger.Info($"No receipts found for seasonal analysis in year {year}");
                    return trends;
                }

                // Calculate seasonal data
                trends.SeasonalData = CalculateSeasonalData(receipts);

                // Find peak and low seasons
                if (trends.SeasonalData.Any())
                {
                    var peakSeason = trends.SeasonalData.OrderByDescending(s => s.TotalWeight).First();
                    var lowSeason = trends.SeasonalData.OrderBy(s => s.TotalWeight).First();
                    trends.PeakSeason = peakSeason.Season;
                    trends.LowSeason = lowSeason.Season;
                }

                Logger.Info($"Seasonal trends calculated for {receipts.Count} receipts");
                return trends;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting seasonal trends for year {year}", ex);
                return new SeasonalTrends { Year = year };
            }
        }

        public async Task<ComparativeAnalytics> GetComparativeAnalyticsAsync(
            DateTime period1Start, DateTime period1End,
            DateTime period2Start, DateTime period2End)
        {
            try
            {
                Logger.Info($"Getting comparative analytics for periods: {period1Start:yyyy-MM-dd} to {period1End:yyyy-MM-dd} vs {period2Start:yyyy-MM-dd} to {period2End:yyyy-MM-dd}");

                var period1Receipts = await _receiptService.GetReceiptsAsync(period1Start, period1End);
                var period2Receipts = await _receiptService.GetReceiptsAsync(period2Start, period2End);

                var analytics = new ComparativeAnalytics
                {
                    Period1 = CalculatePeriodAnalytics(period1Receipts, period1Start, period1End),
                    Period2 = CalculatePeriodAnalytics(period2Receipts, period2Start, period2End)
                };

                // Calculate comparison metrics
                analytics.Comparison = CalculateComparisonMetrics(analytics.Period1, analytics.Period2);

                Logger.Info("Comparative analytics calculated successfully");
                return analytics;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting comparative analytics", ex);
                return new ComparativeAnalytics();
            }
        }

        #region Private Helper Methods

        private List<TopGrower> CalculateTopGrowers(List<Receipt> receipts)
        {
            return receipts
                .GroupBy(r => new { r.GrowerId, r.GrowerName, r.GrowerNumber })
                .Select(g => new TopGrower
                {
                    GrowerId = g.Key.GrowerId,
                    GrowerName = g.Key.GrowerName ?? "Unknown",
                    GrowerNumber = g.Key.GrowerNumber ?? "Unknown",
                    ReceiptCount = g.Count(),
                    TotalWeight = g.Sum(r => r.FinalWeight),
                    AverageWeight = g.Average(r => r.FinalWeight)
                })
                .OrderByDescending(g => g.TotalWeight)
                .Take(10)
                .ToList();
        }

        private List<TopProduct> CalculateTopProducts(List<Receipt> receipts)
        {
            return receipts
                .GroupBy(r => new { r.ProductId, ProductName = r.Product, ProductDescription = r.Product })
                .Select(g => new TopProduct
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName ?? "Unknown",
                    ProductDescription = g.Key.ProductDescription ?? "Unknown",
                    ReceiptCount = g.Count(),
                    TotalWeight = g.Sum(r => r.FinalWeight),
                    AverageWeight = g.Average(r => r.FinalWeight)
                })
                .OrderByDescending(p => p.TotalWeight)
                .Take(10)
                .ToList();
        }

        private List<TopDepot> CalculateTopDepots(List<Receipt> receipts)
        {
            return receipts
                .GroupBy(r => new { r.DepotId, DepotName = r.Depot, DepotAddress = r.Depot })
                .Select(g => new TopDepot
                {
                    DepotId = g.Key.DepotId,
                    DepotName = g.Key.DepotName ?? "Unknown",
                    DepotLocation = g.Key.DepotAddress ?? "Unknown",
                    ReceiptCount = g.Count(),
                    TotalWeight = g.Sum(r => r.FinalWeight),
                    AverageWeight = g.Average(r => r.FinalWeight)
                })
                .OrderByDescending(d => d.TotalWeight)
                .Take(10)
                .ToList();
        }

        private List<DailyTrend> CalculateDailyTrends(List<Receipt> receipts, DateTime startDate, DateTime endDate)
        {
            var trends = new List<DailyTrend>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var dayReceipts = receipts.Where(r => r.ReceiptDate.Date == currentDate.Date).ToList();
                trends.Add(new DailyTrend
                {
                    Date = currentDate,
                    ReceiptCount = dayReceipts.Count,
                    TotalWeight = dayReceipts.Sum(r => r.FinalWeight),
                    AverageWeight = dayReceipts.Any() ? dayReceipts.Average(r => r.FinalWeight) : 0
                });
                currentDate = currentDate.AddDays(1);
            }

            return trends;
        }

        private List<WeeklyTrend> CalculateWeeklyTrends(List<Receipt> receipts, DateTime startDate, DateTime endDate)
        {
            var trends = new List<WeeklyTrend>();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                var weekEnd = currentDate.AddDays(6);
                if (weekEnd > endDate) weekEnd = endDate;

                var weekReceipts = receipts.Where(r => r.ReceiptDate.Date >= currentDate.Date && r.ReceiptDate.Date <= weekEnd.Date).ToList();
                trends.Add(new WeeklyTrend
                {
                    WeekStart = currentDate,
                    WeekEnd = weekEnd,
                    ReceiptCount = weekReceipts.Count,
                    TotalWeight = weekReceipts.Sum(r => r.FinalWeight),
                    AverageWeight = weekReceipts.Any() ? weekReceipts.Average(r => r.FinalWeight) : 0
                });

                currentDate = currentDate.AddDays(7);
            }

            return trends;
        }

        private List<MonthlyTrend> CalculateMonthlyTrends(List<Receipt> receipts, DateTime startDate, DateTime endDate)
        {
            return receipts
                .GroupBy(r => new { r.ReceiptDate.Year, r.ReceiptDate.Month })
                .Select(g => new MonthlyTrend
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    ReceiptCount = g.Count(),
                    TotalWeight = g.Sum(r => r.FinalWeight),
                    AverageWeight = g.Average(r => r.FinalWeight)
                })
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .ToList();
        }

        private List<ProductBreakdown> CalculateProductBreakdown(List<Receipt> receipts)
        {
            var totalWeight = receipts.Sum(r => r.FinalWeight);
            return receipts
                .GroupBy(r => new { r.ProductId, ProductName = r.Product })
                .Select(g => new ProductBreakdown
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName ?? "Unknown",
                    ReceiptCount = g.Count(),
                    TotalWeight = g.Sum(r => r.FinalWeight),
                    Percentage = totalWeight > 0 ? (decimal)((double)g.Sum(r => r.FinalWeight) / (double)totalWeight * 100) : 0
                })
                .OrderByDescending(p => p.TotalWeight)
                .ToList();
        }

        private List<GrowerBreakdown> CalculateGrowerBreakdown(List<Receipt> receipts)
        {
            var totalWeight = receipts.Sum(r => r.FinalWeight);
            return receipts
                .GroupBy(r => new { r.GrowerId, r.GrowerName, r.GrowerNumber })
                .Select(g => new GrowerBreakdown
                {
                    GrowerId = g.Key.GrowerId,
                    GrowerName = g.Key.GrowerName ?? "Unknown",
                    GrowerNumber = g.Key.GrowerNumber ?? "Unknown",
                    ReceiptCount = g.Count(),
                    TotalWeight = g.Sum(r => r.FinalWeight),
                    Percentage = totalWeight > 0 ? (decimal)((double)g.Sum(r => r.FinalWeight) / (double)totalWeight * 100) : 0
                })
                .OrderByDescending(g => g.TotalWeight)
                .ToList();
        }

        private List<MonthlyPerformance> CalculateMonthlyPerformance(List<Receipt> receipts, int year)
        {
            return receipts
                .GroupBy(r => r.ReceiptDate.Month)
                .Select(g => new MonthlyPerformance
                {
                    Year = year,
                    Month = g.Key,
                    ReceiptCount = g.Count(),
                    TotalWeight = g.Sum(r => r.FinalWeight),
                    AverageWeight = (decimal)g.Average(r => r.FinalWeight),
                    QualityScore = (decimal)g.Average(r => r.Grade)
                })
                .OrderBy(p => p.Month)
                .ToList();
        }

        private List<WeightRange> CalculateWeightRanges(List<decimal> weights)
        {
            var ranges = new List<WeightRange>();
            var min = weights.Min();
            var max = weights.Max();
            var rangeSize = (max - min) / 10; // 10 ranges

            for (int i = 0; i < 10; i++)
            {
                var rangeMin = min + (i * rangeSize);
                var rangeMax = i == 9 ? max : min + ((i + 1) * rangeSize);
                var count = weights.Count(w => w >= rangeMin && w < rangeMax);
                
                ranges.Add(new WeightRange
                {
                    MinWeight = rangeMin,
                    MaxWeight = rangeMax,
                    ReceiptCount = count,
                    Percentage = weights.Count > 0 ? (decimal)count / weights.Count * 100 : 0,
                    RangeLabel = $"{rangeMin:N0}-{rangeMax:N0} lbs"
                });
            }

            return ranges;
        }

        private List<SeasonalData> CalculateSeasonalData(List<Receipt> receipts)
        {
            var seasonalData = new List<SeasonalData>();
            var totalWeight = receipts.Sum(r => r.FinalWeight);

            // Define seasons
            var seasons = new[]
            {
                new { Name = "Spring", Months = new[] { 3, 4, 5 } },
                new { Name = "Summer", Months = new[] { 6, 7, 8 } },
                new { Name = "Fall", Months = new[] { 9, 10, 11 } },
                new { Name = "Winter", Months = new[] { 12, 1, 2 } }
            };

            foreach (var season in seasons)
            {
                var seasonReceipts = receipts.Where(r => season.Months.Contains(r.ReceiptDate.Month)).ToList();
                seasonalData.Add(new SeasonalData
                {
                    Season = season.Name,
                    ReceiptCount = seasonReceipts.Count,
                    TotalWeight = seasonReceipts.Sum(r => r.FinalWeight),
                    AverageWeight = seasonReceipts.Any() ? seasonReceipts.Average(r => r.FinalWeight) : 0,
                    Percentage = totalWeight > 0 ? seasonReceipts.Sum(r => r.FinalWeight) / totalWeight * 100 : 0
                });
            }

            return seasonalData;
        }

        private PeriodAnalytics CalculatePeriodAnalytics(List<Receipt> receipts, DateTime startDate, DateTime endDate)
        {
            return new PeriodAnalytics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalReceipts = receipts.Count,
                TotalWeight = receipts.Sum(r => r.FinalWeight),
                AverageWeight = receipts.Any() ? (decimal)receipts.Average(r => r.FinalWeight) : 0,
                UniqueGrowers = receipts.Select(r => r.GrowerId).Distinct().Count(),
                QualityScore = receipts.Any() ? (decimal)receipts.Average(r => r.Grade) : 0
            };
        }

        private ComparisonMetrics CalculateComparisonMetrics(PeriodAnalytics period1, PeriodAnalytics period2)
        {
            return new ComparisonMetrics
            {
                ReceiptCountChange = period1.TotalReceipts > 0 ? (decimal)(period2.TotalReceipts - period1.TotalReceipts) / period1.TotalReceipts * 100 : 0,
                WeightChange = period1.TotalWeight > 0 ? (period2.TotalWeight - period1.TotalWeight) / period1.TotalWeight * 100 : 0,
                AverageWeightChange = period1.AverageWeight > 0 ? (period2.AverageWeight - period1.AverageWeight) / period1.AverageWeight * 100 : 0,
                GrowerCountChange = period1.UniqueGrowers > 0 ? (decimal)(period2.UniqueGrowers - period1.UniqueGrowers) / period1.UniqueGrowers * 100 : 0,
                QualityScoreChange = period1.QualityScore > 0 ? (period2.QualityScore - period1.QualityScore) / period1.QualityScore * 100 : 0,
                OverallTrend = CalculateOverallTrend(period1, period2)
            };
        }

        private string CalculateOverallTrend(PeriodAnalytics period1, PeriodAnalytics period2)
        {
            var improvements = 0;
            var declines = 0;

            if (period2.TotalReceipts > period1.TotalReceipts) improvements++;
            else if (period2.TotalReceipts < period1.TotalReceipts) declines++;

            if (period2.TotalWeight > period1.TotalWeight) improvements++;
            else if (period2.TotalWeight < period1.TotalWeight) declines++;

            if (period2.QualityScore > period1.QualityScore) improvements++;
            else if (period2.QualityScore < period1.QualityScore) declines++;

            return improvements > declines ? "Improving" : declines > improvements ? "Declining" : "Stable";
        }

        private decimal CalculateMedian(List<decimal> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            var count = sorted.Count;
            if (count % 2 == 0)
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
            else
                return sorted[count / 2];
        }

        private decimal CalculateStandardDeviation(List<decimal> values)
        {
            var average = values.Average();
            var sumOfSquares = values.Sum(v => (decimal)Math.Pow((double)(v - average), 2));
            return (decimal)Math.Sqrt((double)(sumOfSquares / values.Count));
        }

        #endregion
    }
}
