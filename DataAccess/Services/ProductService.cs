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
        public async Task<List<Product>> GetAllProductsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Map database columns to model properties
                    var sql = @"
                        SELECT
                            PRODUCT as ProductId,
                            Description,
                            SHORTDesc as ShortDescription,
                            DEDUCT as Deduct,
                            CATEGORY as Category,
                            CHG_GST as ChgGst,
                            VARIETY as Variety
                        FROM Product
                        ORDER BY Description"; // Order alphabetically for display
                    var products = await connection.QueryAsync<Product>(sql);
                    return products.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all products: {ex.Message}", ex);
                throw; // Rethrow or return empty list
            }
        }
    }
}
