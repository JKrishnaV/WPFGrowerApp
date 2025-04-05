using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations for retrieving Product information.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Gets a list of all available products.
        /// </summary>
        /// <returns>A list of Product objects.</returns>
        Task<List<Product>> GetAllProductsAsync();
    }
}
