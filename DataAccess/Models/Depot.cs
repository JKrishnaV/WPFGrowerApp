using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Depot table.
    /// </summary>
    public class Depot : AuditableEntity
    {
        public int DepotId { get; set; }          // Surrogate key
        public string DepotCode { get; set; }      // Business code (legacy mapping)
        public string DepotName { get; set; }
    }
}
