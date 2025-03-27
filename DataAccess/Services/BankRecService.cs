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
    public class BankRecService : BaseDatabaseService, IBankRecService
    {
        public async Task<List<BankRec>> GetAllBankRecsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            ACCT_DATE as AcctDate,
                            DATE_DONE as DateDone,
                            NOTE as Note,
                            AMOUNT as Amount,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM BANK_REC 
                        ORDER BY ACCT_DATE DESC";

                    return (await connection.QueryAsync<BankRec>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllBankRecsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<BankRec> GetBankRecByDateAsync(DateTime acctDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            ACCT_DATE as AcctDate,
                            DATE_DONE as DateDone,
                            NOTE as Note,
                            AMOUNT as Amount,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM BANK_REC 
                        WHERE ACCT_DATE = @AcctDate";

                    var parameters = new { AcctDate = acctDate };
                    return await connection.QueryFirstOrDefaultAsync<BankRec>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetBankRecByDateAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<BankRec>> GetBankRecsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            ACCT_DATE as AcctDate,
                            DATE_DONE as DateDone,
                            NOTE as Note,
                            AMOUNT as Amount,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM BANK_REC 
                        WHERE ACCT_DATE BETWEEN @StartDate AND @EndDate
                        ORDER BY ACCT_DATE DESC";

                    var parameters = new { StartDate = startDate, EndDate = endDate };
                    return (await connection.QueryAsync<BankRec>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetBankRecsByDateRangeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SaveBankRecAsync(BankRec bankRec)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        MERGE INTO BANK_REC AS target
                        USING (SELECT @AcctDate AS ACCT_DATE) AS source
                        ON (target.ACCT_DATE = source.ACCT_DATE)
                        WHEN MATCHED THEN
                            UPDATE SET
                                DATE_DONE = @DateDone,
                                NOTE = @Note,
                                AMOUNT = @Amount,
                                QED_DATE = GETDATE(),
                                QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                                QED_OP = SYSTEM_USER
                        WHEN NOT MATCHED THEN
                            INSERT (
                                ACCT_DATE, DATE_DONE, NOTE, AMOUNT,
                                QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @AcctDate, @DateDone, @Note, @Amount,
                                GETDATE(), CONVERT(varchar(8), GETDATE(), 108),
                                SYSTEM_USER
                            );";

                    var parameters = new
                    {
                        bankRec.AcctDate,
                        bankRec.DateDone,
                        bankRec.Note,
                        bankRec.Amount
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveBankRecAsync: {ex.Message}");
                return false;
            }
        }
    }
} 