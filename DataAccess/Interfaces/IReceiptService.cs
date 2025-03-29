using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IReceiptService
    {
        Task<List<Receipt>> GetReceiptsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<Receipt> GetReceiptByNumberAsync(decimal receiptNumber);
        Task<Receipt> SaveReceiptAsync(Receipt receipt);
        Task<bool> DeleteReceiptAsync(decimal receiptNumber);
        Task<bool> VoidReceiptAsync(decimal receiptNumber, string reason);
        Task<List<Receipt>> GetReceiptsByGrowerAsync(decimal growerNumber, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Receipt>> GetReceiptsByImportBatchAsync(decimal impBatch);
        Task<decimal> GetNextReceiptNumberAsync();
        Task<bool> ValidateReceiptAsync(Receipt receipt);
        Task<decimal> CalculateNetWeightAsync(Receipt receipt);
        Task<decimal> GetPriceForReceiptAsync(Receipt receipt);
    }
}