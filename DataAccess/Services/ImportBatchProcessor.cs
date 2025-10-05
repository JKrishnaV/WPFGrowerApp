using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Exceptions;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class ImportBatchProcessor : IImportBatchProcessor
    {
        private readonly IImportBatchService _importBatchService;
        private readonly IReceiptService _receiptService;
        private readonly IFileImportService _fileImportService;
        private readonly ValidationService _validationService;

        public ImportBatchProcessor(
            IImportBatchService importBatchService,
            IReceiptService receiptService,
            IFileImportService fileImportService,
            ValidationService validationService)
        {
            _importBatchService = importBatchService ?? throw new ArgumentNullException(nameof(importBatchService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _fileImportService = fileImportService ?? throw new ArgumentNullException(nameof(fileImportService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        public async Task<ImportBatch> StartImportBatchAsync(string depot, string fileName)
        {
            try
            {
                Logger.Info($"Starting new import batch. Depot: {depot}, File: {fileName}");
                return await _importBatchService.CreateImportBatchAsync(depot, fileName);
            }
            catch (Exception ex)
            {
                Logger.Error("Error starting import batch", ex);
                throw;
            }
        }

        // Renamed and modified to process a single receipt
        public async Task<bool> ProcessSingleReceiptAsync(
            ImportBatch importBatch,
            Receipt receipt,
            CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                receipt.ImpBatch = importBatch.ImpBatch;
                receipt.ImportBatchId = importBatch.ImportBatchId; // Set modern ImportBatchId from the batch

                // Validate receipt (already done in ViewModel, but keep for robustness?)
                // Consider if validation should solely live here or in ViewModel loop.
                // For now, assume validation is primarily handled before calling this.
                // If validation IS needed here, it should throw or return false on failure.
                // try
                // {
                //     await _validationService.ValidateReceiptAsync(receipt);
                // }
                // catch (ImportValidationException ex)
                // {
                //     // Log the specific validation errors if needed
                //     Logger.Warn($"Validation failed for receipt {receipt.ReceiptNumber}: {string.Join("; ", ex.ValidationErrors)}");
                //     return false; // Indicate failure
                // }

                // Calculate net weight if needed
                // Note: Gross/Tare are now set to 0 in ReceiptService.SaveReceiptAsync based on sample.
                // This calculation might need adjustment or removal depending on final logic for Gross/Tare.
                // if (receipt.Net <= 0 && receipt.Gross > 0) // Only calculate if Gross is positive
                // {
                //     receipt.Net = await _receiptService.CalculateNetWeightAsync(receipt);
                // }

                // Get price if not set (Still commented out as per previous state)
                //if (receipt.ThePrice <= 0)
                //{
                //    receipt.ThePrice = await _receiptService.GetPriceForReceiptAsync(receipt);
                //}

                // Save receipt
                await _receiptService.SaveReceiptAsync(receipt);

                // Log success for this specific receipt
                Logger.Info($"Successfully processed and saved receipt {receipt.ReceiptNumber} for batch {importBatch.ImpBatch}.");
                return true; // Indicate success for this receipt
            }
            catch (MissingReferenceDataException)
            {
                // Let MissingReferenceDataException propagate to ImportViewModel
                // so it can display proper "Skipped... Missing:" messages
                throw;
            }
            catch (Exception ex)
            {
                // Log error for this specific receipt
                var error = $"Error processing receipt {receipt.ReceiptNumber} for grower {receipt.GrowerNumber}: {ex.Message}";
                Logger.Error(error, ex);
                // Do not add to a shared error list here, let the calling ViewModel handle errors per receipt.
                return false; // Indicate failure for this receipt
            }
        }


        public async Task UpdateBatchStatisticsAsync(ImportBatch importBatch)
        {
            try
            {
                Logger.Info($"Updating statistics for import batch {importBatch.ImpBatch}");

                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(importBatch.ImpBatch);
                if (!receipts.Any())
                {
                    Logger.Warn($"No receipts found for import batch {importBatch.ImpBatch}");
                    return;
                }

                importBatch.NoTrans = receipts.Count();
                importBatch.Receipts = receipts.Count(r => !r.IsVoid);
                importBatch.Voids = receipts.Count(r => r.IsVoid);
                importBatch.LowReceipt = receipts.Min(r => r.ReceiptNumber);
                importBatch.HighReceipt = receipts.Max(r => r.ReceiptNumber);
                importBatch.LowDate = receipts.Min(r => r.Date);
                importBatch.HighDate = receipts.Max(r => r.Date);

                await _importBatchService.UpdateImportBatchAsync(importBatch);
                Logger.Info($"Successfully updated statistics for import batch {importBatch.ImpBatch}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating batch statistics for batch {importBatch.ImpBatch}", ex);
                throw;
            }
        }

        public async Task<bool> FinalizeBatchAsync(ImportBatch importBatch)
        {
            try
            {
                Logger.Info($"Finalizing import batch {importBatch.ImpBatch}");
                return await _importBatchService.CloseImportBatchAsync(importBatch.ImpBatch);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finalizing import batch {importBatch.ImpBatch}", ex);
                throw;
            }
        }

        public async Task RollbackBatchAsync(ImportBatch importBatch)
        {
            try
            {
                Logger.Info($"Rolling back import batch {importBatch.ImpBatch}");

                // Delete all receipts for this batch
                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(importBatch.ImpBatch);
                foreach (var receipt in receipts)
                {
                    await _receiptService.DeleteReceiptAsync(receipt.ReceiptNumber);
                }

                // Reopen the batch
                await _importBatchService.ReopenImportBatchAsync(importBatch.ImpBatch);
                Logger.Info($"Successfully rolled back import batch {importBatch.ImpBatch}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error rolling back import batch {importBatch.ImpBatch}", ex);
                throw;
            }
        }

        public async Task<(int TotalReceipts, int ProcessedReceipts, int ErrorCount)> GetBatchStatusAsync(ImportBatch importBatch)
        {
            try
            {
                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(importBatch.ImpBatch);
                var totalReceipts = receipts.Count();
                var processedReceipts = receipts.Count(r => !r.IsVoid);
                var errorCount = receipts.Count(r => r.IsVoid);

                return (totalReceipts, processedReceipts, errorCount);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting batch status for batch {importBatch.ImpBatch}", ex);
                throw;
            }
        }
    }
}
