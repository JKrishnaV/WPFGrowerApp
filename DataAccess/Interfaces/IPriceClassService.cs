using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for price class data operations.
    /// </summary>
    public interface IPriceClassService : IDatabaseService
    {
        /// <summary>
        /// Gets all price classes.
        /// </summary>
        /// <returns>List of all price classes</returns>
        Task<List<PriceClass>> GetAllPriceClassesAsync();

        /// <summary>
        /// Gets a price class by ID.
        /// </summary>
        /// <param name="priceClassId">The price class ID</param>
        /// <returns>The price class, or null if not found</returns>
        Task<PriceClass> GetPriceClassByIdAsync(int priceClassId);

        /// <summary>
        /// Creates a new price class.
        /// </summary>
        /// <param name="priceClass">The price class to create</param>
        /// <returns>The ID of the created price class</returns>
        Task<int> CreatePriceClassAsync(PriceClass priceClass);

        /// <summary>
        /// Updates an existing price class.
        /// </summary>
        /// <param name="priceClass">The price class to update</param>
        /// <returns>True if the update was successful</returns>
        Task<bool> UpdatePriceClassAsync(PriceClass priceClass);

        /// <summary>
        /// Deletes a price class.
        /// </summary>
        /// <param name="priceClassId">The price class ID to delete</param>
        /// <returns>True if the deletion was successful</returns>
        Task<bool> DeletePriceClassAsync(int priceClassId);
    }
}
