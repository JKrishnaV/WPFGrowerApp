using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment group, corresponding to the PaymentGroups table.
    /// </summary>
    public class PayGroup
    {
        /// <summary>
        /// Gets or sets the Payment Group ID (Primary Key).
        /// </summary>
        public int PaymentGroupId { get; set; }

        /// <summary>
        /// Gets or sets the group code (e.g., "STD", "PREM", "NEW").
        /// </summary>
        public string GroupCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the payment group.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the default price level for this payment group (1-3).
        /// </summary>
        public int? DefaultPriceLevel { get; set; }

        /// <summary>
        /// Gets or sets whether the payment group is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets when the record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets who created the record.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the record was last modified.
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets who last modified the record.
        /// </summary>
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets when the record was deleted (soft delete).
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Gets or sets who deleted the record.
        /// </summary>
        public string? DeletedBy { get; set; }
    }
}
