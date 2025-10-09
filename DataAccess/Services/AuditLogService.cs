using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing detailed audit log entries.
    /// Provides complete history tracking for sensitive operations.
    /// </summary>
    public class AuditLogService : BaseDatabaseService, IAuditLogService
    {
        public AuditLogService() : base() { }

        /// <summary>
        /// Logs a single audit entry to the AuditLog table
        /// </summary>
        public async Task<bool> LogAsync(AuditLogEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        INSERT INTO AuditLog (
                            TableName, RecordId, Action, FieldName, OldValue, NewValue,
                            ChangedAt, ChangedBy, IPAddress
                        )
                        VALUES (
                            @TableName, @RecordId, @Action, @FieldName, @OldValue, @NewValue,
                            @ChangedAt, @ChangedBy, @IPAddress
                        );";

                    var parameters = new
                    {
                        entry.TableName,
                        entry.RecordId,
                        entry.Action,
                        entry.FieldName,
                        entry.OldValue,
                        entry.NewValue,
                        entry.ChangedAt,
                        entry.ChangedBy,
                        entry.IPAddress
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    
                    if (rowsAffected > 0)
                    {
                        Logger.Debug($"Audit logged: {entry.Action} on {entry.TableName} record {entry.RecordId} by {entry.ChangedBy}");
                    }
                    
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error logging audit entry for {entry.TableName} record {entry.RecordId}: {ex.Message}", ex);
                // Don't throw - audit logging should not break the main operation
                return false;
            }
        }

        /// <summary>
        /// Logs multiple audit entries in a single transaction
        /// </summary>
        public async Task<bool> LogBatchAsync(IEnumerable<AuditLogEntry> entries)
        {
            if (entries == null || !entries.Any())
                return true; // No entries to log is considered success

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var sql = @"
                                INSERT INTO AuditLog (
                                    TableName, RecordId, Action, FieldName, OldValue, NewValue,
                                    ChangedAt, ChangedBy, IPAddress
                                )
                                VALUES (
                                    @TableName, @RecordId, @Action, @FieldName, @OldValue, @NewValue,
                                    @ChangedAt, @ChangedBy, @IPAddress
                                );";

                            int rowsAffected = await connection.ExecuteAsync(sql, entries, transaction: transaction);
                            transaction.Commit();
                            
                            Logger.Debug($"Batch audit logged: {rowsAffected} entries");
                            return rowsAffected > 0;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error logging batch audit entries: {ex.Message}", ex);
                // Don't throw - audit logging should not break the main operation
                return false;
            }
        }

        /// <summary>
        /// Gets all audit entries for a specific table and record
        /// </summary>
        public async Task<List<AuditLogEntry>> GetAuditHistoryAsync(string tableName, int recordId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            AuditId, TableName, RecordId, Action, FieldName, 
                            OldValue, NewValue, ChangedAt, ChangedBy, IPAddress
                        FROM AuditLog
                        WHERE TableName = @TableName
                          AND RecordId = @RecordId
                        ORDER BY ChangedAt DESC, AuditId DESC;";

                    var result = await connection.QueryAsync<AuditLogEntry>(sql, new { TableName = tableName, RecordId = recordId });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting audit history for {tableName} record {recordId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets recent audit entries (last N entries)
        /// </summary>
        public async Task<List<AuditLogEntry>> GetRecentAuditEntriesAsync(int count = 100)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT TOP (@Count)
                            AuditId, TableName, RecordId, Action, FieldName, 
                            OldValue, NewValue, ChangedAt, ChangedBy, IPAddress
                        FROM AuditLog
                        ORDER BY ChangedAt DESC, AuditId DESC;";

                    var result = await connection.QueryAsync<AuditLogEntry>(sql, new { Count = count });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting recent audit entries: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets audit entries for a specific user
        /// </summary>
        public async Task<List<AuditLogEntry>> GetAuditEntriesByUserAsync(string username, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            AuditId, TableName, RecordId, Action, FieldName, 
                            OldValue, NewValue, ChangedAt, ChangedBy, IPAddress
                        FROM AuditLog
                        WHERE ChangedBy = @Username
                          AND (@StartDate IS NULL OR ChangedAt >= @StartDate)
                          AND (@EndDate IS NULL OR ChangedAt <= @EndDate)
                        ORDER BY ChangedAt DESC, AuditId DESC;";

                    var result = await connection.QueryAsync<AuditLogEntry>(sql, new 
                    { 
                        Username = username, 
                        StartDate = startDate, 
                        EndDate = endDate 
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting audit entries for user {username}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets audit entries for a specific table
        /// </summary>
        public async Task<List<AuditLogEntry>> GetAuditEntriesByTableAsync(string tableName, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            AuditId, TableName, RecordId, Action, FieldName, 
                            OldValue, NewValue, ChangedAt, ChangedBy, IPAddress
                        FROM AuditLog
                        WHERE TableName = @TableName
                          AND (@StartDate IS NULL OR ChangedAt >= @StartDate)
                          AND (@EndDate IS NULL OR ChangedAt <= @EndDate)
                        ORDER BY ChangedAt DESC, AuditId DESC;";

                    var result = await connection.QueryAsync<AuditLogEntry>(sql, new 
                    { 
                        TableName = tableName, 
                        StartDate = startDate, 
                        EndDate = endDate 
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting audit entries for table {tableName}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets audit entries for a specific action type (INSERT, UPDATE, DELETE)
        /// </summary>
        public async Task<List<AuditLogEntry>> GetAuditEntriesByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            AuditId, TableName, RecordId, Action, FieldName, 
                            OldValue, NewValue, ChangedAt, ChangedBy, IPAddress
                        FROM AuditLog
                        WHERE Action = @Action
                          AND (@StartDate IS NULL OR ChangedAt >= @StartDate)
                          AND (@EndDate IS NULL OR ChangedAt <= @EndDate)
                        ORDER BY ChangedAt DESC, AuditId DESC;";

                    var result = await connection.QueryAsync<AuditLogEntry>(sql, new 
                    { 
                        Action = action, 
                        StartDate = startDate, 
                        EndDate = endDate 
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting audit entries for action {action}: {ex.Message}", ex);
                throw;
            }
        }
    }
}

