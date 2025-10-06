using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Process table.
    /// </summary>
    public class Process : AuditableEntity // Inherit from AuditableEntity
    {
        // Corresponds to ProcessId INT (but kept as string for compatibility)
        public string ProcessId { get; set; } // String to match existing code patterns

        // Corresponds to Description NVARCHAR(19)
        public string Description { get; set; }

        // Corresponds to DEF_GRADE DECIMAL(1, 0)
        public int DefGrade { get; set; } // Use int for single digit

        // Corresponds to PROC_CLASS DECIMAL(1, 0)
        public int ProcClass { get; set; } // Use int for single digit

        // Note: GRADE_N1, GRADE_N2, GRADE_N3 are excluded based on user request.
        // Audit fields are inherited from AuditableEntity.
    }
}
