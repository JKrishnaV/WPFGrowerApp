using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IImportBatchService
    {
    Task<ImportBatch> CreateImportBatchAsync(int depotId, string impFile);
        Task<ImportBatch> GetImportBatchAsync(decimal impBatch);
        Task<List<ImportBatch>> GetImportBatchesAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> UpdateImportBatchAsync(ImportBatch importBatch);
        Task<decimal> GetNextImportBatchNumberAsync();
        Task<bool> ValidateImportBatchAsync(ImportBatch importBatch);
        Task<bool> CloseImportBatchAsync(decimal impBatch);
        Task<bool> ReopenImportBatchAsync(decimal impBatch);
    }
}