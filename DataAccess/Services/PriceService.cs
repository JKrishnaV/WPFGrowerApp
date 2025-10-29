using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Modern pricing service that works with the normalized pricing matrix structure:
    /// PriceSchedules (header) -> PriceDetails (matrix) -> PriceClasses, PriceGrades, PriceAdvances, ProcessTypes
    /// </summary>
    public class PriceService : BaseDatabaseService, IPriceService
    {
        #region Helper Methods - Column Name Mapping

        /// <summary>
        /// Maps legacy column naming convention to modern dimension lookups
        /// Example: "CL2G3A1" = Canadian (C) Level 2 (L2) Grade 3 (G3) Advance 1 (A1)
        /// Returns: (ClassCode: "CL2", GradeNumber: 3, AdvanceCode: "A1")
        /// </summary>
        private (string ClassCode, int GradeNumber, string AdvanceCode) ParseLegacyColumnName(
            char currency, int priceLevel, decimal grade, int advanceNumber)
        {
            // Build class code (e.g., "CL1", "CL2", "CL3", "UL1", "UL2", "UL3")
            string currencyPrefix = (currency == 'C' || currency == 'c') ? "C" : "U";
            string classCode = $"{currencyPrefix}L{priceLevel}";
            
            // Grade number (1, 2, or 3)
            int gradeNumber = (int)grade;
            if (gradeNumber < 1 || gradeNumber > 3)
            {
                Logger.Warn($"Invalid grade {grade}, defaulting to 1");
                gradeNumber = 1;
            }
            
            // Advance code (A1, A2, A3, or FN)
            string advanceCode;
            if (advanceNumber >= 1 && advanceNumber <= 3)
            {
                advanceCode = $"A{advanceNumber}";
            }
            else if (advanceNumber == 0)
            {
                advanceCode = "FN"; // Final payment
            }
            else
            {
                Logger.Warn($"Invalid advance number {advanceNumber}, defaulting to A1");
                advanceCode = "A1";
            }
            
            return (classCode, gradeNumber, advanceCode);
        }

        /// <summary>
        /// Gets dimension IDs from codes/numbers
        /// </summary>
        private async Task<(int? ClassId, int? GradeId, int? AdvanceId)> GetDimensionIdsAsync(
            string classCode, int gradeNumber, string advanceCode)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var sql = @"
                    SELECT 
                        pc.PriceClassId AS ClassId,
                        pg.PriceGradeId AS GradeId,
                        pa.PriceAdvanceId AS AdvanceId
                    FROM 
                        (SELECT 1 AS Dummy) d
                    LEFT JOIN PriceClasses pc ON pc.ClassCode = @ClassCode
                    LEFT JOIN PriceGrades pg ON pg.GradeNumber = @GradeNumber
                    LEFT JOIN PriceAdvances pa ON pa.AdvanceCode = @AdvanceCode";
                
                var result = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    sql, 
                    new { ClassCode = classCode, GradeNumber = gradeNumber, AdvanceCode = advanceCode });
                
                return (result?.ClassId, result?.GradeId, result?.AdvanceId);
            }
        }

        #endregion

        #region Payment Calculation Methods

        public async Task<decimal> GetAdvancePriceAsync(
            string productId, 
            string processId, 
            DateTime receiptDate, 
            int advanceNumber, 
            char growerCurrency, 
            int growerPriceLevel, 
            decimal grade,
            decimal priceScheduleId)
        {
            if (advanceNumber < 1 || advanceNumber > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), "Advance number must be 1, 2, or 3.");
            }

            try
            {
                // Parse legacy naming to dimension codes
                var (classCode, gradeNumber, advanceCode) = ParseLegacyColumnName(
                    growerCurrency, growerPriceLevel, grade, advanceNumber);
                
                Logger.Info($"GetAdvancePriceAsync: Product={productId}, Process={processId}, " +
                           $"Date={receiptDate:d}, Advance={advanceNumber}, " +
                            $"Dimensions: Class={classCode}, Grade={gradeNumber}, Advance={advanceCode}");

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Query the modern pricing matrix
                    var sql = @"
                        SELECT PricePerPound
                        FROM PriceDetails
                        WHERE PriceScheduleId = @PriceSchudleId
                          AND PriceClassId = @PriceClassId
                          AND PriceGradeId = @PriceGradeId
                          AND PriceAdvanceId = @PriceAdvanceId";

                    var price = await connection.ExecuteScalarAsync<decimal?>(sql, new
                    {
                        PriceSchudleId = priceScheduleId,
                        PriceClassId = growerPriceLevel,
                        PriceGradeId = grade,
                        PriceAdvanceId = advanceNumber
                    });

                    //// Query the modern pricing matrix
                    //var sql = @"
                    //    SELECT TOP 1 pd.PricePerPound
                    //    FROM PriceSchedules ps
                    //    INNER JOIN Products p ON ps.ProductId = p.ProductId
                    //    INNER JOIN Processes pr ON ps.ProcessId = pr.ProcessId
                    //    INNER JOIN PriceDetails pd ON ps.PriceScheduleId = pd.PriceScheduleId
                    //    INNER JOIN PriceClasses pc ON pd.PriceClassId = pc.PriceClassId
                    //    INNER JOIN PriceGrades pg ON pd.PriceGradeId = pg.PriceGradeId
                    //    INNER JOIN PriceAdvances pa ON pd.PriceAdvanceId = pa.PriceAdvanceId
                    //    WHERE p.ProductCode = @ProductId
                    //      AND pr.ProcessCode = @ProcessId
                    //      AND ps.EffectiveFrom <= @ReceiptDate
                    //      AND (ps.EffectiveTo IS NULL OR ps.EffectiveTo >= @ReceiptDate)
                    //      AND pc.ClassCode = @ClassCode
                    //      AND pg.GradeNumber = @GradeNumber
                    //      AND pa.AdvanceCode = @AdvanceCode
                    //      AND pd.ProcessTypeId IS NULL  -- Generic pricing (fallback)
                    //    ORDER BY ps.EffectiveFrom DESC";

                    //var price = await connection.ExecuteScalarAsync<decimal?>(sql, new
                    //{
                    //    ProductId = productId,
                    //    ProcessId = processId,
                    //    ReceiptDate = receiptDate,
                    //    ClassCode = classCode,
                    //    GradeNumber = gradeNumber,
                    //    AdvanceCode = advanceCode
                    //});

                    if (price.HasValue)
                    {
                        Logger.Info($"Found price: ${price.Value:F4}/lb");
                        return price.Value;
                    }
                    else
                    {
                        Logger.Warn($"No price found for the specified criteria. Returning 0.");
                        return 0m;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance price: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<decimal> GetTimePremiumAsync(
            string productId, 
            string processId, 
            DateTime receiptDate, 
            TimeSpan receiptTime, 
            char growerCurrency)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT TOP 1 
                            ps.TimePremiumEnabled,
                            ISNULL(ps.CanadianPremiumAmount, 0) AS CanadianPremium,
                            ISNULL(ps.USPremiumAmount, 0) AS USPremium,
                            ps.PremiumCutoffTime
                        FROM PriceSchedules ps
                        INNER JOIN Products p ON ps.ProductId = p.ProductId
                        INNER JOIN Processes pr ON ps.ProcessId = pr.ProcessId
                        WHERE p.ProductCode = @ProductId
                          AND pr.ProcessCode = @ProcessId
                          AND ps.EffectiveFrom <= @ReceiptDate
                          AND (ps.EffectiveTo IS NULL OR ps.EffectiveTo >= @ReceiptDate)
                        ORDER BY ps.EffectiveFrom DESC";

                    var result = await connection.QuerySingleOrDefaultAsync<dynamic>(sql, new
                    {
                        ProductId = productId,
                        ProcessId = processId,
                        ReceiptDate = receiptDate
                    });

                    if (result != null)
                    {
                        bool enabled = result.TimePremiumEnabled;
                        // Select premium based on grower currency
                        decimal amount = (growerCurrency == 'C') 
                            ? (decimal)result.CanadianPremium 
                            : (decimal)result.USPremium;
                        TimeSpan? cutoffTime = result.PremiumCutoffTime;
                        
                        // Check if receipt qualifies for premium (before cutoff time)
                        if (enabled && cutoffTime.HasValue && receiptTime <= cutoffTime.Value)
                        {
                            Logger.Info($"Time premium applied: ${amount:F4} (Receipt at {receiptTime}, cutoff {cutoffTime.Value})");
                            return amount;
                        }
                        else
                        {
                            Logger.Info($"No time premium: enabled={enabled}, receipt time={receiptTime}, cutoff={cutoffTime}");
                            return 0m;
                        }
                    }
                    else
                    {
                        Logger.Warn("No price schedule found for time premium lookup");
                        return 0m;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting time premium: {ex.Message}", ex);
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
                    
                    // Query product for marketing deduction
                    var sql = @"
                        SELECT ISNULL(MarketingDeduction, 0) AS Deduction
                        FROM Products
                        WHERE ProductCode = @ProductId";
                    
                    var deduction = await connection.ExecuteScalarAsync<decimal?>(sql, new { ProductId = productId });
                    
                    if (deduction.HasValue)
                    {
                        Logger.Info($"Marketing deduction for {productId}: {deduction.Value:F4}");
                        return deduction.Value;
                    }
                    else
                    {
                        Logger.Warn($"No marketing deduction found for product {productId}");
                        return 0m;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting marketing deduction: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> MarkAdvancePriceAsUsedAsync(decimal priceId, int advanceNumber)
        {
            // NOTE: In the modern structure, we track usage via PriceScheduleLocks table
            // This method may need to be redesigned based on your locking strategy
            
            if (advanceNumber < 1 || advanceNumber > 3)
            {
                Logger.Warn($"Invalid advance number: {advanceNumber}");
                return false;
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Check if PriceScheduleLocks table exists and use it
                    var sql = @"
                        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PriceScheduleLocks')
                        BEGIN
                            IF NOT EXISTS (
                                SELECT 1 FROM PriceScheduleLocks 
                                WHERE PriceScheduleId = @PriceId 
                                  AND AdvanceNumber = @AdvanceNumber
                            )
                            BEGIN
                                INSERT INTO PriceScheduleLocks (PriceScheduleId, AdvanceNumber, LockedAt, LockedBy)
                                VALUES (@PriceId, @AdvanceNumber, GETDATE(), @LockedBy);
                                SELECT 1;
                            END
                            ELSE
                                SELECT 0;  -- Already locked
                        END
                        ELSE
                        BEGIN
                            -- Fallback: Just log the attempt
                            SELECT 1;
                        END";
                    
                    var lockedBy = App.CurrentUser?.Username ?? "SYSTEM";
                    var result = await connection.ExecuteScalarAsync<int>(sql, new
                    {
                        PriceId = priceId,
                        AdvanceNumber = advanceNumber,
                        LockedBy = lockedBy
                    });
                    
                    if (result > 0)
                    {
                        Logger.Info($"Marked advance {advanceNumber} as used for PriceScheduleId {priceId}");
                    }
                    else
                    {
                        Logger.Warn($"Advance {advanceNumber} already marked as used for PriceScheduleId {priceId}");
                    }
                    
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking advance as used: {ex.Message}", ex);
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
                        SELECT TOP 1 ps.PriceScheduleId
                        FROM PriceSchedules ps
                        INNER JOIN Products p ON ps.ProductId = p.ProductId
                        INNER JOIN Processes pr ON ps.ProcessId = pr.ProcessId
                        WHERE p.ProductCode = @ProductId
                          AND pr.ProcessCode = @ProcessId
                          AND ps.EffectiveFrom <= @ReceiptDate
                          AND (ps.EffectiveTo IS NULL OR ps.EffectiveTo >= @ReceiptDate)
                        ORDER BY ps.EffectiveFrom DESC";

                    // Guard against invalid SQL date
                    var safeDate = (receiptDate < new DateTime(1753, 1, 1)) ? new DateTime(2000, 1, 1) : receiptDate;
                    var priceId = await connection.ExecuteScalarAsync<int?>(sql, new
                    {
                        ProductId = productId,
                        ProcessId = processId,
                        ReceiptDate = safeDate
                    });

                    if (priceId.HasValue)
                    {
                        Logger.Info($"Found PriceScheduleId: {priceId.Value}");
                        return priceId.Value;
                    }
                    else
                    {
                        Logger.Warn($"No PriceScheduleId found for Product={productId}, Process={processId}, Date={receiptDate:d}");
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finding price record ID: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region CRUD Operations

        public async Task<IEnumerable<Price>> GetAllAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Query the price schedules with proper lock status checking
                var schedulesSql = @"
                    SELECT 
                        ps.PriceScheduleId AS PriceID,
                        ps.ProductId,
                        ps.ProcessId,
                        p.ProductCode AS Product,
                        pr.ProcessCode AS Process,
                        ps.EffectiveFrom AS [From],
                        ISNULL(ps.TimePremiumEnabled, 0) AS TimePrem,
                        ISNULL(CONVERT(VARCHAR(8), ps.PremiumCutoffTime, 108), '') AS Time,
                        ISNULL(ps.CanadianPremiumAmount, 0) AS CPremium,
                        ISNULL(ps.USPremiumAmount, 0) AS UPremium,
                        ps.CreatedAt AS QADD_TIME,
                        ps.CreatedBy AS QADD_OP,
                        ps.ModifiedAt AS QED_TIME,
                        ps.ModifiedBy AS QED_OP,
                        NULL AS QDEL_TIME,
                        NULL AS QDEL_OP,
                        -- Check lock status from PriceScheduleLocks table
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 1 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS Adv1Used,
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 2 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS Adv2Used,
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 3 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS Adv3Used,
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 4 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS FinUsed
                    FROM PriceSchedules ps
                    INNER JOIN Products p ON ps.ProductId = p.ProductId
                    INNER JOIN Processes pr ON ps.ProcessId = pr.ProcessId
                    WHERE (ps.EffectiveTo IS NULL OR ps.EffectiveTo >= CAST(GETDATE() AS DATE))
                    ORDER BY ps.EffectiveFrom DESC";
                
                var prices = (await connection.QueryAsync<Price>(schedulesSql)).ToList();
                
                // For each price schedule, load the key display price points (CL1 G1 - all areas)
                var detailsSql = @"
                    SELECT 
                        pd.PriceScheduleId,
                        pd.PricePerPound,
                        RTRIM(pc.ClassCode) AS ClassCode,
                        pg.GradeNumber,
                        RTRIM(pa.AdvanceCode) AS AdvanceCode
                    FROM PriceDetails pd
                    INNER JOIN PriceClasses pc ON pd.PriceClassId = pc.PriceClassId
                    INNER JOIN PriceGrades pg ON pd.PriceGradeId = pg.PriceGradeId
                    INNER JOIN PriceAdvances pa ON pd.PriceAdvanceId = pa.PriceAdvanceId
                    WHERE pd.PriceScheduleId IN @ScheduleIds
                      AND pd.ProcessTypeId IS NULL
                      AND pc.ClassCode = 'CL1'
                      AND pg.GradeNumber = 1";
                
                var scheduleIds = prices.Select(p => p.PriceID).ToList();
                var details = await connection.QueryAsync<dynamic>(detailsSql, new { ScheduleIds = scheduleIds });
                
                // Map the details to each price
                foreach (var price in prices)
                {
                    var priceDetails = details.Where(d => d.PriceScheduleId == price.PriceID);
                    
                    foreach (var detail in priceDetails)
                    {
                        string classCode = detail.ClassCode;
                        int gradeNum = detail.GradeNumber;
                        string advanceCode = detail.AdvanceCode;
                        decimal priceValue = detail.PricePerPound;
                        
                        // Build the property name (e.g., "CL1G1A1")
                        string propertyName = $"{classCode}G{gradeNum}{advanceCode}";
                        
                        // Set the property value using reflection
                        var property = typeof(Price).GetProperty(propertyName);
                        if (property != null && property.CanWrite)
                        {
                            property.SetValue(price, priceValue);
                        }
                    }
                }
                
                return prices;
            }
        }

        public async Task<Price> GetByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Get the price schedule header with proper lock status checking
                var scheduleSql = @"
                    SELECT 
                        ps.PriceScheduleId AS PriceID,
                        ps.ProductId,
                        ps.ProcessId,
                        p.ProductCode AS Product,
                        pr.ProcessCode AS Process,
                        ps.EffectiveFrom AS [From],
                        ISNULL(ps.TimePremiumEnabled, 0) AS TimePrem,
                        ISNULL(CONVERT(VARCHAR(8), ps.PremiumCutoffTime, 108), '') AS Time,
                        ISNULL(ps.CanadianPremiumAmount, 0) AS CPremium,
                        ISNULL(ps.USPremiumAmount, 0) AS UPremium,
                        ps.CreatedAt AS QADD_TIME,
                        ps.CreatedBy AS QADD_OP,
                        ps.ModifiedAt AS QED_TIME,
                        ps.ModifiedBy AS QED_OP,
                        NULL AS QDEL_TIME,
                        NULL AS QDEL_OP,
                        -- Check lock status from PriceScheduleLocks table
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 1 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS Adv1Used,
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 2 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS Adv2Used,
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 3 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS Adv3Used,
                        CASE WHEN EXISTS(
                            SELECT 1 FROM PriceScheduleLocks psl 
                            WHERE psl.PriceScheduleId = ps.PriceScheduleId 
                              AND psl.PaymentTypeId = 4 
                              AND psl.DeletedAt IS NULL
                        ) THEN 1 ELSE 0 END AS FinUsed
                    FROM PriceSchedules ps
                    INNER JOIN Products p ON ps.ProductId = p.ProductId
                    INNER JOIN Processes pr ON ps.ProcessId = pr.ProcessId
                    WHERE ps.PriceScheduleId = @Id";
                
                var price = await connection.QuerySingleOrDefaultAsync<Price>(scheduleSql, new { Id = id });
                
                if (price == null)
                {
                    return null;
                }
                
                // Get all price details for this schedule
                var detailsSql = @"
                    SELECT 
                        pd.PriceDetailId,
                        pd.PricePerPound,
                        RTRIM(pc.ClassCode) AS ClassCode,
                        pg.GradeNumber,
                        RTRIM(pa.AdvanceCode) AS AdvanceCode
                    FROM PriceDetails pd
                    INNER JOIN PriceClasses pc ON pd.PriceClassId = pc.PriceClassId
                    INNER JOIN PriceGrades pg ON pd.PriceGradeId = pg.PriceGradeId
                    INNER JOIN PriceAdvances pa ON pd.PriceAdvanceId = pa.PriceAdvanceId
                    WHERE pd.PriceScheduleId = @Id
                      AND pd.ProcessTypeId IS NULL";  // Only generic prices for now
                
                var details = await connection.QueryAsync<dynamic>(detailsSql, new { Id = id });
                
                Logger.Info($"Retrieved {details.Count()} price details for schedule {id}");
                
                // Map the details back to the legacy 72-column structure
                int mappedCount = 0;
                foreach (var detail in details)
                {
                    string classCode = detail.ClassCode;
                    int gradeNum = detail.GradeNumber;
                    string advanceCode = detail.AdvanceCode;
                    decimal priceValue = detail.PricePerPound;
                    
                    // Build the property name (e.g., "CL1G2A3")
                    string propertyName = $"{classCode}G{gradeNum}{advanceCode}";
                    
                    Logger.Info($"Attempting to map: ClassCode='{classCode}', GradeNum={gradeNum}, AdvanceCode='{advanceCode}' -> Property='{propertyName}', Value={priceValue}");
                    
                    // Set the property value using reflection
                    var property = typeof(Price).GetProperty(propertyName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(price, priceValue);
                        mappedCount++;
                        Logger.Info($"✓ Successfully mapped {propertyName} = {priceValue}");
                    }
                    else
                    {
                        Logger.Warn($"✗ Property not found or not writable: {propertyName}");
                    }
                }
                
                Logger.Info($"Mapped {mappedCount} of {details.Count()} properties for schedule {id}");
                return price;
            }
        }

        public async Task<int> CreateAsync(Price price)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Get Product and Process IDs
                        var idsSql = @"
                            SELECT 
                                p.ProductId,
                                pr.ProcessId
                            FROM 
                                (SELECT 1 AS Dummy) d
                            LEFT JOIN Products p ON p.ProductCode = @ProductCode
                            LEFT JOIN Processes pr ON pr.ProcessCode = @ProcessCode";
                        
                        var ids = await connection.QuerySingleOrDefaultAsync<dynamic>(
                            idsSql, 
                            new { ProductCode = price.Product, ProcessCode = price.Process },
                            transaction);
                        
                        if (ids?.ProductId == null || ids?.ProcessId == null)
                        {
                            throw new Exception($"Invalid Product ({price.Product}) or Process ({price.Process})");
                        }
                        
                        // Create the price schedule header
                        var scheduleSql = @"
                            INSERT INTO PriceSchedules (
                                ProductId, ProcessId, EffectiveFrom, 
                                TimePremiumEnabled, PremiumCutoffTime, 
                                CanadianPremiumAmount, USPremiumAmount,
                                CreatedBy
                            )
                            VALUES (
                                @ProductId, @ProcessId, @EffectiveFrom,
                                @TimePremiumEnabled, @PremiumCutoffTime,
                                @CanadianPremiumAmount, @USPremiumAmount,
                                @CreatedBy
                            );
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";
                        
                        // Parse time string (e.g., "10:10" or "10:10:00")
                        TimeSpan? cutoffTime = null;
                        if (price.TimePrem && !string.IsNullOrWhiteSpace(price.Time))
                        {
                            if (TimeSpan.TryParse(price.Time, out TimeSpan parsedTime))
                            {
                                cutoffTime = parsedTime;
                            }
                        }
                        
                        int scheduleId = await connection.ExecuteScalarAsync<int>(scheduleSql, new
                        {
                            ProductId = (int)ids.ProductId,
                            ProcessId = (int)ids.ProcessId,
                            EffectiveFrom = price.From,
                            TimePremiumEnabled = price.TimePrem,
                            PremiumCutoffTime = cutoffTime,
                            CanadianPremiumAmount = price.TimePrem ? price.CPremium : (decimal?)null,
                            USPremiumAmount = price.TimePrem ? price.UPremium : (decimal?)null,
                            CreatedBy = System.Environment.UserName
                        }, transaction);
                        
                        // Now insert all 72 price details (only non-zero values)
                        await InsertPriceDetailsAsync(connection, transaction, scheduleId, price);
                        
                        transaction.Commit();
                        Logger.Info($"Created new price schedule with ID: {scheduleId}");
                        return scheduleId;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error($"Error creating price schedule: {ex.Message}", ex);
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateAsync(Price price)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Validate that ProductId and ProcessId are set (should be loaded from GetByIdAsync)
                        if (price.ProductId <= 0 || price.ProcessId <= 0)
                        {
                            throw new Exception($"Invalid ProductId ({price.ProductId}) or ProcessId ({price.ProcessId}). Price object must be loaded from database first.");
                        }
                        
                        // Update the price schedule header
                        // Note: ProductId and ProcessId are NOT updated - they are immutable for a price schedule
                        var scheduleSql = @"
                            UPDATE PriceSchedules
                            SET EffectiveFrom = @EffectiveFrom,
                                TimePremiumEnabled = @TimePremiumEnabled,
                                PremiumCutoffTime = @PremiumCutoffTime,
                                CanadianPremiumAmount = @CanadianPremiumAmount,
                                USPremiumAmount = @USPremiumAmount,
                                ModifiedBy = @ModifiedBy,
                                ModifiedAt = GETDATE()
                            WHERE PriceScheduleId = @PriceScheduleId";
                        
                        // Parse time string (e.g., "10:10" or "10:10:00")
                        TimeSpan? cutoffTime = null;
                        if (price.TimePrem && !string.IsNullOrWhiteSpace(price.Time))
                        {
                            if (TimeSpan.TryParse(price.Time, out TimeSpan parsedTime))
                            {
                                cutoffTime = parsedTime;
                            }
                        }
                        
                        await connection.ExecuteAsync(scheduleSql, new
                        {
                            PriceScheduleId = price.PriceID,
                            EffectiveFrom = price.From,
                            TimePremiumEnabled = price.TimePrem,
                            PremiumCutoffTime = cutoffTime,
                            CanadianPremiumAmount = price.TimePrem ? price.CPremium : (decimal?)null,
                            USPremiumAmount = price.TimePrem ? price.UPremium : (decimal?)null,
                            ModifiedBy = System.Environment.UserName
                        }, transaction);
                        
                        // Delete existing price details
                        await connection.ExecuteAsync(
                            "DELETE FROM PriceDetails WHERE PriceScheduleId = @PriceScheduleId AND ProcessTypeId IS NULL",
                            new { PriceScheduleId = price.PriceID },
                            transaction);
                        
                        // Insert updated price details
                        await InsertPriceDetailsAsync(connection, transaction, price.PriceID, price);
                        
                        transaction.Commit();
                        Logger.Info($"Updated price schedule ID: {price.PriceID}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Error($"Error updating price schedule: {ex.Message}", ex);
                        throw;
                    }
                }
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Soft delete by setting EffectiveTo to today
                var sql = @"
                    UPDATE PriceSchedules
                    SET EffectiveTo = CAST(GETDATE() AS DATE),
                        ModifiedBy = @ModifiedBy,
                        ModifiedAt = GETDATE()
                    WHERE PriceScheduleId = @PriceScheduleId";
                
                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    PriceScheduleId = id,
                    ModifiedBy = System.Environment.UserName
                });
                
                if (rowsAffected > 0)
                {
                    Logger.Info($"Soft deleted price schedule ID: {id}");
                }
                
                return rowsAffected > 0;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Inserts all 72 price details from the Price model into PriceDetails table
        /// </summary>
        private async Task InsertPriceDetailsAsync(
            SqlConnection connection, 
            SqlTransaction transaction, 
            int scheduleId, 
            Price price)
        {
            // Get all dimension IDs first
            var dimensionsSql = @"
                SELECT 
                    pc.ClassCode, pc.PriceClassId,
                    pg.GradeNumber, pg.PriceGradeId,
                    pa.AdvanceCode, pa.PriceAdvanceId
                FROM PriceClasses pc, PriceGrades pg, PriceAdvances pa
                WHERE pc.IsActive = 1 AND pg.IsActive = 1 AND pa.IsActive = 1";
            
            var dimensions = await connection.QueryAsync<dynamic>(dimensionsSql, transaction: transaction);
            var dimensionDict = dimensions.ToDictionary(
                d => $"{d.ClassCode}G{d.GradeNumber}{d.AdvanceCode}",
                d => new { ClassId = (int)d.PriceClassId, GradeId = (int)d.PriceGradeId, AdvanceId = (int)d.PriceAdvanceId }
            );
            
            // Use reflection to iterate through all 72 price properties
            var priceType = typeof(Price);
            var detailInsertSql = @"
                INSERT INTO PriceDetails (PriceScheduleId, PriceClassId, PriceGradeId, PriceAdvanceId, ProcessTypeId, PricePerPound)
                VALUES (@ScheduleId, @ClassId, @GradeId, @AdvanceId, NULL, @PricePerPound)";
            
            int insertedCount = 0;
            foreach (var property in priceType.GetProperties())
            {
                // Check if this is a price column (e.g., CL1G1A1, UL2G3FN, etc.)
                if (property.Name.Length >= 6 && 
                    (property.Name.StartsWith("CL") || property.Name.StartsWith("UL")) &&
                    property.PropertyType == typeof(decimal))
                {
                    var value = (decimal)property.GetValue(price);
                    
                    // Log non-zero values for debugging
                    if (value > 0)
                    {
                        Logger.Info($"Property {property.Name} has value {value}, dict contains key: {dimensionDict.ContainsKey(property.Name)}");
                    }
                    
                    // Only insert non-zero prices
                    if (value > 0 && dimensionDict.TryGetValue(property.Name, out var dims))
                    {
                        await connection.ExecuteAsync(detailInsertSql, new
                        {
                            ScheduleId = scheduleId,
                            ClassId = dims.ClassId,
                            GradeId = dims.GradeId,
                            AdvanceId = dims.AdvanceId,
                            PricePerPound = value
                        }, transaction);
                        
                        insertedCount++;
                    }
                }
            }
            
            Logger.Info($"Inserted {insertedCount} price detail records for schedule {scheduleId}");
        }

        #endregion
    }
}
