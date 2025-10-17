using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for managing electronic payment operations.
    /// Handles electronic payment file generation, processing, and tracking.
    /// </summary>
    public interface IElectronicPaymentService : IDatabaseService
    {
        /// <summary>
        /// Generates a NACHA formatted file for the specified electronic payments.
        /// </summary>
        /// <param name="electronicPaymentIds">List of electronic payment IDs to include in the file.</param>
        /// <returns>Byte array containing the NACHA formatted file.</returns>
        Task<byte[]> GenerateNachaFileAsync(List<int> electronicPaymentIds);

        /// <summary>
        /// Saves an electronic payment file to the database.
        /// </summary>
        /// <param name="file">The electronic payment file to save.</param>
        /// <returns>The saved file with assigned ID.</returns>
        Task<ElectronicPaymentFile> SaveElectronicPaymentFileAsync(ElectronicPaymentFile file);

        /// <summary>
        /// Gets all pending electronic payments that need to be processed.
        /// </summary>
        /// <returns>List of pending electronic payments.</returns>
        Task<List<ElectronicPayment>> GetPendingElectronicPaymentsAsync();

        /// <summary>
        /// Marks electronic payments as processed.
        /// </summary>
        /// <param name="paymentIds">List of payment IDs to mark as processed.</param>
        /// <param name="processedBy">The user processing the payments.</param>
        /// <returns>True if the update was successful.</returns>
        Task<bool> MarkPaymentsAsProcessedAsync(List<int> paymentIds, string processedBy);

        /// <summary>
        /// Gets all generated electronic payment files.
        /// </summary>
        /// <returns>List of electronic payment files.</returns>
        Task<List<ElectronicPaymentFile>> GetElectronicPaymentFilesAsync();

        /// <summary>
        /// Gets a specific electronic payment file by ID.
        /// </summary>
        /// <param name="fileId">The ID of the file to retrieve.</param>
        /// <returns>The electronic payment file.</returns>
        Task<ElectronicPaymentFile> GetElectronicPaymentFileAsync(int fileId);

        /// <summary>
        /// Updates the status of an electronic payment file.
        /// </summary>
        /// <param name="fileId">The ID of the file to update.</param>
        /// <param name="newStatus">The new status to set.</param>
        /// <param name="processedBy">The user updating the status.</param>
        /// <returns>True if the update was successful.</returns>
        Task<bool> UpdateFileStatusAsync(int fileId, string newStatus, string processedBy);

        /// <summary>
        /// Validates electronic payment data before file generation.
        /// </summary>
        /// <param name="paymentIds">List of payment IDs to validate.</param>
        /// <returns>List of validation errors, empty if valid.</returns>
        Task<List<string>> ValidateElectronicPaymentsAsync(List<int> paymentIds);

        /// <summary>
        /// Gets electronic payment statistics for reporting.
        /// </summary>
        /// <returns>Summary of electronic payment statuses and amounts.</returns>
        Task<Dictionary<string, object>> GetElectronicPaymentStatisticsAsync();

        /// <summary>
        /// Gets all electronic payments.
        /// </summary>
        /// <returns>List of all electronic payments.</returns>
        Task<List<ElectronicPayment>> GetAllElectronicPaymentsAsync();
    }
}
