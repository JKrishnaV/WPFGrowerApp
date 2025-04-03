using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models; // Assuming models like Grower, PostBatch etc. might be needed

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations related to processing and managing grower payments.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Initiates and processes a payment run for a specific advance type.
        /// </summary>
        /// <param name="advanceNumber">The advance number (1, 2, or 3).</param>
        /// <param name="paymentDate">The date to assign to the payment transactions.</param>
        /// <param name="cutoffDate">The cutoff date for including receipts.</param>
        /// <param name="cropYear">The crop year for the payment.</param>
        /// <param name="includeGrowerId">Optional: Process only this grower ID.</param>
        /// <param name="includePayGroup">Optional: Process only this pay group.</param>
        /// <param name="excludeGrowerId">Optional: Exclude this grower ID.</param>
        /// <param name="excludePayGroup">Optional: Exclude this pay group.</param>
        /// <param name="productId">Optional: Filter by product ID.</param>
        /// <param name="processId">Optional: Filter by process ID.</param>
        /// <param name="progress">Optional: Progress reporting.</param>
        /// <returns>A tuple indicating success and a list of any errors encountered.</returns>
        Task<(bool Success, List<string> Errors, PostBatch CreatedBatch)> ProcessAdvancePaymentRunAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            decimal? includeGrowerId = null,
            string includePayGroup = null,
            decimal? excludeGrowerId = null,
            string excludePayGroup = null,
            string productId = null,
            string processId = null,
            IProgress<string> progress = null);

        // Add other payment-related methods as needed, e.g.:
        // Task<List<PaymentDetail>> GetPaymentDetailsForChequeAsync(decimal chequeNumber, string series);
        // Task<bool> ReversePaymentBatchAsync(decimal batchNumber);
    }
}
