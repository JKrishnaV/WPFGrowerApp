using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using System.Linq;
using WPFGrowerApp.Infrastructure.Logging; // Added for Logger

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
                if (receipt == null) throw new ArgumentNullException(nameof(receipt));

                if (receipt.ReceiptNumber == 0)
                {
                    receipt.ReceiptNumber = await GetNextReceiptNumberAsync();
                }

                // --- Derive fields based on CSV data ---
                // Process (first 2 chars of GradeId)
                string derivedProcess = (!string.IsNullOrEmpty(receipt.GradeId) && receipt.GradeId.Length >= 2)
                                        ? receipt.GradeId.Substring(0, 2)
                                        : string.Empty; // Or handle error/default

                // Grade (last char of GradeId)
                decimal derivedGrade = 0;
                if (!string.IsNullOrEmpty(receipt.GradeId) && receipt.GradeId.Length > 2)
                {
                    if (decimal.TryParse(receipt.GradeId.Substring(receipt.GradeId.Length - 1), out var parsedGrade))
                    {
                        derivedGrade = parsedGrade;
                    }
                    // Else: handle error or default to 0
                }

                // IsVoid (based on Voided string)
                bool derivedIsVoid = !string.IsNullOrEmpty(receipt.Voided); // Assuming any non-empty string means voided

                // Time (first 5 chars of TimeIn)
                string derivedTime = (!string.IsNullOrEmpty(receipt.TimeIn) && receipt.TimeIn.Length >= 5)
                                     ? receipt.TimeIn.Substring(0, 5)
                                     : "00:00"; // Or handle error/default

                // Container Errors string
                string derivedContErrs = string.Join(",", receipt.ContainerData?.Select(c => c.Type).Distinct() ?? Enumerable.Empty<string>());
                if (derivedContErrs.Length > 10) derivedContErrs = derivedContErrs.Substring(0, 10); // Truncate if needed

                // Gross and Tare (set to 0 as per sample)
                decimal derivedGross = 0;
                decimal derivedTare = 0;

                // --- Prepare Container Parameters ---
                var containerParams = new DynamicParameters();
                for (int i = 0; i < 20; i++)
                {
                    if (i < receipt.ContainerData?.Count)
                    {
                        containerParams.Add($"@IN{i + 1}", receipt.ContainerData[i].InCount);
                        containerParams.Add($"@OUT{i + 1}", receipt.ContainerData[i].OutCount);
                    }
                    else
                    {
                        containerParams.Add($"@IN{i + 1}", 0); // Default to 0 if no data
                        containerParams.Add($"@OUT{i + 1}", 0);
                    }
                }

                // --- Validate string lengths (using derived/mapped values) ---
                if (receipt.Depot?.Length > 1) throw new ArgumentException("Depot exceeds maximum length of 1.");
                if (receipt.Product?.Length > 2) throw new ArgumentException("Product exceeds maximum length of 2.");
                if (derivedProcess?.Length > 2) throw new ArgumentException("Derived Process exceeds maximum length of 2."); // Validate derived
                if (receipt.PrNote1?.Length > 50) throw new ArgumentException("PrNote1 exceeds maximum length of 50.");
                if (receipt.NpNote1?.Length > 50) throw new ArgumentException("NpNote1 exceeds maximum length of 50.");
                if (receipt.FromField?.Length > 10) throw new ArgumentException("FromField exceeds maximum length of 10.");
                if (derivedContErrs?.Length > 10) throw new ArgumentException("Derived ContainerErrors exceeds maximum length of 10."); // Validate derived
                if (receipt.AddBy?.Length > 10) throw new ArgumentException("AddBy exceeds maximum length of 10."); // Added validation
                if (receipt.EditBy?.Length > 10) throw new ArgumentException("EditBy exceeds maximum length of 10."); // Added validation
                if (receipt.EditReason?.Length > 20) throw new ArgumentException("EditReason exceeds maximum length of 20."); // Added validation


                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Build dynamic SQL for INSERT
                    var columns = new List<string>
                    {
                        "DEPOT", "PRODUCT", "RECPT", "NUMBER", "GROSS", "TARE", "NET",
                        "GRADE", "PROCESS", "DATE", "TIME", "DAY_UNIQ", "IMP_BAT", "FIN_BAT",
                        "DOCK_PCT", "ISVOID", "THEPRICE", "PRICESRC", "PR_NOTE1",
                        "NP_NOTE1", "FROM_FIELD", "IMPORTED", "CONT_ERRS",
                        "ADD_DATE", "ADD_BY", "EDIT_DATE", "EDIT_BY", "EDIT_REAS"
                    };
                    var values = new List<string>
                    {
                        "@Depot", "@Product", "@ReceiptNumber", "@GrowerNumber", "@Gross", "@Tare", "@Net",
                        "@Grade", "@Process", "@Date", "@Time", "@DayUniq", "@ImpBatch", "@FinBatch",
                        "@DockPercent", "@IsVoid", "@ThePrice", "@PriceSource", "@PrNote1",
                        "@NpNote1", "@FromField", "@Imported", "@ContErrs",
                        "@AddDate", "@AddBy", "@EditDate", "@EditBy", "@EditReason"
                    };

                    // Add container columns dynamically
                    for (int i = 1; i <= 20; i++)
                    {
                        columns.Add($"IN{i}");
                        values.Add($"@IN{i}");
                        columns.Add($"OUT{i}");
                        values.Add($"@OUT{i}");
                    }

                    var sql = $"INSERT INTO Daily ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

                    // Combine parameters
                    var parameters = new DynamicParameters(new
                    {
                        receipt.Depot,
                        receipt.Product,
                        receipt.ReceiptNumber,
                        receipt.GrowerNumber,
                        Gross = derivedGross, // Use derived
                        Tare = derivedTare,   // Use derived
                        receipt.Net,
                        Grade = derivedGrade, // Use derived
                        Process = derivedProcess, // Use derived
                        receipt.Date,
                        Time = derivedTime,     // Use derived
                        receipt.DayUniq,
                        receipt.ImpBatch,
                        receipt.FinBatch,
                        receipt.DockPercent,
                        IsVoid = derivedIsVoid, // Use derived
                        receipt.ThePrice,
                        receipt.PriceSource,
                        receipt.PrNote1,
                        receipt.NpNote1,
                        receipt.FromField,
                        receipt.Imported,
                        ContErrs = derivedContErrs, // Use derived
                        receipt.AddDate,            // Use mapped
                        receipt.AddBy,              // Use mapped
                        receipt.EditDate,           // Use mapped
                        receipt.EditBy,             // Use mapped
                        receipt.EditReason          // Use mapped
                    });
                    parameters.AddDynamicParams(containerParams); // Add container parameters

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
                    // Select a default price column (e.g., CL1G1A1) instead of THEPRICE
                    // TODO: Determine the correct default price column based on business rules if needed.
                    var sql = @"
                        SELECT TOP 1 CL1G1A1
                        FROM Price
                        WHERE PRODUCT = @Product
                        AND PROCESS = @Process
                        AND [FROM] <= @Date -- Escaped column name
                        ORDER BY [FROM] DESC"; // Removed SQL comment

                    var price = await connection.ExecuteScalarAsync<decimal?>(sql, new { receipt.Product, receipt.Process, receipt.Date }); // Pass parameters explicitly
                    return price ?? 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetPriceForReceiptAsync: {ex.Message}");
                 throw;
            }
        }

        public async Task<bool> UpdateReceiptAdvanceDetailsAsync(
            decimal receiptNumber,
            int advanceNumber,
            decimal postBatchId,
            decimal advancePrice,
            decimal priceRecordId,
            decimal premiumPrice)
        {
            if (advanceNumber < 1 || advanceNumber > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), "Advance number must be 1, 2, or 3.");
            }

            string postBatchColumn;
            string advPriceColumn;
            string advPriceIdColumn;

            switch (advanceNumber)
            {
                case 1:
                    postBatchColumn = "POST_BAT1";
                    advPriceColumn = "ADV_PR1";
                    advPriceIdColumn = "ADV_PRID1";
                    break;
                case 2:
                    postBatchColumn = "POST_BAT2";
                    advPriceColumn = "ADV_PR2";
                    advPriceIdColumn = "ADV_PRID2";
                    break;
                case 3:
                    postBatchColumn = "POST_BAT3";
                    advPriceColumn = "ADV_PR3";
                    advPriceIdColumn = "ADV_PRID3";
                    break;
                default: return false; // Should not happen
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Update the specific advance columns and the premium price (if applicable for first advance)
                    // Also update LAST_ADVPB as seen in XBase++ code
                    var sql = $@"
                        UPDATE Daily
                        SET {postBatchColumn} = @PostBatchId,
                            {advPriceColumn} = @AdvancePrice,
                            {advPriceIdColumn} = @PriceRecordId,
                            LAST_ADVPB = @PostBatchId
                            {(advanceNumber == 1 ? ", PREM_PRICE = @PremiumPrice" : "")}
                        WHERE RECPT = @ReceiptNumber
                          AND {postBatchColumn} = 0"; // Ensure we don't overwrite existing batch info

                    var parameters = new DynamicParameters();
                    parameters.Add("@ReceiptNumber", receiptNumber);
                    parameters.Add("@PostBatchId", postBatchId);
                    parameters.Add("@AdvancePrice", advancePrice);
                    parameters.Add("@PriceRecordId", priceRecordId);
                    if (advanceNumber == 1)
                    {
                        parameters.Add("@PremiumPrice", premiumPrice);
                    }

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0; // Return true if the record was updated
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating advance details for Receipt: {receiptNumber}, Advance: {advanceNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<Receipt>> GetReceiptsForAdvancePaymentAsync(
            int advanceNumber,
            DateTime cutoffDate,
            decimal? includeGrowerId = null,
            string includePayGroup = null,
            decimal? excludeGrowerId = null,
            string excludePayGroup = null,
            string productId = null,
            string processId = null)
        {
             if (advanceNumber < 1 || advanceNumber > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), "Advance number must be 1, 2, or 3.");
            }

            string postBatchCheckColumn;
            switch (advanceNumber)
            {
                case 1: postBatchCheckColumn = "POST_BAT1"; break;
                case 2: postBatchCheckColumn = "POST_BAT2"; break;
                case 3: postBatchCheckColumn = "POST_BAT3"; break;
                default: return new List<Receipt>(); // Should not happen
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Base query selects from Daily and joins Grower
                    var sqlBuilder = new SqlBuilder();
                    var selector = sqlBuilder.AddTemplate(@"
                        SELECT d.*
                        FROM Daily d
                        INNER JOIN Grower g ON d.NUMBER = g.NUMBER
                        /**where**/
                        ORDER BY d.NUMBER, d.DATE, d.RECPT"
                    );

                    // Add mandatory conditions
                    sqlBuilder.Where("d.DATE <= @CutoffDate", new { CutoffDate = cutoffDate });
                    sqlBuilder.Where("d.FIN_BAT = 0"); // Not finalized
                    sqlBuilder.Where($"d.{postBatchCheckColumn} = 0"); // This specific advance not yet paid
                    sqlBuilder.Where("d.ISVOID = 0"); // Not voided
                    sqlBuilder.Where("g.ONHOLD = 0"); // Grower not on hold

                    // Add optional filters
                    if (includeGrowerId.HasValue)
                        sqlBuilder.Where("d.NUMBER = @IncludeGrowerId", new { IncludeGrowerId = includeGrowerId.Value });
                    if (excludeGrowerId.HasValue)
                        sqlBuilder.Where("d.NUMBER <> @ExcludeGrowerId", new { ExcludeGrowerId = excludeGrowerId.Value });
                    if (!string.IsNullOrEmpty(includePayGroup))
                        sqlBuilder.Where("g.PAYGRP = @IncludePayGroup", new { IncludePayGroup = includePayGroup });
                    if (!string.IsNullOrEmpty(excludePayGroup))
                        sqlBuilder.Where("g.PAYGRP <> @ExcludePayGroup", new { ExcludePayGroup = excludePayGroup });
                    if (!string.IsNullOrEmpty(productId))
                        sqlBuilder.Where("d.PRODUCT = @ProductId", new { ProductId = productId });
                    if (!string.IsNullOrEmpty(processId))
                        sqlBuilder.Where("d.PROCESS = @ProcessId", new { ProcessId = processId });

                    // Execute query
                    var results = await connection.QueryAsync<Receipt>(selector.RawSql, selector.Parameters);
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipts for advance payment {advanceNumber}: {ex.Message}", ex);
                throw;
            }
        }
    }
}
