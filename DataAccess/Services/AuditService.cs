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
    public class AuditService : BaseDatabaseService, IAuditService
    {
        public async Task<List<Audit>> GetAllAuditsAsync()
        {
            var audits = new List<Audit>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM Audit ORDER BY DAY_UNIQ DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                audits.Add(MapAuditFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all audits: {ex.Message}");
            }

            return audits;
        }

        public async Task<Audit> GetAuditByDayUniqAsync(decimal dayUniq)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM Audit WHERE DAY_UNIQ = @DayUniq";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@DayUniq", dayUniq);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapAuditFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting audit: {ex.Message}");
            }

            return null;
        }

        public async Task<List<Audit>> GetAuditsByAccountUniqAsync(decimal acctUniq)
        {
            var audits = new List<Audit>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM Audit WHERE ACCT_UNIQ = @AcctUniq ORDER BY DAY_UNIQ DESC";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@AcctUniq", acctUniq);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                audits.Add(MapAuditFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting audits by account: {ex.Message}");
            }

            return audits;
        }

        public async Task<bool> SaveAuditAsync(Audit audit)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
                        MERGE INTO Audit AS target
                        USING (SELECT @DayUniq AS DAY_UNIQ) AS source
                        ON (target.DAY_UNIQ = source.DAY_UNIQ)
                        WHEN MATCHED THEN
                            UPDATE SET 
                                ACCT_UNIQ = @AcctUniq,
                                QADD_DATE = @QaddDate,
                                QADD_TIME = @QaddTime,
                                QADD_OP = @QaddOp,
                                QED_DATE = @QedDate,
                                QED_TIME = @QedTime,
                                QED_OP = @QedOp,
                                QDEL_DATE = @QdelDate,
                                QDEL_TIME = @QdelTime,
                                QDEL_OP = @QdelOp
                        WHEN NOT MATCHED THEN
                            INSERT (
                                DAY_UNIQ, ACCT_UNIQ, QADD_DATE, QADD_TIME, QADD_OP,
                                QED_DATE, QED_TIME, QED_OP, QDEL_DATE, QDEL_TIME, QDEL_OP
                            )
                            VALUES (
                                @DayUniq, @AcctUniq, @QaddDate, @QaddTime, @QaddOp,
                                @QedDate, @QedTime, @QedOp, @QdelDate, @QdelTime, @QdelOp
                            );";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        AddAuditParameters(command, audit);
                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving audit: {ex.Message}");
                return false;
            }
        }

        private Audit MapAuditFromReader(SqlDataReader reader)
        {
            var audit = new Audit();

            try
            {
                audit.DayUniq = reader.GetDecimal(reader.GetOrdinal("DAY_UNIQ"));
                audit.AcctUniq = reader.GetDecimal(reader.GetOrdinal("ACCT_UNIQ"));

                int qaddDateOrdinal = reader.GetOrdinal("QADD_DATE");
                audit.QaddDate = !reader.IsDBNull(qaddDateOrdinal) ? reader.GetDateTime(qaddDateOrdinal) : null;

                int qaddTimeOrdinal = reader.GetOrdinal("QADD_TIME");
                audit.QaddTime = !reader.IsDBNull(qaddTimeOrdinal) ? reader.GetString(qaddTimeOrdinal) : null;

                int qaddOpOrdinal = reader.GetOrdinal("QADD_OP");
                audit.QaddOp = !reader.IsDBNull(qaddOpOrdinal) ? reader.GetString(qaddOpOrdinal) : null;

                int qedDateOrdinal = reader.GetOrdinal("QED_DATE");
                audit.QedDate = !reader.IsDBNull(qedDateOrdinal) ? reader.GetDateTime(qedDateOrdinal) : null;

                int qedTimeOrdinal = reader.GetOrdinal("QED_TIME");
                audit.QedTime = !reader.IsDBNull(qedTimeOrdinal) ? reader.GetString(qedTimeOrdinal) : null;

                int qedOpOrdinal = reader.GetOrdinal("QED_OP");
                audit.QedOp = !reader.IsDBNull(qedOpOrdinal) ? reader.GetString(qedOpOrdinal) : null;

                int qdelDateOrdinal = reader.GetOrdinal("QDEL_DATE");
                audit.QdelDate = !reader.IsDBNull(qdelDateOrdinal) ? reader.GetDateTime(qdelDateOrdinal) : null;

                int qdelTimeOrdinal = reader.GetOrdinal("QDEL_TIME");
                audit.QdelTime = !reader.IsDBNull(qdelTimeOrdinal) ? reader.GetString(qdelTimeOrdinal) : null;

                int qdelOpOrdinal = reader.GetOrdinal("QDEL_OP");
                audit.QdelOp = !reader.IsDBNull(qdelOpOrdinal) ? reader.GetString(qdelOpOrdinal) : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping audit from reader: {ex.Message}");
            }

            return audit;
        }

        private void AddAuditParameters(SqlCommand command, Audit audit)
        {
            command.Parameters.AddWithValue("@DayUniq", audit.DayUniq);
            command.Parameters.AddWithValue("@AcctUniq", audit.AcctUniq);
            command.Parameters.AddWithValue("@QaddDate", (object)audit.QaddDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@QaddTime", (object)audit.QaddTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@QaddOp", (object)audit.QaddOp ?? DBNull.Value);
            command.Parameters.AddWithValue("@QedDate", (object)audit.QedDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@QedTime", (object)audit.QedTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@QedOp", (object)audit.QedOp ?? DBNull.Value);
            command.Parameters.AddWithValue("@QdelDate", (object)audit.QdelDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@QdelTime", (object)audit.QdelTime ?? DBNull.Value);
            command.Parameters.AddWithValue("@QdelOp", (object)audit.QdelOp ?? DBNull.Value);
        }
    }
} 