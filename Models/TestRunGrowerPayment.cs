using System.Collections.Generic;
using System.Linq;
using WPFGrowerApp.DataAccess.Models; // For GrowerInfo

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Represents the aggregated calculated payment details for a single grower during a test run.
    /// </summary>
    public class TestRunGrowerPayment
    {
        public decimal GrowerNumber { get; set; }
        public string GrowerName { get; set; } = string.Empty; // Populate from GrowerInfo
        public string Currency { get; set; } = string.Empty; // Populate from GrowerInfo
        public bool IsOnHold { get; set; } // Populate from GrowerInfo

        public List<TestRunReceiptDetail> ReceiptDetails { get; set; } = new List<TestRunReceiptDetail>();

        // Calculated Totals for the Grower
        public decimal TotalCalculatedAdvanceAmount => ReceiptDetails.Sum(r => r.CalculatedAdvanceAmount);
        public decimal TotalCalculatedPremiumAmount => ReceiptDetails.Sum(r => r.CalculatedPremiumAmount);
        public decimal TotalCalculatedDeductionAmount => ReceiptDetails.Sum(r => r.CalculatedDeductionAmount);
        public decimal TotalCalculatedPayment => ReceiptDetails.Sum(r => r.CalculatedTotalAmount);

        public int ReceiptCount => ReceiptDetails.Count;
        public decimal TotalNetWeight => ReceiptDetails.Sum(r => r.NetWeight);

        public bool HasErrors => ReceiptDetails.Any(r => !string.IsNullOrEmpty(r.ErrorMessage));
        public List<string> ErrorMessages => ReceiptDetails
                                                .Where(r => !string.IsNullOrEmpty(r.ErrorMessage))
                                                .Select(r => $"Receipt {r.ReceiptNumber}: {r.ErrorMessage}")
                                                .ToList()!; // Non-null asserted as we check IsNullOrEmpty

        // Optional: Reference to the original GrowerInfo if needed
        // public GrowerInfo OriginalGrowerInfo { get; set; }
    }
}
