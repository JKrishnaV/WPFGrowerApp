using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    [Table("PriceClasses")]
    public class PriceClass
    {
        [Key]
        public int PriceClassId { get; set; }
        
        [Required]
        [StringLength(10)]
        public string ClassCode { get; set; } // CL1, CL2, CL3, UL1, UL2, UL3
        
        [Required]
        [StringLength(100)]
        public string ClassName { get; set; } // Canadian Level 1, US Level 2, etc.
        
        [StringLength(255)]
        public string Description { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; }
    }
}
