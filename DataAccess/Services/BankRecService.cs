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
    public class BankRecService : BaseDatabaseService, IBankRecService
    {
        public async Task<List<BankRec>> GetAllBankRecsAsync()
        {
            var bankRecs = new List<BankRec>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM BankRec ORDER BY ACCTDATE DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bankRecs.Add(MapBankRecFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all bank records: {ex.Message}");
            }

            return bankRecs;
        }

        public async Task<BankRec> GetBankRecByDateAsync(DateTime acctDate)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM BankRec WHERE ACCTDATE = @AcctDate";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@AcctDate", acctDate);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapBankRecFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting bank record: {ex.Message}");
            }

            return null;
        }

        public async Task<List<BankRec>> GetBankRecsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var bankRecs = new List<BankRec>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM BankRec WHERE ACCTDATE BETWEEN @StartDate AND @EndDate ORDER BY ACCTDATE DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                bankRecs.Add(MapBankRecFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting bank records by date range: {ex.Message}");
            }

            return bankRecs;
        }

        public async Task<bool> SaveBankRecAsync(BankRec bankRec)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
                        MERGE INTO BankRec AS target
                        USING (SELECT @AcctDate AS ACCTDATE) AS source
                        ON (target.ACCTDATE = source.ACCTDATE)
                        WHEN MATCHED THEN
                            UPDATE SET 
                                DATEDONE = @DateDone,
                                NOTE = @Note,
                                AMOUNT = @Amount,
                                QED_DATE = @QedDate,
                                QED_TIME = @QedTime,
                                QED_OP = @QedOp
                        WHEN NOT MATCHED THEN
                            INSERT (
                                ACCTDATE, DATEDONE, NOTE, AMOUNT,
                                QADD_DATE, QADD_TIME, QADD_OP
                            )
                            VALUES (
                                @AcctDate, @DateDone, @Note, @Amount,
                                @QaddDate, @QaddTime, @QaddOp
                            );";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        AddBankRecParameters(command, bankRec);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving bank record: {ex.Message}");
                return false;
            }
        }

        private BankRec MapBankRecFromReader(SqlDataReader reader)
        {
            var bankRec = new BankRec();

            try
            {
                bankRec.AcctDate = reader.GetDateTime(reader.GetOrdinal("ACCTDATE"));

                int dateDoneOrdinal = reader.GetOrdinal("DATEDONE");
                bankRec.DateDone = !reader.IsDBNull(dateDoneOrdinal) ? reader.GetDateTime(dateDoneOrdinal) : null;

                int noteOrdinal = reader.GetOrdinal("NOTE");
                bankRec.Note = !reader.IsDBNull(noteOrdinal) ? reader.GetString(noteOrdinal) : null;

                bankRec.Amount = reader.GetDecimal(reader.GetOrdinal("AMOUNT"));

                int qaddDateOrdinal = reader.GetOrdinal("QADD_DATE");
                bankRec.QaddDate = !reader.IsDBNull(qaddDateOrdinal) ? reader.GetDateTime(qaddDateOrdinal) : null;

                int qaddTimeOrdinal = reader.GetOrdinal("QADD_TIME");
                bankRec.QaddTime = !reader.IsDBNull(qaddTimeOrdinal) ? reader.GetString(qaddTimeOrdinal) : null;

                int qaddOpOrdinal = reader.GetOrdinal("QADD_OP");
                bankRec.QaddOp = !reader.IsDBNull(qaddOpOrdinal) ? reader.GetString(qaddOpOrdinal) : null;

                int qedDateOrdinal = reader.GetOrdinal("QED_DATE");
                bankRec.QedDate = !reader.IsDBNull(qedDateOrdinal) ? reader.GetDateTime(qedDateOrdinal) : null;

                int qedTimeOrdinal = reader.GetOrdinal("QED_TIME");
                bankRec.QedTime = !reader.IsDBNull(qedTimeOrdinal) ? reader.GetString(qedTimeOrdinal) : null;

                int qedOpOrdinal = reader.GetOrdinal("QED_OP");
                bankRec.QedOp = !reader.IsDBNull(qedOpOrdinal) ? reader.GetString(qedOpOrdinal) : null;

                int qdelDateOrdinal = reader.GetOrdinal("QDEL_DATE");
                bankRec.QdelDate = !reader.IsDBNull(qdelDateOrdinal) ? reader.GetDateTime(qdelDateOrdinal) : null;

                int qdelTimeOrdinal = reader.GetOrdinal("QDEL_TIME");
                bankRec.QdelTime = !reader.IsDBNull(qdelTimeOrdinal) ? reader.GetString(qdelTimeOrdinal) : null;

                int qdelOpOrdinal = reader.GetOrdinal("QDEL_OP");
                bankRec.QdelOp = !reader.IsDBNull(qdelOpOrdinal) ? reader.GetString(qdelOpOrdinal) : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping bank record from reader: {ex.Message}");
            }

            return bankRec;
        }

        private void AddBankRecParameters(SqlCommand command, BankRec bankRec)
        {
            command.Parameters.AddWithValue("@AcctDate", bankRec.AcctDate);
            command.Parameters.AddWithValue("@DateDone", (object)bankRec.DateDone ?? DBNull.Value);
            command.Parameters.AddWithValue("@Note", (object)bankRec.Note ?? DBNull.Value);
            command.Parameters.AddWithValue("@Amount", bankRec.Amount);

            DateTime now = DateTime.Now;
            if (command.CommandText.Contains("UPDATE"))
            {
                command.Parameters.AddWithValue("@QedDate", now);
                command.Parameters.AddWithValue("@QedTime", now.ToString("HHmmss"));
                command.Parameters.AddWithValue("@QedOp", Environment.UserName);
            }
            else
            {
                command.Parameters.AddWithValue("@QaddDate", now);
                command.Parameters.AddWithValue("@QaddTime", now.ToString("HHmmss"));
                command.Parameters.AddWithValue("@QaddOp", Environment.UserName);
            }
        }
    }
} 