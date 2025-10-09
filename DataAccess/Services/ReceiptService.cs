using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using System.Linq;
using WPFGrowerApp.Infrastructure.Logging; // Added for Logger
using WPFGrowerApp.DataAccess.Exceptions; // Added for MissingReferenceDataException

namespace WPFGrowerApp.DataAccess.Services
{
        public class ReceiptService : BaseDatabaseService, IReceiptService
        {
            public async Task<decimal> GetPriceForReceiptAsync(Receipt receipt)
            {
                // TODO: Implement price calculation logic
                await Task.CompletedTask;
                return 0m;
            }

            public async Task<decimal> ApplyDockageAsync(Receipt receipt)
            {
                // TODO: Implement dockage application logic
                await Task.CompletedTask;
                return 0m;
            }


        public async Task<List<Receipt>> GetReceiptsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Query modern Receipts table with grower information
                    var sql = @"
                        SELECT 
                            r.ReceiptId, 
                            r.ReceiptNumber, 
                            r.ReceiptDate, 
                            r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId,
                            r.DepotId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId,
                            r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId,
                            r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy,
                            r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        WHERE r.DeletedAt IS NULL";

                    var parameters = new DynamicParameters();
                    
                    if (startDate.HasValue)
                    {
                        sql += " AND ReceiptDate >= @StartDate";
                        parameters.Add("@StartDate", startDate.Value);
                    }

                    if (endDate.HasValue)
                    {
                        sql += " AND ReceiptDate <= @EndDate";
                        parameters.Add("@EndDate", endDate.Value);
                    }
                    
                    sql += " ORDER BY ReceiptDate DESC, ReceiptNumber DESC";

