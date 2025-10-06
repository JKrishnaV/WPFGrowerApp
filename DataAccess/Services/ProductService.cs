using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class ProductService : BaseDatabaseService, IProductService
    {
        // Helper to get current user initials (replace with actual implementation if available)
        private string GetCurrentUserInitials() => App.CurrentUser?.Username ?? "SYSTEM"; 

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Map database columns to model properties
                    // Ensure QDEL_DATE is NULL for active records
                    var sql = @"
                        SELECT
                            ProductId,
                            ProductName as Description,
                            '' as ShortDescription,
                            0 as Deduct,
                            NULL as Category,
                            ChargeGST as ChargeGst,
                            '' as Variety
                        FROM Products
                        WHERE IsActive = 1
                        ORDER BY ProductName"; // Order alphabetically for display
                    return await connection.QueryAsync<Product>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all products: {ex.Message}", ex);
                throw; // Rethrow to allow higher layers to handle
            }
        }

        public async Task<Product> GetProductByIdAsync(string productId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            ProductCode as ProductId,
                            ProductName as Description,
                            '' as ShortDescription,
                            0 as Deduct,
                            '' as Category,
                            ChargeGST as ChargeGst,
                            '' as Variety
                        FROM Products
                        WHERE ProductCode = @ProductId AND IsActive = 1";
                    return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductId = productId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting product by ID '{productId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> AddProductAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO Products (
                            ProductCode, ProductName, Description, ChargeGST, UnitOfMeasure, IsActive, DisplayOrder, CreatedAt, CreatedBy
                        ) VALUES (
                            @ProductId, @Description, @Description, @ChargeGst, 'LBS', 1, 0, GETDATE(), @OperatorInitials
                        )";

                    int affectedRows = await connection.ExecuteAsync(sql, product);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding product '{product.ProductId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
             if (product == null) throw new ArgumentNullException(nameof(product));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Products SET
                            ProductName = @Description,
                            Description = @Description,
                            ChargeGST = @ChargeGst,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @OperatorInitials
                        WHERE ProductCode = @ProductId AND IsActive = 1";

                    int affectedRows = await connection.ExecuteAsync(sql, product);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating product '{product.ProductId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(string productId, string operatorInitials)
        {
            if (string.IsNullOrWhiteSpace(productId)) throw new ArgumentException("Product ID cannot be empty.", nameof(productId));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials(); // Fallback

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Products SET
                            DeletedAt = GETDATE(),
                            DeletedBy = @OperatorInitials,
                            IsActive = 0
                        WHERE ProductCode = @ProductId AND IsActive = 1";

                    int affectedRows = await connection.ExecuteAsync(sql, new { ProductId = productId, OperatorInitials = operatorInitials });
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting product '{productId}': {ex.Message}", ex);
                throw;
            }
        }
    }
}
