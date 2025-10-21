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
        private readonly IDuplicateDetectionService _duplicateDetectionService;

        public ImportBatchProcessor(
            IImportBatchService importBatchService,
            IReceiptService receiptService,
            IFileImportService fileImportService,
            ValidationService validationService,
            IDuplicateDetectionService duplicateDetectionService)
        {
            _importBatchService = importBatchService ?? throw new ArgumentNullException(nameof(importBatchService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _fileImportService = fileImportService ?? throw new ArgumentNullException(nameof(fileImportService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _duplicateDetectionService = duplicateDetectionService ?? throw new ArgumentNullException(nameof(duplicateDetectionService));
        }

        public async Task<ImportBatch> StartImportBatchAsync(int depotId, string fileName)
        {
            try
            {
                Logger.Info($"Starting new import batch. DepotId: {depotId}, File: {fileName}");
                return await _importBatchService.CreateImportBatchAsync(depotId, fileName);
            }
            catch (Exception ex)
            {
                Logger.Error("Error starting import batch", ex);
                throw;
            }
        }

        public async Task<List<ImportBatch>> StartMultipleImportBatchesAsync(int depotId, string fileName)
        {
            try
            {
                Logger.Info($"Starting multiple import batches for file: {fileName}");
                
                // Analyze the file to get batch groups
                var batchGroups = await _fileImportService.AnalyzeBatchNumbersAsync(fileName);
                
                if (!batchGroups.Any())
                {
                    Logger.Warn($"No valid batch groups found in file: {fileName}");
                    return new List<ImportBatch>();
                }
                
                // Create multiple import batches
                var importBatches = await _importBatchService.CreateMultipleImportBatchesAsync(depotId, fileName, batchGroups);
                
                Logger.Info($"Created {importBatches.Count} import batches for file: {fileName}");
                foreach (var batch in importBatches)
                {
                    var batchKey = batch.OriginalBatchNumber ?? "NO_BATCH";
                    var receiptCount = batchGroups.ContainsKey(batchKey) ? batchGroups[batchKey].Count : 0;
                    Logger.Info($"  Batch {batch.BatchNumber} (Original: {batch.OriginalBatchNumber}): {receiptCount} receipts");
                }
                
                return importBatches;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error starting multiple import batches for file: {fileName}", ex);
                throw;
            }
        }

        public async Task<SmartImportResult> StartSmartImportAsync(int depotId, string fileName)
        {
            try
            {
                Logger.Info($"Starting smart import for file: {fileName}");
                
                // Analyze the file to get batch groups
                var batchGroups = await _fileImportService.AnalyzeBatchNumbersAsync(fileName);
                
                if (!batchGroups.Any())
                {
                    Logger.Warn($"No valid batch groups found in file: {fileName}");
                    return new SmartImportResult 
                    { 
                        Success = false, 
                        Message = "No valid batch groups found in file" 
                    };
                }
                
                var result = new SmartImportResult
                {
                    FileName = fileName,
                    TotalBatches = batchGroups.Count,
                    ImportBatches = new List<ImportBatch>(),
                    Conflicts = new List<BatchConflictAnalysis>()
                };
                
                // Generate BatchGroupId for multi-batch files
                int? batchGroupId = null;
                if (batchGroups.Count > 1)
                {
                    batchGroupId = await _importBatchService.GetNextBatchGroupIdAsync();
                    Logger.Info($"Multi-batch file detected with {batchGroups.Count} batches. Using BatchGroupId: {batchGroupId}");
                }
                
                // Process each batch group with conflict detection
                foreach (var batchGroup in batchGroups)
                {
                    var batchNumber = batchGroup.Key;
                    var receipts = batchGroup.Value;
                    
                    Logger.Info($"Processing batch {batchNumber} with {receipts.Count} receipts");
                    
                    // Analyze for conflicts
                    var conflictAnalysis = await _duplicateDetectionService.AnalyzeBatchConflictsAsync(
                        fileName, batchNumber, receipts);
                    
                    if (conflictAnalysis.BatchExists)
                    {
                        // Handle batch conflict
                        result.Conflicts.Add(conflictAnalysis);
                        
                        // For now, use conflict resolution to create new batch
                        var conflictResolution = await _duplicateDetectionService.PresentBatchConflictOptionsAsync(conflictAnalysis);
                        
                        if (conflictResolution.RequiresUserDecision)
                        {
                            // This would normally show UI to user, for now we'll use default action
                            Logger.Info($"Batch conflict detected for {batchNumber}, creating new batch");
                        }
                        
                        // Create new batch with conflict resolution
                        var importBatch = await _importBatchService.CreateImportBatchWithConflictResolutionAsync(
                            depotId, fileName, batchNumber, batchGroups.Count > 1, batchGroupId);
                        
                        result.ImportBatches.Add(importBatch);
                        
                        // Process receipts with duplicate handling
                        var selectiveResult = await _duplicateDetectionService.ImportWithDuplicateHandlingAsync(
                            receipts, DuplicateHandlingStrategy.SkipDuplicates, importBatch.ImportBatchId);
                        
                        result.ImportResults.Add(selectiveResult);
                    }
                    else
                    {
                        // No conflict - create batch normally
                        var importBatch = await _importBatchService.CreateImportBatchAsync(
                            depotId, fileName, batchNumber, batchGroups.Count > 1, batchGroupId);
                        
                        result.ImportBatches.Add(importBatch);
                        
                        // Process receipts normally
                        var selectiveResult = await _duplicateDetectionService.ImportWithDuplicateHandlingAsync(
                            receipts, DuplicateHandlingStrategy.SkipDuplicates, importBatch.ImportBatchId);
                        
                        result.ImportResults.Add(selectiveResult);
                    }
                }
                
                result.Success = true;
                result.Message = $"Smart import completed: {result.ImportBatches.Count} batches created, {result.Conflicts.Count} conflicts resolved";
                
                Logger.Info($"Smart import completed for {fileName}: {result.Message}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in smart import for file: {fileName}", ex);
                return new SmartImportResult 
                { 
                    Success = false, 
                    Message = $"Smart import failed: {ex.Message}" 
                };
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

                // Set the modern ImportBatchId - this is what's actually stored in the database
                receipt.ImportBatchId = importBatch.ImportBatchId;

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
                Logger.Info($"Successfully processed and saved receipt {receipt.ReceiptNumber} for batch {importBatch.BatchNumber}.");
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
                Logger.Info($"Updating statistics for import batch {importBatch.BatchNumber}");

                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(decimal.Parse(importBatch.BatchNumber));
                if (!receipts.Any())
                {
                    Logger.Warn($"No receipts found for import batch {importBatch.BatchNumber}");
                    return;
                }

                importBatch.NoTrans = receipts.Count();
                importBatch.TotalReceipts = receipts.Count(r => !r.IsVoid);
                importBatch.Voids = receipts.Count(r => r.IsVoid);
                var validReceiptNumbers = receipts
                    .Select(r => {
                        decimal val;
                        return decimal.TryParse(r.ReceiptNumber, out val) ? (decimal?)val : null;
                    })
                    .Where(x => x.HasValue)
                    .ToList();
                importBatch.LowReceipt = validReceiptNumbers.Any() ? validReceiptNumbers.Min(x => x.GetValueOrDefault()).ToString() : "0";
                importBatch.HighReceipt = validReceiptNumbers.Any() ? validReceiptNumbers.Max(x => x.GetValueOrDefault()).ToString() : "0";
                importBatch.LowDate = receipts.Min(r => r.ReceiptDate);
                importBatch.HighDate = receipts.Max(r => r.ReceiptDate);

                await _importBatchService.UpdateImportBatchAsync(importBatch);
                Logger.Info($"Successfully updated statistics for import batch {importBatch.BatchNumber}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating batch statistics for batch {importBatch.BatchNumber}", ex);
                throw;
            }
        }

        public async Task<bool> FinalizeBatchAsync(ImportBatch importBatch)
        {
            try
            {
                Logger.Info($"Finalizing import batch {importBatch.BatchNumber}");
                return await _importBatchService.CloseImportBatchAsync(importBatch.BatchNumber);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finalizing import batch {importBatch.BatchNumber}", ex);
                throw;
            }
        }

        public async Task RollbackBatchAsync(ImportBatch importBatch)
        {
            try
            {
                Logger.Info($"Rolling back import batch {importBatch.BatchNumber}");

                // Get all receipts for this batch
                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(decimal.Parse(importBatch.BatchNumber));
                var receiptsWithPayments = new List<string>();
                var receiptsToDelete = new List<Receipt>();
                
                // Check each receipt for payment allocations
                foreach (var receipt in receipts)
                {
                    if (!string.IsNullOrEmpty(receipt.ReceiptNumber))
                    {
                        if (await _receiptService.HasPaymentAllocationsAsync(receipt.ReceiptId))
                        {
                            receiptsWithPayments.Add(receipt.ReceiptNumber);
                        }
                        else
                        {
                            receiptsToDelete.Add(receipt);
                        }
                    }
                }
                
                // Report receipts that cannot be deleted
                if (receiptsWithPayments.Any())
                {
                    Logger.Warn($"Cannot delete {receiptsWithPayments.Count} receipts due to payment allocations: {string.Join(", ", receiptsWithPayments)}");
                    throw new InvalidOperationException($"Cannot revert batch {importBatch.BatchNumber} because {receiptsWithPayments.Count} receipts have payment allocations: {string.Join(", ", receiptsWithPayments)}. Please void the payments first.");
                }
                
                // Delete receipts that don't have payments
                foreach (var receipt in receiptsToDelete)
                {
                    if (!string.IsNullOrEmpty(receipt.ReceiptNumber))
                    {
                        await _receiptService.DeleteReceiptAsync(receipt.ReceiptNumber);
                    }
                }

                // Reopen the batch
                await _importBatchService.ReopenImportBatchAsync(importBatch.BatchNumber);
                Logger.Info($"Successfully rolled back import batch {importBatch.BatchNumber}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error rolling back import batch {importBatch.BatchNumber}", ex);
                throw;
            }
        }

        public async Task RollbackBatchGroupAsync(ImportBatch importBatch)
        {
            try
            {
                if (!importBatch.BatchGroupId.HasValue)
                {
                    Logger.Warn($"Batch {importBatch.BatchNumber} has no BatchGroupId, falling back to single batch rollback");
                    await RollbackBatchAsync(importBatch);
                    return;
                }

                Logger.Info($"Rolling back all batches in group {importBatch.BatchGroupId}");

                // Get all batches in the same group
                var relatedBatches = await _importBatchService.GetBatchesByGroupIdAsync(importBatch.BatchGroupId.Value);
                
                Logger.Info($"Found {relatedBatches.Count} batches in group {importBatch.BatchGroupId}");

                // Check all batches for payment conflicts before starting rollback
                var allReceiptsWithPayments = new List<string>();
                var allReceiptsToDelete = new List<(ImportBatch batch, Receipt receipt)>();

                foreach (var batch in relatedBatches)
                {
                    var receipts = await _receiptService.GetReceiptsByImportBatchAsync(decimal.Parse(batch.BatchNumber));
                    foreach (var receipt in receipts)
                    {
                        if (!string.IsNullOrEmpty(receipt.ReceiptNumber))
                        {
                            if (await _receiptService.HasPaymentAllocationsAsync(receipt.ReceiptId))
                            {
                                allReceiptsWithPayments.Add(receipt.ReceiptNumber);
                            }
                            else
                            {
                                allReceiptsToDelete.Add((batch, receipt));
                            }
                        }
                    }
                }

                // Report all receipts that cannot be deleted
                if (allReceiptsWithPayments.Any())
                {
                    Logger.Warn($"Cannot delete {allReceiptsWithPayments.Count} receipts due to payment allocations: {string.Join(", ", allReceiptsWithPayments)}");
                    throw new InvalidOperationException($"Cannot revert batch group {importBatch.BatchGroupId} because {allReceiptsWithPayments.Count} receipts have payment allocations: {string.Join(", ", allReceiptsWithPayments)}. Please void the payments first.");
                }

                // Rollback each batch in the group
                foreach (var batch in relatedBatches)
                {
                    await RollbackBatchAsync(batch);
                }

                Logger.Info($"Successfully rolled back all {relatedBatches.Count} batches in group {importBatch.BatchGroupId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error rolling back batch group {importBatch.BatchGroupId}", ex);
                throw;
            }
        }

        public async Task<(int TotalReceipts, int ProcessedReceipts, int ErrorCount)> GetBatchStatusAsync(ImportBatch importBatch)
        {
            try
            {
                var receipts = await _receiptService.GetReceiptsByImportBatchAsync(decimal.Parse(importBatch.BatchNumber));
                var totalReceipts = receipts.Count();
                var processedReceipts = receipts.Count(r => !r.IsVoid);
                var errorCount = receipts.Count(r => r.IsVoid);

                return (totalReceipts, processedReceipts, errorCount);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting batch status for batch {importBatch.BatchNumber}", ex);
                throw;
            }
        }
    }
}
