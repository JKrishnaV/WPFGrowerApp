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
    public class GrowerAccountService : BaseDatabaseService, IGrowerAccountService
    {
        public async Task<GrowerAccount> CreateGrowerAccountAsync(GrowerAccount account)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        INSERT INTO GrowerAccounts (
                            GrowerId, TransactionDate, TransactionType, Description,
                            DebitAmount, CreditAmount, PaymentBatchId, ReceiptId, ChequeId,
                            CurrencyCode, ExchangeRate, CreatedAt, CreatedBy
                        )
                        OUTPUT INSERTED.AccountId, INSERTED.GrowerId, INSERTED.TransactionDate, 
                               INSERTED.TransactionType, INSERTED.Description, INSERTED.DebitAmount,
                               INSERTED.CreditAmount, INSERTED.PaymentBatchId, INSERTED.ReceiptId,
                               INSERTED.ChequeId, INSERTED.CurrencyCode, INSERTED.ExchangeRate,
                               INSERTED.CreatedAt, INSERTED.CreatedBy
                        VALUES (
                            @GrowerId, @TransactionDate, @TransactionType, @Description,
                            @DebitAmount, @CreditAmount, @PaymentBatchId, @ReceiptId, @ChequeId,
                            @CurrencyCode, @ExchangeRate, @CreatedAt, @CreatedBy
                        )";

                    var result = await connection.QueryFirstOrDefaultAsync<GrowerAccount>(sql, new
                    {
                        account.GrowerId,
                        account.TransactionDate,
                        account.TransactionType,
                        account.Description,
                        account.DebitAmount,
                        account.CreditAmount,
                        account.PaymentBatchId,
                        account.ReceiptId,
                        account.ChequeId,
                        account.CurrencyCode,
                        account.ExchangeRate,
                        CreatedAt = DateTime.Now,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                    });

                    Logger.Info($"Created grower account {result?.AccountId} for grower {account.GrowerId}");
                    return result ?? account;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating grower account for grower {account.GrowerId}", ex);
                throw;
            }
        }

        public async Task<GrowerAccount> CreateGrowerAccountAsync(GrowerAccount account, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    INSERT INTO GrowerAccounts (
                        GrowerId, TransactionDate, TransactionType, Description,
                        DebitAmount, CreditAmount, PaymentBatchId, ReceiptId, ChequeId,
                        CurrencyCode, ExchangeRate, CreatedAt, CreatedBy
                    )
                    OUTPUT INSERTED.AccountId, INSERTED.GrowerId, INSERTED.TransactionDate, 
                           INSERTED.TransactionType, INSERTED.Description, INSERTED.DebitAmount,
                           INSERTED.CreditAmount, INSERTED.PaymentBatchId, INSERTED.ReceiptId,
                           INSERTED.ChequeId, INSERTED.CurrencyCode, INSERTED.ExchangeRate,
                           INSERTED.CreatedAt, INSERTED.CreatedBy
                    VALUES (
                        @GrowerId, @TransactionDate, @TransactionType, @Description,
                        @DebitAmount, @CreditAmount, @PaymentBatchId, @ReceiptId, @ChequeId,
                        @CurrencyCode, @ExchangeRate, @CreatedAt, @CreatedBy
                    )";

                var result = await connection.QueryFirstOrDefaultAsync<GrowerAccount>(sql, new
                {
                    account.GrowerId,
                    account.TransactionDate,
                    account.TransactionType,
                    account.Description,
                    account.DebitAmount,
                    account.CreditAmount,
                    account.PaymentBatchId,
                    account.ReceiptId,
                    account.ChequeId,
                    account.CurrencyCode,
                    account.ExchangeRate,
                    CreatedAt = DateTime.Now,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                }, transaction);

                Logger.Info($"Created grower account {result?.AccountId} for grower {account.GrowerId} (in transaction)");
                return result ?? account;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating grower account for grower {account.GrowerId} in transaction", ex);
                throw;
            }
        }

        public async Task<List<GrowerAccount>> GetGrowerAccountsAsync(int growerId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            AccountId, GrowerId, TransactionDate, TransactionType, Description,
                            DebitAmount, CreditAmount, PaymentBatchId, ReceiptId, ChequeId,
                            CurrencyCode, ExchangeRate, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM GrowerAccounts 
                        WHERE GrowerId = @GrowerId 
                          AND DeletedAt IS NULL
                        ORDER BY TransactionDate DESC, AccountId DESC";

                    var result = await connection.QueryAsync<GrowerAccount>(sql, new { GrowerId = growerId });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting grower accounts for grower {growerId}", ex);
                throw;
            }
        }

        public async Task<List<GrowerAccount>> GetAccountsByPaymentBatchAsync(int paymentBatchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            AccountId, GrowerId, TransactionDate, TransactionType, Description,
                            DebitAmount, CreditAmount, PaymentBatchId, ReceiptId, ChequeId,
                            CurrencyCode, ExchangeRate, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM GrowerAccounts 
                        WHERE PaymentBatchId = @PaymentBatchId 
                          AND DeletedAt IS NULL
                        ORDER BY TransactionDate, AccountId";

                    var result = await connection.QueryAsync<GrowerAccount>(sql, new { PaymentBatchId = paymentBatchId });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting accounts for payment batch {paymentBatchId}", ex);
                throw;
            }
        }

        public async Task<List<GrowerAccount>> GetAccountsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            AccountId, GrowerId, TransactionDate, TransactionType, Description,
                            DebitAmount, CreditAmount, PaymentBatchId, ReceiptId, ChequeId,
                            CurrencyCode, ExchangeRate, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
                        FROM GrowerAccounts 
                        WHERE TransactionDate >= @StartDate 
                          AND TransactionDate <= @EndDate
                          AND DeletedAt IS NULL
                        ORDER BY TransactionDate DESC, AccountId DESC";

                    var result = await connection.QueryAsync<GrowerAccount>(sql, new 
                    { 
                        StartDate = startDate.Date, 
                        EndDate = endDate.Date.AddDays(1).AddTicks(-1) 
                    });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting accounts for date range {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}", ex);
                throw;
            }
        }

        public async Task<decimal> GetGrowerBalanceAsync(int growerId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            ISNULL(SUM(CreditAmount), 0) - ISNULL(SUM(DebitAmount), 0) as Balance
                        FROM GrowerAccounts 
                        WHERE GrowerId = @GrowerId 
                          AND DeletedAt IS NULL";

                    var result = await connection.QueryFirstOrDefaultAsync<decimal>(sql, new { GrowerId = growerId });
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting balance for grower {growerId}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateGrowerAccountAsync(GrowerAccount account)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE GrowerAccounts 
                        SET 
                            TransactionDate = @TransactionDate,
                            TransactionType = @TransactionType,
                            Description = @Description,
                            DebitAmount = @DebitAmount,
                            CreditAmount = @CreditAmount,
                            PaymentBatchId = @PaymentBatchId,
                            ReceiptId = @ReceiptId,
                            ChequeId = @ChequeId,
                            CurrencyCode = @CurrencyCode,
                            ExchangeRate = @ExchangeRate,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE AccountId = @AccountId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        account.AccountId,
                        account.TransactionDate,
                        account.TransactionType,
                        account.Description,
                        account.DebitAmount,
                        account.CreditAmount,
                        account.PaymentBatchId,
                        account.ReceiptId,
                        account.ChequeId,
                        account.CurrencyCode,
                        account.ExchangeRate,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = App.CurrentUser?.Username ?? "SYSTEM"
                    });

                    Logger.Info($"Updated grower account {account.AccountId}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating grower account {account.AccountId}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteGrowerAccountAsync(int accountId, string deletedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE GrowerAccounts 
                        SET 
                            DeletedAt = @DeletedAt,
                            DeletedBy = @DeletedBy
                        WHERE AccountId = @AccountId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        AccountId = accountId,
                        DeletedAt = DateTime.Now,
                        DeletedBy = deletedBy
                    });

                    Logger.Info($"Soft deleted grower account {accountId}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting grower account {accountId}", ex);
                throw;
            }
        }

        public async Task<int> CreatePaymentBatchAccountsAsync(List<GrowerAccount> accounts, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    INSERT INTO GrowerAccounts (
                        GrowerId, TransactionDate, TransactionType, Description,
                        DebitAmount, CreditAmount, PaymentBatchId, ReceiptId, ChequeId,
                        CurrencyCode, ExchangeRate, CreatedAt, CreatedBy
                    )
                    VALUES (
                        @GrowerId, @TransactionDate, @TransactionType, @Description,
                        @DebitAmount, @CreditAmount, @PaymentBatchId, @ReceiptId, @ChequeId,
                        @CurrencyCode, @ExchangeRate, @CreatedAt, @CreatedBy
                    )";

                var createdBy = App.CurrentUser?.Username ?? "SYSTEM";
                var createdAt = DateTime.Now;

                var rowsAffected = 0;
                foreach (var account in accounts)
                {
                    var result = await connection.ExecuteAsync(sql, new
                    {
                        account.GrowerId,
                        account.TransactionDate,
                        account.TransactionType,
                        account.Description,
                        account.DebitAmount,
                        account.CreditAmount,
                        account.PaymentBatchId,
                        account.ReceiptId,
                        account.ChequeId,
                        account.CurrencyCode,
                        account.ExchangeRate,
                        CreatedAt = createdAt,
                        CreatedBy = createdBy
                    }, transaction);

                    rowsAffected += result;
                }

                Logger.Info($"Created {rowsAffected} grower account entries for payment batch");
                return rowsAffected;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment batch accounts", ex);
                throw;
            }
        }
    }
}
