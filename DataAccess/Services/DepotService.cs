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

        public async Task<Depot> GetDepotByIdAsync(string depotId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            DepotCode as DepotId, 
                            DepotName
                        FROM Depots 
                        WHERE DepotCode = @DepotId AND IsActive = 1";
                    return await connection.QueryFirstOrDefaultAsync<Depot>(sql, new { DepotId = depotId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting depot by ID '{depotId}': {ex.Message}", ex);
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
                            @DepotId, @DepotName, 1, 0,
                            GETDATE(), @OperatorInitials
                        )";

                    var parameters = new
                    {
                        DepotId = depot.DepotId,
                        DepotName = depot.DepotName,
                        OperatorInitials = GetCurrentUserInitials()
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding depot '{depot.DepotId}': {ex.Message}", ex);
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
                        WHERE DepotCode = @DepotId AND IsActive = 1"; 

                    var parameters = new
                    {
                        DepotId = depot.DepotId,
                        DepotName = depot.DepotName,
                        OperatorInitials = GetCurrentUserInitials()
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating depot '{depot.DepotId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteDepotAsync(string depotId, string operatorInitials)
        {
            if (string.IsNullOrWhiteSpace(depotId)) throw new ArgumentException("Depot ID cannot be empty.", nameof(depotId));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials(); // Fallback

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
                        WHERE DepotCode = @DepotId AND DeletedAt IS NULL"; 

                    var parameters = new 
                    {
                        DepotId = depotId,
                        OperatorInitials = operatorInitials
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
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
    }
}
