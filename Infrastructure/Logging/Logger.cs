using System;
using log4net;

namespace WPFGrowerApp.Infrastructure.Logging
{
    public static class Logger
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Logger));

        static Logger()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public static void Info(string message)
        {
            Log.Info(message);
        }

        public static void Info(string message, Exception exception)
        {
            Log.Info(message, exception);
        }

        public static void Warn(string message)
        {
            Log.Warn(message);
        }

        public static void Warn(string message, Exception exception)
        {
            Log.Warn(message, exception);
        }

        public static void Error(string message)
        {
            Log.Error(message);
        }

        public static void Error(string message, Exception exception)
        {
            Log.Error(message, exception);
        }

        public static void Debug(string message)
        {
            Log.Debug(message);
        }

        public static void Debug(string message, Exception exception)
        {
            Log.Debug(message, exception);
        }

        public static void Fatal(string message)
        {
            Log.Fatal(message);
        }

        public static void Fatal(string message, Exception exception)
        {
            Log.Fatal(message, exception);
        }
    }
} 