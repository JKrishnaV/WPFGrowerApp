using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IGrowerAccountService
    {
        /// <summary>
        /// Creates a new grower account entry.
        /// </summary>
        /// <param name="account">The grower account to create.</param>
        /// <returns>The created account with ID.</returns>
        Task<GrowerAccount> CreateGrowerAccountAsync(GrowerAccount account);

        /// <summary>
        /// Creates a grower account entry within a transaction.
        /// </summary>
        /// <param name="account">The grower account to create.</param>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Database transaction.</param>
        /// <returns>The created account with ID.</returns>
        Task<GrowerAccount> CreateGrowerAccountAsync(GrowerAccount account, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction);

        /// <summary>
        /// Gets all account entries for a specific grower.
        /// </summary>
        /// <param name="growerId">The grower ID.</param>
        /// <returns>List of account entries.</returns>
        Task<List<GrowerAccount>> GetGrowerAccountsAsync(int growerId);

        /// <summary>
        /// Gets all account entries for a specific payment batch.
        /// </summary>
        /// <param name="paymentBatchId">The payment batch ID.</param>
        /// <returns>List of account entries.</returns>
        Task<List<GrowerAccount>> GetAccountsByPaymentBatchAsync(int paymentBatchId);

        /// <summary>
        /// Gets account entries within a date range.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>List of account entries.</returns>
        Task<List<GrowerAccount>> GetAccountsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets the current balance for a grower.
        /// </summary>
        /// <param name="growerId">The grower ID.</param>
        /// <returns>The current balance (credits - debits).</returns>
        Task<decimal> GetGrowerBalanceAsync(int growerId);

        /// <summary>
        /// Updates an existing grower account entry.
        /// </summary>
        /// <param name="account">The account to update.</param>
        /// <returns>True if update was successful.</returns>
        Task<bool> UpdateGrowerAccountAsync(GrowerAccount account);

        /// <summary>
        /// Soft deletes a grower account entry.
        /// </summary>
        /// <param name="accountId">The account ID to delete.</param>
        /// <param name="deletedBy">User who deleted the entry.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> DeleteGrowerAccountAsync(int accountId, string deletedBy);

        /// <summary>
        /// Creates multiple account entries for a payment batch.
        /// </summary>
        /// <param name="accounts">List of accounts to create.</param>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Database transaction.</param>
        /// <returns>Number of accounts created.</returns>
        Task<int> CreatePaymentBatchAccountsAsync(List<GrowerAccount> accounts, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction);
    }
}
