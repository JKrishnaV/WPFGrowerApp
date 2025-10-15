using System;
using System.Security.Permissions;

namespace WPFGrowerApp.Models
{
    public class GrowerSearchResult
    {
        public int GrowerId { get; set; }  // Primary key from database
        public string GrowerNumber { get; set; } = string.Empty;  // Updated to string
        public string GrowerName { get; set; } = string.Empty;
        public string ChequeName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;  // Added missing Email property
        public string Notes { get; set; } = string.Empty;
        public string PayGroup { get; set; } = string.Empty;
        public string PaymentGroupCode { get; set; } = string.Empty;  // Added missing PaymentGroupCode property
        public string Phone2 { get; set; } = string.Empty;
        public bool IsOnHold { get; set; }
        public bool IsActive { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
