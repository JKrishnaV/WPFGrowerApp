using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for managing advance deductions
    /// </summary>
    public interface IAdvanceDeductionService
    {
        /// <summary>
        /// Applies advance deductions to a payment
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="paymentBatchId">The payment batch ID</param>
        /// <param name="paymentAmount">The payment amount before deductions</param>
        /// <param name="createdBy">The user applying the deductions</param>
        /// <returns>The remaining payment amount after deductions</returns>
        Task<decimal> ApplyAdvanceDeductionsAsync(int growerId, int paymentBatchId, decimal paymentAmount, string createdBy);

        /// <summary>
        /// Reverses advance deductions for an advance cheque
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="reason">The reason for reversal</param>
        /// <param name="reversedBy">The user reversing the deductions</param>
        /// <returns>True if successful</returns>
        Task<bool> ReverseAdvanceDeductionsAsync(int advanceChequeId, string reason, string reversedBy);

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
        /// Reverses a single advance deduction
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID</param>
        /// <param name="reason">The reason for reversal</param>
        /// <param name="reversedBy">The user reversing the deduction</param>
        /// <returns>True if successful</returns>
        Task<bool> ReverseAdvanceDeductionAsync(int advanceChequeId, string reason, string reversedBy);
    }
}
