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
        private readonly IGrowerAccountService _growerAccountService;
        private readonly IPriceScheduleLockService _priceScheduleLockService;

        public PaymentBatchManagementService(
            IPaymentTypeService paymentTypeService,
            IGrowerAccountService growerAccountService,
            IPriceScheduleLockService priceScheduleLockService)
        {
            _paymentTypeService = paymentTypeService;
            _growerAccountService = growerAccountService;
            _priceScheduleLockService = priceScheduleLockService;
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
        /// Approve a payment batch (Draft → Approved)
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
                            Status = 'Approved',
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
        /// Post a payment batch (Approved → Posted)
        /// Creates GrowerAccounts and PriceScheduleLocks records
        /// </summary>
        public async Task<bool> PostBatchAsync(int paymentBatchId, string postedBy)
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
                            // 1. Update PaymentBatches status
                            var updateBatchSql = @"
                                UPDATE PaymentBatches
                                SET 
                                    Status = 'Posted',
                                    ProcessedAt = @ProcessedAt,
                                    ProcessedBy = @ProcessedBy,
                                    ModifiedAt = @ModifiedAt,
                                    ModifiedBy = @ModifiedBy
                                WHERE PaymentBatchId = @PaymentBatchId AND Status = 'Approved'";

                            var batchRowsAffected = await connection.ExecuteAsync(updateBatchSql, new
                            {
                                PaymentBatchId = paymentBatchId,
                                ProcessedAt = DateTime.Now,
                                ProcessedBy = postedBy,
                                ModifiedAt = DateTime.Now,
                                ModifiedBy = postedBy
                            }, transaction);

                            if (batchRowsAffected == 0)
                            {
                                Logger.Warn($"Batch {paymentBatchId} is not in Approved status, cannot post");
                                return false;
                            }

                            // 2. Get batch details and receipt allocations
                            var batchSql = @"
                                SELECT pb.*, pt.TypeName AS PaymentTypeName
                                FROM PaymentBatches pb
                                LEFT JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                                WHERE pb.PaymentBatchId = @PaymentBatchId";

                            var batch = await connection.QueryFirstOrDefaultAsync(batchSql, new { PaymentBatchId = paymentBatchId }, transaction);

                            if (batch == null)
                            {
                                Logger.Error($"Batch {paymentBatchId} not found");
                                transaction.Rollback();
                                return false;
                            }

                            // 3. Get receipt allocations for this batch
                            // Note: We don't filter by status here because the trigger may have already updated
                            // the allocation status to 'Posted' when we updated the batch status above
                            var allocationsSql = @"
                                SELECT 
                                    rpa.*,
                                    r.GrowerId,
                                    r.ReceiptNumber,
                                    ps.PriceScheduleId
                                FROM ReceiptPaymentAllocations rpa
                                INNER JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                                LEFT JOIN PriceSchedules ps ON rpa.PriceScheduleId = ps.PriceScheduleId
                                WHERE rpa.PaymentBatchId = @PaymentBatchId
                                  AND rpa.Status IN ('Approved', 'Posted')";

                            var allocations = await connection.QueryAsync(allocationsSql, new { PaymentBatchId = paymentBatchId }, transaction);

                            // 4. Create GrowerAccounts records
                            var growerAccounts = new List<GrowerAccount>();
                            var priceScheduleLocks = new List<PriceScheduleLock>();

                            foreach (var allocation in allocations)
                            {
                                // Create GrowerAccount entry
                                var growerAccount = new GrowerAccount
                                {
                                    GrowerId = allocation.GrowerId,
                                    TransactionDate = DateTime.Now.Date,
                                    TransactionType = "Payment",
                                    Description = $"Payment - {batch.PaymentTypeName} - Receipt {allocation.ReceiptNumber}",
                                    DebitAmount = 0,
                                    CreditAmount = allocation.AmountPaid,
                                    PaymentBatchId = paymentBatchId,
                                    ReceiptId = allocation.ReceiptId,
                                    CurrencyCode = "CAD",
                                    ExchangeRate = 1.0m
                                };
                                growerAccounts.Add(growerAccount);

                                // Create PriceScheduleLock if price schedule exists
                                if (allocation.PriceScheduleId != null)
                                {
                                    var priceScheduleLock = new PriceScheduleLock
                                    {
                                        PriceScheduleId = allocation.PriceScheduleId,
                                        PaymentTypeId = batch.PaymentTypeId,
                                        PaymentBatchId = paymentBatchId
                                    };
                                    priceScheduleLocks.Add(priceScheduleLock);
                                }
                            }

                            // 5. Create GrowerAccounts records in bulk
                            if (growerAccounts.Any())
                            {
                                await _growerAccountService.CreatePaymentBatchAccountsAsync(growerAccounts, connection, transaction);
                                Logger.Info($"Created {growerAccounts.Count} grower account entries for batch {paymentBatchId}");
                            }

                            // 6. Create PriceScheduleLocks records in bulk
                            if (priceScheduleLocks.Any())
                            {
                                await _priceScheduleLockService.CreatePaymentBatchLocksAsync(priceScheduleLocks, connection, transaction);
                                Logger.Info($"Created {priceScheduleLocks.Count} price schedule locks for batch {paymentBatchId}");
                            }

                            transaction.Commit();
                            Logger.Info($"Posted payment batch {paymentBatchId} by {postedBy}");
                            return true;
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
                Logger.Error($"Error posting payment batch {paymentBatchId}", ex);
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
        /// Validate if a batch can be safely voided without breaking payment sequence integrity.
        /// Checks if any receipts in the batch have received subsequent advance payments.
        /// </summary>
        /// <param name="paymentBatchId">Batch ID to validate</param>
        /// <returns>Tuple of (CanVoid: true if safe to void, Reasons: list of conflicts if any)</returns>
        public async Task<(bool CanVoid, List<string> Reasons)> ValidateCanVoidBatchAsync(int paymentBatchId)
        {
            var reasons = new List<string>();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get the batch to check its payment type
                    var batch = await GetPaymentBatchByIdAsync(paymentBatchId);
                    if (batch == null)
                    {
                        reasons.Add("Batch not found.");
                        return (false, reasons);
                    }
                    
                    // Check if batch is already voided
                    if (batch.Status == "Voided")
                    {
                        reasons.Add("Batch is already voided.");
                        return (false, reasons);
                    }
                    
                    // Check for subsequent advance payments on ANY receipt in this batch
                    // This ensures payment sequence integrity - can't void Advance 1 if Advance 2 exists
                    var sql = @"
                        SELECT 
                            r.ReceiptNumber,
                            g.GrowerNumber,
                            g.FullName AS GrowerName,
                            rpa1.PaymentTypeId AS CurrentAdvanceNumber,
                            rpa2.PaymentTypeId AS LaterAdvanceNumber,
                            pb2.BatchNumber AS LaterBatchNumber,
                            pb2.PaymentBatchId AS LaterBatchId,
                            rpa2.Status AS LaterPaymentStatus,
                            pt2.TypeName AS LaterPaymentTypeName
                        FROM ReceiptPaymentAllocations rpa1
                        INNER JOIN Receipts r ON rpa1.ReceiptId = r.ReceiptId
                        INNER JOIN Growers g ON r.GrowerId = g.GrowerId
                        INNER JOIN ReceiptPaymentAllocations rpa2 
                            ON rpa1.ReceiptId = rpa2.ReceiptId
                        INNER JOIN PaymentBatches pb2 ON rpa2.PaymentBatchId = pb2.PaymentBatchId
                        INNER JOIN PaymentTypes pt2 ON rpa2.PaymentTypeId = pt2.PaymentTypeId
                        WHERE rpa1.PaymentBatchId = @PaymentBatchId
                          AND rpa2.PaymentTypeId > rpa1.PaymentTypeId
                          AND rpa2.Status != 'Voided'
                        ORDER BY r.ReceiptNumber, rpa2.PaymentTypeId";
                    
                    var conflicts = (await connection.QueryAsync<dynamic>(sql, 
                        new { PaymentBatchId = paymentBatchId })).ToList();
                    
                    if (conflicts.Any())
                    {
                        // Group by batch to show clear message
                        var batchGroups = conflicts.GroupBy(c => (string)c.LaterBatchNumber);
                        
                        reasons.Add($"Cannot void batch {batch.BatchNumber} - Payment sequence integrity violation:");
                        reasons.Add("");
                        reasons.Add("The following receipts have received later advance payments that depend on this batch:");
                        reasons.Add("");
                        
                        foreach (var batchGroup in batchGroups)
                        {
                            var firstInGroup = batchGroup.First();
                            reasons.Add($"  → Later Batch: {firstInGroup.LaterBatchNumber} ({firstInGroup.LaterPaymentTypeName})");
                            
                            foreach (var conflict in batchGroup.Take(5)) // Show first 5 receipts
                            {
                                reasons.Add($"     • Receipt {conflict.ReceiptNumber} - {conflict.GrowerName} (Grower {conflict.GrowerNumber})");
                            }
                            
                            if (batchGroup.Count() > 5)
                            {
                                reasons.Add($"     ... and {batchGroup.Count() - 5} more receipts");
                            }
                            reasons.Add("");
                        }
                        
                        reasons.Add("TO FIX: You must void later payment batches first (in reverse chronological order):");
                        var batchesToVoidFirst = conflicts
                            .Select(c => new { BatchId = (int)c.LaterBatchId, BatchNumber = (string)c.LaterBatchNumber, TypeName = (string)c.LaterPaymentTypeName })
                            .Distinct()
                            .OrderByDescending(b => b.BatchId)
                            .ToList();
                        
                        for (int i = 0; i < batchesToVoidFirst.Count; i++)
                        {
                            var b = batchesToVoidFirst[i];
                            reasons.Add($"  {i + 1}. Void Batch {b.BatchNumber} ({b.TypeName})");
                        }
                        reasons.Add($"  {batchesToVoidFirst.Count + 1}. Then void Batch {batch.BatchNumber}");
                        
                        return (false, reasons);
                    }
                    
                    // No conflicts found - safe to void
                    return (true, new List<string> { "Batch can be safely voided." });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating batch void for batch {paymentBatchId}: {ex.Message}", ex);
                reasons.Add($"Validation error: {ex.Message}");
                return (false, reasons);
            }
        }

        /// <summary>
        /// Void a payment batch - voids the batch, all allocations, and all cheques in a transaction.
        /// Validates payment sequence integrity before voiding to prevent breaking later advance payments.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if batch cannot be voided due to payment sequence conflicts</exception>
        public async Task<bool> VoidBatchAsync(
            int paymentBatchId,
            string reason,
            string voidedBy)
        {
            // STEP 1: VALIDATE - Check for payment sequence conflicts
            var (canVoid, validationReasons) = await ValidateCanVoidBatchAsync(paymentBatchId);
            
            if (!canVoid)
            {
                var errorMessage = string.Join("\n", validationReasons);
                Logger.Warn($"Void batch {paymentBatchId} blocked due to validation failure:\n{errorMessage}");
                throw new InvalidOperationException(errorMessage);
            }
            
            // STEP 2: PROCEED WITH VOID - Validation passed
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

                        // 3. Void all GrowerAccounts for this batch
                        var voidGrowerAccounts = @"
                            UPDATE GrowerAccounts
                            SET DeletedAt = GETDATE(),
                                DeletedBy = @VoidedBy,
                                ModifiedAt = GETDATE(),
                                ModifiedBy = @VoidedBy
                            WHERE PaymentBatchId = @PaymentBatchId
                              AND DeletedAt IS NULL";
                        
                        var growerAccountsVoided = await connection.ExecuteAsync(voidGrowerAccounts,
                            new { PaymentBatchId = paymentBatchId, VoidedBy = voidedBy },
                            transaction: transaction);

                        // 4. Remove PriceScheduleLocks for this batch
                        var removePriceScheduleLocks = @"
                            UPDATE PriceScheduleLocks
                            SET DeletedAt = GETDATE(),
                                DeletedBy = @VoidedBy,
                                ModifiedAt = GETDATE(),
                                ModifiedBy = @VoidedBy
                            WHERE PaymentBatchId = @PaymentBatchId
                              AND DeletedAt IS NULL";
                        
                        var priceScheduleLocksRemoved = await connection.ExecuteAsync(removePriceScheduleLocks,
                            new { PaymentBatchId = paymentBatchId, VoidedBy = voidedBy },
                            transaction: transaction);
                        
                        // 5. Void the batch with soft delete
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
                        Logger.Info($"Successfully voided batch {paymentBatchId} - voided {allocationsVoided} allocations, {chequesVoided} cheques, {growerAccountsVoided} grower accounts, and removed {priceScheduleLocksRemoved} price schedule locks");
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


