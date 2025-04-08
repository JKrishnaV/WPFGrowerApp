using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Authenticates a user based on username and password.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The plain text password.</param>
        /// <returns>The authenticated User object if successful, otherwise null.</returns>
        Task<User> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Sets or updates a user's password securely.
        /// </summary>
        /// <param name="username">The username of the user to update.</param>
        /// <param name="newPassword">The new plain text password.</param>
        /// <returns>True if the password was updated successfully, otherwise false.</returns>
        Task<bool> SetPasswordAsync(string username, string newPassword);

        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User> GetUserByIdAsync(int userId);
        Task<User> GetUserByUsernameAsync(string username);
        Task<bool> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
        Task<IEnumerable<Role>> GetAllRolesAsync();

        // Add other methods as needed, e.g.:
        // Task<User> GetUserByUsernameAsync(string username);
        // Task CreateUserAsync(User user, string password); // Handles hashing internally
        // Task LockUserAsync(string username);
        // Task UnlockUserAsync(string username);
    }
}
