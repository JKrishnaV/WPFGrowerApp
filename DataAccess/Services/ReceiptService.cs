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
                            r.ReceiptNumber as ReceiptNumberModern, 
                            r.ReceiptDate, 
                            r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId,
                            r.DepotId, r.ContainerId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade as GradeModern, r.PriceClassId, r.PriceAreaId,
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
                    
                    // Map modern properties to legacy properties for backward compatibility
                    foreach (var receipt in receipts)
                    {
                        receipt.ReceiptNumber = decimal.Parse(receipt.ReceiptNumberModern ?? "0");
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoidedModern;
                        receipt.Date = receipt.ReceiptDate;
                        receipt.Grade = receipt.GradeModern; // Map GradeModern to legacy Grade
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
                            r.ReceiptNumber as ReceiptNumberModern, 
                            r.ReceiptDate, 
                            r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId,
                            r.DepotId, r.ContainerId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade as GradeModern, r.PriceClassId, r.PriceAreaId,
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
                        // Map modern properties to legacy properties for backward compatibility
                        receipt.ReceiptNumber = receiptNumber;
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoidedModern;
                        receipt.Date = receipt.ReceiptDate;
                        receipt.Grade = receipt.GradeModern; // Map GradeModern to legacy Grade
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
                if (string.IsNullOrEmpty(receipt.ReceiptNumberModern))
                {
                    var nextNumber = await GetNextReceiptNumberAsync();
                    receipt.ReceiptNumberModern = nextNumber.ToString();
                    receipt.ReceiptNumber = nextNumber;
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
                
                // Validate ContainerId, PriceClassId, PriceAreaId - throw exception if missing
                // This allows the import process to skip receipts with missing reference data
                // and log detailed information about what's missing for user review
                var missingItems = new List<string>();
                
                if (!receipt.ContainerId.HasValue || receipt.ContainerId <= 0)
                {
                    missingItems.Add("ContainerId");
                }
                
                // For non-container-only receipts, PriceClassId and PriceAreaId are required
                if (!isContainerOnly)
                {
                    if (receipt.PriceClassId <= 0)
                    {
                        missingItems.Add("PriceClassId");
                    }
                    
                    if (receipt.PriceAreaId <= 0)
                    {
                        missingItems.Add("PriceAreaId");
                    }
                }
                
                if (missingItems.Any())
                {
                    var receiptId = receipt.ReceiptNumberModern ?? receipt.ReceiptNumber.ToString();
                    throw new MissingReferenceDataException(receiptId, missingItems);
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get current user from App.CurrentUser
                    string currentUser = App.CurrentUser?.Username ?? "SYSTEM";

                    // INSERT into modern Receipts table
                    // Note: NetWeight, DockWeight, FinalWeight are computed columns and should NOT be in INSERT
                    var sql = @"
                        INSERT INTO Receipts (
                            ReceiptNumber, ReceiptDate, ReceiptTime,
                            GrowerId, ProductId, ProcessId, ProcessTypeId,
                            DepotId, ContainerId, VarietyId,
                            GrossWeight, TareWeight, DockPercentage,
                            Grade, PriceClassId, PriceAreaId,
                            IsVoided, VoidedReason,
                            ImportBatchId,
                            CreatedAt, CreatedBy
                        ) VALUES (
                            @ReceiptNumber, @ReceiptDate, @ReceiptTime,
                            @GrowerId, @ProductId, @ProcessId, @ProcessTypeId,
                            @DepotId, @ContainerId, @VarietyId,
                            @GrossWeight, @TareWeight, @DockPercentage,
                            @Grade, @PriceClassId, @PriceAreaId,
                            @IsVoided, @VoidedReason,
                            @ImportBatchId,
                            GETDATE(), @CreatedBy
                        );
                        SELECT CAST(SCOPE_IDENTITY() as int);";

                    var parameters = new
                    {
                        ReceiptNumber = receipt.ReceiptNumberModern,
                        ReceiptDate = receipt.ReceiptDate,
                        ReceiptTime = receipt.ReceiptTime,
                        GrowerId = receipt.GrowerId,
                        ProductId = receipt.ProductId,
                        ProcessId = receipt.ProcessId,
                        ProcessTypeId = receipt.ProcessTypeId,
                        DepotId = receipt.DepotId,
                        ContainerId = receipt.ContainerId,
                        VarietyId = receipt.VarietyId,
                        GrossWeight = receipt.GrossWeight,
                        TareWeight = receipt.TareWeight,
                        DockPercentage = receipt.DockPercentage,
                        // NetWeight, DockWeight, FinalWeight are computed - do not include
                        Grade = receipt.GradeModern,
                        PriceClassId = receipt.PriceClassId,
                        PriceAreaId = receipt.PriceAreaId,
                        IsVoided = receipt.IsVoidedModern,
                        VoidedReason = receipt.VoidedReason,
                        ImportBatchId = receipt.ImportBatchId,
                        CreatedBy = currentUser
                    };

                    var receiptId = await connection.QuerySingleAsync<int>(sql, parameters);
                    receipt.ReceiptId = receiptId;
                    receipt.CreatedAt = DateTime.Now;
                    receipt.CreatedBy = currentUser;

                    Logger.Info($"Receipt {receipt.ReceiptNumberModern} saved successfully with ID {receiptId}");
                    
                    return receipt;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveReceiptAsync: {ex.Message}");
                // Enhanced logging with receipt details for troubleshooting
                var receiptInfo = $"Receipt#{receipt.ReceiptNumberModern ?? "NULL"}, Date={receipt.ReceiptDate:yyyy-MM-dd}, " +
                                 $"GrowerId={receipt.GrowerId}, ProductId={receipt.ProductId}, ProcessId={receipt.ProcessId}, " +
                                 $"PriceClassId={receipt.PriceClassId}, Grade={receipt.GradeModern}, " +
                                 $"ContainerId={receipt.ContainerId}";
                Logger.Error($"Error saving receipt [{receiptInfo}]: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> DeleteReceiptAsync(decimal receiptNumber)
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
                          
                    var parameters = new { ReceiptNumber = receiptNumber.ToString(), DeletedBy = currentUser };
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

        public async Task<bool> VoidReceiptAsync(decimal receiptNumber, string reason)
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
                        ReceiptNumber = receiptNumber.ToString(), 
                        VoidedReason = reason,
                        VoidedBy = currentUser,
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

        public async Task<List<Receipt>> GetReceiptsByGrowerAsync(decimal growerNumber, DateTime? startDate = null, DateTime? endDate = null)
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
                            r.DepotId, r.ContainerId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId, r.PriceAreaId,
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
                        receipt.ReceiptNumber = decimal.Parse(receipt.ReceiptNumberModern ?? "0");
                        receipt.GrowerNumber = growerNumber;
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoidedModern;
                        receipt.Date = receipt.ReceiptDate;
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
                            r.DepotId, r.ContainerId, r.VarietyId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, 
                            r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId, r.PriceAreaId,
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
                        receipt.ReceiptNumber = decimal.Parse(receipt.ReceiptNumberModern ?? "0");
                        receipt.Gross = receipt.GrossWeight;
                        receipt.Tare = receipt.TareWeight;
                        receipt.Net = receipt.NetWeight;
                        receipt.DockPercent = receipt.DockPercentage;
                        receipt.IsVoid = receipt.IsVoidedModern;
                        receipt.Date = receipt.ReceiptDate;
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
                    var existingReceipt = await GetReceiptByNumberAsync(receipt.ReceiptNumber);
                    if (existingReceipt != null)
                    {
                        return false;
                    }

                    // Validate grower exists
                    var growerCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Grower WHERE NUMBER = @GrowerNumber",
                        new { receipt.GrowerNumber });
                    if (growerCount == 0)
                    {
                        return false;
                    }

                    // Validate product exists
                    var productCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Product WHERE PRODUCT = @Product",
                        new { receipt.Product });
                    if (productCount == 0)
                    {
                        return false;
                    }

                    // Validate process exists
                    var processCount = await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM Process WHERE PROCESS = @Process",
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

        public async Task<decimal> GetPriceForReceiptAsync(Receipt receipt)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Select a default price column (e.g., CL1G1A1) instead of THEPRICE
                    // TODO: Determine the correct default price column based on business rules if needed.
                    var sql = @"
                        SELECT TOP 1 CL1G1A1
                        FROM Price
                        WHERE PRODUCT = @Product
                        AND PROCESS = @Process
                        AND [FROM] <= @Date -- Escaped column name
                        ORDER BY [FROM] DESC"; // Removed SQL comment

                    var price = await connection.ExecuteScalarAsync<decimal?>(sql, new { receipt.Product, receipt.Process, receipt.Date }); // Pass parameters explicitly
                    return price ?? 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetPriceForReceiptAsync: {ex.Message}");
                 throw;
            }
        }

        /// <summary>
        /// Applies dockage to a receipt. Stores original net weight in OriNet before applying dockage percentage.
        /// Mirrors XBase logic: ori_net stores weight before dockage, net stores weight after dockage.
        /// Dockage calculation: new_net = ori_net * (1 - dock_pct/100)
        /// </summary>
        public async Task<decimal> ApplyDockageAsync(Receipt receipt)
        {
            try
            {
                // If no dockage percentage, return net weight as-is
                if (receipt.DockPercent <= 0)
                {
                    return receipt.Net;
                }

                // Store original net weight if not already stored
                if (!receipt.OriNet.HasValue || receipt.OriNet.Value == 0)
                {
                    receipt.OriNet = receipt.Net;
                }

                // Calculate dockage: reduce net weight by dockage percentage
                // Formula: adjusted_net = original_net * (1 - dockage_percent/100)
                decimal adjustedNet = receipt.OriNet.Value * (1 - (receipt.DockPercent / 100m));
                
                // Round to 2 decimal places (consistent with legacy rounding)
                adjustedNet = Math.Round(adjustedNet, 2);

                // Update the receipt's net weight
                receipt.Net = adjustedNet;

                Logger.Info($"Applied dockage to Receipt {receipt.ReceiptNumber}: OriNet={receipt.OriNet}, DockPct={receipt.DockPercent}%, AdjustedNet={adjustedNet}, Dockage={receipt.OriNet - adjustedNet}");

                // If receipt is already saved, update the database
                if (receipt.ReceiptNumber > 0)
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        var sql = @"
                            UPDATE Daily 
                            SET ORI_NET = @OriNet, 
                                NET = @Net,
                                DOCK_PCT = @DockPercent
                            WHERE RECPT = @ReceiptNumber";

                        await connection.ExecuteAsync(sql, new 
                        { 
                            receipt.OriNet, 
                            receipt.Net, 
                            receipt.DockPercent, 
                            receipt.ReceiptNumber 
                        });
                    }
                }

                return adjustedNet;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error applying dockage to Receipt {receipt.ReceiptNumber}: {ex.Message}", ex);
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
            List<decimal> includeGrowerIds = null,
            List<string> includePayGroupIds = null,
            List<decimal> excludeGrowerIds = null,
            List<string> excludePayGroupIds = null,
            List<string> productIds = null,
            List<string> processIds = null,
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
                    // Updated SELECT to explicitly list columns with aliases
                    var selector = sqlBuilder.AddTemplate(@"
                        SELECT
                            d.DEPOT as Depot, d.PRODUCT as Product, d.RECPT as ReceiptNumber, d.NUMBER as GrowerNumber,
                            d.GROSS as Gross, d.TARE as Tare, d.NET as Net, d.GRADE as Grade, d.PROCESS as Process,
                            d.DATE as Date, d.DAY_UNIQ as DayUniq, d.IMP_BAT as ImpBatch, d.FIN_BAT as FinBatch,
                            d.DOCK_PCT as DockPercent, d.ISVOID as IsVoid, d.THEPRICE as ThePrice, d.PRICESRC as PriceSource,
                            d.PR_NOTE1 as PrNote1, d.NP_NOTE1 as NpNote1, d.FROM_FIELD as FromField, d.IMPORTED as Imported,
                            d.CONT_ERRS as ContainerErrors, d.ADV_PR1 as AdvPr1, d.ADV_PRID1 as AdvPrid1, d.POST_BAT1 as PostBat1,
                            d.ADV_PR2 as AdvPr2, d.ADV_PRID2 as AdvPrid2, d.POST_BAT2 as PostBat2, d.ADV_PR3 as AdvPr3,
                            d.ADV_PRID3 as AdvPrid3, d.POST_BAT3 as PostBat3, d.PREM_PRICE as PremPrice, d.LAST_ADVPB as LastAdvpb,
                            d.ORI_NET as OriNet, d.CERTIFIED as Certified, d.VARIETY as Variety, d.TIME as Time,
                            d.FIN_PRICE as FinPrice, d.FIN_PR_ID as FinPrId, d.ADD_DATE as AddDate, d.ADD_BY as AddBy,
                            d.EDIT_DATE as EditDate, d.EDIT_BY as EditBy, d.EDIT_REAS as EditReason
                        FROM Daily d
                        INNER JOIN Grower g ON d.NUMBER = g.NUMBER
                        /**where**/
                        ORDER BY d.NUMBER, d.DATE, d.RECPT"
                    );

                    // Add mandatory conditions
                    sqlBuilder.Where("d.DATE <= @CutoffDate", new { CutoffDate = cutoffDate });
                    sqlBuilder.Where("d.FIN_BAT = 0"); // Not finalized
                    sqlBuilder.Where($"(d.{postBatchCheckColumn} = 0 OR d.{postBatchCheckColumn} IS NULL)"); // Check for 0 or NULL
                    sqlBuilder.Where("d.ISVOID = 0"); // Not voided
                    sqlBuilder.Where("g.ONHOLD = 0"); // Grower not on hold
                    sqlBuilder.Where("d.NET > 0"); // for payment , net should be >0
                    if (cropYear.HasValue) // Add Crop Year filter using YEAR() function
                    {
                         sqlBuilder.Where("YEAR(d.DATE) = @CropYear", new { CropYear = cropYear.Value });
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
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipts for advance payment {advanceNumber}: {ex.Message}", ex);
                throw;
            }
        }
    }
}
