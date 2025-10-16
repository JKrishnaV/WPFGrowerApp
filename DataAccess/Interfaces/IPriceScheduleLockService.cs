using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IPriceScheduleLockService
    {
        /// <summary>
        /// Creates a new price schedule lock.
        /// </summary>
        /// <param name="lockEntry">The price schedule lock to create.</param>
        /// <returns>The created lock with ID.</returns>
        Task<PriceScheduleLock> CreatePriceScheduleLockAsync(PriceScheduleLock lockEntry);

        /// <summary>
        /// Creates a price schedule lock within a transaction.
        /// </summary>
        /// <param name="lockEntry">The price schedule lock to create.</param>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Database transaction.</param>
        /// <returns>The created lock with ID.</returns>
        Task<PriceScheduleLock> CreatePriceScheduleLockAsync(PriceScheduleLock lockEntry, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction);

        /// <summary>
        /// Gets all locks for a specific payment batch.
        /// </summary>
        /// <param name="paymentBatchId">The payment batch ID.</param>
        /// <returns>List of price schedule locks.</returns>
        Task<List<PriceScheduleLock>> GetLocksByPaymentBatchAsync(int paymentBatchId);

        /// <summary>
        /// Gets locks for a specific price schedule and payment type.
        /// </summary>
        /// <param name="priceScheduleId">The price schedule ID.</param>
        /// <param name="paymentTypeId">The payment type ID.</param>
        /// <returns>List of price schedule locks.</returns>
        Task<List<PriceScheduleLock>> GetLocksByScheduleAndTypeAsync(int priceScheduleId, int paymentTypeId);

        /// <summary>
        /// Checks if a price schedule is locked for a specific payment type.
        /// </summary>
        /// <param name="priceScheduleId">The price schedule ID.</param>
        /// <param name="paymentTypeId">The payment type ID.</param>
        /// <returns>True if the schedule is locked.</returns>
        Task<bool> IsPriceScheduleLockedAsync(int priceScheduleId, int paymentTypeId);

        /// <summary>
        /// Gets all active locks within a date range.
        /// </summary>
        /// <param name="startDate">Start date.</param>
        /// <param name="endDate">End date.</param>
        /// <returns>List of price schedule locks.</returns>
        Task<List<PriceScheduleLock>> GetLocksByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Updates an existing price schedule lock.
        /// </summary>
        /// <param name="lockEntry">The lock to update.</param>
        /// <returns>True if update was successful.</returns>
        Task<bool> UpdatePriceScheduleLockAsync(PriceScheduleLock lockEntry);

        /// <summary>
        /// Soft deletes a price schedule lock.
        /// </summary>
        /// <param name="lockId">The lock ID to delete.</param>
        /// <param name="deletedBy">User who deleted the lock.</param>
        /// <returns>True if deletion was successful.</returns>
        Task<bool> DeletePriceScheduleLockAsync(int lockId, string deletedBy);

        /// <summary>
        /// Creates multiple locks for a payment batch.
        /// </summary>
        /// <param name="locks">List of locks to create.</param>
        /// <param name="connection">Database connection.</param>
        /// <param name="transaction">Database transaction.</param>
        /// <returns>Number of locks created.</returns>
        Task<int> CreatePaymentBatchLocksAsync(List<PriceScheduleLock> locks, Microsoft.Data.SqlClient.SqlConnection connection, Microsoft.Data.SqlClient.SqlTransaction transaction);

        /// <summary>
        /// Removes locks for a payment batch (soft delete).
        /// </summary>
        /// <param name="paymentBatchId">The payment batch ID.</param>
        /// <param name="deletedBy">User who removed the locks.</param>
        /// <returns>Number of locks removed.</returns>
        Task<int> RemovePaymentBatchLocksAsync(int paymentBatchId, string deletedBy);
    }
}
