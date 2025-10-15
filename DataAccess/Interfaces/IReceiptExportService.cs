using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for receipt export operations
    /// </summary>
    public interface IReceiptExportService
    {
        /// <summary>
        /// Export a single receipt to PDF
        /// </summary>
        /// <param name="receiptId">The receipt ID to export</param>
        /// <param name="filePath">The output file path</param>
        /// <returns>True if successful</returns>
        Task<bool> ExportReceiptToPdfAsync(int receiptId, string filePath);

        /// <summary>
        /// Export a single receipt to Excel
        /// </summary>
        /// <param name="receiptId">The receipt ID to export</param>
        /// <param name="filePath">The output file path</param>
        /// <returns>True if successful</returns>
        Task<bool> ExportReceiptToExcelAsync(int receiptId, string filePath);

        /// <summary>
        /// Export multiple receipts to Excel
        /// </summary>
        /// <param name="receiptIds">The receipt IDs to export</param>
        /// <param name="filePath">The output file path</param>
        /// <returns>True if successful</returns>
        Task<bool> ExportMultipleReceiptsToExcelAsync(List<int> receiptIds, string filePath);

        /// <summary>
        /// Generate a print preview for a receipt
        /// </summary>
        /// <param name="receiptId">The receipt ID</param>
        /// <returns>Byte array of the preview document</returns>
        Task<byte[]> GenerateReceiptPrintPreviewAsync(int receiptId);

        /// <summary>
        /// Export receipt analytics to PDF
        /// </summary>
        /// <param name="startDate">Start date for analytics</param>
        /// <param name="endDate">End date for analytics</param>
        /// <param name="filePath">The output file path</param>
        /// <returns>True if successful</returns>
        Task<bool> ExportReceiptAnalyticsToPdfAsync(DateTime? startDate, DateTime? endDate, string filePath);

        /// <summary>
        /// Export receipt analytics to Excel
        /// </summary>
        /// <param name="startDate">Start date for analytics</param>
        /// <param name="endDate">End date for analytics</param>
        /// <param name="filePath">The output file path</param>
        /// <returns>True if successful</returns>
        Task<bool> ExportReceiptAnalyticsToExcelAsync(DateTime? startDate, DateTime? endDate, string filePath);

        /// <summary>
        /// Generate a receipt summary report
        /// </summary>
        /// <param name="receiptIds">The receipt IDs to include</param>
        /// <param name="filePath">The output file path</param>
        /// <returns>True if successful</returns>
        Task<bool> GenerateReceiptSummaryReportAsync(List<int> receiptIds, string filePath);

        /// <summary>
        /// Export grower receipt summary
        /// </summary>
        /// <param name="growerId">The grower ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="filePath">The output file path</param>
        /// <returns>True if successful</returns>
        Task<bool> ExportGrowerReceiptSummaryAsync(int growerId, DateTime? startDate, DateTime? endDate, string filePath);
    }
}
