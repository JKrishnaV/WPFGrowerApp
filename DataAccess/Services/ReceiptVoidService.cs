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
    public class ReceiptVoidService : BaseDatabaseService, IReceiptVoidService
    {
        public async Task<VoidReceiptResult> VoidReceiptWithCascadingAsync(string receiptNumber, string reason, string voidedBy)
        {
            var result = new VoidReceiptResult();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Analyze impact
                            var impact = await AnalyzeReceiptVoidImpactAsync(receiptNumber);
                            
                            if (impact.RequiresConfirmation)
                            {
                                result.RequiresConfirmation = true;
                                result.WarningMessage = impact.WarningMessage;
                                result.BatchNumber = impact.BatchNumber;
                                result.PaymentBatchId = impact.PaymentBatchId;
                                return result;
                            }
                            
                            // 2. Void the receipt
                            await VoidReceiptAsync(receiptNumber, reason, voidedBy, connection, transaction);
                            
                            // 3. Handle batch reversion if needed
                            if (impact.BatchReverted)
                            {
                                await RevertBatchToDraftAsync(impact.PaymentBatchId, voidedBy, connection, transaction);
                                result.BatchReverted = true;
                            }
                            
                            // 4. Clean up related records
                            await CleanupRelatedRecordsAsync(receiptNumber, connection, transaction);
                            
                            result.Success = true;
                            result.AmountVoided = impact.AmountVoided;
                            result.AffectedGrowers = impact.AffectedGrowers;
                            
                            transaction.Commit();
                            Logger.Info($"Successfully voided receipt {receiptNumber}");
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
                Logger.Error($"Error voiding receipt {receiptNumber}: {ex.Message}", ex);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }

        public async Task<VoidReceiptResult> AnalyzeReceiptVoidImpactAsync(string receiptNumber)
        {
            var result = new VoidReceiptResult();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get receipt details
                    var receiptSql = @"
                        SELECT r.*, pb.Status AS BatchStatus, pb.BatchNumber, pb.PaymentBatchId
                        FROM Receipts r
                        LEFT JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        LEFT JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        WHERE r.ReceiptNumber = @ReceiptNumber";

                    var receiptInfo = await connection.QueryFirstOrDefaultAsync(receiptSql, new { ReceiptNumber = receiptNumber });
                    
                    if (receiptInfo == null)
                    {
                        result.ErrorMessage = "Receipt not found";
                        return result;
                    }
                    
                    result.AmountVoided = receiptInfo.TotalAmount ?? 0;
                    
                    // Check if receipt is in a posted or finalized batch
                    if (receiptInfo.BatchStatus == "Posted" || receiptInfo.BatchStatus == "Finalized")
                    {
                        result.RequiresConfirmation = true;
                        result.BatchNumber = receiptInfo.BatchNumber;
                        result.PaymentBatchId = receiptInfo.PaymentBatchId;
                        result.BatchReverted = true;
                        
                        result.WarningMessage = $"This receipt is in a {receiptInfo.BatchStatus} batch: {receiptInfo.BatchNumber}. " +
                                              $"Voiding will revert the batch to Draft status. Continue?";
                    }
                    
                    // Get affected growers
                    var growersSql = @"
                        SELECT DISTINCT g.FullName
                        FROM Receipts r
                        INNER JOIN Growers g ON r.GrowerId = g.GrowerId
                        WHERE r.ReceiptNumber = @ReceiptNumber";

                    result.AffectedGrowers = (await connection.QueryAsync<string>(growersSql, new { ReceiptNumber = receiptNumber })).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing receipt void impact for {receiptNumber}: {ex.Message}", ex);
                result.ErrorMessage = ex.Message;
            }
            
            return result;
        }

        public async Task<bool> CanVoidReceiptAsync(string receiptNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT COUNT(*)
                        FROM Receipts r
                        LEFT JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        LEFT JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        WHERE r.ReceiptNumber = @ReceiptNumber
                          AND (pb.Status IS NULL OR pb.Status IN ('Draft', 'Approved', 'Posted'))";

                    var count = await connection.ExecuteScalarAsync<int>(sql, new { ReceiptNumber = receiptNumber });
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking if receipt {receiptNumber} can be voided: {ex.Message}", ex);
                return false;
            }
        }

        private async Task VoidReceiptAsync(string receiptNumber, string reason, string voidedBy, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                UPDATE Receipts
                SET Status = 'Voided',
                    ModifiedAt = GETDATE(),
                    ModifiedBy = @VoidedBy,
                    Notes = ISNULL(Notes, '') + CHAR(13) + CHAR(10) + 
                           'VOIDED: ' + @Reason + ' by ' + @VoidedBy + 
                           ' on ' + CONVERT(NVARCHAR, GETDATE(), 120)
                WHERE ReceiptNumber = @ReceiptNumber";

            await connection.ExecuteAsync(sql, new { ReceiptNumber = receiptNumber, Reason = reason, VoidedBy = voidedBy }, transaction);
        }

        private async Task RevertBatchToDraftAsync(int paymentBatchId, string revertedBy, SqlConnection connection, SqlTransaction transaction)
        {
            // Revert batch to Draft status
            var revertBatchSql = @"
                UPDATE PaymentBatches
                SET Status = 'Draft',
                    ModifiedAt = GETDATE(),
                    ModifiedBy = @RevertedBy,
                    Notes = ISNULL(Notes, '') + CHAR(13) + CHAR(10) + 
                           'REVERTED TO DRAFT: Due to receipt void on ' + CONVERT(NVARCHAR, GETDATE(), 120)
                WHERE PaymentBatchId = @PaymentBatchId";

            await connection.ExecuteAsync(revertBatchSql, new { PaymentBatchId = paymentBatchId, RevertedBy = revertedBy }, transaction);

            // Revert receipt allocations to Pending
            var revertAllocationsSql = @"
                UPDATE ReceiptPaymentAllocations
                SET Status = 'Pending',
                    ModifiedAt = GETDATE(),
                    ModifiedBy = @RevertedBy
                WHERE PaymentBatchId = @PaymentBatchId";

            await connection.ExecuteAsync(revertAllocationsSql, new { PaymentBatchId = paymentBatchId, RevertedBy = revertedBy }, transaction);

            // Soft delete GrowerAccounts
            var voidGrowerAccountsSql = @"
                UPDATE GrowerAccounts
                SET DeletedAt = GETDATE(),
                    DeletedBy = @RevertedBy,
                    ModifiedAt = GETDATE(),
                    ModifiedBy = @RevertedBy
                WHERE PaymentBatchId = @PaymentBatchId
                  AND DeletedAt IS NULL";

            await connection.ExecuteAsync(voidGrowerAccountsSql, new { PaymentBatchId = paymentBatchId, RevertedBy = revertedBy }, transaction);

            // Soft delete PriceScheduleLocks
            var removePriceScheduleLocksSql = @"
                UPDATE PriceScheduleLocks
                SET DeletedAt = GETDATE(),
                    DeletedBy = @RevertedBy,
                    ModifiedAt = GETDATE(),
                    ModifiedBy = @RevertedBy
                WHERE PaymentBatchId = @PaymentBatchId
                  AND DeletedAt IS NULL";

            await connection.ExecuteAsync(removePriceScheduleLocksSql, new { PaymentBatchId = paymentBatchId, RevertedBy = revertedBy }, transaction);
        }

        private async Task CleanupRelatedRecordsAsync(string receiptNumber, SqlConnection connection, SqlTransaction transaction)
        {
            // Void any cheques related to this receipt
            var voidChequesSql = @"
                UPDATE c
                SET Status = 'Voided',
                    ModifiedAt = GETDATE(),
                    ModifiedBy = 'SYSTEM'
                FROM Cheques c
                INNER JOIN Receipts r ON c.GrowerId = r.GrowerId
                WHERE r.ReceiptNumber = @ReceiptNumber
                  AND c.Status = 'Generated'";

            await connection.ExecuteAsync(voidChequesSql, new { ReceiptNumber = receiptNumber }, transaction);

            // Update receipt payment allocations
            var updateAllocationsSql = @"
                UPDATE rpa
                SET Status = 'Voided',
                    ModifiedAt = GETDATE(),
                    ModifiedBy = 'SYSTEM'
                FROM ReceiptPaymentAllocations rpa
                INNER JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                WHERE r.ReceiptNumber = @ReceiptNumber";

            await connection.ExecuteAsync(updateAllocationsSql, new { ReceiptNumber = receiptNumber }, transaction);
        }
    }
}
