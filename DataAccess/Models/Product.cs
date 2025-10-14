using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Product : AuditableEntity
    {
        // Surrogate primary key (INT Identity in DB)
        public int ProductId { get; set; }

        // Business code (unique) previously misused as ID in legacy code
        public string ProductCode { get; set; } = string.Empty;

        // Display/description fields
        public string Description { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public decimal Deduct { get; set; }
        public int? Category { get; set; }
        public bool ChargeGst { get; set; }
        public string Variety { get; set; } = string.Empty;
    }
}
