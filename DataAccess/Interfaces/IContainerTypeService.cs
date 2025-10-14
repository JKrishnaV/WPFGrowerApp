using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for container type service operations.
    /// Provides CRUD operations for container type definitions.
    /// </summary>
    public interface IContainerTypeService
    {
        /// <summary>
        /// Retrieves all container types, ordered by display order and container code.
        /// </summary>
        /// <returns>List of all container types</returns>
        Task<IEnumerable<ContainerType>> GetAllAsync();

        /// <summary>
        /// Retrieves only active (IsActive = true) container types.
        /// Used for dropdowns and lookups during receipt entry.
        /// </summary>
        /// <returns>List of active container types</returns>
        Task<IEnumerable<ContainerType>> GetActiveAsync();

        /// <summary>
        /// Retrieves a specific container type by ID.
        /// </summary>
        /// <param name="containerId">Container ID</param>
        /// <returns>ContainerType if found, null otherwise</returns>
        Task<ContainerType?> GetByIdAsync(int containerId);

        /// <summary>
        /// Checks if a container code already exists in the database.
        /// </summary>
        /// <param name="containerCode">Container code to check</param>
        /// <param name="excludeContainerId">Optional: Exclude this container ID (used during updates)</param>
        /// <returns>True if container code exists, false otherwise</returns>
        Task<bool> ContainerCodeExistsAsync(string containerCode, int? excludeContainerId = null);

        /// <summary>
        /// Creates a new container type.
        /// </summary>
        /// <param name="containerType">Container type to create</param>
        /// <param name="username">Username of the operator creating the record</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> CreateAsync(ContainerType containerType, string username);

        /// <summary>
        /// Updates an existing container type.
        /// </summary>
        /// <param name="containerType">Container type with updated values</param>
        /// <param name="username">Username of the operator updating the record</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateAsync(ContainerType containerType, string username);

        /// <summary>
        /// Deletes a container type (soft delete with DeletedAt/DeletedBy).
        /// </summary>
        /// <param name="containerId">Container ID to delete</param>
        /// <param name="username">Username of the operator deleting the record</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteAsync(int containerId, string username);

        /// <summary>
        /// Gets the count of receipts using a specific container type.
        /// Used to prevent deletion of container types that are in use.
        /// </summary>
        /// <param name="containerId">Container ID to check</param>
        /// <returns>Count of receipts using this container</returns>
        Task<int> GetUsageCountAsync(int containerId);

        /// <summary>
        /// Checks if a container type can be safely deleted.
        /// </summary>
        /// <param name="containerId">Container ID to check</param>
        /// <returns>True if safe to delete, false if in use</returns>
        Task<bool> CanDeleteAsync(int containerId);
    }
}
