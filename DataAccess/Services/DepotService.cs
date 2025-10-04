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
                            DepotCode as DepotId, 
                            DepotName,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM Depots 
                        WHERE IsActive = 1
                        ORDER BY DepotCode";
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
                            DepotName,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
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
                        INSERT INTO Depot (
                            DEPOT, DEPOTNAME,
                            QADD_DATE, QADD_TIME, QADD_OP, QED_DATE, QED_TIME, QED_OP, QDEL_DATE, QDEL_TIME, QDEL_OP
                        ) VALUES (
                            @DepotId, @DepotName,
                            @QADD_DATE, @QADD_TIME, @QADD_OP, NULL, NULL, NULL, NULL, NULL, NULL
                        )";

                    // Set audit fields for add
                    depot.QADD_DATE = DateTime.Today;
                    depot.QADD_TIME = DateTime.Now.ToString("HH:mm:ss"); // Or appropriate format
                    depot.QADD_OP = GetCurrentUserInitials(); 

                    int affectedRows = await connection.ExecuteAsync(sql, depot);
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
                        UPDATE Depot SET
                            DEPOTNAME = @DepotName,
                            QED_DATE = @QED_DATE,
                            QED_TIME = @QED_TIME,
                            QED_OP = @QED_OP
                        WHERE DEPOT = @DepotId AND QDEL_DATE IS NULL"; 

                    // Set audit fields for edit
                    depot.QED_DATE = DateTime.Today;
                    depot.QED_TIME = DateTime.Now.ToString("HH:mm:ss"); // Or appropriate format
                    depot.QED_OP = GetCurrentUserInitials();

                    int affectedRows = await connection.ExecuteAsync(sql, depot);
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
                            IsActive = 0,
                            DeletedAt = GETDATE(),
                            DeletedBy = @DeletedBy,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE DepotCode = @DepotId AND IsActive = 1"; 

                    var parameters = new 
                    {
                        DepotId = depotId,
                        DeletedBy = operatorInitials,
                        ModifiedBy = operatorInitials
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
