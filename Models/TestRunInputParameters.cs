using System;
using System.Collections.Generic;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Holds the input parameters used for an advance payment run (actual or test).
    /// </summary>
    public class TestRunInputParameters
    {
        public int AdvanceNumber { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CutoffDate { get; set; }
        public int CropYear { get; set; }
        public List<int>? ExcludeGrowerIds { get; set; } // Changed from List<decimal> to List<int>
        public List<string>? ExcludePayGroupIds { get; set; } // Nullable if not provided
        public List<int>? ProductIds { get; set; } // Nullable if not provided
        public List<int>? ProcessIds { get; set; } // Nullable if not provided

        // Consider adding descriptions for display purposes if needed
        public List<string> ExcludedGrowerDescriptions { get; set; } = new List<string>();
        public List<string> ExcludedPayGroupDescriptions { get; set; } = new List<string>();
        public List<string> ProductDescriptions { get; set; } = new List<string>();
        public List<string> ProcessDescriptions { get; set; } = new List<string>();
    }
}
