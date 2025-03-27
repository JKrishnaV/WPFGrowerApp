using System;
using System.Data.SqlClient;
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
            // Connection string 
            _connectionString = "Server=DESKTOP-LQ92Q06;Database=PackagingPaymentSystem;User Id=localDB;Password=528database@JK;";
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
                Debug.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }
    }
} 