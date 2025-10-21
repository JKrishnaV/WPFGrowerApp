using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class DuplicateDetectionService : BaseDatabaseService, IDuplicateDetectionService
    {
        private readonly IReceiptService _receiptService;

        public DuplicateDetectionService(IReceiptService receiptService)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        }

        public async Task<ImportAnalysisResult> AnalyzePartialDuplicatesAsync(List<Receipt> newReceipts)
        {
            try
            {
                Logger.Info($"Analyzing {newReceipts.Count} receipts for duplicates");
                
                var result = new ImportAnalysisResult
                {
                    TotalReceipts = newReceipts.Count,
                    NewReceipts = new List<Receipt>(),
                    DuplicateReceipts = new List<Receipt>(),
                    SoftDeletedReceipts = new List<Receipt>()
                };

                foreach (var receipt in newReceipts)
                {
                    if (string.IsNullOrEmpty(receipt.ReceiptNumber))
                    {
                        // Receipt without number - treat as new
                        result.NewReceipts.Add(receipt);
                        continue;
                    }

                    var existingReceipt = await _receiptService.GetReceiptByNumberAsync(decimal.Parse(receipt.ReceiptNumber));
                    
                    if (existingReceipt == null)
                    {
                        // New receipt
                        result.NewReceipts.Add(receipt);
                    }
                    else if (existingReceipt.DeletedAt.HasValue)
                    {
                        // Soft-deleted receipt - can be updated
                        result.SoftDeletedReceipts.Add(receipt);
                    }
                    else
                    {
                        // Active duplicate receipt
                        result.DuplicateReceipts.Add(receipt);
                    }
                }

                Logger.Info($"Analysis complete: {result.NewReceipts.Count} new, {result.DuplicateReceipts.Count} duplicates, {result.SoftDeletedReceipts.Count} soft-deleted");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Error analyzing partial duplicates", ex);
                throw;
            }
        }

        public async Task<BatchConflictAnalysis> AnalyzeBatchConflictsAsync(string fileName, string batchNumber, List<Receipt> receipts)
        {
            try
            {
                Logger.Info($"Analyzing batch conflicts for file {fileName}, batch {batchNumber}");
                
                var analysis = new BatchConflictAnalysis
                {
                    FileName = fileName,
                    BatchNumber = batchNumber,
                    TotalReceipts = receipts.Count
                };

                // Check if batch number exists
                var existingBatch = await GetImportBatchByNumberAsync(decimal.Parse(batchNumber));
                analysis.BatchExists = existingBatch != null;

                if (existingBatch != null)
                {
                    Logger.Info($"Batch {batchNumber} already exists, analyzing receipt conflicts");
                    
                    // Get existing receipts in this batch
                    var existingReceipts = await _receiptService.GetReceiptsByImportBatchAsync(decimal.Parse(existingBatch.BatchNumber));
                    
                    // Analyze receipt-level conflicts
                    foreach (var newReceipt in receipts)
                    {
                        if (string.IsNullOrEmpty(newReceipt.ReceiptNumber))
                        {
                            analysis.NewReceipts.Add(newReceipt);
                            continue;
                        }

                        var existingReceipt = existingReceipts.FirstOrDefault(r => r.ReceiptNumber == newReceipt.ReceiptNumber);
                        
                        if (existingReceipt != null)
                        {
                            var conflict = new ReceiptConflict
                            {
                                NewReceipt = newReceipt,
                                ExistingReceipt = existingReceipt,
                                ConflictType = DetermineConflictType(newReceipt, existingReceipt),
                                ResolutionOptions = GetResolutionOptions(newReceipt, existingReceipt)
                            };
                            
                            analysis.Conflicts.Add(conflict);
                        }
                        else
                        {
                            analysis.NewReceipts.Add(newReceipt);
                        }
                    }
                    
                    analysis.Summary = $"Batch {batchNumber} exists with {existingReceipts.Count} receipts. " +
                                     $"New file has {analysis.NewReceipts.Count} new receipts and {analysis.Conflicts.Count} conflicts.";
                }
                else
                {
                    // No existing batch - all receipts are new
                    analysis.NewReceipts = receipts;
                    analysis.Summary = $"Batch {batchNumber} is new. All {receipts.Count} receipts can be imported.";
                }

                Logger.Info($"Batch conflict analysis complete: {analysis.Summary}");
                return analysis;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error analyzing batch conflicts for {fileName}", ex);
                throw;
            }
        }

        public async Task<ConflictResolutionResult> PresentBatchConflictOptionsAsync(BatchConflictAnalysis analysis)
        {
            try
            {
                var result = new ConflictResolutionResult();
                
                if (!analysis.BatchExists)
                {
                    // No conflict - proceed with normal import
                    result.CanProceed = true;
                    result.RecommendedAction = "Create new batch";
                    return result;
                }

                // Present options to user
                var options = new List<ConflictResolutionOption>();
                
                // Option 1: Create new batch with different number
                var nextBatchNumber = await GetNextImportBatchNumberAsync();
                options.Add(new ConflictResolutionOption
                {
                    Id = "create_new_batch",
                    Title = "Create New Batch",
                    Description = $"Create a new batch with generated number ({nextBatchNumber})",
                    Impact = $"Will create separate batch for {analysis.FileName}",
                    Recommended = true
                });
                
                // Option 2: Merge into existing batch (only if conflicts are manageable)
                if (analysis.Conflicts.Count < analysis.TotalReceipts * 0.5) // Less than 50% conflicts
                {
                    options.Add(new ConflictResolutionOption
                    {
                        Id = "merge_batch",
                        Title = "Merge into Existing Batch",
                        Description = $"Add new receipts to existing batch {analysis.BatchNumber}",
                        Impact = $"Will add {analysis.NewReceipts.Count} new receipts to existing batch",
                        Recommended = false
                    });
                }
                
                // Option 3: Replace existing batch
                options.Add(new ConflictResolutionOption
                {
                    Id = "replace_batch",
                    Title = "Replace Existing Batch",
                    Description = $"Replace all receipts in batch {analysis.BatchNumber} with new data",
                    Impact = $"Will replace {analysis.Conflicts.Count} existing receipts",
                    Recommended = false
                });
                
                // Option 4: Cancel import
                options.Add(new ConflictResolutionOption
                {
                    Id = "cancel_import",
                    Title = "Cancel Import",
                    Description = "Do not import this file",
                    Impact = "No changes will be made",
                    Recommended = false
                });

                result.Options = options;
                result.RequiresUserDecision = true;
                result.Analysis = analysis;

                Logger.Info($"Presented {options.Count} conflict resolution options");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Error presenting batch conflict options", ex);
                throw;
            }
        }

        public async Task<SelectiveImportResult> ImportWithDuplicateHandlingAsync(
            List<Receipt> allReceipts, 
            DuplicateHandlingStrategy strategy,
            int importBatchId)
        {
            try
            {
                Logger.Info($"Starting import with duplicate handling strategy: {strategy}");
                
                var analysis = await AnalyzePartialDuplicatesAsync(allReceipts);
                var result = new SelectiveImportResult
                {
                    ImportBatchId = importBatchId,
                    TotalReceipts = allReceipts.Count
                };

                switch (strategy)
                {
                    case DuplicateHandlingStrategy.SkipDuplicates:
                        // Import only new receipts
                        foreach (var newReceipt in analysis.NewReceipts)
                        {
                            newReceipt.ImportBatchId = importBatchId;
                            await _receiptService.SaveReceiptAsync(newReceipt);
                            result.ImportedReceipts.Add(newReceipt);
                        }
                        
                        foreach (var softDeleted in analysis.SoftDeletedReceipts)
                        {
                            softDeleted.ImportBatchId = importBatchId;
                            await _receiptService.SaveReceiptAsync(softDeleted);
                            result.ImportedReceipts.Add(softDeleted);
                        }
                        
                        result.SkippedReceipts = analysis.DuplicateReceipts;
                        result.Message = $"Imported {analysis.NewReceipts.Count + analysis.SoftDeletedReceipts.Count} receipts, skipped {analysis.DuplicateReceipts.Count} duplicates";
                        break;
                        
                    case DuplicateHandlingStrategy.UpdateDuplicates:
                        // Update existing receipts with new data
                        foreach (var duplicate in analysis.DuplicateReceipts)
                        {
                            await _receiptService.UpdateReceiptAsync(duplicate);
                            result.UpdatedReceipts.Add(duplicate);
                        }
                        
                        foreach (var newReceipt in analysis.NewReceipts)
                        {
                            newReceipt.ImportBatchId = importBatchId;
                            await _receiptService.SaveReceiptAsync(newReceipt);
                            result.ImportedReceipts.Add(newReceipt);
                        }
                        
                        foreach (var softDeleted in analysis.SoftDeletedReceipts)
                        {
                            softDeleted.ImportBatchId = importBatchId;
                            await _receiptService.SaveReceiptAsync(softDeleted);
                            result.ImportedReceipts.Add(softDeleted);
                        }
                        
                        result.Message = $"Updated {analysis.DuplicateReceipts.Count} existing receipts, imported {analysis.NewReceipts.Count + analysis.SoftDeletedReceipts.Count} new receipts";
                        break;
                        
                    case DuplicateHandlingStrategy.AskUser:
                        // Present options to user
                        result.RequiresUserDecision = true;
                        result.Analysis = analysis;
                        result.Message = "User decision required for duplicate handling";
                        break;
                }

                Logger.Info($"Import with duplicate handling completed: {result.Message}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error("Error in import with duplicate handling", ex);
                throw;
            }
        }

        private ConflictType DetermineConflictType(Receipt newReceipt, Receipt existing)
        {
            if (existing.DeletedAt.HasValue)
                return ConflictType.SoftDeleted; // Can be updated
            
            if (newReceipt.NetWeight != existing.NetWeight)
                return ConflictType.DataMismatch; // Different data
            
            if (newReceipt.ReceiptDate != existing.ReceiptDate)
                return ConflictType.DateMismatch; // Different date
            
            return ConflictType.ExactDuplicate; // Identical
        }

        private List<string> GetResolutionOptions(Receipt newReceipt, Receipt existing)
        {
            var options = new List<string>();
            
            if (existing.DeletedAt.HasValue)
            {
                options.Add("Update soft-deleted receipt");
            }
            else
            {
                options.Add("Skip duplicate receipt");
                options.Add("Update existing receipt with new data");
            }
            
            return options;
        }

        private async Task<ImportBatch> GetImportBatchByNumberAsync(decimal batchNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM ImportBatches WHERE BatchNumber = @BatchNumber";
                    var parameters = new { BatchNumber = batchNumber };
                    return await connection.QueryFirstOrDefaultAsync<ImportBatch>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting import batch {batchNumber}", ex);
                throw;
            }
        }

        private async Task<decimal> GetNextImportBatchNumberAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT ISNULL(
                            MAX(CAST(BatchNumber AS INT)), 0
                        ) + 1 
                        FROM ImportBatches 
                        WHERE BatchNumber NOT LIKE '%[^0-9]%'";
                    var result = await connection.ExecuteScalarAsync<decimal?>(sql);
                    return result ?? 1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting next import batch number", ex);
                throw;
            }
        }
    }
}
