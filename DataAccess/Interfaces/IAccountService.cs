using System; // Added for DateTime
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IAccountService : IDatabaseService
    {
        Task<List<Account>> GetAllAccountsAsync();
        Task<Account> GetAccountByNumberAsync(decimal number);
        Task<bool> SaveAccountAsync(Account account);
        Task<List<Account>> GetAccountsByYearAsync(decimal year);

        /// <summary>
        /// Creates multiple account ledger entries based on calculated payment details for a grower within a batch.
        /// </summary>
        /// <param name="paymentEntries">A list of Account objects representing the calculated payments, deductions, premiums, etc.</param>
        /// <returns>True if all entries were saved successfully, false otherwise.</returns>
        Task<bool> CreatePaymentAccountEntriesAsync(List<Account> paymentEntries);

        /// <summary>
        /// Retrieves unpaid account entries for a specific grower, currency, and date range, suitable for cheque generation.
        /// </summary>
        /// <param name="growerNumber">The grower number.</param>
        /// <param name="currency">The currency code (e.g., "C" or "U").</param>
        /// <param name="cropYear">The crop year.</param>
        /// <param name="cutoffDate">Include entries up to this date.</param>
        /// <param name="chequeType">The type of cheque being generated (e.g., "W" for Weekly).</param>
        /// <returns>A list of Account objects eligible for payment.</returns>
        Task<List<Account>> GetPayableAccountEntriesAsync(decimal growerNumber, string currency, int cropYear, DateTime cutoffDate, string chequeType);

        /// <summary>
        /// Updates account entries with cheque information after a cheque run.
        /// </summary>
        /// <param name="growerNumber">The grower number.</param>
        /// <param name="currency">The currency code.</param>
        /// <param name="cropYear">The crop year.</param>
        /// <param name="cutoffDate">The cutoff date used for the cheque run.</param>
        /// <param name="chequeType">The type of cheque generated.</param>
        /// <param name="chequeSeries">The cheque series.</param>
        /// <param name="chequeNumber">The cheque number.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateAccountEntriesWithChequeInfoAsync(decimal growerNumber, string currency, int cropYear, DateTime cutoffDate, string chequeType, string chequeSeries, decimal chequeNumber);

        /// <summary>
        /// Reverts temporary cheque info if a cheque run is cancelled before finalization.
        /// </summary>
        /// <param name="currency">The currency code.</param>
        /// <param name="tempChequeSeries">The temporary cheque series used.</param>
        /// <param name="tempChequeNumberStart">The starting temporary cheque number used.</param>
        /// <returns>True if successful.</returns>
        Task<bool> RevertTemporaryChequeInfoAsync(string currency, string tempChequeSeries, decimal tempChequeNumberStart);

    }
}
