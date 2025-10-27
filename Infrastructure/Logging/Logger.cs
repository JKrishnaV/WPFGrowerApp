using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using log4net;

namespace WPFGrowerApp.Infrastructure.Logging
{
    public static class Logger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Logger));
        private static readonly AsyncLocal<string> _correlationId = new AsyncLocal<string>();

        static Logger()
        {
            log4net.Config.XmlConfigurator.Configure();
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

        // Business operation logging
        public static void LogBusinessOperation(string operation, string entityType, object entityId, string user = null, string additionalInfo = "")
        {
            var correlationId = GetCorrelationId();
            var userInfo = string.IsNullOrEmpty(user) ? "System" : user;
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