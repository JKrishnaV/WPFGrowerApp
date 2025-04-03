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
        public async Task<List<Depot>> GetAllDepotsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Map database columns to model properties
                    var sql = "SELECT DEPOT as DepotId, DEPOTNAME as DepotName FROM Depot ORDER BY DEPOT";
                    var depots = await connection.QueryAsync<Depot>(sql);
                    return depots.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all depots: {ex.Message}", ex);
                throw; // Rethrow or return empty list depending on desired error handling
            }
        }
    }
}
