using System;

namespace WPFGrowerApp.DataAccess.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public int? RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public int FailedLoginAttempts { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LastLockoutDate { get; set; }

        // Admin role is RoleId = 1 (based on the initial data in Roles table)
        public bool IsAdmin => RoleId.HasValue && RoleId.Value == 1;

        // Helper method to check if user has a specific role
        public bool HasRole(int roleId) => RoleId.HasValue && RoleId.Value == roleId;
    }
}
