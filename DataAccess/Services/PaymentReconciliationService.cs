using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for payment reconciliation operations.
    /// </summary>
    public class PaymentReconciliationService : BaseDatabaseService, IPaymentReconciliationService
    {
        public PaymentReconciliationService()
        {
        }

        public async Task<ReconciliationReport> ReconcileDistributionAsync(int distributionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Get distribution details
                var distributionSql = @"
                    SELECT pd.*, COUNT(pdi.PaymentDistributionItemId) as ItemCount
                    FROM PaymentDistributions pd
                    LEFT JOIN PaymentDistributionItems pdi ON pd.PaymentDistributionId = pdi.PaymentDistributionId
                    WHERE pd.PaymentDistributionId = @DistributionId
                    GROUP BY pd.PaymentDistributionId, pd.DistributionDate, pd.TotalAmount, 
                             pd.TotalGrowers, pd.TotalBatches, pd.Status, pd.CreatedAt, pd.CreatedBy";

                var distribution = await connection.QueryFirstOrDefaultAsync<PaymentDistribution>(distributionSql, new { DistributionId = distributionId });

                if (distribution == null)
                {
                    throw new InvalidOperationException($"Distribution {distributionId} not found");
                }

                // Get actual payment amounts
                var actualAmountSql = @"
                    SELECT 
                        ISNULL(SUM(c.ChequeAmount), 0) as ChequeTotal,
                        ISNULL(SUM(ep.Amount), 0) as ElectronicTotal,
                        COUNT(c.ChequeId) as ChequeCount,
                        COUNT(ep.ElectronicPaymentId) as ElectronicCount
                    FROM PaymentDistributionItems pdi
                    LEFT JOIN Cheques c ON pdi.ChequeId = c.ChequeId
                    LEFT JOIN ElectronicPayments ep ON pdi.ElectronicPaymentId = ep.ElectronicPaymentId
                    WHERE pdi.PaymentDistributionId = @DistributionId";

                var actualAmounts = await connection.QueryFirstOrDefaultAsync(actualAmountSql, new { DistributionId = distributionId });

                var expectedAmount = distribution.TotalAmount;
                var actualAmount = (decimal)actualAmounts.ChequeTotal + (decimal)actualAmounts.ElectronicTotal;
                var difference = expectedAmount - actualAmount;

                var report = new ReconciliationReport
                {
                    PaymentDistributionId = distributionId,
                    ReportDate = DateTime.Now,
                    ExpectedAmount = expectedAmount,
                    ActualAmount = actualAmount,
                    Difference = difference,
                    Status = Math.Abs(difference) < 0.01m ? "Reconciled" : "Discrepancy",
                    GeneratedBy = App.CurrentUser?.Username ?? "SYSTEM",
                    GeneratedAt = DateTime.Now
                };

                // Save reconciliation report
                var reportSql = @"
                    INSERT INTO ReconciliationReports (
                        PaymentDistributionId, ReportDate, ExpectedAmount, ActualAmount, 
                        Difference, Status, GeneratedBy, GeneratedAt
                    )
                    VALUES (
                        @PaymentDistributionId, @ReportDate, @ExpectedAmount, @ActualAmount,
                        @Difference, @Status, @GeneratedBy, @GeneratedAt
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var reportId = await connection.ExecuteScalarAsync<int>(reportSql, new
                {
                    PaymentDistributionId = report.PaymentDistributionId,
                    ReportDate = report.ReportDate,
                    ExpectedAmount = report.ExpectedAmount,
                    ActualAmount = report.ActualAmount,
                    Difference = report.Difference,
                    Status = report.Status,
                    GeneratedBy = report.GeneratedBy,
                    GeneratedAt = report.GeneratedAt
                });

                report.ReportId = reportId;

                // Check for exceptions
                await CheckForExceptionsAsync(connection, report);

                Logger.Info($"Reconciled distribution {distributionId}: Expected {expectedAmount:C}, Actual {actualAmount:C}, Difference {difference:C}");
                return report;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reconciling distribution {distributionId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<PaymentException>> GetPaymentExceptionsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT pe.*, pd.DistributionDate, pd.TotalAmount
                    FROM PaymentExceptions pe
                    INNER JOIN PaymentDistributions pd ON pe.PaymentDistributionId = pd.PaymentDistributionId
                    WHERE pe.Status = 'Open'
                    ORDER BY pe.CreatedAt DESC";

                var exceptions = await connection.QueryAsync<PaymentException>(sql);
                return exceptions.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving payment exceptions: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> ResolveExceptionAsync(int exceptionId, string resolution, string resolvedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE PaymentExceptions 
                    SET Status = 'Resolved', ResolvedAt = @ResolvedAt, ResolvedBy = @ResolvedBy, ResolutionNotes = @Resolution
                    WHERE ExceptionId = @ExceptionId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    ResolvedAt = DateTime.Now,
                    ResolvedBy = resolvedBy,
                    Resolution = resolution,
                    ExceptionId = exceptionId
                });

                Logger.Info($"Resolved exception {exceptionId}: {resolution}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error resolving exception {exceptionId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> MarkDistributionAsCompletedAsync(int distributionId, string completedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE PaymentDistributions 
                    SET Status = 'Completed', ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy
                    WHERE PaymentDistributionId = @DistributionId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = completedBy,
                    DistributionId = distributionId
                });

                Logger.Info($"Marked distribution {distributionId} as completed");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking distribution as completed: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<PaymentException>> GetExceptionsByDistributionAsync(int distributionId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT pe.*, pd.DistributionDate, pd.TotalAmount
                    FROM PaymentExceptions pe
                    INNER JOIN PaymentDistributions pd ON pe.PaymentDistributionId = pd.PaymentDistributionId
                    WHERE pe.PaymentDistributionId = @DistributionId
                    ORDER BY pe.CreatedAt DESC";

                var exceptions = await connection.QueryAsync<PaymentException>(sql, new { DistributionId = distributionId });
                return exceptions.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving exceptions for distribution {distributionId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<PaymentException> CreateExceptionAsync(PaymentException exception)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO PaymentExceptions (
                        PaymentDistributionId, ExceptionType, Description, Status, CreatedAt, CreatedBy
                    )
                    VALUES (
                        @PaymentDistributionId, @ExceptionType, @Description, @Status, @CreatedAt, @CreatedBy
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var exceptionId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    PaymentDistributionId = exception.PaymentDistributionId,
                    ExceptionType = exception.ExceptionType,
                    Description = exception.Description,
                    Status = exception.Status,
                    CreatedAt = DateTime.Now,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                });

                exception.ExceptionId = exceptionId;
                Logger.Info($"Created payment exception {exceptionId}");
                return exception;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment exception: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetReconciliationStatisticsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        pd.Status,
                        COUNT(*) as DistributionCount,
                        SUM(pd.TotalAmount) as TotalAmount
                    FROM PaymentDistributions pd
                    GROUP BY pd.Status";

                var results = await connection.QueryAsync(sql);
                var stats = new Dictionary<string, object>();
                
                foreach (var result in results)
                {
                    stats[$"{result.Status}_Count"] = result.DistributionCount;
                    stats[$"{result.Status}_Amount"] = result.TotalAmount;
                }

                return stats;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving reconciliation statistics: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<string>> ValidateDistributionForCompletionAsync(int distributionId)
        {
            try
            {
                var issues = new List<string>();
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Check for unresolved exceptions
                var exceptionsSql = @"
                    SELECT COUNT(*) as ExceptionCount
                    FROM PaymentExceptions
                    WHERE PaymentDistributionId = @DistributionId AND Status = 'Open'";

                var exceptionCount = await connection.QueryFirstOrDefaultAsync<int>(exceptionsSql, new { DistributionId = distributionId });
                if (exceptionCount > 0)
                {
                    issues.Add($"{exceptionCount} unresolved exceptions found");
                }

                // Check for missing payments
                var missingPaymentsSql = @"
                    SELECT COUNT(*) as MissingCount
                    FROM PaymentDistributionItems pdi
                    WHERE pdi.PaymentDistributionId = @DistributionId
                    AND pdi.ChequeId IS NULL AND pdi.ElectronicPaymentId IS NULL";

                var missingCount = await connection.QueryFirstOrDefaultAsync<int>(missingPaymentsSql, new { DistributionId = distributionId });
                if (missingCount > 0)
                {
                    issues.Add($"{missingCount} missing payment records found");
                }

                return issues;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating distribution for completion: {ex.Message}", ex);
                throw;
            }
        }

        private async Task CheckForExceptionsAsync(SqlConnection connection, ReconciliationReport report)
        {
            try
            {
                // Check for missing payments
                var missingPaymentsSql = @"
                    SELECT pdi.*, g.GrowerName
                    FROM PaymentDistributionItems pdi
                    INNER JOIN Growers g ON pdi.GrowerId = g.GrowerId
                    WHERE pdi.PaymentDistributionId = @DistributionId
                    AND pdi.ChequeId IS NULL AND pdi.ElectronicPaymentId IS NULL";

                var missingPayments = await connection.QueryAsync(missingPaymentsSql, new { DistributionId = report.PaymentDistributionId });

                foreach (var payment in missingPayments)
                {
                    var exceptionSql = @"
                        INSERT INTO PaymentExceptions (
                            PaymentDistributionId, ExceptionType, Description, Status, CreatedAt, CreatedBy
                        )
                        VALUES (
                            @PaymentDistributionId, @ExceptionType, @Description, @Status, @CreatedAt, @CreatedBy
                        )";

                    await connection.ExecuteAsync(exceptionSql, new
                    {
                        PaymentDistributionId = report.PaymentDistributionId,
                        ExceptionType = "Missing Payment",
                        Description = $"No payment record found for grower {payment.GrowerName}",
                        Status = "Open",
                        CreatedAt = DateTime.Now,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                    });
                }

                // Check for amount discrepancies
                if (Math.Abs(report.Difference) > 0.01m)
                {
                    var discrepancySql = @"
                        INSERT INTO PaymentExceptions (
                            PaymentDistributionId, ExceptionType, Description, Status, CreatedAt, CreatedBy
                        )
                        VALUES (
                            @PaymentDistributionId, @ExceptionType, @Description, @Status, @CreatedAt, @CreatedBy
                        )";

                    await connection.ExecuteAsync(discrepancySql, new
                    {
                        PaymentDistributionId = report.PaymentDistributionId,
                        ExceptionType = "Amount Discrepancy",
                        Description = $"Expected {report.ExpectedAmount:C}, Actual {report.ActualAmount:C}, Difference {report.Difference:C}",
                        Status = "Open",
                        CreatedAt = DateTime.Now,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking for exceptions: {ex.Message}", ex);
                // Don't throw here as this is a secondary operation
            }
        }
    }
}
