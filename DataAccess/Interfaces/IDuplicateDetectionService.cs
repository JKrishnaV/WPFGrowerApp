using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IDuplicateDetectionService
    {
        /// <summary>
        /// Analyzes a list of receipts to identify duplicates, new receipts, and soft-deleted receipts
        /// </summary>
        Task<ImportAnalysisResult> AnalyzePartialDuplicatesAsync(List<Receipt> newReceipts);

        /// <summary>
        /// Analyzes batch conflicts when the same batch number is used in different files
        /// </summary>
        Task<BatchConflictAnalysis> AnalyzeBatchConflictsAsync(string fileName, string batchNumber, List<Receipt> receipts);

        /// <summary>
        /// Presents conflict resolution options to the user
        /// </summary>
        Task<ConflictResolutionResult> PresentBatchConflictOptionsAsync(BatchConflictAnalysis analysis);

        /// <summary>
        /// Imports receipts with specified duplicate handling strategy
        /// </summary>
        Task<SelectiveImportResult> ImportWithDuplicateHandlingAsync(
            List<Receipt> allReceipts, 
            DuplicateHandlingStrategy strategy,
            int importBatchId);
    }
}
