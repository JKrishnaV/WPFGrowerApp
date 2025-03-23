using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Repositories
{
    public class ChequeRepository
    {
        private readonly DapperConnectionManager _connectionManager;

        public ChequeRepository(DapperConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Gets cheques for a specific grower
        /// </summary>
        /// <param name="growerNumber">The grower number</param>
        /// <returns>List of cheques for the grower</returns>
        public async Task<List<Cheque>> GetChequesForGrowerAsync(decimal growerNumber)
        {
            var results = new List<Cheque>();

            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT * FROM Cheque 
                        WHERE GROWER = @GrowerNumber
                        ORDER BY CHEQDATE DESC";

                    var parameters = new { GrowerNumber = growerNumber };
                    
                    results = (await connection.QueryAsync<Cheque>(sql, parameters)).AsList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting cheques: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Gets a cheque by cheque number
        /// </summary>
        /// <param name="chequeNumber">The cheque number</param>
        /// <returns>The cheque if found, null otherwise</returns>
        public async Task<Cheque> GetChequeByNumberAsync(string chequeNumber)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM Cheque WHERE CHEQNUM = @ChequeNumber";
                    var parameters = new { ChequeNumber = chequeNumber };

                    return await connection.QueryFirstOrDefaultAsync<Cheque>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting cheque: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves a cheque to the database
        /// </summary>
        /// <param name="cheque">The cheque to save</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SaveChequeAsync(Cheque cheque)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    await connection.OpenAsync();

                    // Check if the cheque exists
                    string checkSql = "SELECT COUNT(*) FROM Cheque WHERE CHEQNUM = @ChequeNumber";
                    var checkParams = new { ChequeNumber = cheque.ChequeNumber };
                    bool chequeExists = await connection.ExecuteScalarAsync<int>(checkSql, checkParams) > 0;

                    if (chequeExists)
                    {
                        // Update existing cheque
                        string sql = @"
                            UPDATE Cheque SET 
                                GROWER = @GrowerNumber,
                                CHEQDATE = @ChequeDate,
                                AMOUNT = @Amount,
                                MEMO = @Memo,
                                STATUS = @Status
                            WHERE CHEQNUM = @ChequeNumber";

                        await connection.ExecuteAsync(sql, cheque);
                    }
                    else
                    {
                        // Insert new cheque
                        string sql = @"
                            INSERT INTO Cheque (
                                CHEQNUM, GROWER, CHEQDATE, AMOUNT, MEMO, STATUS
                            ) VALUES (
                                @ChequeNumber, @GrowerNumber, @ChequeDate, @Amount, @Memo, @Status
                            )";

                        await connection.ExecuteAsync(sql, cheque);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving cheque: {ex.Message}");
                return false;
            }
        }
    }
}
