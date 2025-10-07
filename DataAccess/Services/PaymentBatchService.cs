using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class PaymentBatchService : BaseDatabaseService, IPaymentBatchService
    {
        public async Task<PaymentBatch> CreatePaymentBatchAsync(int paymentTypeId, DateTime batchDate, string notes = null)
        {
            try
            {
                // Generate batch number (e.g., "ADV1-20251006-001")
                var paymentTypeName = await GetPaymentTypeNameAsync(paymentTypeId);
                var batchNumber = $"{paymentTypeName}-{batchDate:yyyyMMdd}-{DateTime.Now:HHmmss}";

                string sql = @"
                    INSERT INTO PaymentBatches (
                        BatchNumber,
                        PaymentTypeId,
                        BatchDate,
                        Status,
                        Notes,
                        CreatedAt,
                        CreatedBy
                    )
                    OUTPUT INSERTED.PaymentBatchId, INSERTED.BatchNumber, INSERTED.PaymentTypeId, 
                           INSERTED.BatchDate, INSERTED.TotalAmount, INSERTED.TotalGrowers, 
                           INSERTED.TotalReceipts, INSERTED.Status, INSERTED.Notes, 
                           INSERTED.ProcessedAt, INSERTED.ProcessedBy, INSERTED.CreatedAt, INSERTED.CreatedBy
                    VALUES (
                        @BatchNumber,
                        @PaymentTypeId,
                        @BatchDate,
                        'Pending',
                        @Notes,
                        GETDATE(),
                        SYSTEM_USER
                    )";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var batch = await connection.QueryFirstOrDefaultAsync<PaymentBatch>(sql, new
                    {
                        BatchNumber = batchNumber,
                        PaymentTypeId = paymentTypeId,
                        BatchDate = batchDate,
                        Notes = notes
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
                        Status = 'Completed'
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

        public async Task<bool> MarkBatchAsProcessedAsync(int paymentBatchId, string processedBy)
        {
            try
            {
                string sql = @"
                    UPDATE PaymentBatches
                    SET Status = 'Processed',
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

        private async Task<string> GetPaymentTypeNameAsync(int paymentTypeId)
        {
            try
            {
                string sql = "SELECT PaymentTypeName FROM PaymentTypes WHERE PaymentTypeId = @PaymentTypeId";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var typeName = await connection.QueryFirstOrDefaultAsync<string>(sql, new { PaymentTypeId = paymentTypeId });
                    return typeName ?? $"PMT{paymentTypeId}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment type name for {paymentTypeId}", ex);
                return $"PMT{paymentTypeId}";
            }
        }
    }
}
