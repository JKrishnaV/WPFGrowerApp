﻿﻿﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
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
        Task<bool> ValidateReceiptAsync(Receipt receipt);
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
        /// Gets all payment allocations for a specific receipt.
        /// </summary>
        Task<List<ReceiptPaymentAllocation>> GetReceiptPaymentAllocationsAsync(int receiptId);

        /// <summary>
        /// Gets a payment summary for a receipt showing total paid and breakdown by advance.
        /// </summary>
        Task<ReceiptPaymentSummary?> GetReceiptPaymentSummaryAsync(int receiptId);
    }
}
