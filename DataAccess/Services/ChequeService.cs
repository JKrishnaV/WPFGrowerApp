using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for managing cheque operations
    /// NOTE: This service is currently STUBBED OUT pending modernization for the new Cheque model.
    /// The original implementation used legacy column names and properties that no longer exist.
    /// ChequeGenerationService handles core cheque generation for Phase 1.
    /// TODO (Phase 1.5): Rewrite all methods to use modern Cheques table schema.
    /// </summary>
    public class ChequeService : BaseDatabaseService, IChequeService
    {
        // ==================================================================================
        // TEMPORARY STUB IMPLEMENTATIONS - ALL METHODS NEED REWRITING FOR MODERN SCHEMA
        // ==================================================================================
        
        public async Task<Cheque> GetChequeByIdAsync(int chequeId)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                // Comprehensive query to load all cheque information with related data
                // Using only columns that actually exist in the database schema
                var sql = @"
                    SELECT 
                        -- Core Cheque Properties (verified to exist)
                        c.ChequeId, c.ChequeSeriesId, c.ChequeNumber, c.FiscalYear,
                        c.GrowerId, c.PaymentBatchId, c.ChequeDate, c.ChequeAmount,
                        c.CurrencyCode, c.ExchangeRate, c.PayeeName, c.Memo,
                        c.Status, c.ClearedDate, c.VoidedDate, c.VoidedReason,
                        c.CreatedAt, c.CreatedBy, c.ModifiedAt, c.ModifiedBy,
                        c.DeletedAt, c.DeletedBy, c.PrintedAt, c.PrintedBy,
                        c.VoidedBy, c.IssuedAt, c.IssuedBy,
                        
                        -- Unified Cheque System Properties
                        c.IsConsolidated, c.ConsolidatedFromBatches, c.IsAdvanceCheque, c.AdvanceChequeId,
                        
                        -- Related Grower Information
                        g.FullName AS GrowerName, g.GrowerNumber, g.Address AS GrowerAddress,
                        g.PhoneNumber AS GrowerPhone, g.Email AS GrowerEmail,
                        
                        -- Cheque Series Information
                        cs.SeriesCode,
                        
                        -- Payment Batch Information
                        pb.BatchNumber, pb.PaymentTypeId, pb.BatchDate,
                        pt.TypeName AS PaymentTypeName
                        
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    LEFT JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                    LEFT JOIN PaymentBatches pb ON c.PaymentBatchId = pb.PaymentBatchId
                    LEFT JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                    WHERE c.ChequeId = @ChequeId AND c.DeletedAt IS NULL";
                
                var cheque = await connection.QueryFirstOrDefaultAsync<Cheque>(sql, new { ChequeId = chequeId });
                
                if (cheque == null)
                {
                    Logger.Warn($"Cheque with ID {chequeId} not found");
                    return null!;
                }

                // Load additional related data
                await LoadChequeRelatedDataAsync(cheque, connection);
                
                Logger.Info($"Successfully loaded comprehensive cheque data for ChequeId {chequeId}");
                return cheque;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting comprehensive cheque data by ID {chequeId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Loads additional related data for a cheque including receipts and advance deductions
        /// </summary>
        private async Task LoadChequeRelatedDataAsync(Cheque cheque, SqlConnection connection)
        {
            try
            {
                // Load receipt details for this cheque
                var receiptSql = @"
                    SELECT 
                        r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                        r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                        r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                        r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                        r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                        r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                        g.FullName as GrowerName, g.GrowerNumber, g.Address as GrowerAddress, 
                        g.PhoneNumber as GrowerPhone, g.Email as GrowerEmail,
                        p.Description as ProductName, p.Description as ProductDescription,
                        pr.ProcessName, pr.Description as ProcessDescription,
                        v.VarietyName,
                        d.DepotName, d.Address as DepotAddress,
                        pc.ClassName as PriceClassName,
                        rpa.PricePerPound, rpa.AmountPaid as TotalAmountPaid,
                        rpa.PaymentTypeId,
                        pt.TypeName as PaymentTypeName
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    INNER JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                    INNER JOIN Cheques c ON pb.PaymentBatchId = c.PaymentBatchId
                    LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                    LEFT JOIN Products p ON r.ProductId = p.ProductId
                    LEFT JOIN Processes pr ON r.ProcessId = pr.ProcessId
                    LEFT JOIN Varieties v ON r.VarietyId = v.VarietyId
                    LEFT JOIN Depots d ON r.DepotId = d.DepotId
                    LEFT JOIN PriceClasses pc ON r.PriceClassId = pc.PriceClassId
                    LEFT JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                    WHERE c.ChequeId = @ChequeId
                      AND r.GrowerId = c.GrowerId  -- Only show receipts for the specific grower of this cheque
                      AND r.DeletedAt IS NULL
                    ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC";

                var receipts = await connection.QueryAsync<ReceiptDetailDto>(receiptSql, new { ChequeId = cheque.ChequeId });
                // Note: We would need to add a ReceiptDetails property to the Cheque model to store this data
                // For now, we'll log the receipt count for debugging
                Logger.Info($"Loaded {receipts.Count()} receipt details for cheque {cheque.ChequeId}");

                // Load advance deductions for regular cheques that have deducted from advance cheques
                // Note: This is for regular cheques that have deducted from advance cheques, not for advance cheques themselves
                var advanceDeductionSql = @"
                    SELECT 
                        ad.DeductionId, ad.AdvanceChequeId, ad.ChequeId, ad.PaymentBatchId,
                        ad.DeductionAmount, ad.DeductionDate, ad.CreatedBy, ad.CreatedAt,
                        ac.AdvanceAmount, ac.AdvanceDate, ac.Reason,
                        pb.BatchNumber
                    FROM AdvanceDeductions ad
                    INNER JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                    INNER JOIN Cheques c ON ad.ChequeId = c.ChequeId
                    LEFT JOIN PaymentBatches pb ON ad.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.ChequeId = @ChequeId
                    ORDER BY ad.DeductionDate DESC";

                var advanceDeductions = await connection.QueryAsync<AdvanceDeduction>(advanceDeductionSql, new { ChequeId = cheque.ChequeId });
                Logger.Info($"Loaded {advanceDeductions.Count()} advance deductions for cheque {cheque.ChequeId}");

                // Load consolidated cheque information if this is a consolidated cheque
                if (cheque.IsConsolidated)
                {
                    var consolidatedSql = @"
                        SELECT 
                            cc.ConsolidatedChequeId, cc.ChequeId, cc.PaymentBatchId, cc.Amount,
                            cc.CreatedAt, cc.CreatedBy,
                            pb.BatchNumber, pb.PaymentTypeId,
                            pt.TypeName AS PaymentTypeName
                        FROM ConsolidatedCheques cc
                        INNER JOIN PaymentBatches pb ON cc.PaymentBatchId = pb.PaymentBatchId
                        LEFT JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                        WHERE cc.ChequeId = @ChequeId
                        ORDER BY cc.CreatedAt DESC";

                    var consolidatedData = await connection.QueryAsync<ConsolidatedCheque>(consolidatedSql, new { ChequeId = cheque.ChequeId });
                    // Note: We would need to add a ConsolidatedCheques property to the Cheque model to store this data
                    Logger.Info($"Loaded {consolidatedData.Count()} consolidated cheque entries for cheque {cheque.ChequeId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading related data for cheque {cheque.ChequeId}: {ex.Message}", ex);
                // Don't throw here - we still want to return the main cheque data even if related data fails to load
            }
        }

        /// <summary>
        /// Gets an advance cheque by ID from the AdvanceCheques table
        /// This is separate from regular cheques which are in the Cheques table
        /// </summary>
        public async Task<Cheque> GetAdvanceChequeByIdAsync(int advanceChequeId)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                // Query to load advance cheque information with related data
                var sql = @"
                    SELECT 
                        -- Advance Cheque Properties (mapped to Cheque model)
                        ac.AdvanceChequeId as ChequeId,
                        NULL as ChequeSeriesId,
                        'ADV-' + CAST(ac.AdvanceChequeId as VARCHAR) as ChequeNumber,
                        YEAR(ac.AdvanceDate) as FiscalYear,
                        ac.GrowerId,
                        NULL as PaymentBatchId,
                        ac.AdvanceDate as ChequeDate,
                        ac.AdvanceAmount as ChequeAmount,
                        'CAD' as CurrencyCode,
                        1.0 as ExchangeRate,
                        g.FullName as PayeeName,
                        ac.Reason as Memo,
                        ac.Status,
                        NULL as ClearedDate,
                        ac.VoidedDate,
                        ac.VoidedReason,
                        ac.CreatedAt,
                        ac.CreatedBy,
                        ac.ModifiedAt,
                        ac.ModifiedBy,
                        ac.DeletedAt,
                        ac.DeletedBy,
                        ac.PrintedDate as PrintedAt,
                        ac.PrintedBy,
                        NULL as VoidedBy,
                        NULL as IssuedAt,
                        NULL as IssuedBy,
                        
                        -- Advance Cheque specific properties
                        0 as IsConsolidated,
                        NULL as ConsolidatedFromBatches,
                        1 as IsAdvanceCheque,
                        ac.AdvanceChequeId as AdvanceChequeId,
                        
                        -- Related Grower Information
                        g.FullName AS GrowerName, g.GrowerNumber, g.Address AS GrowerAddress,
                        g.PhoneNumber AS GrowerPhone, g.Email AS GrowerEmail,
                        
                        -- Advance cheque series (always 'ADV')
                        'ADV' as SeriesCode,
                        
                        -- No batch information for advance cheques
                        NULL as BatchNumber,
                        NULL as PaymentTypeId,
                        NULL as PaymentTypeName
                        
                    FROM AdvanceCheques ac
                    LEFT JOIN Growers g ON ac.GrowerId = g.GrowerId
                    WHERE ac.AdvanceChequeId = @AdvanceChequeId AND ac.DeletedAt IS NULL";
                
                var advanceCheque = await connection.QueryFirstOrDefaultAsync<Cheque>(sql, new { AdvanceChequeId = advanceChequeId });
                
                if (advanceCheque == null)
                {
                    Logger.Warn($"Advance cheque with ID {advanceChequeId} not found");
                    return null!;
                }

                // Load additional related data for advance cheques
                await LoadAdvanceChequeRelatedDataAsync(advanceCheque, connection);
                
                Logger.Info($"Successfully loaded comprehensive advance cheque data for AdvanceChequeId {advanceChequeId}");
                return advanceCheque;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting comprehensive advance cheque data by ID {advanceChequeId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Loads additional related data for advance cheques
        /// </summary>
        private async Task LoadAdvanceChequeRelatedDataAsync(Cheque advanceCheque, SqlConnection connection)
        {
            try
            {
                // Load advance deductions that were deducted from this advance cheque
                var advanceDeductionSql = @"
                    SELECT 
                        ad.DeductionId, ad.AdvanceChequeId, ad.ChequeId, ad.PaymentBatchId,
                        ad.DeductionAmount, ad.DeductionDate, ad.CreatedBy, ad.CreatedAt,
                        ac.AdvanceAmount, ac.AdvanceDate, ac.Reason,
                        pb.BatchNumber
                    FROM AdvanceDeductions ad
                    INNER JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                    LEFT JOIN PaymentBatches pb ON ad.PaymentBatchId = pb.PaymentBatchId
                    WHERE ad.AdvanceChequeId = @AdvanceChequeId
                    ORDER BY ad.DeductionDate DESC";

                var advanceDeductions = await connection.QueryAsync<AdvanceDeduction>(advanceDeductionSql, new { AdvanceChequeId = advanceCheque.ChequeId });
                Logger.Info($"Loaded {advanceDeductions.Count()} advance deductions for advance cheque {advanceCheque.ChequeId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading related data for advance cheque {advanceCheque.ChequeId}: {ex.Message}", ex);
                // Don't throw here - we still want to return the main advance cheque data even if related data fails to load
            }
        }

        public async Task<bool> VoidChequeAsync(int chequeId, string reason, string voidedBy)
        {
            try
            {
                using var connection = CreateConnection();
                await connection.OpenAsync();
                
                var query = @"UPDATE Cheques 
                             SET Status = 'Voided',
                                 VoidedDate = @VoidedDate,
                                 VoidedBy = @VoidedBy,
                                 VoidedReason = @Reason
                             WHERE ChequeId = @ChequeId";
                             
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ChequeId", chequeId);
                command.Parameters.AddWithValue("@VoidedDate", DateTime.Now);
                command.Parameters.AddWithValue("@VoidedBy", voidedBy);
                command.Parameters.AddWithValue("@Reason", reason);
                
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error voiding cheque: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Cheque>> GetAllChequesAsync()
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        c.ChequeId,
                        c.ChequeSeriesId,
                        c.ChequeNumber,
                        c.FiscalYear,
                        c.GrowerId,
                        c.PaymentBatchId,
                        c.ChequeDate,
                        c.ChequeAmount,
                        c.CurrencyCode,
                        c.ExchangeRate,
                        c.PayeeName,
                        c.Memo,
                        c.Status,
                        c.ClearedDate,
                        c.VoidedDate,
                        c.VoidedReason,
                        c.CreatedAt,
                        c.CreatedBy,
                        c.ModifiedAt,
                        c.ModifiedBy,
                        c.DeletedAt,
                        c.DeletedBy,
                        c.PrintedAt,
                        c.PrintedBy,
                        c.VoidedBy,
                        c.IssuedAt,
                        c.IssuedBy,
                        g.FullName AS GrowerName,
                        g.GrowerNumber,
                        cs.SeriesCode,
                        pb.BatchNumber
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    LEFT JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                    LEFT JOIN PaymentBatches pb ON c.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.DeletedAt IS NULL 
                    AND c.Status IN ('Printed', 'Voided', 'Stopped')
                    ORDER BY c.ChequeDate DESC";
                
                var cheques = await connection.QueryAsync<Cheque>(sql);
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all cheques: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        public async Task<Cheque> GetChequeBySeriesAndNumberAsync(string series, decimal chequeNumber)
        {
            // TODO: Rewrite to use ChequeSeriesId and ChequeNumber (int)
            Logger.Warn($"ChequeService.GetChequeBySeriesAndNumberAsync({series}, {chequeNumber}) not yet implemented for modern schema");
            await Task.CompletedTask;
            return null!;
        }

        public async Task<List<Cheque>> GetChequesByGrowerNumberAsync(decimal growerNumber)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        c.ChequeId,
                        c.ChequeSeriesId,
                        c.ChequeNumber,
                        c.FiscalYear,
                        c.GrowerId,
                        c.PaymentBatchId,
                        c.ChequeDate,
                        c.ChequeAmount,
                        c.CurrencyCode,
                        c.ExchangeRate,
                        c.PayeeName,
                        c.Memo,
                        c.Status,
                        c.ClearedDate,
                        c.VoidedDate,
                        c.VoidedReason,
                        c.CreatedAt,
                        c.CreatedBy,
                        c.ModifiedAt,
                        c.ModifiedBy,
                        c.DeletedAt,
                        c.DeletedBy,
                        c.PrintedAt,
                        c.PrintedBy,
                        c.VoidedBy,
                        c.IssuedAt,
                        c.IssuedBy,
                        g.FullName AS GrowerName,
                        g.GrowerNumber,
                        cs.SeriesCode,
                        pb.BatchNumber
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    LEFT JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                    LEFT JOIN PaymentBatches pb ON c.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.DeletedAt IS NULL 
                    AND g.GrowerNumber = @GrowerNumber
                    AND c.Status IN ('Printed', 'Voided', 'Stopped')
                    ORDER BY c.ChequeDate DESC";
                
                var cheques = await connection.QueryAsync<Cheque>(sql, new { GrowerNumber = growerNumber });
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheques for grower number {growerNumber}: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        public async Task<List<Cheque>> GetChequesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        c.ChequeId,
                        c.ChequeSeriesId,
                        c.ChequeNumber,
                        c.FiscalYear,
                        c.GrowerId,
                        c.PaymentBatchId,
                        c.ChequeDate,
                        c.ChequeAmount,
                        c.CurrencyCode,
                        c.ExchangeRate,
                        c.PayeeName,
                        c.Memo,
                        c.Status,
                        c.ClearedDate,
                        c.VoidedDate,
                        c.VoidedReason,
                        c.CreatedAt,
                        c.CreatedBy,
                        c.ModifiedAt,
                        c.ModifiedBy,
                        c.DeletedAt,
                        c.DeletedBy,
                        c.PrintedAt,
                        c.PrintedBy,
                        c.VoidedBy,
                        c.IssuedAt,
                        c.IssuedBy,
                        g.FullName AS GrowerName,
                        g.GrowerNumber,
                        cs.SeriesCode
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    LEFT JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                    WHERE c.DeletedAt IS NULL 
                    AND c.ChequeDate >= @StartDate
                    AND c.ChequeDate <= @EndDate
                    AND c.Status IN ('Printed', 'Voided', 'Stopped')
                    ORDER BY c.ChequeDate DESC";
                
                var cheques = await connection.QueryAsync<Cheque>(sql, new 
                { 
                    StartDate = startDate.Date,
                    EndDate = endDate.Date.AddDays(1).AddTicks(-1) // End of day
                });
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheques by date range {startDate:d} to {endDate:d}: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        public async Task<bool> SaveChequeAsync(Cheque cheque)
        {
            // TODO: Rewrite to use modern Cheque properties and Cheques table
            // Use ChequeSeriesId, GrowerId, ChequeDate, ChequeAmount, Status, etc.
            Logger.Warn("ChequeService.SaveChequeAsync() not yet implemented for modern schema");
            await Task.CompletedTask;
            return false;
        }

        public async Task<bool> VoidChequeAsync(string series, decimal chequeNumber)
        {
            // TODO: Rewrite to set Status = 'Voided', VoidedDate = NOW, VoidedBy = user
            // Instead of setting VOID = 1
            Logger.Warn($"ChequeService.VoidChequeAsync({series}, {chequeNumber}) not yet implemented for modern schema");
            await Task.CompletedTask;
            return false;
        }

        public async Task<decimal> GetNextChequeNumberAsync(string series, bool isEft = false)
        {
            // TODO: Rewrite to query ChequeSeries table for next number
            // Should increment ChequeSeries.LastChequeNumber
            Logger.Warn($"ChequeService.GetNextChequeNumberAsync({series}, EFT:{isEft}) not yet implemented for modern schema");
            Logger.Warn("Consider using ChequeGenerationService.GetNextChequeNumberAsync() instead");
            await Task.CompletedTask;
            return 1000; // Stub return value
        }

        public async Task<bool> CreateChequesAsync(List<Cheque> chequesToCreate)
        {
            // TODO: Rewrite to bulk insert into modern Cheques table
            // Use modern column names and properties
            Logger.Warn($"ChequeService.CreateChequesAsync() called with {chequesToCreate?.Count ?? 0} cheques - not yet implemented");
            Logger.Warn("Consider using ChequeGenerationService.GenerateChequesForBatchAsync() instead");
            await Task.CompletedTask;
            return false;
        }

        public async Task<List<Cheque>> GetTemporaryChequesAsync(string currency, string tempChequeSeries, decimal tempChequeNumberStart)
        {
            // TODO: Rewrite to query modern Cheques table
            Logger.Warn($"ChequeService.GetTemporaryChequesAsync({currency}, {tempChequeSeries}, {tempChequeNumberStart}) not yet implemented");
            await Task.CompletedTask;
            return new List<Cheque>();
        }


        // New methods for enhanced cheque processing
        public async Task<List<Cheque>> GetChequesByStatusAsync(string status)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        c.ChequeId,
                        c.ChequeSeriesId,
                        c.ChequeNumber,
                        c.FiscalYear,
                        c.GrowerId,
                        c.PaymentBatchId,
                        c.ChequeDate,
                        c.ChequeAmount,
                        c.CurrencyCode,
                        c.ExchangeRate,
                        c.PayeeName,
                        c.Memo,
                        c.Status,
                        c.ClearedDate,
                        c.VoidedDate,
                        c.VoidedReason,
                        c.CreatedAt,
                        c.CreatedBy,
                        c.ModifiedAt,
                        c.ModifiedBy,
                        c.DeletedAt,
                        c.DeletedBy,
                        c.PrintedAt,
                        c.PrintedBy,
                        c.VoidedBy,
                        c.IssuedAt,
                        c.IssuedBy,
                        g.FullName AS GrowerName,
                        g.GrowerNumber,
                        pb.BatchNumber
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    LEFT JOIN PaymentBatches pb ON c.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.Status = @Status 
                    ORDER BY c.ChequeDate DESC";
                var cheques = await connection.QueryAsync<Cheque>(sql, new { Status = status });
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheques by status {status}: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        /// <summary>
        /// Get advance deductions for a specific cheque by cheque number
        /// </summary>
        public async Task<List<AdvanceDeduction>> GetAdvanceDeductionsByChequeNumberAsync(string chequeNumber)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        ad.DeductionId,
                        ad.AdvanceChequeId,
                        ad.ChequeId,
                        ad.PaymentBatchId,
                        ad.DeductionAmount,
                        ad.DeductionDate,
                        ad.CreatedBy,
                        ad.CreatedAt,
                        ac.AdvanceAmount,
                        ac.AdvanceDate,
                        ac.Reason,
                        pb.BatchNumber
                    FROM AdvanceDeductions ad
                    INNER JOIN AdvanceCheques ac ON ad.AdvanceChequeId = ac.AdvanceChequeId
                    INNER JOIN Cheques c ON ad.ChequeId = c.ChequeId
                    LEFT JOIN PaymentBatches pb ON ad.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.ChequeNumber = @ChequeNumber
                    ORDER BY ad.DeductionDate DESC";
                
                var deductions = await connection.QueryAsync<AdvanceDeduction>(sql, new { ChequeNumber = chequeNumber });
                return deductions.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting advance deductions for cheque {chequeNumber}: {ex.Message}", ex);
                return new List<AdvanceDeduction>();
            }
        }

        public async Task<bool> UpdateChequeStatusAsync(int chequeId, string newStatus, string updatedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "UPDATE Cheques SET Status = @Status, ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy WHERE ChequeId = @ChequeId";
                var result = await connection.ExecuteAsync(sql, new 
                { 
                    Status = newStatus, 
                    ModifiedAt = DateTime.Now, 
                    ModifiedBy = updatedBy, 
                    ChequeId = chequeId 
                });
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating cheque status: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> MarkChequesAsPrintedAsync(List<int> chequeIds, string printedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "UPDATE Cheques SET Status = 'Printed', PrintedAt = @PrintedAt, PrintedBy = @PrintedBy WHERE ChequeId IN @ChequeIds";
                var result = await connection.ExecuteAsync(sql, new 
                { 
                    PrintedAt = DateTime.Now, 
                    PrintedBy = printedBy, 
                    ChequeIds = chequeIds 
                });
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheques as printed: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> MarkChequesAsIssuedAsync(List<int> chequeIds, string issuedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "UPDATE Cheques SET Status = 'Issued', IssuedAt = @IssuedAt, IssuedBy = @IssuedBy WHERE ChequeId IN @ChequeIds";
                var result = await connection.ExecuteAsync(sql, new 
                { 
                    IssuedAt = DateTime.Now, 
                    IssuedBy = issuedBy, 
                    ChequeIds = chequeIds 
                });
                return result > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheques as issued: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<byte[]> GenerateChequePdfAsync(int chequeId)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "SELECT * FROM Cheques WHERE ChequeId = @ChequeId";
                var cheque = await connection.QueryFirstOrDefaultAsync<Cheque>(sql, new { ChequeId = chequeId });
                
                if (cheque == null)
                {
                    Logger.Warn($"Cheque {chequeId} not found for PDF generation");
                    return Array.Empty<byte>();
                }

                var pdfGenerator = new WPFGrowerApp.Services.ChequePdfGenerator();
                return await pdfGenerator.GenerateSingleChequePdfAsync(cheque);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating PDF for cheque {chequeId}: {ex.Message}", ex);
                return Array.Empty<byte>();
            }
        }

        public async Task<byte[]> GenerateBatchChequePdfAsync(List<int> chequeIds)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = "SELECT * FROM Cheques WHERE ChequeId IN @ChequeIds ORDER BY ChequeDate";
                var cheques = await connection.QueryAsync<Cheque>(sql, new { ChequeIds = chequeIds });
                
                if (!cheques.Any())
                {
                    Logger.Warn($"No cheques found for batch PDF generation");
                    return Array.Empty<byte>();
                }

                var pdfGenerator = new WPFGrowerApp.Services.ChequePdfGenerator();
                return await pdfGenerator.GenerateBatchChequePdfAsync(cheques.ToList());
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating batch PDF: {ex.Message}", ex);
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Voids multiple cheques with a reason for audit purposes.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to void.</param>
        /// <param name="reason">Reason for voiding the cheques.</param>
        /// <param name="voidedBy">User who voided the cheques.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task VoidChequesAsync(List<int> chequeIds, string reason, string voidedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Voided', 
                        VoidedDate = @VoidedDate, 
                        VoidedBy = @VoidedBy,
                        VoidedReason = @Reason
                    WHERE ChequeId IN @ChequeIds AND Status IN ('Generated', 'Issued')";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    VoidedDate = DateTime.UtcNow,
                    VoidedBy = voidedBy,
                    Reason = reason
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("No cheques were voided. Cheques may already be processed or not found.");
                }

                Logger.Info($"Successfully voided {rowsAffected} cheques. Reason: {reason}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error voiding cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Stops payment on multiple cheques with a reason for audit purposes.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to stop payment on.</param>
        /// <param name="reason">Reason for stopping payment.</param>
        /// <param name="stoppedBy">User who stopped the payment.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task StopPaymentAsync(List<int> chequeIds, string reason, string stoppedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Stopped', 
                        StoppedAt = @StoppedAt, 
                        StoppedBy = @StoppedBy,
                        StopReason = @Reason
                    WHERE ChequeId IN @ChequeIds AND Status IN ('Generated', 'Issued')";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    StoppedAt = DateTime.UtcNow,
                    StoppedBy = stoppedBy,
                    Reason = reason
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                if (rowsAffected == 0)
                {
                    throw new InvalidOperationException("No cheques were stopped. Cheques may already be processed or not found.");
                }

                Logger.Info($"Successfully stopped payment on {rowsAffected} cheques. Reason: {reason}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error stopping payment on cheques: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Logs reprint activity for audit purposes.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs that were reprinted.</param>
        /// <param name="reason">Reason for reprinting.</param>
        /// <param name="reprintedBy">User who reprinted the cheques.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task LogReprintActivityAsync(List<int> chequeIds, string reason, string reprintedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ChequeAuditLog (ChequeId, Action, ActionBy, ActionDate, Reason, Details)
                    VALUES (@ChequeId, 'Reprint', @ReprintedBy, @ReprintedAt, @Reason, @Details)";

                var auditLogs = chequeIds.Select(chequeId => new
                {
                    ChequeId = chequeId,
                    ReprintedBy = reprintedBy,
                    ReprintedAt = DateTime.UtcNow,
                    Reason = reason,
                    Details = $"Cheque reprinted. Reason: {reason}"
                }).ToList();

                await connection.ExecuteAsync(sql, auditLogs);
                
                Logger.Info($"Successfully logged reprint activity for {chequeIds.Count} cheques. Reason: {reason}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error logging reprint activity: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Marks cheques as delivered with delivery method tracking.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to mark as delivered.</param>
        /// <param name="deliveryMethod">Method of delivery (Mail, Pickup, Courier).</param>
        /// <param name="deliveredBy">User who recorded the delivery.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task<bool> MarkChequesAsDeliveredAsync(List<int> chequeIds, string deliveryMethod, string deliveredBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Delivered', 
                        DeliveredAt = @DeliveredAt, 
                        DeliveredBy = @DeliveredBy,
                        DeliveryMethod = @DeliveryMethod
                    WHERE ChequeId IN @ChequeIds AND Status = 'Printed'";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    DeliveredAt = DateTime.UtcNow,
                    DeliveredBy = deliveredBy,
                    DeliveryMethod = deliveryMethod
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                Logger.Info($"Successfully marked {rowsAffected} cheques as delivered via {deliveryMethod}");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking cheques as delivered: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Approves cheques for delivery (updates status to "Delivered").
        /// This is the final step in the workflow after review.
        /// </summary>
        /// <param name="chequeIds">List of cheque IDs to approve for delivery.</param>
        /// <param name="approvedBy">User who approved the cheques.</param>
        /// <returns>Task representing the async operation.</returns>
        public async Task<bool> ApproveChequesForDeliveryAsync(List<int> chequeIds, string approvedBy)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE Cheques 
                    SET Status = 'Delivered', 
                        DeliveredAt = @DeliveredAt, 
                        DeliveredBy = @DeliveredBy,
                        DeliveryMethod = 'Approved for Delivery'
                    WHERE ChequeId IN @ChequeIds AND Status = 'Printed'";

                var parameters = new
                {
                    ChequeIds = chequeIds,
                    DeliveredAt = DateTime.UtcNow,
                    DeliveredBy = approvedBy
                };

                var rowsAffected = await connection.ExecuteAsync(sql, parameters);
                
                Logger.Info($"Successfully approved {rowsAffected} cheques for delivery");
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error approving cheques for delivery: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Searches cheques by cheque number for review and delivery operations.
        /// </summary>
        /// <param name="chequeNumber">Cheque number to search for.</param>
        /// <returns>List of matching cheques.</returns>
        public async Task<List<Cheque>> SearchChequesByNumberAsync(string chequeNumber)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        c.ChequeId,
                        c.ChequeSeriesId,
                        c.ChequeNumber,
                        c.FiscalYear,
                        c.GrowerId,
                        c.PaymentBatchId,
                        c.ChequeDate,
                        c.ChequeAmount,
                        c.CurrencyCode,
                        c.ExchangeRate,
                        c.PayeeName,
                        c.Memo,
                        c.Status,
                        c.ClearedDate,
                        c.VoidedDate,
                        c.VoidedReason,
                        c.CreatedAt,
                        c.CreatedBy,
                        c.ModifiedAt,
                        c.ModifiedBy,
                        c.DeletedAt,
                        c.DeletedBy,
                        c.PrintedAt,
                        c.PrintedBy,
                        c.VoidedBy,
                        c.IssuedAt,
                        c.IssuedBy,
                        g.FullName AS GrowerName,
                        g.GrowerNumber,
                        cs.SeriesCode,
                        pb.BatchNumber
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    LEFT JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                    LEFT JOIN PaymentBatches pb ON c.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.DeletedAt IS NULL 
                    AND c.ChequeNumber LIKE @ChequeNumber
                    AND c.Status IN ('Printed', 'Voided', 'Stopped')
                    ORDER BY c.ChequeDate DESC";
                var cheques = await connection.QueryAsync<Cheque>(sql, new { ChequeNumber = $"%{chequeNumber}%" });
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error searching cheques by number: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }


        /// <summary>
        /// Get all cheques including both regular cheques and advance cheques in a unified view
        /// </summary>
        public async Task<List<Cheque>> GetAllChequesIncludingAdvancesAsync()
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    -- Regular cheques
                    SELECT 
                        c.ChequeId, c.ChequeSeriesId, c.ChequeNumber, c.FiscalYear,
                        c.GrowerId, c.PaymentBatchId, c.ChequeDate, c.ChequeAmount,
                        c.CurrencyCode, c.ExchangeRate, c.PayeeName, c.Memo,
                        c.Status, c.ClearedDate, c.VoidedDate, c.VoidedReason,
                        c.CreatedAt, c.CreatedBy, c.ModifiedAt, c.ModifiedBy,
                        c.DeletedAt, c.DeletedBy, c.PrintedAt, c.PrintedBy,
                        c.VoidedBy, c.IssuedAt, c.IssuedBy,
                        g.FullName AS GrowerName, g.GrowerNumber,
                        cs.SeriesCode, pb.BatchNumber,
                        'Regular' as ChequeType,
                        c.IsAdvanceCheque, c.AdvanceChequeId
                    FROM Cheques c
                    LEFT JOIN Growers g ON c.GrowerId = g.GrowerId
                    LEFT JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                    LEFT JOIN PaymentBatches pb ON c.PaymentBatchId = pb.PaymentBatchId
                    WHERE c.DeletedAt IS NULL 
                    AND c.Status IN ('Printed', 'Voided', 'Stopped')
                    
                    UNION ALL
                    
                    -- Advance cheques (converted to Cheque format)
                    SELECT 
                        ac.AdvanceChequeId as ChequeId,
                        NULL as ChequeSeriesId,
                        'ADV-' + CAST(ac.AdvanceChequeId as VARCHAR) as ChequeNumber,
                        YEAR(ac.AdvanceDate) as FiscalYear,
                        ac.GrowerId,
                        NULL as PaymentBatchId,
                        ac.AdvanceDate as ChequeDate,
                        ac.AdvanceAmount as ChequeAmount,
                        'CAD' as CurrencyCode,
                        1.0 as ExchangeRate,
                        g.FullName as PayeeName,
                        ac.Reason as Memo,
                        ac.Status,
                        NULL as ClearedDate,
                        NULL as VoidedDate,
                        NULL as VoidedReason,
                        ac.CreatedAt,
                        ac.CreatedBy,
                        ac.ModifiedAt,
                        ac.ModifiedBy,
                        ac.DeletedAt,
                        ac.DeletedBy,
                        ac.PrintedDate as PrintedAt,
                        ac.PrintedBy,
                        NULL as VoidedBy,
                        NULL as IssuedAt,
                        NULL as IssuedBy,
                        g.FullName AS GrowerName,
                        g.GrowerNumber,
                        'ADV' as SeriesCode,
                        NULL as BatchNumber,
                        'Advance' as ChequeType,
                        1 as IsAdvanceCheque,
                        ac.AdvanceChequeId as AdvanceChequeId
                    FROM AdvanceCheques ac
                    LEFT JOIN Growers g ON ac.GrowerId = g.GrowerId
                    WHERE ac.DeletedAt IS NULL 
                    AND ac.Status IN ('Printed', 'Voided', 'Stopped')
                    
                    ORDER BY ChequeDate DESC";
                
                var cheques = await connection.QueryAsync<Cheque>(sql);
                return cheques.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting all cheques including advances: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        /// <summary>
        /// Get cheques by type (Regular, Advance, or All)
        /// </summary>
        public async Task<List<Cheque>> GetChequesByTypeAsync(string chequeType)
        {
            try
            {
                var allCheques = await GetAllChequesIncludingAdvancesAsync();
                
                if (chequeType == "All")
                    return allCheques;
                
                if (chequeType == "Regular")
                    return allCheques.Where(c => !c.IsAdvanceCheque).ToList();
                
                if (chequeType == "Advance")
                    return allCheques.Where(c => c.IsAdvanceCheque).ToList();
                
                return allCheques;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting cheques by type {chequeType}: {ex.Message}", ex);
                return new List<Cheque>();
            }
        }

        /// <summary>
        /// Get detailed receipt information for a specific cheque - only receipts for the specific grower
        /// </summary>
        public async Task<List<ReceiptDetailDto>> GetReceiptDetailsForChequeAsync(string chequeNumber)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                var sql = @"
                    SELECT 
                        r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                        r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                        r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                        r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                        r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                        r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                        g.FullName as GrowerName, g.GrowerNumber, g.Address as GrowerAddress, 
                        g.PhoneNumber as GrowerPhone, g.Email as GrowerEmail,
                        p.Description as ProductName, p.Description as ProductDescription,
                        pr.ProcessName, pr.Description as ProcessDescription,
                        v.VarietyName,
                        d.DepotName, d.Address as DepotAddress,
                        pc.ClassName as PriceClassName,
                        rpa.PricePerPound, rpa.AmountPaid as TotalAmountPaid,
                        rpa.PaymentTypeId,
                        pt.TypeName as PaymentTypeName
                    FROM Receipts r
                    INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                    INNER JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                    INNER JOIN Cheques c ON pb.PaymentBatchId = c.PaymentBatchId
                    LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                    LEFT JOIN Products p ON r.ProductId = p.ProductId
                    LEFT JOIN Processes pr ON r.ProcessId = pr.ProcessId
                    LEFT JOIN Varieties v ON r.VarietyId = v.VarietyId
                    LEFT JOIN Depots d ON r.DepotId = d.DepotId
                    LEFT JOIN PriceClasses pc ON r.PriceClassId = pc.PriceClassId
                    LEFT JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                    WHERE c.ChequeNumber = @ChequeNumber
                      AND r.GrowerId = c.GrowerId  -- CRITICAL: Only show receipts for the specific grower of this cheque
                      AND r.DeletedAt IS NULL
                    ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC";

                var result = await connection.QueryAsync<ReceiptDetailDto>(sql, new { ChequeNumber = chequeNumber });
                return result.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipt details for cheque {chequeNumber}: {ex.Message}", ex);
                return new List<ReceiptDetailDto>();
            }
        }

        // ==================================================================================
        // DATABASE CONNECTION (from BaseDatabaseService)
        // ==================================================================================
        public string ConnectionString => _connectionString;
    }
}
