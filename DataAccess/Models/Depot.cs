using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Depot table.
    /// </summary>
    public class Depot : AuditableEntity // Inherit from AuditableEntity
    {
        // Use auto-properties for simplicity
        public string DepotId { get; set; } // Corresponds to DEPOT (NVARCHAR(1))
        public string DepotName { get; set; } // Corresponds to DEPOTNAME (NVARCHAR(12))
        
        // Audit fields are inherited from AuditableEntity
    }
}
