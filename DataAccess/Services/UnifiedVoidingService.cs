using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.DataAccess.Exceptions;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for unified voiding of all payment types
    /// </summary>
    public class UnifiedVoidingService : BaseDatabaseService, IUnifiedVoidingService
    {
        private readonly IChequeService _chequeService;
        private readonly IAdvanceChequeService _advanceChequeService;
        private readonly IAdvanceDeductionService _advanceDeductionService;
        private readonly ICrossBatchPaymentService _crossBatchPaymentService;

        public UnifiedVoidingService(
            IChequeService chequeService,
            IAdvanceChequeService advanceChequeService,
            IAdvanceDeductionService advanceDeductionService,
            ICrossBatchPaymentService crossBatchPaymentService)
        {
            _chequeService = chequeService;
            _advanceChequeService = advanceChequeService;
            _advanceDeductionService = advanceDeductionService;
            _crossBatchPaymentService = crossBatchPaymentService;
        }

        public async Task<VoidingResult> VoidPaymentAsync(PaymentVoidRequest request)
        {
            try
            {
                // Null reference checks
                if (request == null)
                {
                    return new VoidingResult(false, "Void request is null");
                }

                if (string.IsNullOrWhiteSpace(request.EntityType))
                {
                    return new VoidingResult(false, "Entity type is required");
                }

                if (request.EntityId <= 0)
                {
                    return new VoidingResult(false, "Valid entity ID is required");
                }

                if (string.IsNullOrWhiteSpace(request.VoidedBy))
                {
                    request.VoidedBy = Environment.UserName ?? "SYSTEM";
                }

                var result = new VoidingResult();

                // Auto-detect cheque type if not specified or if it's a generic "cheque" type
                if (request.EntityType.ToLower() == "cheque" || request.EntityType.ToLower() == "regular")
                {
                    // Check if this ID belongs to an advance cheque first
                    if (await IsAdvanceChequeIdAsync(request.EntityId))
                    {
                        result = await VoidAdvanceChequeAsync(request.EntityId, request.Reason, request.VoidedBy);
                    }
                    else
                    {
                        // Use regular voiding logic for regular cheques
                        result = await VoidRegularBatchPaymentAsync(request.EntityId, request.Reason, request.VoidedBy);
                    }
                }
                else
                {
                    switch (request.EntityType.ToLower())
                    {
                        case "advance":
                        case "advancecheque":
                            result = await VoidAdvanceChequeAsync(request.EntityId, request.Reason, request.VoidedBy);
                            break;
                        // Consolidated cheques now use regular voiding logic above
                        default:
                            result = new VoidingResult(false, $"Unknown entity type: {request.EntityType}");
                            break;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                return new VoidingResult(false, $"Error voiding payment: {ex.Message}");
            }
        }

        public async Task<VoidingResult> VoidRegularBatchPaymentAsync(int chequeId, string reason, string voidedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    var result = new VoidingResult();

                    // Get the cheque details
                    var cheque = await GetChequeByIdAsync(chequeId, connection, transaction);
                    if (cheque == null)
                    {
                        result.AddError("Cheque not found");
                        await SafeRollbackAsync(transaction);
                        return result;
                    }

                    if (!cheque.CanBeVoided)
                    {
                        result.AddError("Cheque cannot be voided in its current status");
                        await SafeRollbackAsync(transaction);
                        return result;
                    }

                    // Check for advance deductions that need to be reversed
                    var advanceDeductions = await GetAdvanceDeductionsByChequeIdAsync(chequeId, connection, transaction);
                    if (advanceDeductions.Any())
                    {
                        result.AddWarning($"Cheque has {advanceDeductions.Count} advance deductions that will be reversed.");
                    }

                    // 1. Reverse advance deductions if any exist
                    if (advanceDeductions.Any())
                    {
                        await ReverseAdvanceDeductionsAsync(chequeId, reason, voidedBy, connection, transaction);
                        result.DeductionsReversed = true;
                    }

                    // 2. Update payment distribution items
                    await UpdatePaymentDistributionItemsAsync(chequeId, "Voided", voidedBy, connection, transaction);

                    // 3. Void the cheque
                    await VoidChequeAsync(chequeId, reason, voidedBy, connection, transaction);

                    // 3.5. Update PaymentDistributionReceipts for the same grower and distribution
                    if (cheque.PaymentDistributionId.HasValue && cheque.GrowerId > 0)
                    {
                        await UpdatePaymentDistributionReceiptsAsync(cheque.PaymentDistributionId.Value, cheque.GrowerId, "Voided", voidedBy, connection, transaction);
                    }

                    // 4. Clean up orphaned payment distributions
                    //if (cheque.PaymentBatchId.HasValue)
                    //{
                    //    await CleanupOrphanedDistributionsAsync(cheque.PaymentBatchId.Value, connection, transaction);
                    //}

                    // 5. Revert batch status if needed for ALL affected batches (data-driven approach)
                    if (cheque.PaymentDistributionId.HasValue && cheque.GrowerId > 0)
                    {
                        // Get all batch IDs that are actually affected by this payment distribution
                        var affectedBatchIds = await GetAffectedBatchIdsAsync(
                            cheque.PaymentDistributionId.Value, 
                            cheque.GrowerId, 
                            connection, 
                            transaction
                        );
                        
                        // Also include the primary PaymentBatchId if it's not already in the list
                        if (cheque.PaymentBatchId.HasValue && !affectedBatchIds.Contains(cheque.PaymentBatchId.Value))
                        {
                            affectedBatchIds.Add(cheque.PaymentBatchId.Value);
                        }
                        
                        // Revert status for all affected batches
                        foreach (var batchId in affectedBatchIds)
                        {
                            await RevertBatchStatusIfNeededAsync(batchId, voidedBy, connection, transaction);
                        }
                        
                        Logger.Info($"Reverted status for {affectedBatchIds.Count} affected batches: [{string.Join(", ", affectedBatchIds)}]");
                    }
                    else if (cheque.PaymentBatchId.HasValue)
                    {
                        // Fallback: if no PaymentDistributionId, just revert the primary batch
                        await RevertBatchStatusIfNeededAsync(cheque.PaymentBatchId.Value, voidedBy, connection, transaction);
                    }

                    // Set result properties before commit
                    result.Success = true;
                    result.Message = "Regular batch payment voided successfully";
                    result.EntityType = "Regular";
                    result.EntityId = chequeId;
                    result.AmountReversed = cheque.ChequeAmount;
                    result.VoidedBy = voidedBy;
                    result.VoidedAt = DateTime.Now;

                    // Commit the transaction
                    await SafeCommitAsync(transaction);

                    return result;
                }
                catch
                {
                    // Only rollback if transaction is still active
                    await SafeRollbackAsync(transaction);
                    throw;
                }
            }
            catch (Exception ex)
            {
                return new VoidingResult(false, $"Error voiding regular batch payment: {ex.Message}");
            }
        }

        public async Task<VoidingResult> VoidAdvanceChequeAsync(int advanceChequeId, string reason, string voidedBy)
        {
            try
            {
                var result = new VoidingResult();

                // Get the advance cheque details
                var advanceCheque = await _advanceChequeService.GetAdvanceChequeByIdAsync(advanceChequeId);
                if (advanceCheque == null)
                {
                    result.AddError("Advance cheque not found");
                    return result;
                }

                if (!advanceCheque.CanBeVoided)
                {
                    result.AddError("Advance cheque cannot be voided in its current status");
                    return result;
                }

                // Check if advance has been deducted
                var deductions = await _advanceChequeService.GetDeductionHistoryAsync(advanceChequeId);
                if (deductions.Any())
                {
                    result.AddWarning("Advance has been deducted from payments. Deductions will be reversed.");
                }

                // Cancel the advance cheque
                var success = await _advanceChequeService.CancelAdvanceChequeAsync(advanceChequeId, reason, voidedBy);
                if (success)
                {
                    result.Success = true;
                    result.Message = "Advance cheque voided successfully";
                    result.EntityType = "Advance";
                    result.EntityId = advanceChequeId;
                    result.AmountReversed = advanceCheque.AdvanceAmount;
                    result.VoidedBy = voidedBy;
                    result.VoidedAt = DateTime.Now;
                    result.DeductionsReversed = deductions.Any();
                }
                else
                {
                    result.AddError("Failed to void the advance cheque");
                }

                return result;
            }
            catch (Exception ex)
            {
                return new VoidingResult(false, $"Error voiding advance cheque: {ex.Message}");
            }
        }

        public async Task<VoidingResult> VoidConsolidatedPaymentAsync(int chequeId, string reason, string voidedBy)
        {
            try
            {
                var result = new VoidingResult();

                // Get the cheque details
                var cheque = await _chequeService.GetChequeByIdAsync(chequeId);
                if (cheque == null)
                {
                    result.AddError("Consolidated cheque not found");
                    return result;
                }

                if (!cheque.IsConsolidated)
                {
                    result.AddError("Cheque is not a consolidated payment");
                    return result;
                }

                if (!cheque.CanBeVoided)
                {
                    result.AddError("Consolidated cheque cannot be voided in its current status");
                    return result;
                }

                // Get batch breakdown
                var batchBreakdowns = await _crossBatchPaymentService.GetBatchBreakdownAsync(chequeId);
                if (batchBreakdowns.Any())
                {
                    result.AddWarning($"Consolidated payment affects {batchBreakdowns.Count} batches. Batch statuses will be restored.");
                }

                // Use the same voiding logic as regular batch payments for PaymentDistributionItems
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // 1. Update payment distribution items using the new logic
                    await UpdatePaymentDistributionItemsAsync(chequeId, "Voided", voidedBy, connection, transaction);

                    // 2. Void the cheque
                    await VoidChequeAsync(chequeId, reason, voidedBy, connection, transaction);

                    // 3. Revert the consolidation (this handles batch statuses and other cleanup)
                    var success = await _crossBatchPaymentService.RevertConsolidationAsync(chequeId, voidedBy);
                    if (success)
                    {
                        result.Success = true;
                        result.Message = "Consolidated payment voided successfully";
                        result.EntityType = "Consolidated";
                        result.EntityId = chequeId;
                        result.AmountReversed = cheque.ChequeAmount;
                        result.VoidedBy = voidedBy;
                        result.VoidedAt = DateTime.Now;
                        result.BatchStatusRestored = true;

                        // Commit the transaction
                        await SafeCommitAsync(transaction);
                    }
                    else
                    {
                        result.AddError("Failed to void the consolidated payment");
                        await SafeRollbackAsync(transaction);
                    }
                }
                catch
                {
                    await SafeRollbackAsync(transaction);
                    throw;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new VoidingResult(false, $"Error voiding consolidated payment: {ex.Message}");
            }
        }

        public async Task<List<PaymentAuditLog>> GetVoidingHistoryAsync(string entityType, int entityId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT *
                    FROM PaymentAuditLog
                    WHERE EntityType = @EntityType
                    AND EntityId = @EntityId
                    AND Action = 'Voided'
                    ORDER BY ChangedAt DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EntityType", entityType);
                command.Parameters.AddWithValue("@EntityId", entityId);

                var auditLogs = new List<PaymentAuditLog>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    auditLogs.Add(MapPaymentAuditLogFromReader(reader));
                }

                return auditLogs;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting voiding history: {ex.Message}", ex);
            }
        }

        public async Task<bool> CanVoidPaymentAsync(string entityType, int entityId)
        {
            try
            {
                switch (entityType.ToLower())
                {
                    case "regular":
                    case "cheque":
                        var cheque = await _chequeService.GetChequeByIdAsync(entityId);
                        return cheque?.CanBeVoided ?? false;
                    
                    case "advance":
                    case "advancecheque":
                        var advanceCheque = await _advanceChequeService.GetAdvanceChequeByIdAsync(entityId);
                        return advanceCheque?.CanBeVoided ?? false;
                    
                    case "consolidated":
                    case "consolidatedcheque":
                        var consolidatedCheque = await _chequeService.GetChequeByIdAsync(entityId);
                        return consolidatedCheque?.CanBeVoided ?? false;
                    
                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error checking if payment can be voided: {ex.Message}", ex);
            }
        }

        public async Task<VoidingStatistics> GetVoidingStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        EntityType,
                        COUNT(*) as VoidCount,
                        SUM(CASE WHEN EntityType = 'Regular' THEN 1 ELSE 0 END) as RegularVoids,
                        SUM(CASE WHEN EntityType = 'Advance' THEN 1 ELSE 0 END) as AdvanceVoids,
                        SUM(CASE WHEN EntityType = 'Consolidated' THEN 1 ELSE 0 END) as ConsolidatedVoids
                    FROM PaymentAuditLog
                    WHERE Action = 'Voided'";

                if (startDate.HasValue)
                {
                    query += " AND ChangedAt >= @StartDate";
                }

                if (endDate.HasValue)
                {
                    query += " AND ChangedAt <= @EndDate";
                }

                query += @"
                    GROUP BY EntityType
                    ORDER BY EntityType";

                using var command = new SqlCommand(query, connection);
                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value);
                }

                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@EndDate", endDate.Value);
                }

                var statistics = new VoidingStatistics
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalVoids = 0,
                    RegularVoids = 0,
                    AdvanceVoids = 0,
                    ConsolidatedVoids = 0
                };

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var entityType = reader["EntityType"].ToString();
                    var voidCount = Convert.ToInt32(reader["VoidCount"]);

                    statistics.TotalVoids += voidCount;

                    switch (entityType.ToLower())
                    {
                        case "regular":
                            statistics.RegularVoids = voidCount;
                            break;
                        case "advance":
                            statistics.AdvanceVoids = voidCount;
                            break;
                        case "consolidated":
                            statistics.ConsolidatedVoids = voidCount;
                            break;
                    }
                }

                return statistics;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting voiding statistics: {ex.Message}", ex);
            }
        }

        public async Task<bool> ReverseVoidingAsync(int voidingId, string reason, string reversedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE PaymentAuditLog 
                    SET Action = 'VoidingReversed',
                        NewValues = @NewValues,
                        ChangedAt = @ChangedAt,
                        ChangedBy = @ChangedBy,
                        Reason = @Reason
                    WHERE AuditLogId = @AuditLogId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AuditLogId", voidingId);
                command.Parameters.AddWithValue("@NewValues", $"Voiding reversed by {reversedBy}");
                command.Parameters.AddWithValue("@ChangedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ChangedBy", reversedBy);
                command.Parameters.AddWithValue("@Reason", reason);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error reversing voiding: {ex.Message}", ex);
            }
        }

        public async Task<List<VoidedPayment>> GetVoidedPaymentsAsync(DateTime startDate, DateTime endDate, string entityType = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        pal.EntityType,
                        pal.EntityId,
                        pal.ChangedAt,
                        pal.ChangedBy,
                        pal.Reason,
                        CASE 
                            WHEN pal.EntityType = 'Regular' THEN c.ChequeAmount
                            WHEN pal.EntityType = 'Advance' THEN ac.AdvanceAmount
                            WHEN pal.EntityType = 'Consolidated' THEN c.ChequeAmount
                            ELSE 0
                        END as Amount
                    FROM PaymentAuditLog pal
                    LEFT JOIN Cheques c ON pal.EntityId = c.ChequeId AND pal.EntityType = 'Regular'
                    LEFT JOIN AdvanceCheques ac ON pal.EntityId = ac.AdvanceChequeId AND pal.EntityType = 'Advance'
                    WHERE pal.Action = 'Voided'
                    AND pal.ChangedAt >= @StartDate
                    AND pal.ChangedAt <= @EndDate";

                if (!string.IsNullOrEmpty(entityType))
                {
                    query += " AND pal.EntityType = @EntityType";
                }

                query += " ORDER BY pal.ChangedAt DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                if (!string.IsNullOrEmpty(entityType))
                {
                    command.Parameters.AddWithValue("@EntityType", entityType);
                }

                var voidedPayments = new List<VoidedPayment>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    voidedPayments.Add(new VoidedPayment
                    {
                        EntityType = reader["EntityType"].ToString(),
                        EntityId = Convert.ToInt32(reader["EntityId"]),
                        VoidedAt = Convert.ToDateTime(reader["ChangedAt"]),
                        VoidedBy = reader["ChangedBy"].ToString(),
                        Reason = reader["Reason"].ToString(),
                        Amount = Convert.ToDecimal(reader["Amount"])
                    });
                }

                return voidedPayments;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting voided payments: {ex.Message}", ex);
            }
        }

        private PaymentAuditLog MapPaymentAuditLogFromReader(SqlDataReader reader)
        {
            return new PaymentAuditLog
            {
                AuditLogId = Convert.ToInt32(reader["AuditLogId"]),
                EntityType = reader["EntityType"].ToString(),
                EntityId = Convert.ToInt32(reader["EntityId"]),
                Action = reader["Action"].ToString(),
                OldValues = reader.IsDBNull(reader.GetOrdinal("OldValues")) ? null : reader["OldValues"].ToString(),
                NewValues = reader.IsDBNull(reader.GetOrdinal("NewValues")) ? null : reader["NewValues"].ToString(),
                ChangedBy = reader["ChangedBy"].ToString(),
                ChangedAt = Convert.ToDateTime(reader["ChangedAt"]),
                Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader["Reason"].ToString()
            };
        }

        /// <summary>
        /// Update payment distribution item status when voiding a cheque
        /// </summary>
        private async Task UpdatePaymentDistributionItemStatusAsync(int chequeId, string status, string voidedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Find the payment distribution item for this cheque
                var findSql = @"
                    SELECT pdi.PaymentDistributionItemId, pdi.GrowerId, pdi.PaymentBatchId
                    FROM PaymentDistributionItems pdi
                    INNER JOIN Cheques c ON pdi.GrowerId = c.GrowerId AND pdi.PaymentBatchId = c.PaymentBatchId
                    WHERE c.ChequeId = @ChequeId";

                using var findCommand = new SqlCommand(findSql, connection);
                findCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                
                using var reader = await findCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var distributionItemId = reader.GetInt32(reader.GetOrdinal("PaymentDistributionItemId"));
                    var growerId = reader.GetInt32(reader.GetOrdinal("GrowerId"));
                    var paymentBatchId = reader.GetInt32(reader.GetOrdinal("PaymentBatchId"));
                    
                    reader.Close();

                    // Update the payment distribution item status
                    var updateSql = @"
                        UPDATE PaymentDistributionItems 
                        SET Status = @Status,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentDistributionItemId = @PaymentDistributionItemId";

                    using var updateCommand = new SqlCommand(updateSql, connection);
                    updateCommand.Parameters.AddWithValue("@Status", status);
                    updateCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@ModifiedBy", voidedBy);
                    updateCommand.Parameters.AddWithValue("@PaymentDistributionItemId", distributionItemId);
                    
                    await updateCommand.ExecuteNonQueryAsync();
                    try
                    {
                        Logger.Info($"Updated payment distribution item {distributionItemId} status to {status} for grower {growerId} in batch {paymentBatchId}");
                    }
                    catch
                    {
                        // Logger might be null or unavailable, continue without logging
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.Error($"Error updating payment distribution item status for cheque {chequeId}: {ex.Message}", ex);
                }
                catch
                {
                    // Logger might be null or unavailable, continue without logging
                }
                // Don't throw - this is not critical for the void operation
            }
        }

        /// <summary>
        /// Revert batch status if needed when voiding a cheque
        /// </summary>
        private async Task RevertBatchStatusIfNeededAsync(int paymentBatchId, string voidedBy, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    UPDATE PaymentBatches 
                    SET Status = 'Posted',
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE PaymentBatchId = @PaymentBatchId 
                    AND Status = 'Finalized'";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@PaymentBatchId", paymentBatchId);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", voidedBy);
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                if (rowsAffected > 0)
                {
                    Logger.Info($"Reverted batch {paymentBatchId} to Posted status");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reverting batch {paymentBatchId} status: {ex.Message}", ex);
                // Don't throw - this is not critical for the void operation
            }
        }

        /// <summary>
        /// Check if a cheque is consolidated
        /// </summary>
        private async Task<bool> IsConsolidatedChequeAsync(int chequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                var sql = "SELECT IsConsolidated FROM Cheques WHERE ChequeId = @ChequeId";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ChequeId", chequeId);
                
                var result = await command.ExecuteScalarAsync();
                return result != null && Convert.ToBoolean(result);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking if cheque {chequeId} is consolidated: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Safely commit a transaction, handling any exceptions
        /// </summary>
        private async Task SafeCommitAsync(SqlTransaction transaction)
        {
            try
            {
                if (IsTransactionActive(transaction))
                {
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error committing transaction: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Safely rollback a transaction, handling any exceptions
        /// </summary>
        private async Task SafeRollbackAsync(SqlTransaction transaction)
        {
            try
            {
                if (IsTransactionActive(transaction))
                {
                    transaction.Rollback();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error rolling back transaction: {ex.Message}", ex);
                // Don't throw here as we're already in an error state
            }
        }

        /// <summary>
        /// Check if a transaction is still active and usable
        /// </summary>
        private bool IsTransactionActive(SqlTransaction transaction)
        {
            try
            {
                return transaction != null && 
                       transaction.Connection != null && 
                       transaction.Connection.State == System.Data.ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get cheque by ID with connection and transaction
        /// </summary>
        private async Task<Cheque> GetChequeByIdAsync(int chequeId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    SELECT c.*, g.FullName as GrowerName, g.GrowerNumber
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    WHERE c.ChequeId = @ChequeId";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@ChequeId", chequeId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapChequeFromReader(reader);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.Error($"Error getting cheque by ID {chequeId}: {ex.Message}", ex);
                }
                catch
                {
                    // Logger might be null or unavailable, continue without logging
                }
                return null;
            }
        }

        /// <summary>
        /// Get advance deductions by cheque ID
        /// </summary>
        private async Task<List<AdvanceDeduction>> GetAdvanceDeductionsByChequeIdAsync(int chequeId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    SELECT ad.*, ac.AdvanceAmount, ac.AdvanceDate, ac.Reason
                    FROM AdvanceDeductions ad
                    INNER JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ad.ChequeId = @ChequeId";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@ChequeId", chequeId);

                var deductions = new List<AdvanceDeduction>();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        deductions.Add(MapAdvanceDeductionFromReader(reader));
                    }
                }
                return deductions;
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.Error($"Error getting advance deductions for cheque {chequeId}: {ex.Message}", ex);
                }
                catch
                {
                    // Logger might be null or unavailable, continue without logging
                }
                return new List<AdvanceDeduction>();
            }
        }

        /// <summary>
        /// Reverse advance deductions for a cheque
        /// </summary>
        private async Task ReverseAdvanceDeductionsAsync(int chequeId, string reason, string voidedBy, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Get advance deductions for this cheque
                var deductions = await GetAdvanceDeductionsByChequeIdAsync(chequeId, connection, transaction);

                foreach (var deduction in deductions)
                {
                    // Delete the deduction record
                    var deleteSql = "DELETE FROM AdvanceDeductions WHERE DeductionId = @DeductionId";
                    using var deleteCommand = new SqlCommand(deleteSql, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@DeductionId", deduction.DeductionId);
                    await deleteCommand.ExecuteNonQueryAsync();

                    // Clear deduction references without changing advance cheque status
                    // The advance cheque status should remain unchanged (e.g., 'Printed')
                    var resetSql = @"
                        UPDATE AdvanceCheques 
                        SET DeductedByChequeId = NULL,
                            DeductedAt = NULL,
                            DeductedBy = NULL,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE AdvanceChequeId = @AdvanceChequeId";

                    using var resetCommand = new SqlCommand(resetSql, connection, transaction);
                    resetCommand.Parameters.AddWithValue("@AdvanceChequeId", deduction.AdvanceChequeId);
                    resetCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    resetCommand.Parameters.AddWithValue("@ModifiedBy", voidedBy);
                    await resetCommand.ExecuteNonQueryAsync();
                }

                try
                {
                    Logger.Info($"Reversed {deductions.Count} advance deductions for cheque {chequeId}");
                }
                catch
                {
                    // Logger might be null or unavailable, continue without logging
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.Error($"Error reversing advance deductions for cheque {chequeId}: {ex.Message}", ex);
                }
                catch
                {
                    // Logger might be null or unavailable, continue without logging
                }
                throw;
            }
        }

        /// <summary>
        /// Update payment distribution items status
        /// </summary>
        private async Task UpdatePaymentDistributionItemsAsync(int chequeId, string status, string updatedBy, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // First get the cheque details to find PaymentDistributionId and GrowerId
                var chequeSql = "SELECT PaymentDistributionId, GrowerId FROM Cheques WHERE ChequeId = @ChequeId";
                using var chequeCommand = new SqlCommand(chequeSql, connection, transaction);
                chequeCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                
                int? paymentDistributionId = null;
                int growerId = 0;
                
                using (var chequeReader = await chequeCommand.ExecuteReaderAsync())
                {
                    if (await chequeReader.ReadAsync())
                    {
                        paymentDistributionId = chequeReader.IsDBNull(0) ? null : chequeReader.GetInt32(0);
                        growerId = chequeReader.GetInt32(1);
                    }
                } // Reader is explicitly closed here

                if (paymentDistributionId.HasValue && growerId > 0)
                {
                    // Update payment distribution items using PaymentDistributionId AND GrowerId
                    var sql = @"
                        UPDATE PaymentDistributionItems 
                        SET Status = @Status,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentDistributionId = @PaymentDistributionId AND GrowerId = @GrowerId";

                    using var command = new SqlCommand(sql, connection, transaction);
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@ModifiedBy", updatedBy);
                    command.Parameters.AddWithValue("@PaymentDistributionId", paymentDistributionId.Value);
                    command.Parameters.AddWithValue("@GrowerId", growerId);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    
                    Logger.Info($"Updated {rowsAffected} payment distribution items to {status} for PaymentDistributionId {paymentDistributionId} and GrowerId {growerId}");
                }
                else
                {
                    Logger.Info($"Could not find PaymentDistributionId or GrowerId for cheque {chequeId}");
                  
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating payment distribution items for cheque {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Update PaymentDistributionReceipts status for voiding support
        /// </summary>
        private async Task UpdatePaymentDistributionReceiptsAsync(int paymentDistributionId, int growerId, string status, string updatedBy, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Update PaymentDistributionReceipts for the specific grower and distribution
                var sql = @"
                    UPDATE PaymentDistributionReceipts 
                    SET Status = @Status,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy,
                        VoidedAt = @VoidedAt,
                        VoidedBy = @VoidedBy
                    WHERE PaymentDistributionItemId IN (
                        SELECT PaymentDistributionItemId 
                        FROM PaymentDistributionItems 
                        WHERE PaymentDistributionId = @PaymentDistributionId 
                        AND GrowerId = @GrowerId
                    )";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", updatedBy);
                command.Parameters.AddWithValue("@PaymentDistributionId", paymentDistributionId);
                command.Parameters.AddWithValue("@GrowerId", growerId);

                // Set voiding fields only if status is "Voided"
                if (status == "Voided")
                {
                    command.Parameters.AddWithValue("@VoidedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@VoidedBy", updatedBy);
                }
                else
                {
                    command.Parameters.AddWithValue("@VoidedAt", DBNull.Value);
                    command.Parameters.AddWithValue("@VoidedBy", DBNull.Value);
                }

                var rowsAffected = await command.ExecuteNonQueryAsync();
                
                Logger.Info($"Updated {rowsAffected} payment distribution receipts to {status} for PaymentDistributionId {paymentDistributionId} and GrowerId {growerId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating payment distribution receipts for PaymentDistributionId {paymentDistributionId} and GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all batch IDs that are actually affected by a payment distribution for a specific grower
        /// </summary>
        private async Task<List<int>> GetAffectedBatchIdsAsync(int paymentDistributionId, int growerId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    SELECT DISTINCT pdr.PaymentBatchId
                    FROM PaymentDistributionReceipts pdr
                    INNER JOIN PaymentDistributionItems pdi ON pdr.PaymentDistributionItemId = pdi.PaymentDistributionItemId
                    WHERE pdi.PaymentDistributionId = @PaymentDistributionId 
                      AND pdi.GrowerId = @GrowerId";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@PaymentDistributionId", paymentDistributionId);
                command.Parameters.AddWithValue("@GrowerId", growerId);

                var batchIds = new List<int>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    batchIds.Add(reader.GetInt32(0));
                }

                Logger.Info($"Found {batchIds.Count} affected batch IDs for PaymentDistributionId {paymentDistributionId} and GrowerId {growerId}: [{string.Join(", ", batchIds)}]");
                return batchIds;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting affected batch IDs for PaymentDistributionId {paymentDistributionId} and GrowerId {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Void cheque with connection and transaction
        /// </summary>
        private async Task VoidChequeAsync(int chequeId, string reason, string voidedBy, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Voided',
                        VoidedDate = @VoidedDate,
                        VoidedBy = @VoidedBy,
                        VoidedReason = @Reason,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE ChequeId = @ChequeId";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@ChequeId", chequeId);
                command.Parameters.AddWithValue("@VoidedDate", DateTime.Now);
                command.Parameters.AddWithValue("@VoidedBy", voidedBy);
                command.Parameters.AddWithValue("@Reason", reason);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", voidedBy);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException($"No cheques were voided. Cheque {chequeId} may already be processed or not found.");
                }

                Logger.Info($"Successfully voided cheque {chequeId}. Reason: {reason}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error voiding cheque {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Clean up orphaned payment distributions
        /// </summary>
        private async Task CleanupOrphanedDistributionsAsync(int paymentBatchId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Check if all items in distributions for this batch are voided
                var sql = @"
                    SELECT pdi.PaymentDistributionId, COUNT(*) as TotalItems, 
                           SUM(CASE WHEN pdi.Status = 'Voided' THEN 1 ELSE 0 END) as VoidedItems
                    FROM PaymentDistributionItems pdi
                    WHERE pdi.PaymentBatchId = @PaymentBatchId
                    GROUP BY pdi.PaymentDistributionId
                    HAVING COUNT(*) = SUM(CASE WHEN pdi.Status = 'Voided' THEN 1 ELSE 0 END)";

                using var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@PaymentBatchId", paymentBatchId);

                var orphanedDistributionIds = new List<int>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    orphanedDistributionIds.Add(reader.GetInt32(0)); // Use column index instead of name
                }

                // Clean up orphaned distributions
                foreach (var distributionId in orphanedDistributionIds)
                {
                    // Delete orphaned distribution items
                    var deleteItemsSql = "DELETE FROM PaymentDistributionItems WHERE PaymentDistributionId = @DistributionId";
                    using var deleteItemsCommand = new SqlCommand(deleteItemsSql, connection, transaction);
                    deleteItemsCommand.Parameters.AddWithValue("@DistributionId", distributionId);
                    await deleteItemsCommand.ExecuteNonQueryAsync();

                    // Delete orphaned distribution
                    var deleteDistributionSql = "DELETE FROM PaymentDistributions WHERE PaymentDistributionId = @DistributionId";
                    using var deleteDistributionCommand = new SqlCommand(deleteDistributionSql, connection, transaction);
                    deleteDistributionCommand.Parameters.AddWithValue("@DistributionId", distributionId);
                    await deleteDistributionCommand.ExecuteNonQueryAsync();
                }

                if (orphanedDistributionIds.Any())
                {
                    Logger.Info($"Cleaned up {orphanedDistributionIds.Count} orphaned payment distributions for batch {paymentBatchId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error cleaning up orphaned distributions for batch {paymentBatchId}: {ex.Message}", ex);
                // Don't throw - this is not critical for the void operation
            }
        }

        /// <summary>
        /// Map cheque from SqlDataReader
        /// </summary>
        private Cheque MapChequeFromReader(SqlDataReader reader)
        {
            // Use ordinals by column name and guard nulls to avoid SqlNullValueException
            int Ord(string name) => reader.GetOrdinal(name);
            string? GetStr(string name) => reader.IsDBNull(Ord(name)) ? null : reader.GetString(Ord(name));
            DateTime? GetDt(string name) => reader.IsDBNull(Ord(name)) ? (DateTime?)null : reader.GetDateTime(Ord(name));
            int? GetIntN(string name) => reader.IsDBNull(Ord(name)) ? (int?)null : reader.GetInt32(Ord(name));
            decimal GetDec(string name) => reader.IsDBNull(Ord(name)) ? 0m : reader.GetDecimal(Ord(name));

            var cheque = new Cheque
            {
                ChequeId = reader.GetInt32(Ord("ChequeId")),
                ChequeSeriesId = reader.GetInt32(Ord("ChequeSeriesId")),
                ChequeNumber = GetStr("ChequeNumber") ?? string.Empty,
                FiscalYear = reader.GetInt32(Ord("FiscalYear")),
                GrowerId = reader.GetInt32(Ord("GrowerId")),
                PaymentBatchId = GetIntN("PaymentBatchId"),
                ChequeDate = reader.GetDateTime(Ord("ChequeDate")),
                ChequeAmount = GetDec("ChequeAmount"),
                CurrencyCode = GetStr("CurrencyCode") ?? "CAD",
                ExchangeRate = reader.IsDBNull(Ord("ExchangeRate")) ? 1.0m : reader.GetDecimal(Ord("ExchangeRate")),
                PayeeName = GetStr("PayeeName"),
                Memo = GetStr("Memo"),
                Status = GetStr("Status") ?? "Generated",
                ClearedDate = GetDt("ClearedDate"),
                VoidedDate = GetDt("VoidedDate"),
                VoidedReason = GetStr("VoidedReason"),
                VoidedBy = GetStr("VoidedBy"),
                CreatedAt = reader.GetDateTime(Ord("CreatedAt")),
                CreatedBy = GetStr("CreatedBy"),
                // Joined columns from Growers (may be null)
                GrowerName = GetStr("GrowerName"),
                GrowerNumber = GetStr("GrowerNumber"),
                PaymentDistributionId = reader.GetInt32(Ord("PaymentDistributionId"))
            };

            return cheque;
        }

        /// <summary>
        /// Map advance deduction from SqlDataReader
        /// </summary>
        private AdvanceDeduction MapAdvanceDeductionFromReader(SqlDataReader reader)
        {
            int Ord(string name) => reader.GetOrdinal(name);
            var paymentBatchId = reader.IsDBNull(Ord("PaymentBatchId")) ? 0 : reader.GetInt32(Ord("PaymentBatchId"));
            return new AdvanceDeduction
            {
                DeductionId = reader.GetInt32(Ord("DeductionId")),
                AdvanceChequeId = reader.GetInt32(Ord("AdvanceChequeId")),
                PaymentBatchId = paymentBatchId,
                DeductionAmount = reader.GetDecimal(Ord("DeductionAmount")),
                DeductionDate = reader.GetDateTime(Ord("DeductionDate")),
                CreatedBy = reader.IsDBNull(Ord("CreatedBy")) ? string.Empty : reader.GetString(Ord("CreatedBy")),
                CreatedAt = reader.GetDateTime(Ord("CreatedAt"))
            };
        }

        /// <summary>
        /// Checks if the given ID belongs to an advance cheque
        /// </summary>
        private async Task<bool> IsAdvanceChequeIdAsync(int entityId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*) 
                    FROM AdvanceCheques 
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND DeletedAt IS NULL";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", entityId);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - return false to default to regular cheque handling
                Logger.Error($"Error checking if ID {entityId} is an advance cheque: {ex.Message}", ex);
                return false;
            }
        }
    }
}
