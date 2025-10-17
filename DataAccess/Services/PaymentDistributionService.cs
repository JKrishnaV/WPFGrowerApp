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
                        AND pb.PaymentBatchId NOT IN (
                            SELECT DISTINCT PaymentBatchId 
                            FROM PaymentDistributionItems 
                            WHERE PaymentBatchId IS NOT NULL
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
                                    await GenerateChequeAsync(item, connection, transaction);
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

        private async Task GenerateChequeAsync(PaymentDistributionItem item, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                INSERT INTO Cheques (
                    ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId, 
                    ChequeAmount, ChequeDate, Status, CreatedAt, CreatedBy
                )
                VALUES (
                    @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId,
                    @ChequeAmount, @ChequeDate, @Status, @CreatedAt, @CreatedBy
                )";

            // Generate unique cheque number with timestamp for uniqueness
            var timestamp = DateTime.Now.ToString("HHmmssfff"); // Hours, minutes, seconds, milliseconds
            var chequeNumber = $"CHQ-{DateTime.Now:yyyyMMdd}-{timestamp}-{item.GrowerId}";
            
            await connection.ExecuteAsync(sql, new
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
                    
                    var sql = @"
                        SELECT COUNT(*) 
                        FROM PaymentDistributionItems 
                        WHERE PaymentBatchId = @PaymentBatchId";
                    
                    var count = await connection.QuerySingleAsync<int>(sql, new { PaymentBatchId = paymentBatchId });
                    return count > 0;
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
