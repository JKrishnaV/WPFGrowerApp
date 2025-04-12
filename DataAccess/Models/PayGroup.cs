using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a payment group, corresponding to the PayGrp table.
    /// </summary>
    public class PayGroup : AuditableEntity
    {
        /// <summary>
        /// Gets or sets the Payment Group ID (Primary Key). Corresponds to PAYGRP NVARCHAR(1).
        /// </summary>
        public string PayGroupId { get; set; }

        /// <summary>
        /// Gets or sets the description of the payment group. Corresponds to Description NVARCHAR(30).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the default pay level for the group. Corresponds to DEF_PRLVL DECIMAL(1, 0).
        /// </summary>
        public int? DefaultPayLevel { get; set; } // Using nullable int for DECIMAL(1,0)
    }
}
