using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Base class for entities that require audit tracking fields.
    /// Includes both legacy (QADD/QED/QDEL) and modern (Created/Modified/Deleted) audit columns.
    /// </summary>
    public abstract class AuditableEntity
    {
        // ====================================================================
        // MODERN AUDIT COLUMNS (BerryFarmsModern Database Standard)
        // ====================================================================
        
        /// <summary>
        /// When the record was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Who created the record (Username or 'SYSTEM')
        /// </summary>
        public string CreatedBy { get; set; }
        
        /// <summary>
        /// When the record was last modified
        /// </summary>
        public DateTime? ModifiedAt { get; set; }
        
        /// <summary>
        /// Who last modified the record
        /// </summary>
        public string? ModifiedBy { get; set; }
        
        /// <summary>
        /// When the record was soft-deleted (NULL if not deleted)
        /// </summary>
        public DateTime? DeletedAt { get; set; }
        
        /// <summary>
        /// Who soft-deleted the record
        /// </summary>
        public string? DeletedBy { get; set; }
        
        // ====================================================================
        // LEGACY AUDIT COLUMNS (For backward compatibility with old xBase system)
        // ====================================================================
        
        // QADD - Add Audit Fields
        public DateTime? QADD_DATE { get; set; }
        public string? QADD_TIME { get; set; } // Storing time as string based on original schema
        public string? QADD_OP { get; set; }

        // QED - Edit Audit Fields
        public DateTime? QED_DATE { get; set; }
        public string? QED_TIME { get; set; }
        public string? QED_OP { get; set; }

        // QDEL - Delete Audit Fields
        public DateTime? QDEL_DATE { get; set; }
        public string? QDEL_TIME { get; set; }
        public string? QDEL_OP { get; set; }
        
        // ====================================================================
        // HELPER PROPERTIES
        // ====================================================================
        
        /// <summary>
        /// Returns true if the record is soft-deleted (DeletedAt is not null)
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;
    }
}
