using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for migrating and looking up data from legacy SouthAlder database
    /// </summary>
    public class LegacyDataMigrationService
    {
        private readonly string _legacyConnectionString;
        private readonly string _modernConnectionString;

        public LegacyDataMigrationService(string legacyConnectionString, string modernConnectionString)
        {
            _legacyConnectionString = legacyConnectionString;
            _modernConnectionString = modernConnectionString;
        }

        #region Container Migration

        /// <summary>
        /// Migrates all containers from legacy Contain table to modern Containers table
        /// </summary>
        public async Task<int> MigrateContainersAsync()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                Logger.LogUserAction("MigrateContainers", "Migration", "Containers", "Legacy to modern container migration");
                
                using var legacyConnection = new SqlConnection(_legacyConnectionString);
                using var modernConnection = new SqlConnection(_modernConnectionString);

                await legacyConnection.OpenAsync();
                await modernConnection.OpenAsync();

                // Read from legacy Contain table
                var legacyContainers = await legacyConnection.QueryAsync<LegacyContainer>(@"
                    SELECT 
                        CONTAINER as ContainerId,
                        Description as ContainerName,
                        SHORT as ContainerCode,
                        TARE as TareWeight,
                        VALUE as ContainerValue,
                        INUSE as IsActive
                    FROM Contain
                    ORDER BY CONTAINER");

                int migratedCount = 0;

                foreach (var container in legacyContainers)
                {
                    // Check if container already exists in modern DB
                    var existingId = await modernConnection.QuerySingleOrDefaultAsync<int?>(
                        "SELECT ContainerId FROM Containers WHERE ContainerId = @ContainerId",
                        new { container.ContainerId });

                    if (existingId == null)
                    {
                        // Insert into modern Containers table
                        await modernConnection.ExecuteAsync(@"
                            INSERT INTO Containers (ContainerId, ContainerCode, ContainerName, TareWeight, ContainerValue, IsActive, DisplayOrder)
                            VALUES (@ContainerId, @ContainerCode, @ContainerName, @TareWeight, @ContainerValue, @IsActive, @ContainerId)",
                            new
                            {
                                container.ContainerId,
                                container.ContainerCode,
                                container.ContainerName,
                                container.TareWeight,
                                container.ContainerValue,
                                IsActive = container.IsActive == 1,
                                DisplayOrder = container.ContainerId
                            });

                        migratedCount++;
                        Logger.Info($"Migrated container: {container.ContainerCode} - {container.ContainerName}");
                    }
                }

                Logger.Info($"Container migration completed: {migratedCount} containers migrated");
                
                stopwatch.Stop();
                Logger.LogDatabaseOperation("MigrateContainers", "MIGRATION", stopwatch.ElapsedMilliseconds, migratedCount, 
                    $"Legacy to modern container migration completed");
                Logger.LogPerformanceWithThreshold("MigrateContainers", stopwatch.ElapsedMilliseconds, 
                    $"Migrated: {migratedCount} containers");
                
                return migratedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Logger.LogDatabaseOperation("MigrateContainers", "MIGRATION", stopwatch.ElapsedMilliseconds, additionalInfo: $"Error: {ex.Message}");
                Logger.LogPerformanceWithThreshold("MigrateContainers", stopwatch.ElapsedMilliseconds, 
                    $"Error: {ex.Message}");
                Logger.Error($"Error migrating containers: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets container ID from legacy database based on IN/OUT position
        /// Legacy system uses IN1-IN20 and OUT1-OUT20 to store container counts
        /// The position number corresponds to the ContainerId
        /// </summary>
        public async Task<int?> GetContainerIdFromLegacyReceiptAsync(decimal legacyReceiptNumber)
        {
            try
            {
                using var connection = new SqlConnection(_legacyConnectionString);
                
                // Query to find which IN column has a value > 0 (indicates container type used)
                var container = await connection.QuerySingleOrDefaultAsync<LegacyReceiptContainer>(@"
                    SELECT 
                        CASE 
                            WHEN IN1 > 0 THEN 1
                            WHEN IN2 > 0 THEN 2
                            WHEN IN3 > 0 THEN 3
                            WHEN IN4 > 0 THEN 4
                            WHEN IN5 > 0 THEN 5
                            WHEN IN6 > 0 THEN 6
                            WHEN IN7 > 0 THEN 7
                            WHEN IN8 > 0 THEN 8
                            WHEN IN9 > 0 THEN 9
                            WHEN IN10 > 0 THEN 10
                            WHEN IN11 > 0 THEN 11
                            WHEN IN12 > 0 THEN 12
                            WHEN IN13 > 0 THEN 13
                            WHEN IN14 > 0 THEN 14
                            WHEN IN15 > 0 THEN 15
                            WHEN IN16 > 0 THEN 16
                            WHEN IN17 > 0 THEN 17
                            WHEN IN18 > 0 THEN 18
                            WHEN IN19 > 0 THEN 19
                            WHEN IN20 > 0 THEN 20
                            ELSE NULL
                        END AS ContainerId
                    FROM Daily
                    WHERE NUMBER = @ReceiptNumber",
                    new { ReceiptNumber = legacyReceiptNumber });

                return container?.ContainerId;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting container from legacy receipt {legacyReceiptNumber}: {ex.Message}", ex);
                return null;
            }
        }

        #endregion

        #region Price Class/Area Lookup

        /// <summary>
        /// Determines PriceClassId and PriceAreaId from legacy price data
        /// Legacy Price table uses column names like CL1G1A1, UL2G2A3, etc.
        /// Format: [CL/UL][1-3]G[1-3]A[1-3/FN]
        /// CL = Canadian Class, UL = US Class
        /// Numbers 1-3 = Class level (1=Premium, 2=Standard, 3=Economy)
        /// G1-G3 = Grade
        /// A1-A3/FN = Area (1-3 or FN for final/flat)
        /// </summary>
        public async Task<(int PriceClassId, int PriceAreaId)> DeterminePriceClassAndAreaAsync(
            string product, 
            string process, 
            byte grade)
        {
            try
            {
                using var connection = new SqlConnection(_legacyConnectionString);

                // Query the Price table to find which column has a non-zero value for this product/process/grade
                // This tells us which class and area were used
                var priceQuery = $@"
                    SELECT TOP 1
                        CL1G{grade}A1, CL1G{grade}A2, CL1G{grade}A3, CL1G{grade}FN,
                        CL2G{grade}A1, CL2G{grade}A2, CL2G{grade}A3, CL2G{grade}FN,
                        CL3G{grade}A1, CL3G{grade}A2, CL3G{grade}A3, CL3G{grade}FN,
                        UL1G{grade}A1, UL1G{grade}A2, UL1G{grade}A3, UL1G{grade}FN,
                        UL2G{grade}A1, UL2G{grade}A2, UL2G{grade}A3, UL2G{grade}FN,
                        UL3G{grade}A1, UL3G{grade}A2, UL3G{grade}A3, UL3G{grade}FN
                    FROM Price
                    WHERE PRODUCT = @Product AND PROCESS = @Process";

                var priceRow = await connection.QuerySingleOrDefaultAsync(
                    priceQuery, 
                    new { Product = product, Process = process });

                if (priceRow != null)
                {
                    // Find the first non-zero price column
                    var priceDict = (IDictionary<string, object>)priceRow;
                    
                    foreach (var kvp in priceDict)
                    {
                        if (kvp.Value != null && kvp.Value != DBNull.Value)
                        {
                            var price = Convert.ToDecimal(kvp.Value);
                            if (price > 0)
                            {
                                // Parse the column name to get class and area
                                var columnName = kvp.Key;
                                return ParsePriceColumnName(columnName);
                            }
                        }
                    }
                }

                // Default to Class 1 (Premium), Area 1 if not found
                Logger.Warn($"No price found for Product={product}, Process={process}, Grade={grade}. Using defaults.");
                return (PriceClassId: 1, PriceAreaId: 1);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error determining price class/area: {ex.Message}", ex);
                return (PriceClassId: 1, PriceAreaId: 1);
            }
        }

        /// <summary>
        /// Parses legacy price column name to modern PriceClassId and PriceAreaId
        /// Example: "CL1G1A1" -> PriceClassId=1 (CL1), PriceAreaId=1 (A1)
        /// Example: "UL2G2A3" -> PriceClassId=5 (UL2), PriceAreaId=3 (A3)
        /// </summary>
        private (int PriceClassId, int PriceAreaId) ParsePriceColumnName(string columnName)
        {
            try
            {
                // Format: [CL/UL][1-3]G[grade]A[1-3/FN]
                // Examples: CL1G1A1, UL2G2A3, CL3G1FN
                
                var classType = columnName.Substring(0, 2); // CL or UL
                var classLevel = int.Parse(columnName.Substring(2, 1)); // 1, 2, or 3
                
                // Find area part (after 'A')
                var areaIndex = columnName.IndexOf('A');
                int areaId;
                
                if (areaIndex >= 0)
                {
                    var areaPart = columnName.Substring(areaIndex + 1);
                    if (areaPart == "FN")
                    {
                        areaId = 4; // Assuming FN maps to area 4
                    }
                    else
                    {
                        areaId = int.Parse(areaPart); // 1, 2, or 3
                    }
                }
                else
                {
                    areaId = 1; // Default
                }

                // Map to modern PriceClassId
                // Modern DB: 1=CL1, 2=CL2, 3=CL3, 4=UL1, 5=UL2, 6=UL3
                int priceClassId;
                if (classType == "CL")
                {
                    priceClassId = classLevel; // 1, 2, or 3
                }
                else // UL
                {
                    priceClassId = 3 + classLevel; // 4, 5, or 6
                }

                return (priceClassId, areaId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error parsing price column name '{columnName}': {ex.Message}", ex);
                return (PriceClassId: 1, PriceAreaId: 1);
            }
        }

        #endregion

        #region Process Migration

        /// <summary>
        /// Migrates missing processes from legacy Process table to modern Processes table
        /// </summary>
        public async Task<int> MigrateMissingProcessesAsync()
        {
            try
            {
                using var legacyConnection = new SqlConnection(_legacyConnectionString);
                using var modernConnection = new SqlConnection(_modernConnectionString);

                await legacyConnection.OpenAsync();
                await modernConnection.OpenAsync();

                // Read from legacy Process table
                var legacyProcesses = await legacyConnection.QueryAsync<LegacyProcess>(@"
                    SELECT 
                        PROCESS as ProcessCode,
                        PROC_CLASS as ProcessClass,
                        PROCESS_NAME as ProcessName
                    FROM Process
                    ORDER BY PROCESS");

                int migratedCount = 0;

                foreach (var process in legacyProcesses)
                {
                    // Check if process already exists in modern DB
                    var existingId = await modernConnection.QuerySingleOrDefaultAsync<int?>(
                        "SELECT ProcessId FROM Processes WHERE ProcessCode = @ProcessCode",
                        new { process.ProcessCode });

                    if (existingId == null)
                    {
                        // Get next available ProcessId
                        var maxId = await modernConnection.QuerySingleOrDefaultAsync<int?>(
                            "SELECT MAX(ProcessId) FROM Processes") ?? 0;

                        // Insert into modern Processes table
                        await modernConnection.ExecuteAsync(@"
                            INSERT INTO Processes (ProcessCode, ProcessName, Description, IsActive, DisplayOrder)
                            VALUES (@ProcessCode, @ProcessName, @Description, 1, @DisplayOrder)",
                            new
                            {
                                process.ProcessCode,
                                process.ProcessName,
                                Description = $"Migrated from legacy system - Class: {process.ProcessClass}",
                                DisplayOrder = maxId + 1
                            });

                        migratedCount++;
                        Logger.Info($"Migrated process: {process.ProcessCode} - {process.ProcessName}");
                    }
                }

                Logger.Info($"Process migration completed: {migratedCount} processes migrated");
                return migratedCount;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error migrating processes: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Helper Classes

        private class LegacyContainer
        {
            public int ContainerId { get; set; }
            public string ContainerName { get; set; } = string.Empty;
            public string ContainerCode { get; set; } = string.Empty;
            public decimal TareWeight { get; set; }
            public decimal ContainerValue { get; set; }
            public int IsActive { get; set; }
        }

        private class LegacyReceiptContainer
        {
            public int? ContainerId { get; set; }
        }

        private class LegacyProcess
        {
            public string ProcessCode { get; set; } = string.Empty;
            public string ProcessClass { get; set; } = string.Empty;
            public string ProcessName { get; set; } = string.Empty;
        }

        #endregion
    }
}
