using Dapper;
using Microsoft.Data.SqlClient;
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
    /// CORRECTED Service for generating Payment Summary Reports with proper calculations.
    /// Fixes:
    /// 1. Receipt value calculation using PriceDetails table
    /// 2. Payment calculations excluding deleted rows
    /// 3. AdvanceCheques and AdvanceDeductions summary
    /// 4. Correct TransactionType values from actual database
    /// </summary>
    public class PaymentSummaryReportService : BaseDatabaseService, IPaymentSummaryReportService
    {
        private readonly IGrowerService _growerService;
        private readonly IReceiptService _receiptService;
        private readonly IPaymentService _paymentService;
        private readonly IAccountService _accountService;

        public PaymentSummaryReportService()
        {
        }

        public PaymentSummaryReportService(
            IGrowerService growerService,
            IReceiptService receiptService,
            IPaymentService paymentService,
            IAccountService accountService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        }

        #region Main Report Generation

        public async Task<PaymentSummaryReport> GeneratePaymentSummaryReportAsync(ReportFilterOptions options)
        {
            var startTime = DateTime.Now;
            try
            {
                Logger.Info($"Starting CORRECTED Payment Summary Report generation at {startTime:yyyy-MM-dd HH:mm:ss.fff}");

                var report = new PaymentSummaryReport
                {
                    ReportDate = DateTime.Now,
                    PeriodStart = options.PeriodStart,
                    PeriodEnd = options.PeriodEnd,
                    ReportTitle = "Payment Summary Report (Corrected)",
                    GeneratedBy = Environment.UserName,
                    ReportDescription = $"Corrected payment analysis for {options.DateRangeDisplay}"
                };

                // Use consolidated method to get both grower details and summary statistics in single query
                var parallelStartTime = DateTime.Now;
                Logger.Info($"Starting consolidated report generation at {parallelStartTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                var consolidatedDataTask = GetPaymentSummaryDataAsync(options);
                var paymentDistributionTask = GetPaymentDistributionDataAsync(options);
                var monthlyTrendTask = GetMonthlyTrendDataAsync(options);
                var topPerformersTask = GetTopPerformersAsync(options, 10);
                var advanceSummaryTask = GetAdvanceSummaryAsync(options);

                await Task.WhenAll(consolidatedDataTask, paymentDistributionTask, monthlyTrendTask, topPerformersTask, advanceSummaryTask);
                
                var parallelEndTime = DateTime.Now;
                var parallelDuration = parallelEndTime - parallelStartTime;
                Logger.Info($"Consolidated report generation completed in {parallelDuration.TotalMilliseconds:F0}ms at {parallelEndTime:yyyy-MM-dd HH:mm:ss.fff}");

                // Get results from consolidated method
                var (growerDetails, summaryStats) = await consolidatedDataTask;
                report.GrowerDetails = growerDetails;
                report.PaymentDistribution = await paymentDistributionTask;
                report.MonthlyTrends = await monthlyTrendTask;
                report.TopPerformers = await topPerformersTask;

                // Merge summary statistics
                report.TotalGrowers = summaryStats.TotalGrowers;
                report.TotalReceiptsValue = summaryStats.TotalReceiptsValue;
                report.TotalPaymentsMade = summaryStats.TotalPaymentsMade;
                report.OutstandingBalance = summaryStats.OutstandingBalance;
                report.AveragePaymentPerGrower = summaryStats.AveragePaymentPerGrower;
                report.TotalReceipts = summaryStats.TotalReceipts;
                report.TotalWeight = summaryStats.TotalWeight;
                report.Advance1Total = summaryStats.Advance1Total;
                report.Advance2Total = summaryStats.Advance2Total;
                report.Advance3Total = summaryStats.Advance3Total;
                report.FinalPaymentTotal = summaryStats.FinalPaymentTotal;
                report.TotalDeductions = summaryStats.TotalDeductions;
                report.PremiumTotal = summaryStats.PremiumTotal;
                
                // Advance Cheques & Deductions Statistics
                report.TotalAdvanceCheques = summaryStats.TotalAdvanceCheques;
                report.TotalAdvanceChequesAmount = summaryStats.TotalAdvanceChequesAmount;
                report.TotalAdvanceChequesOutstanding = summaryStats.TotalAdvanceChequesOutstanding;
                report.TotalAdvanceDeductions = summaryStats.TotalAdvanceDeductions;
                report.TotalAdvanceDeductionsAmount = summaryStats.TotalAdvanceDeductionsAmount;
                report.ActiveAdvanceCheques = summaryStats.ActiveAdvanceCheques;
                report.FullyDeductedAdvanceCheques = summaryStats.FullyDeductedAdvanceCheques;

                var endTime = DateTime.Now;
                var totalDuration = endTime - startTime;
                Logger.Info($"CORRECTED Payment Summary Report generated successfully with {report.TotalGrowers} growers in {totalDuration.TotalMilliseconds:F0}ms at {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                return report;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating CORRECTED Payment Summary Report: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Interface Implementation

        public async Task<PaymentSummaryReport> GenerateReportAsync(ReportFilterOptions options)
        {
            return await GeneratePaymentSummaryReportAsync(options);
        }

        #endregion

        #region CORRECTED Receipt Value Calculation

        /// <summary>
        /// CORRECTED: Calculate receipt value using PriceDetails table instead of weight
        /// This function can be reused throughout the application
        /// </summary>
        public async Task<decimal> CalculateReceiptValueAsync(int receiptId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        r.FinalWeight,
                        rpa.PricePerPound,
                        (r.FinalWeight * rpa.PricePerPound) as CalculatedValue
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    WHERE r.ReceiptId = @ReceiptId
                      AND r.DeletedAt IS NULL
                      AND r.IsVoided = 0
                      AND rpa.Status IN ('Finalized', 'Posted')";

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { ReceiptId = receiptId });
                
                if (result != null)
                {
                    var weight = GetDecimalValue(GetPropertySafely(result, "FinalWeight"));
                    var pricePerPound = GetDecimalValue(GetPropertySafely(result, "PricePerPound"));
                    return weight * pricePerPound;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating receipt value for receipt {receiptId}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// CORRECTED: Get total receipt value for a grower using proper price calculations
        /// </summary>
        public async Task<decimal> GetGrowerReceiptValueAsync(int growerId, DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        SUM(r.FinalWeight * rpa.PricePerPound) as TotalValue
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    WHERE r.GrowerId = @GrowerId
                      AND r.ReceiptDate >= @StartDate
                      AND r.ReceiptDate <= @EndDate
                      AND r.DeletedAt IS NULL
                      AND r.IsVoided = 0
                      AND rpa.Status IN ('Finalized', 'Posted')";

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new 
                { 
                    GrowerId = growerId, 
                    StartDate = startDate, 
                    EndDate = endDate 
                });

                return GetDecimalValue(GetPropertySafely(result, "TotalValue"));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting grower receipt value for grower {growerId}: {ex.Message}", ex);
                return 0;
            }
        }

        #endregion

        #region CORRECTED Grower Payment Details

        public async Task<List<GrowerPaymentDetail>> GetGrowerPaymentDetailsAsync(ReportFilterOptions options)
        {
            var startTime = DateTime.Now;
            try
            {
                Logger.Info($"Starting GetGrowerPaymentDetailsAsync (using consolidated method) at {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                // Use the consolidated method to get grower details
                var (growerDetails, _) = await GetPaymentSummaryDataAsync(options);
                
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                Logger.Info($"Retrieved {growerDetails.Count} grower payment details via consolidated method in {duration.TotalMilliseconds:F0}ms at {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                return growerDetails;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving grower payment details: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region CORRECTED Summary Statistics

        /// <summary>
        /// Consolidated method that retrieves both individual grower data and summary statistics in a single query
        /// </summary>
        public async Task<(List<GrowerPaymentDetail> GrowerDetails, PaymentSummaryReport Summary)> GetPaymentSummaryDataAsync(ReportFilterOptions options)
        {
            var startTime = DateTime.Now;
            try
            {
                Logger.Info($"Starting GetPaymentSummaryDataAsync at {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Build the base query with CTEs
                var sql = @"
                    WITH ReceiptTotals AS (
                        SELECT 
                            r.GrowerId,
                            SUM(r.FinalWeight * pd.PricePerPound) as TotalReceiptsValue,
                            COUNT(DISTINCT r.ReceiptId) as TotalReceipts,
                            SUM(r.FinalWeight) as TotalWeight,
                            MIN(r.ReceiptDate) as FirstReceiptDate,
                            MAX(r.ReceiptDate) as LastReceiptDate
                        FROM Receipts r
                        INNER JOIN PriceDetails pd ON r.PriceClassId = pd.PriceClassId AND r.Grade = pd.PriceGradeId
                        WHERE r.ReceiptDate >= @PeriodStart 
                          AND r.ReceiptDate <= @PeriodEnd
                          AND r.DeletedAt IS NULL
                          AND r.IsVoided = 0
                          AND pd.PriceAdvanceId = 4
                        GROUP BY r.GrowerId
                    ),
                    PaymentTotals AS (
                        SELECT 
                            ga.GrowerId,
                            SUM(CASE WHEN ga.TransactionType = 'Payment' AND ga.CreditAmount > 0 THEN ga.CreditAmount ELSE 0 END) as TotalPaymentsMade,
                            SUM(CASE WHEN ga.TransactionType = 'Deduction' THEN ga.DebitAmount ELSE 0 END) as TotalDeductions,
                            SUM(CASE WHEN ga.TransactionType = 'Premium' THEN ga.CreditAmount ELSE 0 END) as PremiumAmount,
                            MAX(CASE WHEN ga.TransactionType = 'Payment' AND ga.CreditAmount > 0 THEN ga.TransactionDate END) as LastPaymentDate
                        FROM GrowerAccounts ga
                        WHERE ga.TransactionDate >= @PeriodStart 
                          AND ga.TransactionDate <= @PeriodEnd
                          AND ga.DeletedAt IS NULL
                        GROUP BY ga.GrowerId
                    ),
                    PaymentBreakdown AS (
                        SELECT 
                            r.GrowerId,
                            SUM(CASE WHEN rpa.PaymentTypeId = 1 THEN rpa.AmountPaid ELSE 0 END) as Advance1Paid,
                            SUM(CASE WHEN rpa.PaymentTypeId = 2 THEN rpa.AmountPaid ELSE 0 END) as Advance2Paid,
                            SUM(CASE WHEN rpa.PaymentTypeId = 3 THEN rpa.AmountPaid ELSE 0 END) as Advance3Paid,
                            SUM(CASE WHEN rpa.PaymentTypeId = 4 THEN rpa.AmountPaid ELSE 0 END) as FinalPaymentPaid
                        FROM Receipts r
                        INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        WHERE r.ReceiptDate >= @PeriodStart 
                          AND r.ReceiptDate <= @PeriodEnd
                          AND r.DeletedAt IS NULL
                          AND r.IsVoided = 0
                          AND rpa.Status IN ('Finalized', 'Posted')
                        GROUP BY r.GrowerId
                    ),
                    AdvanceChequesSummary AS (
                        SELECT 
                            -- Advance Cheques Statistics
                            COUNT(DISTINCT ac.AdvanceChequeId) as TotalAdvanceCheques,
                            ISNULL(SUM(ac.AdvanceAmount), 0) as TotalAdvanceChequesAmount,
                            ISNULL(SUM(ac.CurrentAdvanceAmount), 0) as TotalAdvanceChequesOutstanding,
                            COUNT(DISTINCT CASE WHEN ac.IsFullyDeducted = 0 THEN ac.AdvanceChequeId END) as ActiveAdvanceCheques,
                            COUNT(DISTINCT CASE WHEN ac.IsFullyDeducted = 1 THEN ac.AdvanceChequeId END) as FullyDeductedAdvanceCheques,
                            
                            -- Advance Deductions Statistics
                            COUNT(DISTINCT ad.DeductionId) as TotalAdvanceDeductions,
                            ISNULL(SUM(ad.DeductionAmount), 0) as TotalAdvanceDeductionsAmount
                        FROM AdvanceCheques ac
                        LEFT JOIN AdvanceDeductions ad ON ac.AdvanceChequeId = ad.AdvanceChequeId
                        WHERE ac.AdvanceDate >= @PeriodStart 
                          AND ac.AdvanceDate <= @PeriodEnd
                          AND ac.DeletedAt IS NULL
                          AND ac.Status IN ('Printed', 'FullyDeducted', 'PartiallyDeducted')
                          AND (ad.DeductionDate IS NULL OR (ad.DeductionDate >= @PeriodStart AND ad.DeductionDate <= @PeriodEnd))
                          AND (ad.DeletedAt IS NULL OR ad.DeletedAt IS NULL)
                          AND (ad.Status = 'Active' OR ad.Status IS NULL)
                    ),
                    FilteredGrowers AS (
                        SELECT DISTINCT g.GrowerId
                        FROM Growers g
                        WHERE 1=1";

                // Add filters to the FilteredGrowers CTE
                if (!options.IncludeInactiveGrowers)
                    sql += " AND g.IsActive = 1";
                
                if (!options.IncludeOnHoldGrowers)
                    sql += " AND g.IsOnHold = 0";

                if (options.SelectedGrowerIds?.Any() == true)
                    sql += " AND g.GrowerId IN @SelectedGrowerIds";

                sql += @"
                    ),
                    GrowerCount AS (
                        SELECT COUNT(*) as TotalGrowers
                        FROM FilteredGrowers
                    )
                    SELECT 
                        g.GrowerId,
                        g.GrowerNumber,
                        g.FullName,
                        g.CheckPayeeName,
                        g.City,
                        g.Province,
                        g.PhoneNumber,
                        g.Email,
                        g.Address,
                        g.PostalCode,
                        g.IsActive,
                        g.IsOnHold,
                        
                        -- Individual grower totals
                        ISNULL(rt.TotalReceiptsValue, 0) as TotalReceiptsValue,
                        ISNULL(rt.TotalReceipts, 0) as TotalReceipts,
                        ISNULL(rt.TotalWeight, 0) as TotalWeight,
                        ISNULL(pt.TotalPaymentsMade, 0) as TotalPaymentsMade,
                        ISNULL(pb.Advance1Paid, 0) as Advance1Paid,
                        ISNULL(pb.Advance2Paid, 0) as Advance2Paid,
                        ISNULL(pb.Advance3Paid, 0) as Advance3Paid,
                        ISNULL(pb.FinalPaymentPaid, 0) as FinalPaymentPaid,
                        ISNULL(pt.TotalDeductions, 0) as TotalDeductions,
                        ISNULL(pt.PremiumAmount, 0) as PremiumAmount,
                        ISNULL(pt.LastPaymentDate, NULL) as LastPaymentDate,
                        rt.FirstReceiptDate,
                        rt.LastReceiptDate,
                        
                        -- Summary totals (using window functions)
                        SUM(ISNULL(rt.TotalReceiptsValue, 0)) OVER() as GrandTotalReceiptsValue,
                        SUM(ISNULL(pt.TotalPaymentsMade, 0)) OVER() as GrandTotalPaymentsMade,
                        SUM(ISNULL(pb.Advance1Paid, 0)) OVER() as GrandAdvance1Total,
                        SUM(ISNULL(pb.Advance2Paid, 0)) OVER() as GrandAdvance2Total,
                        SUM(ISNULL(pb.Advance3Paid, 0)) OVER() as GrandAdvance3Total,
                        SUM(ISNULL(pb.FinalPaymentPaid, 0)) OVER() as GrandFinalPaymentTotal,
                        SUM(ISNULL(pt.TotalDeductions, 0)) OVER() as GrandTotalDeductions,
                        SUM(ISNULL(pt.PremiumAmount, 0)) OVER() as GrandPremiumTotal,
                        SUM(ISNULL(rt.TotalReceipts, 0)) OVER() as GrandTotalReceipts,
                        SUM(ISNULL(rt.TotalWeight, 0)) OVER() as GrandTotalWeight,
                        gc.TotalGrowers as GrandTotalGrowers,
                        
                        -- Advance Cheques Summary (same for all rows)
                        acs.TotalAdvanceCheques,
                        acs.TotalAdvanceChequesAmount,
                        acs.TotalAdvanceChequesOutstanding,
                        acs.TotalAdvanceDeductions,
                        acs.TotalAdvanceDeductionsAmount,
                        acs.ActiveAdvanceCheques,
                        acs.FullyDeductedAdvanceCheques
                        
                    FROM Growers g
                    INNER JOIN FilteredGrowers fg ON g.GrowerId = fg.GrowerId
                    LEFT JOIN ReceiptTotals rt ON g.GrowerId = rt.GrowerId
                    LEFT JOIN PaymentTotals pt ON g.GrowerId = pt.GrowerId
                    LEFT JOIN PaymentBreakdown pb ON g.GrowerId = pb.GrowerId
                    CROSS JOIN GrowerCount gc
                    CROSS JOIN AdvanceChequesSummary acs
                    WHERE 1=1";

                sql += @"
                    ORDER BY g.FullName";

                var parameters = new
                {
                    PeriodStart = options.PeriodStart,
                    PeriodEnd = options.PeriodEnd,
                    SelectedGrowerIds = options.SelectedGrowerIds
                };

                var results = await connection.QueryAsync<dynamic>(sql, parameters);
                var resultList = results.ToList();

                // Process individual grower details
                var growerDetails = new List<GrowerPaymentDetail>();
                PaymentSummaryReport? summary = null;

                foreach (var row in resultList)
                {
                    if (row == null) continue;
                    
                    try
                    {
                        var totalReceiptsValue = GetDecimalValue(GetPropertySafely(row, "TotalReceiptsValue"));
                        var totalPaymentsMade = GetDecimalValue(GetPropertySafely(row, "TotalPaymentsMade"));
                        var totalDeductions = GetDecimalValue(GetPropertySafely(row, "TotalDeductions"));
                        var outstandingBalance = totalReceiptsValue - totalPaymentsMade - totalDeductions;

                        var detail = new GrowerPaymentDetail
                        {
                            GrowerId = GetIntValue(GetPropertySafely(row, "GrowerId")),
                            GrowerNumber = GetStringValue(GetPropertySafely(row, "GrowerNumber")),
                            FullName = GetStringValue(GetPropertySafely(row, "FullName")),
                            CheckPayeeName = GetStringValue(GetPropertySafely(row, "CheckPayeeName")),
                            City = GetStringValue(GetPropertySafely(row, "City")),
                            Province = GetStringValue(GetPropertySafely(row, "Province")),
                            PhoneNumber = GetStringValue(GetPropertySafely(row, "PhoneNumber")),
                            Email = GetStringValue(GetPropertySafely(row, "Email")),
                            Address = GetStringValue(GetPropertySafely(row, "Address")),
                            PostalCode = GetStringValue(GetPropertySafely(row, "PostalCode")),
                            CurrencyCode = "CAD",
                            IsActive = GetBoolValue(GetPropertySafely(row, "IsActive")),
                            IsOnHold = GetBoolValue(GetPropertySafely(row, "IsOnHold")),
                            
                            // Receipt Information
                            TotalReceiptsValue = totalReceiptsValue,
                            TotalReceipts = GetIntValue(GetPropertySafely(row, "TotalReceipts")),
                            TotalWeight = GetDecimalValue(GetPropertySafely(row, "TotalWeight")),
                            FirstReceiptDate = GetDateTimeValue(GetPropertySafely(row, "FirstReceiptDate")),
                            LastReceiptDate = GetDateTimeValue(GetPropertySafely(row, "LastReceiptDate")),
                            
                            // Payment Information
                            Advance1Paid = GetDecimalValue(GetPropertySafely(row, "Advance1Paid")),
                            Advance2Paid = GetDecimalValue(GetPropertySafely(row, "Advance2Paid")),
                            Advance3Paid = GetDecimalValue(GetPropertySafely(row, "Advance3Paid")),
                            FinalPaymentPaid = GetDecimalValue(GetPropertySafely(row, "FinalPaymentPaid")),
                            TotalDeductions = totalDeductions,
                            PremiumAmount = GetDecimalValue(GetPropertySafely(row, "PremiumAmount")),
                            
                            // Calculated Fields
                            OutstandingBalance = outstandingBalance,
                            
                            // Performance Metrics
                            PaymentCompletionPercentage = totalReceiptsValue > 0 
                                ? (totalPaymentsMade / totalReceiptsValue) * 100 
                                : 0,
                            AveragePaymentPerReceipt = GetIntValue(GetPropertySafely(row, "TotalReceipts")) > 0 
                                ? totalPaymentsMade / GetIntValue(GetPropertySafely(row, "TotalReceipts")) 
                                : 0,
                            AverageReceiptValue = GetIntValue(GetPropertySafely(row, "TotalReceipts")) > 0 
                                ? totalReceiptsValue / GetIntValue(GetPropertySafely(row, "TotalReceipts")) 
                                : 0,
                            AverageReceiptWeight = GetIntValue(GetPropertySafely(row, "TotalReceipts")) > 0 
                                ? GetDecimalValue(GetPropertySafely(row, "TotalWeight")) / GetIntValue(GetPropertySafely(row, "TotalReceipts")) 
                                : 0,

                            // Status Information
                            PaymentStatus = DeterminePaymentStatus(totalReceiptsValue, totalPaymentsMade, totalDeductions),
                            LastPaymentDate = GetDateTimeValue(GetPropertySafely(row, "LastPaymentDate")),
                            DaysSinceLastPayment = CalculateDaysSince(GetDateTimeValue(GetPropertySafely(row, "LastPaymentDate"))) ?? 0,
                            DaysSinceLastReceipt = CalculateDaysSince(GetDateTimeValue(GetPropertySafely(row, "LastReceiptDate"))) ?? 0,
                            PaymentVelocity = CalculatePaymentVelocity(
                                GetIntValue(GetPropertySafely(row, "TotalReceipts")), 
                                GetDateTimeValue(GetPropertySafely(row, "FirstReceiptDate")), 
                                GetDateTimeValue(GetPropertySafely(row, "LastReceiptDate")))
                        };

                        growerDetails.Add(detail);

                        // Create summary from first row (all rows have same summary totals due to window functions)
                        if (summary == null)
                        {
                            var grandTotalReceiptsValue = GetDecimalValue(GetPropertySafely(row, "GrandTotalReceiptsValue"));
                            var grandTotalPaymentsMade = GetDecimalValue(GetPropertySafely(row, "GrandTotalPaymentsMade"));
                            var grandOutstandingBalance = grandTotalReceiptsValue - grandTotalPaymentsMade - GetDecimalValue(GetPropertySafely(row, "GrandTotalDeductions"));

                            summary = new PaymentSummaryReport
                            {
                                TotalGrowers = GetIntValue(GetPropertySafely(row, "GrandTotalGrowers")),
                                TotalReceiptsValue = grandTotalReceiptsValue,
                                TotalPaymentsMade = grandTotalPaymentsMade,
                                OutstandingBalance = grandOutstandingBalance,
                                TotalReceipts = GetIntValue(GetPropertySafely(row, "GrandTotalReceipts")),
                                TotalWeight = GetDecimalValue(GetPropertySafely(row, "GrandTotalWeight")),
                                Advance1Total = GetDecimalValue(GetPropertySafely(row, "GrandAdvance1Total")),
                                Advance2Total = GetDecimalValue(GetPropertySafely(row, "GrandAdvance2Total")),
                                Advance3Total = GetDecimalValue(GetPropertySafely(row, "GrandAdvance3Total")),
                                FinalPaymentTotal = GetDecimalValue(GetPropertySafely(row, "GrandFinalPaymentTotal")),
                                TotalDeductions = GetDecimalValue(GetPropertySafely(row, "GrandTotalDeductions")),
                                PremiumTotal = GetDecimalValue(GetPropertySafely(row, "GrandPremiumTotal")),
                                
                                // Advance Cheques & Deductions Statistics
                                TotalAdvanceCheques = GetIntValue(GetPropertySafely(row, "TotalAdvanceCheques")),
                                TotalAdvanceChequesAmount = GetDecimalValue(GetPropertySafely(row, "TotalAdvanceChequesAmount")),
                                TotalAdvanceChequesOutstanding = GetDecimalValue(GetPropertySafely(row, "TotalAdvanceChequesOutstanding")),
                                TotalAdvanceDeductions = GetIntValue(GetPropertySafely(row, "TotalAdvanceDeductions")),
                                TotalAdvanceDeductionsAmount = GetDecimalValue(GetPropertySafely(row, "TotalAdvanceDeductionsAmount")),
                                ActiveAdvanceCheques = GetIntValue(GetPropertySafely(row, "ActiveAdvanceCheques")),
                                FullyDeductedAdvanceCheques = GetIntValue(GetPropertySafely(row, "FullyDeductedAdvanceCheques"))
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error processing grower row: {ex.Message}");
                        continue;
                    }
                }

                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                Logger.Info($"Retrieved {growerDetails.Count} grower details and summary statistics in {duration.TotalMilliseconds:F0}ms at {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                return (growerDetails, summary ?? new PaymentSummaryReport());
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving payment summary data: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<PaymentSummaryReport> GetSummaryStatisticsAsync(ReportFilterOptions options)
        {
            var startTime = DateTime.Now;
            try
            {
                Logger.Info($"Starting GetSummaryStatisticsAsync (using consolidated method) at {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                // Use the consolidated method to get summary statistics
                var (_, summary) = await GetPaymentSummaryDataAsync(options);
                
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                Logger.Info($"Retrieved summary statistics via consolidated method in {duration.TotalMilliseconds:F0}ms at {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                return summary;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving summary statistics: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region NEW: Advance Cheques and Deductions Summary

        /// <summary>
        /// NEW: Get summary of AdvanceCheques and AdvanceDeductions
        /// </summary>
        public async Task<AdvanceSummary> GetAdvanceSummaryAsync(ReportFilterOptions options)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        -- Advance Cheques Summary
                        COUNT(DISTINCT ac.AdvanceChequeId) as TotalAdvanceCheques,
                        ISNULL(SUM(ac.AdvanceAmount), 0) as TotalAdvanceAmount,
                        ISNULL(SUM(ac.CurrentAdvanceAmount), 0) as TotalCurrentAdvanceAmount,
                        ISNULL(SUM(ac.TotalDeductedAmount), 0) as TotalDeductedFromAdvances,
                        
                        -- Advance Deductions Summary
                        COUNT(DISTINCT ad.DeductionId) as TotalAdvanceDeductions,
                        ISNULL(SUM(ad.DeductionAmount), 0) as TotalAdvanceDeductionAmount,
                        
                        -- Active vs Fully Deducted
                        COUNT(DISTINCT CASE WHEN ac.IsFullyDeducted = 0 THEN ac.AdvanceChequeId END) as ActiveAdvanceCheques,
                        COUNT(DISTINCT CASE WHEN ac.IsFullyDeducted = 1 THEN ac.AdvanceChequeId END) as FullyDeductedAdvanceCheques
                        
                    FROM Growers g
                    LEFT JOIN AdvanceCheques ac ON g.GrowerId = ac.GrowerId
                        AND ac.AdvanceDate >= @PeriodStart 
                        AND ac.AdvanceDate <= @PeriodEnd
                        AND ac.DeletedAt IS NULL
                    LEFT JOIN AdvanceDeductions ad ON ac.AdvanceChequeId = ad.AdvanceChequeId
                        AND ad.DeductionDate >= @PeriodStart 
                        AND ad.DeductionDate <= @PeriodEnd
                        AND ad.DeletedAt IS NULL
                        AND ad.IsVoided = 0
                    WHERE 1=1";

                // Add filters
                if (!options.IncludeInactiveGrowers)
                    sql += " AND g.IsActive = 1";
                
                if (!options.IncludeOnHoldGrowers)
                    sql += " AND g.IsOnHold = 0";

                if (options.SelectedGrowerIds?.Any() == true)
                    sql += " AND g.GrowerId IN @SelectedGrowerIds";

                var parameters = new
                {
                    PeriodStart = options.PeriodStart,
                    PeriodEnd = options.PeriodEnd,
                    SelectedGrowerIds = options.SelectedGrowerIds
                };

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters);

                var summary = new AdvanceSummary
                {
                    TotalAdvanceCheques = result?.TotalAdvanceCheques ?? 0,
                    TotalAdvanceAmount = result?.TotalAdvanceAmount ?? 0,
                    TotalCurrentAdvanceAmount = result?.TotalCurrentAdvanceAmount ?? 0,
                    TotalDeductedFromAdvances = result?.TotalDeductedFromAdvances ?? 0,
                    TotalAdvanceDeductions = result?.TotalAdvanceDeductions ?? 0,
                    TotalAdvanceDeductionAmount = result?.TotalAdvanceDeductionAmount ?? 0,
                    ActiveAdvanceCheques = result?.ActiveAdvanceCheques ?? 0,
                    FullyDeductedAdvanceCheques = result?.FullyDeductedAdvanceCheques ?? 0
                };

                Logger.Info($"Retrieved advance summary: {summary.TotalAdvanceCheques} advance cheques, ${summary.TotalAdvanceAmount:C2} total amount");
                return summary;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving advance summary: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Chart Data (Updated with corrected calculations)

        public async Task<List<PaymentDistributionChart>> GetPaymentDistributionDataAsync(ReportFilterOptions options)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    -- Advance Payment Breakdown using ReceiptPaymentAllocations
                    SELECT 
                        'Advance 1 Payments' as Category,
                        ISNULL(SUM(rpa.AmountPaid), 0) as Value,
                        'Advance 1 payment allocations' as Description,
                        COUNT(DISTINCT r.GrowerId) as Count,
                        '#FF6384' as Color
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    WHERE r.ReceiptDate >= @PeriodStart 
                        AND r.ReceiptDate <= @PeriodEnd
                        AND r.DeletedAt IS NULL
                        AND r.IsVoided = 0
                        AND rpa.Status IN ('Finalized', 'Posted')
                        AND rpa.PaymentTypeId = 1
                UNION ALL
                    SELECT 
                        'Advance 2 Payments' as Category,
                        ISNULL(SUM(rpa.AmountPaid), 0) as Value,
                        'Advance 2 payment allocations' as Description,
                        COUNT(DISTINCT r.GrowerId) as Count,
                        '#36A2EB' as Color
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    WHERE r.ReceiptDate >= @PeriodStart 
                        AND r.ReceiptDate <= @PeriodEnd
                        AND r.DeletedAt IS NULL
                        AND r.IsVoided = 0
                        AND rpa.Status IN ('Finalized', 'Posted')
                        AND rpa.PaymentTypeId = 2
                UNION ALL
                    SELECT 
                        'Advance 3 Payments' as Category,
                        ISNULL(SUM(rpa.AmountPaid), 0) as Value,
                        'Advance 3 payment allocations' as Description,
                        COUNT(DISTINCT r.GrowerId) as Count,
                        '#FFCE56' as Color
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    WHERE r.ReceiptDate >= @PeriodStart 
                        AND r.ReceiptDate <= @PeriodEnd
                        AND r.DeletedAt IS NULL
                        AND r.IsVoided = 0
                        AND rpa.Status IN ('Finalized', 'Posted')
                        AND rpa.PaymentTypeId = 3
                UNION ALL
                    SELECT 
                        'Final Payments' as Category,
                        ISNULL(SUM(rpa.AmountPaid), 0) as Value,
                        'Final payment allocations' as Description,
                        COUNT(DISTINCT r.GrowerId) as Count,
                        '#4BC0C0' as Color
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    WHERE r.ReceiptDate >= @PeriodStart 
                        AND r.ReceiptDate <= @PeriodEnd
                        AND r.DeletedAt IS NULL
                        AND r.IsVoided = 0
                        AND rpa.Status IN ('Finalized', 'Posted')
                        AND rpa.PaymentTypeId = 4
                UNION ALL
                    -- Account Transactions (Deductions and Premiums)
                    SELECT 
                        'Account Deductions' as Category,
                        ISNULL(SUM(CASE WHEN ga.TransactionType = 'Deduction' THEN ga.DebitAmount ELSE 0 END), 0) as Value,
                        'Deduction transactions from grower accounts' as Description,
                        COUNT(DISTINCT CASE WHEN ga.TransactionType = 'Deduction' THEN ga.GrowerId END) as Count,
                        '#FF9F40' as Color
                    FROM GrowerAccounts ga
                    WHERE ga.TransactionDate >= @PeriodStart 
                        AND ga.TransactionDate <= @PeriodEnd
                        AND ga.DeletedAt IS NULL
                        AND ga.TransactionType = 'Deduction'
                UNION ALL
                    SELECT 
                        'Account Premiums' as Category,
                        ISNULL(SUM(CASE WHEN ga.TransactionType = 'Premium' THEN ga.CreditAmount ELSE 0 END), 0) as Value,
                        'Premium payments from grower accounts' as Description,
                        COUNT(DISTINCT CASE WHEN ga.TransactionType = 'Premium' THEN ga.GrowerId END) as Count,
                        '#FF6384' as Color
                    FROM GrowerAccounts ga
                    WHERE ga.TransactionDate >= @PeriodStart 
                        AND ga.TransactionDate <= @PeriodEnd
                        AND ga.DeletedAt IS NULL
                        AND ga.TransactionType = 'Premium'";

                var parameters = new
                {
                    PeriodStart = options.PeriodStart,
                    PeriodEnd = options.PeriodEnd
                };

                var results = await connection.QueryAsync<dynamic>(sql, parameters);

                var chartData = new List<PaymentDistributionChart>();
                foreach (var row in results)
                {
                    if (row == null) continue;

                    chartData.Add(new PaymentDistributionChart
                    {
                        Category = GetStringValue(GetPropertySafely(row, "Category")),
                        Value = GetDecimalValue(GetPropertySafely(row, "Value")),
                        Description = GetStringValue(GetPropertySafely(row, "Description")),
                        Count = GetIntValue(GetPropertySafely(row, "Count")),
                        Color = GetStringValue(GetPropertySafely(row, "Color"))
                    });
                }

                // Calculate percentages
                var totalValue = chartData.Sum(x => x.Value);
                foreach (var item in chartData)
                {
                    item.Percentage = totalValue > 0 ? (item.Value / totalValue) * 100 : 0;
                }

                Logger.Info($"Retrieved {chartData.Count} CORRECTED payment distribution chart items with percentages");
                return chartData;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving CORRECTED payment distribution data: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private string DeterminePaymentStatus(decimal totalReceiptsValue, decimal totalPaymentsMade, decimal totalDeductions)
        {
            if (totalReceiptsValue <= 0) return "No Receipts";
            
            var outstandingBalance = totalReceiptsValue - totalPaymentsMade - totalDeductions;
            
            if (outstandingBalance <= 0) return "Complete";
            if (totalPaymentsMade > 0) return "Partial";
            return "Pending";
        }

        private decimal CalculatePaymentVelocity(int totalReceipts, DateTime? firstReceiptDate, DateTime? lastReceiptDate)
        {
            if (totalReceipts <= 0 || !firstReceiptDate.HasValue || !lastReceiptDate.HasValue)
                return 0;

            var daysDiff = (lastReceiptDate.Value - firstReceiptDate.Value).Days;
            if (daysDiff <= 0) return 0;

            return (decimal)totalReceipts / daysDiff * 30; // Receipts per month
        }

        private int? CalculateDaysSince(DateTime? date)
        {
            if (!date.HasValue) return null;
            return (DateTime.Now - date.Value).Days;
        }

        // Safe property access methods
        private object? GetPropertySafely(dynamic obj, string propertyName)
        {
            try
            {
                if (obj == null) return null;
                
                // Try to access as IDictionary first
                if (obj is IDictionary<string, object> dict)
                {
                    return dict.TryGetValue(propertyName, out var value) ? value : null;
                }
                
                // Try to access as dynamic property
                var property = ((object)obj).GetType().GetProperty(propertyName);
                if (property != null)
                {
                    return property.GetValue(obj);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error accessing property '{propertyName}': {ex.Message}");
                return null;
            }
        }

        private decimal GetDecimalValue(object? value)
        {
            if (value == null) return 0;
            
            try
            {
                // Handle DBNull
                if (value == DBNull.Value) return 0;
                
                // Try direct conversion first
                if (value is decimal decimalValue) return decimalValue;
                
                // Try parsing from string
                if (decimal.TryParse(value.ToString(), out var result)) return result;
                
                // Try converting from other numeric types
                if (value is int intValue) return intValue;
                if (value is double doubleValue) return (decimal)doubleValue;
                if (value is float floatValue) return (decimal)floatValue;
                
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error converting value '{value}' to decimal: {ex.Message}");
                return 0;
            }
        }

        private int GetIntValue(object? value)
        {
            if (value == null) return 0;
            
            try
            {
                // Handle DBNull
                if (value == DBNull.Value) return 0;
                
                // Try direct conversion first
                if (value is int intValue) return intValue;
                
                // Try parsing from string
                if (int.TryParse(value.ToString(), out var result)) return result;
                
                // Try converting from decimal/double
                if (value is decimal decimalValue) return (int)decimalValue;
                if (value is double doubleValue) return (int)doubleValue;
                if (value is float floatValue) return (int)floatValue;
                
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Error converting value '{value}' to int: {ex.Message}");
                return 0;
            }
        }

        private string GetStringValue(object? value)
        {
            return value?.ToString() ?? string.Empty;
        }

        private bool GetBoolValue(object? value)
        {
            if (value == null) return false;
            if (bool.TryParse(value.ToString(), out var result)) return result;
            return false;
        }

        private DateTime? GetDateTimeValue(object? value)
        {
            if (value == null) return null;
            if (DateTime.TryParse(value.ToString(), out var result)) return result;
            return null;
        }

        #endregion

        #region Placeholder Methods (to be implemented)

        public async Task<List<MonthlyTrendChart>> GetMonthlyTrendDataAsync(ReportFilterOptions options)
        {
            var startTime = DateTime.Now;
            try
            {
                Logger.Info($"Starting GetMonthlyTrendDataAsync for period {options.PeriodStart:yyyy-MM-dd} to {options.PeriodEnd:yyyy-MM-dd} at {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    WITH MonthlyData AS (
                        SELECT 
                            YEAR(ga.TransactionDate) as Year,
                            MONTH(ga.TransactionDate) as Month,
                            SUM(CASE WHEN ga.TransactionType = 'Payment' AND ga.CreditAmount > 0 THEN ga.CreditAmount ELSE 0 END) as TotalPayments,
                            COUNT(DISTINCT CASE WHEN ga.TransactionType = 'Payment' AND ga.CreditAmount > 0 THEN ga.GrowerId END) as PaymentCount,
                            COUNT(DISTINCT ga.GrowerId) as GrowerCount
                        FROM GrowerAccounts ga
                        WHERE ga.TransactionDate >= @PeriodStart 
                          AND ga.TransactionDate <= @PeriodEnd
                          AND ga.DeletedAt IS NULL
                        GROUP BY YEAR(ga.TransactionDate), MONTH(ga.TransactionDate)
                    ),
                    MonthlyAdvanceData AS (
                        SELECT 
                            YEAR(r.ReceiptDate) as Year,
                            MONTH(r.ReceiptDate) as Month,
                            SUM(CASE WHEN rpa.PaymentTypeId = 1 THEN rpa.AmountPaid ELSE 0 END) as Advance1Amount,
                            SUM(CASE WHEN rpa.PaymentTypeId = 2 THEN rpa.AmountPaid ELSE 0 END) as Advance2Amount,
                            SUM(CASE WHEN rpa.PaymentTypeId = 3 THEN rpa.AmountPaid ELSE 0 END) as Advance3Amount,
                            SUM(CASE WHEN rpa.PaymentTypeId = 4 THEN rpa.AmountPaid ELSE 0 END) as FinalPaymentAmount
                        FROM Receipts r
                        INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        WHERE r.ReceiptDate >= @PeriodStart 
                          AND r.ReceiptDate <= @PeriodEnd
                          AND r.DeletedAt IS NULL
                          AND r.IsVoided = 0
                          AND rpa.Status IN ('Finalized', 'Posted')
                        GROUP BY YEAR(r.ReceiptDate), MONTH(r.ReceiptDate)
                    ),
                    AllMonths AS (
                        SELECT DISTINCT 
                            YEAR(TransactionDate) as Year,
                            MONTH(TransactionDate) as Month
                        FROM GrowerAccounts
                        WHERE TransactionDate >= @PeriodStart 
                          AND TransactionDate <= @PeriodEnd
                          AND DeletedAt IS NULL
                        UNION
                        SELECT DISTINCT 
                            YEAR(ReceiptDate) as Year,
                            MONTH(ReceiptDate) as Month
                        FROM Receipts
                        WHERE ReceiptDate >= @PeriodStart 
                          AND ReceiptDate <= @PeriodEnd
                          AND DeletedAt IS NULL
                          AND IsVoided = 0
                    )
                    SELECT 
                        am.Year,
                        am.Month,
                        ISNULL(md.TotalPayments, 0) as TotalPayments,
                        ISNULL(md.PaymentCount, 0) as PaymentCount,
                        ISNULL(md.GrowerCount, 0) as GrowerCount,
                        ISNULL(mad.Advance1Amount, 0) as Advance1Amount,
                        ISNULL(mad.Advance2Amount, 0) as Advance2Amount,
                        ISNULL(mad.Advance3Amount, 0) as Advance3Amount,
                        ISNULL(mad.FinalPaymentAmount, 0) as FinalPaymentAmount
                    FROM AllMonths am
                    LEFT JOIN MonthlyData md ON am.Year = md.Year AND am.Month = md.Month
                    LEFT JOIN MonthlyAdvanceData mad ON am.Year = mad.Year AND am.Month = mad.Month
                    ORDER BY am.Year, am.Month";

                var parameters = new
                {
                    PeriodStart = options.PeriodStart,
                    PeriodEnd = options.PeriodEnd
                };

                var results = await connection.QueryAsync<dynamic>(sql, parameters);
                var resultList = results.ToList();

                var monthlyTrends = new List<MonthlyTrendChart>();

                foreach (var row in resultList)
                {
                    if (row == null) continue;
                    
                    try
                    {
                        var year = GetIntValue(GetPropertySafely(row, "Year"));
                        var month = GetIntValue(GetPropertySafely(row, "Month"));
                        var totalPayments = GetDecimalValue(GetPropertySafely(row, "TotalPayments"));
                        var paymentCount = GetIntValue(GetPropertySafely(row, "PaymentCount"));
                        var growerCount = GetIntValue(GetPropertySafely(row, "GrowerCount"));
                        var advance1Amount = GetDecimalValue(GetPropertySafely(row, "Advance1Amount"));
                        var advance2Amount = GetDecimalValue(GetPropertySafely(row, "Advance2Amount"));
                        var advance3Amount = GetDecimalValue(GetPropertySafely(row, "Advance3Amount"));
                        var finalPaymentAmount = GetDecimalValue(GetPropertySafely(row, "FinalPaymentAmount"));

                        var trend = new MonthlyTrendChart
                        {
                            Month = new DateTime(year, month, 1),
                            TotalPayments = totalPayments,
                            PaymentCount = paymentCount,
                            GrowerCount = growerCount,
                            Advance1Amount = advance1Amount,
                            Advance2Amount = advance2Amount,
                            Advance3Amount = advance3Amount,
                            FinalPaymentAmount = finalPaymentAmount
                        };

                        monthlyTrends.Add(trend);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error processing monthly trend row: {ex.Message}");
                        continue;
                    }
                }

                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                Logger.Info($"Retrieved {monthlyTrends.Count} monthly trend data points in {duration.TotalMilliseconds:F0}ms at {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                return monthlyTrends;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving monthly trend data: {ex.Message}", ex);
                return new List<MonthlyTrendChart>();
            }
        }

        public async Task<List<GrowerPerformanceChart>> GetTopPerformersAsync(ReportFilterOptions options, int count)
        {
            var startTime = DateTime.Now;
            try
            {
                Logger.Info($"Starting GetTopPerformersAsync for {count} top performers at {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    WITH ReceiptTotals AS (
                        SELECT 
                            r.GrowerId,
                            SUM(r.FinalWeight * pd.PricePerPound) as TotalReceiptsValue,
                            COUNT(DISTINCT r.ReceiptId) as TotalReceipts,
                            SUM(r.FinalWeight) as TotalWeight
                        FROM Receipts r
                        INNER JOIN PriceDetails pd ON r.PriceClassId = pd.PriceClassId AND r.Grade = pd.PriceGradeId
                        WHERE r.ReceiptDate >= @PeriodStart 
                          AND r.ReceiptDate <= @PeriodEnd
                          AND r.DeletedAt IS NULL
                          AND r.IsVoided = 0
                          AND pd.PriceAdvanceId = 4
                        GROUP BY r.GrowerId
                    ),
                    PaymentTotals AS (
                        SELECT 
                            ga.GrowerId,
                            SUM(CASE WHEN ga.TransactionType = 'Payment' AND ga.CreditAmount > 0 THEN ga.CreditAmount ELSE 0 END) as TotalPaymentsMade,
                            SUM(CASE WHEN ga.TransactionType = 'Deduction' THEN ga.DebitAmount ELSE 0 END) as TotalDeductions,
                            SUM(CASE WHEN ga.TransactionType = 'Premium' THEN ga.CreditAmount ELSE 0 END) as PremiumAmount
                        FROM GrowerAccounts ga
                        WHERE ga.TransactionDate >= @PeriodStart 
                          AND ga.TransactionDate <= @PeriodEnd
                          AND ga.DeletedAt IS NULL
                        GROUP BY ga.GrowerId
                    ),
                    FilteredGrowers AS (
                        SELECT DISTINCT g.GrowerId
                        FROM Growers g
                        WHERE 1=1";

                // Add filters to the FilteredGrowers CTE
                if (!options.IncludeInactiveGrowers)
                    sql += " AND g.IsActive = 1";
                
                if (!options.IncludeOnHoldGrowers)
                    sql += " AND g.IsOnHold = 0";

                if (options.SelectedGrowerIds?.Any() == true)
                    sql += " AND g.GrowerId IN @SelectedGrowerIds";

                sql += @"
                    )
                    SELECT TOP (@Count)
                        g.GrowerId,
                        g.GrowerNumber,
                        g.FullName,
                        g.Province,
                        ISNULL(rt.TotalReceiptsValue, 0) as TotalReceiptsValue,
                        ISNULL(rt.TotalReceipts, 0) as TotalReceipts,
                        ISNULL(pt.TotalPaymentsMade, 0) as TotalPaymentsMade,
                        ISNULL(pt.TotalDeductions, 0) as TotalDeductions,
                        ISNULL(pt.PremiumAmount, 0) as PremiumAmount,
                        CASE 
                            WHEN ISNULL(rt.TotalReceipts, 0) > 0 
                            THEN ISNULL(pt.TotalPaymentsMade, 0) / ISNULL(rt.TotalReceipts, 0) 
                            ELSE 0 
                        END as AveragePaymentPerReceipt
                    FROM Growers g
                    INNER JOIN FilteredGrowers fg ON g.GrowerId = fg.GrowerId
                    LEFT JOIN ReceiptTotals rt ON g.GrowerId = rt.GrowerId
                    LEFT JOIN PaymentTotals pt ON g.GrowerId = pt.GrowerId
                    WHERE ISNULL(pt.TotalPaymentsMade, 0) > 0
                    ORDER BY ISNULL(pt.TotalPaymentsMade, 0) DESC";

                var parameters = new
                {
                    PeriodStart = options.PeriodStart,
                    PeriodEnd = options.PeriodEnd,
                    Count = count,
                    SelectedGrowerIds = options.SelectedGrowerIds
                };

                var results = await connection.QueryAsync<dynamic>(sql, parameters);
                var resultList = results.ToList();

                var topPerformers = new List<GrowerPerformanceChart>();
                var rank = 1;

                foreach (var row in resultList)
                {
                    if (row == null) continue;
                    
                    try
                    {
                        var totalPayments = GetDecimalValue(GetPropertySafely(row, "TotalPaymentsMade"));
                        var totalReceipts = GetDecimalValue(GetPropertySafely(row, "TotalReceiptsValue"));
                        var receiptCount = GetIntValue(GetPropertySafely(row, "TotalReceipts"));
                        var averagePaymentPerReceipt = GetDecimalValue(GetPropertySafely(row, "AveragePaymentPerReceipt"));

                        var performer = new GrowerPerformanceChart
                        {
                            GrowerId = GetIntValue(GetPropertySafely(row, "GrowerId")),
                            GrowerName = GetStringValue(GetPropertySafely(row, "FullName")),
                            GrowerNumber = GetStringValue(GetPropertySafely(row, "GrowerNumber")),
                            TotalPayments = totalPayments,
                            TotalReceipts = totalReceipts,
                            ReceiptCount = receiptCount,
                            AveragePaymentPerReceipt = averagePaymentPerReceipt,
                            Province = GetStringValue(GetPropertySafely(row, "Province")),
                            Rank = rank++
                        };

                        topPerformers.Add(performer);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error processing top performer row: {ex.Message}");
                        continue;
                    }
                }

                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                Logger.Info($"Retrieved {topPerformers.Count} top performers in {duration.TotalMilliseconds:F0}ms at {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                
                return topPerformers;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving top performers: {ex.Message}", ex);
                return new List<GrowerPerformanceChart>();
            }
        }

        #endregion
    }

    /// <summary>
    /// NEW: Model for Advance Cheques and Deductions Summary
    /// </summary>
    public class AdvanceSummary
    {
        public int TotalAdvanceCheques { get; set; }
        public decimal TotalAdvanceAmount { get; set; }
        public decimal TotalCurrentAdvanceAmount { get; set; }
        public decimal TotalDeductedFromAdvances { get; set; }
        public int TotalAdvanceDeductions { get; set; }
        public decimal TotalAdvanceDeductionAmount { get; set; }
        public int ActiveAdvanceCheques { get; set; }
        public int FullyDeductedAdvanceCheques { get; set; }

        // Calculated properties
        public decimal OutstandingAdvanceAmount => TotalAdvanceAmount - TotalDeductedFromAdvances;
        public decimal AdvanceDeductionPercentage => TotalAdvanceAmount > 0 ? (TotalDeductedFromAdvances / TotalAdvanceAmount) * 100 : 0;
    }
}