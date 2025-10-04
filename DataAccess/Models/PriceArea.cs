using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    [Table("PriceAreas")]
    public class PriceArea
    {
        [Key]
        public int PriceAreaId { get; set; }
        
        [Required]
        [StringLength(10)]
        public string AreaCode { get; set; } // A1, A2, A3, FN
        
        [Required]
        [StringLength(50)]
        public string AreaName { get; set; } // Advance 1, Advance 2, Advance 3, Final
        
        [StringLength(255)]
        public string Description { get; set; }
        
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; }
    }
}
