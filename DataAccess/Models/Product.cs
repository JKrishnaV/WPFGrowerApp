using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Product : AuditableEntity // Assuming AuditableEntity exists for QADD/QED/QDEL fields
    {
        // From SQL Script: PRODUCT NVARCHAR(2)
        public string ProductId { get; set; } // Renamed from PRODUCT for C# conventions

        // From SQL Script: Description NVARCHAR(15)
        public string Description { get; set; }

        // From SQL Script: SHORTDescription NVARCHAR(4)
        public string ShortDescription { get; set; }

        // From SQL Script: DEDUCT DECIMAL(9, 6)
        public decimal Deduct { get; set; }

        // From SQL Script: CATEGORY DECIMAL(1, 0)
        public int Category { get; set; } // Using int for a single digit decimal

        // From SQL Script: CHG_GST BIT
        public bool ChargeGst { get; set; } // Renamed from CHG_GST

        // From SQL Script: VARIETY NVARCHAR(8)
        public string Variety { get; set; }

        // Note: Audit fields (QADD_DATE, QADD_TIME, QADD_OP, etc.)
        // are assumed to be handled by the AuditableEntity base class
        // or will be managed directly in the service layer during DB operations.
    }
}
