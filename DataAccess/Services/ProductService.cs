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
                            PRODUCT as ProductId,
                            Description as Description, -- Explicit alias
                            SHORTDesc as ShortDescription,
                            DEDUCT as Deduct,
                            CATEGORY as Category,
                            CHG_GST as ChargeGst, 
                            VARIETY as Variety,
                            QADD_DATE, QADD_TIME, QADD_OP,
                            QED_DATE, QED_TIME, QED_OP,
                            QDEL_DATE, QDEL_TIME, QDEL_OP
                        FROM Product
                        WHERE QDEL_DATE IS NULL 
                        ORDER BY Description"; // Order alphabetically for display
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
                            PRODUCT as ProductId,
                            Description as Description, -- Explicit alias
                            SHORTDesc as ShortDescription,
                            DEDUCT as Deduct,
                            CATEGORY as Category,
                            CHG_GST as ChargeGst,
                            VARIETY as Variety,
                            QADD_DATE, QADD_TIME, QADD_OP,
                            QED_DATE, QED_TIME, QED_OP,
                            QDEL_DATE, QDEL_TIME, QDEL_OP
                        FROM Product
                        WHERE PRODUCT = @ProductId AND QDEL_DATE IS NULL";
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
                        INSERT INTO Product (
                            PRODUCT, Description, SHORTDesc, DEDUCT, CATEGORY, CHG_GST, VARIETY, -- Changed column name
                            QADD_DATE, QADD_TIME, QADD_OP, QED_DATE, QED_TIME, QED_OP, QDEL_DATE, QDEL_TIME, QDEL_OP
                        ) VALUES (
                            @ProductId, @Description, @ShortDescription, @Deduct, @Category, @ChargeGst, @Variety, -- Model property name is correct here
                            @QADD_DATE, @QADD_TIME, @QADD_OP, NULL, NULL, NULL, NULL, NULL, NULL
                        )";

                    // Set audit fields for add
                    product.QADD_DATE = DateTime.Today;
                    product.QADD_TIME = DateTime.Now.ToString("HH:mm:ss"); // Or appropriate format
                    product.QADD_OP = GetCurrentUserInitials(); 

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
                        UPDATE Product SET
                            Description = @Description,
                            SHORTDesc = @ShortDescription, -- Changed column name, model property name is correct here
                            DEDUCT = @Deduct,
                            CATEGORY = @Category,
                            CHG_GST = @ChargeGst,
                            VARIETY = @Variety,
                            QED_DATE = @QED_DATE,
                            QED_TIME = @QED_TIME,
                            QED_OP = @QED_OP
                        WHERE PRODUCT = @ProductId AND QDEL_DATE IS NULL"; 

                    // Set audit fields for edit
                    product.QED_DATE = DateTime.Today;
                    product.QED_TIME = DateTime.Now.ToString("HH:mm:ss"); // Or appropriate format
                    product.QED_OP = GetCurrentUserInitials();

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
                        UPDATE Product SET
                            QDEL_DATE = @QDEL_DATE,
                            QDEL_TIME = @QDEL_TIME,
                            QDEL_OP = @QDEL_OP
                        WHERE PRODUCT = @ProductId AND QDEL_DATE IS NULL"; 

                    var parameters = new 
                    {
                        ProductId = productId,
                        QDEL_DATE = DateTime.Today,
                        QDEL_TIME = DateTime.Now.ToString("HH:mm:ss"), // Or appropriate format
                        QDEL_OP = operatorInitials
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
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
