using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Exceptions;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing advance deductions
    /// </summary>
    public class AdvanceDeductionService : BaseDatabaseService, IAdvanceDeductionService
    {
        public async Task<decimal> ApplyAdvanceDeductionsAsync(int growerId, int paymentBatchId, decimal paymentAmount, string createdBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                // Use the stored procedure to apply deductions
                using var command = new SqlCommand("sp_ApplyAdvanceDeductions", connection);
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@GrowerId", growerId);
                command.Parameters.AddWithValue("@PaymentBatchId", paymentBatchId);
                command.Parameters.AddWithValue("@PaymentAmount", paymentAmount);
                command.Parameters.AddWithValue("@CreatedBy", createdBy);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error applying advance deductions: {ex.Message}", ex);
            }
        }

        public async Task<bool> ReverseAdvanceDeductionsAsync(int advanceChequeId, string reason, string reversedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Get all deductions for this advance cheque
                    var deductions = await GetDeductionHistoryAsync(advanceChequeId);

                    // Reverse each deduction
                    foreach (var deduction in deductions)
                    {
                        // Delete the deduction record
                        var deleteQuery = "DELETE FROM AdvanceDeductions WHERE DeductionId = @DeductionId";
                        using var deleteCommand = new SqlCommand(deleteQuery, connection, transaction);
                        deleteCommand.Parameters.AddWithValue("@DeductionId", deduction.DeductionId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }

                    // Clear deduction references without changing advance cheque status
                    var updateQuery = @"
                        UPDATE AdvanceCheques 
                        SET 
                            DeductedAt = NULL,
                            DeductedBy = NULL,
                            DeductedFromBatchId = NULL,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE AdvanceChequeId = @AdvanceChequeId";

                    using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);
                    updateCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@ModifiedBy", reversedBy);
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
                throw new DatabaseException($"Error reversing advance deductions: {ex.Message}", ex);
            }
        }

        public async Task<List<AdvanceDeduction>> GetDeductionHistoryAsync(int advanceChequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ad.*, pb.BatchNumber
                    FROM AdvanceDeductions ad
                    INNER JOIN PaymentBatches pb ON ad.PaymentBatchId = pb.PaymentBatchId
                    WHERE ad.AdvanceChequeId = @AdvanceChequeId
                    ORDER BY ad.DeductionDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);

                var deductions = new List<AdvanceDeduction>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    deductions.Add(MapAdvanceDeductionFromReader(reader));
                }

                return deductions;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting deduction history: {ex.Message}", ex);
            }
        }

        public async Task<List<AdvanceDeduction>> GetDeductionsByBatchAsync(int paymentBatchId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ad.*, pb.BatchNumber, ac.AdvanceChequeId, ac.GrowerId
                    FROM AdvanceDeductions ad
                    INNER JOIN PaymentBatches pb ON ad.PaymentBatchId = pb.PaymentBatchId
                    INNER JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ad.PaymentBatchId = @PaymentBatchId
                    ORDER BY ad.DeductionDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@PaymentBatchId", paymentBatchId);

                var deductions = new List<AdvanceDeduction>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    deductions.Add(MapAdvanceDeductionFromReader(reader));
                }

                return deductions;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting deductions by batch: {ex.Message}", ex);
            }
        }

        public async Task<decimal> GetTotalDeductionsAsync(int growerId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ISNULL(SUM(ad.DeductionAmount), 0)
                    FROM AdvanceDeductions ad
                    INNER JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.GrowerId = @GrowerId";

                if (startDate.HasValue)
                {
                    query += " AND ad.DeductionDate >= @StartDate";
                }

                if (endDate.HasValue)
                {
                    query += " AND ad.DeductionDate <= @EndDate";
                }

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

                var result = await command.ExecuteScalarAsync();
                return Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting total deductions: {ex.Message}", ex);
            }
        }

        public async Task<bool> CreateDeductionAsync(AdvanceDeduction deduction)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO AdvanceDeductions (AdvanceChequeId, PaymentBatchId, DeductionAmount, DeductionDate, CreatedBy, CreatedAt)
                    VALUES (@AdvanceChequeId, @PaymentBatchId, @DeductionAmount, @DeductionDate, @CreatedBy, @CreatedAt)";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", deduction.AdvanceChequeId);
                command.Parameters.AddWithValue("@PaymentBatchId", deduction.PaymentBatchId);
                command.Parameters.AddWithValue("@DeductionAmount", deduction.DeductionAmount);
                command.Parameters.AddWithValue("@DeductionDate", deduction.DeductionDate);
                command.Parameters.AddWithValue("@CreatedBy", deduction.CreatedBy);
                command.Parameters.AddWithValue("@CreatedAt", deduction.CreatedAt);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error creating deduction: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateDeductionAsync(AdvanceDeduction deduction)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceDeductions 
                    SET DeductionAmount = @DeductionAmount,
                        DeductionDate = @DeductionDate
                    WHERE DeductionId = @DeductionId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@DeductionId", deduction.DeductionId);
                command.Parameters.AddWithValue("@DeductionAmount", deduction.DeductionAmount);
                command.Parameters.AddWithValue("@DeductionDate", deduction.DeductionDate);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error updating deduction: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteDeductionAsync(int deductionId, string deletedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = "DELETE FROM AdvanceDeductions WHERE DeductionId = @DeductionId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@DeductionId", deductionId);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error deleting deduction: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get deduction history for a specific cheque ID
        /// </summary>
        public async Task<List<AdvanceDeduction>> GetDeductionHistoryByChequeIdAsync(int chequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        ad.DeductionId,
                        ad.AdvanceChequeId,
                        ad.ChequeId,
                        ad.PaymentBatchId,
                        ad.DeductionAmount,
                        ad.DeductionDate,
                        ad.CreatedBy,
                        ad.CreatedAt,
                        pb.BatchNumber
                    FROM AdvanceDeductions ad
                    LEFT JOIN PaymentBatches pb ON ad.PaymentBatchId = pb.PaymentBatchId
                    WHERE ad.ChequeId = @ChequeId
                    ORDER BY ad.DeductionDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ChequeId", chequeId);

                var deductions = new List<AdvanceDeduction>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    deductions.Add(MapAdvanceDeductionFromReader(reader));
                }

                return deductions;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting deduction history for cheque {chequeId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reverse a single advance deduction
        /// </summary>
        public async Task<bool> ReverseAdvanceDeductionAsync(int advanceChequeId, string reason, string reversedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Get all deductions for this advance cheque
                    var deductions = await GetDeductionHistoryAsync(advanceChequeId);

                    // Reverse each deduction
                    foreach (var deduction in deductions)
                    {
                        // Delete the deduction record
                        var deleteQuery = "DELETE FROM AdvanceDeductions WHERE DeductionId = @DeductionId";
                        using var deleteCommand = new SqlCommand(deleteQuery, connection, transaction);
                        deleteCommand.Parameters.AddWithValue("@DeductionId", deduction.DeductionId);
                        await deleteCommand.ExecuteNonQueryAsync();
                    }

                    // Clear deduction references without changing advance cheque status
                    var updateQuery = @"
                        UPDATE AdvanceCheques 
                        SET 
                            DeductedAt = NULL,
                            DeductedBy = NULL,
                            DeductedFromBatchId = NULL,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE AdvanceChequeId = @AdvanceChequeId";

                    using var updateCommand = new SqlCommand(updateQuery, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);
                    updateCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@ModifiedBy", reversedBy);
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
                throw new DatabaseException($"Error reversing advance deduction for advance cheque {advanceChequeId}: {ex.Message}", ex);
            }
        }

        private AdvanceDeduction MapAdvanceDeductionFromReader(SqlDataReader reader)
        {
            return new AdvanceDeduction
            {
                DeductionId = Convert.ToInt32(reader["DeductionId"]),
                AdvanceChequeId = Convert.ToInt32(reader["AdvanceChequeId"]),
                PaymentBatchId = Convert.ToInt32(reader["PaymentBatchId"]),
                DeductionAmount = Convert.ToDecimal(reader["DeductionAmount"]),
                DeductionDate = Convert.ToDateTime(reader["DeductionDate"]),
                CreatedBy = reader["CreatedBy"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                PaymentBatch = new PaymentBatch
                {
                    PaymentBatchId = Convert.ToInt32(reader["PaymentBatchId"]),
                    BatchNumber = reader["BatchNumber"].ToString()
                }
            };
        }
    }
}
