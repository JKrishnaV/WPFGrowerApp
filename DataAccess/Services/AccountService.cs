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
                Debug.WriteLine($"Error in GetAllAccountsAsync: {ex.Message}");
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
                Debug.WriteLine($"Error in GetAccountByNumberAsync: {ex.Message}");
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
                                QED_OP = SYSTEM_USER
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
                                SYSTEM_USER
                            );";

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
                        account.FinBat
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveAccountAsync: {ex.Message}");
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
                Debug.WriteLine($"Error in GetAccountsByYearAsync: {ex.Message}");
                throw;
            }
        }
    }
} 