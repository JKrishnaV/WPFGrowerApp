using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class PaymentBatchService : BaseDatabaseService, IPaymentBatchService
    {
        public async Task<PaymentBatch> CreatePaymentBatchAsync(int paymentTypeId, DateTime batchDate, int cropYear, string notes = null)
        {
            try
            {
                // Generate batch number (e.g., "ADV1-20251006-001") - Max 20 chars
                // Use paymentTypeId directly for cleaner, non-hardcoded approach
                var typeAbbrev = $"ADV{paymentTypeId}"; // For paymentTypeId 1,2,3 = ADV1,ADV2,ADV3
                if (paymentTypeId >= 99) typeAbbrev = "FINAL"; // Final payment typically has high ID
                
                var batchNumber = $"{typeAbbrev}-{batchDate:yyyyMMdd}-{DateTime.Now:HHmmss}";
                
                // Ensure batch number doesn't exceed 20 characters
                if (batchNumber.Length > 20)
                {
                    batchNumber = $"{typeAbbrev}-{batchDate:yyMMdd}-{DateTime.Now:HHmm}";
                }

                string sql = @"
                    INSERT INTO PaymentBatches (
                        BatchNumber,
                        PaymentTypeId,
                        BatchDate,
                        CropYear,
                        Status,
                        Notes,
                        CreatedAt,
                        CreatedBy
                    )
                    OUTPUT INSERTED.PaymentBatchId, INSERTED.BatchNumber, INSERTED.PaymentTypeId, 
                           INSERTED.BatchDate, INSERTED.CropYear, INSERTED.TotalAmount, INSERTED.TotalGrowers, 
                           INSERTED.TotalReceipts, INSERTED.Status, INSERTED.Notes, 
                           INSERTED.ProcessedAt, INSERTED.ProcessedBy, INSERTED.CreatedAt, INSERTED.CreatedBy
                    VALUES (
                        @BatchNumber,
                        @PaymentTypeId,
                        @BatchDate,
                        @CropYear,
                        'Draft',
                        @Notes,
                        GETDATE(),
                        @CreatedBy
                    )";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var createdBy = App.CurrentUser?.Username ?? "SYSTEM";
                    var batch = await connection.QueryFirstOrDefaultAsync<PaymentBatch>(sql, new
                    {
                        BatchNumber = batchNumber,
                        PaymentTypeId = paymentTypeId,
                        BatchDate = batchDate,
                        CropYear = cropYear,
                        Notes = notes,
                        CreatedBy = createdBy
                    });

                    Logger.Info($"Created payment batch: {batch.BatchNumber} (ID: {batch.PaymentBatchId})");
                    return batch;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment batch for PaymentTypeId {paymentTypeId}", ex);
                throw;
            }
        }

        // Transaction-aware overload for CreatePaymentBatchAsync
        public async Task<PaymentBatch> CreatePaymentBatchAsync(
            int paymentTypeId, 
            DateTime batchDate, 
            int cropYear,
            string notes,
            SqlConnection connection, 
            SqlTransaction transaction)
        {
            try
            {
                // Generate batch number
                var typeAbbrev = $"ADV{paymentTypeId}";
                if (paymentTypeId >= 99) typeAbbrev = "FINAL";

                var batchNumber = $"{typeAbbrev}-{batchDate:yyyyMMdd}-{DateTime.Now:HHmmss}";

                if (batchNumber.Length > 20)
                {
                    batchNumber = $"{typeAbbrev}-{batchDate:yyMMdd}-{DateTime.Now:HHmm}";
                }

                string sql = @"
                    INSERT INTO PaymentBatches (
                        BatchNumber, PaymentTypeId, BatchDate, CropYear, Status, Notes, CreatedAt, CreatedBy
                    )
                    OUTPUT INSERTED.PaymentBatchId, INSERTED.BatchNumber, INSERTED.PaymentTypeId, 
                           INSERTED.BatchDate, INSERTED.CropYear, INSERTED.TotalAmount, INSERTED.TotalGrowers, 
                           INSERTED.TotalReceipts, INSERTED.Status, INSERTED.Notes, 
                           INSERTED.ProcessedAt, INSERTED.ProcessedBy, INSERTED.CreatedAt, INSERTED.CreatedBy
                    VALUES (
                        @BatchNumber, @PaymentTypeId, @BatchDate, @CropYear, 'Draft', @Notes, GETDATE(), @CreatedBy
                    )";

                var createdBy = App.CurrentUser?.Username ?? "SYSTEM";
                var batch = await connection.QueryFirstOrDefaultAsync<PaymentBatch>(sql, new
                {
                    BatchNumber = batchNumber,
                    PaymentTypeId = paymentTypeId,
                    BatchDate = batchDate,
                    CropYear = cropYear,
                    Notes = notes,
                    CreatedBy = createdBy
                }, transaction: transaction);

                Logger.Info($"Created payment batch (in transaction): {batch.BatchNumber} (ID: {batch.PaymentBatchId})");
                return batch;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment batch in transaction for type {paymentTypeId}", ex);
                throw;
            }
        }

        public async Task<PaymentBatch> GetPaymentBatchByIdAsync(int paymentBatchId)
        {
            try
            {
                string sql = @"
                    SELECT 
                        PaymentBatchId,
                        BatchNumber,
                        PaymentTypeId,
                        BatchDate,
                        TotalAmount,
                        TotalGrowers,
                        TotalReceipts,
                        Status,
                        Notes,
                        ProcessedAt,
                        ProcessedBy,
                        CreatedAt,
                        CreatedBy
                    FROM PaymentBatches
                    WHERE PaymentBatchId = @PaymentBatchId";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    return await connection.QueryFirstOrDefaultAsync<PaymentBatch>(sql, new { PaymentBatchId = paymentBatchId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment batch {paymentBatchId}", ex);
                return null;
            }
        }

        public async Task<bool> UpdatePaymentBatchTotalsAsync(int paymentBatchId, int totalGrowers, int totalReceipts, decimal totalAmount)
        {
            try
            {
                string sql = @"
                    UPDATE PaymentBatches
                    SET TotalGrowers = @TotalGrowers,
                        TotalReceipts = @TotalReceipts,
                        TotalAmount = @TotalAmount,
                        Status = 'Posted'
                    WHERE PaymentBatchId = @PaymentBatchId";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        TotalGrowers = totalGrowers,
                        TotalReceipts = totalReceipts,
                        TotalAmount = totalAmount
                    });

                    Logger.Info($"Updated payment batch {paymentBatchId}: Growers={totalGrowers}, Receipts={totalReceipts}, Amount=${totalAmount:N2}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating payment batch {paymentBatchId} totals", ex);
                return false;
            }
        }

        public async Task<bool> UpdatePaymentBatchTotalsOnlyAsync(int paymentBatchId, int totalGrowers, int totalReceipts, decimal totalAmount)
        {
            try
            {
                string sql = @"
                    UPDATE PaymentBatches
                    SET TotalGrowers = @TotalGrowers,
                        TotalReceipts = @TotalReceipts,
                        TotalAmount = @TotalAmount
                    WHERE PaymentBatchId = @PaymentBatchId";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        TotalGrowers = totalGrowers,
                        TotalReceipts = totalReceipts,
                        TotalAmount = totalAmount
                    });

                    Logger.Info($"Updated payment batch totals only {paymentBatchId}: Growers={totalGrowers}, Receipts={totalReceipts}, Amount=${totalAmount:N2}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating payment batch totals only {paymentBatchId}", ex);
                return false;
            }
        }

        // Transaction-aware overload for UpdatePaymentBatchTotalsOnlyAsync
        public async Task<bool> UpdatePaymentBatchTotalsOnlyAsync(
            int paymentBatchId, 
            int totalGrowers, 
            int totalReceipts, 
            decimal totalAmount,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            try
            {
                string sql = @"
                    UPDATE PaymentBatches
                    SET TotalGrowers = @TotalGrowers,
                        TotalReceipts = @TotalReceipts,
                        TotalAmount = @TotalAmount
                    WHERE PaymentBatchId = @PaymentBatchId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    PaymentBatchId = paymentBatchId,
                    TotalGrowers = totalGrowers,
                    TotalReceipts = totalReceipts,
                    TotalAmount = totalAmount
                }, transaction: transaction);

                Logger.Info($"Updated payment batch totals (in transaction) {paymentBatchId}: Growers={totalGrowers}, Receipts={totalReceipts}, Amount=${totalAmount:N2}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating payment batch totals in transaction {paymentBatchId}", ex);
                throw;
            }
        }

        public async Task<bool> ApproveBatchAsync(int paymentBatchId, string approvedBy)
        {
            try
            {
                string sql = @"
                    UPDATE PaymentBatches
                    SET Status = 'Posted',
                        ProcessedAt = GETDATE(),
                        ProcessedBy = @ApprovedBy
                    WHERE PaymentBatchId = @PaymentBatchId AND Status = 'Draft'";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        ApprovedBy = approvedBy
                    });

                    Logger.Info($"Approved payment batch {paymentBatchId} by {approvedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error approving payment batch {paymentBatchId}", ex);
                return false;
            }
        }

        public async Task<bool> ProcessPaymentsAsync(int paymentBatchId, string processedBy)
        {
            try
            {
                string sql = @"
                    UPDATE PaymentBatches
                    SET Status = 'Finalized',
                        ProcessedAt = GETDATE(),
                        ProcessedBy = @ProcessedBy
                    WHERE PaymentBatchId = @PaymentBatchId AND Status = 'Posted'";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        ProcessedBy = processedBy
                    });

                    Logger.Info($"Processed payments for batch {paymentBatchId} by {processedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing payments for batch {paymentBatchId}", ex);
                return false;
            }
        }

        public async Task<bool> MarkBatchAsProcessedAsync(int paymentBatchId, string processedBy)
        {
            try
            {
                string sql = @"
                    UPDATE PaymentBatches
                    SET Status = 'Finalized',
                        ProcessedAt = GETDATE(),
                        ProcessedBy = @ProcessedBy
                    WHERE PaymentBatchId = @PaymentBatchId";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        ProcessedBy = processedBy
                    });

                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking payment batch {paymentBatchId} as processed", ex);
                return false;
            }
        }

    }
}
