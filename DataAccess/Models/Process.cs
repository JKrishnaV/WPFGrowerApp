using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Process table.
    /// </summary>
    public class Process
    {
        // Corresponds to PROCESS NVARCHAR(2)
        public string ProcessId { get; set; }

        // Corresponds to Description NVARCHAR(19)
        public string Description { get; set; }

        // Corresponds to DEF_GRADE DECIMAL(1, 0)
        public decimal DefGrade { get; set; }

        // Corresponds to PROC_CLASS DECIMAL(1, 0)
        public decimal ProcClass { get; set; }

        // Add audit fields if needed
    }
}
