using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing cheque operations
    /// NOTE: This service is currently STUBBED OUT pending modernization for the new Cheque model.
    /// The original implementation used legacy column names and properties that no longer exist.
    /// ChequeGenerationService handles core cheque generation for Phase 1.
    /// TODO (Phase 1.5): Rewrite all methods to use modern Cheques table schema.
    /// </summary>
    public class ChequeService : BaseDatabaseService, IChequeService
    {
        // ==================================================================================
        // TEMPORARY STUB IMPLEMENTATIONS - ALL METHODS NEED REWRITING FOR MODERN SCHEMA
        // ==================================================================================
        
        public async Task<List<Cheque>> GetAllChequesAsync()
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        ChequeId,
                        ChequeSeriesId,
                        ChequeNumber,
                        FiscalYear,
                        GrowerId,
                        PaymentBatchId,
                        ChequeDate,
                        ChequeAmount,
                        CurrencyCode,
                        ExchangeRate,
                        PayeeName,
                        Memo,
                        Status,
                        ClearedDate,
                        VoidedDate,
                        VoidedReason,
                        CreatedAt,
                        CreatedBy,
                        ModifiedAt,
                        ModifiedBy,
                        DeletedAt,
                        DeletedBy,
                        PrintedAt,
                        PrintedBy,
                        VoidedBy,
                        IssuedAt,
                        IssuedBy
                    FROM Cheques 
                    WHERE DeletedAt IS NULL 
                    AND Status IN ('Printed', 'Voided', 'Stopped')
                    ORDER BY ChequeDate DESC";
                
                var cheques = await connection.QueryAsync<Cheque>(sql);
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all cheques: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        public async Task<Cheque> GetChequeBySeriesAndNumberAsync(string series, decimal chequeNumber)
        {
            // TODO: Rewrite to use ChequeSeriesId and ChequeNumber (int)
            Logger.Warn($"ChequeService.GetChequeBySeriesAndNumberAsync({series}, {chequeNumber}) not yet implemented for modern schema");
            await Task.CompletedTask;
            return null;
        }

        public async Task<List<Cheque>> GetChequesByGrowerNumberAsync(decimal growerNumber)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        ChequeId,
                        ChequeSeriesId,
                        ChequeNumber,
                        FiscalYear,
                        GrowerId,
                        PaymentBatchId,
                        ChequeDate,
                        ChequeAmount,
                        CurrencyCode,
                        ExchangeRate,
                        PayeeName,
                        Memo,
                        Status,
                        ClearedDate,
                        VoidedDate,
                        VoidedReason,
                        CreatedAt,
                        CreatedBy,
                        ModifiedAt,
                        ModifiedBy,
                        DeletedAt,
                        DeletedBy,
                        PrintedAt,
                        PrintedBy,
                        VoidedBy,
                        IssuedAt,
                        IssuedBy
                    FROM Cheques 
                    WHERE DeletedAt IS NULL 
                    AND GrowerId = @GrowerNumber
                    AND Status IN ('Printed', 'Voided', 'Stopped')
                    ORDER BY ChequeDate DESC";
                
                var cheques = await connection.QueryAsync<Cheque>(sql, new { GrowerNumber = growerNumber });
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheques for grower number {growerNumber}: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        public async Task<List<Cheque>> GetChequesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            // TODO: Rewrite to use ChequeDate column
            Logger.Warn($"ChequeService.GetChequesByDateRangeAsync({startDate:d}, {endDate:d}) not yet implemented for modern schema");
            await Task.CompletedTask;
            return new List<Cheque>();
        }

        public async Task<bool> SaveChequeAsync(Cheque cheque)
        {
            // TODO: Rewrite to use modern Cheque properties and Cheques table
            // Use ChequeSeriesId, GrowerId, ChequeDate, ChequeAmount, Status, etc.
            Logger.Warn("ChequeService.SaveChequeAsync() not yet implemented for modern schema");
            await Task.CompletedTask;
            return false;
        }

        public async Task<bool> VoidChequeAsync(string series, decimal chequeNumber)
        {
            // TODO: Rewrite to set Status = 'Voided', VoidedDate = NOW, VoidedBy = user
            // Instead of setting VOID = 1
            Logger.Warn($"ChequeService.VoidChequeAsync({series}, {chequeNumber}) not yet implemented for modern schema");
            await Task.CompletedTask;
            return false;
        }

        public async Task<decimal> GetNextChequeNumberAsync(string series, bool isEft = false)
        {
            // TODO: Rewrite to query ChequeSeries table for next number
            // Should increment ChequeSeries.LastChequeNumber
            Logger.Warn($"ChequeService.GetNextChequeNumberAsync({series}, EFT:{isEft}) not yet implemented for modern schema");
            Logger.Warn("Consider using ChequeGenerationService.GetNextChequeNumberAsync() instead");
            await Task.CompletedTask;
            return 1000; // Stub return value
        }

        public async Task<bool> CreateChequesAsync(List<Cheque> chequesToCreate)
        {
            // TODO: Rewrite to bulk insert into modern Cheques table
            // Use modern column names and properties
            Logger.Warn($"ChequeService.CreateChequesAsync() called with {chequesToCreate?.Count ?? 0} cheques - not yet implemented");
            Logger.Warn("Consider using ChequeGenerationService.GenerateChequesForBatchAsync() instead");
            await Task.CompletedTask;
            return false;
        }

        public async Task<List<Cheque>> GetTemporaryChequesAsync(string currency, string tempChequeSeries, decimal tempChequeNumberStart)
        {
            // TODO: Rewrite to query modern Cheques table
            Logger.Warn($"ChequeService.GetTemporaryChequesAsync({currency}, {tempChequeSeries}, {tempChequeNumberStart}) not yet implemented");
            await Task.CompletedTask;
            return new List<Cheque>();
        }


        // New methods for enhanced cheque processing
        public async Task<List<Cheque>> GetChequesByStatusAsync(string status)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "SELECT * FROM Cheques WHERE Status = @Status ORDER BY ChequeDate DESC";
                var cheques = await connection.QueryAsync<Cheque>(sql, new { Status = status });
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheques by status {status}: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        public async Task<bool> UpdateChequeStatusAsync(int chequeId, string newStatus, string updatedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "UPDATE Cheques SET Status = @Status, ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy WHERE ChequeId = @ChequeId";
                var result = await connection.ExecuteAsync(sql, new 
                { 
                    Status = newStatus, 
                    ModifiedAt = DateTime.Now, 
                    ModifiedBy = updatedBy, 
                    ChequeId = chequeId 
                });
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating cheque status: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> MarkChequesAsPrintedAsync(List<int> chequeIds, string printedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "UPDATE Cheques SET Status = 'Printed', PrintedAt = @PrintedAt, PrintedBy = @PrintedBy WHERE ChequeId IN @ChequeIds";
                var result = await connection.ExecuteAsync(sql, new 
                { 
                    PrintedAt = DateTime.Now, 
                    PrintedBy = printedBy, 
                    ChequeIds = chequeIds 
                });
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheques as printed: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> MarkChequesAsIssuedAsync(List<int> chequeIds, string issuedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "UPDATE Cheques SET Status = 'Issued', IssuedAt = @IssuedAt, IssuedBy = @IssuedBy WHERE ChequeId IN @ChequeIds";
                var result = await connection.ExecuteAsync(sql, new 
                { 
                    IssuedAt = DateTime.Now, 
                    IssuedBy = issuedBy, 
                    ChequeIds = chequeIds 
                });
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheques as issued: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<byte[]> GenerateChequePdfAsync(int chequeId)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "SELECT * FROM Cheques WHERE ChequeId = @ChequeId";
                var cheque = await connection.QueryFirstOrDefaultAsync<Cheque>(sql, new { ChequeId = chequeId });
                
                if (cheque == null)
                {
                    Logger.Warn($"Cheque {chequeId} not found for PDF generation");
                    return Array.Empty<byte>();
                }

                var pdfGenerator = new WPFGrowerApp.Services.ChequePdfGenerator();
                return await pdfGenerator.GenerateSingleChequePdfAsync(cheque);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating PDF for cheque {chequeId}: {ex.Message}", ex);
                return Array.Empty<byte>();
            }
        }

        public async Task<byte[]> GenerateBatchChequePdfAsync(List<int> chequeIds)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "SELECT * FROM Cheques WHERE ChequeId IN @ChequeIds ORDER BY ChequeDate";
                var cheques = await connection.QueryAsync<Cheque>(sql, new { ChequeIds = chequeIds });
                
                if (!cheques.Any())
                {
                    Logger.Warn($"No cheques found for batch PDF generation");
                    return Array.Empty<byte>();
                }

                var pdfGenerator = new WPFGrowerApp.Services.ChequePdfGenerator();
                return await pdfGenerator.GenerateBatchChequePdfAsync(cheques.ToList());
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating batch PDF: {ex.Message}", ex);
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Voids multiple cheques with a reason for audit purposes.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to void.</param>
        /// <param name="reason">Reason for voiding the cheques.</param>
        /// <param name="voidedBy">User who voided the cheques.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task VoidChequesAsync(List<int> chequeIds, string reason, string voidedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Voided', 
                        VoidedAt = @VoidedAt, 
                        VoidedBy = @VoidedBy,
                        VoidReason = @Reason
                    WHERE ChequeId IN @ChequeIds AND Status IN ('Generated', 'Issued')";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    VoidedAt = DateTime.UtcNow,
                    VoidedBy = voidedBy,
                    Reason = reason
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("No cheques were voided. Cheques may already be processed or not found.");
                }

                Logger.Info($"Successfully voided {rowsAffected} cheques. Reason: {reason}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error voiding cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Stops payment on multiple cheques with a reason for audit purposes.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to stop payment on.</param>
        /// <param name="reason">Reason for stopping payment.</param>
        /// <param name="stoppedBy">User who stopped the payment.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task StopPaymentAsync(List<int> chequeIds, string reason, string stoppedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Stopped', 
                        StoppedAt = @StoppedAt, 
                        StoppedBy = @StoppedBy,
                        StopReason = @Reason
                    WHERE ChequeId IN @ChequeIds AND Status IN ('Generated', 'Issued')";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    StoppedAt = DateTime.UtcNow,
                    StoppedBy = stoppedBy,
                    Reason = reason
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("No cheques were stopped. Cheques may already be processed or not found.");
                }

                Logger.Info($"Successfully stopped payment on {rowsAffected} cheques. Reason: {reason}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error stopping payment on cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Logs reprint activity for audit purposes.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs that were reprinted.</param>
        /// <param name="reason">Reason for reprinting.</param>
        /// <param name="reprintedBy">User who reprinted the cheques.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task LogReprintActivityAsync(List<int> chequeIds, string reason, string reprintedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ChequeAuditLog (ChequeId, Action, ActionBy, ActionDate, Reason, Details)
                    VALUES (@ChequeId, 'Reprint', @ReprintedBy, @ReprintedAt, @Reason, @Details)";

                var auditLogs = chequeIds.Select(chequeId => new
                {
                    ChequeId = chequeId,
                    ReprintedBy = reprintedBy,
                    ReprintedAt = DateTime.UtcNow,
                    Reason = reason,
                    Details = $"Cheque reprinted. Reason: {reason}"
                }).ToList();

                await connection.ExecuteAsync(sql, auditLogs);
                
                Logger.Info($"Successfully logged reprint activity for {chequeIds.Count} cheques. Reason: {reason}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error logging reprint activity: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Marks cheques as delivered with delivery method tracking.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to mark as delivered.</param>
        /// <param name="deliveryMethod">Method of delivery (Mail, Pickup, Courier).</param>
        /// <param name="deliveredBy">User who recorded the delivery.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task<bool> MarkChequesAsDeliveredAsync(List<int> chequeIds, string deliveryMethod, string deliveredBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Delivered', 
                        DeliveredAt = @DeliveredAt, 
                        DeliveredBy = @DeliveredBy,
                        DeliveryMethod = @DeliveryMethod
                    WHERE ChequeId IN @ChequeIds AND Status = 'Printed'";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    DeliveredAt = DateTime.UtcNow,
                    DeliveredBy = deliveredBy,
                    DeliveryMethod = deliveryMethod
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                Logger.Info($"Successfully marked {rowsAffected} cheques as delivered via {deliveryMethod}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheques as delivered: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Approves cheques for delivery (updates status to "Delivered").
        /// This is the final step in the workflow after review.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to approve for delivery.</param>
        /// <param name="approvedBy">User who approved the cheques.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task<bool> ApproveChequesForDeliveryAsync(List<int> chequeIds, string approvedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Delivered', 
                        DeliveredAt = @DeliveredAt, 
                        DeliveredBy = @DeliveredBy,
                        DeliveryMethod = 'Approved for Delivery'
                    WHERE ChequeId IN @ChequeIds AND Status = 'Printed'";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    DeliveredAt = DateTime.UtcNow,
                    DeliveredBy = approvedBy
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                Logger.Info($"Successfully approved {rowsAffected} cheques for delivery");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error approving cheques for delivery: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Searches cheques by cheque number for review and delivery operations.
        /// </summary>
        /// <param name="chequeNumber">Cheque number to search for.</param>
        /// <returns>List of matching cheques.</returns>
        public async Task<List<Cheque>> SearchChequesByNumberAsync(string chequeNumber)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        ChequeId,
                        ChequeSeriesId,
                        ChequeNumber,
                        FiscalYear,
                        GrowerId,
                        PaymentBatchId,
                        ChequeDate,
                        ChequeAmount,
                        CurrencyCode,
                        ExchangeRate,
                        PayeeName,
                        Memo,
                        Status,
                        ClearedDate,
                        VoidedDate,
                        VoidedReason,
                        CreatedAt,
                        CreatedBy,
                        ModifiedAt,
                        ModifiedBy,
                        DeletedAt,
                        DeletedBy,
                        PrintedAt,
                        PrintedBy,
                        VoidedBy,
                        IssuedAt,
                        IssuedBy
                    FROM Cheques 
                    WHERE DeletedAt IS NULL 
                    AND ChequeNumber LIKE @ChequeNumber
                    AND Status IN ('Printed', 'Voided', 'Stopped')
                    ORDER BY ChequeDate DESC";
                var cheques = await connection.QueryAsync<Cheque>(sql, new { ChequeNumber = $"%{chequeNumber}%" });
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error searching cheques by number: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }


        // ==================================================================================
        // DATABASE CONNECTION (from BaseDatabaseService)
        // ==================================================================================
        public string ConnectionString => _connectionString;
    }
}
