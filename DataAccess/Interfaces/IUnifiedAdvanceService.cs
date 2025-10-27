using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Unified service interface for managing advance cheques and their deductions
    /// Combines functionality from both IAdvanceChequeService and IAdvanceDeductionService
    /// </summary>
    public interface IUnifiedAdvanceService
    {
        #region Advance Cheque Management

        /// <summary>
        /// Creates a new advance cheque for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="amount">The advance amount</param>
        /// <param name="reason">The reason for the advance</param>
        /// <param name="createdBy">The user creating the advance</param>
        /// <returns>The created advance cheque</returns>
        Task<AdvanceCheque> CreateAdvanceChequeAsync(int growerId, decimal amount, string reason, string createdBy);

        /// <summary>
        /// Gets all outstanding advance cheques for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <returns>List of outstanding advance cheques</returns>
        Task<List<AdvanceCheque>> GetOutstandingAdvancesAsync(int growerId);

        /// <summary>
        /// Calculates the total outstanding advance amount for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <returns>Total outstanding advance amount</returns>
        Task<decimal> CalculateTotalOutstandingAdvancesAsync(int growerId);

        /// <summary>
        /// Gets an advance cheque by ID
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <returns>The advance cheque or null if not found</returns>
        Task<AdvanceCheque> GetAdvanceChequeByIdAsync(int advanceChequeId);

        /// <summary>
        /// Gets all advance cheques with optional status filter
        /// </summary>
        /// <param name="status">Optional status filter (Active, Deducted, Cancelled)</param>
        /// <returns>List of advance cheques</returns>
        Task<List<AdvanceCheque>> GetAllAdvanceChequesAsync(string status = null);

        /// <summary>
        /// Gets advance cheques for a specific grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of advance cheques for the grower</returns>
        Task<List<AdvanceCheque>> GetAdvanceChequesByGrowerAsync(int growerId, string status = null);

        /// <summary>
        /// Gets advance cheques within a date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="status">Optional status filter</param>
        /// <returns>List of advance cheques in the date range</returns>
        Task<List<AdvanceCheque>> GetAdvanceChequesByDateRangeAsync(DateTime startDate, DateTime endDate, string status = null);

        /// <summary>
        /// Updates an advance cheque
        /// </summary>
        /// <param name="advanceCheque">The advance cheque to update</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateAdvanceChequeAsync(AdvanceCheque advanceCheque);

        /// <summary>
        /// Checks if a grower has any outstanding advances
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <returns>True if grower has outstanding advances</returns>
        Task<bool> HasOutstandingAdvancesAsync(int growerId);

        #endregion

        #region Advance Deduction Management

        /// <summary>
        /// Applies advance deductions to a payment automatically
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="paymentBatchId">The payment batch ID</param>
        /// <param name="paymentAmount">The payment amount before deductions</param>
        /// <param name="createdBy">The user applying the deductions</param>
        /// <returns>DeductionResult with details of applied deductions</returns>
        Task<DeductionResult> ApplyAdvanceDeductionsAsync(int growerId, int paymentBatchId, decimal paymentAmount, string createdBy);

        /// <summary>
        /// Applies advance deductions to a payment within an existing transaction
        /// Use this overload when you need to ensure atomicity with other database operations
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="paymentBatchId">The payment batch ID</param>
        /// <param name="paymentAmount">The payment amount before deductions</param>
        /// <param name="createdBy">The user applying the deductions</param>
        /// <param name="connection">Existing database connection</param>
        /// <param name="transaction">Existing database transaction</param>
        /// <returns>DeductionResult with details of applied deductions</returns>
        Task<DeductionResult> ApplyAdvanceDeductionsAsync(int growerId, int paymentBatchId, decimal paymentAmount, string createdBy, SqlConnection connection, SqlTransaction transaction);

        /// <summary>
        /// Applies manual deduction with user-specified amount
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="chequeId">The cheque ID (can be null for fully absorbed payments)</param>
        /// <param name="deductionAmount">The amount to deduct</param>
        /// <param name="paymentBatchId">The payment batch ID</param>
        /// <param name="createdBy">The user applying the deduction</param>
        /// <returns>DeductionResult with details</returns>
        Task<DeductionResult> ApplyManualDeductionAsync(int advanceChequeId, int? chequeId, decimal deductionAmount, int paymentBatchId, string createdBy);

        /// <summary>
        /// Calculates suggested deductions without applying them
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="paymentAmount">The payment amount</param>
        /// <returns>List of suggested deductions</returns>
        Task<List<AdvanceDeduction>> CalculateSuggestedDeductionsAsync(int growerId, decimal paymentAmount);

        /// <summary>
        /// Gets the deduction history for an advance cheque
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <returns>List of deduction records</returns>
        Task<List<AdvanceDeduction>> GetDeductionHistoryAsync(int advanceChequeId);

        /// <summary>
        /// Gets all deductions for a payment batch
        /// </summary>
        /// <param name="paymentBatchId">The payment batch ID</param>
        /// <returns>List of deduction records</returns>
        Task<List<AdvanceDeduction>> GetDeductionsByBatchAsync(int paymentBatchId);

        /// <summary>
        /// Gets total deductions applied to a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>Total deduction amount</returns>
        Task<decimal> GetTotalDeductionsAsync(int growerId, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Creates a deduction record
        /// </summary>
        /// <param name="deduction">The deduction to create</param>
        /// <returns>True if successful</returns>
        Task<bool> CreateDeductionAsync(AdvanceDeduction deduction);

        /// <summary>
        /// Updates a deduction record
        /// </summary>
        /// <param name="deduction">The deduction to update</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateDeductionAsync(AdvanceDeduction deduction);

        /// <summary>
        /// Deletes a deduction record
        /// </summary>
        /// <param name="deductionId">The deduction ID</param>
        /// <param name="deletedBy">The user deleting the deduction</param>
        /// <returns>True if successful</returns>
        Task<bool> DeleteDeductionAsync(int deductionId, string deletedBy);

        /// <summary>
        /// Gets deduction history for a specific cheque ID
        /// </summary>
        /// <param name="chequeId">The cheque ID</param>
        /// <returns>List of deduction records</returns>
        Task<List<AdvanceDeduction>> GetDeductionHistoryByChequeIdAsync(int chequeId);

        /// <summary>
        /// Gets deductions by cheque ID for voiding
        /// </summary>
        /// <param name="chequeId">The cheque ID</param>
        /// <returns>List of deduction records</returns>
        Task<List<AdvanceDeduction>> GetDeductionsByChequeIdAsync(int chequeId);

        /// <summary>
        /// Validates a deduction amount against available advance balance
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="amount">The amount to validate</param>
        /// <returns>True if valid</returns>
        Task<bool> ValidateDeductionAsync(int advanceChequeId, decimal amount);

        /// <summary>
        /// Gets advance deduction summary for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <returns>Advance deduction summary</returns>
        Task<AdvanceDeductionSummary> GetAdvanceDeductionSummaryAsync(int growerId);

        #endregion

        #region Voiding and Reversal

        /// <summary>
        /// Voids a specific deduction record (soft-delete)
        /// </summary>
        /// <param name="deductionId">The deduction ID</param>
        /// <param name="reason">The reason for voiding</param>
        /// <param name="voidedBy">The user voiding the deduction</param>
        /// <returns>True if successful</returns>
        Task<bool> VoidDeductionAsync(int deductionId, string reason, string voidedBy);

        /// <summary>
        /// Reverses advance deductions for an advance cheque
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="reason">The reason for reversal</param>
        /// <param name="reversedBy">The user reversing the deductions</param>
        /// <returns>True if successful</returns>
        Task<bool> ReverseAdvanceDeductionsAsync(int advanceChequeId, string reason, string reversedBy);

        /// <summary>
        /// Reverses a single advance deduction
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="reason">The reason for reversal</param>
        /// <param name="reversedBy">The user reversing the deduction</param>
        /// <returns>True if successful</returns>
        Task<bool> ReverseAdvanceDeductionAsync(int advanceChequeId, string reason, string reversedBy);

        /// <summary>
        /// Restores advance amounts after voiding deductions
        /// </summary>
        /// <param name="deductions">The deductions to restore</param>
        /// <returns>True if successful</returns>
        Task<bool> RestoreAdvanceAmountsAsync(List<AdvanceDeduction> deductions);

        /// <summary>
        /// Cancels an advance cheque
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="reason">The reason for cancellation</param>
        /// <param name="cancelledBy">The user cancelling the advance</param>
        /// <returns>True if successful</returns>
        Task<bool> CancelAdvanceChequeAsync(int advanceChequeId, string reason, string cancelledBy);

        /// <summary>
        /// Void an advance cheque (Generated/Printed -> Voided)
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="voidedBy">The user voiding the cheque</param>
        /// <param name="voidedReason">The reason for voiding</param>
        /// <returns>True if successful</returns>
        Task<bool> VoidAdvanceChequeAsync(int advanceChequeId, string voidedBy, string voidedReason);

        #endregion

        #region Workflow Management

        /// <summary>
        /// Print an advance cheque (Generated -> Printed)
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="printedBy">The user printing the cheque</param>
        /// <returns>True if successful</returns>
        Task<bool> PrintAdvanceChequeAsync(int advanceChequeId, string printedBy);

        /// <summary>
        /// Deliver an advance cheque (Printed -> Delivered)
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="deliveredBy">The user delivering the cheque</param>
        /// <param name="deliveryMethod">The delivery method</param>
        /// <returns>True if successful</returns>
        Task<bool> DeliverAdvanceChequeAsync(int advanceChequeId, string deliveredBy, string deliveryMethod);

        #endregion
    }
}
