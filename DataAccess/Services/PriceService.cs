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
        // This implementation now dynamically builds the column name based on:
        // - Currency: C (Canadian) or U (US)
        // - Level: L1, L2, L3 (grower's price level/status)
        // - Grade: G1, G2, G3
        // - Advance: A1, A2, A3 (or FN for final)

        /// <summary>
        /// Builds the dynamic price column name based on grower and receipt attributes.
        /// Mirrors the XBase logic from PRICEFND.PRG - varAdvance() function.
        /// </summary>
        /// <param name="currency">Currency code: 'C' for Canadian, 'U' for US</param>
        /// <param name="priceLevel">Price level (1-3), typically grower.status in legacy</param>
        /// <param name="grade">Grade (1-3)</param>
        /// <param name="advanceNumber">Advance number (1-3), or 0 for final</param>
        /// <returns>Column name like "CL1G2A1" or "UL3G1FN"</returns>
        private string BuildPriceColumnName(char currency, int priceLevel, decimal grade, int advanceNumber)
        {
            var columnName = string.Empty;

            // 1. Currency prefix (C or U)
            if (currency == 'C' || currency == 'c')
            {
                columnName = "C";
            }
            else if (currency == 'U' || currency == 'u')
            {
                columnName = "U";
            }
            else
            {
                Logger.Warn($"Invalid currency '{currency}', defaulting to 'C' (Canadian)");
                columnName = "C"; // Default to Canadian
            }

            // 2. Price Level (L1, L2, L3)
            if (priceLevel >= 1 && priceLevel <= 3)
            {
                columnName += $"L{priceLevel}";
            }
            else
            {
                Logger.Warn($"Invalid price level {priceLevel}, defaulting to L1");
                columnName += "L1"; // Default to level 1
            }

            // 3. Grade (G1, G2, G3)
            int gradeInt = (int)grade;
            if (gradeInt >= 1 && gradeInt <= 3)
            {
                columnName += $"G{gradeInt}";
            }
            else
            {
                Logger.Warn($"Invalid grade {grade}, defaulting to G1");
                columnName += "G1"; // Default to grade 1
            }

            // 4. Advance or Final (A1, A2, A3, or FN)
            if (advanceNumber >= 1 && advanceNumber <= 3)
            {
                columnName += $"A{advanceNumber}";
            }
            else if (advanceNumber == 0)
            {
                columnName += "FN"; // Final payment
            }
            else
            {
                Logger.Warn($"Invalid advance number {advanceNumber}, defaulting to A1");
                columnName += "A1"; // Default to advance 1
            }

            Logger.Info($"Built price column name: {columnName} (Currency={currency}, Level={priceLevel}, Grade={grade}, Advance={advanceNumber})");
            return columnName;
        }

        public async Task<decimal> GetAdvancePriceAsync(string productId, string processId, DateTime receiptDate, int advanceNumber, char growerCurrency, int growerPriceLevel, decimal grade)
        {
            if (advanceNumber < 1 || advanceNumber > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), "Advance number must be 1, 2, or 3.");
            }

            // Build the dynamic column name based on grower and receipt attributes
            string priceColumn = BuildPriceColumnName(growerCurrency, growerPriceLevel, grade, advanceNumber);

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
                    
                    if (price == null)
                    {
                        Logger.Warn($"No price found for Product={productId}, Process={processId}, Date={receiptDate:yyyy-MM-dd}, Column={priceColumn}");
                    }
                    
                    return price ?? 0; // Return 0 if no price found
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance price {advanceNumber} (Column: {priceColumn}) for Product: {productId}, Process: {processId}, Date: {receiptDate}: {ex.Message}", ex);
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

        public async Task<decimal> GetTimePremiumAsync(string productId, string processId, DateTime receiptDate, TimeSpan receiptTime, char growerCurrency)
        {
             // The XBase++ code checked Price->TIMEPREM and used Price->CPREMIUM or Price->UPREMIUM
             // This requires finding the correct Price record first.
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                     var sql = @"
                        SELECT TOP 1 TIMEPREM, CPREMIUM, UPREMIUM
                        FROM Price
                        WHERE PRODUCT = @ProductId
                          AND PROCESS = @ProcessId
                          AND [FROM] <= @ReceiptDate
                        ORDER BY [FROM] DESC, TIME DESC";

                    var priceRecord = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { ProductId = productId, ProcessId = processId, ReceiptDate = receiptDate });

                    if (priceRecord != null && priceRecord.TIMEPREM == true)
                    {
                        // Select premium based on grower currency
                        if (growerCurrency == 'C' || growerCurrency == 'c')
                        {
                            return priceRecord.CPREMIUM ?? 0;
                        }
                        else if (growerCurrency == 'U' || growerCurrency == 'u')
                        {
                            return priceRecord.UPREMIUM ?? 0;
                        }
                        else
                        {
                            Logger.Warn($"Invalid currency '{growerCurrency}' for time premium, defaulting to Canadian premium");
                            return priceRecord.CPREMIUM ?? 0;
                        }
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
