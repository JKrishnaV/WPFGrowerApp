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
    }
}
