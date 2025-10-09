using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service for calculating grower payments (advances and final payments)
    /// </summary>
    public interface IPaymentCalculationService
    {
        // ==============================================================
        // ADVANCE PAYMENT CALCULATIONS
        // ==============================================================

        /// <summary>
        /// Calculate advance payment for all eligible growers
        /// </summary>
        /// <param name="advanceNumber">1, 2, or 3</param>
        /// <param name="paymentDate">Date of payment</param>
        /// <param name="cutoffDate">Include receipts up to this date</param>
        /// <param name="cropYear">Crop year</param>
        /// <param name="filterPayGroup">Optional: Filter to specific pay group</param>
        /// <param name="filterGrower">Optional: Filter to specific grower</param>
        /// <returns>List of grower payments with calculated amounts</returns>
        Task<List<TestRunGrowerPayment>> CalculateAdvancePaymentBatchAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            string? filterPayGroup = null,
            int? filterGrower = null);

        /// <summary>
        /// Calculate advance payment for a single grower
        /// </summary>
        Task<TestRunGrowerPayment> CalculateGrowerAdvanceAsync(
            int growerNumber,
            int advanceNumber,
            DateTime cutoffDate,
            int cropYear);

        /// <summary>
        /// Get eligible receipts for advance payment
        /// </summary>
        /// <param name="growerNumber">Grower ID</param>
        /// <param name="advanceNumber">Which advance (1, 2, or 3)</param>
        /// <param name="cutoffDate">Include receipts up to this date</param>
        /// <param name="cropYear">Crop year</param>
        /// <returns>List of receipts eligible for this advance</returns>
        Task<List<Receipt>> GetEligibleReceiptsForAdvanceAsync(
            int growerNumber,
            int advanceNumber,
            DateTime cutoffDate,
            int cropYear);

        // ==============================================================
        // FINAL PAYMENT CALCULATIONS
        // ==============================================================

        /// <summary>
        /// Calculate final payment for a grower (after all advances)
        /// </summary>
        /// <param name="growerNumber">Grower ID</param>
        /// <param name="paymentDate">Payment date</param>
        /// <param name="cropYear">Crop year</param>
        /// <returns>Final payment calculation with all details</returns>
        Task<GrowerFinalPayment> CalculateGrowerFinalPaymentAsync(
            int growerNumber,
            DateTime paymentDate,
            int cropYear);

        /// <summary>
        /// Calculate final payments for all eligible growers
        /// </summary>
        Task<List<GrowerFinalPayment>> CalculateFinalPaymentBatchAsync(
            DateTime paymentDate,
            int cropYear,
            string? filterPayGroup = null,
            int? filterGrower = null);

        // ==============================================================
        // DEDUCTION CALCULATIONS
        // ==============================================================

        /// <summary>
        /// Calculate deductions for a grower (containers, loans, adjustments)
        /// </summary>
        /// <param name="growerNumber">Grower ID</param>
        /// <param name="upToDate">Calculate deductions up to this date</param>
        /// <returns>List of deductions to apply</returns>
        Task<List<Deduction>> CalculateDeductionsAsync(
            int growerNumber,
            DateTime upToDate);

        // ==============================================================
        // PAYMENT SUMMARY
        // ==============================================================

        /// <summary>
        /// Get payment summary for a grower (all payments for the year)
        /// </summary>
        /// <param name="growerNumber">Grower ID</param>
        /// <param name="cropYear">Crop year</param>
        /// <returns>Summary of all payments and outstanding balance</returns>
        Task<PaymentSummary> GetGrowerPaymentSummaryAsync(
            int growerNumber,
            int cropYear);

        // ==============================================================
        // PRICE LOOKUP
        // ==============================================================

        /// <summary>
        /// Get applicable price for a receipt at a specific advance level
        /// </summary>
        /// <param name="receipt">Receipt to price</param>
        /// <param name="advanceNumber">Advance number (1, 2, 3) or 0 for final</param>
        /// <param name="priceDate">Date to lookup price</param>
        /// <returns>Price per pound for the receipt at the specified advance level</returns>
        Task<decimal> GetApplicablePriceAsync(
            Receipt receipt,
            int advanceNumber,
            DateTime priceDate);
    }

    // ==============================================================
    // SUPPORTING MODELS FOR PAYMENT CALCULATIONS
    // ==============================================================

    /// <summary>
    /// Represents a deduction to be applied to a grower
    /// </summary>
    public class Deduction
    {
        public string DeductionType { get; set; } = string.Empty;  // Container, Loan, Adjustment
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DeductionDate { get; set; }
        public int? ReferenceId { get; set; }  // Link to source record if applicable
    }

    /// <summary>
    /// Summary of all payments for a grower in a crop year
    /// </summary>
    public class PaymentSummary
    {
        public int GrowerNumber { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public int CropYear { get; set; }

        // Receipts
        public decimal TotalReceiptsValue { get; set; }
        public decimal TotalReceiptsWeight { get; set; }
        public int TotalReceiptsCount { get; set; }

        // Advances
        public decimal Advance1Paid { get; set; }
        public decimal Advance2Paid { get; set; }
        public decimal Advance3Paid { get; set; }
        public decimal TotalAdvancesPaid => Advance1Paid + Advance2Paid + Advance3Paid;

        // Deductions
        public decimal TotalDeductions { get; set; }
        public List<Deduction> Deductions { get; set; } = new();

        // Final payment
        public decimal FinalPaymentAmount { get; set; }
        public bool FinalPaymentMade { get; set; }

        // Balance
        public decimal OutstandingBalance => TotalReceiptsValue - TotalAdvancesPaid - TotalDeductions - FinalPaymentAmount;
    }

    /// <summary>
    /// Represents calculated final payment for a grower
    /// </summary>
    public class GrowerFinalPayment
    {
        public int GrowerNumber { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public bool IsOnHold { get; set; }

        // Receipt totals
        public int ReceiptCount { get; set; }
        public decimal TotalNetWeight { get; set; }
        public decimal TotalReceiptValue { get; set; }

        // Advances already paid
        public decimal TotalAdvancesPaid { get; set; }
        public decimal Advance1Amount { get; set; }
        public decimal Advance2Amount { get; set; }
        public decimal Advance3Amount { get; set; }

        // Deductions
        public decimal TotalDeductions { get; set; }
        public List<Deduction> Deductions { get; set; } = new();

        // Final payment calculation
        public decimal CalculatedFinalPayment { get; set; }
        public decimal NetPayment => CalculatedFinalPayment - TotalDeductions;

        // Validation
        public bool HasErrors { get; set; }
        public List<string> ErrorMessages { get; set; } = new();

        // Receipt details
        public List<TestRunReceiptDetail> ReceiptDetails { get; set; } = new();
    }
}


