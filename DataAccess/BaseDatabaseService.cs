using Microsoft.Data.SqlClient;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;

namespace WPFGrowerApp.DataAccess
{
    public abstract class BaseDatabaseService : IDatabaseService
    {
        protected readonly string _connectionString;

        protected BaseDatabaseService()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                // Log this critical error using the configured logger
                Infrastructure.Logging.Logger.Fatal("FATAL ERROR: Connection string 'DefaultConnection' not found or empty in App.config.");
                // Consider throwing a more specific exception or handling this scenario appropriately
                throw new ConfigurationErrorsException("Connection string 'DefaultConnection' is missing or empty in App.config.");
            }
        }

        protected SqlConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Connection test failed: {ex.Message}", ex);
                return false;
            }
        }
    }
}
