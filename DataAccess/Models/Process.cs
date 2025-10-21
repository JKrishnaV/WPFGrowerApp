using System;
using System.Collections.Generic;
using System.Linq;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Processes table.
    /// </summary>
    public class Process : AuditableEntity
    {
        public int ProcessId { get; set; }                    // Primary key, Identity
        public string ProcessCode { get; set; } = string.Empty;  // Business code (unique)
        public string ProcessName { get; set; } = string.Empty;   // Process name (required)
        public string? Description { get; set; }              // Optional description
        public bool IsActive { get; set; } = true;            // Active status
        public int? DisplayOrder { get; set; }                 // Display order for UI
        public new DateTime CreatedAt { get; set; }               // Creation timestamp
        public new string CreatedBy { get; set; } = string.Empty; // Creator
        public new DateTime? ModifiedAt { get; set; }             // Last modification timestamp
        public new string? ModifiedBy { get; set; }                // Last modifier
        public new DateTime? DeletedAt { get; set; }              // Soft delete timestamp
        public new string? DeletedBy { get; set; }                 // Soft delete user
        public int? DefaultGrade { get; set; }                 // Default grade (1-3)
        public int? ProcessClass { get; set; }                 // Process classification (1-4)
        public string? GradeName1 { get; set; }                // Grade 1 name
        public string? GradeName2 { get; set; }                // Grade 2 name
        public string? GradeName3 { get; set; }                // Grade 3 name

        // Computed properties for display
        public string GradeNames => GetGradeNamesDisplay();
        public string DisplayName => $"{ProcessCode} - {ProcessName}";  // Better display format for dropdowns

        // Legacy compatibility properties for existing code
        public string ProcessDescription => ProcessName;             // Map ProcessName to Description for compatibility
        public int DefGrade => DefaultGrade ?? 0;             // Map DefaultGrade to DefGrade for compatibility
        public int ProcClass => ProcessClass ?? 0;            // Map ProcessClass to ProcClass for compatibility

        private string GetGradeNamesDisplay()
        {
            var names = new List<string>();
            if (!string.IsNullOrWhiteSpace(GradeName1)) names.Add($"1:{GradeName1}");
            if (!string.IsNullOrWhiteSpace(GradeName2)) names.Add($"2:{GradeName2}");
            if (!string.IsNullOrWhiteSpace(GradeName3)) names.Add($"3:{GradeName3}");
            return names.Count > 0 ? string.Join(", ", names) : "None";
        }
    }
}