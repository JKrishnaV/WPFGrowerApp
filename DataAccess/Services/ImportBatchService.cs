using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using System.Linq;

namespace WPFGrowerApp.DataAccess.Services
{
    public class ImportBatchService : BaseDatabaseService, IImportBatchService
    {

        public async Task<ImportBatch> CreateImportBatchAsync(int depotId, string impFile, string? originalBatchNumber = null, bool isFromMultiBatchFile = false, int? batchGroupId = null)
        {
            try
            {
                var importBatch = new ImportBatch
                {
                    BatchNumber = !string.IsNullOrEmpty(originalBatchNumber) ? originalBatchNumber : await GetNextImportBatchNumberAsync(),
                    OriginalBatchNumber = originalBatchNumber,
                    SourceFileName = impFile,
                    IsFromMultiBatchFile = isFromMultiBatchFile,
                    BatchGroupId = batchGroupId,
                    ImportDate = DateTime.Now,
                    DepotId = depotId,
                    TotalReceipts = 0,
                    Status = "Draft",
                    ImportedAt = DateTime.Now,
                    ImportedBy = App.CurrentUser?.Username ?? "SYSTEM",
                    CreatedAt = DateTime.Now,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        INSERT INTO ImportBatches (
                            BatchNumber, ImportDate, DepotId, TotalReceipts,
                            TotalGrossWeight, TotalNetWeight, Status, ImportedAt, ImportedBy, Notes,
                            OriginalBatchNumber, SourceFileName, IsFromMultiBatchFile, BatchGroupId
                        ) VALUES (
                            @BatchNumber, @ImportDate, @DepotId, @TotalReceipts,
                            0, 0, @Status, @ImportedAt, @ImportedBy, @Notes,
                            @OriginalBatchNumber, @SourceFileName, @IsFromMultiBatchFile, @BatchGroupId
                        );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var importBatchId = await connection.ExecuteScalarAsync<int>(sql, new 
                    { 
                        importBatch.BatchNumber,
                        importBatch.ImportDate,
                        importBatch.DepotId,
                        importBatch.TotalReceipts,
                        importBatch.Status,
                        importBatch.ImportedAt,
                        importBatch.ImportedBy,
                        Notes = (string?)null,
                        importBatch.OriginalBatchNumber,
                        importBatch.SourceFileName,
                        importBatch.IsFromMultiBatchFile,
                        importBatch.BatchGroupId
                    });
                    
                    // Set the ImportBatchId on the object
                    importBatch.ImportBatchId = importBatchId;
                    
                    // Return with ImportBatchId
                    return importBatch;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ImportBatch> CreateImportBatchWithConflictResolutionAsync(
            int depotId, 
            string impFile, 
            string? originalBatchNumber, 
            bool isFromMultiBatchFile = false, 
            int? batchGroupId = null)
        {
            try
            {
                // Check if batch number already exists (including soft-deleted ones)
                if (!string.IsNullOrEmpty(originalBatchNumber) && decimal.TryParse(originalBatchNumber, out var parsedBatchNo))
                {
                    // Check for existing batch including soft-deleted ones
                    var existingBatch = await GetImportBatchIncludingDeletedAsync(originalBatchNumber);
                    if (existingBatch != null)
                    {
                        if (existingBatch.DeletedAt.HasValue)
                        {
                            // Batch exists but is soft-deleted - undelete and reuse it
                            Logger.Info($"Reusing soft-deleted batch {originalBatchNumber} for file {impFile}");
                            return await UndeleteImportBatchAsync(existingBatch, depotId, impFile, isFromMultiBatchFile, batchGroupId);
                        }
                        else
                        {
                            // Batch exists and is active - generate new sequential number
                            var newBatchNumber = await GetNextImportBatchNumberAsync();
                            
                            Logger.Info($"Batch number {originalBatchNumber} already exists and is active. " +
                                     $"Using generated batch number {newBatchNumber} for file {impFile}");
                            
                            return await CreateImportBatchAsync(
                                depotId, 
                                impFile, 
                                newBatchNumber.ToString(), // Use new batch number
                                isFromMultiBatchFile, 
                                batchGroupId);
                        }
                    }
                }
                
                // No conflict, use original batch number
                return await CreateImportBatchAsync(depotId, impFile, originalBatchNumber, isFromMultiBatchFile, batchGroupId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in CreateImportBatchWithConflictResolutionAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ImportBatch>> CreateMultipleImportBatchesAsync(int depotId, string fileName, Dictionary<string, List<Receipt>> batchGroups)
        {
            try
            {
                var importBatches = new List<ImportBatch>();
                var batchGroupId = await GetNextBatchGroupIdAsync();
                
                foreach (var batchGroup in batchGroups)
                {
                    var batchNumber = batchGroup.Key;
                    var receipts = batchGroup.Value;
                    
                    var importBatch = await CreateImportBatchAsync(
                        depotId, 
                        fileName, 
                        batchNumber, 
                        batchGroups.Count > 1, // isFromMultiBatchFile
                        batchGroupId
                    );
                    
                    importBatches.Add(importBatch);
                }
                
                return importBatches;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CreateMultipleImportBatchesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<int> GetNextBatchGroupIdAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT ISNULL(MAX(BatchGroupId), 0) + 1 
                        FROM ImportBatches 
                        WHERE BatchGroupId IS NOT NULL";
                    var result = await connection.ExecuteScalarAsync<int>(sql);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetNextBatchGroupIdAsync: {ex.Message}");
                return 1; // Default fallback
            }
        }

        public async Task<ImportBatch> GetImportBatchAsync(string batchNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM ImportBatches WHERE BatchNumber = @BatchNumber AND DeletedAt IS NULL";
                    var parameters = new { BatchNumber = batchNumber };
                    return await connection.QueryFirstOrDefaultAsync<ImportBatch>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ImportBatch?> GetImportBatchIncludingDeletedAsync(string batchNumber)
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
                Logger.Error($"Error in GetImportBatchIncludingDeletedAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ImportBatch>> GetImportBatchesAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            ib.ImportBatchId,
                            ib.BatchNumber,
                            ib.ImportDate,
                            ib.DepotId,
                            ib.TotalReceipts,
                            ib.TotalGrossWeight,
                            ib.TotalNetWeight,
                            ib.Status,
                            ib.ImportedAt,
                            ib.ImportedBy,
                            ib.Notes,
                            ib.CreatedAt,
                            ib.CreatedBy,
                            ib.ModifiedAt,
                            ib.ModifiedBy,
                            ib.DeletedAt,
                            ib.DeletedBy,
                            ib.OriginalBatchNumber,
                            ib.SourceFileName,
                            ib.IsFromMultiBatchFile,
                            ib.BatchGroupId,
                            d.DepotName
                        FROM ImportBatches ib
                        LEFT JOIN Depots d ON ib.DepotId = d.DepotId
                        WHERE ib.DeletedAt IS NULL
                        @StartImportDateFilter
                        @EndImportDateFilter
                        ORDER BY ib.ImportDate DESC";

                    var parameters = new DynamicParameters();
                    if (startDate.HasValue)
                    {
                        sql = sql.Replace("@StartImportDateFilter", "AND ib.ImportDate >= @StartImportDate");
                        parameters.Add("@StartImportDate", startDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@StartImportDateFilter", "");
                    }

                    if (endDate.HasValue)
                    {
                        sql = sql.Replace("@EndImportDateFilter", "AND ib.ImportDate <= @EndImportDate");
                        parameters.Add("@EndImportDate", endDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@EndImportDateFilter", "");
                    }

                    var batches = await connection.QueryAsync<ImportBatch>(sql, parameters);
                    return batches.ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetImportBatchesAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateImportBatchAsync(ImportBatch importBatch)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE ImportBatches 
                        SET TotalReceipts = @TotalReceipts,
                            Status = 'Posted'
                        WHERE BatchNumber = @BatchNumber";

                    var result = await connection.ExecuteAsync(sql, new 
                    { 
                        importBatch.BatchNumber,
                        importBatch.TotalReceipts
                    });
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetNextImportBatchNumberAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Get max numeric BatchNumber and increment
                    var sql = @"
                        SELECT ISNULL(
                            MAX(CAST(BatchNumber AS INT)), 0
                        ) + 1 
                        FROM ImportBatches 
                        WHERE BatchNumber NOT LIKE '%[^0-9]%'";
                    var result = await connection.ExecuteScalarAsync<decimal?>(sql);
                    return (result ?? 1).ToString();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetNextImportBatchNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateImportBatchAsync(ImportBatch importBatch)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if import batch exists
                    var existingBatch = await GetImportBatchAsync(importBatch.BatchNumber);
                    if (existingBatch == null)
                    {
                        return false;
                    }

                    // Validate depot exists
                    var depotCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Depots WHERE DepotCode = @Depot AND IsActive = 1",
                        new { Depot = importBatch.DepotId });
                    if (depotCount == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ValidateImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CloseImportBatchAsync(string batchNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE ImportBatches 
                        SET TotalReceipts = (SELECT COUNT(*) FROM Receipts WHERE ImportBatchId = ib.ImportBatchId AND DeletedAt IS NULL),
                            TotalGrossWeight = (SELECT ISNULL(SUM(GrossWeight), 0) FROM Receipts WHERE ImportBatchId = ib.ImportBatchId AND DeletedAt IS NULL),
                            TotalNetWeight = (SELECT ISNULL(SUM(NetWeight), 0) FROM Receipts WHERE ImportBatchId = ib.ImportBatchId AND DeletedAt IS NULL),
                            Status = 'Posted'
                        FROM ImportBatches ib
                        WHERE ib.BatchNumber = @BatchNumber";

                    var parameters = new { BatchNumber = batchNumber };
                    var result = await connection.ExecuteAsync(sql, parameters);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CloseImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ReopenImportBatchAsync(string batchNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE ImportBatches 
                        SET TotalReceipts = 0,
                            TotalGrossWeight = 0,
                            TotalNetWeight = 0,
                            Status = 'Draft'
                        WHERE BatchNumber = @BatchNumber";

                    var parameters = new { BatchNumber = batchNumber };
                    var result = await connection.ExecuteAsync(sql, parameters);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ReopenImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the count of recent import batches within the specified date range (optimized for dashboard)
        /// </summary>
        public async Task<int> GetRecentImportsCountAsync(DateTime? startDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM ImportBatches WHERE 1=1";
                    
                    var parameters = new DynamicParameters();
                    if (startDate.HasValue)
                    {
                        sql += " AND ImportDate >= @StartImportDate";
                        parameters.Add("@StartImportDate", startDate.Value);
                    }
                    
                    return await connection.ExecuteScalarAsync<int>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRecentImportsCountAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ImportBatch>> GetBatchesByGroupIdAsync(int batchGroupId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT * FROM ImportBatches 
                        WHERE BatchGroupId = @BatchGroupId
                        ORDER BY BatchNumber";
                    var parameters = new { BatchGroupId = batchGroupId };
                    return (await connection.QueryAsync<ImportBatch>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetBatchesByGroupIdAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteImportBatchAsync(string batchNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get the current user for audit trail
                    var deletedBy = App.CurrentUser?.Username ?? "SYSTEM";
                    
                    // First, get the ImportBatchId for the given batch number
                    var batchIdSql = @"
                        SELECT ImportBatchId FROM ImportBatches 
                        WHERE BatchNumber = @BatchNumber";
                    var batchId = await connection.ExecuteScalarAsync<int?>(batchIdSql, new { BatchNumber = batchNumber });
                    
                    var receiptCount = 0;
                    if (batchId.HasValue)
                    {
                        // Check if batch has any receipts
                        var receiptCountSql = @"
                            SELECT COUNT(*) FROM Receipts 
                            WHERE ImportBatchId = @ImportBatchId AND DeletedAt IS NULL";
                        receiptCount = await connection.ExecuteScalarAsync<int>(receiptCountSql, new { ImportBatchId = batchId.Value });
                        
                        if (receiptCount > 0)
                        {
                            // Soft delete all receipts in this batch first
                            var softDeleteReceiptsSql = @"
                                UPDATE Receipts 
                                SET DeletedAt = GETDATE(), 
                                    DeletedBy = @DeletedBy,
                                    ImportBatchId = NULL
                                WHERE ImportBatchId = @ImportBatchId AND DeletedAt IS NULL";
                            await connection.ExecuteAsync(softDeleteReceiptsSql, new { ImportBatchId = batchId.Value, DeletedBy = deletedBy });
                        }
                    }
                    
                    // Then soft delete the batch record
                    var softDeleteBatchSql = @"
                        UPDATE ImportBatches 
                        SET DeletedAt = GETDATE(), 
                            DeletedBy = @DeletedBy
                        WHERE BatchNumber = @BatchNumber";
                    
                    var result = await connection.ExecuteAsync(softDeleteBatchSql, new { BatchNumber = batchNumber, DeletedBy = deletedBy });
                    
                    Logger.Info($"Deleted import batch {batchNumber} with {receiptCount} receipts");
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in DeleteImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<ImportBatch> UndeleteImportBatchAsync(ImportBatch existingBatch, int depotId, string impFile, bool isFromMultiBatchFile, int? batchGroupId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    
                    // Undelete the batch by clearing DeletedAt and DeletedBy
                    var undeleteBatchSql = @"
                        UPDATE ImportBatches 
                        SET DeletedAt = NULL, 
                            DeletedBy = NULL,
                            ImportDate = @ImportDate,
                            DepotId = @DepotId,
                            SourceFileName = @SourceFileName,
                            IsFromMultiBatchFile = @IsFromMultiBatchFile,
                            BatchGroupId = @BatchGroupId,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ImportBatchId = @ImportBatchId";
                    
                    await connection.ExecuteAsync(undeleteBatchSql, new 
                    { 
                        ImportBatchId = existingBatch.ImportBatchId,
                        ImportDate = DateTime.Now,
                        DepotId = depotId,
                        SourceFileName = impFile,
                        IsFromMultiBatchFile = isFromMultiBatchFile,
                        BatchGroupId = batchGroupId,
                        ModifiedBy = currentUser
                    });
                    
                    // Also undelete any associated receipts
                    var undeleteReceiptsSql = @"
                        UPDATE Receipts 
                        SET DeletedAt = NULL, 
                            DeletedBy = NULL,
                            ImportBatchId = @ImportBatchId
                        WHERE ImportBatchId = @ImportBatchId";
                    
                    await connection.ExecuteAsync(undeleteReceiptsSql, new { ImportBatchId = existingBatch.ImportBatchId });
                    
                    Logger.Info($"Undeleted import batch {existingBatch.BatchNumber} for file {impFile}");
                    
                    // Return the updated batch
                    return await GetImportBatchAsync(existingBatch.BatchNumber);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in UndeleteImportBatchAsync: {ex.Message}");
                throw;
            }
        }
    }
}