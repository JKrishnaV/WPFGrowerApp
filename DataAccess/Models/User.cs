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
        public int? RoleId { get; set; } // Nullable if roles are optional or not implemented yet
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? LastLoginDate { get; set; } // Nullable
        public int FailedLoginAttempts { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LastLockoutDate { get; set; } // Nullable
    }
}
