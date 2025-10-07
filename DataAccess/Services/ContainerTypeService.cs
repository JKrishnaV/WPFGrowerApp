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
        /// Retrieves all container types, ordered by display order and container code.
        /// </summary>
        /// <returns>List of all container types</returns>
        public async Task<IEnumerable<ContainerType>> GetAllAsync()
        {
            const string sql = @"
                SELECT 
                    ContainerId,
                    ContainerCode,
                    ContainerName,
                    TareWeight,
                    Value,
                    IsActive,
                    DisplayOrder,
                    CreatedAt,
                    CreatedBy,
                    ModifiedAt,
                    ModifiedBy,
                    DeletedAt,
                    DeletedBy
                FROM Containers
                WHERE DeletedAt IS NULL
                ORDER BY ISNULL(DisplayOrder, 999), ContainerCode";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<ContainerType>(sql);
            }
        }

        /// <summary>
        /// Retrieves only active (IsActive = true) container types.
        /// Used for dropdowns and lookups during receipt entry.
        /// </summary>
        /// <returns>List of active container types</returns>
        public async Task<IEnumerable<ContainerType>> GetActiveAsync()
        {
            const string sql = @"
                SELECT 
                    ContainerId,
                    ContainerCode,
                    ContainerName,
                    TareWeight,
                    Value,
                    IsActive,
                    DisplayOrder,
                    CreatedAt,
                    CreatedBy,
                    ModifiedAt,
                    ModifiedBy,
                    DeletedAt,
                    DeletedBy
                FROM Containers
                WHERE IsActive = 1
                  AND DeletedAt IS NULL
                ORDER BY ISNULL(DisplayOrder, 999), ContainerCode";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryAsync<ContainerType>(sql);
            }
        }

        /// <summary>
        /// Retrieves a specific container type by ID.
        /// </summary>
        /// <param name="containerId">Container ID</param>
        /// <returns>ContainerType if found, null otherwise</returns>
        public async Task<ContainerType?> GetByIdAsync(int containerId)
        {
            const string sql = @"
                SELECT 
                    ContainerId,
                    ContainerCode,
                    ContainerName,
                    TareWeight,
                    Value,
                    IsActive,
                    DisplayOrder,
                    CreatedAt,
                    CreatedBy,
                    ModifiedAt,
                    ModifiedBy,
                    DeletedAt,
                    DeletedBy
                FROM Containers
                WHERE ContainerId = @ContainerId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<ContainerType>(sql, new { ContainerId = containerId });
            }
        }

        /// <summary>
        /// Checks if a container code already exists in the database.
        /// </summary>
        /// <param name="containerCode">Container code to check</param>
        /// <param name="excludeContainerId">Optional: Exclude this container ID (used during updates)</param>
        /// <returns>True if container code exists, false otherwise</returns>
        public async Task<bool> ContainerCodeExistsAsync(string containerCode, int? excludeContainerId = null)
        {
            string sql = @"
                SELECT COUNT(1)
                FROM Containers
                WHERE UPPER(ContainerCode) = UPPER(@ContainerCode)";

            if (excludeContainerId.HasValue)
            {
                sql += " AND ContainerId != @ExcludeContainerId";
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var count = await connection.ExecuteScalarAsync<int>(sql, new 
                { 
                    ContainerCode = containerCode,
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
            // Check for duplicate container code
            if (await ContainerCodeExistsAsync(containerType.ContainerCode))
            {
                throw new InvalidOperationException($"Container code '{containerType.ContainerCode}' already exists");
            }

            const string sql = @"
                INSERT INTO Containers (
                    ContainerCode, ContainerName, TareWeight, Value, IsActive, DisplayOrder,
                    CreatedAt, CreatedBy
                ) VALUES (
                    @ContainerCode, @ContainerName, @TareWeight, @Value, @IsActive, @DisplayOrder,
                    @CreatedAt, @CreatedBy
                )";

            containerType.CreatedAt = DateTime.Now;
            containerType.CreatedBy = username;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var rowsAffected = await connection.ExecuteAsync(sql, containerType);
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
            // Check for duplicate container code (excluding current record)
            if (await ContainerCodeExistsAsync(containerType.ContainerCode, containerType.ContainerId))
            {
                throw new InvalidOperationException($"Container code '{containerType.ContainerCode}' already exists");
            }

            const string sql = @"
                UPDATE Containers
                SET ContainerCode = @ContainerCode,
                    ContainerName = @ContainerName,
                    TareWeight = @TareWeight,
                    Value = @Value,
                    IsActive = @IsActive,
                    DisplayOrder = @DisplayOrder,
                    ModifiedAt = @ModifiedAt,
                    ModifiedBy = @ModifiedBy
                WHERE ContainerId = @ContainerId";

            containerType.ModifiedAt = DateTime.Now;
            containerType.ModifiedBy = username;

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var rowsAffected = await connection.ExecuteAsync(sql, containerType);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Deletes a container type (soft delete with DeletedAt/DeletedBy).
        /// </summary>
        /// <param name="containerId">Container ID to delete</param>
        /// <param name="username">Username of the operator deleting the record</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteAsync(int containerId, string username)
        {
            const string sql = @"
                UPDATE Containers
                SET DeletedAt = @DeletedAt,
                    DeletedBy = @DeletedBy,
                    IsActive = 0
                WHERE ContainerId = @ContainerId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var rowsAffected = await connection.ExecuteAsync(sql, new 
                { 
                    ContainerId = containerId,
                    DeletedAt = DateTime.Now,
                    DeletedBy = username
                });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Gets the count of receipts using a specific container type.
        /// Used to prevent deletion of container types that are in use.
        /// Updated to query ContainerTransactions table instead of Receipts.ContainerId.
        /// </summary>
        /// <param name="containerId">Container ID to check</param>
        /// <returns>Count of receipts using this container</returns>
        public async Task<int> GetUsageCountAsync(int containerId)
        {
            // Check ContainerTransactions table for container usage
            const string sql = @"
                SELECT COUNT(DISTINCT ReceiptId)
                FROM ContainerTransactions
                WHERE ContainerId = @ContainerId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                return await connection.ExecuteScalarAsync<int>(sql, new { ContainerId = containerId });
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