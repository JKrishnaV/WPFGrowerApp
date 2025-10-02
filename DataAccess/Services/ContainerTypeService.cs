using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing container type data (Contain table).
    /// Handles CRUD operations for container definitions.
    /// </summary>
    public class ContainerTypeService : BaseDatabaseService
    {

        /// <summary>
        /// Retrieves all container types, ordered by Container ID.
        /// </summary>
        /// <returns>List of all container types</returns>
        public async Task<IEnumerable<ContainerType>> GetAllAsync()
        {
            const string sql = @"
                SELECT 
                    CONTAINER as ContainerId,
                    Description,
                    SHORT as ShortCode,
                    TARE as TareWeight,
                    VALUE as Value,
                    INUSE as InUse,
                    QADD_DATE as AddedDate,
                    QADD_TIME as AddedTime,
                    QADD_OP as AddedBy,
                    QED_DATE as EditedDate,
                    QED_TIME as EditedTime,
                    QED_OP as EditedBy,
                    QDEL_DATE as DeletedDate,
                    QDEL_TIME as DeletedTime,
                    QDEL_OP as DeletedBy
                FROM Contain
                WHERE QDEL_DATE IS NULL
                ORDER BY CONTAINER";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<ContainerType>(sql);
            }
        }

        /// <summary>
        /// Retrieves only active (InUse = true) container types.
        /// Used for dropdowns and lookups during receipt entry.
        /// </summary>
        /// <returns>List of active container types</returns>
        public async Task<IEnumerable<ContainerType>> GetActiveAsync()
        {
            const string sql = @"
                SELECT 
                    CONTAINER as ContainerId,
                    Description,
                    SHORT as ShortCode,
                    TARE as TareWeight,
                    VALUE as Value,
                    INUSE as InUse,
                    QADD_DATE as AddedDate,
                    QADD_TIME as AddedTime,
                    QADD_OP as AddedBy,
                    QED_DATE as EditedDate,
                    QED_TIME as EditedTime,
                    QED_OP as EditedBy
                FROM Contain
                WHERE INUSE = 1 
                  AND QDEL_DATE IS NULL
                ORDER BY CONTAINER";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<ContainerType>(sql);
            }
        }

        /// <summary>
        /// Retrieves a specific container type by ID.
        /// </summary>
        /// <param name="containerId">Container ID (1-20)</param>
        /// <returns>ContainerType if found, null otherwise</returns>
        public async Task<ContainerType?> GetByIdAsync(int containerId)
        {
            const string sql = @"
                SELECT 
                    CONTAINER as ContainerId,
                    Description,
                    SHORT as ShortCode,
                    TARE as TareWeight,
                    VALUE as Value,
                    INUSE as InUse,
                    QADD_DATE as AddedDate,
                    QADD_TIME as AddedTime,
                    QADD_OP as AddedBy,
                    QED_DATE as EditedDate,
                    QED_TIME as EditedTime,
                    QED_OP as EditedBy,
                    QDEL_DATE as DeletedDate,
                    QDEL_TIME as DeletedTime,
                    QDEL_OP as DeletedBy
                FROM Contain
                WHERE CONTAINER = @ContainerId
                  AND QDEL_DATE IS NULL";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<ContainerType>(sql, new { ContainerId = containerId });
            }
        }

        /// <summary>
        /// Checks if a container ID already exists in the database.
        /// </summary>
        /// <param name="containerId">Container ID to check</param>
        /// <param name="excludeContainerId">Optional: Exclude this ID (used during updates)</param>
        /// <returns>True if container ID exists, false otherwise</returns>
        public async Task<bool> ContainerIdExistsAsync(int containerId, int? excludeContainerId = null)
        {
            string sql = @"
                SELECT COUNT(1)
                FROM Contain
                WHERE CONTAINER = @ContainerId
                  AND QDEL_DATE IS NULL";

            if (excludeContainerId.HasValue)
            {
                sql += " AND CONTAINER != @ExcludeContainerId";
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(sql, new 
                { 
                    ContainerId = containerId,
                    ExcludeContainerId = excludeContainerId 
                });

                return count > 0;
            }
        }

        /// <summary>
        /// Checks if a short code already exists in the database.
        /// </summary>
        /// <param name="shortCode">Short code to check</param>
        /// <param name="excludeContainerId">Optional: Exclude this container ID (used during updates)</param>
        /// <returns>True if short code exists, false otherwise</returns>
        public async Task<bool> ShortCodeExistsAsync(string shortCode, int? excludeContainerId = null)
        {
            string sql = @"
                SELECT COUNT(1)
                FROM Contain
                WHERE UPPER(SHORT) = UPPER(@ShortCode)
                  AND QDEL_DATE IS NULL";

            if (excludeContainerId.HasValue)
            {
                sql += " AND CONTAINER != @ExcludeContainerId";
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(sql, new 
                { 
                    ShortCode = shortCode,
                    ExcludeContainerId = excludeContainerId 
                });

                return count > 0;
            }
        }

        /// <summary>
        /// Creates a new container type.
        /// </summary>
        /// <param name="containerType">Container type to create</param>
        /// <param name="username">Username of the operator creating the record</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> CreateAsync(ContainerType containerType, string username)
        {
            // Validate container ID range
            if (containerType.ContainerId < 1 || containerType.ContainerId > 20)
            {
                throw new ArgumentException("Container ID must be between 1 and 20");
            }

            // Check for duplicate container ID
            if (await ContainerIdExistsAsync(containerType.ContainerId))
            {
                throw new InvalidOperationException($"Container ID {containerType.ContainerId} already exists");
            }

            // Check for duplicate short code
            if (await ShortCodeExistsAsync(containerType.ShortCode))
            {
                throw new InvalidOperationException($"Short code '{containerType.ShortCode}' already exists");
            }

            const string sql = @"
                INSERT INTO Contain (
                    CONTAINER, Description, SHORT, TARE, VALUE, INUSE,
                    QADD_DATE, QADD_TIME, QADD_OP
                ) VALUES (
                    @ContainerId, @Description, @ShortCode, @TareWeight, @Value, @InUse,
                    @AddedDate, @AddedTime, @AddedBy
                )";

            var now = DateTime.Now;
            var parameters = new
            {
                containerType.ContainerId,
                containerType.Description,
                containerType.ShortCode,
                containerType.TareWeight,
                containerType.Value,
                containerType.InUse,
                AddedDate = now.Date,
                AddedTime = now.ToString("HH:mm:ss"),
                AddedBy = username
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Updates an existing container type.
        /// </summary>
        /// <param name="containerType">Container type with updated values</param>
        /// <param name="username">Username of the operator updating the record</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateAsync(ContainerType containerType, string username)
        {
            // Validate container ID range
            if (containerType.ContainerId < 1 || containerType.ContainerId > 20)
            {
                throw new ArgumentException("Container ID must be between 1 and 20");
            }

            // Check for duplicate short code (excluding current record)
            if (await ShortCodeExistsAsync(containerType.ShortCode, containerType.ContainerId))
            {
                throw new InvalidOperationException($"Short code '{containerType.ShortCode}' already exists");
            }

            const string sql = @"
                UPDATE Contain
                SET Description = @Description,
                    SHORT = @ShortCode,
                    TARE = @TareWeight,
                    VALUE = @Value,
                    INUSE = @InUse,
                    QED_DATE = @EditedDate,
                    QED_TIME = @EditedTime,
                    QED_OP = @EditedBy
                WHERE CONTAINER = @ContainerId
                  AND QDEL_DATE IS NULL";

            var now = DateTime.Now;
            var parameters = new
            {
                containerType.ContainerId,
                containerType.Description,
                containerType.ShortCode,
                containerType.TareWeight,
                containerType.Value,
                containerType.InUse,
                EditedDate = now.Date,
                EditedTime = now.ToString("HH:mm:ss"),
                EditedBy = username
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Soft deletes a container type by setting the QDEL_DATE.
        /// </summary>
        /// <param name="containerId">Container ID to delete</param>
        /// <param name="username">Username of the operator deleting the record</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteAsync(int containerId, string username)
        {
            const string sql = @"
                UPDATE Contain
                SET QDEL_DATE = @DeletedDate,
                    QDEL_TIME = @DeletedTime,
                    QDEL_OP = @DeletedBy
                WHERE CONTAINER = @ContainerId
                  AND QDEL_DATE IS NULL";

            var now = DateTime.Now;
            var parameters = new
            {
                ContainerId = containerId,
                DeletedDate = now.Date,
                DeletedTime = now.ToString("HH:mm:ss"),
                DeletedBy = username
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Gets the count of receipts using a specific container type.
        /// Used to prevent deletion of container types that are in use.
        /// </summary>
        /// <param name="containerId">Container ID to check</param>
        /// <returns>Count of receipts using this container</returns>
        public async Task<int> GetUsageCountAsync(int containerId)
        {
            // Check all IN1-IN20 and OUT1-OUT20 fields in Daily table
            var sql = $@"
                SELECT COUNT(DISTINCT DAY_UNIQ)
                FROM Daily
                WHERE (IN{containerId} != 0 OR OUT{containerId} != 0)
                  AND QDEL_DATE IS NULL";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(sql);
            }
        }

        /// <summary>
        /// Checks if a container type can be safely deleted.
        /// </summary>
        /// <param name="containerId">Container ID to check</param>
        /// <returns>True if safe to delete, false if in use</returns>
        public async Task<bool> CanDeleteAsync(int containerId)
        {
            var usageCount = await GetUsageCountAsync(containerId);
            return usageCount == 0;
        }
    }
}
