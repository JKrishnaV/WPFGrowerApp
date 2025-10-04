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
                            ProcessCode as ProcessId,
                            ProcessName as Description,
                            DefaultGrade,
                            ProcessClass,
                            GradeName1,
                            GradeName2,
                            GradeName3,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM Processes
                        WHERE IsActive = 1
                        ORDER BY ProcessName"; // Order alphabetically for display
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
                            DefaultGrade,
                            ProcessClass,
                            GradeName1,
                            GradeName2,
                            GradeName3,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
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
                var currentUser = GetCurrentUserInitials();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO Processes (
                            ProcessCode, ProcessName, Description, 
                            DefaultGrade, ProcessClass, 
                            GradeName1, GradeName2, GradeName3,
                            IsActive, DisplayOrder,
                            CreatedAt, CreatedBy
                        ) VALUES (
                            @ProcessId, @Description, @Description, 
                            @DefaultGrade, @ProcessClass,
                            @GradeName1, @GradeName2, @GradeName3,
                            1, 0,
                            GETDATE(), @CreatedBy
                        )";

                    var parameters = new
                    {
                        ProcessId = process.ProcessId,
                        Description = process.Description,
                        DefaultGrade = process.DefaultGrade,
                        ProcessClass = process.ProcessClass,
                        GradeName1 = process.GradeName1,
                        GradeName2 = process.GradeName2,
                        GradeName3 = process.GradeName3,
                        CreatedBy = currentUser
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
                var currentUser = GetCurrentUserInitials();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Processes SET
                            ProcessName = @Description,
                            Description = @Description,
                            DefaultGrade = @DefaultGrade,
                            ProcessClass = @ProcessClass,
                            GradeName1 = @GradeName1,
                            GradeName2 = @GradeName2,
                            GradeName3 = @GradeName3,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ProcessCode = @ProcessId AND IsActive = 1";

                    var parameters = new
                    {
                        ProcessId = process.ProcessId,
                        Description = process.Description,
                        DefaultGrade = process.DefaultGrade,
                        ProcessClass = process.ProcessClass,
                        GradeName1 = process.GradeName1,
                        GradeName2 = process.GradeName2,
                        GradeName3 = process.GradeName3,
                        ModifiedBy = currentUser
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
                            IsActive = 0,
                            DeletedAt = GETDATE(),
                            DeletedBy = @DeletedBy,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ProcessCode = @ProcessId AND IsActive = 1"; 

                    var parameters = new
                    {
                        ProcessId = processId,
                        DeletedBy = operatorInitials,
                        ModifiedBy = operatorInitials
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
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
