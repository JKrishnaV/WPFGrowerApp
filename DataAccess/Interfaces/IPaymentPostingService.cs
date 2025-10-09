using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service for posting payment batches to accounts and updating receipt tracking
    /// </summary>
    public interface IPaymentPostingService
    {
        // ==============================================================
        // POST OPERATIONS
        // ==============================================================

        /// <summary>
        /// Post an advance payment batch (complete workflow)
        /// </summary>
        /// <param name="paymentBatchId">Payment batch ID</param>
        /// <param name="growerPayments">Calculated grower payments</param>
        /// <param name="postedBy">User posting the batch</param>
        /// <returns>Posting result with success/failure details</returns>
        Task<PostingResult> PostAdvancePaymentBatchAsync(
            int paymentBatchId,
            List<TestRunGrowerPayment> growerPayments,
            string postedBy);

        /// <summary>
        /// Post a final payment batch
        /// </summary>
        Task<PostingResult> PostFinalPaymentBatchAsync(
            int paymentBatchId,
            List<GrowerFinalPayment> growerPayments,
            string postedBy);

        // ==============================================================
        // ACCOUNT TRANSACTIONS
        // ==============================================================

        /// <summary>
        /// Create account transactions for receipts in a payment
        /// </summary>
        /// <param name="growerPayment">Grower payment details</param>
        /// <param name="chequeId">Generated cheque ID</param>
        /// <param name="paymentBatchId">Payment batch ID</param>
        /// <param name="paymentTypeId">Payment type ID</param>
        /// <returns>True if successful</returns>
        Task<bool> CreateAccountTransactionsAsync(
            TestRunGrowerPayment growerPayment,
            int chequeId,
            int paymentBatchId,
            int paymentTypeId);

        /// <summary>
        /// Create receipt payment allocations (links receipts to payments)
        /// </summary>
        Task<bool> CreateReceiptPaymentAllocationsAsync(
            List<TestRunReceiptDetail> receiptDetails,
            int paymentBatchId,
            int paymentTypeId);

        // ==============================================================
        // RECEIPT TRACKING
        // ==============================================================

        /// <summary>
        /// Mark receipts as paid for a specific advance
        /// </summary>
        /// <param name="receiptIds">List of receipt IDs</param>
        /// <param name="advanceNumber">Advance number (1, 2, 3) or 0 for final</param>
        /// <param name="paymentBatchId">Payment batch ID</param>
        /// <param name="chequeId">Cheque ID</param>
        /// <returns>True if successful</returns>
        Task<bool> MarkReceiptsAsPaidAsync(
            List<int> receiptIds,
            int advanceNumber,
            int paymentBatchId,
            int chequeId);

        // ==============================================================
        // REVERSAL OPERATIONS
        // ==============================================================

        /// <summary>
        /// Reverse a posted payment batch (unpost)
        /// </summary>
        /// <param name="paymentBatchId">Payment batch ID</param>
        /// <param name="reason">Reason for reversal</param>
        /// <param name="reversedBy">User reversing the batch</param>
        /// <returns>True if successful</returns>
        Task<bool> ReverseBatchPostingAsync(
            int paymentBatchId,
            string reason,
            string reversedBy);

        // ==============================================================
        // VALIDATION
        // ==============================================================

        /// <summary>
        /// Validate that a batch can be posted
        /// </summary>
        /// <param name="paymentBatchId">Payment batch ID</param>
        /// <returns>Validation result with any errors</returns>
        Task<ValidationResult> ValidateBatchForPostingAsync(int paymentBatchId);
    }

    // ==============================================================
    // SUPPORTING MODELS
    // ==============================================================

    /// <summary>
    /// Result of posting a payment batch
    /// </summary>
    public class PostingResult
    {
        public bool Success { get; set; }
        public int PaymentBatchId { get; set; }
        public int ChequesGenerated { get; set; }
        public int TransactionsCreated { get; set; }
        public int ReceiptsUpdated { get; set; }
        public decimal TotalAmount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime PostedAt { get; set; }
        public string PostedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of batch validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}


