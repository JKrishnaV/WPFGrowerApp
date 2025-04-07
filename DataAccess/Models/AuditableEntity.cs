using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Base class for entities that require audit tracking fields.
    /// </summary>
    public abstract class AuditableEntity
    {
        // QADD - Add Audit Fields
        public DateTime? QADD_DATE { get; set; }
        public string QADD_TIME { get; set; } // Storing time as string based on original schema
        public string QADD_OP { get; set; }

        // QED - Edit Audit Fields
        public DateTime? QED_DATE { get; set; }
        public string QED_TIME { get; set; }
        public string QED_OP { get; set; }

        // QDEL - Delete Audit Fields
        public DateTime? QDEL_DATE { get; set; }
        public string QDEL_TIME { get; set; }
        public string QDEL_OP { get; set; }
    }
}
