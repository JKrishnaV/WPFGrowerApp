using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    public class ChequeService : BaseDatabaseService, IChequeService
    {
        public async Task<List<Cheque>> GetAllChequesAsync()
        {
            var cheques = new List<Cheque>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT * FROM CHEQUE ORDER BY DATE DESC", connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                cheques.Add(MapChequeFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllChequesAsync: {ex.Message}");
                throw;
            }
            return cheques;
        }

        public async Task<Cheque> GetChequeBySeriesAndNumberAsync(string series, decimal chequeNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT * FROM CHEQUE WHERE SERIES = @Series AND CHEQUE_NUMBER = @ChequeNumber", connection))
                    {
                        command.Parameters.AddWithValue("@Series", series);
                        command.Parameters.AddWithValue("@ChequeNumber", chequeNumber);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapChequeFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetChequeBySeriesAndNumberAsync: {ex.Message}");
                throw;
            }
            return null;
        }

        public async Task<List<Cheque>> GetChequesByGrowerNumberAsync(decimal growerNumber)
        {
            var cheques = new List<Cheque>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT * FROM CHEQUE WHERE GROWER_NUMBER = @GrowerNumber ORDER BY DATE DESC", connection))
                    {
                        command.Parameters.AddWithValue("@GrowerNumber", growerNumber);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                cheques.Add(MapChequeFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetChequesByGrowerNumberAsync: {ex.Message}");
                throw;
            }
            return cheques;
        }

        public async Task<List<Cheque>> GetChequesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var cheques = new List<Cheque>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT * FROM CHEQUE WHERE DATE BETWEEN @StartDate AND @EndDate ORDER BY DATE DESC", connection))
                    {
                        command.Parameters.AddWithValue("@StartDate", startDate);
                        command.Parameters.AddWithValue("@EndDate", endDate);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                cheques.Add(MapChequeFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetChequesByDateRangeAsync: {ex.Message}");
                throw;
            }
            return cheques;
        }

        public async Task<bool> SaveChequeAsync(Cheque cheque)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(@"
                        MERGE INTO CHEQUE AS target
                        USING (SELECT @Series AS SERIES, @ChequeNumber AS CHEQUE_NUMBER) AS source
                        ON (target.SERIES = source.SERIES AND target.CHEQUE_NUMBER = source.CHEQUE_NUMBER)
                        WHEN MATCHED THEN
                            UPDATE SET
                                GROWER_NUMBER = @GrowerNumber,
                                DATE = @Date,
                                AMOUNT = @Amount,
                                YEAR = @Year,
                                CHEQUE_TYPE = @ChequeType,
                                VOID = @Void,
                                DATE_CLEAR = @DateClear,
                                IS_CLEARED = @IsCleared,
                                CURRENCY = @Currency,
                                QED_DATE = @QedDate,
                                QED_TIME = @QedTime,
                                QED_OP = @QedOp
                        WHEN NOT MATCHED THEN
                            INSERT (SERIES, CHEQUE_NUMBER, GROWER_NUMBER, DATE, AMOUNT, YEAR, CHEQUE_TYPE, VOID, 
                                   DATE_CLEAR, IS_CLEARED, CURRENCY, QADD_DATE, QADD_TIME, QADD_OP)
                            VALUES (@Series, @ChequeNumber, @GrowerNumber, @Date, @Amount, @Year, @ChequeType, @Void,
                                   @DateClear, @IsCleared, @Currency, GETDATE(), CONVERT(varchar(8), GETDATE(), 108), SYSTEM_USER);",
                        connection))
                    {
                        AddChequeParameters(command, cheque);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
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
                    using (var command = new SqlCommand(@"
                        UPDATE CHEQUE 
                        SET VOID = 1,
                            QED_DATE = GETDATE(),
                            QED_TIME = CONVERT(varchar(8), GETDATE(), 108),
                            QED_OP = SYSTEM_USER
                        WHERE SERIES = @Series AND CHEQUE_NUMBER = @ChequeNumber",
                        connection))
                    {
                        command.Parameters.AddWithValue("@Series", series);
                        command.Parameters.AddWithValue("@ChequeNumber", chequeNumber);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VoidChequeAsync: {ex.Message}");
                return false;
            }
        }

        private Cheque MapChequeFromReader(SqlDataReader reader)
        {
            return new Cheque
            {
                Series = reader["SERIES"].ToString(),
                ChequeNumber = Convert.ToDecimal(reader["CHEQUE_NUMBER"]),
                GrowerNumber = Convert.ToDecimal(reader["GROWER_NUMBER"]),
                Date = Convert.ToDateTime(reader["DATE"]),
                Amount = Convert.ToDecimal(reader["AMOUNT"]),
                Year = Convert.ToDecimal(reader["YEAR"]),
                ChequeType = reader["CHEQUE_TYPE"].ToString(),
                Void = Convert.ToBoolean(reader["VOID"]),
                DateClear = reader["DATE_CLEAR"] != DBNull.Value ? Convert.ToDateTime(reader["DATE_CLEAR"]) : null,
                IsCleared = Convert.ToBoolean(reader["IS_CLEARED"]),
                Currency = reader["CURRENCY"].ToString(),
                QaddDate = reader["QADD_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["QADD_DATE"]) : null,
                QaddTime = reader["QADD_TIME"].ToString(),
                QaddOp = reader["QADD_OP"].ToString(),
                QedDate = reader["QED_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["QED_DATE"]) : null,
                QedTime = reader["QED_TIME"].ToString(),
                QedOp = reader["QED_OP"].ToString(),
                QdelDate = reader["QDEL_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["QDEL_DATE"]) : null,
                QdelTime = reader["QDEL_TIME"].ToString(),
                QdelOp = reader["QDEL_OP"].ToString()
            };
        }

        private void AddChequeParameters(SqlCommand command, Cheque cheque)
        {
            command.Parameters.AddWithValue("@Series", cheque.Series);
            command.Parameters.AddWithValue("@ChequeNumber", cheque.ChequeNumber);
            command.Parameters.AddWithValue("@GrowerNumber", cheque.GrowerNumber);
            command.Parameters.AddWithValue("@Date", cheque.Date);
            command.Parameters.AddWithValue("@Amount", cheque.Amount);
            command.Parameters.AddWithValue("@Year", cheque.Year);
            command.Parameters.AddWithValue("@ChequeType", cheque.ChequeType ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Void", cheque.Void);
            command.Parameters.AddWithValue("@DateClear", cheque.DateClear ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IsCleared", cheque.IsCleared);
            command.Parameters.AddWithValue("@Currency", cheque.Currency ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@QedDate", cheque.QedDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@QedTime", cheque.QedTime ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@QedOp", cheque.QedOp ?? (object)DBNull.Value);
        }
    }
} 