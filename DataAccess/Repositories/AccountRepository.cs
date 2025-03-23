using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task<List<Account>> GetAccountsForGrowerAsync(decimal growerNumber)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        SELECT AccountID, GrowerNumber, AccountNumber, AccountName, Balance, LastActivity
                        FROM Accounts
                        WHERE GrowerNumber = @GrowerNumber";

                    var accounts = await connection.QueryAsync<Account>(query, new { GrowerNumber = growerNumber });
                    return accounts.AsList();
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error retrieving accounts: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving accounts: {ex.Message}", ex);
            }
        }

        public async Task<Account> GetAccountByIdAsync(int accountId)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        SELECT AccountID, GrowerNumber, AccountNumber, AccountName, Balance, LastActivity
                        FROM Accounts
                        WHERE AccountID = @AccountID";

                    return await connection.QueryFirstOrDefaultAsync<Account>(query, new { AccountID = accountId });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error retrieving account: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving account: {ex.Message}", ex);
            }
        }

        public async Task AddAccountAsync(Account account)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        INSERT INTO Accounts (GrowerNumber, AccountNumber, AccountName, Balance, LastActivity)
                        VALUES (@GrowerNumber, @AccountNumber, @AccountName, @Balance, @LastActivity);
                        SELECT CAST(SCOPE_IDENTITY() as int)";

                    int accountId = await connection.QuerySingleAsync<int>(query, account);
                    account.AccountID = accountId;
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error adding account: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error adding account: {ex.Message}", ex);
            }
        }

        public async Task UpdateAccountAsync(Account account)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = @"
                        UPDATE Accounts
                        SET GrowerNumber = @GrowerNumber,
                            AccountNumber = @AccountNumber,
                            AccountName = @AccountName,
                            Balance = @Balance,
                            LastActivity = @LastActivity
                        WHERE AccountID = @AccountID";

                    await connection.ExecuteAsync(query, account);
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error updating account: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating account: {ex.Message}", ex);
            }
        }

        public async Task DeleteAccountAsync(int accountId)
        {
            try
            {
                using (IDbConnection connection = _connectionManager.CreateConnection())
                {
                    string query = "DELETE FROM Accounts WHERE AccountID = @AccountID";
                    await connection.ExecuteAsync(query, new { AccountID = accountId });
                }
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error deleting account: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting account: {ex.Message}", ex);
            }
        }
    }
}
