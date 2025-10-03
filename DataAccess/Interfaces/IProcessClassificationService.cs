using System.Collections.Generic;
using System.Threading.Tasks;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service for classifying processes into Fresh vs Non-Fresh categories.
    /// Mirrors the XBase logic from GROW_AP.PRG - SetFreshProcesses() function.
    /// </summary>
    public interface IProcessClassificationService
    {
        /// <summary>
        /// Determines if a process code represents Fresh berries (proc_class = 1).
        /// </summary>
        /// <param name="processCode">The process code (e.g., 'FR', 'IQF', 'JU')</param>
        /// <returns>True if the process is Fresh (class 1), false otherwise</returns>
        Task<bool> IsFreshProcessAsync(string processCode);

        /// <summary>
        /// Gets all fresh process codes from the Process table.
        /// Caches the result for performance.
        /// </summary>
        /// <returns>List of process codes that are classified as Fresh (proc_class = 1)</returns>
        Task<List<string>> GetFreshProcessCodesAsync();

        /// <summary>
        /// Gets the process class for a given process code.
        /// Process classes: 1=Fresh, 2=Processed, 3=Juice, 4=Other
        /// </summary>
        /// <param name="processCode">The process code</param>
        /// <returns>The process class number (1-4), or 4 (Other) if not found</returns>
        Task<int> GetProcessClassAsync(string processCode);

        /// <summary>
        /// Gets the process class name for display.
        /// </summary>
        /// <param name="processClass">The process class number (1-4)</param>
        /// <returns>The class name: "Fresh", "Processed", "Juice", or "Other"</returns>
        string GetProcessClassName(int processClass);

        /// <summary>
        /// Refreshes the cached list of fresh processes from the database.
        /// Call this if process classifications change.
        /// </summary>
        Task RefreshCacheAsync();
    }
}
