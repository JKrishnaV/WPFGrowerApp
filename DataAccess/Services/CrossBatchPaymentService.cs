using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.DataAccess.Exceptions;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing cross-batch consolidated payments
    /// </summary>
    public class CrossBatchPaymentService : BaseDatabaseService, ICrossBatchPaymentService
    {
        private readonly IChequeService _chequeService;
        private readonly IPaymentBatchService _paymentBatchService;

        public CrossBatchPaymentService(IChequeService chequeService, IPaymentBatchService paymentBatchService)
        {
            _chequeService = chequeService;
            _paymentBatchService = paymentBatchService;
        }

        // Note: GetConsolidatedPaymentForGrowerAsync method removed - consolidated payments replaced by payment distributions

        public async Task<Cheque> GenerateChequeAsync(int growerId, List<int> batchIds, decimal consolidatedAmount, string createdBy,int distributionId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Create the distribution cheque
                    var cheque = new Cheque
                    {
                        GrowerId = growerId,
                        ChequeAmount = consolidatedAmount,
                        ChequeDate = DateTime.Now,
                        Status = "Draft",
                        IsFromDistribution = true,
                        SourceBatches = string.Join(",", batchIds),
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.Now
                    };

                    // Create the distribution cheque directly
                    var chequeSql = @"
                        INSERT INTO Cheques (
                            ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId, PaymentDistributionId,
                            ChequeDate, ChequeAmount, Status, IsFromDistribution, SourceBatches,
                            CreatedAt, CreatedBy
                        )
                        OUTPUT INSERTED.ChequeId
                        VALUES (
                            @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId, @PaymentDistributionId,
                            @ChequeDate, @ChequeAmount, @Status, @IsFromDistribution, @SourceBatches,
                            @CreatedAt, @CreatedBy
                        )";

                    var timestamp = DateTime.Now.ToString("HHmmssfff");
                    var chequeNumber = $"CHQ-{DateTime.Now:yyyyMMdd}-{timestamp}-{growerId}";
                    
                    using var chequeCommand = new SqlCommand(chequeSql, connection, transaction);
                    chequeCommand.Parameters.AddWithValue("@ChequeSeriesId", 2); // PAY series
                    chequeCommand.Parameters.AddWithValue("@ChequeNumber", chequeNumber);
                    chequeCommand.Parameters.AddWithValue("@FiscalYear", DateTime.Now.Year);
                    chequeCommand.Parameters.AddWithValue("@GrowerId", growerId);
                    chequeCommand.Parameters.AddWithValue("@PaymentBatchId", batchIds.First()); // Use first batch as primary
                    chequeCommand.Parameters.AddWithValue("@ChequeDate", cheque.ChequeDate);
                    chequeCommand.Parameters.AddWithValue("@ChequeAmount", consolidatedAmount);
                    chequeCommand.Parameters.AddWithValue("@Status", "Generated");
                    chequeCommand.Parameters.AddWithValue("@IsFromDistribution", true);
                    chequeCommand.Parameters.AddWithValue("@SourceBatches", cheque.SourceBatches);
                    chequeCommand.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    chequeCommand.Parameters.AddWithValue("@CreatedBy", createdBy);
                    chequeCommand.Parameters.AddWithValue("@PaymentDistributionId", distributionId);

                    var chequeId = Convert.ToInt32(await chequeCommand.ExecuteScalarAsync());
                    cheque.ChequeId = chequeId;
                    
                    var createdCheque = cheque;

                    // Create consolidated cheque records
                    foreach (var batchId in batchIds)
                    {
                        var consolidatedCheque = new Cheque
                        {
                            ChequeId = createdCheque.ChequeId,
                            PaymentBatchId = batchId,
                            ChequeAmount = consolidatedAmount / batchIds.Count, // Distribute evenly for now
                            CreatedAt = DateTime.Now,
                            CreatedBy = createdBy
                        };

                        // Note: CreateConsolidatedChequeRecordAsync method removed - consolidated payments replaced by payment distributions
                    }

                    // Update batch statuses to Finalized since they've been processed
                    await UpdateBatchStatusAfterConsolidationAsync(batchIds, "Finalized", createdBy);

                    transaction.Commit();
                    return createdCheque;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error generating consolidated cheque: {ex.Message}", ex);
            }
        }

        public async Task<List<GrowerPaymentAcrossBatches>> GetGrowerPaymentsAcrossBatchesAsync(int growerId, List<int> batchIds)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var batchIdsParam = string.Join(",", batchIds.Select((id, index) => $"@BatchId{index}"));
                var query = $@"
                    SELECT 
                        pb.PaymentBatchId,
                        pb.BatchNumber,
                        pb.BatchDate,
                        pb.Status,
                        ISNULL(SUM(rpa.AmountPaid), 0) as Amount
                    FROM PaymentBatches pb
                    LEFT JOIN ReceiptPaymentAllocations rpa ON pb.PaymentBatchId = rpa.PaymentBatchId
                    LEFT JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                    WHERE pb.PaymentBatchId IN ({batchIdsParam})
                    AND r.GrowerId = @GrowerId
                    GROUP BY pb.PaymentBatchId, pb.BatchNumber, pb.BatchDate, pb.Status
                    ORDER BY pb.BatchDate";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);
                
                // Add parameters for each batch ID
                for (int i = 0; i < batchIds.Count; i++)
                {
                    command.Parameters.AddWithValue($"@BatchId{i}", batchIds[i]);
                }

                var payments = new List<GrowerPaymentAcrossBatches>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    payments.Add(new GrowerPaymentAcrossBatches
                    {
                        BatchId = Convert.ToInt32(reader["PaymentBatchId"]),
                        BatchNumber = reader["BatchNumber"].ToString(),
                        BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                        Amount = Convert.ToDecimal(reader["Amount"]),
                        Status = reader["Status"].ToString()
                    });
                }

                return payments;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting grower payments across batches: {ex.Message}", ex);
            }
        }

        public async Task<ConsolidationValidationResult> ValidateConsolidationAsync(int growerId, List<int> batchIds)
        {
            try
            {
                var result = new ConsolidationValidationResult
                {
                    IsValid = true,
                    Warnings = new List<string>(),
                    Errors = new List<string>()
                };

                // Check if grower exists in all batches
                var growerPayments = await GetGrowerPaymentsAcrossBatchesAsync(growerId, batchIds);
                
                if (growerPayments.Count != batchIds.Count)
                {
                    result.IsValid = false;
                    result.Errors.Add("Grower does not exist in all selected batches");
                }

                // Check if all batches are in draft status
                var invalidBatches = growerPayments.Where(p => p.Status != "Draft").ToList();
                if (invalidBatches.Any())
                {
                    result.IsValid = false;
                    result.Errors.Add($"Batches {string.Join(", ", invalidBatches.Select(b => b.BatchNumber))} are not in draft status");
                }

                // Check if total amount is positive
                var totalAmount = growerPayments.Sum(p => p.Amount);
                if (totalAmount <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Total consolidated amount must be greater than zero");
                }

                // Add warnings for potential issues
                if (batchIds.Count > 5)
                {
                    result.Warnings.Add("Consolidating more than 5 batches may be complex to manage");
                }

                if (totalAmount > 10000)
                {
                    result.Warnings.Add("Large consolidated amount - consider breaking into smaller payments");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error validating consolidation: {ex.Message}", ex);
            }
        }

        public async Task<List<GrowerBatchDetails>> GetGrowersInMultipleBatchesAsync(List<int> batchIds)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var batchIdsParam = string.Join(",", batchIds.Select((id, index) => $"@BatchId{index}"));
                var query = $@"
                    SELECT 
                        g.GrowerId,
                        g.GrowerNumber,
                        g.FullName,
                        COUNT(DISTINCT pb.PaymentBatchId) as BatchCount,
                        STRING_AGG(pb.BatchNumber, ', ') as BatchNumbers
                    FROM Growers g
                    INNER JOIN Receipts r ON g.GrowerId = r.GrowerId
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    INNER JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                    WHERE pb.PaymentBatchId IN ({batchIdsParam})
                    GROUP BY g.GrowerId, g.GrowerNumber, g.FullName
                    ORDER BY BatchCount DESC, g.GrowerNumber";

                Infrastructure.Logging.Logger.Info($"GetGrowersInMultipleBatchesAsync: Executing query for batch IDs: {string.Join(", ", batchIds)}");
                Infrastructure.Logging.Logger.Info($"GetGrowersInMultipleBatchesAsync: Query: {query}");

                using var command = new SqlCommand(query, connection);
                
                // Add parameters for each batch ID
                for (int i = 0; i < batchIds.Count; i++)
                {
                    command.Parameters.AddWithValue($"@BatchId{i}", batchIds[i]);
                }

                var growers = new List<GrowerBatchDetails>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    growers.Add(new GrowerBatchDetails
                    {
                        GrowerId = Convert.ToInt32(reader["GrowerId"]),
                        GrowerNumber = reader["GrowerNumber"].ToString(),
                        GrowerName = reader["FullName"].ToString(),
                        BatchCount = Convert.ToInt32(reader["BatchCount"]),
                        BatchNumbers = reader["BatchNumbers"].ToString()
                    });
                }

                Infrastructure.Logging.Logger.Info($"GetGrowersInMultipleBatchesAsync: Found {growers.Count} growers");
                foreach (var grower in growers)
                {
                    Infrastructure.Logging.Logger.Info($"  - Grower: {grower.GrowerName} (ID: {grower.GrowerId}, Number: {grower.GrowerNumber})");
                }

                return growers;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting growers in multiple batches: {ex.Message}", ex);
            }
        }

        public async Task<List<BatchBreakdown>> GetBatchBreakdownAsync(int chequeId, SqlConnection connection = null, SqlTransaction transaction = null)
        {
            try
            {
                var useExternalConnection = connection != null;
                if (!useExternalConnection)
                {
                    connection = CreateConnection();
                    await connection.OpenAsync();
                }

                // First try to get from Cheques table
                var consolidatedQuery = @"
                    SELECT 
                        cc.PaymentBatchId,
                        pb.BatchNumber,
                        pb.BatchDate,
                        cc.Amount,
                        pb.Status
                    FROM Cheques cc
                    INNER JOIN PaymentBatches pb ON cc.PaymentBatchId = pb.PaymentBatchId
                    WHERE cc.ChequeId = @ChequeId
                    ORDER BY pb.BatchDate";

                using var consolidatedCommand = new SqlCommand(consolidatedQuery, connection, transaction);
                consolidatedCommand.Parameters.AddWithValue("@ChequeId", chequeId);

                var breakdowns = new List<BatchBreakdown>();
                using var reader = await consolidatedCommand.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    breakdowns.Add(new BatchBreakdown
                    {
                        BatchId = Convert.ToInt32(reader["PaymentBatchId"]),
                        BatchNumber = reader["BatchNumber"].ToString(),
                        Amount = Convert.ToDecimal(reader["Amount"]),
                        BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                        Status = reader["Status"].ToString()
                    });
                }

                // If no consolidated records found, try to get batch info from the cheque's SourceBatches field
                if (breakdowns.Count == 0)
                {
                    reader.Close();
                    
                    var fallbackQuery = @"
                        SELECT 
                            pb.PaymentBatchId,
                            pb.BatchNumber,
                            pb.BatchDate,
                            pb.Status,
                            c.ChequeAmount / (LEN(c.SourceBatches) - LEN(REPLACE(c.SourceBatches, ',', '')) + 1) as Amount
                        FROM Cheques c
                        CROSS APPLY STRING_SPLIT(c.SourceBatches, ',') ss
                        INNER JOIN PaymentBatches pb ON CAST(ss.value AS INT) = pb.PaymentBatchId
                        WHERE c.ChequeId = @ChequeId
                        AND c.IsFromDistribution = 1
                        ORDER BY pb.BatchDate";

                    using var fallbackCommand = new SqlCommand(fallbackQuery, connection, transaction);
                    fallbackCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                    
                    using var fallbackReader = await fallbackCommand.ExecuteReaderAsync();
                    while (await fallbackReader.ReadAsync())
                    {
                        breakdowns.Add(new BatchBreakdown
                        {
                            BatchId = Convert.ToInt32(fallbackReader["PaymentBatchId"]),
                            BatchNumber = fallbackReader["BatchNumber"].ToString(),
                            Amount = Convert.ToDecimal(fallbackReader["Amount"]),
                            BatchDate = Convert.ToDateTime(fallbackReader["BatchDate"]),
                            Status = fallbackReader["Status"].ToString()
                        });
                    }
                }

                return breakdowns;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting batch breakdown: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateBatchStatusAfterConsolidationAsync(List<int> batchIds, string newStatus, string updatedBy, SqlConnection connection = null, SqlTransaction transaction = null)
        {
            try
            {
                var useExternalConnection = connection != null;
                if (!useExternalConnection)
                {
                    connection = CreateConnection();
                    await connection.OpenAsync();
                }

                var batchIdsParam = string.Join(",", batchIds.Select((id, index) => $"@BatchId{index}"));
                var query = $@"
                    UPDATE PaymentBatches 
                    SET Status = @NewStatus,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE PaymentBatchId IN ({batchIdsParam})";

                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@NewStatus", newStatus);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", updatedBy);
                
                // Add parameters for each batch ID
                for (int i = 0; i < batchIds.Count; i++)
                {
                    command.Parameters.AddWithValue($"@BatchId{i}", batchIds[i]);
                }

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error updating batch status: {ex.Message}", ex);
            }
        }

        public async Task<bool> RevertConsolidationAsync(int consolidatedChequeId, string revertedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Get the consolidated cheque details
                    var batchBreakdowns = await GetBatchBreakdownAsync(consolidatedChequeId, connection, transaction);
                    var batchIds = batchBreakdowns.Select(b => b.BatchId).ToList();

                    // Update batch statuses back to Posted (so they show in Enhanced Payment Distribution)
                    await UpdateBatchStatusAfterConsolidationAsync(batchIds, "Posted", revertedBy, connection, transaction);

                    // Delete consolidated cheque records
                    var deleteQuery = "DELETE FROM Cheques WHERE ChequeId = @ChequeId";
                    using var deleteCommand = new SqlCommand(deleteQuery, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@ChequeId", consolidatedChequeId);
                    await deleteCommand.ExecuteNonQueryAsync();

                    // Revert advance deductions for this cheque
                    await RevertAdvanceDeductionsAsync(consolidatedChequeId, connection, transaction);

                    // Clean up PaymentDistributionItems for the voided cheque
                    await CleanupPaymentDistributionItemsAsync(consolidatedChequeId, connection, transaction);

                    // Update the cheque status
                    var updateQuery = @"
                        UPDATE Cheques 
                        SET Status = 'Voided',
                            IsFromDistribution = 0,
                            SourceBatches = NULL,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE ChequeId = @ChequeId";

                    using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@ChequeId", consolidatedChequeId);
                    updateCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@ModifiedBy", revertedBy);
                    await updateCommand.ExecuteNonQueryAsync();

                    transaction.Commit();
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error reverting consolidation: {ex.Message}", ex);
            }
        }

        public async Task<List<ConsolidationHistory>> GetConsolidationHistoryAsync(int growerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        c.ChequeId,
                        c.ChequeNumber,
                        c.ChequeDate,
                        c.ChequeAmount,
                        c.Status,
                        STRING_AGG(pb.BatchNumber, ', ') as SourceBatches,
                        COUNT(cc.PaymentBatchId) as BatchCount
                    FROM Cheques c
                    INNER JOIN Cheques cc ON c.ChequeId = cc.ChequeId
                    INNER JOIN PaymentBatches pb ON cc.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.GrowerId = @GrowerId
                    AND c.IsFromDistribution = 1";

                if (startDate.HasValue)
                {
                    query += " AND c.ChequeDate >= @StartDate";
                }

                if (endDate.HasValue)
                {
                    query += " AND c.ChequeDate <= @EndDate";
                }

                query += @"
                    GROUP BY c.ChequeId, c.ChequeNumber, c.ChequeDate, c.ChequeAmount, c.Status
                    ORDER BY c.ChequeDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);

                if (startDate.HasValue)
                {
                    command.Parameters.AddWithValue("@StartDate", startDate.Value);
                }

                if (endDate.HasValue)
                {
                    command.Parameters.AddWithValue("@EndDate", endDate.Value);
                }

                var history = new List<ConsolidationHistory>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    history.Add(new ConsolidationHistory
                    {
                        ChequeId = Convert.ToInt32(reader["ChequeId"]),
                        ChequeNumber = reader["ChequeNumber"].ToString(),
                        ChequeDate = Convert.ToDateTime(reader["ChequeDate"]),
                        Amount = Convert.ToDecimal(reader["ChequeAmount"]),
                        Status = reader["Status"].ToString(),
                        SourceBatches = reader["SourceBatches"].ToString(),
                        BatchCount = Convert.ToInt32(reader["BatchCount"])
                    });
                }

                return history;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting consolidation history: {ex.Message}", ex);
            }
        }

        private async Task RevertAdvanceDeductionsAsync(int chequeId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Delete advance deductions for this cheque
                var deleteDeductionsSql = @"
                    DELETE FROM AdvanceDeductions 
                    WHERE ChequeId = @ChequeId";
                
                using var deleteCommand = new SqlCommand(deleteDeductionsSql, connection, transaction);
                deleteCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                await deleteCommand.ExecuteNonQueryAsync();

                // Clear deduction tracking fields but keep the advance cheque status as 'Printed'
                var resetAdvanceChequesSql = @"
                    UPDATE AdvanceCheques 
                    SET DeductedAt = NULL,
                        DeductedBy = NULL,
                        DeductedFromBatchId = NULL,
                        DeductedByChequeId = NULL,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE DeductedByChequeId = @ChequeId";
                
                using var resetCommand = new SqlCommand(resetAdvanceChequesSql, connection, transaction);
                resetCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                resetCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                resetCommand.Parameters.AddWithValue("@ModifiedBy", "SYSTEM");
                await resetCommand.ExecuteNonQueryAsync();

                Logger.Info($"Reverted advance deductions for consolidated cheque {chequeId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reverting advance deductions for cheque {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        private async Task CleanupPaymentDistributionItemsAsync(int chequeId, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Get the grower ID from the cheque
                var getGrowerSql = "SELECT GrowerId FROM Cheques WHERE ChequeId = @ChequeId";
                using var getGrowerCommand = new SqlCommand(getGrowerSql, connection, transaction);
                getGrowerCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                var growerId = await getGrowerCommand.ExecuteScalarAsync();

                if (growerId != null)
                {
                    // Get only payment distribution items that are specifically related to this voided cheque
                    // This prevents accidentally deleting items from other cheques for the same grower
                    var getItemsSql = @"
                        SELECT DISTINCT pdi.PaymentDistributionItemId, pdi.Status, pdi.CreatedAt
                        FROM PaymentDistributionItems pdi
                        INNER JOIN PaymentDistributionReceipts pdr ON pdi.PaymentDistributionItemId = pdr.PaymentDistributionItemId
                        INNER JOIN ReceiptPaymentAllocations rpa ON pdr.ReceiptId = rpa.ReceiptId
                        INNER JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        INNER JOIN Cheques cc ON pb.PaymentBatchId = cc.PaymentBatchId
                        WHERE cc.ChequeId = @ChequeId
                          AND (pdi.Status = 'Voided' OR pdi.Status = 'Pending')
                        ORDER BY pdi.CreatedAt DESC";
                    
                    using var getItemsCommand = new SqlCommand(getItemsSql, connection, transaction);
                    getItemsCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                    
                    var itemsToDelete = new List<int>();
                    using var reader = await getItemsCommand.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var itemId = Convert.ToInt32(reader["PaymentDistributionItemId"]);
                        var status = reader["Status"].ToString();
                        var createdAt = Convert.ToDateTime(reader["CreatedAt"]);
                        
                        // All items returned by the query are already filtered to be Voided or Pending
                        // and are specifically related to this voided cheque, so add them to delete list
                        itemsToDelete.Add(itemId);
                        
                        Logger.Debug($"Marking PaymentDistributionItem {itemId} (Status: {status}, Created: {createdAt:yyyy-MM-dd HH:mm}) for deletion");
                    }
                    reader.Close();

                    // Delete the identified items
                    if (itemsToDelete.Any())
                    {
                        // First delete PaymentDistributionReceipts (child records)
                        var deleteReceiptsSql = @"
                            DELETE FROM PaymentDistributionReceipts 
                            WHERE PaymentDistributionItemId IN @ItemIds";
                        
                        await connection.ExecuteAsync(deleteReceiptsSql, new { ItemIds = itemsToDelete }, transaction);

                        // Then delete PaymentDistributionItems (parent records)
                        var deleteItemsSql = @"
                            DELETE FROM PaymentDistributionItems 
                            WHERE PaymentDistributionItemId IN @ItemIds";
                        
                        await connection.ExecuteAsync(deleteItemsSql, new { ItemIds = itemsToDelete }, transaction);

                        Logger.Info($"Cleaned up {itemsToDelete.Count} PaymentDistributionItems and related PaymentDistributionReceipts specifically related to consolidated cheque {chequeId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error cleaning up PaymentDistributionItems for cheque {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        // Note: CreateChequeRecordAsync method removed - consolidated payments replaced by payment distributions
    }
}
