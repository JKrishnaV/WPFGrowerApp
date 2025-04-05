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
        /// <param name="excludeGrowerIds">Optional: List of grower IDs to exclude.</param>
        /// <param name="excludePayGroupIds">Optional: List of pay group IDs to exclude.</param>
        /// <param name="productIds">Optional: List of product IDs to include (if empty, include all).</param>
        /// <param name="processIds">Optional: List of process IDs to include (if empty, include all).</param>
        /// <param name="progress">Optional: Progress reporting.</param>
        /// <returns>A tuple indicating success, a list of any errors encountered, and the created PostBatch.</returns>
        Task<(bool Success, List<string> Errors, PostBatch CreatedBatch)> ProcessAdvancePaymentRunAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            // Removed includeGrowerId and includePayGroup
            List<decimal> excludeGrowerIds = null,
            List<string> excludePayGroupIds = null,
            List<string> productIds = null,
            List<string> processIds = null,
            IProgress<string> progress = null);

        // Add other payment-related methods as needed, e.g.:
        // Task<List<PaymentDetail>> GetPaymentDetailsForChequeAsync(decimal chequeNumber, string series);
        // Task<bool> ReversePaymentBatchAsync(decimal batchNumber);
    }
}
