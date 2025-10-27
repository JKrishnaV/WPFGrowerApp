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
using WPFGrowerApp.Models; // Added for ValidationResult

namespace WPFGrowerApp.DataAccess.Services
{
        public class ReceiptService : BaseDatabaseService, IReceiptService
        {
            private readonly IPaymentTypeService _paymentTypeService;

            public ReceiptService(IPaymentTypeService paymentTypeService)
            {
                _paymentTypeService = paymentTypeService ?? throw new ArgumentNullException(nameof(paymentTypeService));
            }
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
            using (var operation = Logger.BeginTimedOperation("GetReceiptsAsync"))
            {
                Logger.Debug($"Starting GetReceiptsAsync. StartDate: {startDate}, EndDate: {endDate}");
                
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
                    
                    Logger.Info($"Successfully retrieved {receipts.Count} receipts");
                    return receipts;
                }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to retrieve receipts. StartDate: {startDate}, EndDate: {endDate}", ex);
                    throw;
                }
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
                Logger.Error($"Failed to retrieve receipt by number: {receiptNumber}", ex);
                throw;
            }
        }

        public async Task<Receipt> SaveReceiptAsync(Receipt receipt)
        {
            using (var operation = Logger.BeginTimedOperation("SaveReceiptAsync"))
            {
                Logger.Debug($"Starting SaveReceiptAsync. ReceiptNumber: {receipt?.ReceiptNumber}, GrowerId: {receipt?.GrowerId}");
                
                try
                {
                    if (receipt == null) throw new ArgumentNullException(nameof(receipt));

                // Generate receipt number if not set
                if (string.IsNullOrEmpty(receipt.ReceiptNumber))
                {
                    var nextNumber = await GetNextReceiptNumberAsync();
                    receipt.ReceiptNumber = nextNumber.ToString();
                }

                // Check if receipt already exists (soft-deleted)
                var existingReceipt = await GetReceiptByNumberAsync(decimal.Parse(receipt.ReceiptNumber));
                if (existingReceipt != null && existingReceipt.DeletedAt.HasValue)
                {
                    // Receipt exists but is soft-deleted - undelete and update it
                    Logger.Info($"Undeleteing soft-deleted receipt {receipt.ReceiptNumber}");
                    await UndeleteReceiptAsync(receipt.ReceiptNumber);
                    
                    // Update the existing receipt with new data
                    return await UpdateReceiptAsync(receipt);
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
                    
                    Logger.Info($"Receipt {receipt.ReceiptNumber} completed successfully");
                    return receipt;
                }
                }
                catch (Exception ex)
                {
                    // Enhanced logging with receipt details for troubleshooting
                    var containerCount = receipt.ContainerData?.Count ?? 0;
                    var receiptInfo = $"Receipt#{receipt.ReceiptNumber ?? "NULL"}, Date={receipt.ReceiptDate:yyyy-MM-dd}, " +
                                     $"GrowerId={receipt.GrowerId}, ProductId={receipt.ProductId}, ProcessId={receipt.ProcessId}, " +
                                     $"Grade={receipt.Grade}, Containers={containerCount}";
                    Logger.Error($"Failed to save receipt [{receiptInfo}]", ex);
                    throw;
                }
            }
        }

        public async Task<bool> DeleteReceiptAsync(string receiptNumber)
        {
            try
            {
                // Check if receipt can be deleted (no payments)
                if (!await CanDeleteReceiptAsync(receiptNumber))
                {
                    Logger.Warn($"Cannot delete receipt {receiptNumber} - has payment allocations");
                    throw new InvalidOperationException($"Cannot delete receipt {receiptNumber} because it has payment allocations. Please void the payments first.");
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get current user
                    string currentUser = App.CurrentUser?.Username ?? "SYSTEM" ; // TODO: Get from current session/user context
                    
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
                Logger.Error($"Failed to delete receipt: {receiptNumber}", ex);
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
                    string currentUser = App.CurrentUser?.Username ?? "SYSTEM"; // TODO: Get from current session/user context
                    
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
                    // Query modern Receipts table by ImportBatchId with Grower join
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
                            r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        WHERE r.ImportBatchId = (SELECT ImportBatchId FROM ImportBatches WHERE BatchNumber = @ImpBatch)
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
            int sequenceNumber,
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
            // Get payment type information by sequence number
            var paymentType = await GetPaymentTypeBySequenceNumberAsync(sequenceNumber);
            if (paymentType == null)
            {
                throw new ArgumentException($"Payment type with sequence number {sequenceNumber} not found.");
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    // Base query selects from Receipts and joins Growers, PaymentGroups, Products, Processes
                    var sqlBuilder = new SqlBuilder();
                    // Updated SELECT to use only columns present in Receipts and Growers tables
                    var selector = sqlBuilder.AddTemplate(@"
                        SELECT
                            r.ReceiptId,
                            r.ReceiptNumber,
                            r.ReceiptDate,
                            r.ReceiptTime,
                            r.GrowerId,
                            g.GrowerNumber,
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
                        LEFT JOIN PaymentGroups pg ON g.PaymentGroupId = pg.PaymentGroupId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        LEFT JOIN Processes pr ON r.ProcessId = pr.ProcessId
                        LEFT JOIN ReceiptPaymentAllocations rpa 
                            ON r.ReceiptId = rpa.ReceiptId 
                            AND rpa.PaymentTypeId = @PaymentTypeId
                            AND (rpa.Status IS NULL OR rpa.Status != 'Voided')
                        /**where**/
                        ORDER BY r.GrowerId, r.ReceiptDate, r.ReceiptNumber"
                    );

                    // Add mandatory conditions
                sqlBuilder.Where("r.ReceiptDate <= @CutoffDate", new { CutoffDate = cutoffDate });
                sqlBuilder.Where("r.IsVoided = 0"); // Not voided
                sqlBuilder.Where("r.DeletedAt IS NULL"); // Not soft deleted
                sqlBuilder.Where("r.NetWeight > 0"); // for payment , net should be >0
                sqlBuilder.Where("rpa.AllocationId IS NULL"); // Exclude receipts already paid for this advance (ignores voided allocations)
                // ==================================================================================
                // GENERIC PAYMENT SEQUENCE LOGIC - WORKS FOR ANY SEQUENCE NUMBER!
                // ==================================================================================
                
                // For payment types with sequence > 1, ensure receipt has been paid in previous sequences
                if (sequenceNumber > 1)
                {
                    // Get all payment types with sequence numbers less than current
                    var previousPaymentTypes = await GetPreviousPaymentSequencesAsync(sequenceNumber);
                    
                    if (previousPaymentTypes.Any())
                    {
                        // Build subquery to ensure receipt has allocations for ALL previous payment types
                        var previousPaymentTypeIds = string.Join(",", previousPaymentTypes.Select(pt => pt.PaymentTypeId));
                        
                        sqlBuilder.Where($@"
                            EXISTS (
                                SELECT 1 
                                FROM ReceiptPaymentAllocations rpa_prev 
                                WHERE rpa_prev.ReceiptId = r.ReceiptId 
                                AND rpa_prev.PaymentTypeId IN ({previousPaymentTypeIds})
                                AND (rpa_prev.Status IS NULL OR rpa_prev.Status != 'Voided')
                            )");
                    }
                }

                // Add PaymentTypeId parameter for the LEFT JOIN
                sqlBuilder.AddParameters(new { PaymentTypeId = paymentType.PaymentTypeId });
                if (cropYear.HasValue) // Add Crop Year filter using YEAR() function
                {
                    sqlBuilder.Where("YEAR(r.ReceiptDate) = @CropYear", new { CropYear = cropYear.Value });
                }


                    // Add optional list filters using WHERE IN / NOT IN
                    if (includeGrowerIds?.Any() ?? false)
                        sqlBuilder.Where("r.GrowerId IN @IncludeGrowerIds", new { IncludeGrowerIds = includeGrowerIds });
                    if (excludeGrowerIds?.Any() ?? false)
                        sqlBuilder.Where("r.GrowerId NOT IN @ExcludeGrowerIds", new { ExcludeGrowerIds = excludeGrowerIds });
                    if (includePayGroupIds?.Any() ?? false)
                        sqlBuilder.Where("pg.GroupCode IN @IncludePayGroupIds", new { IncludePayGroupIds = includePayGroupIds });
                    if (excludePayGroupIds?.Any() ?? false)
                        sqlBuilder.Where("pg.GroupCode NOT IN @ExcludePayGroupIds", new { ExcludePayGroupIds = excludePayGroupIds });
                    if (productIds?.Any() ?? false)
                        sqlBuilder.Where("r.ProductId IN @ProductIds", new { ProductIds = productIds });
                    if (processIds?.Any() ?? false)
                        sqlBuilder.Where("r.ProcessId IN @ProcessIds", new { ProcessIds = processIds });

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
                Logger.Error($"Error getting receipts for sequence number {sequenceNumber}: {ex.Message}", ex);
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
                             PricePerPound, QuantityPaid, AmountPaid, AllocatedAt, Status)
                        VALUES 
                            (@ReceiptId, @PaymentBatchId, @PaymentTypeId, @PriceScheduleId,
                             @PricePerPound, @QuantityPaid, @AmountPaid, @AllocatedAt, @Status);";
                    
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

        // Transaction-aware overload for CreateReceiptPaymentAllocationAsync
        public async Task CreateReceiptPaymentAllocationAsync(
            ReceiptPaymentAllocation allocation,
            SqlConnection connection,
            SqlTransaction transaction)
        {
            try
            {
                var sql = @"
                    INSERT INTO ReceiptPaymentAllocations 
                        (ReceiptId, PaymentBatchId, PaymentTypeId, PriceScheduleId, 
                         PricePerPound, QuantityPaid, AmountPaid, AllocatedAt, Status)
                    VALUES 
                        (@ReceiptId, @PaymentBatchId, @PaymentTypeId, @PriceScheduleId,
                         @PricePerPound, @QuantityPaid, @AmountPaid, @AllocatedAt, @Status);";
                
                await connection.ExecuteAsync(sql, allocation, transaction: transaction);
                Logger.Info($"Created payment allocation (in transaction) for Receipt {allocation.ReceiptId}, Batch {allocation.PaymentBatchId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error creating payment allocation in transaction for Receipt {allocation.ReceiptId}: {ex.Message}", ex);
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
                            rpa.AllocationId,
                            rpa.ReceiptId,
                            rpa.PaymentBatchId,
                            rpa.PaymentTypeId,
                            rpa.PriceScheduleId,
                            rpa.PricePerPound,
                            rpa.QuantityPaid as AllocatedWeight,
                            rpa.AmountPaid,
                            rpa.AllocatedAt,
                            rpa.Status,
                            pb.BatchNumber
                        FROM ReceiptPaymentAllocations rpa
                        LEFT JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        WHERE rpa.ReceiptId = @ReceiptId
                          AND (rpa.Status IS NULL OR rpa.Status != 'Voided')
                        ORDER BY rpa.PaymentTypeId;";
                    
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
                        WHERE ReceiptId = @ReceiptId
                          AND (Status IS NULL OR Status != 'Voided');";
                    
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

        #region New Enhanced Methods

        /// <summary>
        /// Get detailed receipt information for a grower across selected batches
        /// </summary>
        public async Task<List<ReceiptDetailDto>> GetReceiptDetailsForGrowerAsync(int growerId, List<int> batchIds)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get receipts through ReceiptPaymentAllocations table
                    var sql = @"
                        SELECT DISTINCT
                            r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName, g.GrowerNumber, g.Address as GrowerAddress, 
                            g.PhoneNumber as GrowerPhone, g.Email as GrowerEmail,
                            p.ProductName as ProductName, p.Description as ProductDescription,
                            pr.ProcessName, pr.Description as ProcessDescription,
                            v.VarietyName,
                            d.DepotName, d.Address as DepotAddress,
                            pc.ClassName as PriceClassName,
                            rpa.PricePerPound,
                            pb.BatchNumber as BatchName,
                            pt.TypeName as PaymentTypeName,
                            pt.PaymentTypeId
                        FROM Receipts r
                        INNER JOIN ReceiptPaymentAllocations rpa ON r.ReceiptId = rpa.ReceiptId
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        LEFT JOIN Processes pr ON r.ProcessId = pr.ProcessId
                        LEFT JOIN Varieties v ON r.VarietyId = v.VarietyId
                        LEFT JOIN Depots d ON r.DepotId = d.DepotId
                        LEFT JOIN PriceClasses pc ON r.PriceClassId = pc.PriceClassId
                        LEFT JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        LEFT JOIN PaymentTypes pt ON pb.PaymentTypeId = pt.PaymentTypeId
                        WHERE r.GrowerId = @GrowerId 
                          AND rpa.PaymentBatchId IN @BatchIds
                          AND r.DeletedAt IS NULL
                          AND r.IsVoided = 0
                        ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC";
                    
                    var results = (await connection.QueryAsync<ReceiptDetailDto>(sql, new { GrowerId = growerId, BatchIds = batchIds })).ToList();
                    
                    // Calculate amounts for each receipt using PricePerPound from allocation
                    foreach (var result in results)
                    {
                        result.TotalAmountPaid = result.FinalWeight * result.PricePerPound;
                    }
                    
                    return results;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetReceiptDetailsForGrowerAsync: {ex.Message}");
                Logger.Error($"Error getting receipt details for grower {growerId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get detailed receipt information with joined data
        /// </summary>
        public async Task<ReceiptDetailDto?> GetReceiptDetailAsync(int receiptId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
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
                            d.DepotName, d.Address as DepotAddress
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        LEFT JOIN Processes pr ON r.ProcessId = pr.ProcessId
                        LEFT JOIN Varieties v ON r.VarietyId = v.VarietyId
                        LEFT JOIN Depots d ON r.DepotId = d.DepotId
                        WHERE r.ReceiptId = @ReceiptId";
                    
                    var result = await connection.QueryFirstOrDefaultAsync<ReceiptDetailDto>(sql, new { ReceiptId = receiptId });
                    
                    if (result != null)
                    {
                        // Get payment allocation summary
                        var paymentSummary = await GetReceiptPaymentSummaryAsync(receiptId);
                        if (paymentSummary != null)
                        {
                            result.IsPaid = paymentSummary.TotalAmountPaid > 0;
                            result.TotalAmountPaid = paymentSummary.TotalAmountPaid;
                            result.LastPaymentDate = paymentSummary.LastPaymentDate;
                        }
                        
                        // Get audit trail summary (simplified - no audit table exists)
                        var auditCount = 0;
                        result.ChangeCount = auditCount;
                        
                        if (result.ModifiedAt.HasValue)
                        {
                            result.LastModifiedDate = result.ModifiedAt;
                            result.LastModifiedBy = result.ModifiedBy;
                        }
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipt detail for ReceiptId {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get audit history for a receipt
        /// </summary>
        public Task<List<ReceiptAuditEntry>> GetReceiptAuditHistoryAsync(int receiptId)
        {
            try
            {
                // Audit table doesn't exist, return empty list
                Logger.Info($"Audit history requested for ReceiptId {receiptId}, but ReceiptAuditTrail table doesn't exist");
                return Task.FromResult(new List<ReceiptAuditEntry>());
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting audit history for ReceiptId {receiptId}: {ex.Message}", ex);
                return Task.FromResult(new List<ReceiptAuditEntry>());
            }
        }

        /// <summary>
        /// Get related receipts (same grower, same date, or duplicates)
        /// </summary>
        public async Task<List<Receipt>> GetRelatedReceiptsAsync(int receiptId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // First get the source receipt
                    var sourceReceipt = await connection.QueryFirstOrDefaultAsync<Receipt>(
                        "SELECT * FROM Receipts WHERE ReceiptId = @ReceiptId", 
                        new { ReceiptId = receiptId });
                    
                    if (sourceReceipt == null) return new List<Receipt>();
                    
                    var sql = @"
                        SELECT 
                            r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        WHERE r.DeletedAt IS NULL
                          AND r.ReceiptId != @ReceiptId
                          AND (
                              (r.GrowerId = @GrowerId AND r.ReceiptDate = @ReceiptDate) OR
                              (r.ReceiptNumber = @ReceiptNumber AND r.ReceiptId != @ReceiptId)
                          )
                        ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC";
                    
                    var result = await connection.QueryAsync<Receipt>(sql, new { 
                        ReceiptId = receiptId,
                        GrowerId = sourceReceipt.GrowerId,
                        ReceiptDate = sourceReceipt.ReceiptDate,
                        ReceiptNumber = sourceReceipt.ReceiptNumber
                    });
                    
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting related receipts for ReceiptId {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Duplicate a receipt with new receipt number and date
        /// </summary>
        public async Task<Receipt> DuplicateReceiptAsync(int receiptId, string createdBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get the source receipt
                    var sourceReceipt = await connection.QueryFirstOrDefaultAsync<Receipt>(
                        "SELECT * FROM Receipts WHERE ReceiptId = @ReceiptId", 
                        new { ReceiptId = receiptId });
                    
                    if (sourceReceipt == null)
                        throw new ArgumentException($"Receipt with ID {receiptId} not found");
                    
                    // Generate new receipt number
                    var nextNumber = await GetNextReceiptNumberAsync();
                    
                    // Create duplicate with new receipt number and current date/time
                    var duplicateReceipt = new Receipt
                    {
                        ReceiptNumber = nextNumber.ToString(),
                        ReceiptDate = DateTime.Today,
                        ReceiptTime = DateTime.Now.TimeOfDay,
                        GrowerId = sourceReceipt.GrowerId,
                        ProductId = sourceReceipt.ProductId,
                        ProcessId = sourceReceipt.ProcessId,
                        ProcessTypeId = sourceReceipt.ProcessTypeId,
                        VarietyId = sourceReceipt.VarietyId,
                        DepotId = sourceReceipt.DepotId,
                        GrossWeight = sourceReceipt.GrossWeight,
                        TareWeight = sourceReceipt.TareWeight,
                        NetWeight = sourceReceipt.NetWeight,
                        DockPercentage = sourceReceipt.DockPercentage,
                        DockWeight = sourceReceipt.DockWeight,
                        FinalWeight = sourceReceipt.FinalWeight,
                        Grade = sourceReceipt.Grade,
                        PriceClassId = sourceReceipt.PriceClassId,
                        IsVoided = false,
                        VoidedReason = null,
                        VoidedAt = null,
                        VoidedBy = null,
                        ImportBatchId = null,
                        CreatedAt = DateTime.Now,
                        CreatedBy = createdBy,
                        ModifiedAt = null,
                        ModifiedBy = null,
                        QualityCheckedAt = null,
                        QualityCheckedBy = null,
                        DeletedAt = null,
                        DeletedBy = null
                    };
                    
                    // Save the duplicate
                    var savedReceipt = await SaveReceiptAsync(duplicateReceipt);
                    
                    Logger.Info($"Duplicated receipt {sourceReceipt.ReceiptNumber} as {savedReceipt.ReceiptNumber} by {createdBy}");
                    return savedReceipt;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error duplicating receipt {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Mark a receipt as quality checked
        /// </summary>
        public async Task<bool> MarkQualityCheckedAsync(int receiptId, string checkedBy)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        UPDATE Receipts
                        SET QualityCheckedAt = GETDATE(),
                            QualityCheckedBy = @CheckedBy,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ReceiptId = @ReceiptId
                          AND DeletedAt IS NULL
                          AND IsVoided = 0";
                    
                    var parameters = new { 
                        ReceiptId = receiptId, 
                        CheckedBy = checkedBy,
                        ModifiedBy = checkedBy
                    };
                    
                    var result = await connection.ExecuteAsync(sql, parameters);
                    
                    if (result > 0)
                    {
                        Logger.Info($"Receipt {receiptId} marked as quality checked by {checkedBy}");
                    }
                    
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error marking receipt {receiptId} as quality checked: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Validate receipt data comprehensively
        /// </summary>
        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateReceiptAsync(Receipt receipt)
        {
            try
            {
                var validationService = new ReceiptValidationService(
                    null, null, null, null, this); // TODO: Inject proper dependencies
                
                return await validationService.ValidateReceiptDataAsync(receipt);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating receipt: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get receipt analytics for a date range
        /// </summary>
        public async Task<ReceiptAnalytics> GetReceiptAnalyticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var analyticsService = new ReceiptAnalyticsService(this);
                return await analyticsService.GetReceiptAnalyticsAsync(startDate, endDate);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipt analytics: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Generate receipt PDF
        /// </summary>
        public async Task<byte[]> GenerateReceiptPdfAsync(int receiptId)
        {
            try
            {
                var exportService = new ReceiptExportService(this, null); // TODO: Inject proper dependencies
                return await exportService.GenerateReceiptPrintPreviewAsync(receiptId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating PDF for receipt {receiptId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Bulk void multiple receipts
        /// </summary>
        public async Task<bool> BulkVoidReceiptsAsync(List<int> receiptIds, string reason, string voidedBy)
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
                            var sql = @"
                                UPDATE Receipts
                                SET IsVoided = 1,
                                    VoidedReason = @VoidedReason,
                                    VoidedAt = GETDATE(),
                                    VoidedBy = @VoidedBy,
                                    ModifiedAt = GETDATE(),
                                    ModifiedBy = @ModifiedBy
                                WHERE ReceiptId IN @ReceiptIds
                                  AND DeletedAt IS NULL
                                  AND IsVoided = 0";
                            
                            var parameters = new { 
                                ReceiptIds = receiptIds,
                                VoidedReason = reason,
                                VoidedBy = voidedBy,
                                ModifiedBy = voidedBy
                            };
                            
                            var result = await connection.ExecuteAsync(sql, parameters, transaction);
                            
                            transaction.Commit();
                            
                            if (result > 0)
                            {
                                Logger.Info($"Bulk voided {result} receipts by {voidedBy}. Reason: {reason}");
                            }
                            
                            return result > 0;
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
                Logger.Error($"Error bulk voiding receipts: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get receipts with advanced filtering and total count
        /// </summary>
        public async Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsWithFiltersAndCountAsync(ReceiptFilters filters)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // First, get the total count
                    var countBuilder = new SqlBuilder();
                    var countSelector = countBuilder.AddTemplate(@"
                        SELECT COUNT(*)
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        /**where**/"
                    );
                    
                    // Add mandatory conditions for count
                    countBuilder.Where("r.DeletedAt IS NULL");
                    
                    // Add filters for count
                    if (filters.StartDate.HasValue)
                        countBuilder.Where("r.ReceiptDate >= @StartDate", new { filters.StartDate });
                    
                    if (filters.EndDate.HasValue)
                        countBuilder.Where("r.ReceiptDate <= @EndDate", new { filters.EndDate });
                    
                    if (!string.IsNullOrEmpty(filters.SearchText))
                        countBuilder.Where("(r.ReceiptNumber LIKE @SearchText OR g.FullName LIKE @SearchText)", 
                            new { SearchText = $"%{filters.SearchText}%" });
                    
                    if (filters.ShowVoided.HasValue)
                        countBuilder.Where("r.IsVoided = @ShowVoided", new { filters.ShowVoided });
                    
                    if (filters.ProductId.HasValue)
                        countBuilder.Where("r.ProductId = @ProductId", new { filters.ProductId });
                    
                    if (filters.DepotId.HasValue)
                        countBuilder.Where("r.DepotId = @DepotId", new { filters.DepotId });
                    
                    if (filters.GrowerId.HasValue)
                        countBuilder.Where("r.GrowerId = @GrowerId", new { filters.GrowerId });
                    
                    if (!string.IsNullOrEmpty(filters.CreatedBy))
                        countBuilder.Where("r.CreatedBy = @CreatedBy", new { filters.CreatedBy });
                    
                    if (filters.Grade.HasValue)
                        countBuilder.Where("r.Grade = @Grade", new { filters.Grade });
                    
                    if (filters.IsQualityChecked.HasValue)
                    {
                        if (filters.IsQualityChecked.Value)
                            countBuilder.Where("r.QualityCheckedAt IS NOT NULL");
                        else
                            countBuilder.Where("r.QualityCheckedAt IS NULL");
                    }
                    
                    // Get total count
                    var totalCount = await connection.QuerySingleAsync<int>(countSelector.RawSql, countSelector.Parameters);
                    
                    // Now get the paginated results
                    var sqlBuilder = new SqlBuilder();
                    var selector = sqlBuilder.AddTemplate(@"
                        SELECT 
                            r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName,
                            p.ProductName,
                            d.DepotName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        LEFT JOIN Depots d ON r.DepotId = d.DepotId
                        /**where**/
                        ORDER BY 
                            CASE WHEN @SortBy = 'ReceiptDate' AND @SortDescending = 1 THEN r.ReceiptDate END DESC,
                            CASE WHEN @SortBy = 'ReceiptDate' AND @SortDescending = 0 THEN r.ReceiptDate END ASC,
                            CASE WHEN @SortBy = 'ReceiptNumber' AND @SortDescending = 1 THEN r.ReceiptNumber END DESC,
                            CASE WHEN @SortBy = 'ReceiptNumber' AND @SortDescending = 0 THEN r.ReceiptNumber END ASC,
                            r.ReceiptDate DESC, r.ReceiptNumber DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                    );
                    
                    // Add the same conditions for the main query
                    sqlBuilder.Where("r.DeletedAt IS NULL");
                    
                    if (filters.StartDate.HasValue)
                        sqlBuilder.Where("r.ReceiptDate >= @StartDate", new { filters.StartDate });
                    
                    if (filters.EndDate.HasValue)
                        sqlBuilder.Where("r.ReceiptDate <= @EndDate", new { filters.EndDate });
                    
                    if (!string.IsNullOrEmpty(filters.SearchText))
                        sqlBuilder.Where("(r.ReceiptNumber LIKE @SearchText OR g.FullName LIKE @SearchText)", 
                            new { SearchText = $"%{filters.SearchText}%" });
                    
                    if (filters.ShowVoided.HasValue)
                        sqlBuilder.Where("r.IsVoided = @ShowVoided", new { filters.ShowVoided });
                    
                    if (filters.ProductId.HasValue)
                        sqlBuilder.Where("r.ProductId = @ProductId", new { filters.ProductId });
                    
                    if (filters.DepotId.HasValue)
                        sqlBuilder.Where("r.DepotId = @DepotId", new { filters.DepotId });
                    
                    if (filters.GrowerId.HasValue)
                        sqlBuilder.Where("r.GrowerId = @GrowerId", new { filters.GrowerId });
                    
                    if (!string.IsNullOrEmpty(filters.CreatedBy))
                        sqlBuilder.Where("r.CreatedBy = @CreatedBy", new { filters.CreatedBy });
                    
                    if (filters.Grade.HasValue)
                        sqlBuilder.Where("r.Grade = @Grade", new { filters.Grade });
                    
                    if (filters.IsQualityChecked.HasValue)
                    {
                        if (filters.IsQualityChecked.Value)
                            sqlBuilder.Where("r.QualityCheckedAt IS NOT NULL");
                        else
                            sqlBuilder.Where("r.QualityCheckedAt IS NULL");
                    }
                    
                    // Add parameters
                    sqlBuilder.AddParameters(new {
                        filters.SortBy,
                        filters.SortDescending,
                        Offset = (filters.PageNumber - 1) * filters.PageSize,
                        filters.PageSize
                    });
                    
                    var result = await connection.QueryAsync<Receipt>(selector.RawSql, selector.Parameters);
                    return (result.ToList(), totalCount);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipts with filters and count: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get receipts with advanced filtering (legacy method for backward compatibility)
        /// </summary>
        public async Task<List<Receipt>> GetReceiptsWithFiltersAsync(ReceiptFilters filters)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sqlBuilder = new SqlBuilder();
                    var selector = sqlBuilder.AddTemplate(@"
                        SELECT 
                            r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        /**where**/
                        ORDER BY 
                            CASE WHEN @SortBy = 'ReceiptDate' AND @SortDescending = 1 THEN r.ReceiptDate END DESC,
                            CASE WHEN @SortBy = 'ReceiptDate' AND @SortDescending = 0 THEN r.ReceiptDate END ASC,
                            CASE WHEN @SortBy = 'ReceiptNumber' AND @SortDescending = 1 THEN r.ReceiptNumber END DESC,
                            CASE WHEN @SortBy = 'ReceiptNumber' AND @SortDescending = 0 THEN r.ReceiptNumber END ASC,
                            r.ReceiptDate DESC, r.ReceiptNumber DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                    );
                    
                    // Add mandatory conditions
                    sqlBuilder.Where("r.DeletedAt IS NULL");
                    
                    // Add filters
                    if (filters.StartDate.HasValue)
                        sqlBuilder.Where("r.ReceiptDate >= @StartDate", new { filters.StartDate });
                    
                    if (filters.EndDate.HasValue)
                        sqlBuilder.Where("r.ReceiptDate <= @EndDate", new { filters.EndDate });
                    
                    if (!string.IsNullOrEmpty(filters.SearchText))
                        sqlBuilder.Where("(r.ReceiptNumber LIKE @SearchText OR g.FullName LIKE @SearchText)", 
                            new { SearchText = $"%{filters.SearchText}%" });
                    
                    if (filters.ShowVoided.HasValue)
                        sqlBuilder.Where("r.IsVoided = @ShowVoided", new { filters.ShowVoided });
                    
                    if (filters.ProductId.HasValue)
                        sqlBuilder.Where("r.ProductId = @ProductId", new { filters.ProductId });
                    
                    if (filters.DepotId.HasValue)
                        sqlBuilder.Where("r.DepotId = @DepotId", new { filters.DepotId });
                    
                    if (filters.GrowerId.HasValue)
                        sqlBuilder.Where("r.GrowerId = @GrowerId", new { filters.GrowerId });
                    
                    if (!string.IsNullOrEmpty(filters.CreatedBy))
                        sqlBuilder.Where("r.CreatedBy = @CreatedBy", new { filters.CreatedBy });
                    
                    if (filters.Grade.HasValue)
                        sqlBuilder.Where("r.Grade = @Grade", new { filters.Grade });
                    
                    if (filters.IsQualityChecked.HasValue)
                    {
                        if (filters.IsQualityChecked.Value)
                            sqlBuilder.Where("r.QualityCheckedAt IS NOT NULL");
                        else
                            sqlBuilder.Where("r.QualityCheckedAt IS NULL");
                    }
                    
                    // Add parameters
                    sqlBuilder.AddParameters(new {
                        filters.SortBy,
                        filters.SortDescending,
                        Offset = (filters.PageNumber - 1) * filters.PageSize,
                        filters.PageSize
                    });
                    
                    var result = await connection.QueryAsync<Receipt>(selector.RawSql, selector.Parameters);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipts with filters: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Get receipt statistics for dashboard
        /// </summary>
        public async Task<ReceiptStatistics> GetReceiptStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            COUNT(*) as TotalReceipts,
                            SUM(CASE WHEN IsVoided = 0 THEN 1 ELSE 0 END) as ActiveReceipts,
                            SUM(CASE WHEN IsVoided = 1 THEN 1 ELSE 0 END) as VoidedReceipts,
                            SUM(CASE WHEN QualityCheckedAt IS NOT NULL THEN 1 ELSE 0 END) as QualityCheckedReceipts,
                            SUM(GrossWeight) as TotalGrossWeight,
                            SUM(NetWeight) as TotalNetWeight,
                            SUM(FinalWeight) as TotalFinalWeight,
                            AVG(DockPercentage) as AverageDockPercentage,
                            COUNT(DISTINCT GrowerId) as UniqueGrowers,
                            COUNT(DISTINCT ProductId) as UniqueProducts,
                            COUNT(DISTINCT DepotId) as UniqueDepots
                        FROM Receipts
                        WHERE DeletedAt IS NULL";
                    
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
                    
                    var result = await connection.QueryFirstOrDefaultAsync<ReceiptStatistics>(sql, parameters);
                    return result ?? new ReceiptStatistics();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting receipt statistics: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Payment Protection and Re-import Methods

        public async Task<bool> CanDeleteReceiptAsync(string receiptNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Get receipt ID first
                    var receiptIdSql = "SELECT ReceiptId FROM Receipts WHERE ReceiptNumber = @ReceiptNumber AND DeletedAt IS NULL";
                    var receiptId = await connection.ExecuteScalarAsync<int?>(receiptIdSql, new { ReceiptNumber = receiptNumber });
                    
                    if (!receiptId.HasValue)
                        return true; // Receipt doesn't exist, so it can be "deleted"
                    
                    // Check if receipt has any payment allocations
                    var hasPayments = await HasPaymentAllocationsAsync(receiptId.Value);
                    return !hasPayments;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking if receipt {receiptNumber} can be deleted: {ex.Message}", ex);
                return false; // Default to safe side - don't allow deletion if uncertain
            }
        }

        public async Task<bool> HasPaymentAllocationsAsync(int receiptId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        SELECT COUNT(*) 
                        FROM ReceiptPaymentAllocations 
                        WHERE ReceiptId = @ReceiptId 
                          AND (Status IS NULL OR Status != 'Voided')";
                    
                    var count = await connection.ExecuteScalarAsync<int>(sql, new { ReceiptId = receiptId });
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking payment allocations for Receipt {receiptId}: {ex.Message}", ex);
                return true; // Default to safe side - assume has payments if uncertain
            }
        }

        public async Task<bool> UndeleteReceiptAsync(string receiptNumber)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"
                        UPDATE Receipts
                        SET DeletedAt = NULL,
                            DeletedBy = NULL,
                            IsVoided = 0
                        WHERE ReceiptNumber = @ReceiptNumber
                          AND DeletedAt IS NOT NULL";
                          
                    var result = await connection.ExecuteAsync(sql, new { ReceiptNumber = receiptNumber });
                    
                    if (result > 0)
                    {
                        Logger.Info($"Receipt {receiptNumber} undeleted successfully");
                    }
                    
                    return result > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error undeleting receipt {receiptNumber}: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<Receipt> UpdateReceiptAsync(Receipt receipt)
        {
            try
            {
                if (receipt == null) throw new ArgumentNullException(nameof(receipt));

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get current user from App.CurrentUser
                    string currentUser = App.CurrentUser?.Username ?? "SYSTEM";

                    // Calculate computed fields
                    receipt.NetWeight = receipt.GrossWeight - receipt.TareWeight;
                    receipt.DockWeight = receipt.NetWeight * (receipt.DockPercentage / 100);
                    receipt.FinalWeight = receipt.NetWeight - receipt.DockWeight;

                    // Update the receipt
                    var sql = @"
                        UPDATE Receipts SET
                            ReceiptDate = @ReceiptDate,
                            ReceiptTime = @ReceiptTime,
                            GrowerId = @GrowerId,
                            ProductId = @ProductId,
                            ProcessId = @ProcessId,
                            ProcessTypeId = @ProcessTypeId,
                            DepotId = @DepotId,
                            VarietyId = @VarietyId,
                            GrossWeight = @GrossWeight,
                            TareWeight = @TareWeight,
                            DockPercentage = @DockPercentage,
                            Grade = @Grade,
                            PriceClassId = @PriceClassId,
                            IsVoided = @IsVoided,
                            VoidedReason = @VoidedReason,
                            ModifiedAt = GETDATE(),
                            ModifiedBy = @ModifiedBy
                        WHERE ReceiptNumber = @ReceiptNumber
                          AND DeletedAt IS NULL";

                    var parameters = new
                    {
                        receipt.ReceiptNumber,
                        receipt.ReceiptDate,
                        receipt.ReceiptTime,
                        receipt.GrowerId,
                        receipt.ProductId,
                        receipt.ProcessId,
                        receipt.ProcessTypeId,
                        receipt.DepotId,
                        receipt.VarietyId,
                        receipt.GrossWeight,
                        receipt.TareWeight,
                        receipt.DockPercentage,
                        receipt.Grade,
                        receipt.PriceClassId,
                        receipt.IsVoided,
                        receipt.VoidedReason,
                        ModifiedBy = currentUser
                    };

                    var result = await connection.ExecuteAsync(sql, parameters);

                    if (result > 0)
                    {
                        Logger.Info($"Receipt {receipt.ReceiptNumber} updated successfully");
                        
                        // Update container transactions if provided
                        if (receipt.ContainerData != null && receipt.ContainerData.Any())
                        {
                            // Delete existing container transactions
                            await connection.ExecuteAsync(
                                "DELETE FROM ContainerTransactions WHERE ReceiptId = @ReceiptId",
                                new { ReceiptId = receipt.ReceiptId });
                            
                            // Save new container transactions
                            await SaveContainerTransactionsAsync(connection, receipt.ReceiptId, receipt.ContainerData, currentUser);
                        }
                    }

                    return receipt;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error updating receipt {receipt.ReceiptNumber}: {ex.Message}", ex);
                throw;
            }
        }

        #endregion

        #region Optimized Search Methods

        /// <summary>
        /// Optimized search method that uses different query strategies based on search type
        /// </summary>
        public async Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsWithOptimizedSearchAsync(ReceiptFilters filters)
        {
            try
            {
                // Determine search strategy based on search text
                if (!string.IsNullOrEmpty(filters.SearchText))
                {
                    if (IsReceiptNumberSearch(filters.SearchText))
                    {
                        return await GetReceiptsByReceiptNumberAsync(filters);
                    }
                    else if (IsGrowerNameSearch(filters.SearchText))
                    {
                        return await GetReceiptsByGrowerNameAsync(filters);
                    }
                }

                // Use optimized general search for other cases
                return await GetReceiptsOptimizedAsync(filters);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in optimized search: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Optimized search for receipt numbers (exact match)
        /// </summary>
        private async Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsByReceiptNumberAsync(ReceiptFilters filters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var searchText = filters.SearchText.Trim();
                
                // Try exact match first (fastest)
                var exactMatchSql = @"
                    SELECT COUNT(*)
                    FROM Receipts r
                    WHERE r.DeletedAt IS NULL 
                    AND r.ReceiptNumber = @SearchText";
                
                var count = await connection.QuerySingleAsync<int>(exactMatchSql, new { SearchText = searchText });
                
                if (count > 0)
                {
                    // Use exact match query
                    var sql = @"
                        SELECT 
                            r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                            r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                            r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                            r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                            r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                            r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                            g.FullName as GrowerName,
                            p.ProductName,
                            d.DepotName
                        FROM Receipts r
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        LEFT JOIN Depots d ON r.DepotId = d.DepotId
                        WHERE r.DeletedAt IS NULL 
                        AND r.ReceiptNumber = @SearchText
                        ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                    var parameters = new
                    {
                        SearchText = searchText,
                        Offset = (filters.PageNumber - 1) * filters.PageSize,
                        filters.PageSize
                    };

                    var receipts = await connection.QueryAsync<Receipt>(sql, parameters);
                    return (receipts.ToList(), count);
                }
                else
                {
                    // Fall back to LIKE search for partial matches
                    return await GetReceiptsWithLikeSearchAsync(filters);
                }
            }
        }

        /// <summary>
        /// Optimized search for grower names
        /// </summary>
        private async Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsByGrowerNameAsync(ReceiptFilters filters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                var searchText = $"%{filters.SearchText.Trim()}%";
                
                // First get grower IDs that match the search
                var growerIdsSql = @"
                    SELECT GrowerId 
                    FROM Growers 
                    WHERE DeletedAt IS NULL 
                    AND FullName LIKE @SearchText";
                
                var growerIds = await connection.QueryAsync<int>(growerIdsSql, new { SearchText = searchText });
                var growerIdList = growerIds.ToList();
                
                if (!growerIdList.Any())
                {
                    return (new List<Receipt>(), 0);
                }
                
                // Build optimized query with grower ID list
                var countSql = @"
                    SELECT COUNT(*)
                    FROM Receipts r
                    WHERE r.DeletedAt IS NULL 
                    AND r.GrowerId IN @GrowerIds";
                
                var count = await connection.QuerySingleAsync<int>(countSql, new { GrowerIds = growerIdList });
                
                var sql = @"
                    SELECT 
                        r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                        r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                        r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                        r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                        r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                        r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                        g.FullName as GrowerName,
                        p.ProductName,
                        d.DepotName
                    FROM Receipts r
                    LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                    LEFT JOIN Products p ON r.ProductId = p.ProductId
                    LEFT JOIN Depots d ON r.DepotId = d.DepotId
                    WHERE r.DeletedAt IS NULL 
                    AND r.GrowerId IN @GrowerIds
                    ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                var parameters = new
                {
                    GrowerIds = growerIdList,
                    Offset = (filters.PageNumber - 1) * filters.PageSize,
                    filters.PageSize
                };

                var receipts = await connection.QueryAsync<Receipt>(sql, parameters);
                return (receipts.ToList(), count);
            }
        }

        /// <summary>
        /// Optimized general search without search text
        /// </summary>
        private async Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsOptimizedAsync(ReceiptFilters filters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                
                // Build optimized count query
                var countBuilder = new SqlBuilder();
                var countSelector = countBuilder.AddTemplate(@"
                    SELECT COUNT(*)
                    FROM Receipts r
                    /**where**/"
                );
                
                countBuilder.Where("r.DeletedAt IS NULL");
                AddOptimizedFilters(countBuilder, filters);
                
                var count = await connection.QuerySingleAsync<int>(countSelector.RawSql, countSelector.Parameters);
                
                // Build optimized main query
                var sqlBuilder = new SqlBuilder();
                var selector = sqlBuilder.AddTemplate(@"
                    SELECT 
                        r.ReceiptId, r.ReceiptNumber, r.ReceiptDate, r.ReceiptTime,
                        r.GrowerId, r.ProductId, r.ProcessId, r.ProcessTypeId, r.VarietyId, r.DepotId,
                        r.GrossWeight, r.TareWeight, r.NetWeight, r.DockPercentage, r.DockWeight, r.FinalWeight,
                        r.Grade, r.PriceClassId, r.IsVoided, r.VoidedReason, r.VoidedAt, r.VoidedBy,
                        r.ImportBatchId, r.CreatedAt, r.CreatedBy, r.ModifiedAt, r.ModifiedBy,
                        r.QualityCheckedAt, r.QualityCheckedBy, r.DeletedAt, r.DeletedBy,
                        g.FullName as GrowerName,
                        p.ProductName,
                        d.DepotName
                    FROM Receipts r
                    LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                    LEFT JOIN Products p ON r.ProductId = p.ProductId
                    LEFT JOIN Depots d ON r.DepotId = d.DepotId
                    /**where**/
                    ORDER BY r.ReceiptDate DESC, r.ReceiptNumber DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY"
                );
                
                sqlBuilder.Where("r.DeletedAt IS NULL");
                AddOptimizedFilters(sqlBuilder, filters);
                
                // Add pagination parameters
                sqlBuilder.AddParameters(new
                {
                    Offset = (filters.PageNumber - 1) * filters.PageSize,
                    filters.PageSize
                });
                
                var receipts = await connection.QueryAsync<Receipt>(selector.RawSql, selector.Parameters);
                return (receipts.ToList(), count);
            }
        }

        /// <summary>
        /// Fallback method for LIKE searches
        /// </summary>
        private async Task<(List<Receipt> Receipts, int TotalCount)> GetReceiptsWithLikeSearchAsync(ReceiptFilters filters)
        {
            // Use the existing method as fallback
            return await GetReceiptsWithFiltersAndCountAsync(filters);
        }

        /// <summary>
        /// Add optimized filters to query builder
        /// </summary>
        private void AddOptimizedFilters(SqlBuilder builder, ReceiptFilters filters)
        {
            if (filters.StartDate.HasValue)
                builder.Where("r.ReceiptDate >= @StartDate", new { filters.StartDate });
            
            if (filters.EndDate.HasValue)
                builder.Where("r.ReceiptDate <= @EndDate", new { filters.EndDate });
            
            if (filters.ShowVoided.HasValue)
                builder.Where("r.IsVoided = @ShowVoided", new { filters.ShowVoided });
            
            if (filters.ProductId.HasValue)
                builder.Where("r.ProductId = @ProductId", new { filters.ProductId });
            
            if (filters.DepotId.HasValue)
                builder.Where("r.DepotId = @DepotId", new { filters.DepotId });
            
            if (filters.GrowerId.HasValue)
                builder.Where("r.GrowerId = @GrowerId", new { filters.GrowerId });
            
            if (!string.IsNullOrEmpty(filters.CreatedBy))
                builder.Where("r.CreatedBy = @CreatedBy", new { filters.CreatedBy });
            
            if (filters.Grade.HasValue)
                builder.Where("r.Grade = @Grade", new { filters.Grade });
            
            if (filters.IsQualityChecked.HasValue)
            {
                if (filters.IsQualityChecked.Value)
                    builder.Where("r.QualityCheckedAt IS NOT NULL");
                else
                    builder.Where("r.QualityCheckedAt IS NULL");
            }
        }

        /// <summary>
        /// Check if search text is likely a receipt number
        /// </summary>
        private bool IsReceiptNumberSearch(string searchText)
        {
            // Receipt numbers are typically numeric or alphanumeric
            return searchText.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        /// <summary>
        /// Check if search text is likely a grower name
        /// </summary>
        private bool IsGrowerNameSearch(string searchText)
        {
            // Grower names typically contain letters and spaces
            return searchText.Any(c => char.IsLetter(c)) && 
                   (searchText.Contains(' ') || searchText.Length > 3);
        }

        #endregion

        #region Payment Sequence Helper Methods

        /// <summary>
        /// Gets payment type by sequence number
        /// </summary>
        private async Task<PaymentType?> GetPaymentTypeBySequenceNumberAsync(int sequenceNumber)
        {
            try
            {
                var allPaymentTypes = await _paymentTypeService.GetAllPaymentTypesAsync();
                return allPaymentTypes
                    .FirstOrDefault(pt => pt.SequenceNumber == sequenceNumber && pt.IsActive);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting payment type by sequence number {sequenceNumber}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all payment types that have sequence numbers less than the specified sequence.
        /// Used to determine which previous payments must exist before processing current payment.
        /// </summary>
        private async Task<List<PaymentType>> GetPreviousPaymentSequencesAsync(int currentSequenceNumber)
        {
            try
            {
                var allPaymentTypes = await _paymentTypeService.GetAllPaymentTypesAsync();
                return allPaymentTypes
                    .Where(pt => pt.SequenceNumber < currentSequenceNumber && pt.IsActive)
                    .OrderBy(pt => pt.SequenceNumber)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting previous payment sequences for sequence {currentSequenceNumber}: {ex.Message}", ex);
                throw;
            }
        }

        #endregion
    }
}
