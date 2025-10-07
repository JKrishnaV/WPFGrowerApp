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
                            DepotName
                        FROM Depots
                        WHERE IsActive = 1
                        ORDER BY DepotName";
                    return await connection.QueryAsync<Depot>(sql); // Return IEnumerable
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
                            DepotName
                        FROM Depots 
                        WHERE DepotId = @DepotId AND IsActive = 1";
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
                            DepotName
                        FROM Depots 
                        WHERE DepotCode = @DepotCode AND IsActive = 1";
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
                            DepotCode, DepotName, IsActive, DisplayOrder,
                            CreatedAt, CreatedBy
                        ) VALUES (
                            @DepotCode, @DepotName, 1, 0,
                            GETDATE(), @OperatorInitials
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var newId = await connection.ExecuteScalarAsync<int>(sql, new { depot.DepotCode, depot.DepotName, OperatorInitials = GetCurrentUserInitials() });
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
                            DepotName = @DepotName,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @OperatorInitials
                        WHERE DepotId = @DepotId AND IsActive = 1"; 

                    int affectedRows = await connection.ExecuteAsync(sql, new { depot.DepotName, depot.DepotId, OperatorInitials = GetCurrentUserInitials() });
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
