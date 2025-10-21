using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IImportBatchService
    {
        Task<ImportBatch> CreateImportBatchAsync(int depotId, string impFile, string? originalBatchNumber = null, bool isFromMultiBatchFile = false, int? batchGroupId = null);
        Task<ImportBatch> CreateImportBatchWithConflictResolutionAsync(int depotId, string impFile, string? originalBatchNumber = null, bool isFromMultiBatchFile = false, int? batchGroupId = null);
        Task<List<ImportBatch>> CreateMultipleImportBatchesAsync(int depotId, string fileName, Dictionary<string, List<Receipt>> batchGroups);
        Task<ImportBatch> GetImportBatchAsync(string batchNumber);
        Task<List<ImportBatch>> GetImportBatchesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> UpdateImportBatchAsync(ImportBatch importBatch);
        Task<string> GetNextImportBatchNumberAsync();
        Task<int> GetNextBatchGroupIdAsync();
        Task<bool> ValidateImportBatchAsync(ImportBatch importBatch);
        Task<bool> CloseImportBatchAsync(string batchNumber);
        Task<bool> ReopenImportBatchAsync(string batchNumber);
        Task<List<ImportBatch>> GetBatchesByGroupIdAsync(int batchGroupId);
        Task<bool> DeleteImportBatchAsync(string batchNumber);
    }
}