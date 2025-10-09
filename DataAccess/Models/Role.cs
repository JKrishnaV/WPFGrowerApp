using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        
        // ====================================================================
        // AUDIT COLUMNS
        // ====================================================================
        
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        
        /// <summary>
        /// Returns true if the record is soft-deleted
        /// </summary>
        public bool IsDeleted => DeletedAt.HasValue;

        public override string ToString()
        {
            return RoleName;
        }
    }
} 