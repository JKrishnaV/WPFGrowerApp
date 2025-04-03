using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging; // Assuming Logger is available

namespace WPFGrowerApp.DataAccess.Services
{
    public class PostBatchService : BaseDatabaseService, IPostBatchService
    {
        public async Task<PostBatch> CreatePostBatchAsync(DateTime batchDate, DateTime cutoffDate, string postType)
        {
            try
            {
                var nextId = await GetNextPostBatchIdAsync();
                var newBatch = new PostBatch
                {
                    PostBat = nextId,
                    Date = batchDate,
                    Cutoff = cutoffDate,
                    PostType = postType,
                    // Audit fields can be set here or by database defaults if configured
                    QaddDate = DateTime.UtcNow,
                    QaddOp = App.CurrentUser?.Username ?? "SYSTEM" // Get current user if available
                };

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO PostBat (POST_BAT, DATE, CUTOFF, POST_TYPE, QADD_DATE, QADD_OP)
                        VALUES (@PostBat, @Date, @Cutoff, @PostType, @QaddDate, @QaddOp);";
                    
                    await connection.ExecuteAsync(sql, newBatch);
                    Logger.Info($"Created Post Batch ID: {newBatch.PostBat}");
                    return newBatch;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating Post Batch: {ex.Message}", ex);
                throw; // Rethrow to allow higher layers to handle
            }
        }

        public async Task<decimal> GetNextPostBatchIdAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Assuming POST_BAT is the primary key and increments.
                    // Handle potential NULL if table is empty.
                    var sql = "SELECT ISNULL(MAX(POST_BAT), 0) + 1 FROM PostBat";
                    var nextId = await connection.ExecuteScalarAsync<decimal>(sql);
                    return nextId;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting next Post Batch ID: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<PostBatch> GetPostBatchByIdAsync(decimal postBatchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT * FROM PostBat WHERE POST_BAT = @PostBatchId";
                    return await connection.QueryFirstOrDefaultAsync<PostBatch>(sql, new { PostBatchId = postBatchId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting Post Batch by ID {postBatchId}: {ex.Message}", ex);
                throw;
            }
        }
    }
}
