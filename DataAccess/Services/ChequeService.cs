using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging; // Assuming Logger might be needed

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
                            NUMBER as GrowerNumber, -- Corrected column name based on schema
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQTYPE as ChequeType, -- Corrected column name based on schema
                            VOID as Void,
                            DATECLEAR as DateClear, -- Corrected column name based on schema
                            ISCLEARED as IsCleared, -- Corrected column name based on schema
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
                            NUMBER as GrowerNumber, -- Corrected column name
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQTYPE as ChequeType, -- Corrected column name
                            VOID as Void,
                            DATECLEAR as DateClear, -- Corrected column name
                            ISCLEARED as IsCleared, -- Corrected column name
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
                            NUMBER as GrowerNumber, -- Corrected column name
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQTYPE as ChequeType, -- Corrected column name
                            VOID as Void,
                            DATECLEAR as DateClear, -- Corrected column name
                            ISCLEARED as IsCleared, -- Corrected column name
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
                        WHERE NUMBER = @GrowerNumber -- Corrected column name
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
                            NUMBER as GrowerNumber, -- Corrected column name
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQTYPE as ChequeType, -- Corrected column name
                            VOID as Void,
                            DATECLEAR as DateClear, -- Corrected column name
                            ISCLEARED as IsCleared, -- Corrected column name
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
            // This method likely needs adjustment if used by payment run,
            // as CreateChequesAsync handles bulk inserts.
            // Keeping it for potential manual cheque edits/saves.
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
                                NUMBER = @GrowerNumber, -- Corrected column name
                                DATE = @Date,
                                AMOUNT = @Amount,
                                YEAR = @Year,
                                CHEQTYPE = @ChequeType, -- Corrected column name
                                VOID = @Void,
                                DATECLEAR = @DateClear, -- Corrected column name
                                ISCLEARED = @IsCleared, -- Corrected column name
                                CURRENCY = @Currency,
                                QED_DATE = GETDATE(),
                                QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                                QED_OP = @QedOp -- Use parameter
                        WHEN NOT MATCHED THEN
                            INSERT (
                                SERIES, CHEQUE, NUMBER, DATE, AMOUNT, YEAR,
                                CHEQTYPE, VOID, DATECLEAR, ISCLEARED,
                                CURRENCY, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @Series, @ChequeNumber, @GrowerNumber, @Date,
                                @Amount, @Year, @ChequeType, @Void, @DateClear,
                                @IsCleared, @Currency, GETDATE(),
                                CONVERT(varchar(8), GETDATE(), 108), @QaddOp -- Use parameter
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
                        cheque.Currency,
                        QaddOp = App.CurrentUser?.Username ?? "SYSTEM", // Set audit op
                        QedOp = App.CurrentUser?.Username ?? "SYSTEM"  // Set audit op
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
                            QED_OP = @QedOp -- Use parameter
                        WHERE SERIES = @Series AND CHEQUE = @ChequeNumber";

                    var parameters = new {
                        Series = series,
                        ChequeNumber = chequeNumber,
                        QedOp = App.CurrentUser?.Username ?? "SYSTEM" // Set audit op
                    };
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

        // --- New Methods Implementation ---

        public async Task<decimal> GetNextChequeNumberAsync(string series, bool isEft = false)
        {
            // Logic mirrors CHEQRUN.PRG initialization
            // It finds the last used cheque number for the series (excluding negative/placeholder numbers)
            // and adds 1.
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Find the highest cheque number >= 0 for the given series
                    var sql = @"
                        SELECT ISNULL(MAX(CHEQUE), 0)
                        FROM CHEQUE
                        WHERE SERIES = @Series AND CHEQUE >= 0";

                    var lastCheque = await connection.ExecuteScalarAsync<decimal>(sql, new { Series = series });
                    return lastCheque + 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting next cheque number for series {series}: {ex.Message}");
                throw; // Rethrow as this is critical
            }
        }

        public async Task<bool> CreateChequesAsync(List<Cheque> chequesToCreate)
        {
            // This method performs a bulk insert of cheque records.
            // It assumes the Cheque objects in the list have all necessary properties set.
            if (chequesToCreate == null || !chequesToCreate.Any())
            {
                return true; // Nothing to insert
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        var sql = @"
                            INSERT INTO CHEQUE (
                                SERIES, CHEQUE, NUMBER, DATE, AMOUNT, YEAR,
                                CHEQTYPE, VOID, DATECLEAR, ISCLEARED,
                                CURRENCY, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @Series, @ChequeNumber, @GrowerNumber, @Date,
                                @Amount, @Year, @ChequeType, @Void, @DateClear,
                                @IsCleared, @Currency, GETDATE(),
                                CONVERT(varchar(8), GETDATE(), 108), @QaddOp
                            );";

                        // Set audit operator and defaults for all cheques
                         var qaddOp = App.CurrentUser?.Username ?? "SYSTEM";
                         foreach(var cheque in chequesToCreate)
                         {
                             cheque.QaddOp = qaddOp;
                             // Ensure default values are set if needed (e.g., Void=false, IsCleared=false)
                             cheque.Void = cheque.Void ?? false;
                             cheque.IsCleared = cheque.IsCleared ?? false;
                         }


                        int rowsAffected = await connection.ExecuteAsync(sql, chequesToCreate, transaction: transaction);
                        transaction.Commit();
                        return rowsAffected == chequesToCreate.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateChequesAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Cheque>> GetTemporaryChequesAsync(string currency, string tempChequeSeries, decimal tempChequeNumberStart)
        {
            // This method retrieves records from the Cheque table that were created
            // during the current run (identified by the temporary series and starting number).
            // This is used for printing registers/cheques before finalization.
             try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Select cheques matching the series and starting from the first number used in this run.
                    var sql = @"
                        SELECT
                            SERIES as Series,
                            CHEQUE as ChequeNumber,
                            NUMBER as GrowerNumber, -- Corrected column name
                            DATE as Date,
                            AMOUNT as Amount,
                            YEAR as Year,
                            CHEQTYPE as ChequeType, -- Corrected column name
                            VOID as Void,
                            DATECLEAR as DateClear, -- Corrected column name
                            ISCLEARED as IsCleared, -- Corrected column name
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
                        WHERE SERIES = @Series
                          AND CHEQUE >= @ChequeNumberStart
                          AND CURRENCY = @Currency -- Added currency filter
                        ORDER BY CHEQUE"; // Order by cheque number

                    var parameters = new { Series = tempChequeSeries, ChequeNumberStart = tempChequeNumberStart, Currency = currency };
                    var results = await connection.QueryAsync<Cheque>(sql, parameters);
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetTemporaryChequesAsync for Series {tempChequeSeries}: {ex.Message}");
                throw;
            }
        }
    }
}
