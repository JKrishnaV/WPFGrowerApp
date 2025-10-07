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
    Task<ImportBatch> StartImportBatchAsync(int depotId, string fileName);

        /// <summary>
        /// Processes a single receipt within the specified import batch.
        /// Includes validation, calculation, and saving.
        /// </summary>
        /// <returns>True if the single receipt was processed and saved successfully, false otherwise.</returns>
        Task<bool> ProcessSingleReceiptAsync(
            ImportBatch importBatch,
            Receipt receipt,
            CancellationToken cancellationToken = default);
        // Note: Progress reporting might need to be handled differently now, perhaps in the calling ViewModel loop.

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
