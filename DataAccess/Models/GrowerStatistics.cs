using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents statistics and summary data for a grower.
    /// Used for displaying grower history and performance metrics.
    /// </summary>
    public class GrowerStatistics
    {
        public int GrowerId { get; set; }
        public int TotalReceipts { get; set; }
        public decimal TotalReceiptsValue { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalPaymentsValue { get; set; }
        public DateTime? LastReceiptDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public int CurrentYearReceipts { get; set; }
        public decimal CurrentYearValue { get; set; }

        /// <summary>
        /// Gets the average receipt value.
        /// </summary>
        public decimal AverageReceiptValue => TotalReceipts > 0 ? TotalReceiptsValue / TotalReceipts : 0;

        /// <summary>
        /// Gets the average payment value.
        /// </summary>
        public decimal AveragePaymentValue => TotalPayments > 0 ? TotalPaymentsValue / TotalPayments : 0;

        /// <summary>
        /// Gets the current year average receipt value.
        /// </summary>
        public decimal CurrentYearAverageReceiptValue => CurrentYearReceipts > 0 ? CurrentYearValue / CurrentYearReceipts : 0;

        /// <summary>
        /// Gets the net amount (total payments - total receipts).
        /// </summary>
        public decimal NetAmount => TotalPaymentsValue - TotalReceiptsValue;
    }
}
