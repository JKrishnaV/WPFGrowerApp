using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Product entity - represents berry products (Blueberries, Raspberries, etc.)
    /// </summary>
    public class Product
    {
        // ========================================
        // Primary Key & Identity
        // ========================================
        
        /// <summary>Product code (e.g., "BB" for Blueberries)</summary>
        public string ProductId { get; set; } = string.Empty;

        // ========================================
        // Core Business Fields
        // ========================================
        
        /// <summary>Full product name (e.g., "BLUEBERRIES")</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Short description (4 chars, e.g., "BLUE")</summary>
        public string? ShortDescription { get; set; }

        /// <summary>Marketing deduction amount per unit (e.g., -0.008000 per pound)</summary>
        public decimal Deduct { get; set; }

        /// <summary>Report category for grouping (1=Blueberry, 2=Raspberry, etc.)</summary>
        public int Category { get; set; }

        /// <summary>Whether to charge GST on this product</summary>
        public bool ChargeGst { get; set; }

        /// <summary>Default variety code for this product (e.g., "Blueberry")</summary>
        public string? Variety { get; set; }

        // ========================================
        // Audit Fields (Modern Schema)
        // ========================================
        
        /// <summary>Timestamp when the product was created</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Username who created the product</summary>
        public string CreatedBy { get; set; } = "SYSTEM";

        /// <summary>Timestamp when the product was last modified</summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>Username who last modified the product</summary>
        public string? ModifiedBy { get; set; }

        /// <summary>Timestamp when the product was soft-deleted</summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>Username who deleted the product</summary>
        public string? DeletedBy { get; set; }

        // ========================================
        // Legacy Audit Fields (for backward compatibility with old views)
        // ========================================
        
        // These are only used when querying old compatibility views
        // They map to the QADD_DATE/TIME/OP, QED_DATE/TIME/OP, QDEL_DATE/TIME/OP columns
        public DateTime? QADD_DATE { get; set; }
        public string? QADD_TIME { get; set; }
        public string? QADD_OP { get; set; }
        public DateTime? QED_DATE { get; set; }
        public string? QED_TIME { get; set; }
        public string? QED_OP { get; set; }
        public DateTime? QDEL_DATE { get; set; }
        public string? QDEL_TIME { get; set; }
        public string? QDEL_OP { get; set; }
    }
}
