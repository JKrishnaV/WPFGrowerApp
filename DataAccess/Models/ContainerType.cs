using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a container type definition in the Containers table.
    /// Containers are physical items (flats, lugs, pallets) used by growers to ship berries.
    /// </summary>
    [Table("Containers")]
    public class ContainerType
    {
        /// <summary>
        /// Container ID (Primary Key).
        /// </summary>
        [Key]
        public int ContainerId { get; set; }

        /// <summary>
        /// Container code (e.g., "FLAT", "PINT", "QUART").
        /// </summary>
        [Required(ErrorMessage = "Container code is required")]
        [MaxLength(10)]
        public string ContainerCode { get; set; } = string.Empty;

        /// <summary>
        /// Full name/description of the container type.
        /// Example: "Flat (12 pints)", "Pint", "Quart"
        /// </summary>
        [Required(ErrorMessage = "Container name is required")]
        [MaxLength(100)]
        public string ContainerName { get; set; } = string.Empty;

        /// <summary>
        /// Tare weight of the container.
        /// Note: This is for informational purposes only. 
        /// Actual weight calculations are done at the scale.
        /// </summary>
        public decimal? TareWeight { get; set; }

        /// <summary>
        /// Value/price of the container (in dollars).
        /// Used for tracking container costs and inventory value.
        /// </summary>
        public decimal? Value { get; set; }

        /// <summary>
        /// Indicates if this container type is actively being used.
        /// Only active containers are available for selection during receipt entry.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order for sorting containers in dropdowns.
        /// </summary>
        public int? DisplayOrder { get; set; }

        /// <summary>
        /// When the record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Who created the record.
        /// </summary>
        [MaxLength(50)]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// When the record was last modified.
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Who last modified the record.
        /// </summary>
        [MaxLength(50)]
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// When the record was deleted (soft delete).
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Who deleted the record.
        /// </summary>
        [MaxLength(50)]
        public string? DeletedBy { get; set; }

        // Navigation properties for calculated values

        /// <summary>
        /// Returns true if this container type is actively being used.
        /// </summary>
        [NotMapped]
        public string StatusText => IsActive ? "Active" : "Inactive";

        /// <summary>
        /// Formatted value with currency symbol.
        /// </summary>
        [NotMapped]
        public string FormattedValue => Value?.ToString("C2") ?? "$0.00";

        /// <summary>
        /// Formatted tare weight with units.
        /// </summary>
        [NotMapped]
        public string FormattedTareWeight => TareWeight?.ToString("F2") + " lbs" ?? "N/A";

        /// <summary>
        /// Returns a display string for this container type.
        /// </summary>
        /// <returns>Format: "[ID] Code - Name"</returns>
        public override string ToString()
        {
            return $"[{ContainerId}] {ContainerCode} - {ContainerName}";
        }
    }
}
