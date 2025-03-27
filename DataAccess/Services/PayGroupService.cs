using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    public class PayGroupService : BaseDatabaseService, IPayGroupService
    {
        public async Task<List<PayGroup>> GetPayGroupsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PAY_GROUP_ID as PayGroupId,
                            DESCRIPTION as Description,
                            DEFAULT_PRICE_LEVEL as DefaultPriceLevel
                        FROM PAY_GROUP 
                        ORDER BY PAY_GROUP_ID";

                    return (await connection.QueryAsync<PayGroup>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetPayGroupsAsync: {ex.Message}");
                throw;
            }
        }
    }
} 