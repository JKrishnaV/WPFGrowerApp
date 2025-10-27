using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class AccountService : BaseDatabaseService, IAccountService
    {
        public async Task<List<Account>> GetAllAccountsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            NUMBER as Number,
                            DATE as Date,
                            TYPE as Type,
                            CLASS as Class,
                            PRODUCT as Product,
                            PROCESS as Process,
                            GRADE as Grade,
                            LBS as Lbs,
                            UNIT_PRICE as UnitPrice,
                            DOLLARS as Dollars,
                            DESCR as Description,
                            SERIES as Series,
                            CHEQUE as Cheque,
                            T_SERIES as TSeries,
                            T_CHEQUE as TCheque,
                            YEAR as Year,
                            ACCT_UNIQUE as AcctUnique,
                            CURRENCY as Currency,
                            CHG_GST as ChgGst,
                            GST_RATE as GstRate,
                            GST_EST as GstEst,
                            NON_GST_EST as NonGstEst,
                            ADV_NO as AdvNo,
                            ADV_BAT as AdvBat,
                            FIN_BAT as FinBat
                        FROM ACCOUNT 
                        ORDER BY DATE DESC";

                    return (await connection.QueryAsync<Account>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAllAccountsAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Account> GetAccountByNumberAsync(decimal number)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            NUMBER as Number,
                            DATE as Date,
                            TYPE as Type,
                            CLASS as Class,
                            PRODUCT as Product,
                            PROCESS as Process,
                            GRADE as Grade,
                            LBS as Lbs,
                            UNIT_PRICE as UnitPrice,
                            DOLLARS as Dollars,
                            DESCR as Description,
                            SERIES as Series,
                            CHEQUE as Cheque,
                            T_SERIES as TSeries,
                            T_CHEQUE as TCheque,
                            YEAR as Year,
                            ACCT_UNIQUE as AcctUnique,
                            CURRENCY as Currency,
                            CHG_GST as ChgGst,
                            GST_RATE as GstRate,
                            GST_EST as GstEst,
                            NON_GST_EST as NonGstEst,
                            ADV_NO as AdvNo,
                            ADV_BAT as AdvBat,
                            FIN_BAT as FinBat
                        FROM ACCOUNT 
                        WHERE NUMBER = @Number";

                    var parameters = new { Number = number };
                    return await connection.QueryFirstOrDefaultAsync<Account>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAccountByNumberAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> SaveAccountAsync(Account account)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        MERGE INTO ACCOUNT AS target
                        USING (SELECT @Number AS NUMBER) AS source
                        ON (target.NUMBER = source.NUMBER)
                        WHEN MATCHED THEN
                            UPDATE SET
                                DATE = @Date,
                                TYPE = @Type,
                                CLASS = @Class,
                                PRODUCT = @Product,
                                PROCESS = @Process,
                                GRADE = @Grade,
                                LBS = @Lbs,
                                UNIT_PRICE = @UnitPrice,
                                DOLLARS = @Dollars,
                                DESCR = @Description,
                                SERIES = @Series,
                                CHEQUE = @Cheque,
                                T_SERIES = @TSeries,
                                T_CHEQUE = @TCheque,
                                YEAR = @Year,
                                ACCT_UNIQUE = @AcctUnique,
                                CURRENCY = @Currency,
                                CHG_GST = @ChgGst,
                                GST_RATE = @GstRate,
                                GST_EST = @GstEst,
                                NON_GST_EST = @NonGstEst,
                                ADV_NO = @AdvNo,
                                ADV_BAT = @AdvBat,
                                FIN_BAT = @FinBat,
                                QED_DATE = GETDATE(),
                                QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                                QED_OP = @ModifiedBy
                        WHEN NOT MATCHED THEN
                            INSERT (
                                NUMBER, DATE, TYPE, CLASS, PRODUCT, PROCESS, GRADE,
                                LBS, UNIT_PRICE, DOLLARS, DESCR, SERIES, CHEQUE,
                                T_SERIES, T_CHEQUE, YEAR, ACCT_UNIQUE, CURRENCY,
                                CHG_GST, GST_RATE, GST_EST, NON_GST_EST, ADV_NO,
                                ADV_BAT, FIN_BAT, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @Number, @Date, @Type, @Class, @Product, @Process,
                                @Grade, @Lbs, @UnitPrice, @Dollars, @Description,
                                @Series, @Cheque, @TSeries, @TCheque, @Year,
                                @AcctUnique, @Currency, @ChgGst, @GstRate,
                                @GstEst, @NonGstEst, @AdvNo, @AdvBat, @FinBat,
                                GETDATE(), CONVERT(varchar(8), GETDATE(), 108),
                                @CreatedBy
                            );";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new
                    {
                        account.Number,
                        account.Date,
                        account.Type,
                        account.Class,
                        account.Product,
                        account.Process,
                        account.Grade,
                        account.Lbs,
                        account.UnitPrice,
                        account.Dollars,
                        account.Description,
                        account.Series,
                        account.Cheque,
                        account.TSeries,
                        account.TCheque,
                        account.Year,
                        account.AcctUnique,
                        account.Currency,
                        account.ChgGst,
                        account.GstRate,
                        account.GstEst,
                        account.NonGstEst,
                        account.AdvNo,
                        account.AdvBat,
                        account.FinBat,
                        ModifiedBy = currentUser,
                        CreatedBy = currentUser
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in SaveAccountAsync: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<List<Account>> GetAccountsByYearAsync(decimal year)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            NUMBER as Number,
                            DATE as Date,
                            TYPE as Type,
                            CLASS as Class,
                            PRODUCT as Product,
                            PROCESS as Process,
                            GRADE as Grade,
                            LBS as Lbs,
                            UNIT_PRICE as UnitPrice,
                            DOLLARS as Dollars,
                            DESCR as Description,
                            SERIES as Series,
                            CHEQUE as Cheque,
                            T_SERIES as TSeries,
                            T_CHEQUE as TCheque,
                            YEAR as Year,
                            ACCT_UNIQUE as AcctUnique,
                            CURRENCY as Currency,
                            CHG_GST as ChgGst,
                            GST_RATE as GstRate,
                            GST_EST as GstEst,
                            NON_GST_EST as NonGstEst,
                            ADV_NO as AdvNo,
                            ADV_BAT as AdvBat,
                            FIN_BAT as FinBat
                        FROM ACCOUNT 
                        WHERE YEAR = @Year
                        ORDER BY DATE DESC";

                    var parameters = new { Year = year };
                    return (await connection.QueryAsync<Account>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAccountsByYearAsync: {ex.Message}", ex);
                 throw;
            }
        }

        public async Task<bool> CreatePaymentAccountEntriesAsync(List<Account> paymentEntries)
        {
            if (paymentEntries == null || !paymentEntries.Any())
            {
                return true; // Nothing to insert
            }

            // TODO: Need to implement logic to get the next ACCT_UNIQ value.
            // This might involve querying the max value or using a sequence.
            // For now, assuming it's handled or needs to be set on the Account object before calling.
            // decimal nextAcctUniq = await GetNextAcctUniqAsync();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Use a transaction for bulk insert
                    using (var transaction = connection.BeginTransaction())
                    {
                        var sql = @"
                            INSERT INTO ACCOUNT (
                                NUMBER, DATE, TYPE, CLASS, PRODUCT, PROCESS, GRADE,
                                LBS, U_PRICE, DOLLARS, DESCR, SERIES, CHEQUE,
                                T_SER, T_CHEQ, YEAR, ACCT_UNIQ, CURRENCY,
                                CHG_GST, GST_RATE, GST_EST, NON_GST_EST, ADV_NO,
                                ADV_BAT, FIN_BAT, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @Number, @Date, @Type, @Class, @Product, @Process,
                                @Grade, @Lbs, @UnitPrice, @Dollars, @Description,
                                @Series, @Cheque, @TSeries, @TCheque, @Year,
                                @AcctUnique, @Currency, @ChgGst, @GstRate,
                                @GstEst, @NonGstEst, @AdvNo, @AdvBat, @FinBat,
                                GETDATE(), CONVERT(varchar(8), GETDATE(), 108),
                                @QaddOp
                            );";

                        // Assign common values like QaddOp
                        var qaddOp = App.CurrentUser?.Username ?? "SYSTEM";
                        foreach(var entry in paymentEntries)
                        {
                            // entry.AcctUnique = nextAcctUniq++; // Assign unique ID if needed
                            entry.QaddOp = qaddOp; // Set audit operator
                        }

                        int rowsAffected = await connection.ExecuteAsync(sql, paymentEntries, transaction: transaction);
                        transaction.Commit();
                        return rowsAffected == paymentEntries.Count;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in CreatePaymentAccountEntriesAsync: {ex.Message}", ex);
                return false; // Indicate failure
            }
        }

        public async Task<List<Account>> GetPayableAccountEntriesAsync(decimal growerNumber, string currency, int cropYear, DateTime cutoffDate, string chequeType)
        {
            // This query needs to replicate the logic from CHEQRUN.PRG for selecting payable entries
            // It selects entries that haven't been assigned a final cheque (SERIES/CHEQUE are null/empty)
            // and match the specified criteria.
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sqlBuilder = new SqlBuilder();
                    var selector = sqlBuilder.AddTemplate(@"
                        SELECT * -- Select specific columns if needed
                        FROM ACCOUNT a
                        /**where**/
                        ORDER BY a.DATE, a.ACCT_UNIQ"
                    );

                    // Mandatory conditions
                    sqlBuilder.Where("a.NUMBER = @GrowerNumber", new { GrowerNumber = growerNumber });
                    sqlBuilder.Where("a.CURRENCY = @Currency", new { Currency = currency });
                    sqlBuilder.Where("a.YEAR = @CropYear", new { CropYear = cropYear });
                    sqlBuilder.Where("a.DATE <= @CutoffDate", new { CutoffDate = cutoffDate });
                    sqlBuilder.Where("(a.SERIES IS NULL OR a.SERIES = '')"); // Not paid yet
                    sqlBuilder.Where("(a.CHEQUE IS NULL OR a.CHEQUE = 0)");   // Not paid yet

                    // Filter by cheque type (e.g., exclude Equity for Weekly runs)
                    if (chequeType == "W") // Assuming "W" for Weekly/Advance
                    {
                        // Match XBase++ logic: set filter to Account->type<>TT_EQUITY
                        // Need to know the actual value for TT_EQUITY (e.g., 'EQ')
                        sqlBuilder.Where("a.TYPE <> 'EQ'"); // Placeholder - replace 'EQ' with actual equity type code
                    }
                    else if (chequeType == "E") // Assuming "E" for Equity
                    {
                         // Match XBase++ logic: set filter to Account->type==TT_EQUITY
                         sqlBuilder.Where("a.TYPE = 'EQ'"); // Placeholder
                    }
                    // Add other cheque type filters if necessary

                    var results = await connection.QueryAsync<Account>(selector.RawSql, selector.Parameters);
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetPayableAccountEntriesAsync for Grower {growerNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateAccountEntriesWithChequeInfoAsync(decimal growerNumber, string currency, int cropYear, DateTime cutoffDate, string chequeType, string chequeSeries, decimal chequeNumber)
        {
            // This replicates the logic of updating Account records with the final cheque details
            // after temporary assignment (T_SER, T_CHEQ).
             try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sqlBuilder = new SqlBuilder();
                    var updater = sqlBuilder.AddTemplate(@"
                        UPDATE ACCOUNT
                        SET SERIES = @ChequeSeries,
                            CHEQUE = @ChequeNumber,
                            T_SER = NULL,        -- Clear temporary fields
                            T_CHEQ = NULL
                        /**where**/");

                    // Conditions to select the correct records (must match GetPayableAccountEntriesAsync logic + temp fields)
                    sqlBuilder.Where("NUMBER = @GrowerNumber", new { GrowerNumber = growerNumber });
                    sqlBuilder.Where("CURRENCY = @Currency", new { Currency = currency });
                    sqlBuilder.Where("YEAR = @CropYear", new { CropYear = cropYear });
                    sqlBuilder.Where("DATE <= @CutoffDate", new { CutoffDate = cutoffDate });
                    sqlBuilder.Where("(SERIES IS NULL OR SERIES = '')");
                    sqlBuilder.Where("(CHEQUE IS NULL OR CHEQUE = 0)");
                    sqlBuilder.Where("T_SER = @ChequeSeries"); // Match temporary assignment
                    sqlBuilder.Where("T_CHEQ = @ChequeNumber"); // Match temporary assignment

                    // Add cheque type filter again for safety
                     if (chequeType == "W")
                    {
                        sqlBuilder.Where("TYPE <> 'EQ'"); // Placeholder
                    }
                    else if (chequeType == "E")
                    {
                         sqlBuilder.Where("TYPE = 'EQ'"); // Placeholder
                    }

                    // Add parameters for the SET clause and WHERE clause temp fields
                    sqlBuilder.AddParameters(new { ChequeSeries = chequeSeries, ChequeNumber = chequeNumber });


                    int rowsAffected = await connection.ExecuteAsync(updater.RawSql, updater.Parameters);
                    // We might expect multiple rows to be updated if one cheque covers multiple account entries
                    return rowsAffected > 0; // Return true if at least one row was updated
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in UpdateAccountEntriesWithChequeInfoAsync for Grower {growerNumber}, Cheque {chequeSeries}-{chequeNumber}: {ex.Message}", ex);
                throw; // Or return false
            }
        }

         public async Task<bool> RevertTemporaryChequeInfoAsync(string currency, string tempChequeSeries, decimal tempChequeNumberStart)
        {
            // This clears the T_SER and T_CHEQ fields if a cheque run is cancelled.
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE ACCOUNT
                        SET T_SER = NULL,
                            T_CHEQ = NULL
                        WHERE CURRENCY = @Currency
                          AND T_SER = @TempChequeSeries
                          AND T_CHEQ >= @TempChequeNumberStart"; // Clear all temp cheques from this run onwards

                    int rowsAffected = await connection.ExecuteAsync(sql, new { Currency = currency, TempChequeSeries = tempChequeSeries, TempChequeNumberStart = tempChequeNumberStart });
                    // Log how many rows were reverted if needed
                    return true; // Assume success even if 0 rows affected (maybe no temp cheques were assigned)
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in RevertTemporaryChequeInfoAsync for Series {tempChequeSeries}: {ex.Message}", ex);
                throw; // Or return false
            }
        }

        // Placeholder for GetNextAcctUniqAsync - implementation depends on DB strategy
        // private async Task<decimal> GetNextAcctUniqAsync()
        // {
        //     using (var connection = new SqlConnection(_connectionString))
        //     {
        //         await connection.OpenAsync();
        //         var sql = "SELECT ISNULL(MAX(ACCT_UNIQ), 0) + 1 FROM ACCOUNT"; // Example: Max + 1
        //         return await connection.ExecuteScalarAsync<decimal>(sql);
        //     }
        // }

    }
}
