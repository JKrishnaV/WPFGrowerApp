using System;
using System.Collections.Generic;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Represents a saved filter preset for payment runs.
    /// Allows users to save and load common filter configurations.
    /// </summary>
    public class FilterPreset
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;

        // Basic Parameters
        public int AdvanceNumber { get; set; } = 1;
        public DateTime PaymentDate { get; set; } = DateTime.Today;
        public DateTime CutoffDate { get; set; } = DateTime.Today;
        public DateTime? ReceiptDateFrom { get; set; }
        public int CropYear { get; set; } = DateTime.Today.Year;

        // Enhanced Filters
        public decimal? MinimumWeight { get; set; }
        public bool ShowUnpayableReceipts { get; set; } = false;
        public bool IncludePreviouslyPaid { get; set; } = false;

        // Filter Modes - Removed as radio buttons are no longer used

        // Process Class Filters
        public bool IncludeFresh { get; set; } = true;
        public bool IncludeProcessed { get; set; } = true;
        public bool IncludeJuice { get; set; } = true;
        public bool IncludeOther { get; set; } = true;

        // Grade Filters
        public bool IncludeGrade1 { get; set; } = true;
        public bool IncludeGrade2 { get; set; } = true;
        public bool IncludeGrade3 { get; set; } = true;
        public bool IncludeOtherGrade { get; set; } = true;

        // Selected Items (IDs only for serialization)
        public List<int> SelectedProductIds { get; set; } = new List<int>();
        public List<int> SelectedProcessIds { get; set; } = new List<int>();
        public List<int> SelectedGrowerIds { get; set; } = new List<int>();
        public List<string> SelectedPayGroupIds { get; set; } = new List<string>();

        /// <summary>
        /// Creates a display string for the preset.
        /// </summary>
        public override string ToString()
        {
            return $"{Name} ({CreatedAt:yyyy-MM-dd})";
        }
    }
}
