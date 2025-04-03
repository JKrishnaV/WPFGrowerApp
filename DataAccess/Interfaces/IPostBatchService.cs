using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines operations related to managing posting batches (PostBat table).
    /// </summary>
    public interface IPostBatchService
    {
        /// <summary>
        /// Creates a new posting batch record.
        /// </summary>
        /// <param name="batchDate">The date the batch was created.</param>
        /// <param name="cutoffDate">The cutoff date for transactions included in the batch.</param>
        /// <param name="postType">The type of posting (e.g., "ADV 1", "ADV 2").</param>
        /// <returns>The newly created PostBatch object with its assigned ID.</returns>
        Task<PostBatch> CreatePostBatchAsync(DateTime batchDate, DateTime cutoffDate, string postType);

        /// <summary>
        /// Retrieves the next available Post Batch ID.
        /// </summary>
        /// <returns>The next unique Post Batch ID.</returns>
        Task<decimal> GetNextPostBatchIdAsync();

        /// <summary>
        /// Retrieves a PostBatch record by its ID.
        /// </summary>
        /// <param name="postBatchId">The ID of the batch to retrieve.</param>
        /// <returns>The PostBatch object or null if not found.</returns>
        Task<PostBatch> GetPostBatchByIdAsync(decimal postBatchId);

        // Add other methods as needed, e.g., updating or deleting batches if required by logic.
    }
}
