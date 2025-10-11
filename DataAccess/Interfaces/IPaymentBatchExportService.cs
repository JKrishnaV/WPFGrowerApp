using System.Threading.Tasks;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service for exporting payment batch data to various formats
    /// </summary>
    public interface IPaymentBatchExportService
    {
        /// <summary>
        /// Export grower payments to Excel
        /// </summary>
        /// <param name="batchId">Payment batch ID</param>
        /// <param name="filePath">Target file path for Excel file</param>
        /// <returns>True if export successful</returns>
        Task<bool> ExportGrowerPaymentsToExcelAsync(int batchId, string filePath);
        
        /// <summary>
        /// Export receipt allocations to Excel
        /// </summary>
        /// <param name="batchId">Payment batch ID</param>
        /// <param name="filePath">Target file path for Excel file</param>
        /// <returns>True if export successful</returns>
        Task<bool> ExportReceiptAllocationsToExcelAsync(int batchId, string filePath);
        
        /// <summary>
        /// Export cheques to Excel
        /// </summary>
        /// <param name="batchId">Payment batch ID</param>
        /// <param name="filePath">Target file path for Excel file</param>
        /// <returns>True if export successful</returns>
        Task<bool> ExportChequesToExcelAsync(int batchId, string filePath);
        
        /// <summary>
        /// Export complete batch summary to PDF
        /// </summary>
        /// <param name="batchId">Payment batch ID</param>
        /// <param name="filePath">Target file path for PDF file</param>
        /// <returns>True if export successful</returns>
        Task<bool> ExportBatchSummaryToPdfAsync(int batchId, string filePath);
        
        /// <summary>
        /// Export all batch data to Excel (multi-sheet workbook)
        /// </summary>
        /// <param name="batchId">Payment batch ID</param>
        /// <param name="filePath">Target file path for Excel file</param>
        /// <returns>True if export successful</returns>
        Task<bool> ExportCompleteBatchToExcelAsync(int batchId, string filePath);
    }
}

