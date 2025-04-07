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
                            PROCESS as ProcessId,
                            Description,
                            DEF_GRADE as DefGrade,
                            PROC_CLASS as ProcClass,
                            QADD_DATE, QADD_TIME, QADD_OP,
                            QED_DATE, QED_TIME, QED_OP,
                            QDEL_DATE, QDEL_TIME, QDEL_OP
                        FROM Process
                        WHERE QDEL_DATE IS NULL 
                        ORDER BY Description"; // Order alphabetically for display
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
                            PROCESS as ProcessId,
                            Description,
                            DEF_GRADE as DefGrade,
                            PROC_CLASS as ProcClass,
                            QADD_DATE, QADD_TIME, QADD_OP,
                            QED_DATE, QED_TIME, QED_OP,
                            QDEL_DATE, QDEL_TIME, QDEL_OP
                        FROM Process
                        WHERE PROCESS = @ProcessId AND QDEL_DATE IS NULL";
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
                        INSERT INTO Process (
                            PROCESS, Description, DEF_GRADE, PROC_CLASS,
                            QADD_DATE, QADD_TIME, QADD_OP, QED_DATE, QED_TIME, QED_OP, QDEL_DATE, QDEL_TIME, QDEL_OP
                        ) VALUES (
                            @ProcessId, @Description, @DefGrade, @ProcClass,
                            @QADD_DATE, @QADD_TIME, @QADD_OP, NULL, NULL, NULL, NULL, NULL, NULL
                        )";

                    // Set audit fields for add
                    process.QADD_DATE = DateTime.Today;
                    process.QADD_TIME = DateTime.Now.ToString("HH:mm:ss"); // Or appropriate format
                    process.QADD_OP = GetCurrentUserInitials(); 

                    int affectedRows = await connection.ExecuteAsync(sql, process);
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
                        UPDATE Process SET
                            Description = @Description,
                            DEF_GRADE = @DefGrade,
                            PROC_CLASS = @ProcClass,
                            QED_DATE = @QED_DATE,
                            QED_TIME = @QED_TIME,
                            QED_OP = @QED_OP
                        WHERE PROCESS = @ProcessId AND QDEL_DATE IS NULL"; 

                    // Set audit fields for edit
                    process.QED_DATE = DateTime.Today;
                    process.QED_TIME = DateTime.Now.ToString("HH:mm:ss"); // Or appropriate format
                    process.QED_OP = GetCurrentUserInitials();

                    int affectedRows = await connection.ExecuteAsync(sql, process);
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
                        UPDATE Process SET
                            QDEL_DATE = @QDEL_DATE,
                            QDEL_TIME = @QDEL_TIME,
                            QDEL_OP = @QDEL_OP
                        WHERE PROCESS = @ProcessId AND QDEL_DATE IS NULL"; 

                    var parameters = new 
                    {
                        ProcessId = processId,
                        QDEL_DATE = DateTime.Today,
                        QDEL_TIME = DateTime.Now.ToString("HH:mm:ss"), // Or appropriate format
                        QDEL_OP = operatorInitials
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
