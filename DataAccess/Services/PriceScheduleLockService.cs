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
    public class PriceScheduleLockService : BaseDatabaseService, IPriceScheduleLockService
    {
        public async Task<PriceScheduleLock> CreatePriceScheduleLockAsync(PriceScheduleLock lockEntry)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        INSERT INTO PriceScheduleLocks (
                            PriceScheduleId, PaymentTypeId, PaymentBatchId,
                            LockedAt, LockedBy, CreatedAt, CreatedBy
                        )
                        OUTPUT INSERTED.PriceScheduleLockId, INSERTED.PriceScheduleId, 
                               INSERTED.PaymentTypeId, INSERTED.PaymentBatchId, INSERTED.LockedAt,
                               INSERTED.LockedBy, INSERTED.CreatedAt, INSERTED.CreatedBy
                        VALUES (
                            @PriceScheduleId, @PaymentTypeId, @PaymentBatchId,
                            @LockedAt, @LockedBy, @CreatedAt, @CreatedBy
                        )";

                    var result = await connection.QueryFirstOrDefaultAsync<PriceScheduleLock>(sql, new
                    {
                        lockEntry.PriceScheduleId,
                        lockEntry.PaymentTypeId,
                        lockEntry.PaymentBatchId,
                        LockedAt = DateTime.Now,
                        LockedBy = App.CurrentUser?.Username ?? "SYSTEM",
                        CreatedAt = DateTime.Now,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                    });

                    Logger.Info($"Created price schedule lock {result?.PriceScheduleLockId} for schedule {lockEntry.PriceScheduleId}, type {lockEntry.PaymentTypeId}");
                    return result ?? lockEntry;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating price schedule lock for schedule {lockEntry.PriceScheduleId}, type {lockEntry.PaymentTypeId}", ex);
                throw;
            }
        }

        public async Task<PriceScheduleLock> CreatePriceScheduleLockAsync(PriceScheduleLock lockEntry, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    INSERT INTO PriceScheduleLocks (
                        PriceScheduleId, PaymentTypeId, PaymentBatchId,
                        LockedAt, LockedBy, CreatedAt, CreatedBy
                    )
                    OUTPUT INSERTED.PriceScheduleLockId, INSERTED.PriceScheduleId, 
                           INSERTED.PaymentTypeId, INSERTED.PaymentBatchId, INSERTED.LockedAt,
                           INSERTED.LockedBy, INSERTED.CreatedAt, INSERTED.CreatedBy
                    VALUES (
                        @PriceScheduleId, @PaymentTypeId, @PaymentBatchId,
                        @LockedAt, @LockedBy, @CreatedAt, @CreatedBy
                    )";

                var result = await connection.QueryFirstOrDefaultAsync<PriceScheduleLock>(sql, new
                {
                    lockEntry.PriceScheduleId,
                    lockEntry.PaymentTypeId,
                    lockEntry.PaymentBatchId,
                    LockedAt = DateTime.Now,
                    LockedBy = App.CurrentUser?.Username ?? "SYSTEM",
                    CreatedAt = DateTime.Now,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                }, transaction);

                Logger.Info($"Created price schedule lock {result?.PriceScheduleLockId} for schedule {lockEntry.PriceScheduleId}, type {lockEntry.PaymentTypeId} (in transaction)");
                return result ?? lockEntry;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating price schedule lock for schedule {lockEntry.PriceScheduleId}, type {lockEntry.PaymentTypeId} in transaction", ex);
                throw;
            }
        }

        public async Task<List<PriceScheduleLock>> GetLocksByPaymentBatchAsync(int paymentBatchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PriceScheduleLockId, PriceScheduleId, PaymentTypeId, PaymentBatchId,
                            LockedAt, LockedBy, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PriceScheduleLocks 
                        WHERE PaymentBatchId = @PaymentBatchId 
                          AND DeletedAt IS NULL
                        ORDER BY LockedAt DESC";

                    var result = await connection.QueryAsync<PriceScheduleLock>(sql, new { PaymentBatchId = paymentBatchId });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting locks for payment batch {paymentBatchId}", ex);
                throw;
            }
        }

        public async Task<List<PriceScheduleLock>> GetLocksByScheduleAndTypeAsync(int priceScheduleId, int paymentTypeId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PriceScheduleLockId, PriceScheduleId, PaymentTypeId, PaymentBatchId,
                            LockedAt, LockedBy, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PriceScheduleLocks 
                        WHERE PriceScheduleId = @PriceScheduleId 
                          AND PaymentTypeId = @PaymentTypeId
                          AND DeletedAt IS NULL
                        ORDER BY LockedAt DESC";

                    var result = await connection.QueryAsync<PriceScheduleLock>(sql, new 
                    { 
                        PriceScheduleId = priceScheduleId, 
                        PaymentTypeId = paymentTypeId 
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting locks for schedule {priceScheduleId}, type {paymentTypeId}", ex);
                throw;
            }
        }

        public async Task<bool> IsPriceScheduleLockedAsync(int priceScheduleId, int paymentTypeId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT COUNT(1)
                        FROM PriceScheduleLocks 
                        WHERE PriceScheduleId = @PriceScheduleId 
                          AND PaymentTypeId = @PaymentTypeId
                          AND DeletedAt IS NULL";

                    var count = await connection.QueryFirstOrDefaultAsync<int>(sql, new 
                    { 
                        PriceScheduleId = priceScheduleId, 
                        PaymentTypeId = paymentTypeId 
                    });

                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking if schedule {priceScheduleId}, type {paymentTypeId} is locked", ex);
                throw;
            }
        }

        public async Task<List<PriceScheduleLock>> GetLocksByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            PriceScheduleLockId, PriceScheduleId, PaymentTypeId, PaymentBatchId,
                            LockedAt, LockedBy, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM PriceScheduleLocks 
                        WHERE LockedAt >= @StartDate 
                          AND LockedAt <= @EndDate
                          AND DeletedAt IS NULL
                        ORDER BY LockedAt DESC";

                    var result = await connection.QueryAsync<PriceScheduleLock>(sql, new 
                    { 
                        StartDate = startDate, 
                        EndDate = endDate.AddDays(1).AddTicks(-1) 
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting locks for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}", ex);
                throw;
            }
        }

        public async Task<bool> UpdatePriceScheduleLockAsync(PriceScheduleLock lockEntry)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PriceScheduleLocks 
                        SET 
                            PriceScheduleId = @PriceScheduleId,
                            PaymentTypeId = @PaymentTypeId,
                            PaymentBatchId = @PaymentBatchId,
                            LockedAt = @LockedAt,
                            LockedBy = @LockedBy,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PriceScheduleLockId = @PriceScheduleLockId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        lockEntry.PriceScheduleLockId,
                        lockEntry.PriceScheduleId,
                        lockEntry.PaymentTypeId,
                        lockEntry.PaymentBatchId,
                        lockEntry.LockedAt,
                        lockEntry.LockedBy,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = App.CurrentUser?.Username ?? "SYSTEM"
                    });

                    Logger.Info($"Updated price schedule lock {lockEntry.PriceScheduleLockId}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating price schedule lock {lockEntry.PriceScheduleLockId}", ex);
                throw;
            }
        }

        public async Task<bool> DeletePriceScheduleLockAsync(int lockId, string deletedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PriceScheduleLocks 
                        SET 
                            DeletedAt = @DeletedAt,
                            DeletedBy = @DeletedBy
                        WHERE PriceScheduleLockId = @PriceScheduleLockId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PriceScheduleLockId = lockId,
                        DeletedAt = DateTime.Now,
                        DeletedBy = deletedBy
                    });

                    Logger.Info($"Soft deleted price schedule lock {lockId}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting price schedule lock {lockId}", ex);
                throw;
            }
        }

        public async Task<int> CreatePaymentBatchLocksAsync(List<PriceScheduleLock> locks, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // Deduplicate locks by PriceScheduleId and PaymentTypeId combination
                var uniqueLocks = locks
                    .GroupBy(l => new { l.PriceScheduleId, l.PaymentTypeId })
                    .Select(g => g.First())
                    .ToList();

                Logger.Info($"Deduplicated {locks.Count} locks to {uniqueLocks.Count} unique locks");

                var sql = @"
                    INSERT INTO PriceScheduleLocks (
                        PriceScheduleId, PaymentTypeId, PaymentBatchId,
                        LockedAt, LockedBy, CreatedAt, CreatedBy
                    )
                    VALUES (
                        @PriceScheduleId, @PaymentTypeId, @PaymentBatchId,
                        @LockedAt, @LockedBy, @CreatedAt, @CreatedBy
                    )";

                var lockedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var lockedAt = DateTime.Now;
                var createdAt = DateTime.Now;
                var createdBy = App.CurrentUser?.Username ?? "SYSTEM";

                var rowsAffected = 0;
                foreach (var lockEntry in uniqueLocks)
                {
                    try
                    {
                        var result = await connection.ExecuteAsync(sql, new
                        {
                            lockEntry.PriceScheduleId,
                            lockEntry.PaymentTypeId,
                            lockEntry.PaymentBatchId,
                            LockedAt = lockedAt,
                            LockedBy = lockedBy,
                            CreatedAt = createdAt,
                            CreatedBy = createdBy
                        }, transaction);

                        rowsAffected += result;
                    }
                    catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
                    {
                        // Log the duplicate but continue processing other locks
                        Logger.Warn($"Price schedule lock already exists for schedule {lockEntry.PriceScheduleId}, type {lockEntry.PaymentTypeId}. Skipping.");
                    }
                }

                Logger.Info($"Created {rowsAffected} price schedule locks for payment batch");
                return rowsAffected;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment batch locks", ex);
                throw;
            }
        }

        public async Task<int> RemovePaymentBatchLocksAsync(int paymentBatchId, string deletedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE PriceScheduleLocks 
                        SET 
                            DeletedAt = @DeletedAt,
                            DeletedBy = @DeletedBy
                        WHERE PaymentBatchId = @PaymentBatchId 
                          AND DeletedAt IS NULL";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        PaymentBatchId = paymentBatchId,
                        DeletedAt = DateTime.Now,
                        DeletedBy = deletedBy
                    });

                    Logger.Info($"Removed {rowsAffected} price schedule locks for payment batch {paymentBatchId}");
                    return rowsAffected;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error removing price schedule locks for payment batch {paymentBatchId}", ex);
                throw;
            }
        }
    }
}
