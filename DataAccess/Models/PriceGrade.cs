using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    [Table("PriceGrades")]
    public class PriceGrade
    {
        [Key]
        public int PriceGradeId { get; set; }
        
        [Required]
        public int GradeNumber { get; set; } // 1, 2, 3
        
        [Required]
        [StringLength(50)]
        public string GradeName { get; set; } // Grade 1, Grade 2, Grade 3
        
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
        public bool IsDeleted => DeletedAt.HasValue;
    }
}
