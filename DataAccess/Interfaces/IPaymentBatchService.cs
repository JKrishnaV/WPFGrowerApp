using System;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations related to managing payment batches (PaymentBatches table).
    /// </summary>
    public interface IPaymentBatchService
    {
        /// <summary>
        /// Creates a new payment batch record.
        /// </summary>
        /// <param name="paymentTypeId">The type of payment (1=ADV1, 2=ADV2, 3=ADV3, 4=FINAL).</param>
        /// <param name="batchDate">The date the batch was created.</param>
        /// <param name="cropYear">The crop year for the batch.</param>
        /// <param name="notes">Optional notes for the batch.</param>
        /// <returns>The newly created PaymentBatch object with its assigned ID.</returns>
        Task<PaymentBatch> CreatePaymentBatchAsync(int paymentTypeId, DateTime batchDate, int cropYear, string notes = null);

        /// <summary>
        /// Creates a new payment batch record (transaction-aware overload).
        /// </summary>
        Task<PaymentBatch> CreatePaymentBatchAsync(int paymentTypeId, DateTime batchDate, int cropYear, string notes, SqlConnection connection, SqlTransaction transaction);

        /// <summary>
        /// Retrieves a PaymentBatch record by its ID.
        /// </summary>
        /// <param name="paymentBatchId">The ID of the batch to retrieve.</param>
        /// <returns>The PaymentBatch object or null if not found.</returns>
        Task<PaymentBatch> GetPaymentBatchByIdAsync(int paymentBatchId);

        /// <summary>
        /// Updates the totals for a payment batch after processing and changes status to Posted.
        /// </summary>
        /// <param name="paymentBatchId">The ID of the batch to update.</param>
        /// <param name="totalGrowers">Total number of growers in the batch.</param>
        /// <param name="totalReceipts">Total number of receipts processed.</param>
        /// <param name="totalAmount">Total amount allocated.</param>
        /// <returns>True if update was successful.</returns>
        Task<bool> UpdatePaymentBatchTotalsAsync(int paymentBatchId, int totalGrowers, int totalReceipts, decimal totalAmount);

        /// <summary>
        /// Updates only the totals for a payment batch without changing the status.
        /// </summary>
        /// <param name="paymentBatchId">The ID of the batch to update.</param>
        /// <param name="totalGrowers">Total number of growers in the batch.</param>
        /// <param name="totalReceipts">Total number of receipts processed.</param>
        /// <param name="totalAmount">Total amount allocated.</param>
        /// <returns>True if update was successful.</returns>
        Task<bool> UpdatePaymentBatchTotalsOnlyAsync(int paymentBatchId, int totalGrowers, int totalReceipts, decimal totalAmount);

        /// <summary>
        /// Updates only the totals for a payment batch without changing the status (transaction-aware overload).
        /// </summary>
        Task<bool> UpdatePaymentBatchTotalsOnlyAsync(int paymentBatchId, int totalGrowers, int totalReceipts, decimal totalAmount, SqlConnection connection, SqlTransaction transaction);

        /// <summary>
        /// Approves a payment batch (Draft → Posted).
        /// </summary>
        /// <param name="paymentBatchId">The ID of the batch to approve.</param>
        /// <param name="approvedBy">The user who approved the batch.</param>
        /// <returns>True if update was successful.</returns>
        Task<bool> ApproveBatchAsync(int paymentBatchId, string approvedBy);

        /// <summary>
        /// Processes payments for a batch (Posted → Finalized).
        /// </summary>
        /// <param name="paymentBatchId">The ID of the batch to process payments for.</param>
        /// <param name="processedBy">The user who processed the payments.</param>
        /// <returns>True if update was successful.</returns>
        Task<bool> ProcessPaymentsAsync(int paymentBatchId, string processedBy);

        /// <summary>
        /// Marks a payment batch as processed.
        /// </summary>
        /// <param name="paymentBatchId">The ID of the batch to mark as processed.</param>
        /// <param name="processedBy">The user who processed the batch.</param>
        /// <returns>True if update was successful.</returns>
        Task<bool> MarkBatchAsProcessedAsync(int paymentBatchId, string processedBy);
    }
}
