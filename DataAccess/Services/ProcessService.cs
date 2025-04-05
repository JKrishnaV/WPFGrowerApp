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
        public async Task<List<Process>> GetAllProcessesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Map database columns to model properties
                    var sql = @"
                        SELECT
                            PROCESS as ProcessId,
                            Description,
                            DEF_GRADE as DefGrade,
                            PROC_CLASS as ProcClass
                        FROM Process
                        ORDER BY Description"; // Order alphabetically for display
                    var processes = await connection.QueryAsync<Process>(sql);
                    return processes.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all processes: {ex.Message}", ex);
                throw; // Rethrow or return empty list
            }
        }
    }
}
