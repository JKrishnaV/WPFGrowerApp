using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    public class PayGroupService : BaseDatabaseService, IPayGroupService
    {
        public async Task<List<PayGroup>> GetPayGroupsAsync()
        {
            var payGroups = new List<PayGroup>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"SELECT PAYGRP, Description, DEF_PRLVL FROM PayGrp ORDER BY PAYGRP";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                payGroups.Add(new PayGroup
                                {
                                    PayGroupId = !reader.IsDBNull(0) ? reader.GetString(0) : "",
                                    Description = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                                    DefaultPriceLevel = !reader.IsDBNull(2) ? reader.GetDecimal(2) : 1m
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting pay groups: {ex.Message}");
            }

            return payGroups;
        }
    }
} 