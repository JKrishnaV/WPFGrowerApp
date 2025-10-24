using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for managing cross-batch consolidated payments
    /// </summary>
    public interface ICrossBatchPaymentService
    {
        // Note: GetConsolidatedPaymentForGrowerAsync method removed - consolidated payments replaced by payment distributions

        // Note: GenerateConsolidatedChequeAsync method removed - consolidated payments replaced by payment distributions

        /// <summary>
        /// Gets grower payments across multiple batches
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="batchIds">List of batch IDs</param>
        /// <returns>List of grower payments across batches</returns>
        Task<List<GrowerPaymentAcrossBatches>> GetGrowerPaymentsAcrossBatchesAsync(int growerId, List<int> batchIds);

        /// <summary>
        /// Validates if consolidation is possible for a grower and batches
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="batchIds">List of batch IDs</param>
        /// <returns>Validation result with details</returns>
        Task<ConsolidationValidationResult> ValidateConsolidationAsync(int growerId, List<int> batchIds);

        /// <summary>
        /// Gets all growers that appear in multiple batches
        /// </summary>
        /// <param name="batchIds">List of batch IDs to check</param>
        /// <returns>List of growers with their batch details</returns>
        Task<List<GrowerBatchDetails>> GetGrowersInMultipleBatchesAsync(List<int> batchIds);

        /// <summary>
        /// Gets batch breakdown for a consolidated cheque
        /// </summary>
        /// <param name="chequeId">The consolidated cheque ID</param>
        /// <param name="connection">Optional database connection</param>
        /// <param name="transaction">Optional database transaction</param>
        /// <returns>List of batch breakdowns</returns>
        Task<List<BatchBreakdown>> GetBatchBreakdownAsync(int chequeId, SqlConnection connection = null, SqlTransaction transaction = null);

        /// <summary>
        /// Updates batch status after consolidation
        /// </summary>
        /// <param name="batchIds">List of batch IDs</param>
        /// <param name="newStatus">New status for the batches</param>
        /// <param name="updatedBy">The user updating the status</param>
        /// <param name="connection">Optional database connection</param>
        /// <param name="transaction">Optional database transaction</param>
        /// <returns>True if successful</returns>
        Task<bool> UpdateBatchStatusAfterConsolidationAsync(List<int> batchIds, string newStatus, string updatedBy, SqlConnection connection = null, SqlTransaction transaction = null);

        /// <summary>
        /// Reverts consolidation and restores batch status
        /// </summary>
        /// <param name="consolidatedChequeId">The consolidated cheque ID to revert</param>
        /// <param name="revertedBy">The user reverting the consolidation</param>
        /// <returns>True if successful</returns>
        Task<bool> RevertConsolidationAsync(int consolidatedChequeId, string revertedBy);

        /// <summary>
        /// Gets consolidation history for a grower
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>List of consolidation records</returns>
        Task<List<ConsolidationHistory>> GetConsolidationHistoryAsync(int growerId, DateTime? startDate = null, DateTime? endDate = null);
    }
}
