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
    public class AccountRepository
    {
        private readonly DapperConnectionManager _connectionManager;

        public AccountRepository(DapperConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        /// <summary>
        /// Gets accounts for a specific grower
        /// </summary>
        /// <param name="growerNumber">The grower number</param>
        /// <returns>List of accounts for the grower</returns>
        public async Task<List<Account>> GetAccountsForGrowerAsync(decimal growerNumber)
        {
            var results = new List<Account>();

            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = @"
                        SELECT * FROM Account 
                        WHERE GROWER = @GrowerNumber
                        ORDER BY ACCTNUM";

                    var parameters = new { GrowerNumber = growerNumber };
                    
                    results = (await connection.QueryAsync<Account>(sql, parameters)).AsList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting accounts: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Gets an account by account number
        /// </summary>
        /// <param name="accountNumber">The account number</param>
        /// <returns>The account if found, null otherwise</returns>
        public async Task<Account> GetAccountByNumberAsync(string accountNumber)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    await connection.OpenAsync();

                    string sql = "SELECT * FROM Account WHERE ACCTNUM = @AccountNumber";
                    var parameters = new { AccountNumber = accountNumber };

                    return await connection.QueryFirstOrDefaultAsync<Account>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting account: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves an account to the database
        /// </summary>
        /// <param name="account">The account to save</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SaveAccountAsync(Account account)
        {
            try
            {
                using (var connection = _connectionManager.CreateConnection())
                {
                    await connection.OpenAsync();

                    // Check if the account exists
                    string checkSql = "SELECT COUNT(*) FROM Account WHERE ACCTNUM = @AccountNumber";
                    var checkParams = new { AccountNumber = account.AccountNumber };
                    bool accountExists = await connection.ExecuteScalarAsync<int>(checkSql, checkParams) > 0;

                    if (accountExists)
                    {
                        // Update existing account
                        string sql = @"
                            UPDATE Account SET 
                                GROWER = @GrowerNumber,
                                ACCTTYPE = @AccountType,
                                ACCTDESC = @Description,
                                BALANCE = @Balance,
                                LASTDATE = @LastDate
                            WHERE ACCTNUM = @AccountNumber";

                        await connection.ExecuteAsync(sql, account);
                    }
                    else
                    {
                        // Insert new account
                        string sql = @"
                            INSERT INTO Account (
                                ACCTNUM, GROWER, ACCTTYPE, ACCTDESC, BALANCE, LASTDATE
                            ) VALUES (
                                @AccountNumber, @GrowerNumber, @AccountType, @Description, @Balance, @LastDate
                            )";

                        await connection.ExecuteAsync(sql, account);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving account: {ex.Message}");
                return false;
            }
        }
    }
}
