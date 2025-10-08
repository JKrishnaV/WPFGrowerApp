using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public bool IsActive { get; set; }
        
        // Audit columns
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        
        // Login tracking
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastLockoutAt { get; set; }

        // Legacy property mappings for backward compatibility
        public DateTime DateCreated
        {
            get => CreatedAt;
            set => CreatedAt = value;
        }

        public DateTime? LastLoginDate
        {
            get => LastLoginAt;
            set => LastLoginAt = value;
        }

        public bool IsLockedOut
        {
            get => IsLocked;
            set => IsLocked = value;
        }

        public DateTime? LastLockoutDate
        {
            get => LastLockoutAt;
            set => LastLockoutAt = value;
        }

        // Admin role is RoleId = 1 (based on the initial data in Roles table)
        public bool IsAdmin => RoleId.HasValue && RoleId.Value == 1;

        // Helper method to check if user has a specific role
        public bool HasRole(int roleId) => RoleId.HasValue && RoleId.Value == roleId;

        // Helper property to check if user is soft deleted
        public bool IsDeleted => DeletedAt.HasValue;
    }
}
