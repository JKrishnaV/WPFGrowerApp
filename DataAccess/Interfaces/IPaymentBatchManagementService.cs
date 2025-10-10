using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service for managing payment batch lifecycle
    /// </summary>
    public interface IPaymentBatchManagementService
    {
        // ==============================================================
        // BATCH CRUD OPERATIONS
        // ==============================================================

        /// <summary>
        /// Create a new payment batch
        /// </summary>
        /// <param name="paymentTypeId">Payment type (Advance 1, 2, 3, Final, etc.)</param>
        /// <param name="batchDate">Batch/payment date</param>
        /// <param name="cropYear">Crop year</param>
        /// <param name="cutoffDate">Include receipts up to this date</param>
        /// <param name="notes">Optional notes</param>
        /// <param name="createdBy">User creating the batch</param>
        /// <returns>Created payment batch</returns>
        Task<PaymentBatch> CreatePaymentBatchAsync(
            int paymentTypeId,
            DateTime batchDate,
            int cropYear,
            DateTime? cutoffDate = null,
            string? notes = null,
            string? createdBy = null);

        /// <summary>
        /// Update an existing payment batch
        /// </summary>
        Task<bool> UpdatePaymentBatchAsync(PaymentBatch batch, string modifiedBy);

        /// <summary>
        /// Get payment batch by ID
        /// </summary>
        Task<PaymentBatch?> GetPaymentBatchByIdAsync(int paymentBatchId);

        /// <summary>
        /// Get all payment batches (optionally filtered by year or status)
        /// </summary>
        Task<List<PaymentBatch>> GetAllPaymentBatchesAsync(
            int? cropYear = null,
            string? status = null,
            int? paymentTypeId = null);

        // ==============================================================
        // BATCH STATUS MANAGEMENT
        // ==============================================================

        /// <summary>
        /// Update batch status
        /// </summary>
        /// <param name="paymentBatchId">Batch ID</param>
        /// <param name="newStatus">New status (Draft, Posted, Finalized, Voided)</param>
        /// <param name="modifiedBy">User making the change</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateBatchStatusAsync(
            int paymentBatchId,
            string newStatus,
            string modifiedBy);

        /// <summary>
        /// Mark batch as posted
        /// </summary>
        Task<bool> MarkBatchAsPostedAsync(
            int paymentBatchId,
            int totalGrowers,
            decimal totalAmount,
            int totalReceipts,
            string postedBy);

        /// <summary>
        /// Approve a payment batch (Draft → Posted)
        /// </summary>
        /// <param name="paymentBatchId">Batch ID</param>
        /// <param name="approvedBy">User approving the batch</param>
        /// <returns>True if successful</returns>
        Task<bool> ApproveBatchAsync(int paymentBatchId, string approvedBy);

        /// <summary>
        /// Process payments for a batch (Posted → Finalized)
        /// </summary>
        /// <param name="paymentBatchId">Batch ID</param>
        /// <param name="processedBy">User processing the payments</param>
        /// <returns>True if successful</returns>
        Task<bool> ProcessPaymentsAsync(int paymentBatchId, string processedBy);

        /// <summary>
        /// Rollback/undo a Draft or Posted batch - voids allocations and marks batch as voided (with transaction)
        /// </summary>
        /// <param name="paymentBatchId">Batch ID</param>
        /// <param name="reason">Reason for rollback</param>
        /// <param name="rolledBackBy">User performing rollback</param>
        /// <returns>True if successful</returns>
        Task<bool> RollbackBatchAsync(int paymentBatchId, string reason, string rolledBackBy);

        /// <summary>
        /// Void a payment batch (soft delete)
        /// </summary>
        /// <param name="paymentBatchId">Batch ID</param>
        /// <param name="reason">Reason for voiding</param>
        /// <param name="voidedBy">User voiding the batch</param>
        /// <returns>True if successful</returns>
        Task<bool> VoidBatchAsync(
            int paymentBatchId,
            string reason,
            string voidedBy);

        // ==============================================================
        // BATCH SUMMARIES & REPORTING
        // ==============================================================

        /// <summary>
        /// Get summary information for a payment batch
        /// </summary>
        Task<PaymentBatchSummary> GetBatchSummaryAsync(int paymentBatchId);

        /// <summary>
        /// Get batch statistics for a crop year
        /// </summary>
        Task<BatchStatistics> GetBatchStatisticsAsync(int cropYear);

        // ==============================================================
        // BATCH NUMBER GENERATION
        // ==============================================================

        /// <summary>
        /// Generate next batch number for a payment type and year
        /// </summary>
        /// <param name="paymentTypeId">Payment type ID</param>
        /// <param name="cropYear">Crop year</param>
        /// <returns>Next batch number (e.g., "ADV1-2025-001")</returns>
        Task<string> GenerateNextBatchNumberAsync(int paymentTypeId, int cropYear);
    }

    // ==============================================================
    // SUPPORTING MODELS
    // ==============================================================

    /// <summary>
    /// Summary information for a payment batch
    /// </summary>
    public class PaymentBatchSummary
    {
        public int PaymentBatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string PaymentTypeName { get; set; } = string.Empty;
        public DateTime BatchDate { get; set; }
        public int CropYear { get; set; }
        public string Status { get; set; } = string.Empty;

        // Totals
        public int TotalGrowers { get; set; }
        public int TotalReceipts { get; set; }
        public decimal TotalAmount { get; set; }
        public int ChequesGenerated { get; set; }

        // Grower breakdown
        public int ActiveGrowers { get; set; }
        public int OnHoldGrowers { get; set; }

        // Dates
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? PostedAt { get; set; }
        public string? PostedBy { get; set; }

        // Details
        public string? Notes { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Statistics for batches in a crop year
    /// </summary>
    public class BatchStatistics
    {
        public int CropYear { get; set; }
        public int TotalBatches { get; set; }
        public int DraftBatches { get; set; }
        public int PostedBatches { get; set; }
        public int VoidedBatches { get; set; }

        // By payment type
        public int Advance1Batches { get; set; }
        public int Advance2Batches { get; set; }
        public int Advance3Batches { get; set; }
        public int FinalBatches { get; set; }
        public int SpecialBatches { get; set; }

        // Totals
        public decimal TotalAmountPaid { get; set; }
        public int TotalChequesIssued { get; set; }
    }
}


