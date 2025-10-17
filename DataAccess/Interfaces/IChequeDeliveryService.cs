using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for managing cheque delivery operations.
    /// Handles delivery tracking, status updates, and delivery management.
    /// </summary>
    public interface IChequeDeliveryService : IDatabaseService
    {
        /// <summary>
        /// Creates a new cheque delivery record.
        /// </summary>
        /// <param name="delivery">The delivery information to create.</param>
        /// <returns>The created delivery record with assigned ID.</returns>
        Task<ChequeDelivery> CreateDeliveryAsync(ChequeDelivery delivery);

        /// <summary>
        /// Updates the status of a cheque delivery.
        /// </summary>
        /// <param name="deliveryId">The ID of the delivery to update.</param>
        /// <param name="newStatus">The new status to set.</param>
        /// <param name="updatedBy">The user updating the status.</param>
        /// <returns>True if the update was successful.</returns>
        Task<bool> UpdateDeliveryStatusAsync(int deliveryId, string newStatus, string updatedBy);

        /// <summary>
        /// Gets all delivery records for a specific cheque.
        /// </summary>
        /// <param name="chequeId">The ID of the cheque.</param>
        /// <returns>List of delivery records for the cheque.</returns>
        Task<List<ChequeDelivery>> GetDeliveriesByChequeIdAsync(int chequeId);

        /// <summary>
        /// Gets all pending delivery records.
        /// </summary>
        /// <returns>List of deliveries that are not yet completed.</returns>
        Task<List<ChequeDelivery>> GetPendingDeliveriesAsync();

        /// <summary>
        /// Gets deliveries that are overdue based on the specified number of days.
        /// </summary>
        /// <param name="daysOverdue">Number of days to consider as overdue.</param>
        /// <returns>List of overdue deliveries.</returns>
        Task<List<ChequeDelivery>> GetOverdueDeliveriesAsync(int daysOverdue);

        /// <summary>
        /// Marks a delivery as delivered with delivery details.
        /// </summary>
        /// <param name="deliveryId">The ID of the delivery.</param>
        /// <param name="deliveredDate">The date the delivery was completed.</param>
        /// <param name="receivedBy">The person who received the cheque.</param>
        /// <returns>True if the update was successful.</returns>
        Task<bool> MarkAsDeliveredAsync(int deliveryId, DateTime deliveredDate, string receivedBy);

        /// <summary>
        /// Gets delivery statistics for reporting.
        /// </summary>
        /// <returns>Summary of delivery statuses and counts.</returns>
        Task<Dictionary<string, int>> GetDeliveryStatisticsAsync();

        /// <summary>
        /// Updates delivery tracking information.
        /// </summary>
        /// <param name="deliveryId">The ID of the delivery.</param>
        /// <param name="trackingNumber">The tracking number.</param>
        /// <param name="notes">Additional notes.</param>
        /// <param name="updatedBy">The user making the update.</param>
        /// <returns>True if the update was successful.</returns>
        Task<bool> UpdateTrackingInfoAsync(int deliveryId, string trackingNumber, string notes, string updatedBy);

        /// <summary>
        /// Gets all delivery records.
        /// </summary>
        /// <returns>List of all delivery records.</returns>
        Task<List<ChequeDelivery>> GetAllDeliveriesAsync();
    }
}
