using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Product : AuditableEntity // Assuming AuditableEntity exists for QADD/QED/QDEL fields
    {
        // From SQL Script: ProductId INT (but kept as string for compatibility with existing ViewModels)
        public string ProductId { get; set; } // String to match existing code patterns

        // From SQL Script: Description NVARCHAR(15)
        public string Description { get; set; }

        // From SQL Script: SHORTDescription NVARCHAR(4)
        public string ShortDescription { get; set; }

        // From SQL Script: DEDUCT DECIMAL(9, 6)
        public decimal Deduct { get; set; }

        // From SQL Script: CATEGORY DECIMAL(1, 0) - nullable because some products don't have category
        public int? Category { get; set; } // Nullable to handle empty/NULL values from database

        // From SQL Script: CHG_GST BIT
        public bool ChargeGst { get; set; } // Renamed from CHG_GST

        // From SQL Script: VARIETY NVARCHAR(8)
        public string Variety { get; set; }

        // Note: Audit fields (QADD_DATE, QADD_TIME, QADD_OP, etc.)
        // are assumed to be handled by the AuditableEntity base class
        // or will be managed directly in the service layer during DB operations.
    }
}
