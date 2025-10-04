using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Process table (Process Types - varieties and processing methods).
    /// </summary>
    public class Process : AuditableEntity // Inherit from AuditableEntity
    {
        // Corresponds to ProcessCode NVARCHAR(2)
        public string ProcessId { get; set; }

        // Corresponds to ProcessName/Description NVARCHAR(100)
        public string Description { get; set; }

        // Corresponds to DefaultGrade INT
        public int? DefaultGrade { get; set; }

        // Corresponds to ProcessClass INT
        public int? ProcessClass { get; set; }

        // Grade names for this process type
        public string? GradeName1 { get; set; }
        public string? GradeName2 { get; set; }
        public string? GradeName3 { get; set; }

        // Modern audit fields (CreatedAt, CreatedBy, ModifiedAt, ModifiedBy inherited from AuditableEntity)
        
        /// <summary>Timestamp when the process was soft-deleted</summary>
        public DateTime? DeletedAt { get; set; }
        
        /// <summary>Username who deleted the process</summary>
        public string? DeletedBy { get; set; }
    }
}
