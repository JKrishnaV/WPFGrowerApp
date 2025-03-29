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
    public class ChequeService : BaseDatabaseService, IChequeService
    {
        public async Task<List<Cheque>> GetAllChequesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            SERIES as Series,
                            CHEQUE as ChequeNumber,
                            GROWER as GrowerNumber,
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQUE_TYPE as ChequeType,
                            VOID as Void,
                            DATE_CLEAR as DateClear,
                            IS_CLEARED as IsCleared,
                            CURRENCY as Currency,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM CHEQUE 
                        ORDER BY DATE DESC";

                    return (await connection.QueryAsync<Cheque>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllChequesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Cheque> GetChequeBySeriesAndNumberAsync(string series, decimal chequeNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            SERIES as Series,
                            CHEQUE as ChequeNumber,
                            GROWER as GrowerNumber,
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQUE_TYPE as ChequeType,
                            VOID as Void,
                            DATE_CLEAR as DateClear,
                            IS_CLEARED as IsCleared,
                            CURRENCY as Currency,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM CHEQUE 
                        WHERE SERIES = @Series AND CHEQUE = @ChequeNumber";

                    var parameters = new { Series = series, ChequeNumber = chequeNumber };
                    return await connection.QueryFirstOrDefaultAsync<Cheque>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetChequeBySeriesAndNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Cheque>> GetChequesByGrowerNumberAsync(decimal growerNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            SERIES as Series,
                            CHEQUE as ChequeNumber,
                            GROWER as GrowerNumber,
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQUE_TYPE as ChequeType,
                            VOID as Void,
                            DATE_CLEAR as DateClear,
                            IS_CLEARED as IsCleared,
                            CURRENCY as Currency,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM CHEQUE 
                        WHERE GROWER = @GrowerNumber
                        ORDER BY DATE DESC";

                    var parameters = new { GrowerNumber = growerNumber };
                    return (await connection.QueryAsync<Cheque>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetChequesByGrowerNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Cheque>> GetChequesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            SERIES as Series,
                            CHEQUE as ChequeNumber,
                            GROWER as GrowerNumber,
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQUE_TYPE as ChequeType,
                            VOID as Void,
                            DATE_CLEAR as DateClear,
                            IS_CLEARED as IsCleared,
                            CURRENCY as Currency,
                            QADD_DATE as QaddDate,
                            QADD_TIME as QaddTime,
                            QADD_OP as QaddOp,
                            QED_DATE as QedDate,
                            QED_TIME as QedTime,
                            QED_OP as QedOp,
                            QDEL_DATE as QdelDate,
                            QDEL_TIME as QdelTime,
                            QDEL_OP as QdelOp
                        FROM CHEQUE 
                        WHERE DATE BETWEEN @StartDate AND @EndDate
                        ORDER BY DATE DESC";

                    var parameters = new { StartDate = startDate, EndDate = endDate };
                    return (await connection.QueryAsync<Cheque>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetChequesByDateRangeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SaveChequeAsync(Cheque cheque)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        MERGE INTO CHEQUE AS target
                        USING (SELECT @Series AS SERIES, @ChequeNumber AS CHEQUE) AS source
                        ON (target.SERIES = source.SERIES AND target.CHEQUE = source.CHEQUE)
                        WHEN MATCHED THEN
                            UPDATE SET
                                GROWER = @GrowerNumber,
                                DATE = @Date,
                                AMOUNT = @Amount,
                                YEAR = @Year,
                                CHEQUE_TYPE = @ChequeType,
                                VOID = @Void,
                                DATE_CLEAR = @DateClear,
                                IS_CLEARED = @IsCleared,
                                CURRENCY = @Currency,
                                QED_DATE = GETDATE(),
                                QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                                QED_OP = SYSTEM_USER
                        WHEN NOT MATCHED THEN
                            INSERT (
                                SERIES, CHEQUE, GROWER, DATE, AMOUNT, YEAR,
                                CHEQUE_TYPE, VOID, DATE_CLEAR, IS_CLEARED,
                                CURRENCY, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @Series, @ChequeNumber, @GrowerNumber, @Date,
                                @Amount, @Year, @ChequeType, @Void, @DateClear,
                                @IsCleared, @Currency, GETDATE(),
                                CONVERT(varchar(8), GETDATE(), 108), SYSTEM_USER
                            );";

                    var parameters = new
                    {
                        cheque.Series,
                        cheque.ChequeNumber,
                        cheque.GrowerNumber,
                        cheque.Date,
                        cheque.Amount,
                        cheque.Year,
                        cheque.ChequeType,
                        cheque.Void,
                        cheque.DateClear,
                        cheque.IsCleared,
                        cheque.Currency
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveChequeAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> VoidChequeAsync(string series, decimal chequeNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE CHEQUE 
                        SET 
                            VOID = 1,
                            QED_DATE = GETDATE(),
                            QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                            QED_OP = SYSTEM_USER
                        WHERE SERIES = @Series AND CHEQUE = @ChequeNumber";

                    var parameters = new { Series = series, ChequeNumber = chequeNumber };
                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VoidChequeAsync: {ex.Message}");
                return false;
            }
        }
    }
} 