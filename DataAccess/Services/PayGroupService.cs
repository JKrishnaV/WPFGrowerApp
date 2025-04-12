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
            // Dapper maps PayGroupId property to PAYGRP column automatically
            const string sql = "SELECT PAYGRP AS PayGroupId, Description, DEF_PRLVL AS DefaultPayLevel, QADD_DATE, QADD_TIME, QADD_OP, QED_DATE, QED_TIME, QED_OP FROM PayGrp WHERE QDEL_DATE IS NULL ORDER BY PAYGRP;";
            using (var connection = CreateConnection())
            {
                return await connection.QueryAsync<PayGroup>(sql);
            }
        }

        /// <summary>
        /// Retrieves a specific PayGroup record by its ID asynchronously.
        /// </summary>
        public async Task<PayGroup> GetPayGroupByIdAsync(string payGroupId)
        {
            // Dapper maps PayGroupId property to PAYGRP column automatically
            // Parameter name @PayGroupId matches the C# property name
            const string sql = "SELECT PAYGRP AS PayGroupId, Description, DEF_PRLVL AS DefaultPayLevel, QADD_DATE, QADD_TIME, QADD_OP, QED_DATE, QED_TIME, QED_OP FROM PayGrp WHERE PAYGRP = @PayGroupId AND QDEL_DATE IS NULL;";
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
            // var currentUser = _userService.GetCurrentUser()?.Username ?? "SYSTEM"; // TODO: Implement getting current user
            var currentUser = "SYSTEM"; // Temporary placeholder
            payGroup.QADD_DATE = DateTime.Now.Date;
            payGroup.QADD_TIME = DateTime.Now.ToString("HH:mm:ss");
            payGroup.QADD_OP = currentUser; // Use placeholder
            payGroup.QED_DATE = null;
            payGroup.QED_TIME = null;
            payGroup.QED_OP = null;
            payGroup.QDEL_DATE = null; // Ensure not marked as deleted
            payGroup.QDEL_TIME = null;
            payGroup.QDEL_OP = null;


            const string sql = @"
                INSERT INTO PayGrp (PAYGRP, Description, DEF_PRLVL, QADD_DATE, QADD_TIME, QADD_OP)
                VALUES (@PayGroupId, @Description, @DefaultPayLevel, @QADD_DATE, @QADD_TIME, @QADD_OP);";

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
            // var currentUser = _userService.GetCurrentUser()?.Username ?? "SYSTEM"; // TODO: Implement getting current user
            var currentUser = "SYSTEM"; // Temporary placeholder
            payGroup.QED_DATE = DateTime.Now.Date;
            payGroup.QED_TIME = DateTime.Now.ToString("HH:mm:ss");
            payGroup.QED_OP = currentUser; // Use placeholder

            const string sql = @"
                UPDATE PayGrp
                SET Description = @Description,
                    DEF_PRLVL = @DefaultPayLevel,
                    QED_DATE = @QED_DATE,
                    QED_TIME = @QED_TIME,
                    QED_OP = @QED_OP
                WHERE PAYGRP = @PayGroupId AND QDEL_DATE IS NULL;";

            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, payGroup);
                return affectedRows > 0;
            }
        }

        /// <summary>
        /// Deletes a PayGroup record by its ID asynchronously (soft delete).
        /// </summary>
        public async Task<bool> DeletePayGroupAsync(string payGroupId)
        {
            // var currentUser = _userService.GetCurrentUser()?.Username ?? "SYSTEM"; // TODO: Implement getting current user
            var currentUser = "SYSTEM"; // Temporary placeholder
            var deleteDate = DateTime.Now.Date;
            var deleteTime = DateTime.Now.ToString("HH:mm:ss");

            const string sql = @"
                UPDATE PayGrp
                SET QDEL_DATE = @DeleteDate,
                    QDEL_TIME = @DeleteTime,
                    QDEL_OP = @CurrentUser
                WHERE PAYGRP = @PayGroupId AND QDEL_DATE IS NULL;";

            using (var connection = CreateConnection())
            {
                var affectedRows = await connection.ExecuteAsync(sql, new { PayGroupId = payGroupId, DeleteDate = deleteDate, DeleteTime = deleteTime, CurrentUser = currentUser });
                return affectedRows > 0;
            }
        }
    }
}
