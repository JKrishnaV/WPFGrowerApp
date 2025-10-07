using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Summary of all payments made for a receipt
    /// </summary>
    public class ReceiptPaymentSummary
    {
        public int ReceiptId { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public decimal Advance1Amount { get; set; }
        public decimal Advance2Amount { get; set; }
        public decimal Advance3Amount { get; set; }
        public decimal FinalAmount { get; set; }
        public int PaymentCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public bool HasAdvance1 { get; set; }
        public bool HasAdvance2 { get; set; }
        public bool HasAdvance3 { get; set; }
        public bool HasFinal { get; set; }
    }
}
