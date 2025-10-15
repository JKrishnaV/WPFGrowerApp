using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for payment method data operations.
    /// </summary>
    public class PaymentMethodService : BaseDatabaseService, IPaymentMethodService
    {
        public PaymentMethodService() : base() { }

        public async Task<List<PaymentMethod>> GetAllPaymentMethodsAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT PaymentMethodId, MethodName, IsActive, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PaymentMethods
                        WHERE IsActive = 1
                        ORDER BY MethodName";

                    return (await connection.QueryAsync<PaymentMethod>(sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetAllPaymentMethodsAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<PaymentMethod> GetPaymentMethodByIdAsync(int paymentMethodId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT PaymentMethodId, MethodName, IsActive, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PaymentMethods
                        WHERE PaymentMethodId = @PaymentMethodId";

                    var parameters = new { PaymentMethodId = paymentMethodId };
                    return await connection.QueryFirstOrDefaultAsync<PaymentMethod>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetPaymentMethodByIdAsync for PaymentMethodId {paymentMethodId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<int> CreatePaymentMethodAsync(PaymentMethod paymentMethod)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO PaymentMethods (MethodName, IsActive, CreatedAt, CreatedBy)
                        VALUES (@MethodName, @IsActive, GETDATE(), @CreatedBy);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new
                    {
                        paymentMethod.MethodName,
                        paymentMethod.IsActive,
                        CreatedBy = currentUser
                    };

                    return await connection.QuerySingleAsync<int>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in CreatePaymentMethodAsync: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> UpdatePaymentMethodAsync(PaymentMethod paymentMethod)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PaymentMethods SET
                            MethodName = @MethodName,
                            IsActive = @IsActive,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentMethodId = @PaymentMethodId";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new
                    {
                        paymentMethod.PaymentMethodId,
                        paymentMethod.MethodName,
                        paymentMethod.IsActive,
                        ModifiedBy = currentUser
                    };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in UpdatePaymentMethodAsync for PaymentMethodId {paymentMethod?.PaymentMethodId}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeletePaymentMethodAsync(int paymentMethodId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PaymentMethods SET
                            IsActive = 0,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentMethodId = @PaymentMethodId";

                    var currentUser = App.CurrentUser?.Username ?? "SYSTEM";
                    var parameters = new { PaymentMethodId = paymentMethodId, ModifiedBy = currentUser };

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in DeletePaymentMethodAsync for PaymentMethodId {paymentMethodId}: {ex.Message}", ex);
                throw;
            }
        }
    }
}
