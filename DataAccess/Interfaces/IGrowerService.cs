using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for grower data operations.
    /// Provides complete CRUD functionality and business operations for growers.
    /// </summary>
    public interface IGrowerService : IDatabaseService
    {
        // ======================================================================
        // CORE CRUD OPERATIONS
        // ======================================================================

        /// <summary>
        /// Gets a grower by their unique ID.
        /// </summary>
        /// <param name="growerId">The grower's unique identifier</param>
        /// <returns>The grower with all related data, or null if not found</returns>
        Task<Grower> GetGrowerByIdAsync(int growerId);

        /// <summary>
        /// Gets a grower by their grower number.
        /// </summary>
        /// <param name="growerNumber">The grower's unique number</param>
        /// <returns>The grower with all related data, or null if not found</returns>
        Task<Grower> GetGrowerByNumberAsync(string growerNumber);

        /// <summary>
        /// Gets all active growers from the database.
        /// </summary>
        /// <returns>List of all active growers with full details</returns>
        Task<List<Grower>> GetAllGrowersAsync();

        /// <summary>
        /// Creates a new grower in the database.
        /// </summary>
        /// <param name="grower">The grower to create</param>
        /// <returns>The ID of the newly created grower</returns>
        Task<int> CreateGrowerAsync(Grower grower);

        /// <summary>
        /// Updates an existing grower in the database.
        /// </summary>
        /// <param name="grower">The grower with updated data</param>
        /// <returns>True if the update was successful</returns>
        Task<bool> UpdateGrowerAsync(Grower grower);

        /// <summary>
        /// Saves a grower (creates new or updates existing).
        /// </summary>
        /// <param name="grower">The grower object to save</param>
        /// <returns>True if the save was successful</returns>
        Task<bool> SaveGrowerAsync(Grower grower);

        /// <summary>
        /// Soft deletes a grower by setting DeletedAt timestamp.
        /// </summary>
        /// <param name="growerId">The ID of the grower to delete</param>
        /// <returns>True if the deletion was successful</returns>
        Task<bool> DeleteGrowerAsync(int growerId);

        // ======================================================================
        // SEARCH & FILTER OPERATIONS
        // ======================================================================

        /// <summary>
        /// Searches growers by various criteria (name, number, city, etc.).
        /// </summary>
        /// <param name="searchTerm">The search term to match against</param>
        /// <returns>List of matching growers in search result format</returns>
        Task<List<GrowerSearchResult>> SearchGrowersAsync(string searchTerm);

        /// <summary>
        /// Gets all growers in search result format for list displays.
        /// </summary>
        /// <returns>List of all growers in search result format</returns>
        Task<List<GrowerSearchResult>> GetAllGrowersForListAsync();

        /// <summary>
        /// Gets growers by province.
        /// </summary>
        /// <param name="province">The province code to filter by</param>
        /// <returns>List of growers in the specified province</returns>
        Task<List<GrowerSearchResult>> GetGrowersByProvinceAsync(string province);

        /// <summary>
        /// Gets growers by payment group.
        /// </summary>
        /// <param name="paymentGroupId">The payment group ID to filter by</param>
        /// <returns>List of growers in the specified payment group</returns>
        Task<List<GrowerSearchResult>> GetGrowersByPaymentGroupAsync(int paymentGroupId);

        /// <summary>
        /// Gets growers by status (active, on hold, etc.).
        /// </summary>
        /// <param name="isActive">True for active growers, false for inactive</param>
        /// <param name="isOnHold">True for on-hold growers, false for not on hold</param>
        /// <returns>List of growers matching the status criteria</returns>
        Task<List<GrowerSearchResult>> GetGrowersByStatusAsync(bool? isActive = null, bool? isOnHold = null);

        // ======================================================================
        // VALIDATION OPERATIONS
        // ======================================================================

        /// <summary>
        /// Checks if a grower number is unique.
        /// </summary>
        /// <param name="growerNumber">The grower number to check</param>
        /// <param name="excludeGrowerId">Optional grower ID to exclude from the check (for updates)</param>
        /// <returns>True if the grower number is unique</returns>
        Task<bool> IsGrowerNumberUniqueAsync(string growerNumber, int? excludeGrowerId = null);

        /// <summary>
        /// Checks if a grower exists in the database.
        /// </summary>
        /// <param name="growerId">The grower ID to check</param>
        /// <returns>True if the grower exists</returns>
        Task<bool> GrowerExistsAsync(int growerId);

        /// <summary>
        /// Validates grower data before saving.
        /// </summary>
        /// <param name="grower">The grower to validate</param>
        /// <returns>Dictionary of validation errors (field name -> error message)</returns>
        Task<Dictionary<string, string>> ValidateGrowerAsync(Grower grower);

        // ======================================================================
        // STATISTICS & HISTORY OPERATIONS
        // ======================================================================

        /// <summary>
        /// Gets comprehensive statistics for a grower.
        /// </summary>
        /// <param name="growerId">The grower ID to get statistics for</param>
        /// <returns>Grower statistics including totals and averages</returns>
        Task<GrowerStatistics> GetGrowerStatisticsAsync(int growerId);

        /// <summary>
        /// Gets recent receipts for a grower.
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="count">Number of recent receipts to retrieve</param>
        /// <returns>List of recent receipts</returns>
        Task<List<Receipt>> GetGrowerRecentReceiptsAsync(int growerId, int count = 10);

        /// <summary>
        /// Gets recent payments for a grower.
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="count">Number of recent payments to retrieve</param>
        /// <returns>List of recent payments</returns>
        Task<List<Payment>> GetGrowerRecentPaymentsAsync(int growerId, int count = 10);

        /// <summary>
        /// Gets grower receipts within a date range.
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="fromDate">Start date (inclusive)</param>
        /// <param name="toDate">End date (inclusive)</param>
        /// <returns>List of receipts in the date range</returns>
        Task<List<Receipt>> GetGrowerReceiptsAsync(int growerId, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// Gets grower payments within a date range.
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="fromDate">Start date (inclusive)</param>
        /// <param name="toDate">End date (inclusive)</param>
        /// <returns>List of payments in the date range</returns>
        Task<List<Payment>> GetGrowerPaymentsAsync(int growerId, DateTime? fromDate, DateTime? toDate);

        // ======================================================================
        // DASHBOARD & REPORTING OPERATIONS
        // ======================================================================

        /// <summary>
        /// Gets the total count of all growers (excluding soft-deleted).
        /// </summary>
        /// <returns>Total number of growers</returns>
        Task<int> GetTotalGrowersCountAsync();

        /// <summary>
        /// Gets the count of active growers.
        /// </summary>
        /// <returns>Number of active growers</returns>
        Task<int> GetActiveGrowersCountAsync();

        /// <summary>
        /// Gets the count of growers on hold.
        /// </summary>
        /// <returns>Number of growers on hold</returns>
        Task<int> GetOnHoldGrowersCountAsync();

        /// <summary>
        /// Gets the count of inactive growers.
        /// </summary>
        /// <returns>Number of inactive growers</returns>
        Task<int> GetInactiveGrowersCountAsync();

        /// <summary>
        /// Gets unique provinces from all growers.
        /// </summary>
        /// <returns>List of unique province codes</returns>
        Task<List<string>> GetUniqueProvincesAsync();

        /// <summary>
        /// Gets grower counts by province.
        /// </summary>
        /// <returns>Dictionary of province code to grower count</returns>
        Task<Dictionary<string, int>> GetGrowerCountsByProvinceAsync();

        /// <summary>
        /// Gets grower counts by payment group.
        /// </summary>
        /// <returns>Dictionary of payment group ID to grower count</returns>
        Task<Dictionary<int, int>> GetGrowerCountsByPaymentGroupAsync();

        // ======================================================================
        // LOOKUP & REFERENCE DATA
        // ======================================================================

        /// <summary>
        /// Gets basic grower information for dropdowns and lookups.
        /// </summary>
        /// <returns>List of basic grower information</returns>
        Task<List<GrowerInfo>> GetAllGrowersBasicInfoAsync();

        /// <summary>
        /// Gets only growers that are on hold.
        /// </summary>
        /// <returns>List of on-hold growers</returns>
        Task<List<GrowerInfo>> GetOnHoldGrowersAsync();

        /// <summary>
        /// Gets only active growers for selection purposes.
        /// </summary>
        /// <returns>List of active growers</returns>
        Task<List<GrowerInfo>> GetActiveGrowersAsync();
    }
}