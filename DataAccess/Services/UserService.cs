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
                               RoleId, IsActive, DateCreated, LastLoginDate, 
                               FailedLoginAttempts, IsLockedOut, LastLockoutDate
                        FROM AppUsers 
                        WHERE Username = @Username";
                    
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
                         Logger.Warn($"Authentication failed: User '{username}' is locked out.");
                         // Optionally check if lockout duration has expired
                         return null; // Account locked out
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
                            IsLockedOut = 0,         -- Unlock account on password change
                            LastLockoutDate = NULL
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
            // Basic lockout logic: Lock after 5 failed attempts (adjust as needed)
            const int maxFailedAttempts = 5; 
            int newAttemptCount = user.FailedLoginAttempts + 1;
            bool lockAccount = newAttemptCount >= maxFailedAttempts;

            var sql = @"
                UPDATE AppUsers 
                SET FailedLoginAttempts = @NewAttemptCount, 
                    IsLockedOut = @LockAccount,
                    LastLockoutDate = CASE WHEN @LockAccount = 1 THEN GETUTCDATE() ELSE LastLockoutDate END
                WHERE UserId = @UserId";
            
            try
            {
                await connection.ExecuteAsync(sql, new 
                { 
                    NewAttemptCount = newAttemptCount, 
                    LockAccount = lockAccount, 
                    UserId = user.UserId 
                });

                if(lockAccount)
                {
                    Logger.Warn($"User account '{user.Username}' locked due to excessive failed login attempts.");
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
                    LastLoginDate = GETUTCDATE(),
                    IsLockedOut = 0, -- Ensure account is unlocked on successful login
                    LastLockoutDate = NULL 
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
                        SELECT UserId, Username, FullName, Email, RoleId, 
                               IsActive, DateCreated, LastLoginDate, 
                               FailedLoginAttempts, IsLockedOut, LastLockoutDate
                        FROM AppUsers";

                    Logger.Info("Executing SQL query to get all users");
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
                        SELECT UserId, Username, FullName, Email, RoleId, 
                               IsActive, DateCreated, LastLoginDate, 
                               FailedLoginAttempts, IsLockedOut, LastLockoutDate
                        FROM AppUsers 
                        WHERE UserId = @UserId";

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
                        SELECT UserId, Username, FullName, Email, RoleId, 
                               IsActive, DateCreated, LastLoginDate, 
                               FailedLoginAttempts, IsLockedOut, LastLockoutDate
                        FROM AppUsers 
                        WHERE Username = @Username";

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
                user.DateCreated = DateTime.UtcNow;

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO AppUsers (
                            Username, FullName, Email, PasswordHash, PasswordSalt,
                            RoleId, IsActive, DateCreated, FailedLoginAttempts, IsLockedOut
                        ) VALUES (
                            @Username, @FullName, @Email, @PasswordHash, @PasswordSalt,
                            @RoleId, @IsActive, @DateCreated, 0, 0
                        )";

                    int rowsAffected = await connection.ExecuteAsync(sql, user);
                    
                    if (rowsAffected > 0)
                    {
                        Logger.Info($"User created successfully: {user.Username}");
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
                    var sql = @"
                        UPDATE AppUsers 
                        SET Username = @Username,
                            FullName = @FullName,
                            Email = @Email,
                            RoleId = @RoleId,
                            IsActive = @IsActive
                        WHERE UserId = @UserId";

                    int rowsAffected = await connection.ExecuteAsync(sql, user);

                    if (rowsAffected > 0)
                    {
                        Logger.Info($"User updated successfully: {user.Username}");
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
                    var sql = "DELETE FROM AppUsers WHERE UserId = @UserId";

                    int rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId });

                    if (rowsAffected > 0)
                    {
                        Logger.Info($"User deleted successfully. UserId: {userId}");
                        return true;
                    }
                    else
                    {
                        Logger.Warn($"Failed to delete user. UserId: {userId} not found.");
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
    }
}
