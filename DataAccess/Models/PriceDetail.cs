using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    [Table("PriceDetails")]
    public class PriceDetail
    {
        [Key]
        public int PriceDetailId { get; set; }
        
        [Required]
        public int PriceScheduleId { get; set; }
        
        [Required]
        public int PriceClassId { get; set; }
        
        [Required]
        public int PriceGradeId { get; set; }
        
        [Required]
        public int PriceAreaId { get; set; }
        
        public int? ProcessTypeId { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(10,4)")]
        public decimal PricePerPound { get; set; }
        
        // Navigation properties (for display)
        [NotMapped]
        public string ClassCode { get; set; }
        
        [NotMapped]
        public string ClassName { get; set; }
        
        [NotMapped]
        public int GradeNumber { get; set; }
        
        [NotMapped]
        public string GradeName { get; set; }
        
        [NotMapped]
        public string AreaCode { get; set; }
        
        [NotMapped]
        public string AreaName { get; set; }
        
        [NotMapped]
        public string ProcessTypeName { get; set; }
        
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
