using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IProductService
    {
        /// <summary>
        /// Gets a product by its ID.
        /// </summary>
        /// <param name="productId">The unique identifier for the product (PRODUCT column).</param>
        /// <returns>The Product object or null if not found.</returns>
    Task<Product?> GetProductByIdAsync(int productId);
    Task<Product?> GetProductByCodeAsync(string productCode);

        /// <summary>
        /// Gets all products.
        /// </summary>
        /// <returns>A collection of all Product objects.</returns>
        Task<IEnumerable<Product>> GetAllProductsAsync();

        /// <summary>
        /// Adds a new product to the database.
        /// </summary>
        /// <param name="product">The product object to add.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        Task<bool> AddProductAsync(Product product);

        /// <summary>
        /// Updates an existing product in the database.
        /// </summary>
        /// <param name="product">The product object with updated information.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
        Task<bool> UpdateProductAsync(Product product);

        /// <summary>
        /// Deletes a product from the database (logically, by setting QDEL fields).
        /// </summary>
        /// <param name="productId">The ID of the product to delete.</param>
        /// <param name="operatorInitials">The initials of the operator performing the deletion.</param>
        /// <returns>True if the operation was successful, otherwise false.</returns>
    Task<bool> DeleteProductAsync(int productId, string operatorInitials);
    Task<bool> DeleteProductByCodeAsync(string productCode, string operatorInitials);
    }
}
