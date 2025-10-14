using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a record from the Depots table.
    /// </summary>
    public class Depot : AuditableEntity
    {
        public int DepotId { get; set; }                    // Primary key, Identity
        public string DepotCode { get; set; } = string.Empty;  // Business code (unique, max 10)
        public string DepotName { get; set; } = string.Empty;   // Depot name (max 100)
        public string? Address { get; set; }                // Optional address (max 200)
        public string? City { get; set; }                   // Optional city (max 50)
        public string? Province { get; set; }               // Optional province (max 2)
        public string? PostalCode { get; set; }             // Optional postal code (max 10)
        public string? PhoneNumber { get; set; }            // Optional phone number (max 20)
        public bool IsActive { get; set; } = true;         // Active status
        public int? DisplayOrder { get; set; }              // Display order for UI
        public new DateTime CreatedAt { get; set; }        // Creation timestamp
        public new string CreatedBy { get; set; } = string.Empty; // Creator
        public new DateTime? ModifiedAt { get; set; }      // Last modification timestamp
        public new string? ModifiedBy { get; set; }        // Last modifier
        public new DateTime? DeletedAt { get; set; }        // Soft delete timestamp
        public new string? DeletedBy { get; set; }          // Soft delete user

        // Computed properties for display
        public string FullAddress => GetFullAddress();
        public string StatusText => IsActive ? "Active" : "Inactive";

        private string GetFullAddress()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Address)) parts.Add(Address);
            if (!string.IsNullOrWhiteSpace(City)) parts.Add(City);
            if (!string.IsNullOrWhiteSpace(Province)) parts.Add(Province);
            if (!string.IsNullOrWhiteSpace(PostalCode)) parts.Add(PostalCode);
            return parts.Any() ? string.Join(", ", parts) : "N/A";
        }
    }
}