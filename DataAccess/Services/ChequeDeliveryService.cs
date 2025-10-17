using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing cheque delivery operations.
    /// </summary>
    public class ChequeDeliveryService : BaseDatabaseService, IChequeDeliveryService
    {
        public ChequeDeliveryService()
        {
        }

        public async Task<ChequeDelivery> CreateDeliveryAsync(ChequeDelivery delivery)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ChequeDeliveries (
                        ChequeId, MailedDate, TrackingNumber, DeliveryMethod, 
                        Status, DeliveredDate, DeliveredBy, ReceivedBy, Notes,
                        CreatedAt, CreatedBy
                    )
                    VALUES (
                        @ChequeId, @MailedDate, @TrackingNumber, @DeliveryMethod,
                        @Status, @DeliveredDate, @DeliveredBy, @ReceivedBy, @Notes,
                        @CreatedAt, @CreatedBy
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                var deliveryId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    ChequeId = delivery.ChequeId,
                    MailedDate = delivery.MailedDate,
                    TrackingNumber = delivery.TrackingNumber,
                    DeliveryMethod = delivery.DeliveryMethod,
                    Status = delivery.Status,
                    DeliveredDate = delivery.DeliveredDate,
                    DeliveredBy = delivery.DeliveredBy,
                    ReceivedBy = delivery.ReceivedBy,
                    Notes = delivery.Notes,
                    CreatedAt = DateTime.Now,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                });

                delivery.DeliveryId = deliveryId;
                Logger.Info($"Created cheque delivery record for cheque {delivery.ChequeId}, delivery ID: {deliveryId}");
                return delivery;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating cheque delivery record: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateDeliveryStatusAsync(int deliveryId, string newStatus, string updatedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE ChequeDeliveries 
                    SET Status = @Status, ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy
                    WHERE DeliveryId = @DeliveryId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    Status = newStatus,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = updatedBy,
                    DeliveryId = deliveryId
                });

                Logger.Info($"Updated delivery status for delivery {deliveryId} to {newStatus}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating delivery status: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<ChequeDelivery>> GetDeliveriesByChequeIdAsync(int chequeId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT cd.*, c.ChequeNumber, c.GrowerName
                    FROM ChequeDeliveries cd
                    INNER JOIN Cheques c ON cd.ChequeId = c.ChequeId
                    WHERE cd.ChequeId = @ChequeId
                    ORDER BY cd.CreatedAt DESC";

                var deliveries = await connection.QueryAsync<ChequeDelivery>(sql, new { ChequeId = chequeId });
                return deliveries.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving deliveries for cheque {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<ChequeDelivery>> GetPendingDeliveriesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT cd.*, c.ChequeNumber, c.GrowerName
                    FROM ChequeDeliveries cd
                    INNER JOIN Cheques c ON cd.ChequeId = c.ChequeId
                    WHERE cd.Status IN ('Mailed', 'In Transit')
                    ORDER BY cd.MailedDate DESC";

                var deliveries = await connection.QueryAsync<ChequeDelivery>(sql);
                return deliveries.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving pending deliveries: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<ChequeDelivery>> GetOverdueDeliveriesAsync(int daysOverdue)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var cutoffDate = DateTime.Now.AddDays(-daysOverdue);
                var sql = @"
                    SELECT cd.*, c.ChequeNumber, c.GrowerName
                    FROM ChequeDeliveries cd
                    INNER JOIN Cheques c ON cd.ChequeId = c.ChequeId
                    WHERE cd.Status = 'In Transit' 
                    AND cd.MailedDate < @CutoffDate
                    ORDER BY cd.MailedDate ASC";

                var deliveries = await connection.QueryAsync<ChequeDelivery>(sql, new { CutoffDate = cutoffDate });
                return deliveries.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving overdue deliveries: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> MarkAsDeliveredAsync(int deliveryId, DateTime deliveredDate, string receivedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE ChequeDeliveries 
                    SET Status = 'Delivered', DeliveredDate = @DeliveredDate, 
                        ReceivedBy = @ReceivedBy, ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy
                    WHERE DeliveryId = @DeliveryId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    DeliveredDate = deliveredDate,
                    ReceivedBy = receivedBy,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = App.CurrentUser?.Username ?? "SYSTEM",
                    DeliveryId = deliveryId
                });

                Logger.Info($"Marked delivery {deliveryId} as delivered on {deliveredDate:yyyy-MM-dd}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking delivery as delivered: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<ChequeDelivery>> GetAllDeliveriesAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT cd.*, c.ChequeNumber, c.GrowerName
                    FROM ChequeDeliveries cd
                    INNER JOIN Cheques c ON cd.ChequeId = c.ChequeId
                    ORDER BY cd.CreatedAt DESC";

                var deliveries = await connection.QueryAsync<ChequeDelivery>(sql);
                return deliveries.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving all deliveries: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Dictionary<string, int>> GetDeliveryStatisticsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT Status, COUNT(*) as Count
                    FROM ChequeDeliveries
                    GROUP BY Status";

                var results = await connection.QueryAsync(sql);
                return results.ToDictionary(r => (string)r.Status, r => (int)r.Count);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving delivery statistics: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateTrackingInfoAsync(int deliveryId, string trackingNumber, string notes, string updatedBy)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE ChequeDeliveries 
                    SET TrackingNumber = @TrackingNumber, Notes = @Notes, 
                        ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy
                    WHERE DeliveryId = @DeliveryId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    TrackingNumber = trackingNumber,
                    Notes = notes,
                    ModifiedAt = DateTime.Now,
                    ModifiedBy = updatedBy,
                    DeliveryId = deliveryId
                });

                Logger.Info($"Updated tracking info for delivery {deliveryId}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating tracking info: {ex.Message}", ex);
                throw;
            }
        }
    }
}
