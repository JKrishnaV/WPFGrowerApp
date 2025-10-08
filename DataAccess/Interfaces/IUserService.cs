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

        /// <summary>
        /// Manually unlocks a user account (admin function).
        /// </summary>
        /// <param name="userId">The ID of the user to unlock.</param>
        /// <returns>True if the account was unlocked successfully, otherwise false.</returns>
        Task<bool> UnlockUserAsync(int userId);

        /// <summary>
        /// Gets the lockout status and remaining time for a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>Tuple with IsLocked status and RemainingMinutes (null if not locked or no time info).</returns>
        Task<(bool IsLocked, int? RemainingMinutes)> GetLockoutStatusAsync(int userId);
    }
}
