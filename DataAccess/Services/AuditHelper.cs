using System;
using System.Data;
using Microsoft.Data.SqlClient;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Helper class for managing audit columns in database operations.
    /// Provides consistent audit trail tracking across all services.
    /// </summary>
    public static class AuditHelper
    {
        /// <summary>
        /// Sets the audit columns for a new entity being created.
        /// Sets CreatedAt to current time and CreatedBy to the provided username.
        /// </summary>
        /// <param name="entity">The entity that inherits from AuditableEntity</param>
        /// <param name="username">The username of the user creating the record</param>
        public static void SetCreatedAudit(AuditableEntity entity, string username)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.CreatedAt = DateTime.Now;
            entity.CreatedBy = username ?? "SYSTEM";
        }

        /// <summary>
        /// Sets the audit columns for an entity being modified.
        /// Sets ModifiedAt to current time and ModifiedBy to the provided username.
        /// </summary>
        /// <param name="entity">The entity that inherits from AuditableEntity</param>
        /// <param name="username">The username of the user modifying the record</param>
        public static void SetModifiedAudit(AuditableEntity entity, string username)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.ModifiedAt = DateTime.Now;
            entity.ModifiedBy = username ?? "SYSTEM";
        }

        /// <summary>
        /// Sets the audit columns for an entity being soft-deleted.
        /// Sets DeletedAt to current time and DeletedBy to the provided username.
        /// </summary>
        /// <param name="entity">The entity that inherits from AuditableEntity</param>
        /// <param name="username">The username of the user deleting the record</param>
        public static void SetDeletedAudit(AuditableEntity entity, string username)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.DeletedAt = DateTime.Now;
            entity.DeletedBy = username ?? "SYSTEM";
        }

        /// <summary>
        /// Clears the soft-delete audit columns (used when restoring a deleted entity).
        /// Sets DeletedAt and DeletedBy to null.
        /// </summary>
        /// <param name="entity">The entity that inherits from AuditableEntity</param>
        public static void ClearDeletedAudit(AuditableEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.DeletedAt = null;
            entity.DeletedBy = null;
        }

        /// <summary>
        /// Adds audit column parameters for CREATE operations to a SqlCommand.
        /// Adds CreatedAt and CreatedBy parameters.
        /// </summary>
        /// <param name="cmd">The SqlCommand to add parameters to</param>
        /// <param name="username">The username of the user creating the record</param>
        public static void AddCreateAuditParameters(SqlCommand cmd, string username)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = DateTime.Now;
            cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 50).Value = username ?? "SYSTEM";
        }

        /// <summary>
        /// Adds audit column parameters for UPDATE operations to a SqlCommand.
        /// Adds ModifiedAt and ModifiedBy parameters.
        /// </summary>
        /// <param name="cmd">The SqlCommand to add parameters to</param>
        /// <param name="username">The username of the user modifying the record</param>
        public static void AddModifyAuditParameters(SqlCommand cmd, string username)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            cmd.Parameters.Add("@ModifiedAt", SqlDbType.DateTime2).Value = DateTime.Now;
            cmd.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 50).Value = username ?? "SYSTEM";
        }

        /// <summary>
        /// Adds audit column parameters for SOFT DELETE operations to a SqlCommand.
        /// Adds DeletedAt and DeletedBy parameters.
        /// </summary>
        /// <param name="cmd">The SqlCommand to add parameters to</param>
        /// <param name="username">The username of the user deleting the record</param>
        public static void AddDeleteAuditParameters(SqlCommand cmd, string username)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            cmd.Parameters.Add("@DeletedAt", SqlDbType.DateTime2).Value = DateTime.Now;
            cmd.Parameters.Add("@DeletedBy", SqlDbType.NVarChar, 50).Value = username ?? "SYSTEM";
        }

        /// <summary>
        /// Adds all audit column parameters (Create, Modify, Delete) to a SqlCommand.
        /// This is useful for INSERT statements that want to initialize all audit columns.
        /// </summary>
        /// <param name="cmd">The SqlCommand to add parameters to</param>
        /// <param name="username">The username of the user creating the record</param>
        public static void AddAllAuditParameters(SqlCommand cmd, string username)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));

            // Created (required)
            cmd.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = DateTime.Now;
            cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 50).Value = username ?? "SYSTEM";
            
            // Modified (nullable)
            cmd.Parameters.Add("@ModifiedAt", SqlDbType.DateTime2).Value = DBNull.Value;
            cmd.Parameters.Add("@ModifiedBy", SqlDbType.NVarChar, 50).Value = DBNull.Value;
            
            // Deleted (nullable)
            cmd.Parameters.Add("@DeletedAt", SqlDbType.DateTime2).Value = DBNull.Value;
            cmd.Parameters.Add("@DeletedBy", SqlDbType.NVarChar, 50).Value = DBNull.Value;
        }

        /// <summary>
        /// Generates SQL column list for audit columns in INSERT statements.
        /// Returns: "CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, DeletedAt, DeletedBy"
        /// </summary>
        public static string GetAuditColumnsForInsert()
        {
            return "CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, DeletedAt, DeletedBy";
        }

        /// <summary>
        /// Generates SQL parameter list for audit columns in INSERT statements.
        /// Returns: "@CreatedAt, @CreatedBy, @ModifiedAt, @ModifiedBy, @DeletedAt, @DeletedBy"
        /// </summary>
        public static string GetAuditParametersForInsert()
        {
            return "@CreatedAt, @CreatedBy, @ModifiedAt, @ModifiedBy, @DeletedAt, @DeletedBy";
        }

        /// <summary>
        /// Generates SQL SET clause for audit columns in UPDATE statements.
        /// Returns: "ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy"
        /// </summary>
        public static string GetAuditSetClauseForUpdate()
        {
            return "ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy";
        }

        /// <summary>
        /// Generates SQL SET clause for soft-delete operations.
        /// Returns: "DeletedAt = @DeletedAt, DeletedBy = @DeletedBy"
        /// </summary>
        public static string GetAuditSetClauseForDelete()
        {
            return "DeletedAt = @DeletedAt, DeletedBy = @DeletedBy";
        }

        /// <summary>
        /// Generates SQL WHERE clause to exclude soft-deleted records.
        /// Returns: "DeletedAt IS NULL"
        /// </summary>
        public static string GetWhereClauseExcludeDeleted()
        {
            return "DeletedAt IS NULL";
        }

        /// <summary>
        /// Generates SQL WHERE clause to include only soft-deleted records.
        /// Returns: "DeletedAt IS NOT NULL"
        /// </summary>
        public static string GetWhereClauseOnlyDeleted()
        {
            return "DeletedAt IS NOT NULL";
        }
    }
}

