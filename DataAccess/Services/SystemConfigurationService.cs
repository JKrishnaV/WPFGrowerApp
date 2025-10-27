using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    public class SystemConfigurationService : BaseDatabaseService, ISystemConfigurationService
    {
        private static readonly object _cacheLock = new object();
        private static System.Collections.Concurrent.ConcurrentDictionary<string, string> _configCache = 
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        private static DateTime _lastCacheRefresh = DateTime.MinValue;
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

        public async Task<string> GetConfigValueAsync(string configKey, string defaultValue = null)
        {
            try
            {
                // Check cache first
                if (ShouldRefreshCache())
                {
                    await RefreshCacheAsync();
                }

                if (_configCache.TryGetValue(configKey, out string cachedValue))
                {
                    return cachedValue ?? defaultValue;
                }

                // If not in cache, get from database
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT ConfigValue 
                        FROM SystemConfiguration 
                        WHERE ConfigKey = @ConfigKey 
                        AND DeletedAt IS NULL";

                    var result = await connection.QueryFirstOrDefaultAsync<string>(sql, new { ConfigKey = configKey });
                    
                    // Cache the result (even if null)
                    _configCache.TryAdd(configKey, result);
                    
                    return result ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting config value for key '{configKey}': {ex.Message}", ex);
                return defaultValue;
            }
        }

        public async Task<bool> GetConfigBoolAsync(string configKey, bool defaultValue = false)
        {
            var value = await GetConfigValueAsync(configKey);
            if (bool.TryParse(value, out bool result))
                return result;
            return defaultValue;
        }

        public async Task<int> GetConfigIntAsync(string configKey, int defaultValue = 0)
        {
            var value = await GetConfigValueAsync(configKey);
            if (int.TryParse(value, out int result))
                return result;
            return defaultValue;
        }

        public async Task<long> GetConfigLongAsync(string configKey, long defaultValue = 0)
        {
            var value = await GetConfigValueAsync(configKey);
            if (long.TryParse(value, out long result))
                return result;
            return defaultValue;
        }

        public async Task<decimal> GetConfigDecimalAsync(string configKey, decimal defaultValue = 0)
        {
            var value = await GetConfigValueAsync(configKey);
            if (decimal.TryParse(value, out decimal result))
                return result;
            return defaultValue;
        }

        public async Task SetConfigValueAsync(string configKey, string value, string description = null)
        {
            var user = App.CurrentUser?.Username ?? "SYSTEM";
            var now = DateTime.UtcNow;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Check if config exists
                    var existsSql = "SELECT COUNT(1) FROM SystemConfiguration WHERE ConfigKey = @ConfigKey AND DeletedAt IS NULL";
                    var exists = await connection.QuerySingleAsync<int>(existsSql, new { ConfigKey = configKey }) > 0;

                    if (exists)
                    {
                        // Update existing
                        var updateSql = @"
                            UPDATE SystemConfiguration 
                            SET ConfigValue = @ConfigValue,
                                Description = ISNULL(@Description, Description),
                                ModifiedAt = @ModifiedAt,
                                ModifiedBy = @ModifiedBy
                            WHERE ConfigKey = @ConfigKey AND DeletedAt IS NULL";

                        await connection.ExecuteAsync(updateSql, new
                        {
                            ConfigKey = configKey,
                            ConfigValue = value,
                            Description = description,
                            ModifiedAt = now,
                            ModifiedBy = user
                        });
                    }
                    else
                    {
                        // Insert new
                        var insertSql = @"
                            INSERT INTO SystemConfiguration 
                            (ConfigKey, ConfigValue, Description, DataType, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy)
                            VALUES 
                            (@ConfigKey, @ConfigValue, @Description, @DataType, @CreatedAt, @CreatedBy, @ModifiedAt, @ModifiedBy)";

                        await connection.ExecuteAsync(insertSql, new
                        {
                            ConfigKey = configKey,
                            ConfigValue = value,
                            Description = description,
                            DataType = "String",
                            CreatedAt = now,
                            CreatedBy = user,
                            ModifiedAt = now,
                            ModifiedBy = user
                        });
                    }

                    // Update cache
                    _configCache.AddOrUpdate(configKey, value, (key, oldValue) => value);
                    
                    Logger.LogUserAction("SetConfigValue", "SystemConfiguration", configKey, $"Value: {value}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error setting config value for key '{configKey}': {ex.Message}", ex);
                throw;
            }
        }

        public async Task SetConfigBoolAsync(string configKey, bool value, string description = null)
        {
            await SetConfigValueAsync(configKey, value.ToString(), description);
        }

        public async Task SetConfigIntAsync(string configKey, int value, string description = null)
        {
            await SetConfigValueAsync(configKey, value.ToString(), description);
        }

        public async Task SetConfigLongAsync(string configKey, long value, string description = null)
        {
            await SetConfigValueAsync(configKey, value.ToString(), description);
        }

        public async Task SetConfigDecimalAsync(string configKey, decimal value, string description = null)
        {
            await SetConfigValueAsync(configKey, value.ToString(), description);
        }

        public async Task<SystemConfiguration> GetConfigAsync(string configKey)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT ConfigId, ConfigKey, ConfigValue, Description, DataType,
                               ModifiedAt, ModifiedBy, CreatedAt, CreatedBy, DeletedAt, DeletedBy
                        FROM SystemConfiguration 
                        WHERE ConfigKey = @ConfigKey AND DeletedAt IS NULL";

                    return await connection.QueryFirstOrDefaultAsync<SystemConfiguration>(sql, new { ConfigKey = configKey });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting config for key '{configKey}': {ex.Message}", ex);
                return null;
            }
        }

        public async Task<bool> ConfigExistsAsync(string configKey)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(1) FROM SystemConfiguration WHERE ConfigKey = @ConfigKey AND DeletedAt IS NULL";
                    return await connection.QuerySingleAsync<int>(sql, new { ConfigKey = configKey }) > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking if config exists for key '{configKey}': {ex.Message}", ex);
                return false;
            }
        }

        private bool ShouldRefreshCache()
        {
            return DateTime.UtcNow - _lastCacheRefresh > CacheExpiry;
        }

        private async Task RefreshCacheAsync()
        {
            lock (_cacheLock)
            {
                if (!ShouldRefreshCache()) return;
                
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        var sql = "SELECT ConfigKey, ConfigValue FROM SystemConfiguration WHERE DeletedAt IS NULL";
                        var configs = connection.Query<(string Key, string Value)>(sql);
                        
                        _configCache.Clear();
                        foreach (var config in configs)
                        {
                            _configCache.TryAdd(config.Key, config.Value);
                        }
                        
                        _lastCacheRefresh = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error refreshing config cache: {ex.Message}", ex);
                }
            }
        }
    }
}
