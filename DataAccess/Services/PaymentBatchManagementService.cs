using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing payment batch lifecycle
    /// </summary>
    public class PaymentBatchManagementService : BaseDatabaseService, IPaymentBatchManagementService
    {
        private readonly IPaymentTypeService _paymentTypeService;

        public PaymentBatchManagementService(IPaymentTypeService paymentTypeService)
        {
            _paymentTypeService = paymentTypeService;
        }

        // ==============================================================
        // BATCH CRUD OPERATIONS
        // ==============================================================

        /// <summary>
        /// Create a new payment batch
        /// </summary>
        public async Task<PaymentBatch> CreatePaymentBatchAsync(
            int paymentTypeId,
            DateTime batchDate,
            int cropYear,
            DateTime? cutoffDate = null,
            string? notes = null,
            string? createdBy = null)
        {
            try
            {
                var batchNumber = await GenerateNextBatchNumberAsync(paymentTypeId, cropYear);
                createdBy ??= App.CurrentUser?.Username ?? "SYSTEM";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        INSERT INTO PaymentBatches (
                            BatchNumber, PaymentTypeId, BatchDate, CropYear, CutoffDate,
                            Status, Notes, CreatedAt, CreatedBy
                        )
                        OUTPUT INSERTED.*
                        VALUES (
                            @BatchNumber, @PaymentTypeId, @BatchDate, @CropYear, @CutoffDate,
                            @Status, @Notes, @CreatedAt, @CreatedBy
                        )";

                    var batch = await connection.QuerySingleAsync<PaymentBatch>(sql, new
                    {
                        BatchNumber = batchNumber,
                        PaymentTypeId = paymentTypeId,
                        BatchDate = batchDate,
                        CropYear = cropYear,
                        CutoffDate = cutoffDate,
                        Status = "Draft",
                        Notes = notes,
                        CreatedAt = DateTime.Now,
                        CreatedBy = createdBy
                    });

                    Logger.Info($"Created payment batch: {batchNumber} by {createdBy}");
                    return batch;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment batch: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Update an existing payment batch
        /// </summary>
        public async Task<bool> UpdatePaymentBatchAsync(PaymentBatch batch, string modifiedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE PaymentBatches
                        SET 
                            BatchDate = @BatchDate,
                            CropYear = @CropYear,
                            CutoffDate = @CutoffDate,
                            FilterPayGroup = @FilterPayGroup,
                            FilterGrower = @FilterGrower,
                            Notes = @Notes,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentBatchId = @PaymentBatchId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        batch.PaymentBatchId,
                        batch.BatchDate,
                        batch.CropYear,
                        batch.CutoffDate,
                        batch.FilterPayGroup,
                        batch.FilterGrower,
                        batch.Notes,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = modifiedBy
                    });

                    Logger.Info($"Updated payment batch {batch.BatchNumber} by {modifiedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating payment batch {batch.PaymentBatchId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get payment batch by ID
        /// </summary>
        public async Task<PaymentBatch?> GetPaymentBatchByIdAsync(int paymentBatchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            pb.PaymentBatchId,
                            pb.BatchNumber,
                            pb.PaymentTypeId,
                            pb.BatchDate,
                            pb.TotalAmount,
                            pb.TotalGrowers,
                            pb.TotalReceipts,
                            pb.Status,
                            pb.Notes,
                            pb.ProcessedAt,
                            pb.ProcessedBy,
                            pb.CreatedAt,
                            pb.CreatedBy,
                            pb.ModifiedAt,
                            pb.ModifiedBy,
                            pb.DeletedAt,
                            pb.DeletedBy,
                            pb.CropYear,
                            pb.CutoffDate,
                            pb.FilterPayGroup,
                            pb.FilterGrower,
                            pt.TypeName as PaymentTypeName
                        FROM PaymentBatches pb
                        INNER JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                        WHERE pb.PaymentBatchId = @PaymentBatchId";

                    return await connection.QueryFirstOrDefaultAsync<PaymentBatch>(sql, 
                        new { PaymentBatchId = paymentBatchId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment batch {paymentBatchId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all payment batches (optionally filtered)
        /// </summary>
        public async Task<List<PaymentBatch>> GetAllPaymentBatchesAsync(
            int? cropYear = null,
            string? status = null,
            int? paymentTypeId = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            pb.PaymentBatchId,
                            pb.BatchNumber,
                            pb.PaymentTypeId,
                            pb.BatchDate,
                            pb.TotalAmount,
                            pb.TotalGrowers,
                            pb.TotalReceipts,
                            pb.Status,
                            pb.Notes,
                            pb.ProcessedAt,
                            pb.ProcessedBy,
                            pb.CreatedAt,
                            pb.CreatedBy,
                            pb.ModifiedAt,
                            pb.ModifiedBy,
                            pb.DeletedAt,
                            pb.DeletedBy,
                            pb.CropYear,
                            pb.CutoffDate,
                            pb.FilterPayGroup,
                            pb.FilterGrower,
                            pt.TypeName as PaymentTypeName
                        FROM PaymentBatches pb
                        INNER JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                        WHERE 
                            -- Handle soft delete based on status filter
                            (
                                (@Status = 'Voided' AND pb.DeletedAt IS NOT NULL) OR
                                (@Status != 'Voided' AND @Status IS NOT NULL AND pb.DeletedAt IS NULL) OR
                                (@Status IS NULL)
                            )
                          AND (@CropYear IS NULL OR pb.CropYear = @CropYear)
                          AND (@Status IS NULL OR pb.Status = @Status)
                          AND (@PaymentTypeId IS NULL OR pb.PaymentTypeId = @PaymentTypeId)
                        ORDER BY pb.BatchDate DESC, pb.BatchNumber DESC";

                    var batches = (await connection.QueryAsync<PaymentBatch>(sql, new
                    {
                        CropYear = cropYear,
                        Status = status,
                        PaymentTypeId = paymentTypeId
                    })).ToList();

                    Logger.Info($"Retrieved {batches.Count} payment batches");
                    return batches;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment batches: {ex.Message}", ex);
                throw;
            }
        }

        // ==============================================================
        // BATCH STATUS MANAGEMENT
        // ==============================================================

        /// <summary>
        /// Update batch status
        /// </summary>
        public async Task<bool> UpdateBatchStatusAsync(
            int paymentBatchId,
            string newStatus,
            string modifiedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE PaymentBatches
                        SET 
                            Status = @NewStatus,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentBatchId = @PaymentBatchId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        NewStatus = newStatus,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = modifiedBy
                    });

                    Logger.Info($"Updated batch {paymentBatchId} status to {newStatus} by {modifiedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating batch status: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Mark batch as posted
        /// </summary>
        public async Task<bool> MarkBatchAsPostedAsync(
            int paymentBatchId,
            int totalGrowers,
            decimal totalAmount,
            int totalReceipts,
            string postedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE PaymentBatches
                        SET 
                            Status = 'Posted',
                            TotalGrowers = @TotalGrowers,
                            TotalAmount = @TotalAmount,
                            TotalReceipts = @TotalReceipts,
                            ProcessedAt = @ProcessedAt,
                            ProcessedBy = @ProcessedBy,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentBatchId = @PaymentBatchId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        TotalGrowers = totalGrowers,
                        TotalAmount = totalAmount,
                        TotalReceipts = totalReceipts,
                        ProcessedAt = DateTime.Now,
                        ProcessedBy = postedBy,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = postedBy
                    });

                    Logger.Info($"Marked batch {paymentBatchId} as posted: {totalGrowers} growers, ${totalAmount:N2} by {postedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking batch as posted: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Approve a payment batch (Draft → Posted)
        /// </summary>
        public async Task<bool> ApproveBatchAsync(int paymentBatchId, string approvedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE PaymentBatches
                        SET 
                            Status = 'Posted',
                            ProcessedAt = @ProcessedAt,
                            ProcessedBy = @ProcessedBy,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentBatchId = @PaymentBatchId AND Status = 'Draft'";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        ProcessedAt = DateTime.Now,
                        ProcessedBy = approvedBy,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = approvedBy
                    });

                    Logger.Info($"Approved payment batch {paymentBatchId} by {approvedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error approving payment batch {paymentBatchId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Process payments for a batch (Posted → Finalized)
        /// </summary>
        public async Task<bool> ProcessPaymentsAsync(int paymentBatchId, string processedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE PaymentBatches
                        SET 
                            Status = 'Finalized',
                            ProcessedAt = @ProcessedAt,
                            ProcessedBy = @ProcessedBy,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentBatchId = @PaymentBatchId AND Status = 'Posted'";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        ProcessedAt = DateTime.Now,
                        ProcessedBy = processedBy,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = processedBy
                    });

                    Logger.Info($"Processed payments for batch {paymentBatchId} by {processedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing payments for batch {paymentBatchId}", ex);
                throw;
            }
        }

        /// <summary>
        /// Void a payment batch - voids the batch, all allocations, and all cheques in a transaction
        /// </summary>
        public async Task<bool> VoidBatchAsync(
            int paymentBatchId,
            string reason,
            string voidedBy)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Void all allocations
                        var voidAllocations = @"
                            UPDATE ReceiptPaymentAllocations
                            SET Status = 'Voided',
                                ModifiedAt = GETDATE(),
                                ModifiedBy = @VoidedBy
                            WHERE PaymentBatchId = @PaymentBatchId
                              AND Status != 'Voided'";
                        
                        var allocationsVoided = await connection.ExecuteAsync(voidAllocations, 
                            new { PaymentBatchId = paymentBatchId, VoidedBy = voidedBy },
                            transaction: transaction);
                        
                        // 2. Void all cheques for this batch
                        var voidCheques = @"
                            UPDATE Cheques
                            SET Status = 'Voided',
                                ModifiedAt = GETDATE(),
                                ModifiedBy = @VoidedBy
                            WHERE PaymentBatchId = @PaymentBatchId
                              AND Status != 'Voided'";
                        
                        var chequesVoided = await connection.ExecuteAsync(voidCheques,
                            new { PaymentBatchId = paymentBatchId, VoidedBy = voidedBy },
                            transaction: transaction);
                        
                        // 3. Void the batch with soft delete
                        var voidBatch = @"
                            UPDATE PaymentBatches
                            SET Status = 'Voided',
                                DeletedAt = GETDATE(),
                                DeletedBy = @VoidedBy,
                                Notes = ISNULL(Notes, '') + CHAR(13) + CHAR(10) + 
                                       'VOIDED: ' + @Reason + ' by ' + @VoidedBy + 
                                       ' on ' + CONVERT(NVARCHAR, GETDATE(), 120),
                                ModifiedAt = GETDATE(),
                                ModifiedBy = @VoidedBy
                            WHERE PaymentBatchId = @PaymentBatchId";
                        
                        await connection.ExecuteAsync(voidBatch,
                            new { PaymentBatchId = paymentBatchId, Reason = reason, VoidedBy = voidedBy },
                            transaction: transaction);
                        
                        transaction.Commit();
                        Logger.Info($"Successfully voided batch {paymentBatchId} - voided {allocationsVoided} allocations and {chequesVoided} cheques");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error($"Failed to void batch {paymentBatchId}, transaction rolled back", ex);
                        throw;
                    }
                }
            }
        }

        // ==============================================================
        // BATCH SUMMARIES & REPORTING
        // ==============================================================

        /// <summary>
        /// Get summary information for a payment batch
        /// </summary>
        public async Task<PaymentBatchSummary> GetBatchSummaryAsync(int paymentBatchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            pb.PaymentBatchId,
                            pb.BatchNumber,
                            pt.TypeName AS PaymentTypeName,
                            pb.BatchDate,
                            pb.CropYear,
                            pb.Status,
                            pb.TotalAmount,
                            pb.TotalGrowers,
                            pb.TotalReceipts,
                            pb.CreatedAt,
                            pb.CreatedBy,
                            pb.ProcessedAt AS PostedAt,
                            pb.ProcessedBy AS PostedBy,
                            pb.Notes,
                            COUNT(DISTINCT c.ChequeId) AS ChequesGenerated
                        FROM PaymentBatches pb
                        INNER JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                        LEFT JOIN Cheques c ON pb.PaymentBatchId = c.PaymentBatchId AND c.Status != 'Voided'
                        WHERE pb.PaymentBatchId = @PaymentBatchId
                        GROUP BY 
                            pb.PaymentBatchId, pb.BatchNumber, pt.TypeName, pb.BatchDate,
                            pb.CropYear, pb.Status, pb.TotalAmount, pb.TotalGrowers, pb.TotalReceipts,
                            pb.CreatedAt, pb.CreatedBy, pb.ProcessedAt, pb.ProcessedBy, pb.Notes";

                    var summary = await connection.QueryFirstOrDefaultAsync<PaymentBatchSummary>(sql, 
                        new { PaymentBatchId = paymentBatchId });

                    if (summary != null)
                    {
                        // Get grower breakdown (active vs on-hold)
                        var growerBreakdownSql = @"
                            SELECT 
                                COUNT(CASE WHEN g.IsOnHold = 0 THEN 1 END) AS ActiveGrowers,
                                COUNT(CASE WHEN g.IsOnHold = 1 THEN 1 END) AS OnHoldGrowers
                            FROM Cheques c
                            INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                            WHERE c.PaymentBatchId = @PaymentBatchId AND c.Status != 'Voided'";

                        var breakdown = await connection.QueryFirstOrDefaultAsync<dynamic>(growerBreakdownSql, 
                            new { PaymentBatchId = paymentBatchId });

                        if (breakdown != null)
                        {
                            summary.ActiveGrowers = breakdown.ActiveGrowers ?? 0;
                            summary.OnHoldGrowers = breakdown.OnHoldGrowers ?? 0;
                        }
                    }

                    return summary ?? new PaymentBatchSummary { PaymentBatchId = paymentBatchId };
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting batch summary: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get batch statistics for a crop year
        /// </summary>
        public async Task<BatchStatistics> GetBatchStatisticsAsync(int cropYear)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            @CropYear AS CropYear,
                            COUNT(*) AS TotalBatches,
                            SUM(CASE WHEN Status = 'Draft' THEN 1 ELSE 0 END) AS DraftBatches,
                            SUM(CASE WHEN Status = 'Posted' OR Status = 'Finalized' THEN 1 ELSE 0 END) AS PostedBatches,
                            SUM(CASE WHEN Status = 'Voided' THEN 1 ELSE 0 END) AS VoidedBatches,
                            SUM(CASE WHEN pt.TypeCode = 'ADV1' THEN 1 ELSE 0 END) AS Advance1Batches,
                            SUM(CASE WHEN pt.TypeCode = 'ADV2' THEN 1 ELSE 0 END) AS Advance2Batches,
                            SUM(CASE WHEN pt.TypeCode = 'ADV3' THEN 1 ELSE 0 END) AS Advance3Batches,
                            SUM(CASE WHEN pt.TypeCode = 'FINAL' THEN 1 ELSE 0 END) AS FinalBatches,
                            SUM(CASE WHEN pt.TypeCode = 'SPECIAL' THEN 1 ELSE 0 END) AS SpecialBatches,
                            ISNULL(SUM(CASE WHEN Status = 'Posted' OR Status = 'Finalized' THEN pb.TotalAmount ELSE 0 END), 0) AS TotalAmountPaid,
                            (SELECT COUNT(*) FROM Cheques c 
                             INNER JOIN PaymentBatches pb2 ON c.PaymentBatchId = pb2.PaymentBatchId
                             WHERE pb2.CropYear = @CropYear AND c.Status != 'Voided') AS TotalChequesIssued
                        FROM PaymentBatches pb
                        INNER JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                        WHERE pb.CropYear = @CropYear AND pb.DeletedAt IS NULL
                        GROUP BY pt.TypeCode";

                    // Execute and aggregate results
                    var results = await connection.QueryAsync<dynamic>(sql, new { CropYear = cropYear });
                    
                    var stats = new BatchStatistics { CropYear = cropYear };
                    
                    foreach (var result in results)
                    {
                        stats.TotalBatches += result.TotalBatches ?? 0;
                        stats.DraftBatches += result.DraftBatches ?? 0;
                        stats.PostedBatches += result.PostedBatches ?? 0;
                        stats.VoidedBatches += result.VoidedBatches ?? 0;
                        stats.Advance1Batches += result.Advance1Batches ?? 0;
                        stats.Advance2Batches += result.Advance2Batches ?? 0;
                        stats.Advance3Batches += result.Advance3Batches ?? 0;
                        stats.FinalBatches += result.FinalBatches ?? 0;
                        stats.SpecialBatches += result.SpecialBatches ?? 0;
                        stats.TotalAmountPaid += result.TotalAmountPaid ?? 0;
                        stats.TotalChequesIssued = result.TotalChequesIssued ?? 0;
                    }

                    return stats;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting batch statistics for year {cropYear}: {ex.Message}", ex);
                throw;
            }
        }

        // ==============================================================
        // BATCH NUMBER GENERATION
        // ==============================================================

        /// <summary>
        /// Generate next batch number for a payment type and year
        /// Format: {TypeCode}-{Year}-{Sequence:000}
        /// Example: ADV1-2025-001, ADV2-2025-001, FINAL-2025-001
        /// </summary>
        public async Task<string> GenerateNextBatchNumberAsync(int paymentTypeId, int cropYear)
        {
            try
            {
                var paymentType = await _paymentTypeService.GetPaymentTypeByIdAsync(paymentTypeId);
                if (paymentType == null)
                {
                    throw new InvalidOperationException($"Payment type {paymentTypeId} not found");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get the highest batch number for this type and year
                    var sql = @"
                        SELECT MAX(pb.BatchNumber)
                        FROM PaymentBatches pb
                        WHERE pb.PaymentTypeId = @PaymentTypeId 
                          AND pb.CropYear = @CropYear
                          AND pb.BatchNumber LIKE @Pattern";

                    var pattern = $"{paymentType.TypeCode}-{cropYear}-%";
                    var lastBatchNumber = await connection.QueryFirstOrDefaultAsync<string>(sql, new
                    {
                        PaymentTypeId = paymentTypeId,
                        CropYear = cropYear,
                        Pattern = pattern
                    });

                    int nextSequence = 1;
                    
                    if (!string.IsNullOrEmpty(lastBatchNumber))
                    {
                        // Parse sequence from last batch number (format: ADV1-2025-001)
                        var parts = lastBatchNumber.Split('-');
                        if (parts.Length == 3 && int.TryParse(parts[2], out int lastSequence))
                        {
                            nextSequence = lastSequence + 1;
                        }
                    }

                    var newBatchNumber = $"{paymentType.TypeCode}-{cropYear}-{nextSequence:D3}";
                    Logger.Info($"Generated batch number: {newBatchNumber}");
                    return newBatchNumber;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating batch number: {ex.Message}", ex);
                throw;
            }
        }
    }
}


