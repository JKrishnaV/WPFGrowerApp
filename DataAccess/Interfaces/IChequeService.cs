using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IChequeService : IDatabaseService
    {
        Task<List<Cheque>> GetAllChequesAsync();
        Task<Cheque> GetChequeBySeriesAndNumberAsync(string series, decimal chequeNumber);
        Task<List<Cheque>> GetChequesByGrowerNumberAsync(decimal growerNumber);
        Task<List<Cheque>> GetChequesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<bool> SaveChequeAsync(Cheque cheque); // Used for manual cheque entry? Keep for now.
        Task<bool> VoidChequeAsync(string series, decimal chequeNumber);

        /// <summary>
        /// Gets the next available cheque number for a given series.
        /// </summary>
        /// <param name="series">The cheque series.</param>
        /// <param name="isEft">Indicates if this is for an EFT series (which might have separate numbering).</param>
        /// <returns>The next available cheque number.</returns>
        Task<decimal> GetNextChequeNumberAsync(string series, bool isEft = false);

        /// <summary>
        /// Creates multiple cheque records based on aggregated payment amounts.
        /// </summary>
        /// <param name="chequesToCreate">A list of Cheque objects to be inserted.</param>
        /// <returns>True if all cheques were saved successfully, false otherwise.</returns>
        Task<bool> CreateChequesAsync(List<Cheque> chequesToCreate);

        /// <summary>
        /// Retrieves cheques created within a specific temporary assignment range (used for printing/registers).
        /// </summary>
        /// <param name="currency">The currency code.</param>
        /// <param name="tempChequeSeries">The temporary cheque series used.</param>
        /// <param name="tempChequeNumberStart">The starting temporary cheque number used.</param>
        /// <returns>A list of Cheque objects.</returns>
        Task<List<Cheque>> GetTemporaryChequesAsync(string currency, string tempChequeSeries, decimal tempChequeNumberStart);

    }
}
