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
    public class ProcessService : BaseDatabaseService, IProcessService
    {
        // Helper to get current user initials (replace with actual implementation if available)
        private string GetCurrentUserInitials() => App.CurrentUser?.Username ?? "SYSTEM"; 

        public async Task<IEnumerable<Process>> GetAllProcessesAsync() // Changed return type
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
                            ProcessId,
                            ProcessName as Description,
                            0 as DefGrade,
                            0 as ProcClass
                        FROM Processes
                        WHERE IsActive = 1
                        ORDER BY ProcessName"; // Order alphabetically
                    return await connection.QueryAsync<Process>(sql); // Return IEnumerable
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all processes: {ex.Message}", ex);
                throw; // Rethrow to allow higher layers to handle
            }
        }

        public async Task<Process> GetProcessByIdAsync(string processId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            ProcessCode as ProcessId,
                            ProcessName as Description,
                            0 as DefGrade,
                            '' as ProcClass
                        FROM Processes
                        WHERE ProcessCode = @ProcessId AND IsActive = 1";
                    return await connection.QueryFirstOrDefaultAsync<Process>(sql, new { ProcessId = processId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting process by ID '{processId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> AddProcessAsync(Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO Processes (
                            ProcessCode, ProcessName, Description, IsActive, DisplayOrder,
                            CreatedAt, CreatedBy
                        ) VALUES (
                            @ProcessId, @Description, @Description, 1, 0,
                            GETDATE(), @OperatorInitials
                        )";

                    var parameters = new
                    {
                        ProcessId = process.ProcessId,
                        Description = process.Description,
                        OperatorInitials = GetCurrentUserInitials()
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding process '{process.ProcessId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateProcessAsync(Process process)
        {
             if (process == null) throw new ArgumentNullException(nameof(process));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Processes SET
                            ProcessName = @Description,
                            Description = @Description,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @OperatorInitials
                        WHERE ProcessCode = @ProcessId AND IsActive = 1";

                    var parameters = new
                    {
                        ProcessId = process.ProcessId,
                        Description = process.Description,
                        OperatorInitials = GetCurrentUserInitials()
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating process '{process.ProcessId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteProcessAsync(string processId, string operatorInitials)
        {
            if (string.IsNullOrWhiteSpace(processId)) throw new ArgumentException("Process ID cannot be empty.", nameof(processId));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials(); // Fallback

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Processes SET
                            DeletedAt = GETDATE(),
                            DeletedBy = @OperatorInitials,
                            IsActive = 0
                        WHERE ProcessCode = @ProcessId AND IsActive = 1"; 

                    int affectedRows = await connection.ExecuteAsync(sql, new { ProcessId = processId, OperatorInitials = operatorInitials });
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting process '{processId}': {ex.Message}", ex);
                throw;
            }
        }
    }
}
