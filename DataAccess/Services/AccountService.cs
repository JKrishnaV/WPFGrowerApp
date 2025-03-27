using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    public class AccountService : BaseDatabaseService, IAccountService
    {
        public async Task<List<Account>> GetAllAccountsAsync()
        {
            var accounts = new List<Account>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"SELECT * FROM Account ORDER BY DATE DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                accounts.Add(MapAccountFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all accounts: {ex.Message}");
            }

            return accounts;
        }

        public async Task<Account> GetAccountByNumberAsync(decimal number)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM Account WHERE NUMBER = @Number";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Number", number);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapAccountFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting account: {ex.Message}");
            }

            return null;
        }

        public async Task<List<Account>> GetAccountsByYearAsync(decimal year)
        {
            var accounts = new List<Account>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM Account WHERE YEAR = @Year ORDER BY DATE DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Year", year);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                accounts.Add(MapAccountFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting accounts by year: {ex.Message}");
            }

            return accounts;
        }

        public async Task<bool> SaveAccountAsync(Account account)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
                        MERGE INTO Account AS target
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
                                U_PRICE = @UnitPrice,
                                DOLLARS = @Dollars,
                                Description = @Description,
                                SERIES = @Series,
                                CHEQUE = @Cheque,
                                T_SER = @TSeries,
                                T_CHEQ = @TCheque,
                                YEAR = @Year,
                                ACCT_UNIQ = @AcctUnique,
                                CURRENCY = @Currency,
                                CHG_GST = @ChgGst,
                                GST_RATE = @GstRate,
                                GST_EST = @GstEst,
                                NONGST_EST = @NonGstEst,
                                ADV_NO = @AdvNo,
                                ADV_BAT = @AdvBat,
                                FIN_BAT = @FinBat,
                                QED_DATE = @EditDate,
                                QED_TIME = @EditTime,
                                QED_OP = @EditOperator
                        WHEN NOT MATCHED THEN
                            INSERT (
                                NUMBER, DATE, TYPE, CLASS, PRODUCT, PROCESS, GRADE, LBS, U_PRICE,
                                DOLLARS, Description, SERIES, CHEQUE, T_SER, T_CHEQ, YEAR, ACCT_UNIQ,
                                CURRENCY, CHG_GST, GST_RATE, GST_EST, NONGST_EST, ADV_NO, ADV_BAT,
                                FIN_BAT, QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @Number, @Date, @Type, @Class, @Product, @Process, @Grade, @Lbs,
                                @UnitPrice, @Dollars, @Description, @Series, @Cheque, @TSeries,
                                @TCheque, @Year, @AcctUnique, @Currency, @ChgGst, @GstRate,
                                @GstEst, @NonGstEst, @AdvNo, @AdvBat, @FinBat, @AddDate,
                                @AddTime, @AddOperator
                            );";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        AddAccountParameters(command, account);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving account: {ex.Message}");
                return false;
            }
        }

        private Account MapAccountFromReader(SqlDataReader reader)
        {
            var account = new Account();

            try
            {
                account.Number = reader.GetDecimal(reader.GetOrdinal("NUMBER"));
                account.Date = reader.GetDateTime(reader.GetOrdinal("DATE"));
                account.Type = !reader.IsDBNull(reader.GetOrdinal("TYPE")) ? reader.GetString(reader.GetOrdinal("TYPE")) : null;
                account.Class = !reader.IsDBNull(reader.GetOrdinal("CLASS")) ? reader.GetString(reader.GetOrdinal("CLASS")) : null;
                account.Product = !reader.IsDBNull(reader.GetOrdinal("PRODUCT")) ? reader.GetString(reader.GetOrdinal("PRODUCT")) : null;
                account.Process = !reader.IsDBNull(reader.GetOrdinal("PROCESS")) ? reader.GetString(reader.GetOrdinal("PROCESS")) : null;
                account.Grade = !reader.IsDBNull(reader.GetOrdinal("GRADE")) ? reader.GetDecimal(reader.GetOrdinal("GRADE")) : 0;
                account.Lbs = !reader.IsDBNull(reader.GetOrdinal("LBS")) ? reader.GetDecimal(reader.GetOrdinal("LBS")) : 0;
                account.UnitPrice = !reader.IsDBNull(reader.GetOrdinal("U_PRICE")) ? reader.GetDecimal(reader.GetOrdinal("U_PRICE")) : 0;
                account.Dollars = !reader.IsDBNull(reader.GetOrdinal("DOLLARS")) ? reader.GetDecimal(reader.GetOrdinal("DOLLARS")) : 0;
                account.Description = !reader.IsDBNull(reader.GetOrdinal("Description")) ? reader.GetString(reader.GetOrdinal("Description")) : null;
                account.Series = !reader.IsDBNull(reader.GetOrdinal("SERIES")) ? reader.GetString(reader.GetOrdinal("SERIES")) : null;
                account.Cheque = !reader.IsDBNull(reader.GetOrdinal("CHEQUE")) ? reader.GetDecimal(reader.GetOrdinal("CHEQUE")) : 0;
                account.TSeries = !reader.IsDBNull(reader.GetOrdinal("T_SER")) ? reader.GetString(reader.GetOrdinal("T_SER")) : null;
                account.TCheque = !reader.IsDBNull(reader.GetOrdinal("T_CHEQ")) ? reader.GetDecimal(reader.GetOrdinal("T_CHEQ")) : 0;
                account.Year = !reader.IsDBNull(reader.GetOrdinal("YEAR")) ? reader.GetDecimal(reader.GetOrdinal("YEAR")) : 0;
                account.AcctUnique = !reader.IsDBNull(reader.GetOrdinal("ACCT_UNIQ")) ? reader.GetDecimal(reader.GetOrdinal("ACCT_UNIQ")) : 0;
                account.Currency = !reader.IsDBNull(reader.GetOrdinal("CURRENCY")) ? reader.GetString(reader.GetOrdinal("CURRENCY")) : null;
                account.ChgGst = !reader.IsDBNull(reader.GetOrdinal("CHG_GST")) && reader.GetBoolean(reader.GetOrdinal("CHG_GST"));
                account.GstRate = !reader.IsDBNull(reader.GetOrdinal("GST_RATE")) ? reader.GetDecimal(reader.GetOrdinal("GST_RATE")) : 0;
                account.GstEst = !reader.IsDBNull(reader.GetOrdinal("GST_EST")) ? reader.GetDecimal(reader.GetOrdinal("GST_EST")) : 0;
                account.NonGstEst = !reader.IsDBNull(reader.GetOrdinal("NONGST_EST")) ? reader.GetDecimal(reader.GetOrdinal("NONGST_EST")) : 0;
                account.AdvNo = !reader.IsDBNull(reader.GetOrdinal("ADV_NO")) ? reader.GetDecimal(reader.GetOrdinal("ADV_NO")) : 0;
                account.AdvBat = !reader.IsDBNull(reader.GetOrdinal("ADV_BAT")) ? reader.GetDecimal(reader.GetOrdinal("ADV_BAT")) : 0;
                account.FinBat = !reader.IsDBNull(reader.GetOrdinal("FIN_BAT")) ? reader.GetDecimal(reader.GetOrdinal("FIN_BAT")) : 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping account from reader: {ex.Message}");
            }

            return account;
        }

        private void AddAccountParameters(SqlCommand command, Account account)
        {
            command.Parameters.AddWithValue("@Number", account.Number);
            command.Parameters.AddWithValue("@Date", account.Date);
            command.Parameters.AddWithValue("@Type", (object)account.Type ?? DBNull.Value);
            command.Parameters.AddWithValue("@Class", (object)account.Class ?? DBNull.Value);
            command.Parameters.AddWithValue("@Product", (object)account.Product ?? DBNull.Value);
            command.Parameters.AddWithValue("@Process", (object)account.Process ?? DBNull.Value);
            command.Parameters.AddWithValue("@Grade", account.Grade);
            command.Parameters.AddWithValue("@Lbs", account.Lbs);
            command.Parameters.AddWithValue("@UnitPrice", account.UnitPrice);
            command.Parameters.AddWithValue("@Dollars", account.Dollars);
            command.Parameters.AddWithValue("@Description", (object)account.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@Series", (object)account.Series ?? DBNull.Value);
            command.Parameters.AddWithValue("@Cheque", account.Cheque);
            command.Parameters.AddWithValue("@TSeries", (object)account.TSeries ?? DBNull.Value);
            command.Parameters.AddWithValue("@TCheque", account.TCheque);
            command.Parameters.AddWithValue("@Year", account.Year);
            command.Parameters.AddWithValue("@AcctUnique", account.AcctUnique);
            command.Parameters.AddWithValue("@Currency", (object)account.Currency ?? DBNull.Value);
            command.Parameters.AddWithValue("@ChgGst", account.ChgGst);
            command.Parameters.AddWithValue("@GstRate", account.GstRate);
            command.Parameters.AddWithValue("@GstEst", account.GstEst);
            command.Parameters.AddWithValue("@NonGstEst", account.NonGstEst);
            command.Parameters.AddWithValue("@AdvNo", account.AdvNo);
            command.Parameters.AddWithValue("@AdvBat", account.AdvBat);
            command.Parameters.AddWithValue("@FinBat", account.FinBat);

            DateTime now = DateTime.Now;
            command.Parameters.AddWithValue("@EditDate", now.Date);
            command.Parameters.AddWithValue("@EditTime", now.ToString("HHmmss"));
            command.Parameters.AddWithValue("@EditOperator", Environment.UserName);
            command.Parameters.AddWithValue("@AddDate", now.Date);
            command.Parameters.AddWithValue("@AddTime", now.ToString("HHmmss"));
            command.Parameters.AddWithValue("@AddOperator", Environment.UserName);
        }
    }
} 