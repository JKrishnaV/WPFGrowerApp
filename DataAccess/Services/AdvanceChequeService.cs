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
    /// Service for managing advance cheques and their deductions
    /// </summary>
    public class AdvanceChequeService : BaseDatabaseService, IAdvanceChequeService
    {
        private readonly IGrowerService _growerService;

        public AdvanceChequeService(IGrowerService growerService)
        {
            _growerService = growerService;
        }

        public async Task<AdvanceCheque> CreateAdvanceChequeAsync(int growerId, decimal amount, string reason, string createdBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    INSERT INTO AdvanceCheques (GrowerId, AdvanceAmount, AdvanceDate, Reason, Status, CreatedBy, CreatedAt)
                    VALUES (@GrowerId, @AdvanceAmount, @AdvanceDate, @Reason, @Status, @CreatedBy, @CreatedAt);
                    SELECT SCOPE_IDENTITY();";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);
                command.Parameters.AddWithValue("@AdvanceAmount", amount);
                command.Parameters.AddWithValue("@AdvanceDate", DateTime.Now);
                command.Parameters.AddWithValue("@Reason", reason ?? string.Empty);
                command.Parameters.AddWithValue("@Status", "Generated");
                command.Parameters.AddWithValue("@CreatedBy", createdBy);
                command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                var advanceChequeId = Convert.ToInt32(await command.ExecuteScalarAsync());

                return await GetAdvanceChequeByIdAsync(advanceChequeId);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error creating advance cheque: {ex.Message}", ex);
            }
        }

        public async Task<List<AdvanceCheque>> GetOutstandingAdvancesAsync(int growerId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.GrowerId = @GrowerId 
                    AND ac.Status IN ('Printed', 'Delivered')
                    AND ac.DeletedAt IS NULL
                    ORDER BY ac.AdvanceDate ASC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);

                var advances = new List<AdvanceCheque>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    advances.Add(MapAdvanceChequeFromReader(reader));
                }

                return advances;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting outstanding advances: {ex.Message}", ex);
            }
        }

        public async Task<decimal> CalculateTotalOutstandingAdvancesAsync(int growerId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ISNULL(SUM(AdvanceAmount), 0)
                    FROM AdvanceCheques
                    WHERE GrowerId = @GrowerId 
                    AND Status IN ('Printed', 'Delivered')
                    AND DeletedAt IS NULL";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToDecimal(result);
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error calculating total outstanding advances: {ex.Message}", ex);
            }
        }

        public async Task<AdvanceCheque> GetAdvanceChequeByIdAsync(int advanceChequeId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.AdvanceChequeId = @AdvanceChequeId
                    AND ac.DeletedAt IS NULL";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapAdvanceChequeFromReader(reader);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting advance cheque by ID: {ex.Message}", ex);
            }
        }

        public async Task<List<AdvanceCheque>> GetAllAdvanceChequesAsync(string status = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.DeletedAt IS NULL";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ac.Status = @Status";
                }

                query += " ORDER BY ac.AdvanceDate DESC";

                using var command = new SqlCommand(query, connection);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                var advances = new List<AdvanceCheque>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    advances.Add(MapAdvanceChequeFromReader(reader));
                }

                return advances;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting all advance cheques: {ex.Message}", ex);
            }
        }

        public async Task<bool> CancelAdvanceChequeAsync(int advanceChequeId, string reason, string cancelledBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Voided',
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId
                    AND Status = 'Generated'";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", cancelledBy);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error cancelling advance cheque: {ex.Message}", ex);
            }
        }

        public async Task<List<AdvanceCheque>> GetAdvanceChequesByGrowerAsync(int growerId, string status = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.GrowerId = @GrowerId
                    AND ac.DeletedAt IS NULL";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ac.Status = @Status";
                }

                query += " ORDER BY ac.AdvanceDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                var advances = new List<AdvanceCheque>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    advances.Add(MapAdvanceChequeFromReader(reader));
                }

                return advances;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting advance cheques by grower: {ex.Message}", ex);
            }
        }

        public async Task<List<AdvanceCheque>> GetAdvanceChequesByDateRangeAsync(DateTime startDate, DateTime endDate, string status = null)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT ac.*, g.FullName as GrowerName, g.GrowerNumber, c.ChequeNumber
                    FROM AdvanceCheques ac
                    INNER JOIN Growers g ON ac.GrowerId = g.GrowerId
                    LEFT JOIN Cheques c ON c.AdvanceChequeId = ac.AdvanceChequeId
                    WHERE ac.AdvanceDate >= @StartDate
                    AND ac.AdvanceDate <= @EndDate
                    AND ac.DeletedAt IS NULL";

                if (!string.IsNullOrEmpty(status))
                {
                    query += " AND ac.Status = @Status";
                }

                query += " ORDER BY ac.AdvanceDate DESC";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                if (!string.IsNullOrEmpty(status))
                {
                    command.Parameters.AddWithValue("@Status", status);
                }

                var advances = new List<AdvanceCheque>();
                using var reader = await command.ExecuteReaderAsync();
                
                while (await reader.ReadAsync())
                {
                    advances.Add(MapAdvanceChequeFromReader(reader));
                }

                return advances;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error getting advance cheques by date range: {ex.Message}", ex);
            }
        }

        public async Task<bool> UpdateAdvanceChequeAsync(AdvanceCheque advanceCheque)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET AdvanceAmount = @AdvanceAmount,
                        Reason = @Reason,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", advanceCheque.AdvanceChequeId);
                command.Parameters.AddWithValue("@AdvanceAmount", advanceCheque.AdvanceAmount);
                command.Parameters.AddWithValue("@Reason", advanceCheque.Reason ?? string.Empty);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", advanceCheque.ModifiedBy ?? string.Empty);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error updating advance cheque: {ex.Message}", ex);
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

        public async Task<bool> HasOutstandingAdvancesAsync(int growerId)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    SELECT COUNT(*)
                    FROM AdvanceCheques
                    WHERE GrowerId = @GrowerId 
                    AND Status IN ('Printed', 'Delivered')
                    AND DeletedAt IS NULL";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@GrowerId", growerId);

                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error checking outstanding advances: {ex.Message}", ex);
            }
        }

        private AdvanceCheque MapAdvanceChequeFromReader(SqlDataReader reader)
        {
            return new AdvanceCheque
            {
                AdvanceChequeId = Convert.ToInt32(reader["AdvanceChequeId"]),
                GrowerId = Convert.ToInt32(reader["GrowerId"]),
                AdvanceAmount = Convert.ToDecimal(reader["AdvanceAmount"]),
                AdvanceDate = Convert.ToDateTime(reader["AdvanceDate"]),
                Reason = reader["Reason"].ToString(),
                Status = reader["Status"].ToString(),
                CreatedBy = reader["CreatedBy"].ToString(),
                CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                DeductedAt = reader.IsDBNull(reader.GetOrdinal("DeductedAt")) ? null : (DateTime?)Convert.ToDateTime(reader["DeductedAt"]),
                DeductedBy = reader.IsDBNull(reader.GetOrdinal("DeductedBy")) ? null : reader["DeductedBy"].ToString(),
                DeductedFromBatchId = reader.IsDBNull(reader.GetOrdinal("DeductedFromBatchId")) ? null : (int?)Convert.ToInt32(reader["DeductedFromBatchId"]),
                ModifiedAt = reader.IsDBNull(reader.GetOrdinal("ModifiedAt")) ? null : (DateTime?)Convert.ToDateTime(reader["ModifiedAt"]),
                ModifiedBy = reader.IsDBNull(reader.GetOrdinal("ModifiedBy")) ? null : reader["ModifiedBy"].ToString(),
                DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt")) ? null : (DateTime?)Convert.ToDateTime(reader["DeletedAt"]),
                DeletedBy = reader.IsDBNull(reader.GetOrdinal("DeletedBy")) ? null : reader["DeletedBy"].ToString(),
                ChequeNumber = HasColumn(reader, "ChequeNumber") && !reader.IsDBNull(reader.GetOrdinal("ChequeNumber")) ? (int?)Convert.ToInt32(reader["ChequeNumber"]) : null,
                Grower = new Grower
                {
                    GrowerId = Convert.ToInt32(reader["GrowerId"]),
                    FullName = reader["GrowerName"].ToString(),
                    GrowerNumber = reader["GrowerNumber"].ToString()
                }
            };
        }

        /// <summary>
        /// Helper method to check if a column exists in the SqlDataReader
        /// </summary>
        private bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
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

        #region Unified Workflow Methods (Same as regular cheques)

        /// <summary>
        /// Print an advance cheque (Generated -> Printed)
        /// </summary>
        public async Task<bool> PrintAdvanceChequeAsync(int advanceChequeId, string printedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Printed', 
                        PrintedDate = @PrintedDate, 
                        PrintedBy = @PrintedBy,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND Status = 'Generated'";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);
                command.Parameters.AddWithValue("@PrintedDate", DateTime.Now);
                command.Parameters.AddWithValue("@PrintedBy", printedBy);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", printedBy);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error printing advance cheque: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deliver an advance cheque (Printed -> Delivered)
        /// </summary>
        public async Task<bool> DeliverAdvanceChequeAsync(int advanceChequeId, string deliveredBy, string deliveryMethod)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Delivered', 
                        DeliveredAt = @DeliveredAt, 
                        DeliveredBy = @DeliveredBy,
                        DeliveryMethod = @DeliveryMethod,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND Status = 'Printed'";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);
                command.Parameters.AddWithValue("@DeliveredAt", DateTime.Now);
                command.Parameters.AddWithValue("@DeliveredBy", deliveredBy);
                command.Parameters.AddWithValue("@DeliveryMethod", deliveryMethod);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", deliveredBy);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error delivering advance cheque: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Void an advance cheque (Generated/Printed -> Voided)
        /// </summary>
        public async Task<bool> VoidAdvanceChequeAsync(int advanceChequeId, string voidedBy, string voidedReason)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();

                var query = @"
                    UPDATE AdvanceCheques 
                    SET Status = 'Voided', 
                        VoidedDate = @VoidedDate, 
                        VoidedBy = @VoidedBy,
                        VoidedReason = @VoidedReason,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE AdvanceChequeId = @AdvanceChequeId 
                    AND Status IN ('Generated', 'Printed')";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@AdvanceChequeId", advanceChequeId);
                command.Parameters.AddWithValue("@VoidedDate", DateTime.Now);
                command.Parameters.AddWithValue("@VoidedBy", voidedBy);
                command.Parameters.AddWithValue("@VoidedReason", voidedReason);
                command.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                command.Parameters.AddWithValue("@ModifiedBy", voidedBy);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                throw new DatabaseException($"Error voiding advance cheque: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
