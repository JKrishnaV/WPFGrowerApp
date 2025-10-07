using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations for managing Depot information.
    /// </summary>
    public interface IDepotService
    {
        /// <summary>
        /// Gets a depot by its ID.
        /// </summary>
        /// <param name="depotId">The unique identifier for the depot (DEPOT column).</param>
        /// <returns>The Depot object or null if not found.</returns>
    Task<Depot?> GetDepotByIdAsync(int depotId);
    Task<Depot?> GetDepotByCodeAsync(string depotCode);

        /// <summary>
        /// Gets all depots.
        /// </summary>
        /// <returns>A collection of all Depot objects.</returns>
        Task<IEnumerable<Depot>> GetAllDepotsAsync(); // Changed return type to IEnumerable

        /// <summary>
        /// Adds a new depot to the database.
        /// </summary>
        /// <param name="depot">The depot object to add.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        Task<bool> AddDepotAsync(Depot depot);

        /// <summary>
        /// Updates an existing depot in the database.
        /// </summary>
        /// <param name="depot">The depot object with updated information.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        Task<bool> UpdateDepotAsync(Depot depot);

        /// <summary>
        /// Deletes a depot from the database (logically, by setting QDEL fields).
        /// </summary>
        /// <param name="depotId">The ID of the depot to delete.</param>
        /// <param name="operatorInitials">The initials of the operator performing the deletion.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
    Task<bool> DeleteDepotAsync(int depotId, string operatorInitials); 
    Task<bool> DeleteDepotByCodeAsync(string depotCode, string operatorInitials); 
    }
}
