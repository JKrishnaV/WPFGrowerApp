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

        public async Task<(bool Success, List<string> Errors)> ProcessReceiptsAsync(
            ImportBatch importBatch,
            IEnumerable<Receipt> receipts,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                Logger.Info($"Processing receipts for import batch {importBatch.ImpBatch}");
                var errors = new List<string>();
                var receiptList = receipts.ToList();
                var totalReceipts = receiptList.Count;
                var processedCount = 0;

                foreach (var receipt in receiptList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        receipt.ImpBatch = importBatch.ImpBatch;
                        
                        // Validate receipt                     
                        try
                        {
                            await _validationService.ValidateReceiptAsync(receipt);
                        }
                        catch (ImportValidationException ex)
                        {
                            errors.Add($"Invalid receipt data for grower {receipt.GrowerNumber}");
                            continue;
                        }

                        // Calculate net weight if needed
                        if (receipt.Net <= 0)
                        {
                            receipt.Net = await _receiptService.CalculateNetWeightAsync(receipt);
                        }

                        // Get price if not set
                        if (receipt.ThePrice <= 0)
                        {
                            receipt.ThePrice = await _receiptService.GetPriceForReceiptAsync(receipt);
                        }

                        // Save receipt
                        await _receiptService.SaveReceiptAsync(receipt);

                        processedCount++;
                        progress?.Report((int)(processedCount * 100.0 / totalReceipts));
                    }
                    catch (Exception ex)
                    {
                        var error = $"Error processing receipt for grower {receipt.GrowerNumber}: {ex.Message}";
                        Logger.Error(error, ex);
                        errors.Add(error);
                    }
                }

                // Update batch statistics
                await UpdateBatchStatisticsAsync(importBatch);

                var success = !errors.Any();
                Logger.Info($"Receipt processing completed. Success: {success}, Error count: {errors.Count}");
                return (success, errors);
            }
            catch (Exception ex)
            {
                Logger.Error("Error processing receipts", ex);
                throw;
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