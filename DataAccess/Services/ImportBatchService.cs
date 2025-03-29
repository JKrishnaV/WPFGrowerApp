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
                    var sql = @"
                        INSERT INTO ImpBat (
                            IMP_BAT, DATE, DATA_DATE, DEPOT, IMP_FILE,
                            NO_TRANS, VOIDS, RECEIPTS
                        ) VALUES (
                            @ImpBatch, @Date, @DataDate, @Depot, @ImpFile,
                            @NoTrans, @Voids, @Receipts
                        )";

                    await connection.ExecuteAsync(sql, importBatch);
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
                    var sql = "SELECT * FROM ImpBat WHERE IMP_BAT = @ImpBatch";
                    var parameters = new { ImpBatch = impBatch };
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
                        SELECT * FROM ImpBat 
                        WHERE 1=1
                        @StartDateFilter
                        @EndDateFilter
                        ORDER BY DATE DESC";

                    var parameters = new DynamicParameters();
                    if (startDate.HasValue)
                    {
                        sql = sql.Replace("@StartDateFilter", "AND DATE >= @StartDate");
                        parameters.Add("@StartDate", startDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@StartDateFilter", "");
                    }

                    if (endDate.HasValue)
                    {
                        sql = sql.Replace("@EndDateFilter", "AND DATE <= @EndDate");
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
                        UPDATE ImpBat 
                        SET NO_TRANS = @NoTrans,
                            LOW_ID = @LowId,
                            HIGH_ID = @HighId,
                            LOW_RECPT = @LowReceipt,
                            HI_RECPT = @HighReceipt,
                            LOW_DATE = @LowDate,
                            HIGH_DATE = @HighDate,
                            VOIDS = @Voids,
                            RECEIPTS = @Receipts
                        WHERE IMP_BAT = @ImpBatch";

                    var result = await connection.ExecuteAsync(sql, importBatch);
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
                    var sql = "SELECT MAX(IMP_BAT) + 1 FROM ImpBat";
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
                        "SELECT COUNT(*) FROM Depot WHERE DEPOT = @Depot",
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
                        UPDATE ImpBat 
                        SET RECEIPTS = (SELECT COUNT(*) FROM Daily WHERE IMP_BAT = @ImpBatch),
                            NO_TRANS = (SELECT COUNT(*) FROM Daily WHERE IMP_BAT = @ImpBatch),
                            VOIDS = (SELECT COUNT(*) FROM Daily WHERE IMP_BAT = @ImpBatch AND ISVOID = 1)
                        WHERE IMP_BAT = @ImpBatch";

                    var parameters = new { ImpBatch = impBatch };
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
                        UPDATE ImpBat 
                        SET RECEIPTS = 0,
                            NO_TRANS = 0,
                            VOIDS = 0
                        WHERE IMP_BAT = @ImpBatch";

                    var parameters = new { ImpBatch = impBatch };
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