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
                    
                    // First try to get from modern Processes table
                    var modernSql = @"
                        SELECT
                            ProcessId,
                            ProcessCode,
                            ProcessName as Description,
                            ISNULL(DefaultGrade, 0) as DefGrade,
                            ISNULL(ProcessClass, 0) as ProcClass
                        FROM Processes
                        WHERE IsActive = 1
                        ORDER BY ProcessName";
                    
                    var modernProcesses = await connection.QueryAsync<Process>(modernSql);
                    
                    // If we got results, return them
                    if (modernProcesses.Any())
                    {
                        Logger.Info($"Found {modernProcesses.Count()} processes from modern Processes table");
                        foreach (var process in modernProcesses)
                        {
                            Logger.Info($"Process: ID={process.ProcessId}, Code={process.ProcessCode}, Name={process.Description}");
                        }
                        return modernProcesses;
                    }
                    else
                    {
                        Logger.Warn("No processes found in modern Processes table");
                    }
                    
                    // If no results from modern table, try legacy Process table
                    var legacySql = @"
                        SELECT
                            ROW_NUMBER() OVER (ORDER BY PROCESS) as ProcessId,
                            PROCESS as ProcessCode,
                            Description,
                            ISNULL(DEF_GRADE, 0) as DefGrade,
                            ISNULL(PROC_CLASS, 0) as ProcClass
                        FROM Process
                        WHERE QDEL_DATE IS NULL
                        ORDER BY PROCESS";
                    
                    var legacyProcesses = await connection.QueryAsync<Process>(legacySql);
                    
                    if (legacyProcesses.Any())
                    {
                        Logger.Info($"Using legacy Process table - found {legacyProcesses.Count()} processes");
                        foreach (var process in legacyProcesses)
                        {
                            Logger.Info($"Legacy Process: ID={process.ProcessId}, Code={process.ProcessCode}, Name={process.Description}");
                        }
                        return legacyProcesses;
                    }
                    else
                    {
                        Logger.Warn("No processes found in legacy Process table either");
                    }
                    
                    // If still no results, return empty collection
                    Logger.Warn("No processes found in either modern Processes table or legacy Process table");
                    return new List<Process>();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all processes: {ex.Message}", ex);
                throw; // Rethrow to allow higher layers to handle
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
                            ProcessName as Description,
                            ISNULL(DefaultGrade, 0) as DefGrade,
                            ISNULL(ProcessClass, 0) as ProcClass
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
                            ProcessName as Description,
                            ISNULL(DefaultGrade, 0) as DefGrade,
                            ISNULL(ProcessClass, 0) as ProcClass
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
                            CreatedAt, CreatedBy, DefaultGrade, ProcessClass
                        ) VALUES (
                            @ProcessCode, @Description, @Description, 1, 0,
                            GETDATE(), @OperatorInitials, @DefGrade, @ProcClass
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var parameters = new
                    {
                        process.ProcessCode,
                        process.Description,
                        process.DefGrade,
                        process.ProcClass,
                        OperatorInitials = GetCurrentUserInitials()
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
                            ProcessName = @Description,
                            Description = @Description,
                            DefaultGrade = @DefGrade,
                            ProcessClass = @ProcClass,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @OperatorInitials
                        WHERE ProcessId = @ProcessId AND IsActive = 1";

                    int affectedRows = await connection.ExecuteAsync(sql, new { process.Description, process.DefGrade, process.ProcClass, process.ProcessId, OperatorInitials = GetCurrentUserInitials() });
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
                            DeletedAt = GETDATE(),
                            DeletedBy = @OperatorInitials,
                            IsActive = 0
                        WHERE ProcessId = @ProcessId AND IsActive = 1"; 

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
                            DeletedAt = GETDATE(),
                            DeletedBy = @OperatorInitials,
                            IsActive = 0
                        WHERE ProcessCode = @ProcessCode AND IsActive = 1"; 

                    int affectedRows = await connection.ExecuteAsync(sql, new { ProcessCode = processCode, OperatorInitials = operatorInitials });
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
