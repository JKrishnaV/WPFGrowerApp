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

        public async Task<IEnumerable<Process>> GetAllProcessesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT
                            ProcessId,
                            ProcessCode,
                            ProcessName,
                            Description,
                            IsActive,
                            DisplayOrder,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy,
                            DefaultGrade,
                            ProcessClass,
                            GradeName1,
                            GradeName2,
                            GradeName3
                        FROM Processes
                        WHERE DeletedAt IS NULL
                        ORDER BY ISNULL(DisplayOrder, ProcessId), ProcessName";
                    
                    var processes = await connection.QueryAsync<Process>(sql);
                    
                    Logger.Info($"Found {processes.Count()} processes from Processes table");
                    foreach (var process in processes)
                    {
                        Logger.Info($"Process: ID={process.ProcessId}, Code={process.ProcessCode}, Name={process.ProcessName}");
                    }
                    
                    return processes;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all processes: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Process?> GetProcessByIdAsync(int processId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            ProcessId,
                            ProcessCode,
                            ProcessName,
                            Description,
                            IsActive,
                            DisplayOrder,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy,
                            DefaultGrade,
                            ProcessClass,
                            GradeName1,
                            GradeName2,
                            GradeName3
                        FROM Processes
                        WHERE ProcessId = @ProcessId AND IsActive = 1";
                    return await connection.QueryFirstOrDefaultAsync<Process>(sql, new { ProcessId = processId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting process by ID '{processId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Process?> GetProcessByCodeAsync(string processCode)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT
                            ProcessId,
                            ProcessCode,
                            ProcessName,
                            Description,
                            IsActive,
                            DisplayOrder,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy,
                            DefaultGrade,
                            ProcessClass,
                            GradeName1,
                            GradeName2,
                            GradeName3
                        FROM Processes
                        WHERE ProcessCode = @ProcessCode AND IsActive = 1";
                    return await connection.QueryFirstOrDefaultAsync<Process>(sql, new { ProcessCode = processCode });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting process by code '{processCode}': {ex.Message}", ex);
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
                            CreatedAt, CreatedBy, DefaultGrade, ProcessClass, GradeName1, GradeName2, GradeName3
                        ) VALUES (
                            @ProcessCode, @ProcessName, @Description, @IsActive, @DisplayOrder,
                            @CreatedAt, @CreatedBy, @DefaultGrade, @ProcessClass, @GradeName1, @GradeName2, @GradeName3
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var parameters = new
                    {
                        process.ProcessCode,
                        ProcessName = process.ProcessName,
                        Description = process.Description,
                        IsActive = process.IsActive,
                        DisplayOrder = process.DisplayOrder,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = GetCurrentUserInitials(),
                        process.DefaultGrade,
                        process.ProcessClass,
                        process.GradeName1,
                        process.GradeName2,
                        process.GradeName3
                    };

                    int newId = await connection.ExecuteScalarAsync<int>(sql, parameters);
                    process.ProcessId = newId;
                    return newId > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error adding process '{process.ProcessCode}': {ex.Message}", ex);
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
                            ProcessCode = @ProcessCode,
                            ProcessName = @ProcessName,
                            Description = @Description,
                            IsActive = @IsActive,
                            DisplayOrder = @DisplayOrder,
                            DefaultGrade = @DefaultGrade,
                            ProcessClass = @ProcessClass,
                            GradeName1 = @GradeName1,
                            GradeName2 = @GradeName2,
                            GradeName3 = @GradeName3,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE ProcessId = @ProcessId";

                    var parameters = new
                    {
                        process.ProcessId,
                        process.ProcessCode,
                        ProcessName = process.ProcessName,
                        Description = process.Description,
                        process.IsActive,
                        process.DisplayOrder,
                        process.DefaultGrade,
                        process.ProcessClass,
                        process.GradeName1,
                        process.GradeName2,
                        process.GradeName3,
                        ModifiedAt = DateTime.UtcNow,
                        ModifiedBy = GetCurrentUserInitials()
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    
                    if (affectedRows == 0)
                    {
                        Logger.Warn($"UpdateProcessAsync: No rows were affected when updating ProcessId {process.ProcessId}. Process may not exist or may have been deleted.");
                    }
                    else
                    {
                        Logger.Info($"UpdateProcessAsync: Successfully updated ProcessId {process.ProcessId}. Rows affected: {affectedRows}");
                    }
                    
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating process '{process.ProcessId}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteProcessAsync(int processId, string operatorInitials)
        {
            if (processId <= 0) throw new ArgumentException("Process ID must be positive.", nameof(processId));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Processes SET
                            DeletedAt = @DeletedAt,
                            DeletedBy = @DeletedBy,
                            IsActive = 0
                        WHERE ProcessId = @ProcessId AND IsActive = 1"; 

                    var parameters = new
                    {
                        ProcessId = processId,
                        DeletedAt = DateTime.UtcNow,
                        DeletedBy = operatorInitials
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

        public async Task<bool> DeleteProcessByCodeAsync(string processCode, string operatorInitials)
        {
            if (string.IsNullOrWhiteSpace(processCode)) throw new ArgumentException("Process code cannot be empty.", nameof(processCode));
            if (string.IsNullOrWhiteSpace(operatorInitials)) operatorInitials = GetCurrentUserInitials();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Processes SET
                            DeletedAt = @DeletedAt,
                            DeletedBy = @DeletedBy,
                            IsActive = 0
                        WHERE ProcessCode = @ProcessCode AND IsActive = 1"; 

                    var parameters = new
                    {
                        ProcessCode = processCode,
                        DeletedAt = DateTime.UtcNow,
                        DeletedBy = operatorInitials
                    };

                    int affectedRows = await connection.ExecuteAsync(sql, parameters);
                    return affectedRows > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting process by code '{processCode}': {ex.Message}", ex);
                throw;
            }
        }
    }
}
