using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for classifying processes into Fresh vs Non-Fresh categories.
    /// Mirrors the XBase logic from GROW_AP.PRG - SetFreshProcesses() function.
    /// 
    /// Process Classes (from BerryPay.ch):
    /// - 1 = Fresh (e.g., FR)
    /// - 2 = Processed (e.g., IQF)
    /// - 3 = Juice (e.g., JU)
    /// - 4 = Other (catch-all)
    /// </summary>
    public class ProcessClassificationService : BaseDatabaseService, IProcessClassificationService
    {
        // Cache for fresh process codes (loaded once, refreshed on demand)
        private List<string>? _freshProcessCodes;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        // Process class constants (matching XBase BerryPay.ch)
        private const int PROCESS_CLASS_FRESH = 1;
        private const int PROCESS_CLASS_PROCESSED = 2;
        private const int PROCESS_CLASS_JUICE = 3;
        private const int PROCESS_CLASS_OTHER = 4;

        public ProcessClassificationService()
        {
        }

        /// <summary>
        /// Determines if a process code represents Fresh berries (proc_class = 1).
        /// Uses cached list of fresh processes for performance.
        /// </summary>
        public async Task<bool> IsFreshProcessAsync(string processCode)
        {
            if (string.IsNullOrEmpty(processCode))
            {
                return false;
            }

            var freshCodes = await GetFreshProcessCodesAsync();
            return freshCodes.Contains(processCode.ToUpper());
        }

        /// <summary>
        /// Gets all fresh process codes from the Process table.
        /// Caches the result for performance.
        /// Mirrors XBase logic: Process->proc_class == 1
        /// </summary>
        public async Task<List<string>> GetFreshProcessCodesAsync()
        {
            // Check cache first (without lock for performance)
            if (_freshProcessCodes != null)
            {
                return _freshProcessCodes;
            }

            // Use async-compatible lock
            await _cacheLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (_freshProcessCodes != null)
                {
                    return _freshProcessCodes;
                }

                // Load from database (properly await)
                _freshProcessCodes = await LoadFreshProcessCodesAsync();
                return _freshProcessCodes;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        /// <summary>
        /// Actually loads fresh process codes from the database.
        /// </summary>
        private async Task<List<string>> LoadFreshProcessCodesAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Query for processes with PROC_CLASS = 1 (Fresh)
                    var sql = @"
                        SELECT ProcessCode
                        FROM Processes
                        WHERE ProcessClass = @FreshClass AND IsActive = 1
                        ORDER BY ProcessCode";

                    var freshCodes = await connection.QueryAsync<string>(
                        sql, 
                        new { FreshClass = PROCESS_CLASS_FRESH });

                    var result = freshCodes.ToList();
                    
                    Logger.Info($"Loaded {result.Count} fresh process codes: {string.Join(", ", result)}");
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading fresh process codes: {ex.Message}", ex);
                // Return empty list on error
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets the process class for a given process code.
        /// </summary>
        public async Task<int> GetProcessClassAsync(string processCode)
        {
            if (string.IsNullOrEmpty(processCode))
            {
                return PROCESS_CLASS_OTHER; // Default to Other
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT ProcessClass
                        FROM Processes
                        WHERE ProcessCode = @ProcessCode AND IsActive = 1";

                    var processClass = await connection.ExecuteScalarAsync<int?>(
                        sql, 
                        new { ProcessCode = processCode });

                    // Return the class, or default to Other if not found
                    return processClass ?? PROCESS_CLASS_OTHER;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting process class for '{processCode}': {ex.Message}", ex);
                return PROCESS_CLASS_OTHER; // Default to Other on error
            }
        }

        /// <summary>
        /// Gets the process class name for display.
        /// Matches XBase PROCESS_CLASS_ARRAY from BerryPay.ch
        /// </summary>
        public string GetProcessClassName(int processClass)
        {
            return processClass switch
            {
                PROCESS_CLASS_FRESH => "Fresh",
                PROCESS_CLASS_PROCESSED => "Processed",
                PROCESS_CLASS_JUICE => "Juice",
                PROCESS_CLASS_OTHER => "Other",
                _ => "Other" // Default for invalid values
            };
        }

        /// <summary>
        /// Refreshes the cached list of fresh processes from the database.
        /// Call this if process classifications change.
        /// </summary>
        public async Task RefreshCacheAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                _freshProcessCodes = null; // Clear cache
            }
            finally
            {
                _cacheLock.Release();
            }

            // Reload cache
            await GetFreshProcessCodesAsync();
            
            Logger.Info("Process classification cache refreshed");
        }
    }
}
