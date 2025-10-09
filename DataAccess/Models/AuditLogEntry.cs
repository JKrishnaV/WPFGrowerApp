using System;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents a detailed audit log entry in the AuditLog table.
    /// Provides complete history tracking for sensitive tables.
    /// </summary>
    public class AuditLogEntry
    {
        /// <summary>
        /// Unique identifier for the audit log entry
        /// </summary>
        public long AuditId { get; set; }
        
        /// <summary>
        /// Name of the table that was modified (e.g., "AppUsers", "Growers")
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// ID of the record that was modified
        /// </summary>
        public int RecordId { get; set; }
        
        /// <summary>
        /// Type of action performed: INSERT, UPDATE, or DELETE
        /// </summary>
        public string Action { get; set; }
        
        /// <summary>
        /// Name of the field that was changed (for UPDATE actions)
        /// </summary>
        public string? FieldName { get; set; }
        
        /// <summary>
        /// Previous value before the change (for UPDATE actions)
        /// </summary>
        public string? OldValue { get; set; }
        
        /// <summary>
        /// New value after the change (for UPDATE and INSERT actions)
        /// </summary>
        public string? NewValue { get; set; }
        
        /// <summary>
        /// When the change occurred
        /// </summary>
        public DateTime ChangedAt { get; set; }
        
        /// <summary>
        /// Username of the person who made the change
        /// </summary>
        public string? ChangedBy { get; set; }
        
        /// <summary>
        /// IP address of the user who made the change (optional)
        /// </summary>
        public string? IPAddress { get; set; }

        /// <summary>
        /// Creates a new audit log entry for an INSERT action
        /// </summary>
        public static AuditLogEntry CreateInsertEntry(string tableName, int recordId, string username, string? fieldName = null, string? newValue = null)
        {
            return new AuditLogEntry
            {
                TableName = tableName,
                RecordId = recordId,
                Action = "INSERT",
                FieldName = fieldName,
                NewValue = newValue,
                ChangedAt = DateTime.Now,
                ChangedBy = username
            };
        }

        /// <summary>
        /// Creates a new audit log entry for an UPDATE action
        /// </summary>
        public static AuditLogEntry CreateUpdateEntry(string tableName, int recordId, string fieldName, string? oldValue, string? newValue, string username)
        {
            return new AuditLogEntry
            {
                TableName = tableName,
                RecordId = recordId,
                Action = "UPDATE",
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedAt = DateTime.Now,
                ChangedBy = username
            };
        }

        /// <summary>
        /// Creates a new audit log entry for a DELETE action
        /// </summary>
        public static AuditLogEntry CreateDeleteEntry(string tableName, int recordId, string username, string? fieldName = null, string? oldValue = null)
        {
            return new AuditLogEntry
            {
                TableName = tableName,
                RecordId = recordId,
                Action = "DELETE",
                FieldName = fieldName,
                OldValue = oldValue,
                ChangedAt = DateTime.Now,
                ChangedBy = username
            };
        }
    }
}

