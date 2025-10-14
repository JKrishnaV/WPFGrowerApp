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
    public class DepotService : BaseDatabaseService, IDepotService
    {
        // Helper to get current user initials (replace with actual implementation if available)
        private string GetCurrentUserInitials() => App.CurrentUser?.Username ?? "SYSTEM"; 

        public async Task<IEnumerable<Depot>> GetAllDepotsAsync() // Changed return type
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Map database columns to model properties
                    // Include audit fields and filter by QDEL_DATE
                    var sql = @"
                        SELECT
                            DepotId,
                            DepotCode,
                            DepotName,
                            Address,
                            City,
                            Province,
                            PostalCode,
                            PhoneNumber,
                            IsActive,
                            DisplayOrder,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM Depots
                        WHERE DeletedAt IS NULL
                        ORDER BY DepotName";
                    var depots = await connection.QueryAsync<Depot>(sql);
                    
                    Logger.Info($"Found {depots.Count()} depots from Depots table");
                    foreach (var depot in depots)
                    {
                        Logger.Info($"Depot: ID={depot.DepotId}, Name={depot.DepotName}, Code={depot.DepotCode}");
                    }
                    
                    return depots;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all depots: {ex.Message}", ex);
                throw; // Rethrow to allow higher layers to handle
            }
        }

        public async Task<Depot?> GetDepotByIdAsync(int depotId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            DepotId,
                            DepotCode,
                            DepotName,
                            Address,
                            City,
                            Province,
                            PostalCode,
                            PhoneNumber,
                            IsActive,
                            DisplayOrder,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM Depots 
                        WHERE DepotId = @DepotId";
                    return await connection.QueryFirstOrDefaultAsync<Depot>(sql, new { DepotId = depotId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting depot by ID '{depotId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Depot?> GetDepotByCodeAsync(string depotCode)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            DepotId,
                            DepotCode,
                            DepotName,
                            Address,
                            City,
                            Province,
                            PostalCode,
                            PhoneNumber,
                            IsActive,
                            DisplayOrder,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM Depots 
                        WHERE DepotCode = @DepotCode";
                    return await connection.QueryFirstOrDefaultAsync<Depot>(sql, new { DepotCode = depotCode });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting depot by code '{depotCode}': {ex.Message}", ex);
                throw;
            }
        }

    public async Task<bool> AddDepotAsync(Depot depot)
        {
            if (depot == null) throw new ArgumentNullException(nameof(depot));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO Depots (
                            DepotCode, DepotName, Address, City, Province, PostalCode, PhoneNumber,
                            IsActive, DisplayOrder, CreatedAt, CreatedBy
                        ) VALUES (
                            @DepotCode, @DepotName, @Address, @City, @Province, @PostalCode, @PhoneNumber,
                            @IsActive, @DisplayOrder, @CreatedAt, @CreatedBy
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var parameters = new
                    {
                        depot.DepotCode,
                        depot.DepotName,
                        depot.Address,
                        depot.City,
                        depot.Province,
                        depot.PostalCode,
                        depot.PhoneNumber,
                        depot.IsActive,
                        depot.DisplayOrder,
                        CreatedAt = DateTime.Now,
                        CreatedBy = GetCurrentUserInitials()
                    };

                    var newId = await connection.ExecuteScalarAsync<int>(sql, parameters);
                    depot.DepotId = newId;
                    return newId > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding depot '{depot.DepotCode}': {ex.Message}", ex);
                throw;
            }
        }

    public async Task<bool> UpdateDepotAsync(Depot depot)
        {
             if (depot == null) throw new ArgumentNullException(nameof(depot));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Depots SET
                            DepotCode = @DepotCode,
                            DepotName = @DepotName,
                            Address = @Address,
                            City = @City,
                            Province = @Province,
                            PostalCode = @PostalCode,
                            PhoneNumber = @PhoneNumber,
                            IsActive = @IsActive,
                            DisplayOrder = @DisplayOrder,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE DepotId = @DepotId";

                    var parameters = new
                    {
                        depot.DepotId,
                        depot.DepotCode,
                        depot.DepotName,
                        depot.Address,
                        depot.City,
                        depot.Province,
                        depot.PostalCode,
                        depot.PhoneNumber,
                        depot.IsActive,
                        depot.DisplayOrder,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = GetCurrentUserInitials()
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    
                    if (affectedRows == 0)
                    {
                        Logger.Warn($"UpdateDepotAsync: No rows were affected when updating DepotId {depot.DepotId}. Depot may not exist or may have been deleted.");
                    }
                    else
                    {
                        Logger.Info($"UpdateDepotAsync: Successfully updated DepotId {depot.DepotId}. Rows affected: {affectedRows}");
                    }
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating depot '{depot.DepotId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteDepotAsync(int depotId, string operatorInitials)
        {
            if (depotId <= 0) throw new ArgumentException("Depot ID must be positive.", nameof(depotId));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Depots SET
                            DeletedAt = GETDATE(),
                            DeletedBy = @OperatorInitials,
                            IsActive = 0
                        WHERE DepotId = @DepotId AND DeletedAt IS NULL"; 

                    int affectedRows = await connection.ExecuteAsync(sql, new { DepotId = depotId, OperatorInitials = operatorInitials });
                    // Logger.Info($"Attempted soft delete for DepotId '{depotId}'. Rows affected reported by database: {affectedRows}"); // Removed log
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting depot '{depotId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteDepotByCodeAsync(string depotCode, string operatorInitials)
        {
            if (string.IsNullOrWhiteSpace(depotCode)) throw new ArgumentException("Depot code cannot be empty.", nameof(depotCode));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Depots SET
                            DeletedAt = GETDATE(),
                            DeletedBy = @OperatorInitials,
                            IsActive = 0
                        WHERE DepotCode = @DepotCode AND DeletedAt IS NULL"; 

                    int affectedRows = await connection.ExecuteAsync(sql, new { DepotCode = depotCode, OperatorInitials = operatorInitials });
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting depot by code '{depotCode}': {ex.Message}", ex);
                throw;
            }
        }
    }
}
