using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for price class data operations.
    /// </summary>
    public class PriceClassService : BaseDatabaseService, IPriceClassService
    {
        public PriceClassService() : base() { }

        public async Task<List<PriceClass>> GetAllPriceClassesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT PriceClassId, ClassCode, ClassName, Description, IsActive, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PriceClasses
                        WHERE IsActive = 1
                        ORDER BY ClassName";

                    return (await connection.QueryAsync<PriceClass>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAllPriceClassesAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<PriceClass>> GetPriceClassesByCurrencyAsync(string currencyCode)
        {
            try
            {
                // Validate currency code
                if (string.IsNullOrEmpty(currencyCode))
                {
                    Logger.Warn("Currency code is null or empty, defaulting to CAD");
                    currencyCode = "CAD";
                }

                var upperCurrencyCode = currencyCode.ToUpper();
                if (upperCurrencyCode != "CAD" && upperCurrencyCode != "USD")
                {
                    Logger.Warn($"Unsupported currency code '{currencyCode}', defaulting to CAD");
                    upperCurrencyCode = "CAD";
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Filter based on ClassCode pattern:
                    // CAD: ClassCode starts with 'C' (CL1, CL2, CL3, etc.)
                    // USD: ClassCode starts with 'U' (UL1, UL2, UL3, etc.)
                    string classCodePattern = upperCurrencyCode == "CAD" ? "C%" : "U%";
                    
                    var sql = @"
                        SELECT PriceClassId, ClassCode, ClassName, Description, IsActive, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PriceClasses
                        WHERE IsActive = 1 
                        AND ClassCode LIKE @ClassCodePattern
                        ORDER BY ClassName";

                    var parameters = new { ClassCodePattern = classCodePattern };
                    var priceClasses = (await connection.QueryAsync<PriceClass>(sql, parameters)).ToList();
                    
                    Logger.Info($"Loaded {priceClasses.Count} price classes for currency {upperCurrencyCode} (filtered by ClassCode pattern: {classCodePattern})");
                    return priceClasses;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetPriceClassesByCurrencyAsync for currency {currencyCode}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<PriceClass> GetPriceClassByIdAsync(int priceClassId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT PriceClassId, ClassName, Description, IsActive, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PriceClasses
                        WHERE PriceClassId = @PriceClassId";

                    var parameters = new { PriceClassId = priceClassId };
                    return await connection.QueryFirstOrDefaultAsync<PriceClass>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetPriceClassByIdAsync for PriceClassId {priceClassId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<int> CreatePriceClassAsync(PriceClass priceClass)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO PriceClasses (ClassName, Description, IsActive, CreatedAt, CreatedBy)
                        VALUES (@ClassName, @Description, @IsActive, GETDATE(), @CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new
                    {
                        priceClass.ClassName,
                        priceClass.Description,
                        priceClass.IsActive,
                        CreatedBy = currentUser
                    };

                    return await connection.QuerySingleAsync<int>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in CreatePriceClassAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdatePriceClassAsync(PriceClass priceClass)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PriceClasses SET
                            ClassName = @ClassName,
                            Description = @Description,
                            IsActive = @IsActive,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE PriceClassId = @PriceClassId";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new
                    {
                        priceClass.PriceClassId,
                        priceClass.ClassName,
                        priceClass.Description,
                        priceClass.IsActive,
                        ModifiedBy = currentUser
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in UpdatePriceClassAsync for PriceClassId {priceClass?.PriceClassId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeletePriceClassAsync(int priceClassId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PriceClasses SET
                            IsActive = 0,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE PriceClassId = @PriceClassId";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new { PriceClassId = priceClassId, ModifiedBy = currentUser };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in DeletePriceClassAsync for PriceClassId {priceClassId}: {ex.Message}", ex);
                throw;
            }
        }
    }
}
