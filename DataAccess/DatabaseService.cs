using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            _connectionString = "Server=DESKTOP-LQ92Q06;Database=PackagingPaymentSystem;User Id=localDB;Password=528database@JK;";
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Searches for growers by name or number
        /// </summary>
        /// <param name="searchTerm">The search term (name or number)</param>
        /// <returns>List of matching growers</returns>
        public async Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm)
        {
            var results = new List<GrowerSearchResult>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT NUMBER, NAME, CHEQNAME, CITY, PHONE 
                        FROM Grower 
                        WHERE NAME LIKE @SearchTerm 
                        OR CHEQNAME LIKE @SearchTerm 
                        OR CONVERT(VARCHAR, NUMBER) LIKE @SearchTerm
                        ORDER BY NAME";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new GrowerSearchResult
                                {
                                    GrowerNumber = reader.GetDecimal(0),
                                    GrowerName = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                                    ChequeName = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                                    City = !reader.IsDBNull(3) ? reader.GetString(3) : "",
                                    Phone = !reader.IsDBNull(4) ? reader.GetString(4) : ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching growers: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Gets a grower by number
        /// </summary>
        /// <param name="growerNumber">The grower number</param>
        /// <returns>The grower if found, null otherwise</returns>
        public async Task<Grower> GetGrowerByNumberAsync(decimal growerNumber)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // First, let's check if the connection is successful and the Grower table exists
                    string checkTableSql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Grower'";
                    using (SqlCommand checkCommand = new SqlCommand(checkTableSql, connection))
                    {
                        int tableCount = (int)await checkCommand.ExecuteScalarAsync();
                        if (tableCount == 0)
                        {
                            Debug.WriteLine("Grower table does not exist in the database");
                            return null;
                        }
                    }

                    string sql = "SELECT * FROM Grower WHERE NUMBER = @GrowerNumber";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@GrowerNumber", growerNumber);

                        try
                        {
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                // Debug column names
                                var schemaTable = reader.GetSchemaTable();
                                if (schemaTable != null)
                                {
                                    foreach (DataRow row in schemaTable.Rows)
                                    {
                                        string columnName = row["ColumnName"].ToString();
                                        Debug.WriteLine($"Column found: {columnName}");
                                    }
                                }

                                if (await reader.ReadAsync())
                                {
                                    return MapGrowerFromReader(reader);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error executing reader: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting grower: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Create a default grower with the error message for debugging
                return new Grower
                {
                    GrowerNumber = growerNumber,
                    GrowerName = $"Error: {ex.Message}",
                    Notes = ex.StackTrace
                };
            }

            return null;
        }

        /// <summary>
        /// Saves a grower to the database
        /// </summary>
        /// <param name="grower">The grower to save</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SaveGrowerAsync(Grower grower)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if the grower exists
                    string checkSql = "SELECT COUNT(*) FROM Grower WHERE NUMBER = @GrowerNumber";
                    bool growerExists = false;

                    using (SqlCommand checkCommand = new SqlCommand(checkSql, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@GrowerNumber", grower.GrowerNumber);
                        growerExists = (int)await checkCommand.ExecuteScalarAsync() > 0;
                    }

                    string sql;
                    if (growerExists)
                    {
                        // Update existing grower
                        sql = @"
                            UPDATE Grower SET 
                                STATUS = @Status,
                                CHEQNAME = @ChequeName,
                                NAME = @GrowerName,
                                STREET = @Address,
                                CITY = @City,
                                PROV = @Province,
                                PCODE = @Postal,
                                PHONE = @Phone,
                                ACRES = @Acres,
                                NOTES = @Notes,
                                CONTRACT = @Contract,
                                CURRENCY = @Currency,
                                CONTLIM = @ContractLimit,
                                PAYGRP = @PayGroup,
                                ONHOLD = @OnHold,
                                PHONE2 = @PhoneAdditional1,
                                STREET2 = @AddressLine2,
                                ALT_NAME1 = @OtherNames,
                                ALT_PHONE1 = @PhoneAdditional1,
                                ALT_NAME2 = '',
                                ALT_PHONE2 = @PhoneAdditional2,
                                NOTE2 = '',
                                LY_FRESH = @LYFresh,
                                LY_OTHER = @LYOther,
                                QED_DATE = @EditDate,
                                QED_TIME = @EditTime,
                                QED_OP = @EditOperator,
                                CERTIFIED = @Certified,
                                CHG_GST = @ChargeGST
                            WHERE NUMBER = @GrowerNumber";
                    }
                    else
                    {
                        // Insert new grower
                        sql = @"
                            INSERT INTO Grower (
                                NUMBER, STATUS, CHEQNAME, NAME, STREET, CITY, PROV, PCODE, PHONE,
                                ACRES, NOTES, CONTRACT, CURRENCY, CONTLIM, PAYGRP, ONHOLD,
                                PHONE2, STREET2, ALT_NAME1, ALT_PHONE1, ALT_NAME2, ALT_PHONE2, NOTE2,
                                LY_FRESH, LY_OTHER, QADD_DATE, QADD_TIME, QADD_OP, CERTIFIED, CHG_GST
                            ) VALUES (
                                @GrowerNumber, @Status, @ChequeName, @GrowerName, @Address, @City, @Province, @Postal, @Phone,
                                @Acres, @Notes, @Contract, @Currency, @ContractLimit, @PayGroup, @OnHold,
                                @PhoneAdditional1, @AddressLine2, @OtherNames, @PhoneAdditional1, '', @PhoneAdditional2, '',
                                @LYFresh, @LYOther, @AddDate, @AddTime, @AddOperator, @Certified, @ChargeGST
                            )";
                    }

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        // Add parameters
                        command.Parameters.AddWithValue("@GrowerNumber", grower.GrowerNumber);
                        command.Parameters.AddWithValue("@Status", 1); // Default status
                        command.Parameters.AddWithValue("@ChequeName", grower.ChequeName ?? "");
                        command.Parameters.AddWithValue("@GrowerName", grower.GrowerName ?? "");
                        command.Parameters.AddWithValue("@Address", grower.Address ?? "");
                        command.Parameters.AddWithValue("@City", grower.City ?? "");
                        command.Parameters.AddWithValue("@Province", grower.Prov ?? "");
                        command.Parameters.AddWithValue("@Postal", grower.Postal ?? "");
                        command.Parameters.AddWithValue("@Phone", grower.Phone ?? "");
                        command.Parameters.AddWithValue("@Acres", grower.Acres);
                        command.Parameters.AddWithValue("@Notes", grower.Notes ?? "");
                        command.Parameters.AddWithValue("@Contract", grower.Contract ?? "");
                        command.Parameters.AddWithValue("@Currency", grower.Currency.ToString());
                        command.Parameters.AddWithValue("@ContractLimit", grower.ContractLimit);
                        command.Parameters.AddWithValue("@PayGroup", grower.PayGroup.ToString());
                        command.Parameters.AddWithValue("@OnHold", grower.OnHold);
                        command.Parameters.AddWithValue("@PhoneAdditional1", grower.PhoneAdditional1 ?? "");
                        command.Parameters.AddWithValue("@AddressLine2", ""); // Not in the original model
                        command.Parameters.AddWithValue("@OtherNames", grower.OtherNames ?? "");
                        command.Parameters.AddWithValue("@PhoneAdditional2", grower.PhoneAdditional2 ?? "");
                        command.Parameters.AddWithValue("@LYFresh", grower.LYFresh);
                        command.Parameters.AddWithValue("@LYOther", grower.LYOther);
                        command.Parameters.AddWithValue("@Certified", grower.Certified ?? "");
                        command.Parameters.AddWithValue("@ChargeGST", grower.ChargeGST);

                        // Date/time parameters
                        DateTime now = DateTime.Now;
                        if (growerExists)
                        {
                            command.Parameters.AddWithValue("@EditDate", now.ToString("yyyyMMdd"));
                            command.Parameters.AddWithValue("@EditTime", now.ToString("HHmmss"));
                            command.Parameters.AddWithValue("@EditOperator", Environment.UserName);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@AddDate", now.ToString("yyyyMMdd"));
                            command.Parameters.AddWithValue("@AddTime", now.ToString("HHmmss"));
                            command.Parameters.AddWithValue("@AddOperator", Environment.UserName);
                        }

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving grower: {ex.Message}");
                return false;
            }
        }

        private Grower MapGrowerFromReader(SqlDataReader reader)
        {
            var grower = new Grower();

            try
            {
                // Get all column names from the reader
                var columnNames = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                }

                // Map fields safely, checking if columns exist
                if (columnNames.Contains("NUMBER"))
                    grower.GrowerNumber = reader.GetDecimal(reader.GetOrdinal("NUMBER"));

                if (columnNames.Contains("CHEQNAME") && !reader.IsDBNull(reader.GetOrdinal("CHEQNAME")))
                    grower.ChequeName = reader.GetString(reader.GetOrdinal("CHEQNAME"));

                if (columnNames.Contains("NAME") && !reader.IsDBNull(reader.GetOrdinal("NAME")))
                    grower.GrowerName = reader.GetString(reader.GetOrdinal("NAME"));

                if (columnNames.Contains("STREET") && !reader.IsDBNull(reader.GetOrdinal("STREET")))
                    grower.Address = reader.GetString(reader.GetOrdinal("STREET"));

                if (columnNames.Contains("CITY") && !reader.IsDBNull(reader.GetOrdinal("CITY")))
                    grower.City = reader.GetString(reader.GetOrdinal("CITY"));

                if (columnNames.Contains("PROV") && !reader.IsDBNull(reader.GetOrdinal("PROV")))
                    grower.Prov = reader.GetString(reader.GetOrdinal("PROV"));

                if (columnNames.Contains("PCODE") && !reader.IsDBNull(reader.GetOrdinal("PCODE")))
                    grower.Postal = reader.GetString(reader.GetOrdinal("PCODE"));

                if (columnNames.Contains("PHONE") && !reader.IsDBNull(reader.GetOrdinal("PHONE")))
                    grower.Phone = reader.GetString(reader.GetOrdinal("PHONE"));

                if (columnNames.Contains("PHONE2") && !reader.IsDBNull(reader.GetOrdinal("PHONE2")))
                {
                    grower.PhoneAdditional1 = reader.GetString(reader.GetOrdinal("PHONE2")).Trim();
                    Debug.WriteLine($"Phone2 value from database: {grower.PhoneAdditional1}"); // Debug line
                }

                if (columnNames.Contains("ACRES") && !reader.IsDBNull(reader.GetOrdinal("ACRES")))
                    grower.Acres = reader.GetDecimal(reader.GetOrdinal("ACRES"));

                if (columnNames.Contains("NOTES"))
                {
                    int notesOrdinal = reader.GetOrdinal("NOTES");
                    grower.Notes = !reader.IsDBNull(notesOrdinal) ? 
                        reader.GetString(notesOrdinal).TrimEnd() : string.Empty;
                }

                if (columnNames.Contains("CONTRACT") && !reader.IsDBNull(reader.GetOrdinal("CONTRACT")))
                    grower.Contract = reader.GetString(reader.GetOrdinal("CONTRACT"));

                if (columnNames.Contains("CURRENCY"))
                {
                    int currencyOrdinal = reader.GetOrdinal("CURRENCY");
                    if (!reader.IsDBNull(currencyOrdinal))
                    {
                        string currencyStr = reader.GetString(currencyOrdinal).Trim();
                        grower.Currency = !string.IsNullOrEmpty(currencyStr) ? currencyStr[0] : 'C';
                    }
                    else
                    {
                        grower.Currency = 'C';
                    }
                }

                if (columnNames.Contains("CONTLIM") && !reader.IsDBNull(reader.GetOrdinal("CONTLIM")))
                    grower.ContractLimit = reader.GetInt32(reader.GetOrdinal("CONTLIM"));

                if (columnNames.Contains("PAYGRP"))
                {
                    int payGrpOrdinal = reader.GetOrdinal("PAYGRP");
                    if (!reader.IsDBNull(payGrpOrdinal))
                    {
                        string payGrpStr = reader.GetString(payGrpOrdinal).Trim();
                        grower.PayGroup = !string.IsNullOrEmpty(payGrpStr) ? payGrpStr : "1";
                    }
                    else
                    {
                        grower.PayGroup = "1";
                    }
                }

                if (columnNames.Contains("ONHOLD"))
                {
                    int onHoldOrdinal = reader.GetOrdinal("ONHOLD");
                    grower.OnHold = !reader.IsDBNull(onHoldOrdinal) && reader.GetBoolean(onHoldOrdinal);
                }


                if (columnNames.Contains("ALT_NAME1") && !reader.IsDBNull(reader.GetOrdinal("ALT_NAME1")))
                    grower.OtherNames = reader.GetString(reader.GetOrdinal("ALT_NAME1"));

                if (columnNames.Contains("ALT_PHONE2") && !reader.IsDBNull(reader.GetOrdinal("ALT_PHONE2")))
                    grower.PhoneAdditional2 = reader.GetString(reader.GetOrdinal("ALT_PHONE2"));

                if (columnNames.Contains("LY_FRESH") && !reader.IsDBNull(reader.GetOrdinal("LY_FRESH")))
                    grower.LYFresh = reader.GetInt32(reader.GetOrdinal("LY_FRESH"));

                if (columnNames.Contains("LY_OTHER") && !reader.IsDBNull(reader.GetOrdinal("LY_OTHER")))
                    grower.LYOther = reader.GetInt32(reader.GetOrdinal("LY_OTHER"));

                if (columnNames.Contains("CERTIFIED") && !reader.IsDBNull(reader.GetOrdinal("CERTIFIED")))
                    grower.Certified = reader.GetString(reader.GetOrdinal("CERTIFIED"));

                if (columnNames.Contains("CHG_GST") && !reader.IsDBNull(reader.GetOrdinal("CHG_GST")))
                    grower.ChargeGST = reader.GetBoolean(reader.GetOrdinal("CHG_GST"));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping grower from reader: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return grower;
        }

        /// <summary>
        /// Gets all growers
        /// </summary>
        /// <returns>List of all growers</returns>
        public async Task<List<GrowerSearchResult>> GetAllGrowersAsync()
        {
            var results = new List<GrowerSearchResult>();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
                SELECT NUMBER, NAME, CHEQNAME, CITY, PHONE 
                FROM Grower 
                ORDER BY NAME";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new GrowerSearchResult
                                {
                                    GrowerNumber = reader.GetDecimal(0),
                                    GrowerName = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                                    ChequeName = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                                    City = !reader.IsDBNull(3) ? reader.GetString(3) : "",
                                    Phone = !reader.IsDBNull(4) ? reader.GetString(4) : ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting all growers: {ex.Message}");
            }

            return results;
        }

        public async Task<List<Grower>> GetGrowersAsync()
        {
            var growers = new List<Grower>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT NUMBER, NAME, STREET, CITY, PROV, PCODE, PHONE, PHONE2, CURRENCY, PAYGRP, CONTLIM, NOTES, ONHOLD " +
                    "FROM Grower ORDER BY NUMBER", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                growers.Add(new Grower
                                {
                                    GrowerNumber = reader.GetDecimal(reader.GetOrdinal("NUMBER")),
                                    GrowerName = reader["NAME"].ToString(),
                                    Address = reader["STREET"].ToString(),
                                    City = reader["CITY"].ToString(),
                                    Postal = reader["PCODE"].ToString(),
                                    Phone = reader["PHONE"].ToString(),
                                    PhoneAdditional1 = reader["PHONE2"].ToString(),
                                    Currency = !reader.IsDBNull(reader.GetOrdinal("CURRENCY")) ?
                                    reader.GetString(reader.GetOrdinal("CURRENCY"))[0] : 'C',
                                    PayGroup = reader["PAYGRP"].ToString(),
                                    ContractLimit = !reader.IsDBNull(reader.GetOrdinal("CONTLIM")) ?
                                        Convert.ToInt32(reader.GetValue(reader.GetOrdinal("CONTLIM"))) : 0,
                                    Notes = reader["NOTES"].ToString(),
                                    OnHold = !reader.IsDBNull(reader.GetOrdinal("ONHOLD")) &&
                                        reader.GetBoolean(reader.GetOrdinal("ONHOLD"))
                                });
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error adding grower: {ex.Message}");
                                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                                // Continue with next record instead of failing completely
                                continue;
                            }
                        }
                    }
                }
            }

            return growers;
        }

        public async Task<Grower> GetGrowerByIdAsync(string growerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT NUMBER, NAME, STREET, CITY, PROV, PCODE, PHONE, PHONE2, CURRENCY, PAYGRP, CONTLIM, NOTES, ONHOLD " +
                    "FROM Grower WHERE NUMBER = @GrowerId", connection))
                {
                    command.Parameters.AddWithValue("@GrowerId", decimal.Parse(growerId));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Grower
                            {
                                GrowerNumber = reader.GetDecimal(reader.GetOrdinal("NUMBER")),
                                GrowerName = reader["NAME"].ToString(),
                                Address = reader["STREET"].ToString(),
                                City = reader["CITY"].ToString(),
                                Postal = reader["PCODE"].ToString(),
                                Phone = reader["PHONE"].ToString(),
                                PhoneAdditional1 = reader["PHONE2"].ToString(),
                                Currency = !reader.IsDBNull(reader.GetOrdinal("CURRENCY")) ? 
                                    reader.GetString(reader.GetOrdinal("CURRENCY"))[0] : 'C',
                                PayGroup = reader["PAYGRP"].ToString(),
                                ContractLimit = !reader.IsDBNull(reader.GetOrdinal("CONTLIM")) ? 
                                    reader.GetInt32(reader.GetOrdinal("CONTLIM")) : 0,
                                Notes = reader["NOTES"].ToString(),
                                OnHold = !reader.IsDBNull(reader.GetOrdinal("ONHOLD")) && 
                                    reader.GetBoolean(reader.GetOrdinal("ONHOLD"))
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<List<PayGroup>> GetPayGroupsAsync()
        {
            var payGroups = new List<PayGroup>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var command = new SqlCommand(
                    "SELECT PAYGRP, Description, DEF_PRLVL FROM PayGrp ORDER BY PAYGRP", connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            payGroups.Add(new PayGroup
                            {
                                PayGroupId = reader["PAYGRP"].ToString(),
                                Description = reader["Description"].ToString(),
                                DefaultPriceLevel = reader["DEF_PRLVL"].ToString()
                            });
                        }
                    }
                }
            }

            return payGroups;
        }
        public class PayGroup
        {
            public string PayGroupId { get; set; }
            public string Description { get; set; }
            public string DefaultPriceLevel { get; set; }
        }
    }

    
}
