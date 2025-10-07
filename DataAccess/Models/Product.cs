using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Product : AuditableEntity
    {
        // Surrogate primary key (INT Identity in DB)
        public int ProductId { get; set; }

        // Business code (unique) previously misused as ID in legacy code
        public string ProductCode { get; set; }

        // Display/description fields
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public decimal Deduct { get; set; }
        public int? Category { get; set; }
        public bool ChargeGst { get; set; }
        public string Variety { get; set; }
    }
}
