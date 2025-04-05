using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Product table.
    /// </summary>
    public class Product
    {
        // Corresponds to PRODUCT NVARCHAR(2)
        public string ProductId { get; set; }

        // Corresponds to Description NVARCHAR(15)
        public string Description { get; set; }

        // Corresponds to SHORTDescription NVARCHAR(4)
        public string ShortDescription { get; set; }

        // Corresponds to DEDUCT DECIMAL(9, 6)
        public decimal Deduct { get; set; }

        // Corresponds to CATEGORY DECIMAL(1, 0)
        public decimal Category { get; set; }

        // Corresponds to CHG_GST BIT
        public bool ChgGst { get; set; }

        // Corresponds to VARIETY NVARCHAR(8)
        public string Variety { get; set; }

        // Add audit fields if needed
    }
}
