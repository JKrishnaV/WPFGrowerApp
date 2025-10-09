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
    /// Service for managing payment types (Advance 1, Advance 2, Advance 3, Final, etc.)
    /// Implements caching for performance since payment types rarely change
    /// </summary>
    public class PaymentTypeService : BaseDatabaseService, IPaymentTypeService
    {
        // Cache payment types in memory for performance
        private static List<PaymentType>? _cachedPaymentTypes;
        private static DateTime? _cacheLastUpdated;
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Get all active payment types (with caching)
        /// </summary>
        public async Task<List<PaymentType>> GetAllPaymentTypesAsync()
        {
            try
            {
                // Return from cache if valid
                if (_cachedPaymentTypes != null && 
                    _cacheLastUpdated.HasValue && 
                    DateTime.Now - _cacheLastUpdated.Value < CacheExpiration)
                {
                    Logger.Info("Returning cached payment types");
                    return _cachedPaymentTypes;
                }

                // Load from database
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PaymentTypeId,
                            TypeCode,
                            TypeName,
                            SequenceNumber,
                            IsFinalPayment,
                            Description,
                            DisplayOrder,
                            IsActive,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM PaymentTypes
                        WHERE IsActive = 1 AND DeletedAt IS NULL
                        ORDER BY SequenceNumber";

                    var paymentTypes = (await connection.QueryAsync<PaymentType>(sql)).ToList();
                    
                    // Update cache
                    _cachedPaymentTypes = paymentTypes;
                    _cacheLastUpdated = DateTime.Now;
                    
                    Logger.Info($"Loaded {paymentTypes.Count} payment types from database");
                    return paymentTypes;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all payment types: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get payment type by ID
        /// </summary>
        public async Task<PaymentType?> GetPaymentTypeByIdAsync(int paymentTypeId)
        {
            try
            {
                // Try cache first
                var allTypes = await GetAllPaymentTypesAsync();
                var cachedType = allTypes.FirstOrDefault(pt => pt.PaymentTypeId == paymentTypeId);
                if (cachedType != null)
                {
                    return cachedType;
                }

                // If not in cache, query database directly
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PaymentTypeId,
                            TypeCode,
                            TypeName,
                            SequenceNumber,
                            IsFinalPayment,
                            Description,
                            DisplayOrder,
                            IsActive,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM PaymentTypes
                        WHERE PaymentTypeId = @PaymentTypeId";

                    return await connection.QueryFirstOrDefaultAsync<PaymentType>(sql, new { PaymentTypeId = paymentTypeId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment type by ID {paymentTypeId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get payment type by code (ADV1, ADV2, ADV3, FINAL, etc.)
        /// </summary>
        public async Task<PaymentType?> GetPaymentTypeByCodeAsync(string paymentTypeCode)
        {
            try
            {
                // Try cache first
                var allTypes = await GetAllPaymentTypesAsync();
                var cachedType = allTypes.FirstOrDefault(pt => 
                    pt.TypeCode.Equals(paymentTypeCode, StringComparison.OrdinalIgnoreCase));
                
                if (cachedType != null)
                {
                    return cachedType;
                }

                // If not in cache, query database directly
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PaymentTypeId,
                            TypeCode,
                            TypeName,
                            SequenceNumber,
                            IsFinalPayment,
                            Description,
                            DisplayOrder,
                            IsActive,
                            CreatedAt,
                            CreatedBy,
                            ModifiedAt,
                            ModifiedBy,
                            DeletedAt,
                            DeletedBy
                        FROM PaymentTypes
                        WHERE TypeCode = @TypeCode AND IsActive = 1 AND DeletedAt IS NULL";

                    return await connection.QueryFirstOrDefaultAsync<PaymentType>(sql, new { TypeCode = paymentTypeCode });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment type by code '{paymentTypeCode}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get advance payment types only (ADV1, ADV2, ADV3, ADV4, etc.)
        /// Excludes FINAL, SPECIAL, LOAN types
        /// </summary>
        public async Task<List<PaymentType>> GetAdvancePaymentTypesAsync()
        {
            try
            {
                var allTypes = await GetAllPaymentTypesAsync();
                
                // Filter to only advance payments (TypeCode starts with "ADV" and not final payment)
                var advanceTypes = allTypes
                    .Where(pt => pt.IsAdvancePayment && !pt.IsFinalPayment)
                    .OrderBy(pt => pt.SequenceNumber)
                    .ToList();
                
                Logger.Info($"Found {advanceTypes.Count} advance payment types");
                return advanceTypes;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance payment types: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Clear the cache (call this if payment types are modified)
        /// </summary>
        public static void ClearCache()
        {
            _cachedPaymentTypes = null;
            _cacheLastUpdated = null;
            Logger.Info("Payment types cache cleared");
        }
    }
}


