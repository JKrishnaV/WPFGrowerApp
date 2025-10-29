using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    [Table("PriceAdvances")]
    public class PriceAdvance
    {
        [Key]
        public int PriceAdvanceId { get; set; }
        
        [Required]
        [StringLength(10)]
        public string AdvanceCode { get; set; } // A1, A2, A3, FN
        
        [Required]
        [StringLength(50)]
        public string AdvanceName { get; set; } // Advance 1, Advance 2, Advance 3, Final
        
        [StringLength(255)]
        public string Description { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; }
        
        // ====================================================================
        // AUDIT COLUMNS
        // ====================================================================
        
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        
        /// <summary>
        /// Returns true if the record is soft-deleted
        /// </summary>
        [NotMapped]
        public bool IsDeleted => DeletedAt.HasValue;
    }
}
