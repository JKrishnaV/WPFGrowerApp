using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations for retrieving Depot information.
    /// </summary>
    public interface IDepotService
    {
        /// <summary>
        /// Gets a list of all available depots.
        /// </summary>
        /// <returns>A list of Depot objects.</returns>
        Task<List<Depot>> GetAllDepotsAsync();
    }
}
