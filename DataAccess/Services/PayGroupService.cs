using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
                            PAYGRP as PayGroupId,
                            Description as Description,
                            DEF_PRLVL as DefaultPriceLevel,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM PayGrp 
                        ORDER BY PAYGRP";

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