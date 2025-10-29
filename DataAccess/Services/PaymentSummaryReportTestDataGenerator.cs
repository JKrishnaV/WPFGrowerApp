using System;
using System.Collections.Generic;
using System.Linq;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for generating test data for Payment Summary Report testing and validation.
    /// Provides comprehensive test data scenarios for development and testing purposes.
    /// </summary>
    public class PaymentSummaryReportTestDataGenerator
    {
        private readonly Random _random = new Random();

        #region Test Data Generation

        /// <summary>
        /// Generates comprehensive test data for Payment Summary Report testing.
        /// </summary>
        /// <param name="growerCount">Number of growers to generate</param>
        /// <param name="monthsBack">Number of months to generate data for</param>
        /// <returns>Complete PaymentSummaryReport with test data</returns>
        public PaymentSummaryReport GenerateTestReport(int growerCount = 50, int monthsBack = 12)
        {
            var report = new PaymentSummaryReport
            {
                ReportDate = DateTime.Now,
                PeriodStart = DateTime.Now.AddMonths(-monthsBack),
                PeriodEnd = DateTime.Now,
                ReportTitle = "Test Payment Summary Report",
                GeneratedBy = "TestDataGenerator",
                ReportDescription = $"Test report with {growerCount} growers over {monthsBack} months"
            };

            // Generate grower details
            report.GrowerDetails = GenerateGrowerDetails(growerCount, monthsBack);

            // Generate chart data
            report.PaymentDistribution = GeneratePaymentDistributionData(report.GrowerDetails);
            report.MonthlyTrends = GenerateMonthlyTrendData(monthsBack);
            report.TopPerformers = GenerateTopPerformersData(report.GrowerDetails);

            // Calculate summary statistics
            CalculateSummaryStatistics(report);

            return report;
        }

        /// <summary>
        /// Generates test grower payment details.
        /// </summary>
        private List<GrowerPaymentDetail> GenerateGrowerDetails(int count, int monthsBack)
        {
            var growers = new List<GrowerPaymentDetail>();
            var provinces = new[] { "BC", "AB", "SK", "MB", "ON", "QC", "NB", "NS", "PE", "NL" };
            var cities = new[] { "Vancouver", "Calgary", "Toronto", "Montreal", "Halifax", "Winnipeg", "Edmonton", "Ottawa" };
            var paymentMethods = new[] { "Cheque", "EFT", "Wire Transfer", "Cash" };
            var paymentStatuses = new[] { "Complete", "Partial", "Pending", "Overdue" };

            for (int i = 1; i <= count; i++)
            {
                var grower = new GrowerPaymentDetail
                {
                    GrowerId = i,
                    GrowerNumber = $"GR{i:D4}",
                    FullName = $"Test Grower {i}",
                    CheckPayeeName = $"Test Payee {i}",
                    City = cities[_random.Next(cities.Length)],
                    Province = provinces[_random.Next(provinces.Length)],
                    PhoneNumber = GeneratePhoneNumber(),
                    Email = $"grower{i}@test.com",
                    Address = $"{_random.Next(100, 9999)} Test Street",
                    PostalCode = GeneratePostalCode(),
                    CurrencyCode = "CAD",
                    IsActive = _random.NextDouble() > 0.1, // 90% active
                    IsOnHold = _random.NextDouble() > 0.9, // 10% on hold
                    PaymentMethod = paymentMethods[_random.Next(paymentMethods.Length)],
                    PaymentGroupName = $"Group {_random.Next(1, 6)}"
                };

                // Generate financial data
                GenerateFinancialData(grower, monthsBack);
                GenerateReceiptData(grower, monthsBack);
                GenerateStatusData(grower);

                growers.Add(grower);
            }

            return growers;
        }

        /// <summary>
        /// Generates financial data for a grower.
        /// </summary>
        private void GenerateFinancialData(GrowerPaymentDetail grower, int monthsBack)
        {
            var baseAmount = _random.Next(1000, 50000);
            
            grower.TotalReceiptsValue = baseAmount;
            grower.Advance1Paid = baseAmount * (decimal)(0.3 + _random.NextDouble() * 0.2); // 30-50%
            grower.Advance2Paid = baseAmount * (decimal)(0.2 + _random.NextDouble() * 0.2); // 20-40%
            grower.Advance3Paid = baseAmount * (decimal)(0.1 + _random.NextDouble() * 0.1); // 10-20%
            grower.FinalPaymentPaid = baseAmount * (decimal)(0.05 + _random.NextDouble() * 0.15); // 5-20%
            grower.TotalDeductions = baseAmount * (decimal)(0.01 + _random.NextDouble() * 0.05); // 1-6%
            grower.PremiumAmount = baseAmount * (decimal)(0.02 + _random.NextDouble() * 0.03); // 2-5%

            // TotalPaymentsMade is calculated automatically from individual payments
            grower.OutstandingBalance = grower.TotalReceiptsValue - grower.TotalPaymentsMade - grower.TotalDeductions;
        }

        /// <summary>
        /// Generates receipt data for a grower.
        /// </summary>
        private void GenerateReceiptData(GrowerPaymentDetail grower, int monthsBack)
        {
            grower.TotalReceipts = _random.Next(5, 50);
            grower.TotalWeight = _random.Next(1000, 10000);
            grower.AverageReceiptValue = grower.TotalReceipts > 0 ? grower.TotalReceiptsValue / grower.TotalReceipts : 0;
            grower.AverageReceiptWeight = grower.TotalReceipts > 0 ? grower.TotalWeight / grower.TotalReceipts : 0;

            var startDate = DateTime.Now.AddMonths(-monthsBack);
            grower.FirstReceiptDate = startDate.AddDays(_random.Next(0, 30));
            grower.LastReceiptDate = DateTime.Now.AddDays(-_random.Next(0, 30));
            grower.LastPaymentDate = DateTime.Now.AddDays(-_random.Next(0, 60));
        }

        /// <summary>
        /// Generates status and performance data for a grower.
        /// </summary>
        private void GenerateStatusData(GrowerPaymentDetail grower)
        {
            var paymentStatuses = new[] { "Complete", "Partial", "Pending", "Overdue" };
            grower.PaymentStatus = paymentStatuses[_random.Next(paymentStatuses.Length)];

            grower.PaymentCompletionPercentage = grower.TotalReceiptsValue > 0 
                ? (grower.TotalPaymentsMade / grower.TotalReceiptsValue) * 100 
                : 0;

            grower.AveragePaymentPerReceipt = grower.TotalReceipts > 0 
                ? grower.TotalPaymentsMade / grower.TotalReceipts 
                : 0;

            if (grower.LastPaymentDate.HasValue)
            {
                grower.DaysSinceLastPayment = (DateTime.Now - grower.LastPaymentDate.Value).Days;
            }

            if (grower.LastReceiptDate.HasValue)
            {
                grower.DaysSinceLastReceipt = (DateTime.Now - grower.LastReceiptDate.Value).Days;
            }

            grower.PaymentVelocity = _random.Next(1, 10); // Payments per month
        }

        /// <summary>
        /// Generates payment distribution chart data.
        /// </summary>
        private List<PaymentDistributionChart> GeneratePaymentDistributionData(List<GrowerPaymentDetail> growers)
        {
            var totalAdvance1 = growers.Sum(g => g.Advance1Paid);
            var totalAdvance2 = growers.Sum(g => g.Advance2Paid);
            var totalAdvance3 = growers.Sum(g => g.Advance3Paid);
            var totalFinal = growers.Sum(g => g.FinalPaymentPaid);

            var charts = new List<PaymentDistributionChart>
            {
                new PaymentDistributionChart
                {
                    Category = "Advance 1",
                    Value = totalAdvance1,
                    Description = "First advance payments",
                    Count = growers.Count(g => g.Advance1Paid > 0),
                    Color = "#FF6384"
                },
                new PaymentDistributionChart
                {
                    Category = "Advance 2",
                    Value = totalAdvance2,
                    Description = "Second advance payments",
                    Count = growers.Count(g => g.Advance2Paid > 0),
                    Color = "#36A2EB"
                },
                new PaymentDistributionChart
                {
                    Category = "Advance 3",
                    Value = totalAdvance3,
                    Description = "Third advance payments",
                    Count = growers.Count(g => g.Advance3Paid > 0),
                    Color = "#FFCE56"
                },
                new PaymentDistributionChart
                {
                    Category = "Final Payment",
                    Value = totalFinal,
                    Description = "Final settlement payments",
                    Count = growers.Count(g => g.FinalPaymentPaid > 0),
                    Color = "#4BC0C0"
                }
            };

            // Calculate percentages
            var totalValue = charts.Sum(c => c.Value);
            foreach (var chart in charts)
            {
                chart.Percentage = totalValue > 0 ? (chart.Value / totalValue) * 100 : 0;
            }

            return charts;
        }

        /// <summary>
        /// Generates monthly trend chart data.
        /// </summary>
        private List<MonthlyTrendChart> GenerateMonthlyTrendData(int monthsBack)
        {
            var trends = new List<MonthlyTrendChart>();
            var baseAmount = 100000m;

            for (int i = monthsBack; i >= 0; i--)
            {
                var month = DateTime.Now.AddMonths(-i);
                var variation = (decimal)(0.5 + _random.NextDouble()); // 50-150% variation

                trends.Add(new MonthlyTrendChart
                {
                    Month = new DateTime(month.Year, month.Month, 1),
                    TotalPayments = baseAmount * variation,
                    Advance1Amount = baseAmount * variation * 0.4m,
                    Advance2Amount = baseAmount * variation * 0.3m,
                    Advance3Amount = baseAmount * variation * 0.15m,
                    FinalPaymentAmount = baseAmount * variation * 0.15m,
                    PaymentCount = _random.Next(50, 200),
                    GrowerCount = _random.Next(20, 80)
                });
            }

            return trends;
        }

        /// <summary>
        /// Generates top performers chart data.
        /// </summary>
        private List<GrowerPerformanceChart> GenerateTopPerformersData(List<GrowerPaymentDetail> growers)
        {
            var performers = growers
                .OrderByDescending(g => g.TotalPaymentsMade)
                .Take(10)
                .Select((g, index) => new GrowerPerformanceChart
                {
                    GrowerId = g.GrowerId,
                    GrowerName = g.FullName,
                    GrowerNumber = g.GrowerNumber,
                    TotalPayments = g.TotalPaymentsMade,
                    TotalReceipts = g.TotalReceiptsValue,
                    ReceiptCount = g.TotalReceipts,
                    AveragePaymentPerReceipt = g.AveragePaymentPerReceipt,
                    Province = g.Province,
                    Rank = index + 1
                })
                .ToList();

            return performers;
        }

        /// <summary>
        /// Calculates summary statistics for the report.
        /// </summary>
        private void CalculateSummaryStatistics(PaymentSummaryReport report)
        {
            report.TotalGrowers = report.GrowerDetails.Count;
            report.TotalReceiptsValue = report.GrowerDetails.Sum(g => g.TotalReceiptsValue);
            report.TotalPaymentsMade = report.GrowerDetails.Sum(g => g.TotalPaymentsMade);
            report.OutstandingBalance = report.GrowerDetails.Sum(g => g.OutstandingBalance);
            report.AveragePaymentPerGrower = report.TotalGrowers > 0 ? report.TotalPaymentsMade / report.TotalGrowers : 0;
            report.TotalReceipts = report.GrowerDetails.Sum(g => g.TotalReceipts);
            report.TotalWeight = report.GrowerDetails.Sum(g => g.TotalWeight);

            report.Advance1Total = report.GrowerDetails.Sum(g => g.Advance1Paid);
            report.Advance2Total = report.GrowerDetails.Sum(g => g.Advance2Paid);
            report.Advance3Total = report.GrowerDetails.Sum(g => g.Advance3Paid);
            report.FinalPaymentTotal = report.GrowerDetails.Sum(g => g.FinalPaymentPaid);
            report.TotalDeductions = report.GrowerDetails.Sum(g => g.TotalDeductions);
            report.PremiumTotal = report.GrowerDetails.Sum(g => g.PremiumAmount);
        }

        #endregion

        #region Helper Methods

        private string GeneratePhoneNumber()
        {
            return $"{_random.Next(200, 999)}-{_random.Next(100, 999)}-{_random.Next(1000, 9999)}";
        }

        private string GeneratePostalCode()
        {
            var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return $"{letters[_random.Next(letters.Length)]}{_random.Next(0, 9)}{letters[_random.Next(letters.Length)]} {_random.Next(0, 9)}{letters[_random.Next(letters.Length)]}{_random.Next(0, 9)}";
        }

        #endregion

        #region Test Scenarios

        /// <summary>
        /// Generates test data for edge cases and validation scenarios.
        /// </summary>
        public PaymentSummaryReport GenerateEdgeCaseTestData()
        {
            var report = new PaymentSummaryReport
            {
                ReportDate = DateTime.Now,
                PeriodStart = DateTime.Now.AddMonths(-1),
                PeriodEnd = DateTime.Now,
                ReportTitle = "Edge Case Test Report",
                GeneratedBy = "TestDataGenerator",
                ReportDescription = "Test report with edge cases and validation scenarios"
            };

            // Generate edge case scenarios
            var growers = new List<GrowerPaymentDetail>();

            // Zero balance grower
            growers.Add(CreateEdgeCaseGrower("Zero Balance", 0, 0, 0, 0, 0, 0));

            // High payment grower
            growers.Add(CreateEdgeCaseGrower("High Payment", 100000, 50000, 30000, 20000, 0, 0));

            // Overdue grower
            growers.Add(CreateEdgeCaseGrower("Overdue", 50000, 10000, 5000, 0, 0, 35000));

            // Complete payment grower
            growers.Add(CreateEdgeCaseGrower("Complete", 30000, 15000, 10000, 5000, 0, 0));

            report.GrowerDetails = growers;
            report.PaymentDistribution = GeneratePaymentDistributionData(growers);
            report.MonthlyTrends = GenerateMonthlyTrendData(1);
            report.TopPerformers = GenerateTopPerformersData(growers);
            CalculateSummaryStatistics(report);

            return report;
        }

        private GrowerPaymentDetail CreateEdgeCaseGrower(string name, decimal receipts, decimal advance1, decimal advance2, decimal advance3, decimal final, decimal outstanding)
        {
            return new GrowerPaymentDetail
            {
                GrowerId = _random.Next(1000, 9999),
                GrowerNumber = $"EC{_random.Next(100, 999)}",
                FullName = name,
                CheckPayeeName = name,
                City = "Test City",
                Province = "BC",
                PhoneNumber = GeneratePhoneNumber(),
                Email = $"{name.ToLower().Replace(" ", "")}@test.com",
                TotalReceiptsValue = receipts,
                Advance1Paid = advance1,
                Advance2Paid = advance2,
                Advance3Paid = advance3,
                FinalPaymentPaid = final,
                OutstandingBalance = outstanding,
                // TotalPaymentsMade is calculated automatically from individual payments
                TotalReceipts = _random.Next(1, 20),
                TotalWeight = _random.Next(100, 5000),
                PaymentStatus = outstanding > 0 ? "Overdue" : "Complete",
                IsActive = true,
                IsOnHold = false,
                PaymentMethod = "Cheque",
                CurrencyCode = "CAD"
            };
        }

        #endregion
    }
}
