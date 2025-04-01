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
    public class ReceiptService : BaseDatabaseService, IReceiptService
    {
       

        public async Task<List<Receipt>> GetReceiptsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT * FROM Daily 
                        WHERE 1=1
                        @StartDateFilter
                        @EndDateFilter
                        ORDER BY Date DESC, ReceiptNumber DESC";

                    var parameters = new DynamicParameters();
                    if (startDate.HasValue)
                    {
                        sql = sql.Replace("@StartDateFilter", "AND Date >= @StartDate");
                        parameters.Add("@StartDate", startDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@StartDateFilter", "");
                    }

                    if (endDate.HasValue)
                    {
                        sql = sql.Replace("@EndDateFilter", "AND Date <= @EndDate");
                        parameters.Add("@EndDate", endDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@EndDateFilter", "");
                    }

                    return (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Receipt> GetReceiptByNumberAsync(decimal receiptNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM Daily WHERE RECPT = @ReceiptNumber";
                    var parameters = new { ReceiptNumber = receiptNumber };
                    return await connection.QueryFirstOrDefaultAsync<Receipt>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptByNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Receipt> SaveReceiptAsync(Receipt receipt)
        {
            try
            {
                if (receipt.ReceiptNumber == 0)
                {
                    receipt.ReceiptNumber = await GetNextReceiptNumberAsync();
                }

                // Validate string lengths
                if (receipt.Depot?.Length > 1) throw new ArgumentException("Depot exceeds maximum length of 1.");
                if (receipt.Product?.Length > 2) throw new ArgumentException("Product exceeds maximum length of 2.");
                if (receipt.Process?.Length > 2) throw new ArgumentException("Process exceeds maximum length of 2.");
                if (receipt.PrNote1?.Length > 50) throw new ArgumentException("PrNote1 exceeds maximum length of 50.");
                if (receipt.NpNote1?.Length > 50) throw new ArgumentException("NpNote1 exceeds maximum length of 50.");
                if (receipt.FromField?.Length > 10) throw new ArgumentException("FromField exceeds maximum length of 10.");
                if (receipt.ContainerErrors?.Length > 10) throw new ArgumentException("ContainerErrors exceeds maximum length of 10.");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO Daily (
                            DEPOT, PRODUCT, RECPT, NUMBER, GROSS, TARE, NET, 
                            GRADE, PROCESS, DATE, DAY_UNIQ, IMP_BAT, FIN_BAT,
                            DOCK_PCT, ISVOID, THEPRICE, PRICESRC, PR_NOTE1,
                            NP_NOTE1, FROM_FIELD, IMPORTED, CONT_ERRS
                        ) VALUES (
                            @Depot, @Product, @ReceiptNumber, @GrowerNumber, @Gross, @Tare, @Net,
                            @Grade, @Process, @Date, @DayUniq, @ImpBatch, @FinBatch,
                            @DockPercent, @IsVoid, @ThePrice, @PriceSource, @PrNote1,
                            @NpNote1, @FromField, @Imported, @ContainerErrors
                        )";

                    var parameters = new
                    {
                        receipt.Depot,
                        receipt.Product,
                        receipt.ReceiptNumber,
                        receipt.GrowerNumber,
                        receipt.Gross,
                        receipt.Tare,
                        receipt.Net,
                        receipt.Grade,
                        receipt.Process,
                        receipt.Date,
                        receipt.DayUniq,
                        receipt.ImpBatch,
                        receipt.FinBatch,
                        receipt.DockPercent,
                        receipt.IsVoid,
                        receipt.ThePrice,
                        receipt.PriceSource,
                        receipt.PrNote1,
                        receipt.NpNote1,
                        receipt.FromField,
                        receipt.Imported,
                        receipt.ContainerErrors
                    };

                    await connection.ExecuteAsync(sql, parameters);
                    return receipt;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveReceiptAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteReceiptAsync(decimal receiptNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "DELETE FROM Daily WHERE RECPT = @ReceiptNumber";
                    var parameters = new { ReceiptNumber = receiptNumber };
                    var result = await connection.ExecuteAsync(sql, parameters);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteReceiptAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VoidReceiptAsync(decimal receiptNumber, string reason)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Daily 
                        SET ISVOID = 1, 
                            EDIT_DATE = GETDATE(),
                            EDIT_REAS = @Reason
                        WHERE RECPT = @ReceiptNumber";

                    var parameters = new { ReceiptNumber = receiptNumber, Reason = reason };
                    var result = await connection.ExecuteAsync(sql, parameters);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VoidReceiptAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Receipt>> GetReceiptsByGrowerAsync(decimal growerNumber, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT * FROM Daily 
                        WHERE NUMBER = @GrowerNumber
                        @StartDateFilter
                        @EndDateFilter
                        ORDER BY Date DESC, ReceiptNumber DESC";

                    var parameters = new DynamicParameters();
                    parameters.Add("@GrowerNumber", growerNumber);

                    if (startDate.HasValue)
                    {
                        sql = sql.Replace("@StartDateFilter", "AND Date >= @StartDate");
                        parameters.Add("@StartDate", startDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@StartDateFilter", "");
                    }

                    if (endDate.HasValue)
                    {
                        sql = sql.Replace("@EndDateFilter", "AND Date <= @EndDate");
                        parameters.Add("@EndDate", endDate.Value);
                    }
                    else
                    {
                        sql = sql.Replace("@EndDateFilter", "");
                    }

                    return (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptsByGrowerAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Receipt>> GetReceiptsByImportBatchAsync(decimal impBatch)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM Daily WHERE IMP_BAT = @ImpBatch ORDER BY Date DESC, RECPT DESC";
                    var parameters = new { ImpBatch = impBatch };
                    return (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptsByImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<decimal> GetNextReceiptNumberAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT MAX(RECPT) + 1 FROM Daily";
                    var result = await connection.ExecuteScalarAsync<decimal?>(sql);
                    return result ?? 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetNextReceiptNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateReceiptAsync(Receipt receipt)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if receipt number exists
                    var existingReceipt = await GetReceiptByNumberAsync(receipt.ReceiptNumber);
                    if (existingReceipt != null)
                    {
                        return false;
                    }

                    // Validate grower exists
                    var growerCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Grower WHERE NUMBER = @GrowerNumber",
                        new { receipt.GrowerNumber });
                    if (growerCount == 0)
                    {
                        return false;
                    }

                    // Validate product exists
                    var productCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Product WHERE PRODUCT = @Product",
                        new { receipt.Product });
                    if (productCount == 0)
                    {
                        return false;
                    }

                    // Validate process exists
                    var processCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Process WHERE PROCESS = @Process",
                        new { receipt.Process });
                    if (processCount == 0)
                    {
                        return false;
                    }                 

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ValidateReceiptAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<decimal> CalculateNetWeightAsync(Receipt receipt)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT SUM(TARE) as TotalTare 
                        FROM Contain 
                        WHERE CONTAINER IN (
                            SELECT DISTINCT CONTAINER 
                            FROM Daily 
                            WHERE RECPT = @ReceiptNumber
                        )";

                    var totalTare = await connection.ExecuteScalarAsync<decimal?>(sql, new { receipt.ReceiptNumber }) ?? 0;
                    return receipt.Gross - totalTare;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CalculateNetWeightAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<decimal> GetPriceForReceiptAsync(Receipt receipt)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT TOP 1 THEPRICE 
                        FROM Price 
                        WHERE PRODUCT = @Product 
                        AND PROCESS = @Process 
                        AND FROM_DATE <= @Date 
                        ORDER BY FROM_DATE DESC";

                    var price = await connection.ExecuteScalarAsync<decimal?>(sql, receipt);
                    return price ?? 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetPriceForReceiptAsync: {ex.Message}");
                throw;
            }
        }
    }
} 