using Dapper;
// using Microsoft.Extensions.Configuration; // No longer needed
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service class for handling data operations for PayGroup entities.
    /// </summary>
    public class PayGroupService : BaseDatabaseService, IPayGroupService // Inherit from BaseDatabaseService
    {
        // _connectionString is inherited from BaseDatabaseService
        private readonly IUserService _userService; 

        // Constructor now only needs IUserService, connection string comes from base
        public PayGroupService(IUserService userService) : base() 
        {
            _userService = userService; 
        }

        // CreateConnection() is inherited from BaseDatabaseService

        /// <summary>
        /// Retrieves all PayGroup records asynchronously.
        /// </summary>
        public async Task<IEnumerable<PayGroup>> GetAllPayGroupsAsync()
        {
            const string sql = @"
                SELECT 
                    PaymentGroupId,
                    GroupCode,
                    GroupName,
                    Description,
                    DefaultPriceLevel,
                    IsActive,
                    CreatedAt,
                    CreatedBy,
                    ModifiedAt,
                    ModifiedBy,
                    DeletedAt,
                    DeletedBy
                FROM PaymentGroups 
                WHERE DeletedAt IS NULL 
                ORDER BY GroupCode";
                
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<PayGroup>(sql);
            }
        }

        /// <summary>
        /// Retrieves a specific PayGroup record by its code asynchronously.
        /// </summary>
        public async Task<PayGroup> GetPayGroupByIdAsync(string payGroupId)
        {
            const string sql = @"
                SELECT 
                    PaymentGroupId,
                    GroupCode,
                    GroupName,
                    Description,
                    DefaultPriceLevel,
                    IsActive,
                    CreatedAt,
                    CreatedBy,
                    ModifiedAt,
                    ModifiedBy,
                    DeletedAt,
                    DeletedBy
                FROM PaymentGroups 
                WHERE GroupCode = @PayGroupId 
                  AND DeletedAt IS NULL";
                  
            using (var connection = CreateConnection())
            {
                return await connection.QuerySingleOrDefaultAsync<PayGroup>(sql, new { PayGroupId = payGroupId });
            }
        }

        /// <summary>
        /// Adds a new PayGroup record asynchronously.
        /// </summary>
        public async Task<bool> AddPayGroupAsync(PayGroup payGroup)
        {
            var currentUser = "SYSTEM"; // Temporary placeholder
            payGroup.CreatedAt = DateTime.Now;
            payGroup.CreatedBy = currentUser;
            payGroup.IsActive = true;

            const string sql = @"
                INSERT INTO PaymentGroups (GroupCode, GroupName, Description, DefaultPriceLevel, IsActive, CreatedAt, CreatedBy)
                VALUES (@GroupCode, @GroupName, @Description, @DefaultPriceLevel, @IsActive, @CreatedAt, @CreatedBy)";

            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, payGroup);
                return affectedRows > 0;
            }
        }

        /// <summary>
        /// Updates an existing PayGroup record asynchronously.
        /// </summary>
        public async Task<bool> UpdatePayGroupAsync(PayGroup payGroup)
        {
            var currentUser = "SYSTEM"; // Temporary placeholder
            payGroup.ModifiedAt = DateTime.Now;
            payGroup.ModifiedBy = currentUser;

            const string sql = @"
                UPDATE PaymentGroups
                SET GroupName = @GroupName,
                    Description = @Description,
                    DefaultPriceLevel = @DefaultPriceLevel,
                    IsActive = @IsActive,
                    ModifiedAt = @ModifiedAt,
                    ModifiedBy = @ModifiedBy
                WHERE PaymentGroupId = @PaymentGroupId 
                  AND DeletedAt IS NULL";

            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, payGroup);
                return affectedRows > 0;
            }
        }

        /// <summary>
        /// Deletes a PayGroup record by its code asynchronously (soft delete).
        /// </summary>
        public async Task<bool> DeletePayGroupAsync(string payGroupId)
        {
            var currentUser = "SYSTEM"; // Temporary placeholder
            var deleteTime = DateTime.Now;

            const string sql = @"
                UPDATE PaymentGroups
                SET DeletedAt = @DeleteTime,
                    DeletedBy = @CurrentUser
                WHERE GroupCode = @PayGroupId 
                  AND DeletedAt IS NULL";

            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, new { PayGroupId = payGroupId, DeleteTime = deleteTime, CurrentUser = currentUser });
                return affectedRows > 0;
            }
        }
    }
}
