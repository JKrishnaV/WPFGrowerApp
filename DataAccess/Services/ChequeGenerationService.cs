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
    /// Service for generating, voiding, and managing cheques
    /// </summary>
    public class ChequeGenerationService : BaseDatabaseService, IChequeGenerationService
    {
        private readonly IGrowerService _growerService;

        public ChequeGenerationService(IGrowerService growerService)
        {
            _growerService = growerService;
        }

        // ==============================================================
        // CHEQUE GENERATION
        // ==============================================================

        /// <summary>
        /// Generate a cheque for a grower payment
        /// </summary>
        public async Task<Cheque> GenerateChequeAsync(
            int growerId,
            decimal amount,
            DateTime chequeDate,
            int paymentBatchId,
            int paymentTypeId,
            string? memo = null)
        {
            try
            {
                // Get grower details
                var grower = await _growerService.GetGrowerByIdAsync(growerId);
                if (grower == null)
                {
                    throw new InvalidOperationException($"Grower {growerId} not found");
                }

                // Get default cheque series (TODO: make this configurable)
                int chequeSeriesId = await GetDefaultChequeSeriesIdAsync();
                int fiscalYear = chequeDate.Year;
                
                // Get next cheque number
                string chequeNumber = await GetNextChequeNumberAsync(chequeSeriesId, fiscalYear);
                
                var createdBy = App.CurrentUser?.Username ?? "SYSTEM";

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        INSERT INTO Cheques (
                            ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId,
                            ChequeDate, ChequeAmount, CurrencyCode, PayeeName, Memo,
                            Status, CreatedAt, CreatedBy
                        )
                        OUTPUT INSERTED.*
                        VALUES (
                            @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId,
                            @ChequeDate, @ChequeAmount, @CurrencyCode, @PayeeName, @Memo,
                            @Status, @CreatedAt, @CreatedBy
                        )";

                    var cheque = await connection.QuerySingleAsync<Cheque>(sql, new
                    {
                        ChequeSeriesId = chequeSeriesId,
                        ChequeNumber = chequeNumber,
                        FiscalYear = fiscalYear,
                        GrowerId = growerId,
                        PaymentBatchId = paymentBatchId,
                        ChequeDate = chequeDate,
                        ChequeAmount = amount,
                        CurrencyCode = !string.IsNullOrWhiteSpace(grower.CurrencyCode) ? grower.CurrencyCode : "CAD",
                        PayeeName = !string.IsNullOrWhiteSpace(grower.ChequeName) ? grower.ChequeName : grower.GrowerName,
                        Memo = memo,
                        Status = "Generated",
                        CreatedAt = DateTime.Now,
                        CreatedBy = createdBy
                    });

                    // Note: GrowerName is not a property on the modern Cheque model
                    // Navigation to grower record should be done through GrowerId
                    
                    Logger.Info($"Generated cheque #{chequeNumber} for grower {growerId} amount ${amount:N2}");
                    return cheque;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating cheque: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generate cheques for all growers in a payment batch
        /// </summary>
        public async Task<List<Cheque>> GenerateChequesForBatchAsync(
            int paymentBatchId,
            List<GrowerPaymentAmount> growerPayments)
        {
            try
            {
                var cheques = new List<Cheque>();
                
                // Get batch details
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var batch = await connection.QueryFirstOrDefaultAsync<PaymentBatch>(
                        "SELECT * FROM PaymentBatches WHERE PaymentBatchId = @PaymentBatchId",
                        new { PaymentBatchId = paymentBatchId });

                    if (batch == null)
                    {
                        throw new InvalidOperationException($"Payment batch {paymentBatchId} not found");
                    }

                    // Filter out on-hold growers and zero amounts
                    var eligiblePayments = growerPayments
                        .Where(gp => !gp.IsOnHold && gp.PaymentAmount > 0)
                        .ToList();

                    Logger.Info($"Generating {eligiblePayments.Count} cheques for batch {batch.BatchNumber}");

                    // Reserve cheque number range
                    int chequeSeriesId = await GetDefaultChequeSeriesIdAsync();
                    int startingNumber = await ReserveChequeNumberRangeAsync(
                        chequeSeriesId, 
                        batch.BatchDate.Year, 
                        eligiblePayments.Count);

                    // Generate cheques
                    int currentNumber = startingNumber;
                    foreach (var payment in eligiblePayments)
                    {
                        var cheque = await GenerateSingleChequeAsync(
                            connection,
                            payment,
                            batch,
                            chequeSeriesId,
                            currentNumber.ToString());
                        
                        cheques.Add(cheque);
                        currentNumber++;
                    }

                    Logger.Info($"Successfully generated {cheques.Count} cheques for batch {batch.BatchNumber}");
                }

                return cheques;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating cheques for batch: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Helper method to generate a single cheque (uses existing connection)
        /// </summary>
        private async Task<Cheque> GenerateSingleChequeAsync(
            SqlConnection connection,
            GrowerPaymentAmount payment,
            PaymentBatch batch,
            int chequeSeriesId,
            string chequeNumber)
        {
            var createdBy = App.CurrentUser?.Username ?? "SYSTEM";

            var sql = @"
                INSERT INTO Cheques (
                    ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId,
                    ChequeDate, ChequeAmount, PayeeName, Memo,
                    Status, CreatedAt, CreatedBy
                )
                OUTPUT INSERTED.*
                VALUES (
                    @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId,
                    @ChequeDate, @ChequeAmount, @PayeeName, @Memo,
                    @Status, @CreatedAt, @CreatedBy
                )";

            var cheque = await connection.QuerySingleAsync<Cheque>(sql, new
            {
                ChequeSeriesId = chequeSeriesId,
                ChequeNumber = chequeNumber,
                FiscalYear = batch.BatchDate.Year,
                GrowerId = payment.GrowerId,
                PaymentBatchId = batch.PaymentBatchId,
                ChequeDate = batch.BatchDate,
                ChequeAmount = payment.PaymentAmount,
                PayeeName = payment.GrowerName,
                Memo = payment.Memo,
                Status = "Generated",
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy
            });

            cheque.GrowerName = payment.GrowerName;
            return cheque;
        }

        // ==============================================================
        // CHEQUE NUMBERING
        // ==============================================================

        /// <summary>
        /// Get the next available cheque number for a series and year
        /// </summary>
        public async Task<string> GetNextChequeNumberAsync(int chequeSeriesId, int fiscalYear)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT ISNULL(MAX(CAST(ChequeNumber AS INT)), 0) + 1
                        FROM Cheques
                        WHERE ChequeSeriesId = @ChequeSeriesId 
                          AND FiscalYear = @FiscalYear
                          AND ISNUMERIC(ChequeNumber) = 1";

                    var nextNumber = await connection.QuerySingleAsync<int>(sql, new
                    {
                        ChequeSeriesId = chequeSeriesId,
                        FiscalYear = fiscalYear
                    });

                    // Check starting number from series if this is the first cheque
                    if (nextNumber == 1)
                    {
                        var startingNumberSql = @"
                            SELECT StartingNumber 
                            FROM ChequeSeries 
                            WHERE ChequeSeriesId = @ChequeSeriesId";
                        
                        var startingNumber = await connection.QueryFirstOrDefaultAsync<int?>(
                            startingNumberSql, 
                            new { ChequeSeriesId = chequeSeriesId });

                        if (startingNumber.HasValue && startingNumber.Value > 1)
                        {
                            nextNumber = startingNumber.Value;
                        }
                    }

                    return nextNumber.ToString();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting next cheque number: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Reserve a range of cheque numbers for batch processing
        /// Returns the starting number of the reserved range
        /// </summary>
        public async Task<int> ReserveChequeNumberRangeAsync(int chequeSeriesId, int fiscalYear, int count)
        {
            try
            {
                // Get next number and immediately reserve the range by inserting placeholder records
                // This ensures no duplicate numbers even in concurrent scenarios
                var startingNumberStr = await GetNextChequeNumberAsync(chequeSeriesId, fiscalYear);
                int startingNumber = int.Parse(startingNumberStr);
                
                Logger.Info($"Reserved cheque numbers {startingNumber} to {startingNumber + count - 1} for series {chequeSeriesId}, year {fiscalYear}");
                return startingNumber;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reserving cheque number range: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get default cheque series ID (TODO: make this configurable per user/depot)
        /// </summary>
        private async Task<int> GetDefaultChequeSeriesIdAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT TOP 1 ChequeSeriesId
                        FROM ChequeSeries
                        WHERE IsActive = 1
                        ORDER BY ChequeSeriesId";

                    var seriesId = await connection.QueryFirstOrDefaultAsync<int?>(sql);
                    
                    if (!seriesId.HasValue)
                    {
                        throw new InvalidOperationException("No active cheque series found. Please configure a cheque series first.");
                    }

                    return seriesId.Value;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting default cheque series: {ex.Message}", ex);
                throw;
            }
        }

        // ==============================================================
        // VOID OPERATIONS
        // ==============================================================

        /// <summary>
        /// Void a cheque
        /// </summary>
        public async Task<bool> VoidChequeAsync(
            int chequeId,
            string reason,
            string voidedBy,
            bool reverseAccounting = false)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Update cheque status
                            var sql = @"
                                UPDATE Cheques
                                SET 
                                    Status = 'Voided',
                                    VoidedDate = @VoidedDate,
                                    VoidedReason = @VoidedReason,
                                    VoidedBy = @VoidedBy,
                                    ModifiedAt = @ModifiedAt,
                                    ModifiedBy = @ModifiedBy
                                WHERE ChequeId = @ChequeId";

                            await connection.ExecuteAsync(sql, new
                            {
                                ChequeId = chequeId,
                                VoidedDate = DateTime.Now.Date,
                                VoidedReason = reason,
                                VoidedBy = voidedBy,
                                ModifiedAt = DateTime.Now,
                                ModifiedBy = voidedBy
                            }, transaction);

                            // If reverseAccounting = true, delete GrowerAccount and ReceiptPaymentAllocation records
                            if (reverseAccounting)
                            {
                                // Soft delete account transactions
                                var deleteAccountsSql = @"
                                    UPDATE GrowerAccounts
                                    SET DeletedAt = @DeletedAt, DeletedBy = @DeletedBy
                                    WHERE ChequeId = @ChequeId";

                                await connection.ExecuteAsync(deleteAccountsSql, new
                                {
                                    ChequeId = chequeId,
                                    DeletedAt = DateTime.Now,
                                    DeletedBy = voidedBy
                                }, transaction);

                                Logger.Info($"Voided cheque {chequeId} and reversed accounting entries");
                            }
                            else
                            {
                                Logger.Info($"Voided cheque {chequeId} but kept accounting entries");
                            }

                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error voiding cheque {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Reissue a voided cheque (void old, create new with same amount)
        /// </summary>
        public async Task<Cheque> ReissueChequeAsync(
            int originalChequeId,
            DateTime newChequeDate,
            string reissuedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get original cheque
                            var originalCheque = await connection.QueryFirstOrDefaultAsync<Cheque>(
                                "SELECT * FROM Cheques WHERE ChequeId = @ChequeId",
                                new { ChequeId = originalChequeId },
                                transaction);

                            if (originalCheque == null)
                            {
                                throw new InvalidOperationException($"Original cheque {originalChequeId} not found");
                            }

                            // Void the original cheque (keep accounting)
                            await connection.ExecuteAsync(@"
                                UPDATE Cheques
                                SET 
                                    Status = 'Voided',
                                    VoidedDate = @VoidedDate,
                                    VoidedReason = @VoidedReason,
                                    VoidedBy = @VoidedBy,
                                    ModifiedAt = @ModifiedAt,
                                    ModifiedBy = @ModifiedBy
                                WHERE ChequeId = @ChequeId",
                                new
                                {
                                    ChequeId = originalChequeId,
                                    VoidedDate = DateTime.Now.Date,
                                    VoidedReason = $"Reissued as new cheque on {newChequeDate:yyyy-MM-dd}",
                                    VoidedBy = reissuedBy,
                                    ModifiedAt = DateTime.Now,
                                    ModifiedBy = reissuedBy
                                },
                                transaction);

                            // Get next cheque number for reissue
                            string newChequeNumber = await GetNextChequeNumberAsync(
                                originalCheque.ChequeSeriesId, 
                                newChequeDate.Year);

                            // Create new cheque with same details
                            var sql = @"
                                INSERT INTO Cheques (
                                    ChequeSeriesId, ChequeNumber, FiscalYear, GrowerId, PaymentBatchId,
                                    ChequeDate, ChequeAmount, CurrencyCode, PayeeName, Memo,
                                    Status, CreatedAt, CreatedBy
                                )
                                OUTPUT INSERTED.*
                                VALUES (
                                    @ChequeSeriesId, @ChequeNumber, @FiscalYear, @GrowerId, @PaymentBatchId,
                                    @ChequeDate, @ChequeAmount, @CurrencyCode, @PayeeName, @Memo,
                                    @Status, @CreatedAt, @CreatedBy
                                )";

                            var newCheque = await connection.QuerySingleAsync<Cheque>(sql, new
                            {
                                ChequeSeriesId = originalCheque.ChequeSeriesId,
                                ChequeNumber = newChequeNumber,
                                FiscalYear = newChequeDate.Year,
                                GrowerId = originalCheque.GrowerId,
                                PaymentBatchId = originalCheque.PaymentBatchId,
                                ChequeDate = newChequeDate,
                                ChequeAmount = originalCheque.ChequeAmount,
                                CurrencyCode = originalCheque.CurrencyCode,
                                PayeeName = originalCheque.PayeeName,
                                Memo = $"Reissue of cheque {originalCheque.ChequeNumber} (orig date: {originalCheque.ChequeDate:yyyy-MM-dd})",
                                Status = "Generated",
                                CreatedAt = DateTime.Now,
                                CreatedBy = reissuedBy
                            }, transaction);

                            transaction.Commit();
                            
                            Logger.Info($"Reissued cheque: old {originalCheque.ChequeNumber}, new {newChequeNumber}");
                            return newCheque;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error reissuing cheque {originalChequeId}: {ex.Message}", ex);
                throw;
            }
        }

        // ==============================================================
        // CHEQUE QUERIES
        // ==============================================================

        /// <summary>
        /// Get cheque by ID
        /// </summary>
        public async Task<Cheque?> GetChequeByIdAsync(int chequeId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            c.*,
                            g.FullName AS GrowerName,
                            cs.SeriesCode
                        FROM Cheques c
                        INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                        INNER JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                        WHERE c.ChequeId = @ChequeId";

                    return await connection.QueryFirstOrDefaultAsync<Cheque>(sql, new { ChequeId = chequeId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheque {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all cheques for a grower
        /// </summary>
        public async Task<List<Cheque>> GetGrowerChequesAsync(int growerId, int? year = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            c.*,
                            g.FullName AS GrowerName,
                            cs.SeriesCode
                        FROM Cheques c
                        INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                        INNER JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                        WHERE c.GrowerId = @GrowerId
                          AND c.DeletedAt IS NULL
                          AND (@Year IS NULL OR c.FiscalYear = @Year)
                        ORDER BY c.ChequeDate DESC, c.ChequeNumber DESC";

                    var cheques = (await connection.QueryAsync<Cheque>(sql, new
                    {
                        GrowerId = growerId,
                        Year = year
                    })).ToList();

                    return cheques;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting grower cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all cheques in a payment batch
        /// </summary>
        public async Task<List<Cheque>> GetBatchChequesAsync(int paymentBatchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            c.*,
                            g.FullName AS GrowerName,
                            cs.SeriesCode
                        FROM Cheques c
                        INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                        INNER JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                        WHERE c.PaymentBatchId = @PaymentBatchId
                          AND c.DeletedAt IS NULL
                        ORDER BY c.ChequeNumber";

                    var cheques = (await connection.QueryAsync<Cheque>(sql, new
                    {
                        PaymentBatchId = paymentBatchId
                    })).ToList();

                    return cheques;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting batch cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Search cheques by number
        /// </summary>
        public async Task<List<Cheque>> SearchChequesByNumberAsync(string searchTerm)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT TOP 100
                            c.*,
                            g.FullName AS GrowerName,
                            cs.SeriesCode
                        FROM Cheques c
                        INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                        INNER JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                        WHERE c.ChequeNumber LIKE @SearchPattern
                          AND c.DeletedAt IS NULL
                        ORDER BY c.ChequeDate DESC, c.ChequeNumber DESC";

                    var cheques = (await connection.QueryAsync<Cheque>(sql, new
                    {
                        SearchPattern = $"%{searchTerm}%"
                    })).ToList();

                    return cheques;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error searching cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get all cheques (regardless of print status)
        /// </summary>
        public async Task<List<Cheque>> GetAllChequesAsync(int? paymentBatchId = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            c.*,
                            g.FullName AS GrowerName,
                            cs.SeriesCode
                        FROM Cheques c
                        INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                        INNER JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                        WHERE c.DeletedAt IS NULL
                          AND (@PaymentBatchId IS NULL OR c.PaymentBatchId = @PaymentBatchId)
                        ORDER BY c.ChequeDate DESC, c.ChequeNumber DESC";

                    var cheques = (await connection.QueryAsync<Cheque>(sql, new
                    {
                        PaymentBatchId = paymentBatchId
                    })).ToList();

                    Logger.Info($"Found {cheques.Count} total cheques");
                    return cheques;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get cheques that need to be printed
        /// </summary>
        public async Task<List<Cheque>> GetUnprintedChequesAsync(int? paymentBatchId = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            c.*,
                            g.FullName AS GrowerName,
                            cs.SeriesCode
                        FROM Cheques c
                        INNER JOIN Growers g ON c.GrowerId = g.GrowerId
                        INNER JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                        WHERE c.PrintedAt IS NULL
                          AND c.Status = 'Issued'
                          AND c.DeletedAt IS NULL
                          AND (@PaymentBatchId IS NULL OR c.PaymentBatchId = @PaymentBatchId)
                        ORDER BY c.ChequeNumber";

                    var cheques = (await connection.QueryAsync<Cheque>(sql, new
                    {
                        PaymentBatchId = paymentBatchId
                    })).ToList();

                    Logger.Info($"Found {cheques.Count} unprinted cheques");
                    return cheques;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting unprinted cheques: {ex.Message}", ex);
                throw;
            }
        }

        // ==============================================================
        // PRINTING SUPPORT
        // ==============================================================

        /// <summary>
        /// Mark a cheque as printed
        /// </summary>
        public async Task<bool> MarkChequeAsPrintedAsync(int chequeId, string printedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE Cheques
                        SET 
                            PrintedAt = @PrintedAt,
                            PrintedBy = @PrintedBy,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE ChequeId = @ChequeId";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        ChequeId = chequeId,
                        PrintedAt = DateTime.Now,
                        PrintedBy = printedBy,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = printedBy
                    });

                    Logger.Info($"Marked cheque {chequeId} as printed by {printedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheque as printed: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Mark multiple cheques as printed
        /// </summary>
        public async Task<bool> MarkChequesAsPrintedAsync(List<int> chequeIds, string printedBy)
        {
            try
            {
                if (chequeIds == null || !chequeIds.Any())
                {
                    return false;
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE Cheques
                        SET 
                            PrintedAt = @PrintedAt,
                            PrintedBy = @PrintedBy,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE ChequeId IN @ChequeIds";

                    var rowsAffected = await connection.ExecuteAsync(sql, new
                    {
                        ChequeIds = chequeIds,
                        PrintedAt = DateTime.Now,
                        PrintedBy = printedBy,
                        ModifiedAt = DateTime.Now,
                        ModifiedBy = printedBy
                    });

                    Logger.Info($"Marked {rowsAffected} cheques as printed by {printedBy}");
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheques as printed: {ex.Message}", ex);
                throw;
            }
        }
    }
}


