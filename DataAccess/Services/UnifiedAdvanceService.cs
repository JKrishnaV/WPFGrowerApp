using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Exceptions;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Unified service for managing advance cheques and their deductions
    /// Consolidates functionality from AdvanceChequeService and AdvanceDeductionService
    /// </summary>
    public class UnifiedAdvanceService : BaseDatabaseService, IUnifiedAdvanceService
    {
        private readonly IGrowerService _growerService;

        public UnifiedAdvanceService(IGrowerService growerService)
        {
            _growerService = growerService;
        }

        #region Advance Cheque Management

        public async Task<AdvanceCheque> CreateAdvanceChequeAsync(int growerId, decimal amount, string reason, string createdBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // First, try with enhanced columns
                var query = @"
                    INSERT INTO AdvanceCheques (
                        GrowerId, AdvanceAmount, OriginalAdvanceAmount, CurrentAdvanceAmount, AdvanceDate, Reason, 
                        Status, CreatedBy, CreatedAt
                    )
                    VALUES (
                        @GrowerId, @AdvanceAmount, @OriginalAdvanceAmount, @CurrentAdvanceAmount, @AdvanceDate, @Reason,
                        @Status, @CreatedBy, @CreatedAt
                    );
                    SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);
                command.Parameters.AddWithValue("@AdvanceAmount", amount);
                command.Parameters.AddWithValue("@OriginalAdvanceAmount", amount);
                command.Parameters.AddWithValue("@CurrentAdvanceAmount", amount);
                command.Parameters.AddWithValue("@AdvanceDate", DateTime.Now);
                command.Parameters.AddWithValue("@Reason", reason ?? string.Empty);
                command.Parameters.AddWithValue("@Status", "Generated");
                command.Parameters.AddWithValue("@CreatedBy", createdBy);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                var advanceChequeId = Convert.ToInt32(await command.ExecuteScalarAsync());

                // Try to update with new columns if they exist
                try
                {
                    var updateQuery = @"
                        UPDATE AdvanceCheques 
                        SET FiscalYear = @FiscalYear,
                            AccountingPeriod = @AccountingPeriod,
                            GLAccountCode = @GLAccountCode,
                            CostCenter = @CostCenter,
                            GrowerNumber = @GrowerNumber,
                            SystemVersion = @SystemVersion
                        WHERE AdvanceChequeId = @AdvanceChequeId";

                    using var updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@FiscalYear", DateTime.Now.Year);
                    updateCommand.Parameters.AddWithValue("@AccountingPeriod", GetAccountingPeriod(DateTime.Now));
                    updateCommand.Parameters.AddWithValue("@GLAccountCode", "ADVANCE_PAYABLE");
                    updateCommand.Parameters.AddWithValue("@CostCenter", "GROWER_ADVANCES");
                    updateCommand.Parameters.AddWithValue("@GrowerNumber", await GetGrowerNumberAsync(growerId));
                    updateCommand.Parameters.AddWithValue("@SystemVersion", "2.0");
                    updateCommand.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);

                    await updateCommand.ExecuteNonQueryAsync();
                }
                catch (SqlException)
                {
                    // Columns don't exist, skip the update
                    Logger.Debug("Enhanced columns not found in AdvanceCheques table, skipping update");
                }

                return await GetAdvanceChequeByIdAsync(advanceChequeId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating advance cheque: {ex.Message}", ex);
                throw new DatabaseException($"Error creating advance cheque: {ex.Message}", ex);
            }
        }

        public async Task<List<AdvanceCheque>> GetOutstandingAdvancesAsync(int growerId)
        {
            try
            {
                Logger.Info($"GetOutstandingAdvancesAsync: Starting for grower {growerId}");
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.GrowerId = @GrowerId 
                    AND ac.Status IN ('Printed', 'Delivered', 'Active', 'PartiallyDeducted')
                    AND ac.DeletedAt IS NULL
                    AND ac.CurrentAdvanceAmount > 0
                    ORDER BY ac.AdvanceDate ASC";

                var advances = (await connection.QueryAsync<AdvanceCheque>(query, new { GrowerId = growerId })).ToList();
                Logger.Info($"GetOutstandingAdvancesAsync: Found {advances.Count} advances for grower {growerId}");
                
                if (advances.Any())
                {
                    foreach (var advance in advances)
                    {
                        Logger.Info($"GetOutstandingAdvancesAsync: Advance {advance.AdvanceChequeId} - Status: {advance.Status}, Amount: {advance.CurrentAdvanceAmount:C}");
                    }
                }
                
                return advances;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting outstanding advances for grower {growerId}: {ex.Message}", ex);
                return new List<AdvanceCheque>();
            }
        }

        public async Task<decimal> CalculateTotalOutstandingAdvancesAsync(int growerId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ISNULL(SUM(CurrentAdvanceAmount), 0)
                    FROM AdvanceCheques
                    WHERE GrowerId = @GrowerId 
                    AND Status IN ('Printed', 'Delivered', 'Active', 'PartiallyDeducted')
                    AND DeletedAt IS NULL
                    AND CurrentAdvanceAmount > 0";

                return await connection.QuerySingleAsync<decimal>(query, new { GrowerId = growerId });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating total outstanding advances: {ex.Message}", ex);
                return 0;
            }
        }

        public async Task<AdvanceCheque> GetAdvanceChequeByIdAsync(int advanceChequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.AdvanceChequeId = @AdvanceChequeId
                    AND ac.DeletedAt IS NULL";

                return await connection.QueryFirstOrDefaultAsync<AdvanceCheque>(query, new { AdvanceChequeId = advanceChequeId });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance cheque by ID: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<List<AdvanceCheque>> GetAllAdvanceChequesAsync(string status = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.DeletedAt IS NULL";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ac.Status = @Status";
                }

                query += " ORDER BY ac.AdvanceDate DESC";

                var parameters = new { Status = status };
                return (await connection.QueryAsync<AdvanceCheque>(query, parameters)).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all advance cheques: {ex.Message}", ex);
                return new List<AdvanceCheque>();
            }
        }

        public async Task<List<AdvanceCheque>> GetAdvanceChequesByGrowerAsync(int growerId, string status = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.GrowerId = @GrowerId
                    AND ac.DeletedAt IS NULL";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ac.Status = @Status";
                }

                query += " ORDER BY ac.AdvanceDate DESC";

                var parameters = new { GrowerId = growerId, Status = status };
                return (await connection.QueryAsync<AdvanceCheque>(query, parameters)).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance cheques by grower: {ex.Message}", ex);
                return new List<AdvanceCheque>();
            }
        }

        public async Task<List<AdvanceCheque>> GetAdvanceChequesByDateRangeAsync(DateTime startDate, DateTime endDate, string status = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.AdvanceDate >= @StartDate
                    AND ac.AdvanceDate <= @EndDate
                    AND ac.DeletedAt IS NULL";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ac.Status = @Status";
                }

                query += " ORDER BY ac.AdvanceDate DESC";

                var parameters = new { StartDate = startDate, EndDate = endDate, Status = status };
                return (await connection.QueryAsync<AdvanceCheque>(query, parameters)).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance cheques by date range: {ex.Message}", ex);
                return new List<AdvanceCheque>();
            }
        }

        public async Task<bool> UpdateAdvanceChequeAsync(AdvanceCheque advanceCheque)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET OriginalAdvanceAmount = @OriginalAdvanceAmount,
                        CurrentAdvanceAmount = @CurrentAdvanceAmount,
                        Reason = @Reason,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId";

                var parameters = new
                {
                    AdvanceChequeId = advanceCheque.AdvanceChequeId,
                    OriginalAdvanceAmount = advanceCheque.OriginalAdvanceAmount,
                    CurrentAdvanceAmount = advanceCheque.CurrentAdvanceAmount,
                    Reason = advanceCheque.Reason ?? string.Empty,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = advanceCheque.ModifiedBy ?? string.Empty
                };

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating advance cheque: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> HasOutstandingAdvancesAsync(int growerId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*)
                    FROM AdvanceCheques
                    WHERE GrowerId = @GrowerId 
                    AND Status IN ('Printed', 'Delivered', 'Active', 'PartiallyDeducted')
                    AND DeletedAt IS NULL
                    AND CurrentAdvanceAmount > 0";

                var count = await connection.QuerySingleAsync<int>(query, new { GrowerId = growerId });
                return count > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking outstanding advances: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Advance Deduction Management

        public async Task<DeductionResult> ApplyAdvanceDeductionsAsync(int growerId, int paymentBatchId, decimal paymentAmount, string createdBy)
        {
            var result = new DeductionResult();
            
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Get active advances for grower (oldest first)
                    var advances = await GetOutstandingAdvancesForGrowerAsync(growerId, connection, transaction);
                    
                    if (!advances.Any())
                    {
                        result.IsSuccessful = true;
                        result.Message = "No outstanding advances found";
                        result.RemainingPaymentAmount = paymentAmount;
                        transaction.Commit();
                        return result;
                    }

                    decimal remainingPayment = paymentAmount;
                    int deductionSequence = 1;

                    // 2. Process each advance in chronological order
                    foreach (var advance in advances)
                    {
                        if (remainingPayment <= 0) break;

                        decimal deductionAmount = Math.Min(remainingPayment, advance.CurrentAdvanceAmount);
                        
                        if (deductionAmount <= 0) continue;

                        // 3. Create deduction record
                        var deduction = new AdvanceDeduction
                        {
                            AdvanceChequeId = advance.AdvanceChequeId,
                            PaymentBatchId = paymentBatchId,
                            DeductionAmount = deductionAmount,
                            DeductionDate = DateTime.Now,
                            TransactionType = "Deduction",
                            Status = "Active",
                            IsVoided = false,
                            CreatedBy = createdBy,
                            CreatedAt = DateTime.Now,
                            FiscalYear = DateTime.Now.Year,
                            AccountingPeriod = GetAccountingPeriod(DateTime.Now),
                            GLAccountCode = "ADVANCE_DEDUCTION",
                            CostCenter = "GROWER_PAYMENTS",
                            GrowerId = growerId,
                            GrowerNumber = advance.GrowerNumber,
                            OriginalAdvanceAmount = advance.OriginalAdvanceAmount,
                            RemainingAdvanceAmount = advance.CurrentAdvanceAmount - deductionAmount,
                            BatchSequence = deductionSequence,
                            ProcessingOrder = deductionSequence,
                            SystemVersion = "2.0"
                        };

                        // 4. Insert deduction record
                        var deductionId = await InsertDeductionAsync(deduction, connection, transaction);
                        deduction.DeductionId = deductionId;

                        // 5. Update parent advance cheque
                        await UpdateAdvanceChequeAfterDeductionAsync(advance.AdvanceChequeId, deductionAmount, connection, transaction);

                        // 6. Track results
                        result.Deductions.Add(deduction);
                        result.TotalDeductedAmount += deductionAmount;
                        result.DeductionCount++;
                        remainingPayment -= deductionAmount;
                        deductionSequence++;

                        // 7. Check if advance is fully deducted
                        if (advance.CurrentAdvanceAmount <= deductionAmount)
                        {
                            await MarkAdvanceAsFullyDeductedAsync(advance.AdvanceChequeId, connection, transaction);
                        }
                    }

                    // 8. Set final results
                    result.IsSuccessful = true;
                    result.RemainingPaymentAmount = remainingPayment;
                    result.IsDeductionFullyApplied = remainingPayment == 0;
                    result.Message = $"Applied {result.DeductionCount} deductions totaling {result.TotalDeductedAmount:C}";

                    if (result.IsDeductionFullyApplied)
                    {
                        result.Message += " - All deduction amount was successfully applied to advance cheques";
                    }
                    else if (remainingPayment > 0)
                    {
                        result.Warnings.Add($"Deduction partially applied. Remaining balance of {remainingPayment:C} could not be allocated to advances (exceeds available advance amounts)");
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error applying advance deductions for grower {growerId}: {ex.Message}", ex);
                result.IsSuccessful = false;
                result.Errors.Add($"Failed to apply deductions: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Overload that accepts existing connection and transaction for atomicity
        /// </summary>
        public async Task<DeductionResult> ApplyAdvanceDeductionsAsync(
            int growerId, 
            int paymentBatchId, 
            decimal paymentAmount, 
            string createdBy,
            SqlConnection connection, 
            SqlTransaction transaction)
        {
            var result = new DeductionResult();
            
            try
            {
                // 1. Get active advances for grower (oldest first)
                var advances = await GetOutstandingAdvancesForGrowerAsync(growerId, connection, transaction);
                
                if (!advances.Any())
                {
                    result.IsSuccessful = true;
                    result.Message = "No outstanding advances found";
                    result.RemainingPaymentAmount = paymentAmount;
                    return result;
                }

                decimal remainingPayment = paymentAmount;
                int deductionSequence = 1;

                // 2. Process each advance in chronological order
                foreach (var advance in advances)
                {
                    if (remainingPayment <= 0) break;

                    decimal deductionAmount = Math.Min(remainingPayment, advance.CurrentAdvanceAmount);
                    
                    if (deductionAmount <= 0) continue;

                    // 3. Create deduction record
                    var deduction = new AdvanceDeduction
                    {
                        AdvanceChequeId = advance.AdvanceChequeId,
                        PaymentBatchId = paymentBatchId,
                        DeductionAmount = deductionAmount,
                        DeductionDate = DateTime.Now,
                        TransactionType = "Deduction",
                        Status = "Active",
                        IsVoided = false,
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.Now,
                        FiscalYear = DateTime.Now.Year,
                        AccountingPeriod = GetAccountingPeriod(DateTime.Now),
                        GLAccountCode = "ADVANCE_DEDUCTION",
                        CostCenter = "GROWER_PAYMENTS",
                        GrowerId = growerId,
                        GrowerNumber = advance.GrowerNumber,
                        OriginalAdvanceAmount = advance.OriginalAdvanceAmount,
                        RemainingAdvanceAmount = advance.CurrentAdvanceAmount - deductionAmount,
                        BatchSequence = deductionSequence,
                        ProcessingOrder = deductionSequence,
                        SystemVersion = "2.0"
                    };

                    // 4. Insert deduction record
                    var deductionId = await InsertDeductionAsync(deduction, connection, transaction);
                    deduction.DeductionId = deductionId;

                    // 5. Update parent advance cheque
                    await UpdateAdvanceChequeAfterDeductionAsync(advance.AdvanceChequeId, deductionAmount, connection, transaction);

                    // 6. Track results
                    result.Deductions.Add(deduction);
                    result.TotalDeductedAmount += deductionAmount;
                    result.DeductionCount++;
                    remainingPayment -= deductionAmount;
                    deductionSequence++;

                    // 7. Check if advance is fully deducted
                    if (advance.CurrentAdvanceAmount <= deductionAmount)
                    {
                        await MarkAdvanceAsFullyDeductedAsync(advance.AdvanceChequeId, connection, transaction);
                    }
                }

                // 8. Set final results
                result.IsSuccessful = true;
                result.RemainingPaymentAmount = remainingPayment;
                result.IsDeductionFullyApplied = remainingPayment == 0;
                result.Message = $"Applied {result.DeductionCount} deductions totaling {result.TotalDeductedAmount:C}";

                if (result.IsDeductionFullyApplied)
                {
                    result.Message += " - All deduction amount was successfully applied to advance cheques";
                }
                else if (remainingPayment > 0)
                {
                    result.Warnings.Add($"Deduction partially applied. Remaining balance of {remainingPayment:C} could not be allocated to advances (exceeds available advance amounts)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error applying advance deductions for grower {growerId}: {ex.Message}", ex);
                result.IsSuccessful = false;
                result.Errors.Add($"Failed to apply deductions: {ex.Message}");
                throw; // Re-throw to trigger rollback in calling transaction
            }

            return result;
        }

        public async Task<DeductionResult> ApplyManualDeductionAsync(int advanceChequeId, int? chequeId, decimal deductionAmount, int paymentBatchId, string createdBy)
        {
            var result = new DeductionResult();
            
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Validate advance exists and has sufficient balance
                    var advance = await GetAdvanceChequeByIdAsync(advanceChequeId);
                    if (advance == null)
                    {
                        result.Errors.Add("Advance cheque not found");
                        return result;
                    }

                    if (advance.CurrentAdvanceAmount < deductionAmount)
                    {
                        result.Errors.Add($"Insufficient advance balance. Available: {advance.CurrentAdvanceAmount:C}, Requested: {deductionAmount:C}");
                        return result;
                    }

                    // 2. Create deduction record
                    var deduction = new AdvanceDeduction
                    {
                        AdvanceChequeId = advanceChequeId,
                        ChequeId = chequeId,
                        PaymentBatchId = paymentBatchId,
                        DeductionAmount = deductionAmount,
                        DeductionDate = DateTime.Now,
                        TransactionType = "ManualDeduction",
                        Status = "Active",
                        IsVoided = false,
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.Now,
                        FiscalYear = DateTime.Now.Year,
                        AccountingPeriod = GetAccountingPeriod(DateTime.Now),
                        GLAccountCode = "ADVANCE_DEDUCTION",
                        CostCenter = "GROWER_PAYMENTS",
                        GrowerId = advance.GrowerId,
                        GrowerNumber = advance.GrowerNumber,
                        OriginalAdvanceAmount = advance.OriginalAdvanceAmount,
                        RemainingAdvanceAmount = advance.CurrentAdvanceAmount - deductionAmount,
                        BatchSequence = 1,
                        ProcessingOrder = 1,
                        SystemVersion = "2.0"
                    };

                    // 3. Insert deduction record
                    var deductionId = await InsertDeductionAsync(deduction, connection, transaction);
                    deduction.DeductionId = deductionId;

                    // 4. Update parent advance cheque
                    await UpdateAdvanceChequeAfterDeductionAsync(advanceChequeId, deductionAmount, connection, transaction);

                    // 5. Set results
                    result.IsSuccessful = true;
                    result.Deductions.Add(deduction);
                    result.TotalDeductedAmount = deductionAmount;
                    result.DeductionCount = 1;
                    result.Message = $"Applied manual deduction of {deductionAmount:C}";

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error applying manual deduction for advance {advanceChequeId}: {ex.Message}", ex);
                result.IsSuccessful = false;
                result.Errors.Add($"Failed to apply manual deduction: {ex.Message}");
            }

            return result;
        }

        public async Task<List<AdvanceDeduction>> CalculateSuggestedDeductionsAsync(int growerId, decimal paymentAmount)
        {
            var suggestions = new List<AdvanceDeduction>();
            
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Get outstanding advances
                var advances = await GetOutstandingAdvancesForGrowerAsync(growerId, connection, null);
                
                decimal remainingPayment = paymentAmount;
                int sequence = 1;

                foreach (var advance in advances)
                {
                    if (remainingPayment <= 0) break;

                    decimal suggestedAmount = Math.Min(remainingPayment, advance.CurrentAdvanceAmount);
                    
                    if (suggestedAmount <= 0) continue;

                    var suggestion = new AdvanceDeduction
                    {
                        AdvanceChequeId = advance.AdvanceChequeId,
                        DeductionAmount = suggestedAmount,
                        DeductionDate = DateTime.Now,
                        TransactionType = "SuggestedDeduction",
                        Status = "Suggested",
                        GrowerId = growerId,
                        GrowerNumber = advance.GrowerNumber,
                        OriginalAdvanceAmount = advance.OriginalAdvanceAmount,
                        RemainingAdvanceAmount = advance.CurrentAdvanceAmount - suggestedAmount,
                        BatchSequence = sequence,
                        ProcessingOrder = sequence
                    };

                    suggestions.Add(suggestion);
                    remainingPayment -= suggestedAmount;
                    sequence++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating suggested deductions for grower {growerId}: {ex.Message}", ex);
            }

            return suggestions;
        }

        public async Task<List<AdvanceDeduction>> GetDeductionHistoryAsync(int advanceChequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                return (await connection.QueryAsync<AdvanceDeduction>(@"
                    SELECT ad.*, pb.BatchNumber
                    FROM AdvanceDeductions ad
                    INNER JOIN PaymentBatches pb ON ad.PaymentBatchId = pb.PaymentBatchId
                    WHERE ad.AdvanceChequeId = @AdvanceChequeId
                    ORDER BY ad.DeductionDate DESC",
                    new { AdvanceChequeId = advanceChequeId })).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting deduction history for advance {advanceChequeId}: {ex.Message}", ex);
                return new List<AdvanceDeduction>();
            }
        }

        public async Task<List<AdvanceDeduction>> GetDeductionsByBatchAsync(int paymentBatchId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                return (await connection.QueryAsync<AdvanceDeduction>(@"
                    SELECT * FROM AdvanceDeductions 
                    WHERE PaymentBatchId = @PaymentBatchId 
                    ORDER BY DeductionDate ASC",
                    new { PaymentBatchId = paymentBatchId })).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting deductions for batch {paymentBatchId}: {ex.Message}", ex);
                return new List<AdvanceDeduction>();
            }
        }

        public async Task<decimal> GetTotalDeductionsAsync(int growerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT ISNULL(SUM(DeductionAmount), 0) 
                    FROM AdvanceDeductions 
                    WHERE GrowerId = @GrowerId AND Status = 'Active' AND DeletedAt IS NULL";
                
                if (startDate.HasValue)
                {
                    sql += " AND DeductionDate >= @StartDate";
                }
                
                if (endDate.HasValue)
                {
                    sql += " AND DeductionDate <= @EndDate";
                }
                
                var parameters = new { GrowerId = growerId, StartDate = startDate, EndDate = endDate };

                return await connection.QuerySingleAsync<decimal>(sql, parameters);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting total deductions for grower {growerId}: {ex.Message}", ex);
                return 0;
            }
        }

        public async Task<bool> CreateDeductionAsync(AdvanceDeduction deduction)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    await InsertDeductionAsync(deduction, connection, transaction);
                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating deduction: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateDeductionAsync(AdvanceDeduction deduction)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    await connection.ExecuteAsync(@"
                        UPDATE AdvanceDeductions 
                        SET DeductionAmount = @DeductionAmount,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE DeductionId = @DeductionId",
                        new
                        {
                            DeductionId = deduction.DeductionId,
                            DeductionAmount = deduction.DeductionAmount,
                            ModifiedAt = DateTime.Now,
                            ModifiedBy = "SYSTEM_UPDATE"
                        }, transaction);

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating deduction {deduction.DeductionId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteDeductionAsync(int deductionId, string deletedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    await connection.ExecuteAsync(@"
                        UPDATE AdvanceDeductions 
                        SET DeletedAt = @DeletedAt,
                            DeletedBy = @DeletedBy,
                            DeletedReason = 'Soft Delete',
                            Status = 'Deleted'
                        WHERE DeductionId = @DeductionId",
                        new
                        {
                            DeductionId = deductionId,
                            DeletedAt = DateTime.Now,
                            DeletedBy = deletedBy
                        }, transaction);

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting deduction {deductionId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<List<AdvanceDeduction>> GetDeductionHistoryByChequeIdAsync(int chequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                return (await connection.QueryAsync<AdvanceDeduction>(@"
                    SELECT * FROM AdvanceDeductions 
                    WHERE ChequeId = @ChequeId 
                    ORDER BY DeductionDate ASC",
                    new { ChequeId = chequeId })).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting deduction history for cheque {chequeId}: {ex.Message}", ex);
                return new List<AdvanceDeduction>();
            }
        }

        public async Task<List<AdvanceDeduction>> GetDeductionsByChequeIdAsync(int chequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                return (await connection.QueryAsync<AdvanceDeduction>(@"
                    SELECT * FROM AdvanceDeductions 
                    WHERE ChequeId = @ChequeId AND Status = 'Active'",
                    new { ChequeId = chequeId })).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting deductions for cheque {chequeId}: {ex.Message}", ex);
                return new List<AdvanceDeduction>();
            }
        }

        public async Task<bool> ValidateDeductionAsync(int advanceChequeId, decimal amount)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                var advance = await connection.QueryFirstOrDefaultAsync<AdvanceCheque>(@"
                    SELECT * FROM AdvanceCheques 
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND Status IN ('Active', 'PartiallyDeducted') 
                    AND DeletedAt IS NULL",
                    new { AdvanceChequeId = advanceChequeId });

                return advance != null && advance.CurrentAdvanceAmount >= amount;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating deduction for advance {advanceChequeId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<AdvanceDeductionSummary> GetAdvanceDeductionSummaryAsync(int growerId)
        {
            var summary = new AdvanceDeductionSummary { GrowerId = growerId };
            
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Get grower info
                var grower = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT GrowerNumber, GrowerName FROM Growers WHERE GrowerId = @GrowerId",
                    new { GrowerId = growerId });

                if (grower != null)
                {
                    summary.GrowerNumber = grower.GrowerNumber;
                    summary.GrowerName = grower.GrowerName;
                }

                // Get advance summary
                var advanceSummary = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT 
                        COUNT(*) as ActiveAdvanceCount,
                        SUM(OriginalAdvanceAmount) as TotalOriginalAdvances,
                        SUM(CurrentAdvanceAmount) as TotalOutstandingAdvances,
                        SUM(TotalDeductedAmount) as TotalDeductedAmount,
                        MAX(AdvanceDate) as LastAdvanceDate
                    FROM AdvanceCheques 
                    WHERE GrowerId = @GrowerId AND Status IN ('Active', 'PartiallyDeducted') AND DeletedAt IS NULL",
                    new { GrowerId = growerId });

                if (advanceSummary != null)
                {
                    summary.ActiveAdvanceCount = advanceSummary.ActiveAdvanceCount;
                    summary.TotalOriginalAdvances = advanceSummary.TotalOriginalAdvances ?? 0;
                    summary.TotalOutstandingAdvances = advanceSummary.TotalOutstandingAdvances ?? 0;
                    summary.TotalDeductedAmount = advanceSummary.TotalDeductedAmount ?? 0;
                    summary.LastAdvanceDate = advanceSummary.LastAdvanceDate;
                }

                // Get deduction summary
                var deductionSummary = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT 
                        COUNT(*) as TotalDeductionCount,
                        SUM(CASE WHEN Status = 'Voided' THEN 1 ELSE 0 END) as VoidedDeductionCount,
                        SUM(CASE WHEN IsVoided = 1 THEN DeductionAmount ELSE 0 END) as TotalVoidedAmount,
                        MAX(DeductionDate) as LastDeductionDate
                    FROM AdvanceDeductions 
                    WHERE GrowerId = @GrowerId AND DeletedAt IS NULL",
                    new { GrowerId = growerId });

                if (deductionSummary != null)
                {
                    summary.TotalDeductionCount = deductionSummary.TotalDeductionCount;
                    summary.VoidedDeductionCount = deductionSummary.VoidedDeductionCount;
                    summary.TotalVoidedAmount = deductionSummary.TotalVoidedAmount ?? 0;
                    summary.LastDeductionDate = deductionSummary.LastDeductionDate;
                }

                // Get active advances
                summary.ActiveAdvances = (await GetOutstandingAdvancesAsync(growerId)).ToList();

                // Get recent deductions
                summary.RecentDeductions = (await connection.QueryAsync<AdvanceDeduction>(@"
                    SELECT TOP 10 * FROM AdvanceDeductions 
                    WHERE GrowerId = @GrowerId AND DeletedAt IS NULL 
                    ORDER BY DeductionDate DESC",
                    new { GrowerId = growerId })).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance deduction summary for grower {growerId}: {ex.Message}", ex);
            }

            return summary;
        }

        #endregion

        #region Voiding and Reversal

        public async Task<bool> VoidDeductionAsync(int deductionId, string reason, string voidedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Get deduction record
                    var deduction = await GetDeductionByIdAsync(deductionId, connection, transaction);
                    if (deduction == null)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // 2. Update deduction status
                    await connection.ExecuteAsync(@"
                        UPDATE AdvanceDeductions 
                        SET Status = 'Voided', 
                            IsVoided = 1, 
                            VoidedAt = @VoidedAt, 
                            VoidedBy = @VoidedBy, 
                            VoidReason = @VoidReason,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE DeductionId = @DeductionId",
                        new
                        {
                            DeductionId = deductionId,
                            VoidedAt = DateTime.Now,
                            VoidedBy = voidedBy,
                            VoidReason = reason,
                            ModifiedAt = DateTime.Now,
                            ModifiedBy = voidedBy
                        }, transaction);

                    // 3. Restore parent advance amounts
                    await connection.ExecuteAsync(@"
                        UPDATE AdvanceCheques 
                        SET CurrentAdvanceAmount = CurrentAdvanceAmount + @DeductionAmount,
                            TotalDeductedAmount = TotalDeductedAmount - @DeductionAmount,
                            DeductionCount = DeductionCount - 1,
                            IsFullyDeducted = CASE 
                                WHEN (CurrentAdvanceAmount + @DeductionAmount) > 0 THEN 0 
                                ELSE 1 
                            END,
                            Status = CASE 
                                WHEN (CurrentAdvanceAmount + @DeductionAmount) > 0 THEN 'Active' 
                                ELSE Status 
                            END,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE AdvanceChequeId = @AdvanceChequeId",
                        new
                        {
                            DeductionAmount = deduction.DeductionAmount,
                            AdvanceChequeId = deduction.AdvanceChequeId,
                            ModifiedAt = DateTime.Now,
                            ModifiedBy = voidedBy
                        }, transaction);

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error voiding deduction {deductionId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> ReverseAdvanceDeductionsAsync(int advanceChequeId, string reason, string reversedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Get all active deductions for this advance
                    var deductions = await connection.QueryAsync<AdvanceDeduction>(@"
                        SELECT * FROM AdvanceDeductions 
                        WHERE AdvanceChequeId = @AdvanceChequeId AND Status = 'Active'",
                        new { AdvanceChequeId = advanceChequeId }, transaction);

                    foreach (var deduction in deductions)
                    {
                        // Void each deduction
                        await VoidDeductionAsync(deduction.DeductionId, reason, reversedBy);
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reversing advance deductions for advance {advanceChequeId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> ReverseAdvanceDeductionAsync(int advanceChequeId, string reason, string reversedBy)
        {
            return await ReverseAdvanceDeductionsAsync(advanceChequeId, reason, reversedBy);
        }

        public async Task<bool> RestoreAdvanceAmountsAsync(List<AdvanceDeduction> deductions)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    foreach (var deduction in deductions)
                    {
                        await connection.ExecuteAsync(@"
                            UPDATE AdvanceCheques 
                            SET CurrentAdvanceAmount = CurrentAdvanceAmount + @DeductionAmount,
                                TotalDeductedAmount = TotalDeductedAmount - @DeductionAmount,
                                DeductionCount = DeductionCount - 1,
                                ModifiedAt = @ModifiedAt,
                                ModifiedBy = @ModifiedBy
                            WHERE AdvanceChequeId = @AdvanceChequeId",
                            new
                            {
                                DeductionAmount = deduction.DeductionAmount,
                                AdvanceChequeId = deduction.AdvanceChequeId,
                                ModifiedAt = DateTime.Now,
                                ModifiedBy = "SYSTEM_RESTORE"
                            }, transaction);
                    }

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error restoring advance amounts: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> CancelAdvanceChequeAsync(int advanceChequeId, string reason, string cancelledBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Voided',
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId
                    AND Status = 'Generated'";

                var parameters = new
                {
                    AdvanceChequeId = advanceChequeId,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = cancelledBy
                };

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error cancelling advance cheque: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> VoidAdvanceChequeAsync(int advanceChequeId, string voidedBy, string voidedReason)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Voided', 
                        VoidedDate = @VoidedDate, 
                        VoidedBy = @VoidedBy,
                        VoidedReason = @VoidedReason,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND Status IN ('Generated', 'Printed')";

                var parameters = new
                {
                    AdvanceChequeId = advanceChequeId,
                    VoidedDate = DateTime.Now,
                    VoidedBy = voidedBy,
                    VoidedReason = voidedReason,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = voidedBy
                };

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error voiding advance cheque: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Workflow Management

        public async Task<bool> PrintAdvanceChequeAsync(int advanceChequeId, string printedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Printed', 
                        PrintedDate = @PrintedDate, 
                        PrintedBy = @PrintedBy,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND Status = 'Generated'";

                var parameters = new
                {
                    AdvanceChequeId = advanceChequeId,
                    PrintedDate = DateTime.Now,
                    PrintedBy = printedBy,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = printedBy
                };

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error printing advance cheque: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> DeliverAdvanceChequeAsync(int advanceChequeId, string deliveredBy, string deliveryMethod)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Delivered', 
                        DeliveredAt = @DeliveredAt, 
                        DeliveredBy = @DeliveredBy,
                        DeliveryMethod = @DeliveryMethod,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND Status = 'Printed'";

                var parameters = new
                {
                    AdvanceChequeId = advanceChequeId,
                    DeliveredAt = DateTime.Now,
                    DeliveredBy = deliveredBy,
                    DeliveryMethod = deliveryMethod,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = deliveredBy
                };

                var rowsAffected = await connection.ExecuteAsync(query, parameters);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error delivering advance cheque: {ex.Message}", ex);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<List<AdvanceCheque>> GetOutstandingAdvancesForGrowerAsync(int growerId, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction)
        {
            var query = @"
                SELECT 
                    ac.*, 
                    g.GrowerNumber
                FROM AdvanceCheques ac
                INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                WHERE ac.GrowerId = @GrowerId 
                AND ac.Status IN ('Printed', 'Delivered', 'Active', 'PartiallyDeducted') 
                AND ac.DeletedAt IS NULL 
                AND ac.CurrentAdvanceAmount > 0
                ORDER BY ac.AdvanceDate ASC";

            Logger.Info($"GetOutstandingAdvancesForGrowerAsync: Executing query for grower {growerId}");
            var advances = (await connection.QueryAsync<AdvanceCheque>(query, new { GrowerId = growerId }, transaction)).ToList();
            Logger.Info($"GetOutstandingAdvancesForGrowerAsync: Query returned {advances.Count} advances for grower {growerId}");
            
            if (advances.Any())
            {
                foreach (var advance in advances)
                {
                    Logger.Info($"GetOutstandingAdvancesForGrowerAsync: Advance {advance.AdvanceChequeId} - Status: {advance.Status}, Amount: {advance.CurrentAdvanceAmount:C}");
                }
            }
            
            return advances;
        }

        private async Task<AdvanceDeduction> GetDeductionByIdAsync(int deductionId, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction)
        {
            return await connection.QueryFirstOrDefaultAsync<AdvanceDeduction>(@"
                SELECT * FROM AdvanceDeductions WHERE DeductionId = @DeductionId",
                new { DeductionId = deductionId }, transaction);
        }

        private async Task<int> InsertDeductionAsync(AdvanceDeduction deduction, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction)
        {
            var sql = @"
                INSERT INTO AdvanceDeductions (
                    AdvanceChequeId, ChequeId, PaymentBatchId, DeductionAmount, DeductionDate,
                    TransactionType, Status, IsVoided, CreatedBy, CreatedAt,
                    FiscalYear, AccountingPeriod, GLAccountCode, CostCenter,
                    GrowerId, GrowerNumber, OriginalAdvanceAmount, RemainingAdvanceAmount,
                    BatchSequence, ProcessingOrder, SystemVersion
                ) VALUES (
                    @AdvanceChequeId, @ChequeId, @PaymentBatchId, @DeductionAmount, @DeductionDate,
                    @TransactionType, @Status, @IsVoided, @CreatedBy, @CreatedAt,
                    @FiscalYear, @AccountingPeriod, @GLAccountCode, @CostCenter,
                    @GrowerId, @GrowerNumber, @OriginalAdvanceAmount, @RemainingAdvanceAmount,
                    @BatchSequence, @ProcessingOrder, @SystemVersion
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.QuerySingleAsync<int>(sql, deduction, transaction);
        }

        private async Task UpdateAdvanceChequeAfterDeductionAsync(int advanceChequeId, decimal deductionAmount, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction)
        {
            // First, get the current state to determine if it should be PartiallyDeducted
            var currentAdvance = await connection.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT CurrentAdvanceAmount, OriginalAdvanceAmount FROM AdvanceCheques WHERE AdvanceChequeId = @AdvanceChequeId",
                new { AdvanceChequeId = advanceChequeId }, transaction);

            // Calculate the remaining amount after this deduction
            decimal remainingAmount = (decimal)currentAdvance.CurrentAdvanceAmount - deductionAmount;
            decimal originalAmount = (decimal)currentAdvance.OriginalAdvanceAmount;

            await connection.ExecuteAsync(@"
                UPDATE AdvanceCheques 
                SET CurrentAdvanceAmount = CurrentAdvanceAmount - @DeductionAmount,
                    TotalDeductedAmount = TotalDeductedAmount + @DeductionAmount,
                    DeductionCount = DeductionCount + 1,
                    LastDeductionDate = @LastDeductionDate,
                    Status = CASE 
                        WHEN (CurrentAdvanceAmount - @DeductionAmount) > 0 AND (CurrentAdvanceAmount - @DeductionAmount) < OriginalAdvanceAmount THEN 'PartiallyDeducted'
                        ELSE Status
                    END,
                    ModifiedAt = @ModifiedAt,
                    ModifiedBy = @ModifiedBy
                WHERE AdvanceChequeId = @AdvanceChequeId",
                new
                {
                    AdvanceChequeId = advanceChequeId,
                    DeductionAmount = deductionAmount,
                    LastDeductionDate = DateTime.Now,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = "SYSTEM_DEDUCTION"
                }, transaction);
        }

        private async Task MarkAdvanceAsFullyDeductedAsync(int advanceChequeId, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction)
        {
            await connection.ExecuteAsync(@"
                UPDATE AdvanceCheques 
                SET IsFullyDeducted = 1,
                    Status = 'FullyDeducted',
                    ModifiedAt = @ModifiedAt,
                    ModifiedBy = @ModifiedBy
                WHERE AdvanceChequeId = @AdvanceChequeId",
                new
                {
                    AdvanceChequeId = advanceChequeId,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = "SYSTEM_DEDUCTION"
                }, transaction);
        }

        private string GetAccountingPeriod(DateTime date)
        {
            return $"Q{Math.Ceiling(date.Month / 3.0)}-{date.Year}";
        }

        private async Task<string> GetGrowerNumberAsync(int growerId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                return await connection.QuerySingleAsync<string>(@"
                    SELECT GrowerNumber FROM Growers WHERE GrowerId = @GrowerId",
                    new { GrowerId = growerId });
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        #endregion
    }
}
