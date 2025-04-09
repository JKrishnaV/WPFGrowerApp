using System;
using WPFGrowerApp.DataAccess.Models; // Assuming Receipt model is here

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Represents the calculated payment details for a single receipt during a test run.
    /// </summary>
    public class TestRunReceiptDetail
    {
        public decimal ReceiptNumber { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string Product { get; set; } = string.Empty;
        public string Process { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal NetWeight { get; set; } // Lbs

        // Calculated Values
        public decimal CalculatedAdvancePrice { get; set; }
        public decimal CalculatedPremiumPrice { get; set; }
        public decimal CalculatedMarketingDeduction { get; set; } // Note: This is often a negative value representing the rate
        public decimal CalculatedAdvanceAmount { get; set; } // NetWeight * CalculatedAdvancePrice
        public decimal CalculatedPremiumAmount { get; set; } // NetWeight * CalculatedPremiumPrice
        public decimal CalculatedDeductionAmount { get; set; } // NetWeight * CalculatedMarketingDeduction
        public decimal CalculatedTotalAmount => CalculatedAdvanceAmount + CalculatedPremiumAmount + CalculatedDeductionAmount;

        public decimal PriceRecordId { get; set; } // The Price record used
        public string? ErrorMessage { get; set; } // If an error occurred processing this specific receipt

        // Optional: Reference to the original receipt if needed later
        // public Receipt OriginalReceipt { get; set; }
    }
}
