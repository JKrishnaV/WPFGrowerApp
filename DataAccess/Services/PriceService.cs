using System;
using System.Collections.Generic;
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
                    if(priceId == null)
                        Logger.Warn($"No PriceRecordId found for Product: {productId}, Process: {processId}, Date: {receiptDate}");
                    return priceId ?? 0;
                 }
             }
             catch (Exception ex)
             {
                 Logger.Error($"Error finding PriceRecordId for Product: {productId}, Process: {processId}, Date: {receiptDate}: {ex.Message}", ex);
                 throw;
             }
         }

        public async Task<IEnumerable<Price>> GetAllAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<Price>("SELECT * FROM Price");
            }
        }

        public async Task<Price> GetByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QuerySingleOrDefaultAsync<Price>("SELECT * FROM Price WHERE Id = @Id", new { Id = id });
            }
        }

        public async Task<int> CreateAsync(Price price)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "INSERT INTO Price (Product, Process, [From], TimePrem, Time, CPremium, UPremium, ADV1_USED, ADV2_USED, ADV3_USED, FIN_USED, CL1G1A1, CL1G1A2, CL1G1A3, CL1G1FN, CL1G2A1, CL1G2A2, CL1G2A3, CL1G2FN, CL1G3A1, CL1G3A2, CL1G3A3, CL1G3FN, CL2G1A1, CL2G1A2, CL2G1A3, CL2G1FN, CL2G2A1, CL2G2A2, CL2G2A3, CL2G2FN, CL2G3A1, CL2G3A2, CL2G3A3, CL2G3FN, CL3G1A1, CL3G1A2, CL3G1A3, CL3G1FN, CL3G2A1, CL3G2A2, CL3G2A3, CL3G2FN, CL3G3A1, CL3G3A2, CL3G3A3, CL3G3FN, UL1G1A1, UL1G1A2, UL1G1A3, UL1G1FN, UL1G2A1, UL1G2A2, UL1G2A3, UL1G2FN, UL1G3A1, UL1G3A2, UL1G3A3, UL1G3FN, UL2G1A1, UL2G1A2, UL2G1A3, UL2G1FN, UL2G2A1, UL2G2A2, UL2G2A3, UL2G2FN, UL2G3A1, UL2G3A2, UL2G3A3, UL2G3FN, UL3G1A1, UL3G1A2, UL3G1A3, UL3G1FN, UL3G2A1, UL3G2A2, UL3G2A3, UL3G2FN, UL3G3A1, UL3G3A2, UL3G3A3, UL3G3FN) VALUES (@Product, @Process, @From, @TimePrem, @Time, @CPremium, @UPremium, @Adv1Used, @Adv2Used, @Adv3Used, @FinUsed, @CL1G1A1, @CL1G1A2, @CL1G1A3, @CL1G1FN, @CL1G2A1, @CL1G2A2, @CL1G2A3, @CL1G2FN, @CL1G3A1, @CL1G3A2, @CL1G3A3, @CL1G3FN, @CL2G1A1, @CL2G1A2, @CL2G1A3, @CL2G1FN, @CL2G2A1, @CL2G2A2, @CL2G2A3, @CL2G2FN, @CL2G3A1, @CL2G3A2, @CL2G3A3, @CL2G3FN, @CL3G1A1, @CL3G1A2, @CL3G1A3, @CL3G1FN, @CL3G2A1, @CL3G2A2, @CL3G2A3, @CL3G2FN, @CL3G3A1, @CL3G3A2, @CL3G3A3, @CL3G3FN, @UL1G1A1, @UL1G1A2, @UL1G1A3, @UL1G1FN, @UL1G2A1, @UL1G2A2, @UL1G2A3, @UL1G2FN, @UL1G3A1, @UL1G3A2, @UL1G3A3, @UL1G3FN, @UL2G1A1, @UL2G1A2, @UL2G1A3, @UL2G1FN, @UL2G2A1, @UL2G2A2, @UL2G2A3, @UL2G2FN, @UL2G3A1, @UL2G3A2, @UL2G3A3, @UL2G3FN, @UL3G1A1, @UL3G1A2, @UL3G1A3, @UL3G1FN, @UL3G2A1, @UL3G2A2, @UL3G2A3, @UL3G2FN, @UL3G3A1, @UL3G3A2, @UL3G3A3, @UL3G3FN); SELECT CAST(SCOPE_IDENTITY() as int)";
                return await connection.ExecuteScalarAsync<int>(sql, price);
            }
        }

        public async Task<bool> UpdateAsync(Price price)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "UPDATE Price SET Product = @Product, Process = @Process, [From] = @From, TimePrem = @TimePrem, Time = @Time, CPremium = @CPremium, UPremium = @UPremium, ADV1_USED = @Adv1Used, ADV2_USED = @Adv2Used, ADV3_USED = @Adv3Used, FIN_USED = @FinUsed, CL1G1A1 = @CL1G1A1, CL1G1A2 = @CL1G1A2, CL1G1A3 = @CL1G1A3, CL1G1FN = @CL1G1FN, CL1G2A1 = @CL1G2A1, CL1G2A2 = @CL1G2A2, CL1G2A3 = @CL1G2A3, CL1G2FN = @CL1G2FN, CL1G3A1 = @CL1G3A1, CL1G3A2 = @CL1G3A2, CL1G3A3 = @CL1G3A3, CL1G3FN = @CL1G3FN, CL2G1A1 = @CL2G1A1, CL2G1A2 = @CL2G1A2, CL2G1A3 = @CL2G1A3, CL2G1FN = @CL2G1FN, CL2G2A1 = @CL2G2A1, CL2G2A2 = @CL2G2A2, CL2G2A3 = @CL2G2A3, CL2G2FN = @CL2G2FN, CL2G3A1 = @CL2G3A1, CL2G3A2 = @CL2G3A2, CL2G3A3 = @CL2G3A3, CL2G3FN = @CL2G3FN, CL3G1A1 = @CL3G1A1, CL3G1A2 = @CL3G1A2, CL3G1A3 = @CL3G1A3, CL3G1FN = @CL3G1FN, CL3G2A1 = @CL3G2A1, CL3G2A2 = @CL3G2A2, CL3G2A3 = @CL3G2A3, CL3G2FN = @CL3G2FN, CL3G3A1 = @CL3G3A1, CL3G3A2 = @CL3G3A2, CL3G3A3 = @CL3G3A3, CL3G3FN = @CL3G3FN, UL1G1A1 = @UL1G1A1, UL1G1A2 = @UL1G1A2, UL1G1A3 = @UL1G1A3, UL1G1FN = @UL1G1FN, UL1G2A1 = @UL1G2A1, UL1G2A2 = @UL1G2A2, UL1G2A3 = @UL1G2A3, UL1G2FN = @UL1G2FN, UL1G3A1 = @UL1G3A1, UL1G3A2 = @UL1G3A2, UL1G3A3 = @UL1G3A3, UL1G3FN = @UL1G3FN, UL2G1A1 = @UL2G1A1, UL2G1A2 = @UL2G1A2, UL2G1A3 = @UL2G1A3, UL2G1FN = @UL2G1FN, UL2G2A1 = @UL2G2A1, UL2G2A2 = @UL2G2A2, UL2G2A3 = @UL2G2A3, UL2G2FN = @UL2G2FN, UL2G3A1 = @UL2G3A1, UL2G3A2 = @UL2G3A2, UL2G3A3 = @UL2G3A3, UL2G3FN = @UL2G3FN, UL3G1A1 = @UL3G1A1, UL3G1A2 = @UL3G1A2, UL3G1A3 = @UL3G1A3, UL3G1FN = @UL3G1FN, UL3G2A1 = @UL3G2A1, UL3G2A2 = @UL3G2A2, UL3G2A3 = @UL3G2A3, UL3G2FN = @UL3G2FN, UL3G3A1 = @UL3G3A1, UL3G3A2 = @UL3G3A2, UL3G3A3 = @UL3G3A3, UL3G3FN = @UL3G3FN WHERE PriceID = @PriceID";
                var rowsAffected = await connection.ExecuteAsync(sql, price);
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sql = "DELETE FROM Price WHERE PriceID = @PriceID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { PriceID = id });
                return rowsAffected > 0;
            }
        }
    }
}
