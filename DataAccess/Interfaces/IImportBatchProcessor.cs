using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IImportBatchProcessor
    {
        /// <summary>
        /// Starts a new import batch process
        /// </summary>
        Task<ImportBatch> StartImportBatchAsync(string depot, string fileName);

        /// <summary>
        /// Processes a collection of receipts within the specified import batch
        /// </summary>
        Task<(bool Success, List<string> Errors)> ProcessReceiptsAsync(
            ImportBatch importBatch,
            IEnumerable<Receipt> receipts,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the import batch statistics
        /// </summary>
        Task UpdateBatchStatisticsAsync(ImportBatch importBatch);

        /// <summary>
        /// Finalizes the import batch
        /// </summary>
        Task<bool> FinalizeBatchAsync(ImportBatch importBatch);

        /// <summary>
        /// Rolls back an import batch in case of failure
        /// </summary>
        Task RollbackBatchAsync(ImportBatch importBatch);

        /// <summary>
        /// Gets the current status of an import batch
        /// </summary>
        Task<(int TotalReceipts, int ProcessedReceipts, int ErrorCount)> GetBatchStatusAsync(ImportBatch importBatch);
    }
} 