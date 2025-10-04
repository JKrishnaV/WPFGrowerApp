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
                            ProductCode as ProductId,
                            ProductName as Description,
                            ISNULL(ShortDescription, '') as ShortDescription,
                            ISNULL(MarketingDeduction, 0) as Deduct,
                            ISNULL(ReportCategory, 0) as Category,
                            ChargeGST as ChargeGst,
                            ISNULL(VarietyCode, '') as Variety,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
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
                            ISNULL(ShortDescription, '') as ShortDescription,
                            ISNULL(MarketingDeduction, 0) as Deduct,
                            ISNULL(ReportCategory, 0) as Category,
                            ChargeGST as ChargeGst,
                            ISNULL(VarietyCode, '') as Variety,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
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
                    
                    // Get current user for audit trail
                    string currentUser = GetCurrentUserInitials();
                    
                    var sql = @"
                        INSERT INTO Products (
                            ProductCode, ProductName, Description, ChargeGST, UnitOfMeasure, IsActive, DisplayOrder,
                            ShortDescription, VarietyCode, MarketingDeduction, ReportCategory,
                            CreatedAt, CreatedBy
                        ) VALUES (
                            @ProductId, @Description, @Description, @ChargeGst, 'LBS', 1, 0,
                            @ShortDescription, @Variety, @Deduct, @Category,
                            GETDATE(), @CreatedBy
                        )";

                    int affectedRows = await connection.ExecuteAsync(sql, new {
                        product.ProductId,
                        product.Description,
                        product.ChargeGst,
                        product.ShortDescription,
                        product.Variety,
                        product.Deduct,
                        product.Category,
                        CreatedBy = currentUser
                    });
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
                    
                    // Get current user for audit trail
                    string currentUser = GetCurrentUserInitials();
                    
                    var sql = @"
                        UPDATE Products SET
                            ProductName = @Description,
                            Description = @Description,
                            ChargeGST = @ChargeGst,
                            ShortDescription = @ShortDescription,
                            VarietyCode = @Variety,
                            MarketingDeduction = @Deduct,
                            ReportCategory = @Category,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ProductCode = @ProductId AND IsActive = 1";

                    int affectedRows = await connection.ExecuteAsync(sql, new {
                        product.ProductId,
                        product.Description,
                        product.ChargeGst,
                        product.ShortDescription,
                        product.Variety,
                        product.Deduct,
                        product.Category,
                        ModifiedBy = currentUser
                    });
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
                            IsActive = 0,
                            DeletedAt = GETDATE(),
                            DeletedBy = @DeletedBy,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ProductCode = @ProductId AND IsActive = 1"; 

                    int affectedRows = await connection.ExecuteAsync(sql, new { 
                        ProductId = productId, 
                        DeletedBy = operatorInitials,
                        ModifiedBy = operatorInitials 
                    });
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
