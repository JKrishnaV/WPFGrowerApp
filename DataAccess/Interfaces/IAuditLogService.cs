using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for managing detailed audit log entries.
    /// Provides complete history tracking for sensitive operations.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Logs a single audit entry to the AuditLog table
        /// </summary>
        Task<bool> LogAsync(AuditLogEntry entry);

        /// <summary>
        /// Logs multiple audit entries in a single transaction (for batch operations)
        /// </summary>
        Task<bool> LogBatchAsync(IEnumerable<AuditLogEntry> entries);

        /// <summary>
        /// Gets all audit entries for a specific table and record
        /// </summary>
        Task<List<AuditLogEntry>> GetAuditHistoryAsync(string tableName, int recordId);

        /// <summary>
        /// Gets recent audit entries (last N entries)
        /// </summary>
        Task<List<AuditLogEntry>> GetRecentAuditEntriesAsync(int count = 100);

        /// <summary>
        /// Gets audit entries for a specific user
        /// </summary>
        Task<List<AuditLogEntry>> GetAuditEntriesByUserAsync(string username, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets audit entries for a specific table
        /// </summary>
        Task<List<AuditLogEntry>> GetAuditEntriesByTableAsync(string tableName, DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Gets audit entries for a specific action type (INSERT, UPDATE, DELETE)
        /// </summary>
        Task<List<AuditLogEntry>> GetAuditEntriesByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null);
    }
}

