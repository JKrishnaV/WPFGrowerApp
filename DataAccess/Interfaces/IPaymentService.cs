using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models; // Assuming models like Grower, PostBatch etc. might be needed
using WPFGrowerApp.Models; // Added for TestRunResult, GrowerPaymentSummary

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations related to processing and managing grower payments.
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Initiates and processes a payment run for a specific advance type.
        /// SUPPORTS UNLIMITED ADVANCES: Works for any advance number (1, 2, 3, 4, 5, ...)
        /// </summary>
        /// <param name="advanceNumber">The advance number (1, 2, 3, 4, 5, ...) - unlimited!</param>
        /// <param name="paymentDate">The date to assign to the payment transactions.</param>
        /// <param name="cutoffDate">The cutoff date for including receipts.</param>
        /// <param name="cropYear">The crop year for the payment.</param>
        /// <param name="excludeGrowerIds">Optional: List of grower IDs to exclude.</param>
        /// <param name="excludePayGroupIds">Optional: List of pay group IDs to exclude.</param>
        /// <param name="productIds">Optional: List of product IDs to include (if empty, include all).</param>
        /// <param name="processIds">Optional: List of process IDs to include (if empty, include all).</param>
        /// <param name="progress">Optional: Progress reporting.</param>
        /// <returns>A tuple indicating success, a list of any errors encountered, and the created PaymentBatch (null if error).</returns>
        Task<(bool Success, List<string> Errors, PaymentBatch? CreatedBatch)> ProcessAdvancePaymentRunAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            // Removed includeGrowerId and includePayGroup
            List<int>? excludeGrowerIds = null,
            List<string>? excludePayGroupIds = null,
            List<int>? productIds = null,
            List<int>? processIds = null,
            IProgress<string>? progress = null);

        /// <summary>
        /// Performs a test run simulation of an advance payment run without committing changes.
        /// SUPPORTS UNLIMITED ADVANCES: Works for any advance number (1, 2, 3, 4, 5, ...)
        /// </summary>
        /// <param name="advanceNumber">The advance number (1, 2, 3, 4, 5, ...) - unlimited!</param>
        /// <param name="paymentDate">The date to simulate payment transactions.</param>
        /// <param name="cutoffDate">The cutoff date for including receipts.</param>
        /// <param name="cropYear">The crop year for the payment.</param>
        /// <param name="excludeGrowerIds">Optional: List of grower IDs to exclude.</param>
        /// <param name="excludePayGroupIds">Optional: List of pay group IDs to exclude.</param>
        /// <param name="productIds">Optional: List of product IDs to include (if empty, include all).</param>
        /// <param name="processIds">Optional: List of process IDs to include (if empty, include all).</param>
        /// <param name="progress">Optional: Progress reporting.</param>
        /// <returns>A TestRunResult object containing the input parameters and calculated payment details.</returns>
        Task<TestRunResult> PerformAdvancePaymentTestRunAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            List<int>? excludeGrowerIds = null,
            List<string>? excludePayGroupIds = null,
            List<int>? productIds = null,
            List<int>? processIds = null,
            IProgress<string>? progress = null);

        /// <summary>
        /// Creates a payment draft (Draft status) with calculated totals but no posting/finalizing.
        /// NEW: Split workflow - creates draft for review and approval with validation.
        /// </summary>
        /// <param name="advanceNumber">The advance number (1, 2, 3, 4, 5, ...)</param>
        /// <param name="paymentDate">The date to assign to the payment transactions.</param>
        /// <param name="cutoffDate">The cutoff date for including receipts.</param>
        /// <param name="cropYear">The crop year for the payment.</param>
        /// <param name="excludeGrowerIds">Optional: List of grower IDs to exclude.</param>
        /// <param name="excludePayGroupIds">Optional: List of pay group IDs to exclude.</param>
        /// <param name="productIds">Optional: List of product IDs to include (if empty, include all).</param>
        /// <param name="processIds">Optional: List of process IDs to include (if empty, include all).</param>
        /// <param name="progress">Optional: Progress reporting.</param>
        /// <returns>A tuple indicating success, errors, created PaymentBatch, preview results, and validation results.</returns>
        Task<(bool Success, List<string> Errors, PaymentBatch? CreatedBatch, TestRunResult? PreviewResult, PaymentValidationResult? ValidationResult)> CreatePaymentDraftAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            List<int>? excludeGrowerIds = null,
            List<string>? excludePayGroupIds = null,
            List<int>? productIds = null,
            List<int>? processIds = null,
            IProgress<string>? progress = null);

        /// <summary>
        /// Creates payment draft after user confirmation - skips validation, uses existing calculation.
        /// Only creates allocations for valid receipts.
        /// </summary>
        /// <param name="advanceNumber">The advance number</param>
        /// <param name="paymentDate">Payment date</param>
        /// <param name="cutoffDate">Cutoff date</param>
        /// <param name="cropYear">Crop year</param>
        /// <param name="excludeGrowerIds">Excluded growers</param>
        /// <param name="excludePayGroupIds">Excluded pay groups</param>
        /// <param name="productIds">Product filter</param>
        /// <param name="processIds">Process filter</param>
        /// <param name="existingCalculation">Pre-calculated results from validation phase</param>
        /// <param name="progress">Progress reporting</param>
        /// <returns>A tuple indicating success, errors, created PaymentBatch, and preview results.</returns>
        Task<(bool Success, List<string> Errors, PaymentBatch? CreatedBatch, TestRunResult? PreviewResult)> CreatePaymentDraftConfirmedAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            List<int>? excludeGrowerIds,
            List<string>? excludePayGroupIds,
            List<int>? productIds,
            List<int>? processIds,
            TestRunResult existingCalculation,
            IProgress<string>? progress = null);

        // Add other payment-related methods as needed, e.g.:
        // Task<List<PaymentDetail>> GetPaymentDetailsForChequeAsync(decimal chequeNumber, string series);
        // Task<bool> ReversePaymentBatchAsync(decimal batchNumber);

        /// <summary>
        /// Gets grower-level payment summary for a specific payment batch
        /// </summary>
        Task<List<GrowerPaymentSummary>> GetGrowerPaymentsForBatchAsync(int batchId);

        /// <summary>
        /// Gets all receipt allocations for a specific payment batch with full details
        /// </summary>
        Task<List<ReceiptPaymentAllocation>> GetBatchAllocationsAsync(int batchId);

        /// <summary>
        /// Calculate analytics and statistics for a payment batch
        /// </summary>
        Task<PaymentBatchAnalytics> CalculateBatchAnalyticsAsync(int batchId);
    }
}
