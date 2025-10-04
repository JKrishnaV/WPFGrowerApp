using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    [Table("PriceSchedules")]
    public class PriceSchedule : AuditableEntity
    {
        [Key]
        public int PriceScheduleId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int ProcessId { get; set; }
        
        [Required]
        public DateTime EffectiveFrom { get; set; }
        
        public DateTime? EffectiveTo { get; set; }
        
        public bool TimePremiumEnabled { get; set; }
        
        [Column(TypeName = "time")]
        public TimeSpan? PremiumCutoffTime { get; set; }
        
        [Column(TypeName = "decimal(10,4)")]
        public decimal? TimePremiumAmount { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        public bool IsActive { get; set; }
        
        // Navigation properties
        [NotMapped]
        public string ProductCode { get; set; }
        
        [NotMapped]
        public string ProcessCode { get; set; }
    }
}
