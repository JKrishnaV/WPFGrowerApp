using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IGrowerService : IDatabaseService
    {
        Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm);
    Task<Grower> GetGrowerByNumberAsync(string growerNumber);
    Task<Grower> GetGrowerByIdAsync(int growerId);
        Task<bool> SaveGrowerAsync(Grower grower);
        Task<List<GrowerSearchResult>> GetAllGrowersAsync(); // Returns search result model
        Task<List<string>> GetUniqueProvincesAsync();

        /// <summary>
        /// Gets a simplified list of all growers (Number and Name) for populating lists.
        /// </summary>
        /// <returns>A list of GrowerInfo objects.</returns>
        Task<List<GrowerInfo>> GetAllGrowersBasicInfoAsync();

        /// <summary>
        /// Gets a list of growers currently marked as OnHold.
        /// </summary>
        /// <returns>A list of GrowerInfo objects for on-hold growers.</returns>
        Task<List<GrowerInfo>> GetOnHoldGrowersAsync();
    }
}
