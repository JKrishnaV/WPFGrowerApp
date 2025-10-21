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
                var result = new VoidingResult();

                switch (request.EntityType.ToLower())
                {
                    case "regular":
                    case "cheque":
                        result = await VoidRegularBatchPaymentAsync(request.EntityId, request.Reason, request.VoidedBy);
                        break;
                    case "advance":
                    case "advancecheque":
                        result = await VoidAdvanceChequeAsync(request.EntityId, request.Reason, request.VoidedBy);
                        break;
                    case "consolidated":
                    case "consolidatedcheque":
                        result = await VoidConsolidatedPaymentAsync(request.EntityId, request.Reason, request.VoidedBy);
                        break;
                    default:
                        result = new VoidingResult(false, $"Unknown entity type: {request.EntityType}");
                        break;
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
                var result = new VoidingResult();

                // Get the cheque details
                var cheque = await _chequeService.GetChequeByIdAsync(chequeId);
                if (cheque == null)
                {
                    result.AddError("Cheque not found");
                    return result;
                }

                if (!cheque.CanBeVoided)
                {
                    result.AddError("Cheque cannot be voided in its current status");
                    return result;
                }

                // Check for advance deductions that need to be reversed
                var advanceDeductions = await _advanceDeductionService.GetDeductionHistoryByChequeIdAsync(chequeId);
                if (advanceDeductions.Any())
                {
                    result.AddWarning($"Cheque has {advanceDeductions.Count} advance deductions that will be reversed.");
                }

                // Void the cheque
                var success = await _chequeService.VoidChequeAsync(chequeId, reason, voidedBy);
                if (success)
                {
                    // Reverse advance deductions if any exist
                    if (advanceDeductions.Any())
                    {
                        foreach (var deduction in advanceDeductions)
                        {
                            await _advanceDeductionService.ReverseAdvanceDeductionAsync(deduction.AdvanceChequeId, reason, voidedBy);
                        }
                        result.DeductionsReversed = true;
                    }

                    // Update corresponding payment distribution item status
                    await UpdatePaymentDistributionItemStatusAsync(chequeId, "Voided", voidedBy);

                    // Revert batch status if needed
                    if (cheque.PaymentBatchId.HasValue)
                    {
                        await RevertBatchStatusIfNeededAsync(cheque.PaymentBatchId.Value, voidedBy);
                    }

                    result.Success = true;
                    result.Message = "Regular batch payment voided successfully";
                    result.EntityType = "Regular";
                    result.EntityId = chequeId;
                    result.AmountReversed = cheque.ChequeAmount;
                    result.VoidedBy = voidedBy;
                    result.VoidedAt = DateTime.Now;
                }
                else
                {
                    result.AddError("Failed to void the cheque");
                }

                return result;
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

                // Revert the consolidation
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
                }
                else
                {
                    result.AddError("Failed to void the consolidated payment");
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
                    Logger.Info($"Updated payment distribution item {distributionItemId} status to {status} for grower {growerId} in batch {paymentBatchId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating payment distribution item status for cheque {chequeId}: {ex.Message}", ex);
                // Don't throw - this is not critical for the void operation
            }
        }

        /// <summary>
        /// Revert batch status if needed when voiding a cheque
        /// </summary>
        private async Task RevertBatchStatusIfNeededAsync(int paymentBatchId, string voidedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Check if batch has any non-voided cheques
                var checkSql = @"
                    SELECT COUNT(*) as ActiveChequeCount
                    FROM Cheques 
                    WHERE PaymentBatchId = @PaymentBatchId 
                    AND Status != 'Voided'";

                using var checkCommand = new SqlCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@PaymentBatchId", paymentBatchId);
                var activeChequeCount = (int)await checkCommand.ExecuteScalarAsync();

                // If no active cheques remain, revert batch to Draft status
                if (activeChequeCount == 0)
                {
                    var revertSql = @"
                        UPDATE PaymentBatches 
                        SET Status = 'Draft',
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentBatchId = @PaymentBatchId";

                    using var revertCommand = new SqlCommand(revertSql, connection);
                    revertCommand.Parameters.AddWithValue("@PaymentBatchId", paymentBatchId);
                    revertCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    revertCommand.Parameters.AddWithValue("@ModifiedBy", voidedBy);
                    
                    await revertCommand.ExecuteNonQueryAsync();
                    Logger.Info($"Reverted batch {paymentBatchId} to Draft status - no active cheques remaining");
                }
                else
                {
                    // If there are still active cheques, revert to Posted status
                    var revertSql = @"
                        UPDATE PaymentBatches 
                        SET Status = 'Posted',
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentBatchId = @PaymentBatchId 
                        AND Status = 'Finalized'";

                    using var revertCommand = new SqlCommand(revertSql, connection);
                    revertCommand.Parameters.AddWithValue("@PaymentBatchId", paymentBatchId);
                    revertCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    revertCommand.Parameters.AddWithValue("@ModifiedBy", voidedBy);
                    
                    var rowsAffected = await revertCommand.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        Logger.Info($"Reverted batch {paymentBatchId} to Posted status - {activeChequeCount} active cheques remaining");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reverting batch status for batch {paymentBatchId}: {ex.Message}", ex);
                // Don't throw - this is not critical for the void operation
            }
        }
    }
}
