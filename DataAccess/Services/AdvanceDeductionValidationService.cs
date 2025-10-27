using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for validating advance deduction data integrity and reconciliation
    /// </summary>
    public class AdvanceDeductionValidationService : BaseDatabaseService, IAdvanceDeductionValidationService
    {
        private readonly string _connectionString;

        public AdvanceDeductionValidationService(string connectionString) : base()
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Validate that advance balances are correct
        /// </summary>
        public async Task<AdvanceValidationResult> ValidateAdvanceBalancesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ac.AdvanceChequeId,
                            ac.OriginalAdvanceAmount,
                            ac.CurrentAdvanceAmount,
                            ac.TotalDeductedAmount,
                            ISNULL(SUM(ad.DeductionAmount), 0) as CalculatedDeductions,
                            ISNULL(SUM(CASE WHEN ad.IsVoided = 1 THEN ad.DeductionAmount ELSE 0 END), 0) as CalculatedVoided
                        FROM AdvanceCheques ac
                        LEFT JOIN AdvanceDeductions ad ON ac.AdvanceChequeId = ad.AdvanceChequeId 
                            AND ad.DeletedAt IS NULL
                        WHERE ac.DeletedAt IS NULL
                        GROUP BY ac.AdvanceChequeId, ac.OriginalAdvanceAmount, ac.CurrentAdvanceAmount, 
                                 ac.TotalDeductedAmount
                        HAVING ac.TotalDeductedAmount != ISNULL(SUM(ad.DeductionAmount), 0)
                            OR ac.CurrentAdvanceAmount != (ac.OriginalAdvanceAmount - ISNULL(SUM(ad.DeductionAmount), 0) + ISNULL(SUM(CASE WHEN ad.IsVoided = 1 THEN ad.DeductionAmount ELSE 0 END), 0))";

                    var discrepancies = await connection.QueryAsync(sql);

                    var result = new AdvanceValidationResult
                    {
                        IsValid = !discrepancies.Any(),
                        Discrepancies = discrepancies.ToList(),
                        Message = discrepancies.Any() ? 
                            $"Found {discrepancies.Count()} advance balance discrepancies" : 
                            "All advance balances are correct"
                    };

                    Logger.Info($"Advance balance validation completed: {result.Message}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating advance balances: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Validate that deduction totals match parent advance totals
        /// </summary>
        public async Task<AdvanceValidationResult> ValidateDeductionTotalsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ac.AdvanceChequeId,
                            ac.GrowerId,
                            ac.OriginalAdvanceAmount,
                            ac.TotalDeductedAmount,
                            ISNULL(SUM(ad.DeductionAmount), 0) as ChildDeductions,
                            (ac.TotalDeductedAmount - ISNULL(SUM(ad.DeductionAmount), 0)) as Difference
                        FROM AdvanceCheques ac
                        LEFT JOIN AdvanceDeductions ad ON ac.AdvanceChequeId = ad.AdvanceChequeId 
                            AND ad.DeletedAt IS NULL AND ad.IsVoided = 0
                        WHERE ac.DeletedAt IS NULL
                        GROUP BY ac.AdvanceChequeId, ac.GrowerId, ac.OriginalAdvanceAmount, ac.TotalDeductedAmount
                        HAVING ABS(ac.TotalDeductedAmount - ISNULL(SUM(ad.DeductionAmount), 0)) > 0.01";

                    var discrepancies = await connection.QueryAsync(sql);

                    var result = new AdvanceValidationResult
                    {
                        IsValid = !discrepancies.Any(),
                        Discrepancies = discrepancies.ToList(),
                        Message = discrepancies.Any() ? 
                            $"Found {discrepancies.Count()} deduction total discrepancies" : 
                            "All deduction totals are correct"
                    };

                    Logger.Info($"Deduction total validation completed: {result.Message}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating deduction totals: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Find orphaned deductions (deductions without valid parent advance)
        /// </summary>
        public async Task<AdvanceValidationResult> FindOrphanedDeductionsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ad.DeductionId,
                            ad.AdvanceChequeId,
                            ad.GrowerId,
                            ad.DeductionAmount,
                            ad.CreatedAt
                        FROM AdvanceDeductions ad
                        LEFT JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                        WHERE ad.DeletedAt IS NULL
                            AND (ac.AdvanceChequeId IS NULL OR ac.DeletedAt IS NOT NULL)";

                    var orphanedDeductions = await connection.QueryAsync(sql);

                    var result = new AdvanceValidationResult
                    {
                        IsValid = !orphanedDeductions.Any(),
                        Discrepancies = orphanedDeductions.ToList(),
                        Message = orphanedDeductions.Any() ? 
                            $"Found {orphanedDeductions.Count()} orphaned deductions" : 
                            "No orphaned deductions found"
                    };

                    Logger.Info($"Orphaned deduction check completed: {result.Message}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finding orphaned deductions: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Reconcile advance amounts by fixing any discrepancies
        /// </summary>
        public async Task<ReconciliationResult> ReconcileAdvanceAmountsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get all advances that need reconciliation
                            var reconciliationSql = @"
                                SELECT 
                                    ac.AdvanceChequeId,
                                    ac.OriginalAdvanceAmount,
                                    ISNULL(SUM(ad.DeductionAmount), 0) as CalculatedDeductions,
                                    ISNULL(SUM(CASE WHEN ad.IsVoided = 1 THEN ad.DeductionAmount ELSE 0 END), 0) as CalculatedVoided
                                FROM AdvanceCheques ac
                                LEFT JOIN AdvanceDeductions ad ON ac.AdvanceChequeId = ad.AdvanceChequeId 
                                    AND ad.DeletedAt IS NULL
                                WHERE ac.DeletedAt IS NULL
                                GROUP BY ac.AdvanceChequeId, ac.OriginalAdvanceAmount";

                            var advances = await connection.QueryAsync(reconciliationSql, transaction: transaction);

                            var fixedCount = 0;
                            var errors = new List<string>();

                            foreach (var advance in advances)
                            {
                                try
                                {
                                    var calculatedCurrent = advance.OriginalAdvanceAmount - advance.CalculatedDeductions + advance.CalculatedVoided;
                                    
                                    var updateSql = @"
                                        UPDATE AdvanceCheques 
                                        SET CurrentAdvanceAmount = @CurrentAmount,
                                            TotalDeductedAmount = @DeductedAmount,
                                            ModifiedAt = @ModifiedAt,
                                            ModifiedBy = @ModifiedBy
                                        WHERE AdvanceChequeId = @AdvanceChequeId";

                                    await connection.ExecuteAsync(updateSql, new
                                    {
                                        CurrentAmount = calculatedCurrent,
                                        DeductedAmount = advance.CalculatedDeductions,
                                        AdvanceChequeId = advance.AdvanceChequeId,
                                        ModifiedAt = DateTime.Now,
                                        ModifiedBy = App.CurrentUser?.Username ?? "SYSTEM"
                                    }, transaction);

                                    fixedCount++;
                                }
                                catch (Exception ex)
                                {
                                    errors.Add($"Error fixing advance {advance.AdvanceChequeId}: {ex.Message}");
                                }
                            }

                            transaction.Commit();

                            var result = new ReconciliationResult
                            {
                                IsSuccessful = errors.Count == 0,
                                FixedCount = fixedCount,
                                Errors = errors,
                                Message = $"Reconciliation completed: {fixedCount} advances fixed, {errors.Count} errors"
                            };

                            Logger.Info($"Advance reconciliation completed: {result.Message}");
                            return result;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reconciling advance amounts: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get comprehensive validation report
        /// </summary>
        public async Task<ValidationReport> GetValidationReportAsync()
        {
            try
            {
                var advanceBalanceResult = await ValidateAdvanceBalancesAsync();
                var deductionTotalResult = await ValidateDeductionTotalsAsync();
                var orphanedDeductionResult = await FindOrphanedDeductionsAsync();

                var report = new ValidationReport
                {
                    AdvanceBalanceValidation = advanceBalanceResult,
                    DeductionTotalValidation = deductionTotalResult,
                    OrphanedDeductionValidation = orphanedDeductionResult,
                    OverallIsValid = advanceBalanceResult.IsValid && 
                                   deductionTotalResult.IsValid && 
                                   orphanedDeductionResult.IsValid,
                    GeneratedAt = DateTime.Now,
                    GeneratedBy = App.CurrentUser?.Username ?? "SYSTEM"
                };

                Logger.Info($"Validation report generated: Overall valid = {report.OverallIsValid}");
                return report;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating validation report: {ex.Message}", ex);
                throw;
            }
        }
    }
}
