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
    public class AuditService : BaseDatabaseService, IAuditService
    {
        public async Task<List<Audit>> GetAllAuditsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            DAY_UNIQ as DayUniq,
                            ACCT_UNIQ as AcctUniq,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM AUDIT 
                        ORDER BY DAY_UNIQ DESC";

                    return (await connection.QueryAsync<Audit>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllAuditsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Audit> GetAuditByDayUniqAsync(decimal dayUniq)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            DAY_UNIQ as DayUniq,
                            ACCT_UNIQ as AcctUniq,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM AUDIT 
                        WHERE DAY_UNIQ = @DayUniq";

                    var parameters = new { DayUniq = dayUniq };
                    return await connection.QueryFirstOrDefaultAsync<Audit>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAuditByDayUniqAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Audit>> GetAuditsByAccountUniqAsync(decimal acctUniq)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            DAY_UNIQ as DayUniq,
                            ACCT_UNIQ as AcctUniq,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM AUDIT 
                        WHERE ACCT_UNIQ = @AcctUniq
                        ORDER BY DAY_UNIQ DESC";

                    var parameters = new { AcctUniq = acctUniq };
                    return (await connection.QueryAsync<Audit>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAuditsByAccountUniqAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SaveAuditAsync(Audit audit)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        MERGE INTO AUDIT AS target
                        USING (SELECT @DayUniq AS DAY_UNIQ) AS source
                        ON (target.DAY_UNIQ = source.DAY_UNIQ)
                        WHEN MATCHED THEN
                            UPDATE SET
                                ACCT_UNIQ = @AcctUniq,
                                QED_DATE = GETDATE(),
                                QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                                QED_OP = SYSTEM_USER
                        WHEN NOT MATCHED THEN
                            INSERT (
                                DAY_UNIQ, ACCT_UNIQ, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @DayUniq, @AcctUniq,
                                GETDATE(), CONVERT(varchar(8), GETDATE(), 108),
                                SYSTEM_USER
                            );";

                    var parameters = new
                    {
                        audit.DayUniq,
                        audit.AcctUniq
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveAuditAsync: {ex.Message}");
                return false;
            }
        }
    }
} 