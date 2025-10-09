using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Infrastructure.Security; // Added for PasswordHasher

namespace WPFGrowerApp.DataAccess.Services
{
    public class UserService : BaseDatabaseService, IUserService
    {
        // Note: BaseDatabaseService constructor handles getting the connection string
        private readonly IAuditLogService _auditLogService;

        public UserService()
        {
            // Initialize audit log service for detailed history tracking
            _auditLogService = new AuditLogService();
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null; // Cannot authenticate with empty credentials
            }

            User user = null;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT UserId, Username, FullName, Email, PasswordHash, PasswordSalt, 
                               RoleId, IsActive, 
                               CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, DeletedAt, DeletedBy,
                               LastLoginAt, FailedLoginAttempts, IsLocked, LastLockoutAt
                        FROM AppUsers 
                        WHERE Username = @Username AND DeletedAt IS NULL";
                    
                    user = await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });

                    if (user == null)
                    {
                        Logger.Warn($"Authentication failed: User '{username}' not found.");
                        return null; // User not found
                    }

                    if (!user.IsActive)
                    {
                         Logger.Warn($"Authentication failed: User '{username}' is inactive.");
                         // Optionally update FailedLoginAttempts here if desired
                         return null; // Account inactive
                    }

                    if (user.IsLockedOut)
                    {
                         // Check if auto-unlock period has expired (30 minutes)
                         if (user.LastLockoutDate.HasValue)
                         {
                             var lockoutDuration = TimeSpan.FromMinutes(30);
                             if (DateTime.UtcNow - user.LastLockoutDate.Value > lockoutDuration)
                             {
                                 // Auto-unlock the account
                                 Logger.Info($"Auto-unlocking account '{username}' after lockout period expired.");
                                 await UnlockUserInternalAsync(connection, user.UserId);
                                 user.IsLocked = false;
                                 user.FailedLoginAttempts = 0;
                             }
                             else
                             {
                                 var remainingMinutes = (int)(lockoutDuration - (DateTime.UtcNow - user.LastLockoutDate.Value)).TotalMinutes;
                                 Logger.Warn($"Authentication failed: User '{username}' is locked out. Try again in {remainingMinutes} minutes.");
                                 return null; // Account still locked
                             }
                         }
                         else
                         {
                             Logger.Warn($"Authentication failed: User '{username}' is locked out.");
                             return null; // Account locked out
                         }
                    }

                    // Verify password
                    if (!PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                    {
                        Logger.Warn($"Authentication failed: Invalid password for user '{username}'.");
                        // Handle failed login attempt (increment counter, potentially lock account)
                        await HandleFailedLoginAttemptAsync(connection, user);
                        return null; // Invalid password
                    }

                    // Authentication successful - reset failed attempts and update last login
                    await HandleSuccessfulLoginAsync(connection, user);
                    Logger.Info($"Authentication successful for user '{username}'.");
                    return user; 
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during authentication for user '{username}': {ex.Message}", ex);
                // Depending on policy, might want to handle specific DB exceptions differently
                return null; // Return null on error to prevent login
            }
        }

        public async Task<bool> SetPasswordAsync(string username, string newPassword)
        {
             if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ArgumentException("Username and password cannot be empty.");
            }

            try
            {
                var (hash, salt) = PasswordHasher.HashPassword(newPassword);

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE AppUsers 
                        SET PasswordHash = @PasswordHash, 
                            PasswordSalt = @PasswordSalt,
                            FailedLoginAttempts = 0, -- Reset failed attempts on password change
                            IsLocked = 0             -- Unlock account on password change
                        WHERE Username = @Username";
                    
                    int rowsAffected = await connection.ExecuteAsync(sql, new 
                    { 
                        PasswordHash = hash, 
                        PasswordSalt = salt, 
                        Username = username 
                    });

                    if (rowsAffected > 0)
                    {
                         Logger.Info($"Password updated successfully for user '{username}'.");
                         return true;
                    }
                    else
                    {
                         Logger.Warn($"Failed to update password for user '{username}'. User not found?");
                         return false;
                    }
                }
            }
            catch (Exception ex)
            {
                 Logger.Error($"Error setting password for user '{username}': {ex.Message}", ex);
                 return false;
            }
        }

        private async Task HandleFailedLoginAttemptAsync(SqlConnection connection, User user)
        {
            // Lock after 3 failed attempts
            const int maxFailedAttempts = 3; 
            int newAttemptCount = user.FailedLoginAttempts + 1;
            bool lockAccount = newAttemptCount >= maxFailedAttempts;

            var sql = @"
                UPDATE AppUsers 
                SET FailedLoginAttempts = @NewAttemptCount, 
                    IsLocked = @LockAccount,
                    LastLockoutAt = @LastLockoutAt
                WHERE UserId = @UserId";
            
            try
            {
                await connection.ExecuteAsync(sql, new 
                { 
                    NewAttemptCount = newAttemptCount, 
                    LockAccount = lockAccount,
                    LastLockoutAt = lockAccount ? DateTime.UtcNow : (DateTime?)null,
                    UserId = user.UserId 
                });

                if(lockAccount)
                {
                    Logger.Warn($"User account '{user.Username}' locked due to {maxFailedAttempts} failed login attempts. Account will auto-unlock in 30 minutes.");
                }
            }
            catch (Exception ex)
            {
                 Logger.Error($"Error updating failed login attempts for user '{user.Username}'.", ex);
                 // Decide if this error should prevent the overall authentication failure from being returned
            }
        }

         private async Task HandleSuccessfulLoginAsync(SqlConnection connection, User user)
        {
             var sql = @"
                UPDATE AppUsers 
                SET FailedLoginAttempts = 0, 
                    LastLoginAt = GETUTCDATE(),
                    IsLocked = 0 -- Ensure account is unlocked on successful login
                WHERE UserId = @UserId";
            
            try
            {
                await connection.ExecuteAsync(sql, new { UserId = user.UserId });
            }
            catch (Exception ex)
            {
                 Logger.Error($"Error updating last login time for user '{user.Username}'.", ex);
                 // Decide if this error should prevent successful login
            }
        }

        public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE AppUsers 
                        SET IsActive = @IsActive
                        WHERE UserId = @UserId";

                    int rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId, IsActive = isActive });

                    if (rowsAffected > 0)
                    {
                        Logger.Info($"User status updated successfully. UserId: {userId}, IsActive: {isActive}");
                        return true;
                    }
                    else
                    {
                        Logger.Warn($"Failed to update user status. UserId: {userId} not found.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating user status for UserId: {userId}: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                Logger.Info("Attempting to retrieve all users from database");
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    Logger.Info("Database connection opened successfully");
                    
                    var sql = @"
                        SELECT UserId, Username, FullName, Email, 
                               RoleId, IsActive,
                               CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, DeletedAt, DeletedBy,
                               LastLoginAt, FailedLoginAttempts, IsLocked, LastLockoutAt
                        FROM AppUsers
                        WHERE DeletedAt IS NULL";

                    Logger.Info("Executing SQL query to get all users (excluding soft-deleted)");
                    var result = (await connection.QueryAsync<User>(sql)).ToList();
                    Logger.Info($"Query executed successfully. Retrieved {result.Count} users");
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error retrieving all users", ex);
                return new List<User>(); // Return empty list on error
            }
        }

        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT UserId, Username, FullName, Email, 
                               RoleId, IsActive,
                               CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, DeletedAt, DeletedBy,
                               LastLoginAt, FailedLoginAttempts, IsLocked, LastLockoutAt
                        FROM AppUsers 
                        WHERE UserId = @UserId AND DeletedAt IS NULL";

                    return await connection.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving user by ID: {userId}", ex);
                return null;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT UserId, Username, FullName, Email, 
                               RoleId, IsActive,
                               CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, DeletedAt, DeletedBy,
                               LastLoginAt, FailedLoginAttempts, IsLocked, LastLockoutAt
                        FROM AppUsers 
                        WHERE Username = @Username AND DeletedAt IS NULL";

                    return await connection.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving user by username: {username}", ex);
                return null;
            }
        }

        public async Task<bool> CreateUserAsync(User user, string password)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty", nameof(password));

            try
            {
                var (hash, salt) = PasswordHasher.HashPassword(password);
                user.PasswordHash = hash;
                user.PasswordSalt = salt;
                user.CreatedAt = DateTime.UtcNow;
                user.CreatedBy = App.CurrentUser?.Username ?? "System";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO AppUsers (
                            Username, FullName, Email, PasswordHash, PasswordSalt,
                            RoleId, IsActive, CreatedAt, CreatedBy, FailedLoginAttempts, IsLocked
                        ) 
                        OUTPUT INSERTED.UserId
                        VALUES (
                            @Username, @FullName, @Email, @PasswordHash, @PasswordSalt,
                            @RoleId, @IsActive, @CreatedAt, @CreatedBy, 0, 0
                        )";

                    // Get the new UserId
                    int newUserId = await connection.ExecuteScalarAsync<int>(sql, user);
                    
                    if (newUserId > 0)
                    {
                        user.UserId = newUserId;
                        Logger.Info($"User created successfully: {user.Username} (ID: {newUserId}) by {user.CreatedBy}");
                        
                        // Log to detailed audit log
                        var auditEntries = new List<AuditLogEntry>
                        {
                            AuditLogEntry.CreateInsertEntry("AppUsers", newUserId, user.CreatedBy, "Username", user.Username),
                            AuditLogEntry.CreateInsertEntry("AppUsers", newUserId, user.CreatedBy, "FullName", user.FullName),
                            AuditLogEntry.CreateInsertEntry("AppUsers", newUserId, user.CreatedBy, "Email", user.Email),
                            AuditLogEntry.CreateInsertEntry("AppUsers", newUserId, user.CreatedBy, "RoleId", user.RoleId?.ToString()),
                            AuditLogEntry.CreateInsertEntry("AppUsers", newUserId, user.CreatedBy, "IsActive", user.IsActive.ToString())
                        };
                        await _auditLogService.LogBatchAsync(auditEntries);
                        
                        return true;
                    }
                    else
                    {
                        Logger.Warn($"Failed to create user: {user.Username}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating user: {user.Username}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get the current user data BEFORE updating to track changes
                    var sqlGetOld = @"
                        SELECT UserId, Username, FullName, Email, RoleId, IsActive
                        FROM AppUsers 
                        WHERE UserId = @UserId AND DeletedAt IS NULL";
                    
                    var oldUser = await connection.QuerySingleOrDefaultAsync<User>(sqlGetOld, new { UserId = user.UserId });
                    
                    if (oldUser == null)
                    {
                        Logger.Warn($"User not found for update: {user.UserId}");
                        return false;
                    }
                    
                    // Now perform the update
                    var sql = @"
                        UPDATE AppUsers 
                        SET Username = @Username,
                            FullName = @FullName,
                            Email = @Email,
                            RoleId = @RoleId,
                            IsActive = @IsActive,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE UserId = @UserId AND DeletedAt IS NULL";

                    user.ModifiedAt = DateTime.UtcNow;
                    user.ModifiedBy = App.CurrentUser?.Username ?? "System";

                    int rowsAffected = await connection.ExecuteAsync(sql, user);

                    if (rowsAffected > 0)
                    {
                        Logger.Info($"User updated successfully: {user.Username} by {user.ModifiedBy}");
                        
                        // Log detailed changes to audit log
                        var auditEntries = new List<AuditLogEntry>();
                        
                        if (oldUser.Username != user.Username)
                            auditEntries.Add(AuditLogEntry.CreateUpdateEntry("AppUsers", user.UserId, "Username", oldUser.Username, user.Username, user.ModifiedBy));
                        
                        if (oldUser.FullName != user.FullName)
                            auditEntries.Add(AuditLogEntry.CreateUpdateEntry("AppUsers", user.UserId, "FullName", oldUser.FullName, user.FullName, user.ModifiedBy));
                        
                        if (oldUser.Email != user.Email)
                            auditEntries.Add(AuditLogEntry.CreateUpdateEntry("AppUsers", user.UserId, "Email", oldUser.Email, user.Email, user.ModifiedBy));
                        
                        if (oldUser.RoleId != user.RoleId)
                            auditEntries.Add(AuditLogEntry.CreateUpdateEntry("AppUsers", user.UserId, "RoleId", oldUser.RoleId?.ToString(), user.RoleId?.ToString(), user.ModifiedBy));
                        
                        if (oldUser.IsActive != user.IsActive)
                            auditEntries.Add(AuditLogEntry.CreateUpdateEntry("AppUsers", user.UserId, "IsActive", oldUser.IsActive.ToString(), user.IsActive.ToString(), user.ModifiedBy));
                        
                        // Only log if there were actual changes
                        if (auditEntries.Any())
                        {
                            await _auditLogService.LogBatchAsync(auditEntries);
                            Logger.Debug($"Logged {auditEntries.Count} field changes to audit log for user {user.UserId}");
                        }
                        
                        return true;
                    }
                    else
                    {
                        Logger.Warn($"Failed to update user: {user.Username}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating user: {user.Username}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get user info before deleting for audit log
                    var sqlGetUser = "SELECT Username, FullName, Email FROM AppUsers WHERE UserId = @UserId AND DeletedAt IS NULL";
                    var userInfo = await connection.QuerySingleOrDefaultAsync<User>(sqlGetUser, new { UserId = userId });
                    
                    if (userInfo == null)
                    {
                        Logger.Warn($"Failed to delete user. UserId: {userId} not found or already deleted.");
                        return false;
                    }
                    
                    // Soft delete: Set DeletedAt and DeletedBy instead of hard deleting
                    var sql = @"
                        UPDATE AppUsers 
                        SET DeletedAt = @DeletedAt,
                            DeletedBy = @DeletedBy,
                            IsActive = 0
                        WHERE UserId = @UserId AND DeletedAt IS NULL";

                    var deletedBy = App.CurrentUser?.Username ?? "System";
                    var deletedAt = DateTime.UtcNow;

                    int rowsAffected = await connection.ExecuteAsync(sql, new 
                    { 
                        UserId = userId,
                        DeletedAt = deletedAt,
                        DeletedBy = deletedBy
                    });

                    if (rowsAffected > 0)
                    {
                        Logger.Info($"User soft deleted successfully. UserId: {userId} ({userInfo.Username}) by {deletedBy}");
                        
                        // Log to detailed audit log
                        var auditEntry = AuditLogEntry.CreateDeleteEntry("AppUsers", userId, deletedBy, "Username", userInfo.Username);
                        await _auditLogService.LogAsync(auditEntry);
                        
                        return true;
                    }
                    else
                    {
                        Logger.Warn($"Failed to delete user. UserId: {userId} not found or already deleted.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting user. UserId: {userId}", ex);
                return false;
            }
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var sql = "SELECT RoleId, RoleName, Description FROM Roles";
                    return await connection.QueryAsync<Role>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving roles" ,ex);
                throw;
            }
        }

        /// <summary>
        /// Unlocks a user account manually (for admin use).
        /// </summary>
        public async Task<bool> UnlockUserAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    await UnlockUserInternalAsync(connection, userId);
                    Logger.Info($"User account unlocked manually. UserId: {userId} by {App.CurrentUser?.Username ?? "System"}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error unlocking user. UserId: {userId}", ex);
                return false;
            }
        }

        /// <summary>
        /// Internal method to unlock a user (used by both manual and auto-unlock).
        /// </summary>
        private async Task UnlockUserInternalAsync(SqlConnection connection, int userId)
        {
            var sql = @"
                UPDATE AppUsers 
                SET IsLocked = 0,
                    FailedLoginAttempts = 0
                WHERE UserId = @UserId";
            
            await connection.ExecuteAsync(sql, new { UserId = userId });
        }

        /// <summary>
        /// Gets the lockout status and remaining time for a user.
        /// </summary>
        public async Task<(bool IsLocked, int? RemainingMinutes)> GetLockoutStatusAsync(int userId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT IsLocked, LastLockoutAt
                        FROM AppUsers 
                        WHERE UserId = @UserId";

                    var result = await connection.QuerySingleOrDefaultAsync<(bool IsLocked, DateTime? LastLockoutAt)>(
                        sql, new { UserId = userId });

                    if (!result.IsLocked)
                    {
                        return (false, null);
                    }

                    if (result.LastLockoutAt.HasValue)
                    {
                        var lockoutDuration = TimeSpan.FromMinutes(30);
                        var elapsed = DateTime.UtcNow - result.LastLockoutAt.Value;
                        var remaining = lockoutDuration - elapsed;
                        
                        if (remaining.TotalMinutes > 0)
                        {
                            return (true, (int)Math.Ceiling(remaining.TotalMinutes));
                        }
                    }

                    return (true, null);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting lockout status for UserId: {userId}", ex);
                return (false, null);
            }
        }
    }
}
