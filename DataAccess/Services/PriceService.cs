using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models; // May need Price model if created
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class PriceService : BaseDatabaseService, IPriceService
    {
        // Note: The Price table structure is complex with many columns (CL1G1A1, UL1G1A1 etc.)
        // The exact logic to select the correct column based on grade/level might need refinement
        // based on deeper analysis or specific business rules not fully captured yet.
        // This initial implementation focuses on getting *a* price based on advance number.

        public async Task<decimal> GetAdvancePriceAsync(string productId, string processId, DateTime receiptDate, int advanceNumber)
        {
            if (advanceNumber < 1 || advanceNumber > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), "Advance number must be 1, 2, or 3.");
            }

            // Simplified logic: Selects the first relevant price column for the advance.
            // TODO: Refine column selection based on detailed business rules (Grade, Level etc.)
            // This likely requires a more complex query or mapping logic.
            // For now, we assume CL1G1A1 for Adv1, CL1G1A2 for Adv2, CL1G1A3 for Adv3 as placeholders.
            string priceColumn;
            switch (advanceNumber)
            {
                case 1: priceColumn = "CL1G1A1"; break; // Placeholder
                case 2: priceColumn = "CL1G1A2"; break; // Placeholder
                case 3: priceColumn = "CL1G1A3"; break; // Placeholder
                default: return 0; // Should not happen due to check above
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Find the most recent price record effective on or before the receipt date
                    var sql = $@"
                        SELECT TOP 1 {priceColumn}
                        FROM Price
                        WHERE PRODUCT = @ProductId
                          AND PROCESS = @ProcessId
                          AND [FROM] <= @ReceiptDate
                        ORDER BY [FROM] DESC, TIME DESC"; // Order by date then time

                    var price = await connection.ExecuteScalarAsync<decimal?>(sql, new { ProductId = productId, ProcessId = processId, ReceiptDate = receiptDate });
                    return price ?? 0; // Return 0 if no price found
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance price {advanceNumber} for Product: {productId}, Process: {processId}, Date: {receiptDate}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<decimal> GetMarketingDeductionAsync(string productId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT DEDUCT FROM Product WHERE PRODUCT = @ProductId";
                    var deduction = await connection.ExecuteScalarAsync<decimal?>(sql, new { ProductId = productId });
                    return deduction ?? 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting marketing deduction for Product: {productId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<decimal> GetTimePremiumAsync(string productId, string processId, DateTime receiptDate, TimeSpan receiptTime)
        {
             // The XBase++ code checked Price->TIMEPREM and used Price->CPREMIUM or Price->UPREMIUM
             // This requires finding the correct Price record first.
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                     var sql = @"
                        SELECT TOP 1 TIMEPREM, CPREMIUM -- Assuming CPREMIUM for now, might need UPREMIUM logic
                        FROM Price
                        WHERE PRODUCT = @ProductId
                          AND PROCESS = @ProcessId
                          AND [FROM] <= @ReceiptDate
                        ORDER BY [FROM] DESC, TIME DESC";

                    var priceRecord = await connection.QueryFirstOrDefaultAsync(sql, new { ProductId = productId, ProcessId = processId, ReceiptDate = receiptDate });

                    if (priceRecord != null && priceRecord.TIMEPREM == true)
                    {
                        // Further logic might be needed to compare receiptTime with Price.TIME
                        // For simplicity now, return CPREMIUM if TIMEPREM is true
                        return priceRecord.CPREMIUM ?? 0;
                    }
                    return 0; // No premium applicable
                }
            }
            catch (Exception ex)
            {
                 Logger.Error($"Error getting time premium for Product: {productId}, Process: {processId}, Date: {receiptDate}: {ex.Message}", ex);
                 throw;
            }
        }

         public async Task<bool> MarkAdvancePriceAsUsedAsync(decimal priceId, int advanceNumber)
         {
             if (advanceNumber < 1 || advanceNumber > 3 || priceId <= 0)
             {
                 return false; // Invalid input
             }

             string usedColumn;
             switch (advanceNumber)
             {
                 case 1: usedColumn = "ADV1_USED"; break;
                 case 2: usedColumn = "ADV2_USED"; break;
                 case 3: usedColumn = "ADV3_USED"; break;
                 default: return false;
             }

             try
             {
                 using (var connection = new SqlConnection(_connectionString))
                 {
                     await connection.OpenAsync();
                     var sql = $"UPDATE Price SET {usedColumn} = 1 WHERE PRICEID = @PriceId AND {usedColumn} = 0"; // Only update if not already used
                     int rowsAffected = await connection.ExecuteAsync(sql, new { PriceId = priceId });
                     return rowsAffected > 0; // Return true if the record was updated
                 }
             }
             catch (Exception ex)
             {
                 Logger.Error($"Error marking advance {advanceNumber} as used for PriceID: {priceId}: {ex.Message}", ex);
                 throw;
             }
         }

         public async Task<decimal> FindPriceRecordIdAsync(string productId, string processId, DateTime receiptDate)
         {
             try
             {
                 using (var connection = new SqlConnection(_connectionString))
                 {
                     await connection.OpenAsync();
                     var sql = @"
                        SELECT TOP 1 PRICEID
                        FROM Price
                        WHERE PRODUCT = @ProductId
                          AND PROCESS = @ProcessId
                          AND [FROM] <= @ReceiptDate
                        ORDER BY [FROM] DESC, TIME DESC";

                     var priceId = await connection.ExecuteScalarAsync<decimal?>(sql, new { ProductId = productId, ProcessId = processId, ReceiptDate = receiptDate });
                     return priceId ?? 0;
                 }
             }
             catch (Exception ex)
             {
                 Logger.Error($"Error finding PriceRecordId for Product: {productId}, Process: {processId}, Date: {receiptDate}: {ex.Message}", ex);
                 throw;
             }
         }
    }
}