                    var receipts = (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                    
                    // Map modern properties
                    foreach (var receipt in receipts)
                    {
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoided;
                        // Removed legacy Date property; use ReceiptDate only
                        // Grade is already mapped
                    }
                    
                    return receipts;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptsAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Receipt> GetReceiptByNumberAsync(decimal receiptNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Query modern Receipts table with grower information
                    var sql = @"
                        SELECT 
                            r.ReceiptId, 
                            r.ReceiptNumber, 
                            r.ReceiptDate, 
                            r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId,
                            r.DepotId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId,
                            r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId,
                            r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy,
                            r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        WHERE r.ReceiptNumber = @ReceiptNumber
                          AND r.DeletedAt IS NULL";
                    var parameters = new { ReceiptNumber = receiptNumber.ToString() };
                    var receipt = await connection.QueryFirstOrDefaultAsync<Receipt>(sql, parameters);
                    if (receipt != null)
                    {
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoided;
                        receipt.ReceiptDate = receipt.ReceiptDate;
                        // Grade is already mapped
                    }
                    
                    return receipt;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptByNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<Receipt> SaveReceiptAsync(Receipt receipt)
        {
            try
            {
                if (receipt == null) throw new ArgumentNullException(nameof(receipt));

                // Generate receipt number if not set
                if (string.IsNullOrEmpty(receipt.ReceiptNumber))
                {
                    var nextNumber = await GetNextReceiptNumberAsync();
                    receipt.ReceiptNumber = nextNumber.ToString();
                }

                // Calculate computed fields
                receipt.NetWeight = receipt.GrossWeight - receipt.TareWeight;
                receipt.DockWeight = receipt.NetWeight * (receipt.DockPercentage / 100);
                receipt.FinalWeight = receipt.NetWeight - receipt.DockWeight;

                // Check if this is a container-only receipt (no product movement)
                bool isContainerOnly = receipt.ProductId <= 0 && receipt.NetWeight == 0;

                // Validate required fields
                if (receipt.GrowerId <= 0) throw new ArgumentException("GrowerId is required.");
                if (receipt.DepotId <= 0) throw new ArgumentException("DepotId is required.");
                
                // For non-container-only receipts, ProductId and ProcessId are required
                if (!isContainerOnly)
                {
                    if (receipt.ProductId <= 0) throw new ArgumentException("ProductId is required for receipts with product movement.");
                    if (receipt.ProcessId <= 0) throw new ArgumentException("ProcessId is required for receipts with product movement.");
                }
                
                // Validate container data - EVERY receipt must have at least one container
                // ContainerData is parsed from CSV (CONT1-12, IN1-12, OUT1-12 columns)
                var missingItems = new List<string>();
                
                if (receipt.ContainerData == null || !receipt.ContainerData.Any())
                {
                    missingItems.Add("ContainerData (no containers specified)");
                }
                
                // PriceClassId will be populated from Grower.DefaultPriceClassId
                // PriceAreaId is not needed for receipts (only for Advance Payment Run)
                
                if (missingItems.Any())
                {
                    var receiptId = receipt.ReceiptNumber ?? "NULL";
                    throw new MissingReferenceDataException(receiptId, missingItems);
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get current user from App.CurrentUser
                    string currentUser = App.CurrentUser?.Username ?? "SYSTEM";

                    // Get grower's DefaultPriceClassId (if not already set from import)
                    int priceClassId = receipt.PriceClassId; // Use value from Receipt if already set
                    if (priceClassId <= 0 && !isContainerOnly && receipt.GrowerId > 0)
                    {
                        var growerSql = "SELECT DefaultPriceClassId FROM Growers WHERE GrowerId = @GrowerId";
                        var growerPriceClass = await connection.QueryFirstOrDefaultAsync<int?>(growerSql, new { receipt.GrowerId });
                        priceClassId = growerPriceClass ?? 1;
                        Logger.Info($"Receipt #{receipt.ReceiptNumber} - Using PriceClassId={priceClassId} from Grower #{receipt.GrowerId}");
                    }
                    else if (priceClassId > 0)
                    {
                        Logger.Info($"Receipt #{receipt.ReceiptNumber} - Using PriceClassId={priceClassId} from Receipt (already set)");
                    }
                    else
                    {
                        priceClassId = 1; // Default fallback for container-only receipts
                    }

                    // INSERT into modern Receipts table (ContainerId removed - now in ContainerTransactions)
                    // Note: NetWeight, DockWeight, FinalWeight are computed columns and should NOT be in INSERT
                    // Note: PriceAreaId removed - only needed for Advance Payment Run, not for receipts
                    var sql = @"
                        INSERT INTO Receipts (
                            ReceiptNumber, ReceiptDate, ReceiptTime,
                            GrowerId, ProductId, ProcessId, ProcessTypeId,
                            DepotId, VarietyId,
                            GrossWeight, TareWeight, DockPercentage,
                            Grade, PriceClassId,
                            IsVoided, VoidedReason,
                            ImportBatchId,
                            CreatedAt, CreatedBy
                        ) VALUES (
                            @ReceiptNumber, @ReceiptDate, @ReceiptTime,
                            @GrowerId, @ProductId, @ProcessId, @ProcessTypeId,
                            @DepotId, @VarietyId,
                            @GrossWeight, @TareWeight, @DockPercentage,
                            @Grade, @PriceClassId,
                            @IsVoided, @VoidedReason,
                            @ImportBatchId,
                            GETDATE(), @CreatedBy
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var parameters = new
                    {
                        ReceiptNumber = receipt.ReceiptNumber,
                        ReceiptDate = receipt.ReceiptDate,
                        ReceiptTime = receipt.ReceiptTime,
                        GrowerId = receipt.GrowerId,
                        ProductId = receipt.ProductId,
                        ProcessId = receipt.ProcessId,
                        ProcessTypeId = receipt.ProcessTypeId,
                        DepotId = receipt.DepotId,
                        // ContainerId removed - stored in ContainerTransactions instead
                        VarietyId = receipt.VarietyId,
                        GrossWeight = receipt.GrossWeight,
                        TareWeight = receipt.TareWeight,
                        DockPercentage = receipt.DockPercentage,
                        // NetWeight, DockWeight, FinalWeight are computed - do not include
                        Grade = receipt.Grade,
                        PriceClassId = priceClassId, // From Grower.DefaultPriceClassId
                        // PriceAreaId removed - only for Advance Payment Run
                        IsVoided = receipt.IsVoided,
                        VoidedReason = receipt.VoidedReason,
                        ImportBatchId = receipt.ImportBatchId,
                        CreatedBy = currentUser
                    };

                    var receiptId = await connection.QuerySingleAsync<int>(sql, parameters);
                    receipt.ReceiptId = receiptId;
                    receipt.CreatedAt = DateTime.Now;
                    receipt.CreatedBy = currentUser;

                    Logger.Info($"Receipt {receipt.ReceiptNumber} saved successfully with ID {receiptId}");
                    
                    // Save container transactions
                    if (receipt.ContainerData != null && receipt.ContainerData.Any())
                    {
                        await SaveContainerTransactionsAsync(connection, receiptId, receipt.ContainerData, currentUser);
                    }
                    
                    return receipt;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveReceiptAsync: {ex.Message}");
                // Enhanced logging with receipt details for troubleshooting
                var containerCount = receipt.ContainerData?.Count ?? 0;
                var receiptInfo = $"Receipt#{receipt.ReceiptNumber ?? "NULL"}, Date={receipt.ReceiptDate:yyyy-MM-dd}, " +
                                 $"GrowerId={receipt.GrowerId}, ProductId={receipt.ProductId}, ProcessId={receipt.ProcessId}, " +
                                 $"Grade={receipt.Grade}, Containers={containerCount}";
                Logger.Error($"Error saving receipt [{receiptInfo}]: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteReceiptAsync(string receiptNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get current user
                    string currentUser = "SYSTEM"; // TODO: Get from current session/user context
                    
                    // Soft delete in modern Receipts table
                    var sql = @"
                        UPDATE Receipts
                        SET DeletedAt = GETDATE(),
                            DeletedBy = @DeletedBy,
                            IsVoided = 1
                        WHERE ReceiptNumber = @ReceiptNumber
                          AND DeletedAt IS NULL";
                          
                    var parameters = new { ReceiptNumber = receiptNumber, DeletedBy = currentUser };
                    var result = await connection.ExecuteAsync(sql, parameters);
                    
                    if (result > 0)
                    {
                        Logger.Info($"Receipt {receiptNumber} soft deleted by {currentUser}");
                    }
                    
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteReceiptAsync: {ex.Message}");
                Logger.Error($"Error deleting receipt {receiptNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> VoidReceiptAsync(string receiptNumber, string reason)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get current user
                    string currentUser = "SYSTEM"; // TODO: Get from current session/user context
                    
                    // Void receipt in modern Receipts table
                    var sql = @"
                        UPDATE Receipts
                        SET IsVoided = 1,
                            VoidedReason = @VoidedReason,
                            VoidedAt = GETDATE(),
                            VoidedBy = @VoidedBy,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ReceiptNumber = @ReceiptNumber
                          AND DeletedAt IS NULL";

                    var parameters = new { 
                        ReceiptNumber = receiptNumber, 
                        VoidedReason = reason,
                                        // Removed legacy Date property; use ReceiptDate only
                        ModifiedBy = currentUser
                    };
                    var result = await connection.ExecuteAsync(sql, parameters);
                    
                    if (result > 0)
                    {
                        Logger.Info($"Receipt {receiptNumber} voided by {currentUser}. Reason: {reason}");
                    }
                    
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in VoidReceiptAsync: {ex.Message}");
                Logger.Error($"Error voiding receipt {receiptNumber}: {ex.Message}", ex);
                throw;
            }
        }

    public async Task<List<Receipt>> GetReceiptsByGrowerAsync(string growerNumber, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Query modern Receipts table with Growers join
                    var sql = @"
                        SELECT 
                            r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId,
                            r.DepotId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId,
                            r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId,
                            r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy,
                            r.DeletedAt, r.DeletedBy
                        FROM Receipts r
                        INNER JOIN Growers g ON r.GrowerId = g.GrowerId
                        WHERE g.GrowerNumber = @GrowerNumber
                          AND r.DeletedAt IS NULL";

                    var parameters = new DynamicParameters();
                    parameters.Add("@GrowerNumber", growerNumber);

                    if (startDate.HasValue)
                    {
                        sql += " AND r.ReceiptDate >= @StartDate";
                        parameters.Add("@StartDate", startDate.Value);
                    }

                    if (endDate.HasValue)
                    {
                        sql += " AND r.ReceiptDate <= @EndDate";
                        parameters.Add("@EndDate", endDate.Value);
                    }
                    
                    sql += " ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC";

                    var receipts = (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                    
                    // Map modern properties to legacy properties for backward compatibility
                    foreach (var receipt in receipts)
                    {
                        receipt.GrowerNumber = growerNumber;
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoided;
                        receipt.ReceiptDate = receipt.ReceiptDate;
                    }
                    
                    return receipts;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptsByGrowerAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Receipt>> GetReceiptsByImportBatchAsync(decimal impBatch)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Query modern Receipts table by ImportBatchId
                    var sql = @"
                        SELECT 
                            r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId,
                            r.DepotId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId,
                            r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId,
                            r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy,
                            r.DeletedAt, r.DeletedBy
                        FROM Receipts r
                        WHERE r.ImportBatchId = (SELECT ImportBatchId FROM ImportBatches WHERE BatchNumber = @ImpBatch)
                          AND r.DeletedAt IS NULL
                        ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC";
                    var parameters = new { ImpBatch = impBatch.ToString() };
                    var receipts = (await connection.QueryAsync<Receipt>(sql, parameters)).ToList();
                    
                    // Map modern properties to legacy properties for backward compatibility
                    foreach (var receipt in receipts)
                    {
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoided;
                        receipt.ReceiptDate = receipt.ReceiptDate;
                    }
                    
                    return receipts;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptsByImportBatchAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<decimal> GetNextReceiptNumberAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT MAX(RECPT) + 1 FROM Daily";
                    var result = await connection.ExecuteScalarAsync<decimal?>(sql);
                    return result ?? 1;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetNextReceiptNumberAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateReceiptAsync(Receipt receipt)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Check if receipt number exists
                    var existingReceipt = await GetReceiptByNumberAsync(decimal.TryParse(receipt.ReceiptNumber, out var num) ? num : 0);
                    if (existingReceipt != null)
                    {
                        return false;
                    }

                    // Validate grower exists
                    var growerCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Growers WHERE NUMBER = @GrowerNumber",
                        new { receipt.GrowerNumber });
                    if (growerCount == 0)
                    {
                        return false;
                    }

                    // Validate product exists
                    var productCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Products WHERE ProductCode = @Product",
                        new { receipt.Product });
                    if (productCount == 0)
                    {
                        return false;
                    }

                    // Validate process exists
                    var processCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Processes WHERE ProcessCode = @Process",
                        new { receipt.Process });
                    if (processCount == 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ValidateReceiptAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<decimal> CalculateNetWeightAsync(Receipt receipt)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT SUM(TARE) as TotalTare
                        FROM Contain
                        WHERE CONTAINER IN (
                            SELECT DISTINCT CONTAINER
                            FROM Daily
                            WHERE RECPT = @ReceiptNumber
                        )";

                    var totalTare = await connection.ExecuteScalarAsync<decimal?>(sql, new { receipt.ReceiptNumber }) ?? 0;
                    return receipt.Gross - totalTare;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CalculateNetWeightAsync: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Calculates the actual dockage amount (weight lost due to quality deduction).
        /// Formula: dockage_amount = ori_net - net (if ori_net exists, else 0)
        /// </summary>
        public decimal CalculateDockageAmount(Receipt receipt)
        {
            // If no original net weight stored, there's no dockage
            if (!receipt.OriNet.HasValue || receipt.OriNet.Value == 0)
            {
                return 0;
            }

            // Calculate the difference between original and adjusted net
            // This matches the XBase formula: (Daily->Ori_net - Daily->net)
            decimal dockageAmount = receipt.OriNet.Value - receipt.Net;
            
            return Math.Max(0, dockageAmount); // Ensure non-negative
        }

        public async Task<bool> UpdateReceiptAdvanceDetailsAsync(
            decimal receiptNumber,
            int advanceNumber,
            decimal postBatchId,
            decimal advancePrice,
            decimal priceRecordId,
            decimal premiumPrice)
        {
            if (advanceNumber < 1 || advanceNumber > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), "Advance number must be 1, 2, or 3.");
            }

            string postBatchColumn;
            string advPriceColumn;
            string advPriceIdColumn;

            switch (advanceNumber)
            {
                case 1:
                    postBatchColumn = "POST_BAT1";
                    advPriceColumn = "ADV_PR1";
                    advPriceIdColumn = "ADV_PRID1";
                    break;
                case 2:
                    postBatchColumn = "POST_BAT2";
                    advPriceColumn = "ADV_PR2";
                    advPriceIdColumn = "ADV_PRID2";
                    break;
                case 3:
                    postBatchColumn = "POST_BAT3";
                    advPriceColumn = "ADV_PR3";
                    advPriceIdColumn = "ADV_PRID3";
                    break;
                default: return false; // Should not happen
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Update the specific advance columns and the premium price (if applicable for first advance)
                    // Also update LAST_ADVPB as seen in XBase++ code
                    var sql = $@"
                        UPDATE Daily
                        SET {postBatchColumn} = @PostBatchId,
                            {advPriceColumn} = @AdvancePrice,
                            {advPriceIdColumn} = @PriceRecordId,
                            LAST_ADVPB = @PostBatchId
                            {(advanceNumber == 1 ? ", PREM_PRICE = @PremiumPrice" : "")}
                        WHERE RECPT = @ReceiptNumber
                          AND ( {postBatchColumn} = 0 OR {postBatchColumn} IS NULL)"; // Ensure we don't overwrite existing batch info

                    var parameters = new DynamicParameters();
                    parameters.Add("@ReceiptNumber", receiptNumber);
                    parameters.Add("@PostBatchId", postBatchId);
                    parameters.Add("@AdvancePrice", advancePrice);
                    parameters.Add("@PriceRecordId", priceRecordId);
                    if (advanceNumber == 1)
                    {
                        parameters.Add("@PremiumPrice", premiumPrice);
                    }

                    int rowsAffected = await connection.ExecuteAsync(sql, parameters);
                    return rowsAffected > 0; // Return true if the record was updated
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating advance details for Receipt: {receiptNumber}, Advance: {advanceNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<List<Receipt>> GetReceiptsForAdvancePaymentAsync(
            int advanceNumber,
            DateTime cutoffDate,
            // Updated signature to accept lists
            List<int>? includeGrowerIds = null,
            List<string>? includePayGroupIds = null,
            List<int>? excludeGrowerIds = null,
            List<string>? excludePayGroupIds = null,
            List<int>? productIds = null,
            List<int>? processIds = null,
            int? cropYear = null) // Added cropYear parameter
        {
             if (advanceNumber < 1 || advanceNumber > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), "Advance number must be 1, 2, or 3.");
            }

            string postBatchCheckColumn;
            switch (advanceNumber)
            {
                case 1: postBatchCheckColumn = "POST_BAT1"; break;
                case 2: postBatchCheckColumn = "POST_BAT2"; break;
                case 3: postBatchCheckColumn = "POST_BAT3"; break;
                default: return new List<Receipt>(); // Should not happen
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Base query selects from Daily and joins Grower
                    var sqlBuilder = new SqlBuilder();
                    // Updated SELECT to use only columns present in Receipts and Growers tables
                    var selector = sqlBuilder.AddTemplate(@"
                        SELECT
                            r.ReceiptId,
                            r.ReceiptNumber,
                            r.ReceiptDate,
                            r.ReceiptTime,
                            r.GrowerId,
                            r.ProductId,
                            p.ProductCode as Product,
                            r.ProcessId,
                            pr.ProcessCode as Process,
                            r.ProcessTypeId,
                            r.VarietyId,
                            r.DepotId,
                            r.GrossWeight,
                            r.TareWeight,
                            r.NetWeight,
                            r.DockPercentage,
                            r.DockWeight,
                            r.FinalWeight,
                            r.Grade,
                            r.PriceClassId,
                            r.IsVoided,
                            r.VoidedReason,
                            r.VoidedAt,
                            r.VoidedBy,
                            r.ImportBatchId,
                            r.CreatedAt,
                            r.CreatedBy,
                            r.ModifiedAt,
                            r.ModifiedBy,
                            r.QualityCheckedAt,
                            r.QualityCheckedBy,
                            r.DeletedAt,
                            r.DeletedBy,
                            g.FullName as GrowerName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        LEFT JOIN Processes pr ON r.ProcessId = pr.ProcessId
                        LEFT JOIN ReceiptPaymentAllocations rpa 
                            ON r.ReceiptId = rpa.ReceiptId 
                            AND rpa.PaymentTypeId = @PaymentTypeId
                        /**where**/
                        ORDER BY r.GrowerId, r.ReceiptDate, r.ReceiptNumber"
                    );

                    // Add mandatory conditions
                sqlBuilder.Where("r.ReceiptDate <= @CutoffDate", new { CutoffDate = cutoffDate });
                sqlBuilder.Where("r.IsVoided = 0"); // Not voided
                sqlBuilder.Where("r.NetWeight > 0"); // for payment , net should be >0
                sqlBuilder.Where("rpa.AllocationId IS NULL"); // Exclude receipts already paid for this advance
                // Add PaymentTypeId parameter for the LEFT JOIN
                sqlBuilder.AddParameters(new { PaymentTypeId = advanceNumber });
                if (cropYear.HasValue) // Add Crop Year filter using YEAR() function
                {
                    sqlBuilder.Where("YEAR(r.ReceiptDate) = @CropYear", new { CropYear = cropYear.Value });
                }


                    // Add optional list filters using WHERE IN / NOT IN
                    if (includeGrowerIds?.Any() ?? false)
                        sqlBuilder.Where("d.NUMBER IN @IncludeGrowerIds", new { IncludeGrowerIds = includeGrowerIds });
                    if (excludeGrowerIds?.Any() ?? false)
                        sqlBuilder.Where("d.NUMBER NOT IN @ExcludeGrowerIds", new { ExcludeGrowerIds = excludeGrowerIds });
                    if (includePayGroupIds?.Any() ?? false)
                        sqlBuilder.Where("g.PAYGRP IN @IncludePayGroupIds", new { IncludePayGroupIds = includePayGroupIds });
                    if (excludePayGroupIds?.Any() ?? false)
                        sqlBuilder.Where("g.PAYGRP NOT IN @ExcludePayGroupIds", new { ExcludePayGroupIds = excludePayGroupIds });
                    if (productIds?.Any() ?? false)
                        sqlBuilder.Where("d.PRODUCT IN @ProductIds", new { ProductIds = productIds });
                    if (processIds?.Any() ?? false)
                        sqlBuilder.Where("d.PROCESS IN @ProcessIds", new { ProcessIds = processIds });

                    // Execute query
                    var results = await connection.QueryAsync<Receipt>(selector.RawSql, selector.Parameters);
                    
                    // Fix: Set Net property from NetWeight for payment calculations
                    foreach (var receipt in results)
                    {
                        receipt.Net = receipt.NetWeight;
                    }
                    
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipts for advance payment {advanceNumber}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Creates a payment allocation record linking a receipt to a payment batch.
        /// This replaces the old method of updating Daily table columns (ADV_PR1, ADV_PR2, ADV_PR3).
        /// </summary>
        public async Task CreateReceiptPaymentAllocationAsync(ReceiptPaymentAllocation allocation)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        INSERT INTO ReceiptPaymentAllocations 
                            (ReceiptId, PaymentBatchId, PaymentTypeId, PriceScheduleId, 
                             PricePerPound, QuantityPaid, AmountPaid, AllocatedAt)
                        VALUES 
                            (@ReceiptId, @PaymentBatchId, @PaymentTypeId, @PriceScheduleId,
                             @PricePerPound, @QuantityPaid, @AmountPaid, @AllocatedAt);";
                    
                    await connection.ExecuteAsync(sql, allocation);
                    Logger.Info($"Created payment allocation for Receipt {allocation.ReceiptId}, Batch {allocation.PaymentBatchId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment allocation for Receipt {allocation.ReceiptId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all payment allocations for a specific receipt.
        /// Used to check previous payments and prevent duplicate payments.
        /// </summary>
        public async Task<List<ReceiptPaymentAllocation>> GetReceiptPaymentAllocationsAsync(int receiptId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            AllocationId,
                            ReceiptId,
                            PaymentBatchId,
                            PaymentTypeId,
                            PriceScheduleId,
                            PricePerPound,
                            QuantityPaid,
                            AmountPaid,
                            AllocatedAt
                        FROM ReceiptPaymentAllocations
                        WHERE ReceiptId = @ReceiptId
                        ORDER BY PaymentTypeId;";
                    
                    var result = await connection.QueryAsync<ReceiptPaymentAllocation>(sql, new { ReceiptId = receiptId });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment allocations for Receipt {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets a payment summary for a receipt showing total paid and breakdown by advance.
        /// Useful for displaying payment history on receipt forms.
        /// </summary>
        public async Task<ReceiptPaymentSummary?> GetReceiptPaymentSummaryAsync(int receiptId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT 
                            ISNULL(SUM(AmountPaid), 0) as TotalAmountPaid,
                            ISNULL(SUM(CASE WHEN PaymentTypeId = 1 THEN AmountPaid ELSE 0 END), 0) as Advance1Amount,
                            ISNULL(SUM(CASE WHEN PaymentTypeId = 2 THEN AmountPaid ELSE 0 END), 0) as Advance2Amount,
                            ISNULL(SUM(CASE WHEN PaymentTypeId = 3 THEN AmountPaid ELSE 0 END), 0) as Advance3Amount,
                            CAST(MAX(CASE WHEN PaymentTypeId = 1 THEN 1 ELSE 0 END) AS BIT) as HasAdvance1,
                            CAST(MAX(CASE WHEN PaymentTypeId = 2 THEN 1 ELSE 0 END) AS BIT) as HasAdvance2,
                            CAST(MAX(CASE WHEN PaymentTypeId = 3 THEN 1 ELSE 0 END) AS BIT) as HasAdvance3,
                            MAX(AllocatedAt) as LastPaymentDate
                        FROM ReceiptPaymentAllocations
                        WHERE ReceiptId = @ReceiptId;";
                    
                    return await connection.QueryFirstOrDefaultAsync<ReceiptPaymentSummary>(sql, new { ReceiptId = receiptId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment summary for Receipt {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        #region Container Transaction Methods

        /// <summary>
        /// Saves container transactions for a receipt.
        /// Converts ContainerData (from CSV parsing) into ContainerTransactions table records.
        /// </summary>
        private async Task SaveContainerTransactionsAsync(
            SqlConnection connection, 
            int receiptId, 
            List<ContainerInfo> containerData, 
            string createdBy)
        {
            try
            {
                foreach (var container in containerData)
                {
                    // Lookup ContainerId by container type code
                    var containerId = await GetContainerIdByCodeAsync(connection, container.Type);
                    
                    if (!containerId.HasValue)
                    {
                        Logger.Warn($"Container type '{container.Type}' not found in Containers table. Skipping.");
                        continue;
                    }

                    // Insert IN transaction
                    if (container.InCount > 0)
                    {
                        await InsertContainerTransactionAsync(
                            connection, 
                            receiptId, 
                            containerId.Value, 
                            "IN", 
                            container.InCount, 
                            createdBy);
                    }

                    // Insert OUT transaction
                    if (container.OutCount > 0)
                    {
                        await InsertContainerTransactionAsync(
                            connection, 
                            receiptId, 
                            containerId.Value, 
                            "OUT", 
                            container.OutCount, 
                            createdBy);
                    }
                }

                Logger.Info($"Saved {containerData.Count} container transaction(s) for Receipt {receiptId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error saving container transactions for Receipt {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Inserts a single container transaction record.
        /// </summary>
        private async Task InsertContainerTransactionAsync(
            SqlConnection connection,
            int receiptId,
            int containerId,
            string direction,
            int quantity,
            string createdBy)
        {
            const string sql = @"
                INSERT INTO ContainerTransactions (
                    ReceiptId, ContainerId, Direction, Quantity, CreatedAt, CreatedBy
                ) VALUES (
                    @ReceiptId, @ContainerId, @Direction, @Quantity, GETDATE(), @CreatedBy
                )";

            await connection.ExecuteAsync(sql, new
            {
                ReceiptId = receiptId,
                ContainerId = containerId,
                Direction = direction,
                Quantity = quantity,
                CreatedBy = createdBy
            });
        }

        /// <summary>
        /// Gets ContainerId by container code (e.g., "FP", "PINT", "FLAT").
        /// </summary>
        private async Task<int?> GetContainerIdByCodeAsync(SqlConnection connection, string? containerCode)
        {
            if (string.IsNullOrEmpty(containerCode)) return null;

            const string sql = @"
                SELECT ContainerId 
                FROM Containers 
                WHERE ContainerCode = @ContainerCode 
                  AND DeletedAt IS NULL";

            return await connection.QueryFirstOrDefaultAsync<int?>(sql, new { ContainerCode = containerCode });
        }

        /// <summary>
        /// Gets all container transactions for a specific receipt.
        /// </summary>
        public async Task<List<ContainerTransaction>> GetContainerTransactionsByReceiptAsync(int receiptId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    const string sql = @"
                        SELECT 
                            ct.ContainerTransactionId,
                            ct.ReceiptId,
                            ct.ContainerId,
                            ct.Direction,
                            ct.Quantity,
                            ct.CreatedAt,
                            ct.CreatedBy,
                            c.ContainerCode,
                            c.ContainerName,
                            c.TareWeight
                        FROM ContainerTransactions ct
                        INNER JOIN Containers c ON ct.ContainerId = c.ContainerId
                        WHERE ct.ReceiptId = @ReceiptId
                        ORDER BY ct.Direction DESC, c.ContainerName";
                    
                    var transactions = await connection.QueryAsync<ContainerTransaction>(sql, new { ReceiptId = receiptId });
                    return transactions.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting container transactions for Receipt {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Dashboard Statistics Methods

        /// <summary>
        /// Gets the total count of receipts (optimized for dashboard)
        /// </summary>
        public async Task<int> GetTotalReceiptsCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Receipts WHERE DeletedAt IS NULL";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetTotalReceiptsCountAsync: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the count of non-voided receipts (pending receipts, optimized for dashboard)
        /// </summary>
        public async Task<int> GetPendingReceiptsCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT COUNT(*) FROM Receipts WHERE IsVoided = 0 AND DeletedAt IS NULL";
                    return await connection.ExecuteScalarAsync<int>(sql);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetPendingReceiptsCountAsync: {ex.Message}", ex);
                throw;
            }
        }

        #endregion
    }
}
