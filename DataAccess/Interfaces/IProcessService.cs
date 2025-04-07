using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IProcessService
    {
        /// <summary>
        /// Gets a process by its ID.
        /// </summary>
        /// <param name="processId">The unique identifier for the process (PROCESS column).</param>
        /// <returns>The Process object or null if not found.</returns>
        Task<Process> GetProcessByIdAsync(string processId);

        /// <summary>
        /// Gets all processes.
        /// </summary>
        /// <returns>A collection of all Process objects.</returns>
        Task<IEnumerable<Process>> GetAllProcessesAsync();

        /// <summary>
        /// Adds a new process to the database.
        /// </summary>
        /// <param name="process">The process object to add.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        Task<bool> AddProcessAsync(Process process);

        /// <summary>
        /// Updates an existing process in the database.
        /// </summary>
        /// <param name="process">The process object with updated information.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        Task<bool> UpdateProcessAsync(Process process);

        /// <summary>
        /// Deletes a process from the database (logically, by setting QDEL fields).
        /// </summary>
        /// <param name="processId">The ID of the process to delete.</param>
        /// <param name="operatorInitials">The initials of the operator performing the deletion.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        Task<bool> DeleteProcessAsync(string processId, string operatorInitials); 
    }
}
