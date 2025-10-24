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
        private readonly ICrossBatchPaymentService _crossBatchPaymentService;

        public PaymentDistributionService(ICrossBatchPaymentService crossBatchPaymentService)
        {
            _crossBatchPaymentService = crossBatchPaymentService;
        }
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
                        WHERE pb.Status = 'Posted'
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
                            // Check for grower-specific conflicts instead of batch-level conflicts
                            if (distribution.Items?.Any() == true)
                            {
                                foreach (var item in distribution.Items)
                                {
                                    if (item.PaymentBatchId.HasValue && item.GrowerId > 0)
                                    {
                                        var hasExisting = await HasExistingDistributionsForGrowerAsync(
                                            item.PaymentBatchId.Value, 
                                            item.GrowerId, 
                                            connection, 
                                            transaction);
                                        
                                        if (hasExisting)
                                        {
                                            throw new InvalidOperationException($"Grower {item.GrowerId} already has payment distributions for batch {item.PaymentBatchId}. Cannot create duplicate distributions.");
                                        }
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
                            var batchIds = distribution.Items?.Select(i => i.PaymentBatchId).Where(id => id.HasValue).Select(id => id.Value).Distinct().ToList();
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
                item.PaymentDistributionId = distributionId;
                item.Status = "Pending";
                item.CreatedAt = DateTime.Now;
                item.CreatedBy = App.CurrentUser?.Username ?? "SYSTEM";
                // ReceiptId is now set in the ViewModel when creating the items
            }

            await connection.ExecuteAsync(sql, items, transaction);
        }

        private async Task<int> GenerateChequeAsync(PaymentDistributionItem item, SqlConnection connection, SqlTransaction transaction)
        {
            // Get source batches from the distribution
            var sourceBatches = await GetSourceBatchesFromDistributionAsync(item.PaymentDistributionId ?? 0, connection, transaction);
            
            var sql = @"
                INSERT INTO Cheques (
                    ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId, PaymentDistributionId,
                    ChequeAmount, ChequeDate, Status, IsFromDistribution, SourceBatches, CreatedAt, CreatedBy
                )
                OUTPUT INSERTED.ChequeId
                VALUES (
                    @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId, @PaymentDistributionId,
                    @ChequeAmount, @ChequeDate, @Status, @IsFromDistribution, @SourceBatches, @CreatedAt, @CreatedBy
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
                PaymentDistributionId = item.PaymentDistributionId,
                ChequeAmount = item.Amount,
                ChequeDate = DateTime.Now,
                Status = "Generated",
                IsFromDistribution = true, // Set to true for distribution cheques
                SourceBatches = sourceBatches, // Comma-separated list of batch IDs
                CreatedAt = DateTime.Now,
                CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
            }, transaction);

            // Create advance deductions for outstanding advances
            await CreateAdvanceDeductionsAsync(item.GrowerId, chequeId, item.PaymentBatchId ?? 0, connection, transaction);

            return chequeId;
        }

        /// <summary>
        /// Get source batch IDs from a payment distribution
        /// </summary>
        private async Task<string> GetSourceBatchesFromDistributionAsync(int distributionId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    SELECT DISTINCT PaymentBatchId 
                    FROM PaymentDistributionItems 
                    WHERE PaymentDistributionId = @DistributionId 
                    AND PaymentBatchId IS NOT NULL
                    ORDER BY PaymentBatchId";

                var batchIds = await connection.QueryAsync<int>(sql, new { DistributionId = distributionId }, transaction);
                
                return string.Join(",", batchIds);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting source batches for distribution {distributionId}: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Create advance deductions for outstanding advances when generating a cheque
        /// </summary>
        private async Task CreateAdvanceDeductionsAsync(int growerId, int chequeId, int paymentBatchId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Validate that PaymentBatchId is valid
                if (paymentBatchId <= 0)
                {
                    Logger.Info($"Invalid PaymentBatchId {paymentBatchId} for grower {growerId}. Skipping advance deductions.");
                    return;
                }
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
                        PaymentBatchId = paymentBatchId,
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

        /// <summary>
        /// Generate complete payment distribution in a single transaction
        /// This ensures all operations succeed or all fail together
        /// </summary>
        private async Task<int?> GetBatchIdByNumberAsync(string batchNumber, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = "SELECT PaymentBatchId FROM PaymentBatches WHERE BatchNumber = @BatchNumber";
                var result = await connection.ExecuteScalarAsync<int?>(sql, new { BatchNumber = batchNumber }, transaction);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting batch ID for batch number {batchNumber}: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<PaymentDistribution> GenerateCompletePaymentDistributionAsync(PaymentDistribution distribution, string generatedBy, List<int> selectedBatchIds = null)
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
                            // STEP 1: Validate batches are available
                            // Use the selected batch IDs passed from the ViewModel
                            var batchIds = selectedBatchIds ?? new List<int>();
                            
                            // Fallback: if no batch IDs provided, extract from distribution items
                            if (!batchIds.Any() && distribution.Items?.Any() == true)
                            {
                                batchIds = distribution.Items
                                    .Select(i => i.PaymentBatchId)
                                    .Where(id => id.HasValue)
                                    .Select(id => id.Value)
                                    .Distinct()
                                    .ToList();
                            }
                            // Check for grower-specific conflicts instead of batch-level conflicts
                            if (distribution.Items?.Any() == true)
                            {
                                foreach (var item in distribution.Items)
                                {
                                    if (item.PaymentBatchId.HasValue && item.GrowerId > 0)
                                    {
                                        var hasExisting = await HasExistingDistributionsForGrowerAsync(
                                            item.PaymentBatchId.Value, 
                                            item.GrowerId, 
                                            connection, 
                                            transaction);
                                        
                                        if (hasExisting)
                                        {
                                            throw new InvalidOperationException($"Grower {item.GrowerId} already has payment distributions for batch {item.PaymentBatchId}. Cannot create duplicate distributions.");
                                        }
                                    }
                                }
                            }

                            // STEP 2: Create PaymentDistribution
                            var distributionSql = @"
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

                            // Generate distribution number
                            distribution.DistributionNumber = $"DIST-{DateTime.Now:yyyyMMdd-HHmmss}";
                            distribution.DistributionDate = DateTime.Today;
                            distribution.Status = "Draft";
                            distribution.CreatedAt = DateTime.Now;
                            distribution.CreatedBy = App.CurrentUser?.Username ?? "SYSTEM";

                            var distributionId = await connection.ExecuteScalarAsync<int>(distributionSql, distribution, transaction);
                            distribution.DistributionId = distributionId;

                            // STEP 3: Create PaymentDistributionItems
                            foreach (var item in distribution.Items)
                            {
                                item.DistributionId = distributionId;
                                item.PaymentDistributionId = distributionId;
                                item.Status = "Pending";
                                item.CreatedAt = DateTime.Now;
                                item.CreatedBy = App.CurrentUser?.Username ?? "SYSTEM";
                            }

                            // Insert items and retrieve the generated IDs
                            var insertedItems = new List<PaymentDistributionItem>();
                            foreach (var item in distribution.Items)
                            {
                                var sqlWithOutput = @"
                                    INSERT INTO PaymentDistributionItems (
                                        PaymentDistributionId, GrowerId, PaymentBatchId, ReceiptId,
                                        Amount, PaymentMethod, Status, CreatedAt, CreatedBy
                                    )
                                    OUTPUT INSERTED.PaymentDistributionItemId
                                    VALUES (
                                        @DistributionId, @GrowerId, @PaymentBatchId, @ReceiptId,
                                        @Amount, @PaymentMethod, @Status, @CreatedAt, @CreatedBy
                                    )";

                                var generatedId = await connection.QuerySingleAsync<int>(sqlWithOutput, new
                                {
                                    DistributionId = item.DistributionId,
                                    GrowerId = item.GrowerId,
                                    PaymentBatchId = item.PaymentBatchId,
                                    ReceiptId = item.ReceiptId,
                                    Amount = item.Amount,
                                    PaymentMethod = item.PaymentMethod,
                                    Status = item.Status,
                                    CreatedAt = item.CreatedAt,
                                    CreatedBy = item.CreatedBy
                                }, transaction);

                                // Update the item with the generated ID
                                item.ItemId = generatedId;
                                insertedItems.Add(item);
                            }

                            // STEP 3.5: Create PaymentDistributionReceipts for detailed audit tracking
                            foreach (var item in insertedItems)
                            {
                                if (item.ReceiptContributions?.Any() == true)
                                {
                                    var receiptSql = @"
                                        INSERT INTO PaymentDistributionReceipts (
                                            PaymentDistributionItemId, ReceiptId, PaymentBatchId, Amount,
                                            BatchNumber, ReceiptDate, CreatedAt, CreatedBy
                                        )
                                        VALUES (
                                            @PaymentDistributionItemId, @ReceiptId, @PaymentBatchId, @Amount,
                                            @BatchNumber, @ReceiptDate, @CreatedAt, @CreatedBy
                                        )";

                                    foreach (var contribution in item.ReceiptContributions)
                                    {
                                        await connection.ExecuteAsync(receiptSql, new
                                        {
                                            PaymentDistributionItemId = item.ItemId,
                                            ReceiptId = contribution.ReceiptId,
                                            PaymentBatchId = contribution.PaymentBatchId,
                                            Amount = contribution.Amount,
                                            BatchNumber = contribution.BatchNumber,
                                            ReceiptDate = contribution.ReceiptDate,
                                            CreatedAt = DateTime.Now,
                                            CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                                        }, transaction);
                                    }
                                }
                            }

                            // STEP 4: Generate Consolidated Cheques and Advance Deductions
                            foreach (var item in distribution.Items)
                            {
                                if (item.PaymentMethod == "Cheque")
                                {
                                    // Note: Consolidated payment functionality removed - consolidated payments replaced by payment distributions
                                    // All payments are now handled as regular payments through the payment distribution system
                                    
                                    // Generate regular cheque for single batch
                                    var chequeSql = @"
                                            INSERT INTO Cheques (
                                                ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId, PaymentDistributionId,
                                                ChequeAmount, ChequeDate, Status, CreatedAt, CreatedBy
                                            )
                                            OUTPUT INSERTED.ChequeId
                                            VALUES (
                                                @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId, @PaymentDistributionId,
                                                @ChequeAmount, @ChequeDate, @Status, @CreatedAt, @CreatedBy
                                            )";

                                    var timestamp = DateTime.Now.ToString("HHmmssfff");
                                    var chequeNumber = $"CHQ-{DateTime.Now:yyyyMMdd}-{timestamp}-{item.GrowerId}";
                                    
                                    var primaryBatchId = item.PaymentBatchId;
                                    
                                    var chequeId = await connection.ExecuteScalarAsync<int>(chequeSql, new
                                    {
                                        ChequeSeriesId = 2,
                                        ChequeNumber = chequeNumber,
                                        FiscalYear = DateTime.Now.Year,
                                        GrowerId = item.GrowerId,
                                        PaymentBatchId = primaryBatchId,
                                        PaymentDistributionId = distributionId,
                                        ChequeAmount = item.Amount,
                                        ChequeDate = DateTime.Now,
                                        Status = "Generated",
                                        CreatedAt = DateTime.Now,
                                        CreatedBy = generatedBy
                                    }, transaction);

                                    // Create advance deductions for this grower
                                    await CreateAdvanceDeductionsAsync(item.GrowerId, chequeId, primaryBatchId ?? 0, connection, transaction);
                                    
                                    Logger.Info($"Generated regular cheque {chequeId} for grower {item.GrowerId}");
                                }
                            }

                            // STEP 5: Update batch statuses for ALL selected batches
                            if (batchIds?.Any() == true)
                            {
                                // Build dynamic SQL with parameter placeholders
                                var batchIdPlaceholders = string.Join(",", batchIds.Select((_, i) => $"@BatchId{i}"));
                                var updateBatchSql = $@"
                                    UPDATE PaymentBatches 
                                    SET Status = 'Finalized',
                                        ModifiedAt = GETDATE(),
                                        ModifiedBy = @ModifiedBy
                                    WHERE PaymentBatchId IN ({batchIdPlaceholders})";

                                // Create parameters dictionary
                                var parameters = new Dictionary<string, object> { { "ModifiedBy", generatedBy } };
                                for (int i = 0; i < batchIds.Count; i++)
                                {
                                    parameters[$"BatchId{i}"] = batchIds[i];
                                }

                                await connection.ExecuteAsync(updateBatchSql, parameters, transaction);
                            }

                            // STEP 6: Update distribution status
                            var updateDistributionSql = @"
                                UPDATE PaymentDistributions
                                SET Status = 'Generated',
                                    ProcessedAt = GETDATE(),
                                    ProcessedBy = @GeneratedBy
                                WHERE PaymentDistributionId = @DistributionId";

                            await connection.ExecuteAsync(updateDistributionSql, new 
                            { 
                                DistributionId = distributionId, 
                                GeneratedBy = generatedBy 
                            }, transaction);

                            // COMMIT ALL CHANGES
                            transaction.Commit();
                            
                            Logger.Info($"Successfully generated complete payment distribution {distribution.DistributionNumber} with {distribution.Items.Count} items");
                            return distribution;
                        }
                        catch (Exception ex)
                        {
                            // ROLLBACK ALL CHANGES ON ANY ERROR
                            transaction.Rollback();
                            Logger.Error($"Error generating complete payment distribution: {ex.Message}", ex);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GenerateCompletePaymentDistributionAsync: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Check if batch has existing distributions (with transaction support)
        /// </summary>
        private async Task<bool> HasExistingDistributionsAsync(int batchId, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM PaymentDistributionItems 
                WHERE PaymentBatchId = @BatchId 
                AND Status NOT IN ('Voided')";

            var count = await connection.ExecuteScalarAsync<int>(sql, new { BatchId = batchId }, transaction);
            return count > 0;
        }

        /// <summary>
        /// Check if a specific grower has existing distributions for a batch
        /// </summary>
        private async Task<bool> HasExistingDistributionsForGrowerAsync(int batchId, int growerId, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                SELECT COUNT(*) 
                FROM PaymentDistributionItems 
                WHERE PaymentBatchId = @BatchId 
                AND GrowerId = @GrowerId
                AND Status NOT IN ('Voided')";

            var count = await connection.ExecuteScalarAsync<int>(sql, new { BatchId = batchId, GrowerId = growerId }, transaction);
            return count > 0;
        }

        /// <summary>
        /// Update batch processing status based on which growers have been processed
        /// </summary>
        public async Task UpdateBatchProcessingStatusAsync(List<int> batchIds, List<int> processedGrowerIds, string processedBy)
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
                            foreach (var batchId in batchIds)
                            {
                                // Get all growers in this batch
                                var allGrowersInBatch = await GetGrowersInBatchAsync(batchId, connection, transaction);
                                
                                // Check if all growers in this batch have been processed
                                var unprocessedGrowers = allGrowersInBatch.Except(processedGrowerIds).ToList();
                                
                                string newStatus;
                                if (unprocessedGrowers.Any())
                                {
                                    // Some growers still pending - keep as Posted
                                    newStatus = "Posted";
                                }
                                else
                                {
                                    // All growers processed - mark as Finalized
                                    newStatus = "Finalized";
                                }
                                
                                // Update batch status
                                var updateSql = @"
                                    UPDATE PaymentBatches 
                                    SET Status = @Status,
                                        ModifiedAt = @ModifiedAt,
                                        ModifiedBy = @ModifiedBy
                                    WHERE PaymentBatchId = @BatchId";
                                
                                await connection.ExecuteAsync(updateSql, new
                                {
                                    Status = newStatus,
                                    ModifiedAt = DateTime.UtcNow,
                                    ModifiedBy = processedBy,
                                    BatchId = batchId
                                }, transaction);
                                
                                Logger.Info($"Updated batch {batchId} status to {newStatus} (processed {processedGrowerIds.Count} growers, {unprocessedGrowers.Count} remaining)");
                            }
                            
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Logger.Error($"Error updating batch processing status: {ex.Message}", ex);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in UpdateBatchProcessingStatusAsync: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all grower IDs in a specific batch
        /// </summary>
        private async Task<List<int>> GetGrowersInBatchAsync(int batchId, SqlConnection connection, SqlTransaction transaction)
        {
            var sql = @"
                SELECT DISTINCT r.GrowerId
                FROM Receipts r
                INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                WHERE rpa.PaymentBatchId = @BatchId";

            var growerIds = await connection.QueryAsync<int>(sql, new { BatchId = batchId }, transaction);
            return growerIds.ToList();
        }
    }
}
