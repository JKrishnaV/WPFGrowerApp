using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using log4net;
using WPFGrowerApp.DataAccess.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace WPFGrowerApp.Infrastructure.Logging
{
    public static class Logger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Logger));
        private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();
        
        // Performance thresholds (in milliseconds) - configurable via database
        private static long _slowDatabaseOperationThreshold = 1000; // 1 second
        private static long _slowBusinessOperationThreshold = 2000; // 2 seconds
        
        // Feature flags - configurable via database
        private static bool _enableDatabaseLogging = true;
        private static bool _enableUserActionLogging = true;
        private static bool _enablePerformanceLogging = true;
        private static bool _enableSlowOperationWarnings = true;
        
        // Cache for configuration values
        private static DateTime _lastConfigRefresh = DateTime.MinValue;
        private static readonly TimeSpan ConfigCacheExpiry = TimeSpan.FromMinutes(2);

        static Logger()
        {
            log4net.Config.XmlConfigurator.Configure();
            RefreshConfiguration();
        }

        // Correlation ID management
        public static void SetCorrelationId(string correlationId)
        {
            _correlationId.Value = correlationId;
        }

        public static string GetCorrelationId()
        {
            return _correlationId.Value ?? Guid.NewGuid().ToString();
        }

        // Enhanced logging methods with correlation IDs and structured parameters
        public static void Trace(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Debug($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        public static void Trace(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Debug($"[{correlationId}] [{className}.{memberName}] {message}", exception);
        }

        public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Debug($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        public static void Debug(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Debug($"[{correlationId}] [{className}.{memberName}] {message}", exception);
        }

        public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Info($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        public static void Info(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Info($"[{correlationId}] [{className}.{memberName}] {message}", exception);
        }

        public static void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Warn($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        public static void Warn(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Warn($"[{correlationId}] [{className}.{memberName}] {message}", exception);
        }

        public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Error($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        public static void Error(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Error($"[{correlationId}] [{className}.{memberName}] {message}", exception);
        }

        public static void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Fatal($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        public static void Fatal(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Log.Fatal($"[{correlationId}] [{className}.{memberName}] {message}", exception);
        }

        // Performance logging methods
        public static IDisposable BeginTimedOperation(string operationName, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            return new TimedOperation(operationName, memberName, filePath);
        }

        public static void LogPerformance(string operationName, long elapsedMilliseconds, string additionalInfo = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var message = string.IsNullOrEmpty(additionalInfo) 
                ? $"Performance: {operationName} completed in {elapsedMilliseconds}ms"
                : $"Performance: {operationName} completed in {elapsedMilliseconds}ms - {additionalInfo}";
            
            Log.Info($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        // ===== PRIORITY 2 ENHANCEMENTS =====

        /// <summary>
        /// Logs database operations with timing and performance metrics (configurable)
        /// </summary>
        public static void LogDatabaseOperation(string operation, string queryType, long elapsedMs, int rowCount = 0, string additionalInfo = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            if (!_enableDatabaseLogging) return;
            
            RefreshConfigurationIfNeeded();
            
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var user = App.CurrentUser?.Username ?? "SYSTEM";
            
            var rowInfo = rowCount > 0 ? $" - Rows affected: {rowCount}" : "";
            var info = !string.IsNullOrEmpty(additionalInfo) ? $" - {additionalInfo}" : "";
            
            var message = $"Database {queryType}: {operation} - Duration: {elapsedMs}ms{rowInfo} - User: {user}{info}";
            
            // Warn if slow operation (configurable threshold)
            if (_enableSlowOperationWarnings && elapsedMs > _slowDatabaseOperationThreshold)
            {
                Log.Warn($"[{correlationId}] [{className}.{memberName}] SLOW OPERATION - {message}");
            }
            else
            {
                Log.Info($"[{correlationId}] [{className}.{memberName}] {message}");
            }
        }

        /// <summary>
        /// Logs user actions with automatic context capture (configurable)
        /// </summary>
        public static void LogUserAction(string action, string entityType, object entityId, string details = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            if (!_enableUserActionLogging) return;
            
            RefreshConfigurationIfNeeded();
            
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var user = App.CurrentUser?.Username ?? "SYSTEM";
            
            var detailInfo = !string.IsNullOrEmpty(details) ? $" - {details}" : "";
            var message = $"User Action: {action} on {entityType} (ID: {entityId}) - User: {user}{detailInfo}";
            
            Log.Info($"[{correlationId}] [{className}.{memberName}] {message}");
        }

        /// <summary>
        /// Enhanced performance monitoring with automatic slow operation detection (configurable)
        /// </summary>
        public static void LogPerformanceWithThreshold(string operationName, long elapsedMilliseconds, string thresholdType = "business", string additionalInfo = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            if (!_enablePerformanceLogging) return;
            
            RefreshConfigurationIfNeeded();
            
            var correlationId = GetCorrelationId();
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var user = App.CurrentUser?.Username ?? "SYSTEM";
            
            var threshold = thresholdType == "database" ? _slowDatabaseOperationThreshold : _slowBusinessOperationThreshold;
            var thresholdLabel = thresholdType == "database" ? "SLOW_DATABASE" : "SLOW_BUSINESS";
            
            var info = !string.IsNullOrEmpty(additionalInfo) ? $" - {additionalInfo}" : "";
            var message = $"Performance: {operationName} - Duration: {elapsedMilliseconds}ms - User: {user}{info}";
            
            if (_enableSlowOperationWarnings && elapsedMilliseconds > threshold)
            {
                Log.Warn($"[{correlationId}] [{className}.{memberName}] {thresholdLabel} OPERATION - {message}");
            }
            else
            {
                Log.Info($"[{correlationId}] [{className}.{memberName}] {message}");
            }
        }

        // Business operation logging (enhanced with automatic user context)
        public static void LogBusinessOperation(string operation, string entityType, object entityId, string user = null, string additionalInfo = "")
        {
            var correlationId = GetCorrelationId();
            var userInfo = user ?? App.CurrentUser?.Username ?? "System";
            var message = $"Business Operation: {operation} on {entityType} (ID: {entityId}) by {userInfo}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                message += $" - {additionalInfo}";
            }
            Log.Info($"[{correlationId}] {message}");
        }

        // Security event logging
        public static void LogSecurityEvent(string eventType, string user = null, string details = "", bool isSuccess = true)
        {
            var correlationId = GetCorrelationId();
            var userInfo = string.IsNullOrEmpty(user) ? "Unknown" : user;
            var status = isSuccess ? "SUCCESS" : "FAILED";
            var message = $"Security Event: {eventType} - User: {userInfo} - Status: {status}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" - Details: {details}";
            }
            
            if (isSuccess)
                Log.Info($"[{correlationId}] {message}");
            else
                Log.Warn($"[{correlationId}] {message}");
        }

        // Configuration management methods
        private static void RefreshConfigurationIfNeeded()
        {
            if (DateTime.UtcNow - _lastConfigRefresh > ConfigCacheExpiry)
            {
                RefreshConfiguration();
            }
        }

        private static void RefreshConfiguration()
        {
            try
            {
                // Try to get configuration service from DI container
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider != null)
                {
                    var configService = serviceProvider.GetService<ISystemConfigurationService>();
                    if (configService != null)
                    {
                        // Load configuration values asynchronously but cache them
                        Task.Run(async () =>
                        {
                            try
                            {
                                _slowDatabaseOperationThreshold = await configService.GetConfigLongAsync("Logging.SlowDatabaseThreshold", 1000);
                                _slowBusinessOperationThreshold = await configService.GetConfigLongAsync("Logging.SlowBusinessThreshold", 2000);
                                _enableDatabaseLogging = await configService.GetConfigBoolAsync("Logging.EnableDatabaseLogging", true);
                                _enableUserActionLogging = await configService.GetConfigBoolAsync("Logging.EnableUserActionLogging", true);
                                _enablePerformanceLogging = await configService.GetConfigBoolAsync("Logging.EnablePerformanceLogging", true);
                                _enableSlowOperationWarnings = await configService.GetConfigBoolAsync("Logging.EnableSlowOperationWarnings", true);
                                
                                _lastConfigRefresh = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                // Log error but don't throw - use defaults
                                Log.Error($"Error refreshing logging configuration: {ex.Message}", ex);
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // If DI container is not available, use defaults
                Log.Debug($"Could not access configuration service: {ex.Message}");
            }
        }

        /// <summary>
        /// Force refresh of logging configuration from database
        /// </summary>
        public static void ForceRefreshConfiguration()
        {
            _lastConfigRefresh = DateTime.MinValue;
            RefreshConfiguration();
        }

        // Helper class for timed operations
        private class TimedOperation : IDisposable
        {
            private readonly string _operationName;
            private readonly string _memberName;
            private readonly string _filePath;
            private readonly Stopwatch _stopwatch;

            public TimedOperation(string operationName, string memberName, string filePath)
            {
                _operationName = operationName;
                _memberName = memberName;
                _filePath = filePath;
                _stopwatch = Stopwatch.StartNew();
                
                var correlationId = GetCorrelationId();
                var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
                Log.Debug($"[{correlationId}] [{className}.{memberName}] Starting operation: {operationName}");
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                LogPerformance(_operationName, _stopwatch.ElapsedMilliseconds, _memberName, _filePath);
            }
        }
    }
} 