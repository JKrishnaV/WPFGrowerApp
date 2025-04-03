using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Depot table.
    /// </summary>
    public class Depot // Removed inheritance from ViewModelBase
    {
        // Use auto-properties for simplicity
        public string DepotId { get; set; } // Corresponds to DEPOT (NVARCHAR(1))
        public string DepotName { get; set; } // Corresponds to DEPOTNAME (NVARCHAR(12))
        // Add audit fields here if needed
    }
}
