using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IReceiptService
    {
        Task<List<Receipt>> GetReceiptsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Receipt> GetReceiptByNumberAsync(decimal receiptNumber);
        Task<Receipt> SaveReceiptAsync(Receipt receipt);
    Task<bool> DeleteReceiptAsync(string receiptNumber);
    Task<bool> VoidReceiptAsync(string receiptNumber, string reason);
    Task<List<Receipt>> GetReceiptsByGrowerAsync(string growerNumber, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Receipt>> GetReceiptsByImportBatchAsync(decimal impBatch);
        Task<decimal> GetNextReceiptNumberAsync();
        Task<decimal> CalculateNetWeightAsync(Receipt receipt);
        Task<decimal> GetPriceForReceiptAsync(Receipt receipt);

        /// <summary>
        /// Applies dockage to a receipt. Stores original net weight in OriNet before applying dockage percentage.
        /// Mirrors XBase logic: ori_net stores weight before dockage, net stores weight after dockage.
        /// Dockage calculation: new_net = ori_net * (1 - dock_pct/100)
        /// </summary>
        /// <param name="receipt">The receipt to apply dockage to</param>
        /// <returns>The adjusted net weight after dockage</returns>
        Task<decimal> ApplyDockageAsync(Receipt receipt);

        /// <summary>
        /// Calculates the actual dockage amount (weight lost due to quality deduction).
        /// Formula: dockage_amount = ori_net - net (if ori_net exists, else 0)
        /// </summary>
        /// <param name="receipt">The receipt to calculate dockage for</param>
        /// <returns>The dockage amount in weight units</returns>
        decimal CalculateDockageAmount(Receipt receipt);

        /// <summary>
        /// Updates a Daily record with advance payment details after calculation during a payment run.
        /// </summary>
        /// <param name="receiptNumber">The unique receipt number (RECPT).</param>
        /// <param name="advanceNumber">The advance number being processed (1, 2, or 3).</param>
        /// <param name="postBatchId">The ID of the posting batch.</param>
        /// <param name="advancePrice">The calculated price for this advance.</param>
        /// <param name="priceRecordId">The ID of the Price record used.</param>
        /// <param name="premiumPrice">The calculated time premium price, if applicable.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateReceiptAdvanceDetailsAsync(
            decimal receiptNumber,
            int advanceNumber,
            decimal postBatchId,
            decimal advancePrice,
            decimal priceRecordId,
            decimal premiumPrice);

        /// <summary>
        /// Retrieves eligible receipts for a specific advance payment run.
        /// </summary>
        /// <param name="advanceNumber">The advance number (1, 2, or 3).</param>
        /// <param name="cutoffDate">Include receipts up to this date.</param>
        /// <param name="includeGrowerIds">Optional: List of grower IDs to include (if empty, include all).</param> // Assuming include might also become multi-select later
        /// <param name="includePayGroupIds">Optional: List of pay group IDs to include (if empty, include all).</param> // Assuming include might also become multi-select later
        /// <param name="excludeGrowerIds">Optional: List of grower IDs to exclude.</param>
        /// <param name="excludePayGroupIds">Optional: List of pay group IDs to exclude.</param>
        /// <param name="productIds">Optional: List of product IDs to include (if empty, include all).</param>
        /// <param name="processIds">Optional: List of process IDs to include (if empty, include all).</param>
        /// <returns>A list of Receipt objects eligible for the payment run.</returns>
        Task<List<Receipt>> GetReceiptsForAdvancePaymentAsync(
            int advanceNumber,
            DateTime cutoffDate,
            List<int>? includeGrowerIds = null, // Changed to List<int>
            List<string>? includePayGroupIds = null, // Changed to List
            List<int>? excludeGrowerIds = null, // Changed to List<int>
            List<string>? excludePayGroupIds = null, // Changed to List
            List<int>? productIds = null, // Changed to List
            List<int>? processIds = null, // Changed to List
            int? cropYear = null); // Added cropYear parameter

        /// <summary>
        /// Creates a payment allocation record linking a receipt to a payment batch.
        /// </summary>
        Task CreateReceiptPaymentAllocationAsync(ReceiptPaymentAllocation allocation);

        /// <summary>
        /// Creates a payment allocation record (transaction-aware overload).
        /// </summary>
        Task CreateReceiptPaymentAllocationAsync(ReceiptPaymentAllocation allocation, SqlConnection connection, SqlTransaction transaction);


        /// <summary>
        /// Gets a payment summary for a receipt showing total paid and breakdown by advance.
        /// </summary>
        Task<ReceiptPaymentSummary?> GetReceiptPaymentSummaryAsync(int receiptId);

        // ======================================================================
        // NEW METHODS FOR ENHANCED RECEIPT MANAGEMENT
        // ======================================================================

        /// <summary>
        /// Get detailed receipt information with joined data
        /// </summary>
        /// <param name="receiptId">The receipt ID</param>
        /// <returns>Receipt detail DTO</returns>
        Task<ReceiptDetailDto?> GetReceiptDetailAsync(int receiptId);

        /// <summary>
        /// Get audit history for a receipt
        /// </summary>
        /// <param name="receiptId">The receipt ID</param>
        /// <returns>List of audit entries</returns>
        Task<List<ReceiptAuditEntry>> GetReceiptAuditHistoryAsync(int receiptId);

        /// <summary>
        /// Get payment allocations for a receipt
        /// </summary>
        /// <param name="receiptId">The receipt ID</param>
        /// <returns>List of payment allocations</returns>
        Task<List<ReceiptPaymentAllocation>> GetReceiptPaymentAllocationsAsync(int receiptId);

        /// <summary>
        /// Get related receipts (same grower, same date, or duplicates)
        /// </summary>
        /// <param name="receiptId">The receipt ID</param>
        /// <returns>List of related receipts</returns>
        Task<List<Receipt>> GetRelatedReceiptsAsync(int receiptId);

        /// <summary>
        /// Duplicate a receipt with new receipt number and date
        /// </summary>
        /// <param name="receiptId">The receipt ID to duplicate</param>
        /// <param name="createdBy">User creating the duplicate</param>
        /// <returns>New receipt</returns>
        Task<Receipt> DuplicateReceiptAsync(int receiptId, string createdBy);

        /// <summary>
        /// Mark a receipt as quality checked
        /// </summary>
        /// <param name="receiptId">The receipt ID</param>
        /// <param name="checkedBy">User performing quality check</param>
        /// <returns>True if successful</returns>
        Task<bool> MarkQualityCheckedAsync(int receiptId, string checkedBy);

        /// <summary>
        /// Validate receipt data comprehensively
        /// </summary>
        /// <param name="receipt">The receipt to validate</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateReceiptAsync(Receipt receipt);

        /// <summary>
        /// Get receipt analytics for a date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Receipt analytics</returns>
        Task<ReceiptAnalytics> GetReceiptAnalyticsAsync(DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Generate receipt PDF
        /// </summary>
        /// <param name="receiptId">The receipt ID</param>
        /// <returns>PDF byte array</returns>
        Task<byte[]> GenerateReceiptPdfAsync(int receiptId);

        /// <summary>
        /// Bulk void multiple receipts
        /// </summary>
        /// <param name="receiptIds">List of receipt IDs to void</param>
        /// <param name="reason">Void reason</param>
        /// <param name="voidedBy">User voiding receipts</param>
        /// <returns>True if successful</returns>
        Task<bool> BulkVoidReceiptsAsync(List<int> receiptIds, string reason, string voidedBy);

        /// <summary>
        /// Get receipts with advanced filtering
        /// </summary>
        /// <param name="filters">Filter criteria</param>
        /// <returns>Filtered receipts</returns>
        Task<List<Receipt>> GetReceiptsWithFiltersAsync(ReceiptFilters filters);
        Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsWithFiltersAndCountAsync(ReceiptFilters filters);

        /// <summary>
        /// Get receipt statistics for dashboard
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Receipt statistics</returns>
        Task<ReceiptStatistics> GetReceiptStatisticsAsync(DateTime? startDate, DateTime? endDate);

        // ======================================================================
        // PAYMENT PROTECTION AND RE-IMPORT METHODS
        // ======================================================================

        /// <summary>
        /// Checks if a receipt can be deleted (no payment allocations)
        /// </summary>
        /// <param name="receiptNumber">The receipt number to check</param>
        /// <returns>True if receipt can be deleted, false if it has payments</returns>
        Task<bool> CanDeleteReceiptAsync(string receiptNumber);

        /// <summary>
        /// Checks if a receipt has any payment allocations
        /// </summary>
        /// <param name="receiptId">The receipt ID to check</param>
        /// <returns>True if receipt has payment allocations</returns>
        Task<bool> HasPaymentAllocationsAsync(int receiptId);

        /// <summary>
        /// Undeletes a soft-deleted receipt (for re-import scenarios)
        /// </summary>
        /// <param name="receiptNumber">The receipt number to undelete</param>
        /// <returns>True if successful</returns>
        Task<bool> UndeleteReceiptAsync(string receiptNumber);

        /// <summary>
        /// Updates an existing receipt with new data (for re-import scenarios)
        /// </summary>
        /// <param name="receipt">The receipt with updated data</param>
        /// <returns>The updated receipt</returns>
        Task<Receipt> UpdateReceiptAsync(Receipt receipt);

        /// <summary>
        /// Get receipts with optimized search performance
        /// </summary>
        /// <param name="filters">Search and filter criteria</param>
        /// <returns>Tuple containing receipts and total count</returns>
        Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsWithOptimizedSearchAsync(ReceiptFilters filters);
    }

    /// <summary>
    /// Receipt filter criteria
    /// </summary>
    public class ReceiptFilters
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchText { get; set; }
        public bool? ShowVoided { get; set; }
        public int? ProductId { get; set; }
        public int? DepotId { get; set; }
        public int? GrowerId { get; set; }
        public string? CreatedBy { get; set; }
        public int? Grade { get; set; }
        public bool? IsQualityChecked { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
    }

    /// <summary>
    /// Receipt statistics for dashboard
    /// </summary>
    public class ReceiptStatistics
    {
        public int TotalReceipts { get; set; }
        public int ActiveReceipts { get; set; }
        public int VoidedReceipts { get; set; }
        public int QualityCheckedReceipts { get; set; }
        public decimal TotalGrossWeight { get; set; }
        public decimal TotalNetWeight { get; set; }
        public decimal TotalFinalWeight { get; set; }
        public decimal AverageDockPercentage { get; set; }
        public int UniqueGrowers { get; set; }
        public int UniqueProducts { get; set; }
        public int UniqueDepots { get; set; }
    }
}
