using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models; // Assuming Receipt model might be needed

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations related to retrieving product pricing information.
    /// </summary>
    public interface IPriceService
    {
        /// <summary>
        /// Gets the applicable price for a specific advance payment for a given receipt's details.
        /// </summary>
        /// <param name="productId">The product ID.</param>
        /// <param name="processId">The process ID.</param>
        /// <param name="receiptDate">The date of the receipt.</param>
        /// <param name="advanceNumber">The advance number (1, 2, or 3).</param>
        /// <returns>The price per unit for the specified advance, or 0 if not found.</returns>
        Task<decimal> GetAdvancePriceAsync(string productId, string processId, DateTime receiptDate, int advanceNumber);

        /// <summary>
        /// Gets the marketing deduction rate for a given product.
        /// </summary>
        /// <param name="productId">The product ID.</param>
        /// <returns>The deduction rate, or 0 if not applicable.</returns>
        Task<decimal> GetMarketingDeductionAsync(string productId);

        /// <summary>
        /// Gets any applicable time-based premium for a receipt.
        /// </summary>
        /// <param name="productId">The product ID.</param>
        /// <param name="processId">The process ID.</param>
        /// <param name="receiptDate">The date of the receipt.</param>
        /// <param name="receiptTime">The time of the receipt.</param>
        /// <returns>The premium amount per unit, or 0 if not applicable.</returns>
        Task<decimal> GetTimePremiumAsync(string productId, string processId, DateTime receiptDate, TimeSpan receiptTime);

        /// <summary>
        /// Marks a specific advance price record as used for a given batch.
        /// (Mirrors the Price->advN_used logic from XBase++)
        /// </summary>
        /// <param name="priceId">The unique ID of the Price record.</param>
        /// <param name="advanceNumber">The advance number (1, 2, or 3) being marked.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> MarkAdvancePriceAsUsedAsync(decimal priceId, int advanceNumber);

        /// <summary>
        /// Finds the relevant Price record ID for a given receipt context.
        /// </summary>
        /// <param name="productId">The product ID.</param>
        /// <param name="processId">The process ID.</param>
        /// <param name="receiptDate">The date of the receipt.</param>
        /// <returns>The PriceID (PRICEID column) or 0 if not found.</returns>
        Task<decimal> FindPriceRecordIdAsync(string productId, string processId, DateTime receiptDate);

        Task<IEnumerable<Price>> GetAllAsync();
        Task<Price> GetByIdAsync(int id);
        Task<int> CreateAsync(Price price);
        Task<bool> UpdateAsync(Price price);
        Task<bool> DeleteAsync(int id);
    }
}
