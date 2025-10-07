using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Process table.
    /// </summary>
    public class Process : AuditableEntity
    {
        public int ProcessId { get; set; }          // Surrogate key
        public string ProcessCode { get; set; }      // Business code (legacy usage)
        public string Description { get; set; }
        public int DefGrade { get; set; }
        public int ProcClass { get; set; }
    }
}
