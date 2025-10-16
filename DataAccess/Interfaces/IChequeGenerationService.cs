using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service for generating, voiding, and managing cheques
    /// </summary>
    public interface IChequeGenerationService
    {
        // ==============================================================
        // CHEQUE GENERATION
        // ==============================================================

        /// <summary>
        /// Generate a cheque for a grower payment
        /// </summary>
        /// <param name="growerId">Grower ID</param>
        /// <param name="amount">Cheque amount</param>
        /// <param name="chequeDate">Date of cheque</param>
        /// <param name="paymentBatchId">Payment batch ID</param>
        /// <param name="paymentTypeId">Payment type (Advance 1, 2, 3, Final, etc.)</param>
        /// <param name="memo">Optional memo/note for cheque</param>
        /// <returns>Generated cheque</returns>
        Task<Cheque> GenerateChequeAsync(
            int growerId,
            decimal amount,
            DateTime chequeDate,
            int paymentBatchId,
            int paymentTypeId,
            string? memo = null);

        /// <summary>
        /// Generate cheques for all growers in a payment batch
        /// </summary>
        /// <param name="paymentBatchId">Payment batch ID</param>
        /// <param name="growerPayments">List of grower payments with amounts</param>
        /// <returns>List of generated cheques</returns>
        Task<List<Cheque>> GenerateChequesForBatchAsync(
            int paymentBatchId,
            List<GrowerPaymentAmount> growerPayments);

        // ==============================================================
        // CHEQUE NUMBERING
        // ==============================================================

        /// <summary>
        /// Get the next available cheque number for a series and year
        /// </summary>
        /// <param name="chequeSeriesId">Cheque series ID</param>
        /// <param name="fiscalYear">Fiscal year</param>
        /// <returns>Next cheque number</returns>
        Task<string> GetNextChequeNumberAsync(int chequeSeriesId, int fiscalYear);

        /// <summary>
        /// Reserve a range of cheque numbers for batch processing
        /// </summary>
        /// <param name="chequeSeriesId">Cheque series ID</param>
        /// <param name="fiscalYear">Fiscal year</param>
        /// <param name="count">Number of cheques to reserve</param>
        /// <returns>Starting cheque number</returns>
        Task<int> ReserveChequeNumberRangeAsync(int chequeSeriesId, int fiscalYear, int count);

        // ==============================================================
        // VOID OPERATIONS
        // ==============================================================

        /// <summary>
        /// Void a cheque
        /// </summary>
        /// <param name="chequeId">Cheque ID to void</param>
        /// <param name="reason">Reason for voiding</param>
        /// <param name="voidedBy">User voiding the cheque</param>
        /// <param name="reverseAccounting">If true, reverse accounting entries; if false, keep A/P records</param>
        /// <returns>True if successful</returns>
        Task<bool> VoidChequeAsync(
            int chequeId,
            string reason,
            string voidedBy,
            bool reverseAccounting = false);

        /// <summary>
        /// Reissue a voided cheque (void old, create new with same amount)
        /// </summary>
        /// <param name="originalChequeId">Original cheque ID</param>
        /// <param name="newChequeDate">Date for new cheque</param>
        /// <param name="reissuedBy">User reissuing the cheque</param>
        /// <returns>New cheque</returns>
        Task<Cheque> ReissueChequeAsync(
            int originalChequeId,
            DateTime newChequeDate,
            string reissuedBy);

        // ==============================================================
        // CHEQUE QUERIES
        // ==============================================================

        /// <summary>
        /// Get cheque by ID
        /// </summary>
        Task<Cheque?> GetChequeByIdAsync(int chequeId);

        /// <summary>
        /// Get all cheques for a grower
        /// </summary>
        Task<List<Cheque>> GetGrowerChequesAsync(int growerId, int? year = null);

        /// <summary>
        /// Get all cheques in a payment batch
        /// </summary>
        Task<List<Cheque>> GetBatchChequesAsync(int paymentBatchId);

        /// <summary>
        /// Search cheques by number
        /// </summary>
        Task<List<Cheque>> SearchChequesByNumberAsync(string searchTerm);

        /// <summary>
        /// Get cheques that need to be printed
        /// </summary>
        Task<List<Cheque>> GetUnprintedChequesAsync(int? paymentBatchId = null);

        // ==============================================================
        // PRINTING SUPPORT
        // ==============================================================

        /// <summary>
        /// Mark a cheque as printed
        /// </summary>
        /// <param name="chequeId">Cheque ID</param>
        /// <param name="printedBy">User who printed</param>
        /// <returns>True if successful</returns>
        Task<bool> MarkChequeAsPrintedAsync(int chequeId, string printedBy);

        /// <summary>
        /// Mark multiple cheques as printed
        /// </summary>
        Task<bool> MarkChequesAsPrintedAsync(List<int> chequeIds, string printedBy);
    }

    // ==============================================================
    // SUPPORTING MODELS
    // ==============================================================

    /// <summary>
    /// Represents a grower payment amount for cheque generation
    /// </summary>
    public class GrowerPaymentAmount
    {
        public int GrowerId { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public decimal PaymentAmount { get; set; }
        public string? Memo { get; set; }
        public bool IsOnHold { get; set; }
    }
}


