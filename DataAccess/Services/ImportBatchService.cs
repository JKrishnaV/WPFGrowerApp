using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using System.Linq;

namespace WPFGrowerApp.DataAccess.Services
{
    public class ImportBatchService : BaseDatabaseService, IImportBatchService
    {

        public async Task<ImportBatch> CreateImportBatchAsync(string depot, string impFile)
        {
            try
            {
                var importBatch = new ImportBatch
                {
                    ImpBatch = await GetNextImportBatchNumberAsync(),
                    Date = DateTime.Now,
                    DataDate = DateTime.Now,
                    Depot = depot,
                    ImpFile = impFile,
                    NoTrans = 0,
                    Voids = 0,
                    Receipts = 0
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // TODO: Lookup DepotId from depot code
                    var depotId = await connection.ExecuteScalarAsync<int>(
                        "SELECT DepotId FROM Depots WHERE DepotCode = @Depot AND IsActive = 1",
                        new { importBatch.Depot });
                    
                    var sql = @"
                        INSERT INTO ImportBatches (
                            BatchNumber, ImportDate, DepotId, TotalReceipts,
                            TotalGrossWeight, TotalNetWeight, Status, ImportedAt, ImportedBy, Notes
                        ) VALUES (
                            @ImpBatch, @Date, @DepotId, @Receipts,
                            0, 0, 'Draft', GETDATE(), 'SYSTEM', @ImpFile
                        );
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var importBatchId = await connection.ExecuteScalarAsync<int>(sql, new 
                    { 
                        importBatch.ImpBatch,
                        importBatch.Date,
                        DepotId = depotId,
                        importBatch.Receipts,
                        importBatch.ImpFile
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

        public async Task<ImportBatch> GetImportBatchAsync(decimal impBatch)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM ImportBatches WHERE BatchNumber = @ImpBatch";
                    var parameters = new { ImpBatch = impBatch.ToString() };
                    return await connection.QueryFirstOrDefaultAsync<ImportBatch>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetImportBatchAsync: {ex.Message}");
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
                        SELECT * FROM ImportBatches 
                        WHERE 1=1
                        @StartDateFilter
                        @EndDateFilter
                        ORDER BY ImportDate DESC";

                    var parameters = new DynamicParameters();
                    if (startDate.HasValue)
                    {
                        sql = sql.Replace("@StartDateFilter", "AND ImportDate >= @StartDate");
                        parameters.Add("@StartDate", startDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@StartDateFilter", "");
                    }

                    if (endDate.HasValue)
                    {
                        sql = sql.Replace("@EndDateFilter", "AND ImportDate <= @EndDate");
                        parameters.Add("@EndDate", endDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@EndDateFilter", "");
                    }

                    return (await connection.QueryAsync<ImportBatch>(sql, parameters)).ToList();
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
                        SET TotalReceipts = @Receipts,
                            Status = 'Posted'
                        WHERE BatchNumber = @ImpBatch";

                    var result = await connection.ExecuteAsync(sql, new 
                    { 
                        importBatch.ImpBatch,
                        importBatch.Receipts
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

        public async Task<decimal> GetNextImportBatchNumberAsync()
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
                    return result ?? 1;
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
                    var existingBatch = await GetImportBatchAsync(importBatch.ImpBatch);
                    if (existingBatch == null)
                    {
                        return false;
                    }

                    // Validate depot exists
                    var depotCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Depots WHERE DepotCode = @Depot AND IsActive = 1",
                        new { importBatch.Depot });
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

        public async Task<bool> CloseImportBatchAsync(decimal impBatch)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE ImportBatches 
                        SET TotalReceipts = (SELECT COUNT(*) FROM Receipts WHERE ImportBatchId = ib.ImportBatchId),
                            TotalGrossWeight = (SELECT ISNULL(SUM(GrossWeight), 0) FROM Receipts WHERE ImportBatchId = ib.ImportBatchId),
                            TotalNetWeight = (SELECT ISNULL(SUM(NetWeight), 0) FROM Receipts WHERE ImportBatchId = ib.ImportBatchId),
                            Status = 'Posted'
                        FROM ImportBatches ib
                        WHERE ib.BatchNumber = @ImpBatch";

                    var parameters = new { ImpBatch = impBatch.ToString() };
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

        public async Task<bool> ReopenImportBatchAsync(decimal impBatch)
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
                        WHERE BatchNumber = @ImpBatch";

                    var parameters = new { ImpBatch = impBatch.ToString() };
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
    }
}