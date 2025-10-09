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
                            ProductCode,
                            ProductName as Description,
                            '' as ShortDescription,
                            0 as Deduct,
                            NULL as Category,
                            ChargeGST as ChargeGst,
                            '' as Variety
                        FROM Products
                        WHERE IsActive = 1
                        ORDER BY ProductName"; 
                    return await connection.QueryAsync<Product>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all products: {ex.Message}", ex);
                throw; // Rethrow to allow higher layers to handle
            }
        }

        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            ProductId,
                            ProductCode,
                            ProductName as Description,
                            '' as ShortDescription,
                            0 as Deduct,
                            NULL as Category,
                            ChargeGST as ChargeGst,
                            '' as Variety
                        FROM Products
                        WHERE ProductId = @ProductId AND IsActive = 1";
                    return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductId = productId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting product by ID '{productId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Product?> GetProductByCodeAsync(string productCode)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            ProductId,
                            ProductCode,
                            ProductName as Description,
                            '' as ShortDescription,
                            0 as Deduct,
                            NULL as Category,
                            ChargeGST as ChargeGst,
                            '' as Variety
                        FROM Products
                        WHERE ProductCode = @ProductCode AND IsActive = 1";
                    return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductCode = productCode });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting product by code '{productCode}': {ex.Message}", ex);
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
                            @ProductCode, @Description, @Description, @ChargeGst, 'LBS', 1, 0, GETDATE(), @OperatorInitials
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var parameters = new
                    {
                        product.ProductCode,
                        product.Description,
                        product.ChargeGst,
                        OperatorInitials = GetCurrentUserInitials()
                    };

                    int newId = await connection.ExecuteScalarAsync<int>(sql, parameters);
                    product.ProductId = newId;
                    int affectedRows = newId > 0 ? 1 : 0;
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
                        WHERE ProductId = @ProductId AND IsActive = 1";

                    var parameters = new
                    {
                        product.ProductId,
                        product.Description,
                        product.ChargeGst,
                        OperatorInitials = GetCurrentUserInitials()
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating product '{product.ProductId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int productId, string operatorInitials)
        {
            if (productId <= 0) throw new ArgumentException("Product ID must be positive.", nameof(productId));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials();

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
                        WHERE ProductId = @ProductId AND IsActive = 1";

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

        public async Task<bool> DeleteProductByCodeAsync(string productCode, string operatorInitials)
        {
            if (string.IsNullOrWhiteSpace(productCode)) throw new ArgumentException("Product code cannot be empty.", nameof(productCode));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials();

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
                        WHERE ProductCode = @ProductCode AND IsActive = 1";
                    int affectedRows = await connection.ExecuteAsync(sql, new { ProductCode = productCode, OperatorInitials = operatorInitials });
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting product by code '{productCode}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the total count of active products (optimized for dashboard)
        /// </summary>
        public async Task<int> GetTotalProductsCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Products WHERE IsActive = 1";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetTotalProductsCountAsync: {ex.Message}", ex);
                throw;
            }
        }
    }
}
