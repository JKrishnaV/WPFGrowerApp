using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace WPFGrowerApp.DataAccess
{
    public class DapperConnectionManager
    {
        private readonly string _connectionString;

        public DapperConnectionManager()
        {
            // Connection string with the provided credentials
            _connectionString = "Server=DESKTOP-LQ92Q06;Database=PackagingPaymentSystem;User Id=localDB;Password=528database@JK;";
        }

        /// <summary>
        /// Creates and returns a new SQL connection
        /// </summary>
        /// <returns>An open SQL connection</returns>
        public SqlConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            return connection;
        }

        /// <summary>
        /// Tests the database connection
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
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
                System.Diagnostics.Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }
    }
}
