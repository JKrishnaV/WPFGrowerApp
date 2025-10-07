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
    public string GrowerNumber { get; set; } = string.Empty;
    public int GrowerId { get; set; } // Modern DB primary key
        public string GrowerName { get; set; } = string.Empty; // Populate from GrowerInfo
        public string Currency { get; set; } = string.Empty; // Populate from GrowerInfo
        public bool IsOnHold { get; set; } // Populate from GrowerInfo

        public List<TestRunReceiptDetail> ReceiptDetails { get; set; } = new List<TestRunReceiptDetail>();

        // Calculated Totals for the Grower
        public decimal TotalCalculatedAdvanceAmount => ReceiptDetails.Sum(r => r.CalculatedAdvanceAmount);
        public decimal TotalCalculatedPremiumAmount => ReceiptDetails.Sum(r => r.CalculatedPremiumAmount);
        public decimal TotalCalculatedDeductionAmount => ReceiptDetails.Sum(r => r.CalculatedDeductionAmount);
        public decimal TotalCalculatedPayment => ReceiptDetails.Sum(r => r.CalculatedTotalAmount);

        // Fix #4: Fresh vs Non-Fresh breakdown (mirrors legacy aScan(aFresh, Daily->process))
        public decimal FreshAdvanceAmount => ReceiptDetails.Where(r => r.IsFresh).Sum(r => r.CalculatedAdvanceAmount);
        public decimal NonFreshAdvanceAmount => ReceiptDetails.Where(r => !r.IsFresh).Sum(r => r.CalculatedAdvanceAmount);
        public decimal FreshNetWeight => ReceiptDetails.Where(r => r.IsFresh).Sum(r => r.NetWeight);
        public decimal NonFreshNetWeight => ReceiptDetails.Where(r => !r.IsFresh).Sum(r => r.NetWeight);
        public int FreshReceiptCount => ReceiptDetails.Count(r => r.IsFresh);
        public int NonFreshReceiptCount => ReceiptDetails.Count(r => !r.IsFresh);

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
