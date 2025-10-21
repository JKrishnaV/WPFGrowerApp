using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.DataAccess.Exceptions;

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

        public async Task<ConsolidatedPayment> GetConsolidatedPaymentForGrowerAsync(int growerId, List<int> batchIds)
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
                        ISNULL(SUM(rpa.AmountPaid), 0) as TotalAmount
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

                var consolidatedPayment = new ConsolidatedPayment
                {
                    GrowerId = growerId,
                    BatchIds = batchIds
                };

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var batchBreakdown = new BatchBreakdown
                    {
                        BatchId = Convert.ToInt32(reader["PaymentBatchId"]),
                        BatchNumber = reader["BatchNumber"].ToString(),
                        Amount = Convert.ToDecimal(reader["TotalAmount"]),
                        BatchDate = Convert.ToDateTime(reader["BatchDate"]),
                        Status = reader["Status"].ToString()
                    };

                    consolidatedPayment.AddBatch(batchBreakdown);
                }

                return consolidatedPayment;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting consolidated payment: {ex.Message}", ex);
            }
        }

        public async Task<Cheque> GenerateConsolidatedChequeAsync(int growerId, List<int> batchIds, decimal consolidatedAmount, string createdBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Create the consolidated cheque
                    var cheque = new Cheque
                    {
                        GrowerId = growerId,
                        ChequeAmount = consolidatedAmount,
                        ChequeDate = DateTime.Now,
                        Status = "Draft",
                        IsConsolidated = true,
                        ConsolidatedFromBatches = string.Join(",", batchIds),
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.Now
                    };

                    var success = await _chequeService.CreateChequesAsync(new List<Cheque> { cheque });
                    if (!success)
                    {
                        throw new Exception("Failed to create consolidated cheque");
                    }
                    
                    // The cheque object should now have its ID populated after creation
                    var createdCheque = cheque;

                    // Create consolidated cheque records
                    foreach (var batchId in batchIds)
                    {
                        var consolidatedCheque = new ConsolidatedCheque
                        {
                            ChequeId = createdCheque.ChequeId,
                            PaymentBatchId = batchId,
                            Amount = consolidatedAmount / batchIds.Count, // Distribute evenly for now
                            CreatedAt = DateTime.Now,
                            CreatedBy = createdBy
                        };

                        await CreateConsolidatedChequeRecordAsync(consolidatedCheque, connection, transaction);
                    }

                    // Update batch statuses
                    await UpdateBatchStatusAfterConsolidationAsync(batchIds, "Consolidated", createdBy);

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

        public async Task<List<BatchBreakdown>> GetBatchBreakdownAsync(int chequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        cc.PaymentBatchId,
                        pb.BatchNumber,
                        pb.BatchDate,
                        cc.Amount,
                        pb.Status
                    FROM ConsolidatedCheques cc
                    INNER JOIN PaymentBatches pb ON cc.PaymentBatchId = pb.PaymentBatchId
                    WHERE cc.ChequeId = @ChequeId
                    ORDER BY pb.BatchDate";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ChequeId", chequeId);

                var breakdowns = new List<BatchBreakdown>();
                using var reader = await command.ExecuteReaderAsync();
                
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

                return breakdowns;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting batch breakdown: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateBatchStatusAfterConsolidationAsync(List<int> batchIds, string newStatus, string updatedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var batchIdsParam = string.Join(",", batchIds.Select((id, index) => $"@BatchId{index}"));
                var query = $@"
                    UPDATE PaymentBatches 
                    SET Status = @NewStatus,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE PaymentBatchId IN ({batchIdsParam})";

                using var command = new SqlCommand(query, connection);
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
                    var batchBreakdowns = await GetBatchBreakdownAsync(consolidatedChequeId);
                    var batchIds = batchBreakdowns.Select(b => b.BatchId).ToList();

                    // Update batch statuses back to Draft
                    await UpdateBatchStatusAfterConsolidationAsync(batchIds, "Draft", revertedBy);

                    // Delete consolidated cheque records
                    var deleteQuery = "DELETE FROM ConsolidatedCheques WHERE ChequeId = @ChequeId";
                    using var deleteCommand = new SqlCommand(deleteQuery, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@ChequeId", consolidatedChequeId);
                    await deleteCommand.ExecuteNonQueryAsync();

                    // Update the cheque status
                    var updateQuery = @"
                        UPDATE Cheques 
                        SET Status = 'Voided',
                            IsConsolidated = 0,
                            ConsolidatedFromBatches = NULL,
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
                    INNER JOIN ConsolidatedCheques cc ON c.ChequeId = cc.ChequeId
                    INNER JOIN PaymentBatches pb ON cc.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.GrowerId = @GrowerId
                    AND c.IsConsolidated = 1";

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

        private async Task CreateConsolidatedChequeRecordAsync(ConsolidatedCheque consolidatedCheque, SqlConnection connection, SqlTransaction transaction)
        {
            var query = @"
                INSERT INTO ConsolidatedCheques (ChequeId, PaymentBatchId, Amount, CreatedAt, CreatedBy)
                VALUES (@ChequeId, @PaymentBatchId, @Amount, @CreatedAt, @CreatedBy)";

            using var command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@ChequeId", consolidatedCheque.ChequeId);
            command.Parameters.AddWithValue("@PaymentBatchId", consolidatedCheque.PaymentBatchId);
            command.Parameters.AddWithValue("@Amount", consolidatedCheque.Amount);
            command.Parameters.AddWithValue("@CreatedAt", consolidatedCheque.CreatedAt);
            command.Parameters.AddWithValue("@CreatedBy", consolidatedCheque.CreatedBy);

            await command.ExecuteNonQueryAsync();
        }
    }
}
