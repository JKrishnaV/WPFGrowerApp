using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a container type definition in the Contain table.
    /// Containers are physical items (flats, lugs, pallets) used by growers to ship berries.
    /// The system supports up to 20 different container types.
    /// </summary>
    [Table("Contain")]
    public class ContainerType : AuditableEntity
    {
        /// <summary>
        /// Container ID (1-20). Must be unique.
        /// </summary>
        [Key]
        [Column("CONTAINER")]
        [Range(1, 20, ErrorMessage = "Container ID must be between 1 and 20")]
        public int ContainerId { get; set; }

        /// <summary>
        /// Full description of the container type.
        /// Example: "Partition Plastic Flat", "Blueberry Lugs"
        /// </summary>
        [Column("Description")]
        [Required(ErrorMessage = "Description is required")]
        [MaxLength(30)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Short code used in reports (max 6 characters).
        /// Example: "PPFL", "BBL", "GHL"
        /// </summary>
        [Column("SHORT")]
        [Required(ErrorMessage = "Short code is required")]
        [MaxLength(6)]
        public string ShortCode { get; set; } = string.Empty;

        /// <summary>
        /// Tare weight of the container.
        /// Note: This is for informational purposes only. 
        /// Actual weight calculations are done at the scale.
        /// </summary>
        [Column("TARE")]
        public int TareWeight { get; set; }

        /// <summary>
        /// Container value/deposit amount.
        /// Used to calculate the value of containers in grower's possession.
        /// </summary>
        [Column("VALUE")]
        [Range(0, 9999.99, ErrorMessage = "Value must be between 0 and 9999.99")]
        public decimal Value { get; set; }

        /// <summary>
        /// Indicates if this container type is actively being tracked.
        /// Only containers with InUse = true are available for selection during receipt entry.
        /// </summary>
        [Column("INUSE")]
        public bool InUse { get; set; }

        // Navigation properties for calculated values (not stored in database)

        /// <summary>
        /// Returns true if this container type is actively being used.
        /// </summary>
        [NotMapped]
        public string InUseStatus => InUse ? "Active" : "Inactive";

        /// <summary>
        /// Formatted value with currency symbol.
        /// </summary>
        [NotMapped]
        public string FormattedValue => Value.ToString("C2");

        /// <summary>
        /// Returns a display string for this container type.
        /// </summary>
        /// <returns>Format: "[ID] ShortCode - Description"</returns>
        public override string ToString()
        {
            return $"[{ContainerId}] {ShortCode} - {Description}";
        }
    }
}
