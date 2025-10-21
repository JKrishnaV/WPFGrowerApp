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
    public class PaymentDistributionService : BaseDatabaseService, IPaymentDistributionService
    {
        public async Task<IEnumerable<PaymentBatch>> GetAvailableBatchesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT pb.*, pt.TypeName AS PaymentTypeName
                        FROM PaymentBatches pb
                        LEFT JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                        WHERE pb.Status IN ('Approved', 'Posted')
                        AND (
                            pb.PaymentBatchId NOT IN (
                                SELECT DISTINCT PaymentBatchId 
                                FROM PaymentDistributionItems 
                                WHERE PaymentBatchId IS NOT NULL
                            )
                            OR pb.PaymentBatchId IN (
                                SELECT DISTINCT pdi.PaymentBatchId
                                FROM PaymentDistributionItems pdi
                                WHERE pdi.PaymentBatchId IS NOT NULL
                                AND pdi.Status = 'Voided'
                            )
                        )
                        ORDER BY pb.CreatedAt DESC";
                    
                    return await connection.QueryAsync<PaymentBatch>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting available batches: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<PaymentDistribution> CreateDistributionAsync(PaymentDistribution distribution)
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
                            // Check for duplicate distributions for the same batch
                            var batchIds = distribution.Items?.Select(i => i.PaymentBatchId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
                            if (batchIds?.Any() == true)
                            {
                                foreach (var batchId in batchIds)
                                {
                                    var hasExisting = await HasExistingDistributionsAsync(batchId);
                                    if (hasExisting)
                                    {
                                        throw new InvalidOperationException($"Batch {batchId} already has payment distributions. Cannot create duplicate distributions.");
                                    }
                                }
                            }

                            // Generate distribution number
                            distribution.DistributionNumber = $"DIST-{DateTime.Now:yyyyMMdd-HHmmss}";
                            distribution.DistributionDate = DateTime.Today;
                            distribution.Status = "Draft";
                            distribution.CreatedAt = DateTime.Now;
                            distribution.CreatedBy = App.CurrentUser?.Username ?? "SYSTEM";

                            var sql = @"
                                INSERT INTO PaymentDistributions (
                                    DistributionNumber, DistributionDate, DistributionType, PaymentMethod,
                                    TotalAmount, TotalGrowers, TotalBatches, Status,
                                    CreatedAt, CreatedBy
                                )
                                VALUES (
                                    @DistributionNumber, @DistributionDate, @DistributionType, @PaymentMethod,
                                    @TotalAmount, @TotalGrowers, @TotalBatches, @Status,
                                    @CreatedAt, @CreatedBy
                                );
                                SELECT CAST(SCOPE_IDENTITY() as int)";

                            var distributionId = await connection.ExecuteScalarAsync<int>(sql, distribution, transaction);
                            distribution.DistributionId = distributionId;

                            // Create distribution items
                            if (distribution.Items?.Any() == true)
                            {
                                await CreateDistributionItemsAsync(distribution.Items, distributionId, connection, transaction);
                            }

                            // Update batch status to "Finalized" after successful distribution creation
                            if (batchIds?.Any() == true)
                            {
                                foreach (var batchId in batchIds)
                                {
                                    await UpdateBatchStatusAsync(batchId, "Finalized", connection, transaction);
                                }
                            }

                            transaction.Commit();
                            Logger.Info($"Created payment distribution {distribution.DistributionNumber} and updated batch statuses");
                            return distribution;
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
                Logger.Error($"Error creating payment distribution: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> GeneratePaymentsAsync(int distributionId, string generatedBy)
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
                            // Update distribution status
                            var updateSql = @"
                                UPDATE PaymentDistributions
                                SET Status = 'Generated',
                                    ProcessedAt = GETDATE(),
                                    ProcessedBy = @GeneratedBy
                                WHERE PaymentDistributionId = @DistributionId";

                            await connection.ExecuteAsync(updateSql, new { DistributionId = distributionId, GeneratedBy = generatedBy }, transaction);

                            // Generate cheques or electronic payments based on distribution items
                            var itemsSql = @"
                                SELECT pdi.*, g.FullName AS GrowerName, g.GrowerNumber
                                FROM PaymentDistributionItems pdi
                                LEFT JOIN Growers g ON pdi.GrowerId = g.GrowerId
                                WHERE pdi.PaymentDistributionId = @DistributionId";

                            var items = await connection.QueryAsync<PaymentDistributionItem>(itemsSql, new { DistributionId = distributionId }, transaction);

                            foreach (var item in items)
                            {
                                if (item.PaymentMethod == "Cheque")
                                {
                                    var chequeId = await GenerateChequeAsync(item, connection, transaction);
                                    Logger.Info($"Generated cheque {chequeId} for grower {item.GrowerId}");
                                }
                                else if (item.PaymentMethod == "Electronic")
                                {
                                    await GenerateElectronicPaymentAsync(item, connection, transaction);
                                }
                            }

                            transaction.Commit();
                            Logger.Info($"Generated payments for distribution {distributionId}");
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
                Logger.Error($"Error generating payments for distribution {distributionId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<IEnumerable<PaymentDistribution>> GetDistributionsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT * FROM PaymentDistributions
                        ORDER BY CreatedAt DESC";
                    
                    return await connection.QueryAsync<PaymentDistribution>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment distributions: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<PaymentDistribution> GetDistributionByIdAsync(int distributionId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT * FROM PaymentDistributions
                        WHERE PaymentDistributionId = @DistributionId";
                    
                    var distribution = await connection.QueryFirstOrDefaultAsync<PaymentDistribution>(sql, new { DistributionId = distributionId });
                    
                    if (distribution != null)
                    {
                        // Load items
                        var itemsSql = @"
                            SELECT * FROM PaymentDistributionItems
                            WHERE PaymentDistributionId = @DistributionId";
                        
                        distribution.Items = (await connection.QueryAsync<PaymentDistributionItem>(itemsSql, new { DistributionId = distributionId })).ToList();
                    }
                    
                    return distribution;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment distribution {distributionId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> ProcessDistributionAsync(int distributionId, string processedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PaymentDistributions
                        SET Status = 'Processed',
                            ProcessedAt = GETDATE(),
                            ProcessedBy = @ProcessedBy
                        WHERE PaymentDistributionId = @DistributionId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new { DistributionId = distributionId, ProcessedBy = processedBy });
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing distribution {distributionId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> VoidDistributionAsync(int distributionId, string reason, string voidedBy)
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
                            // Void distribution
                            var voidDistributionSql = @"
                                UPDATE PaymentDistributions
                                SET Status = 'Voided',
                                    ModifiedAt = GETDATE(),
                                    ModifiedBy = @VoidedBy
                                WHERE PaymentDistributionId = @DistributionId";

                            await connection.ExecuteAsync(voidDistributionSql, new { DistributionId = distributionId, VoidedBy = voidedBy }, transaction);

                            // Void distribution items
                            var voidItemsSql = @"
                                UPDATE PaymentDistributionItems
                                SET Status = 'Voided',
                                    ModifiedAt = GETDATE(),
                                    ModifiedBy = @VoidedBy
                                WHERE PaymentDistributionId = @DistributionId";

                            await connection.ExecuteAsync(voidItemsSql, new { DistributionId = distributionId, VoidedBy = voidedBy }, transaction);

                            transaction.Commit();
                            Logger.Info($"Voided distribution {distributionId}");
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
                Logger.Error($"Error voiding distribution {distributionId}: {ex.Message}", ex);
                return false;
            }
        }

        private async Task CreateDistributionItemsAsync(IEnumerable<PaymentDistributionItem> items, int distributionId, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                INSERT INTO PaymentDistributionItems (
                    PaymentDistributionId, GrowerId, PaymentBatchId, ReceiptId,
                    Amount, PaymentMethod, Status, CreatedAt, CreatedBy
                )
                VALUES (
                    @DistributionId, @GrowerId, @PaymentBatchId, @ReceiptId,
                    @Amount, @PaymentMethod, @Status, @CreatedAt, @CreatedBy
                )";

            foreach (var item in items)
            {
                item.DistributionId = distributionId;
                item.Status = "Pending";
                item.CreatedAt = DateTime.Now;
                item.CreatedBy = App.CurrentUser?.Username ?? "SYSTEM";
                // ReceiptId is now set in the ViewModel when creating the items
            }

            await connection.ExecuteAsync(sql, items, transaction);
        }

        private async Task<int> GenerateChequeAsync(PaymentDistributionItem item, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                INSERT INTO Cheques (
                    ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId, 
                    ChequeAmount, ChequeDate, Status, CreatedAt, CreatedBy
                )
                OUTPUT INSERTED.ChequeId
                VALUES (
                    @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId,
                    @ChequeAmount, @ChequeDate, @Status, @CreatedAt, @CreatedBy
                )";

            // Generate unique cheque number with timestamp for uniqueness
            var timestamp = DateTime.Now.ToString("HHmmssfff"); // Hours, minutes, seconds, milliseconds
            var chequeNumber = $"CHQ-{DateTime.Now:yyyyMMdd}-{timestamp}-{item.GrowerId}";
            
            var chequeId = await connection.ExecuteScalarAsync<int>(sql, new
            {
                ChequeSeriesId = 2, // PAY series for grower payments
                ChequeNumber = chequeNumber,
                FiscalYear = DateTime.Now.Year,
                GrowerId = item.GrowerId,
                PaymentBatchId = item.PaymentBatchId,
                ChequeAmount = item.Amount,
                ChequeDate = DateTime.Now,
                Status = "Generated",
                CreatedAt = DateTime.Now,
                CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
            }, transaction);

            // Create advance deductions for outstanding advances
            await CreateAdvanceDeductionsAsync(item.GrowerId, chequeId, connection, transaction);

            return chequeId;
        }

        /// <summary>
        /// Create advance deductions for outstanding advances when generating a cheque
        /// </summary>
        private async Task CreateAdvanceDeductionsAsync(int growerId, int chequeId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Get all outstanding advances for the grower
                var outstandingAdvancesSql = @"
                    SELECT AdvanceChequeId, AdvanceAmount 
                    FROM AdvanceCheques 
                    WHERE GrowerId = @GrowerId 
                    AND Status = 'Printed'
                    AND DeductedByChequeId IS NULL";

                var outstandingAdvances = await connection.QueryAsync<(int AdvanceChequeId, decimal AdvanceAmount)>(outstandingAdvancesSql, new { GrowerId = growerId }, transaction);

                foreach (var advance in outstandingAdvances)
                {
                    // Create deduction record
                    var deductionSql = @"
                        INSERT INTO AdvanceDeductions (
                            AdvanceChequeId, ChequeId, PaymentBatchId, DeductionAmount, 
                            DeductionDate, CreatedBy, CreatedAt
                        )
                        VALUES (
                            @AdvanceChequeId, @ChequeId, @PaymentBatchId, @DeductionAmount,
                            @DeductionDate, @CreatedBy, @CreatedAt
                        )";

                    await connection.ExecuteAsync(deductionSql, new
                    {
                        AdvanceChequeId = advance.AdvanceChequeId,
                        ChequeId = chequeId,
                        PaymentBatchId = 0, // Will be updated by the calling method
                        DeductionAmount = advance.AdvanceAmount,
                        DeductionDate = DateTime.Now,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM",
                        CreatedAt = DateTime.Now
                    }, transaction);

                    // Update the advance cheque record
                    var updateAdvanceSql = @"
                        UPDATE AdvanceCheques 
                        SET DeductedByChequeId = @ChequeId,
                            DeductedAt = @DeductedAt,
                            DeductedBy = @DeductedBy
                        WHERE AdvanceChequeId = @AdvanceChequeId";

                    await connection.ExecuteAsync(updateAdvanceSql, new
                    {
                        ChequeId = chequeId,
                        AdvanceChequeId = advance.AdvanceChequeId,
                        DeductedAt = DateTime.Now,
                        DeductedBy = App.CurrentUser?.Username ?? "SYSTEM"
                    }, transaction);

                    Logger.Info($"Created advance deduction for AdvanceChequeId {advance.AdvanceChequeId}, Amount: {advance.AdvanceAmount:C}, ChequeId: {chequeId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating advance deductions for grower {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get complete audit trail for a cheque including advance deductions
        /// </summary>
        public async Task<ChequeAuditTrail> GetChequeAuditTrailAsync(string chequeNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            c.ChequeId,
                            c.ChequeNumber,
                            c.ChequeAmount,
                            c.ChequeDate,
                            c.Status AS ChequeStatus,
                            g.FullName AS GrowerName,
                            g.GrowerNumber,
                            
                            -- Distribution Information
                            pdi.Amount AS DistributionAmount,
                            pd.DistributionNumber,
                            pd.DistributionDate,
                            
                            -- Advance Deduction Information
                            ad.DeductionId,
                            ad.DeductionAmount,
                            ad.DeductionDate,
                            ac.AdvanceChequeId,
                            ac.AdvanceAmount,
                            ac.AdvanceDate,
                            ac.Status AS AdvanceStatus
                            
                        FROM Cheques c
                        INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                        LEFT JOIN PaymentDistributionItems pdi ON c.GrowerId = pdi.GrowerId AND c.PaymentBatchId = pdi.PaymentBatchId
                        LEFT JOIN PaymentDistributions pd ON pdi.PaymentDistributionId = pd.PaymentDistributionId
                        LEFT JOIN AdvanceDeductions ad ON c.ChequeId = ad.ChequeId
                        LEFT JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                        WHERE c.ChequeNumber = @ChequeNumber";

                    var result = await connection.QueryAsync<ChequeAuditTrail>(sql, new { ChequeNumber = chequeNumber });
                    return result.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheque audit trail for {chequeNumber}: {ex.Message}", ex);
                throw;
            }
        }

        private async Task GenerateElectronicPaymentAsync(PaymentDistributionItem item, SqlConnection connection, SqlTransaction transaction)
        {
            // Create electronic payment record
            var sql = @"
                INSERT INTO ElectronicPayments (
                    PaymentBatchId, GrowerId, Amount, PaymentDate, Status, 
                    CreatedAt, CreatedBy, PaymentMethod, ReferenceNumber
                )
                VALUES (
                    @PaymentBatchId, @GrowerId, @Amount, @PaymentDate, @Status,
                    @CreatedAt, @CreatedBy, @PaymentMethod, @ReferenceNumber
                )";

            // Generate unique reference number for electronic payment
            var referenceNumber = $"EFT-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}-{item.GrowerId}";
            
            await connection.ExecuteAsync(sql, new
            {
                PaymentBatchId = item.PaymentBatchId,
                GrowerId = item.GrowerId,
                Amount = item.Amount,
                PaymentDate = DateTime.Now,
                Status = "Generated",
                CreatedAt = DateTime.Now,
                CreatedBy = App.CurrentUser?.Username ?? "SYSTEM",
                PaymentMethod = "Electronic Transfer",
                ReferenceNumber = referenceNumber
            }, transaction);

            Logger.Info($"Generated electronic payment for grower {item.GrowerId}, amount: {item.Amount:C}, reference: {referenceNumber}");
        }

        public async Task<bool> HasExistingDistributionsAsync(int paymentBatchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Check if there are any active (non-voided) distribution items
                    var activeItemsSql = @"
                        SELECT COUNT(*) 
                        FROM PaymentDistributionItems 
                        WHERE PaymentBatchId = @PaymentBatchId
                        AND Status != 'Voided'";
                    
                    var activeCount = await connection.QuerySingleAsync<int>(activeItemsSql, new { PaymentBatchId = paymentBatchId });
                    
                    // If there are no active items, allow regeneration
                    if (activeCount == 0)
                    {
                        return false;
                    }
                    
                    // If there are active items, check if there are any voided items that can be regenerated
                    var voidedItemsSql = @"
                        SELECT COUNT(*) 
                        FROM PaymentDistributionItems 
                        WHERE PaymentBatchId = @PaymentBatchId
                        AND Status = 'Voided'";
                    
                    var voidedCount = await connection.QuerySingleAsync<int>(voidedItemsSql, new { PaymentBatchId = paymentBatchId });
                    
                    // Allow regeneration if there are voided items (even if there are active items)
                    return voidedCount == 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking existing distributions for batch {paymentBatchId}: {ex.Message}");
                return false;
            }
        }

        private async Task UpdateBatchStatusAsync(int paymentBatchId, string newStatus, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                UPDATE PaymentBatches 
                SET Status = @Status, 
                    ModifiedAt = @ModifiedAt, 
                    ModifiedBy = @ModifiedBy
                WHERE PaymentBatchId = @PaymentBatchId";
            
            await connection.ExecuteAsync(sql, new
            {
                Status = newStatus,
                PaymentBatchId = paymentBatchId,
                ModifiedAt = DateTime.Now,
                ModifiedBy = App.CurrentUser?.Username ?? "SYSTEM"
            }, transaction);
        }

        public async Task<List<PaymentDistribution>> GetAllDistributionsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT *
                    FROM PaymentDistributions
                    ORDER BY DistributionDate DESC";

                var distributions = await connection.QueryAsync<PaymentDistribution>(sql);
                return distributions.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving all distributions: {ex.Message}", ex);
                throw;
            }
        }
    }
}
