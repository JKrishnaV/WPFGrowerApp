using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            // Connection string with the provided credentials
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
            catch (Exception)
            {
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
                                    GrowerName = reader.GetString(1),
                                    ChequeName = reader.GetString(2),
                                    City = reader.GetString(3),
                                    Phone = reader.GetString(4)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // In a production app, you would log this exception
                Console.WriteLine($"Error searching growers: {ex.Message}");
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

                    string sql = "SELECT * FROM Grower WHERE NUMBER = @GrowerNumber";

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@GrowerNumber", growerNumber);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapGrowerFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // In a production app, you would log this exception
                Console.WriteLine($"Error getting grower: {ex.Message}");
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
                        command.Parameters.AddWithValue("@Province", ""); // Not in the original model
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

                        // Audit fields
                        DateTime now = DateTime.Now;
                        string currentTime = now.ToString("HH:mm:ss");
                        string currentUser = Environment.UserName;

                        if (growerExists)
                        {
                            command.Parameters.AddWithValue("@EditDate", now);
                            command.Parameters.AddWithValue("@EditTime", currentTime);
                            command.Parameters.AddWithValue("@EditOperator", currentUser);
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@AddDate", now);
                            command.Parameters.AddWithValue("@AddTime", currentTime);
                            command.Parameters.AddWithValue("@AddOperator", currentUser);
                        }

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // In a production app, you would log this exception
                Console.WriteLine($"Error saving grower: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Maps a SqlDataReader to a Grower object
        /// </summary>
        private Grower MapGrowerFromReader(SqlDataReader reader)
        {
            var grower = new Grower();

            grower.GrowerNumber = reader.GetDecimal(reader.GetOrdinal("NUMBER"));
            
            // Handle nullable fields
            if (!reader.IsDBNull(reader.GetOrdinal("CHEQNAME")))
                grower.ChequeName = reader.GetString(reader.GetOrdinal("CHEQNAME"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("NAME")))
                grower.GrowerName = reader.GetString(reader.GetOrdinal("NAME"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("STREET")))
                grower.Address = reader.GetString(reader.GetOrdinal("STREET"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("CITY")))
                grower.City = reader.GetString(reader.GetOrdinal("CITY"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("PCODE")))
                grower.Postal = reader.GetString(reader.GetOrdinal("PCODE"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("PHONE")))
                grower.Phone = reader.GetString(reader.GetOrdinal("PHONE"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("ACRES")))
                grower.Acres = reader.GetDecimal(reader.GetOrdinal("ACRES"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("NOTES")))
                grower.Notes = reader.GetString(reader.GetOrdinal("NOTES"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("CONTRACT")))
                grower.Contract = reader.GetString(reader.GetOrdinal("CONTRACT"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("CURRENCY")))
                grower.Currency = reader.GetString(reader.GetOrdinal("CURRENCY"))[0];
            
            if (!reader.IsDBNull(reader.GetOrdinal("CONTLIM")))
                grower.ContractLimit = reader.GetInt32(reader.GetOrdinal("CONTLIM"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("PAYGRP")))
                grower.PayGroup = int.Parse(reader.GetString(reader.GetOrdinal("PAYGRP")));
            
            if (!reader.IsDBNull(reader.GetOrdinal("ONHOLD")))
                grower.OnHold = reader.GetBoolean(reader.GetOrdinal("ONHOLD"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("PHONE2")))
                grower.PhoneAdditional1 = reader.GetString(reader.GetOrdinal("PHONE2"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("ALT_NAME1")))
                grower.OtherNames = reader.GetString(reader.GetOrdinal("ALT_NAME1"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("ALT_PHONE2")))
                grower.PhoneAdditional2 = reader.GetString(reader.GetOrdinal("ALT_PHONE2"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("LY_FRESH")))
                grower.LYFresh = reader.GetInt32(reader.GetOrdinal("LY_FRESH"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("LY_OTHER")))
                grower.LYOther = reader.GetInt32(reader.GetOrdinal("LY_OTHER"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("CERTIFIED")))
                grower.Certified = reader.GetString(reader.GetOrdinal("CERTIFIED"));
            
            if (!reader.IsDBNull(reader.GetOrdinal("CHG_GST")))
                grower.ChargeGST = reader.GetBoolean(reader.GetOrdinal("CHG_GST"));

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
                // In a production app, you would log this exception
                Console.WriteLine($"Error getting all growers: {ex.Message}");
            }

            return results;
        }

    }

    /// <summary>
    /// Simple class for grower search results
    /// </summary>
    public class GrowerSearchResult
    {
        public decimal GrowerNumber { get; set; }
        public string GrowerName { get; set; }
        public string ChequeName { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
    }
}
