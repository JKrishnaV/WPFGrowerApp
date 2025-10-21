using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for managing advance cheques and their deductions
    /// </summary>
    public interface IAdvanceChequeService
    {
        /// <summary>
        /// Creates a new advance cheque for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="amount">The advance amount</param>
        /// <param name="reason">The reason for the advance</param>
        /// <param name="createdBy">The user creating the advance</param>
        /// <returns>The created advance cheque</returns>
        Task<AdvanceCheque> CreateAdvanceChequeAsync(int growerId, decimal amount, string reason, string createdBy);

        /// <summary>
        /// Gets all outstanding advance cheques for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <returns>List of outstanding advance cheques</returns>
        Task<List<AdvanceCheque>> GetOutstandingAdvancesAsync(int growerId);

        /// <summary>
        /// Calculates the total outstanding advance amount for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <returns>Total outstanding advance amount</returns>
        Task<decimal> CalculateTotalOutstandingAdvancesAsync(int growerId);

        /// <summary>
        /// Gets an advance cheque by ID
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <returns>The advance cheque or null if not found</returns>
        Task<AdvanceCheque> GetAdvanceChequeByIdAsync(int advanceChequeId);

        /// <summary>
        /// Gets all advance cheques with optional status filter
        /// </summary>
        /// <param name="status">Optional status filter (Active, Deducted, Cancelled)</param>
        /// <returns>List of advance cheques</returns>
        Task<List<AdvanceCheque>> GetAllAdvanceChequesAsync(string status = null);

        /// <summary>
        /// Cancels an advance cheque
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="reason">The reason for cancellation</param>
        /// <param name="cancelledBy">The user cancelling the advance</param>
        /// <returns>True if successful</returns>
        Task<bool> CancelAdvanceChequeAsync(int advanceChequeId, string reason, string cancelledBy);

        /// <summary>
        /// Gets advance cheques for a specific grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of advance cheques for the grower</returns>
        Task<List<AdvanceCheque>> GetAdvanceChequesByGrowerAsync(int growerId, string status = null);

        /// <summary>
        /// Gets advance cheques within a date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of advance cheques in the date range</returns>
        Task<List<AdvanceCheque>> GetAdvanceChequesByDateRangeAsync(DateTime startDate, DateTime endDate, string status = null);

        /// <summary>
        /// Updates an advance cheque
        /// </summary>
        /// <param name="advanceCheque">The advance cheque to update</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateAdvanceChequeAsync(AdvanceCheque advanceCheque);

        /// <summary>
        /// Gets the deduction history for an advance cheque
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <returns>List of deduction records</returns>
        Task<List<AdvanceDeduction>> GetDeductionHistoryAsync(int advanceChequeId);

        /// <summary>
        /// Checks if a grower has any outstanding advances
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <returns>True if grower has outstanding advances</returns>
        Task<bool> HasOutstandingAdvancesAsync(int growerId);

        /// <summary>
        /// Print an advance cheque (Generated -> Printed)
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="printedBy">The user printing the cheque</param>
        /// <returns>True if successful</returns>
        Task<bool> PrintAdvanceChequeAsync(int advanceChequeId, string printedBy);

        /// <summary>
        /// Deliver an advance cheque (Printed -> Delivered)
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="deliveredBy">The user delivering the cheque</param>
        /// <param name="deliveryMethod">The delivery method</param>
        /// <returns>True if successful</returns>
        Task<bool> DeliverAdvanceChequeAsync(int advanceChequeId, string deliveredBy, string deliveryMethod);

        /// <summary>
        /// Void an advance cheque (Generated/Printed -> Voided)
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="voidedBy">The user voiding the cheque</param>
        /// <param name="voidedReason">The reason for voiding</param>
        /// <returns>True if successful</returns>
        Task<bool> VoidAdvanceChequeAsync(int advanceChequeId, string voidedBy, string voidedReason);
    }
}
