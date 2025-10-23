// Duplicate removed
using System;
using System.Collections.Generic;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging; // Assuming Logger is available
using System.Diagnostics; // Added for Debug.WriteLine if needed
using WPFGrowerApp.Models; // Added for TestRunResult etc.

namespace WPFGrowerApp.DataAccess.Services
{
    public class PaymentService : BaseDatabaseService, IPaymentService
    {
        private readonly IReceiptService _receiptService;
        private readonly IPriceService _priceService;
        private readonly IAccountService _accountService;
        private readonly IPaymentBatchService _paymentBatchService;
        private readonly IGrowerService _growerService; // Needed for grower details like currency, GST status
        private readonly IProcessClassificationService _processClassificationService; // For Fresh vs Non-Fresh tracking
        private readonly IPaymentTypeService _paymentTypeService; // For sequence-based payment logic

        // Constants for Account Types (mirroring TT_ values from XBase++)
        // Note: Advance types are now dynamically generated from PaymentTypes table
        // These constants remain for non-advance payment types
        private const string AccTypeDeduction = "DED";  // Placeholder
        private const string AccTypePremium = "PREM"; // Placeholder
        // Add other types as needed (e.g., Loan payments, Equity)

        public PaymentService(
            IReceiptService receiptService,
            IPriceService priceService,
            IAccountService accountService,
            IPaymentBatchService paymentBatchService,
            IGrowerService growerService,
            IProcessClassificationService processClassificationService,
            IPaymentTypeService paymentTypeService)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _paymentBatchService = paymentBatchService ?? throw new ArgumentNullException(nameof(paymentBatchService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _processClassificationService = processClassificationService ?? throw new ArgumentNullException(nameof(processClassificationService));
            _paymentTypeService = paymentTypeService ?? throw new ArgumentNullException(nameof(paymentTypeService));
        }

        // --- Payment Validation (NEW: Pre-flight checks) ---
        /// <summary>
        /// Validates receipts before creating payment draft - checks for missing data and calculation errors
        /// </summary>
        private async Task<PaymentValidationResult> ValidatePaymentCalculationAsync(
            TestRunResult calculationResult,
            IProgress<string>? progress = null)
        {
            var validationResult = new PaymentValidationResult
            {
                TotalReceipts = calculationResult.GrowerPayments.Sum(gp => gp.ReceiptCount)
            };

            foreach (var growerPayment in calculationResult.GrowerPayments)
            {
                foreach (var receiptDetail in growerPayment.ReceiptDetails)
                {
                    // Check for errors in receipt calculation
                    if (!string.IsNullOrEmpty(receiptDetail.ErrorMessage))
                    {
                        validationResult.Errors.Add(new ValidationIssue
                        {
                            ReceiptNumber = receiptDetail.ReceiptNumber.ToString(),
                            GrowerNumber = growerPayment.GrowerNumber,
                            GrowerName = growerPayment.GrowerName,
                            IssueType = DetermineIssueType(receiptDetail.ErrorMessage),
                            Message = receiptDetail.ErrorMessage,
                            Details = $"Product: {receiptDetail.Product}, Process: {receiptDetail.Process}"
                        });
                        validationResult.InvalidReceipts++;
                    }
                    
                    // Check for missing price schedule
                    if (receiptDetail.PriceRecordId <= 0 && string.IsNullOrEmpty(receiptDetail.ErrorMessage))
                    {
                        validationResult.Errors.Add(new ValidationIssue
                        {
                            ReceiptNumber = receiptDetail.ReceiptNumber.ToString(),
                            GrowerNumber = growerPayment.GrowerNumber,
                            GrowerName = growerPayment.GrowerName,
                            IssueType = ValidationIssueType.MissingPriceSchedule,
                            Message = "No price schedule found",
                            Details = $"Product: {receiptDetail.Product}, Process: {receiptDetail.Process}"
                        });
                        validationResult.InvalidReceipts++;
                    }
                    
                    // Check for zero or negative calculated amount (will be filtered out during processing)
                    if (receiptDetail.CalculatedAdvanceAmount <= 0 && string.IsNullOrEmpty(receiptDetail.ErrorMessage))
                    {
                        validationResult.Warnings.Add(new ValidationIssue
                        {
                            ReceiptNumber = receiptDetail.ReceiptNumber.ToString(),
                            GrowerNumber = growerPayment.GrowerNumber,
                            GrowerName = growerPayment.GrowerName,
                            IssueType = ValidationIssueType.CalculationError,
                            Message = "Receipt will be skipped - zero or negative calculated amount",
                            Details = $"Amount: ${receiptDetail.CalculatedAdvanceAmount:N2} (will be filtered out during payment processing)"
                        });
                    }
                }
            }

            validationResult.ValidReceipts = validationResult.TotalReceipts - validationResult.InvalidReceipts;
            validationResult.HasErrors = validationResult.Errors.Any();
            validationResult.HasWarnings = validationResult.Warnings.Any();

            return validationResult;
        }

        private ValidationIssueType DetermineIssueType(string errorMessage)
        {
            if (errorMessage.Contains("product", StringComparison.OrdinalIgnoreCase))
                return ValidationIssueType.MissingProduct;
            if (errorMessage.Contains("process", StringComparison.OrdinalIgnoreCase))
                return ValidationIssueType.MissingProcess;
            if (errorMessage.Contains("price", StringComparison.OrdinalIgnoreCase))
                return ValidationIssueType.MissingPriceSchedule;
            return ValidationIssueType.Other;
        }

        // --- Create Payment Draft (NEW: Split Workflow) ---
        public async Task<(bool Success, List<string> Errors, PaymentBatch? CreatedBatch, TestRunResult? PreviewResult, PaymentValidationResult? ValidationResult)> CreatePaymentDraftAsync(
            int sequenceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            List<int>? excludeGrowerIds = null,
            List<string>? excludePayGroupIds = null,
            List<int>? productIds = null,
            List<int>? processIds = null,
            IProgress<string>? progress = null)
        {
            var errors = new List<string>();
            PaymentBatch? createdBatch = null;
            TestRunResult? previewResult = null;
            PaymentValidationResult? validation = null;

            try
            {
                progress?.Report("Calculating payment details...");
                LogAnEvent(EVT_TYPE_START_ADVANCE_DETERMINE, $"Sequence {sequenceNumber} draft creation started.");

                // 1. Perform Calculation FIRST (before any database writes)
                progress?.Report("Calculating payment details...");
                var parameters = new TestRunInputParameters
                {
                    SequenceNumber = sequenceNumber,
                    PaymentDate = paymentDate,
                    CutoffDate = cutoffDate,
                    CropYear = cropYear,
                    ExcludeGrowerIds = excludeGrowerIds,
                    ExcludePayGroupIds = excludePayGroupIds,
                    ProductIds = productIds,
                    ProcessIds = processIds
                };
                previewResult = await CalculateAdvancePaymentDetailsAsync(parameters, progress);

                if (!previewResult.GrowerPayments.Any())
                {
                    errors.Add("No eligible growers found for payment calculation.");
                    return (false, errors, null, previewResult, null);
                }

                // 2. Validate calculation results BEFORE any database writes (NEW)
                progress?.Report("Validating payment calculations...");
                validation = await ValidatePaymentCalculationAsync(previewResult, progress);

                if (validation.HasErrors || validation.HasWarnings)
                {
                    // Return validation results to UI for user confirmation
                    // Don't create batch or allocations yet - wait for user confirmation
                    var validationSummary = $"Validation found {validation.Errors.Count} errors and {validation.Warnings.Count} warnings.\n" +
                                           $"Valid receipts: {validation.ValidReceipts}/{validation.TotalReceipts}";
                    progress?.Report(validationSummary);
                    
                    // Add validation issues to errors list for UI display
                    errors.AddRange(validation.Errors.Select(e => $"{e.ReceiptNumber} ({e.GrowerName}): {e.Message}"));
                    if (validation.HasWarnings)
                    {
                        errors.AddRange(validation.Warnings.Select(w => $"WARNING - {w.ReceiptNumber}: {w.Message}"));
                    }
                    
                    // Return with validation flag - UI will show confirmation dialog
                    Logger.Info($"Payment validation found {validation.Errors.Count} errors, {validation.Warnings.Count} warnings. Awaiting user confirmation.");
                    return (false, errors, null, previewResult, validation);
                }

                // 3. Validation passed - create batch with allocations using transaction
                // Delegate to CreatePaymentDraftConfirmedAsync for consistent transaction-based creation
                progress?.Report("Validation passed. Creating draft with allocations...");
                
                (bool draftSuccess, List<string> draftErrors, PaymentBatch? finalBatch, TestRunResult? finalResult) = 
                    await CreatePaymentDraftConfirmedAsync(
                        sequenceNumber, paymentDate, cutoffDate, cropYear,
                        excludeGrowerIds, excludePayGroupIds, productIds, processIds,
                        previewResult, // Use the calculated result
                        progress);

                return (draftSuccess, draftErrors, finalBatch, finalResult, null); // No validation issues
            }
            catch (Exception ex)
            {
                errors.Add($"Critical error during draft creation: {ex.Message}");
                Logger.Error($"Critical error during draft creation Sequence {sequenceNumber}", ex);
                return (false, errors, createdBatch, previewResult, validation);
            }
        }

        // --- Create Payment Draft (Confirmed - After User Validation) ---
        /// <summary>
        /// Creates payment draft after user confirmation - skips validation, uses existing calculation
        /// </summary>
        public async Task<(bool Success, List<string> Errors, PaymentBatch? CreatedBatch, TestRunResult? PreviewResult)> CreatePaymentDraftConfirmedAsync(
            int sequenceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            List<int>? excludeGrowerIds,
            List<string>? excludePayGroupIds,
            List<int>? productIds,
            List<int>? processIds,
            TestRunResult existingCalculation,
            IProgress<string>? progress = null)
        {
            var errors = new List<string>();
            PaymentBatch? createdBatch = null;

            try
            {
                progress?.Report("Creating payment draft (user confirmed)...");
                LogAnEvent(EVT_TYPE_START_ADVANCE_DETERMINE, $"Sequence {sequenceNumber} confirmed draft creation started.");
                
                // Filter to only VALID receipts (those without errors)
                var validGrowers = existingCalculation.GrowerPayments
                    .Where(gp => gp.ReceiptDetails.Any(rd => string.IsNullOrEmpty(rd.ErrorMessage)))
                    .ToList();

                if (!validGrowers.Any())
                {
                    errors.Add("No valid receipts found after filtering errors.");
                    return (false, errors, null, existingCalculation);
                }

                // Calculate totals from VALID receipts with positive amounts only
                decimal totalAmount = validGrowers.Sum(gp => 
                    gp.ReceiptDetails
                        .Where(rd => string.IsNullOrEmpty(rd.ErrorMessage) && rd.CalculatedAdvanceAmount > 0)
                        .Sum(rd => rd.CalculatedAdvanceAmount));
                
                int totalGrowersProcessed = validGrowers.Count;
                int totalReceiptsProcessed = validGrowers.Sum(gp => 
                    gp.ReceiptDetails.Count(rd => string.IsNullOrEmpty(rd.ErrorMessage) && rd.CalculatedAdvanceAmount > 0));

                // Wrap ALL database operations in a single transaction
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Get payment type by sequence number to get the correct PaymentTypeId
                            var paymentType = await GetPaymentTypeBySequenceNumberAsync(sequenceNumber);
                            if (paymentType == null)
                            {
                                errors.Add($"Payment type with sequence number {sequenceNumber} not found.");
                                return (false, errors, null, existingCalculation);
                            }
                            
                            // 1. Create Payment Batch (with transaction)
                            createdBatch = await _paymentBatchService.CreatePaymentBatchAsync(
                                paymentType.PaymentTypeId, 
                                paymentDate, 
                                cropYear,
                                $"Sequence {sequenceNumber} Payment Draft (User Confirmed)",
                                connection,
                                transaction);
                            progress?.Report($"Created Payment Draft: {createdBatch.BatchNumber}");

                            // 2. Update batch totals (with transaction)
                            await _paymentBatchService.UpdatePaymentBatchTotalsOnlyAsync(
                                createdBatch.PaymentBatchId,
                                totalGrowersProcessed,
                                totalReceiptsProcessed,
                                totalAmount,
                                connection,
                                transaction);

                            progress?.Report($"Draft totals: {totalGrowersProcessed} growers, {totalReceiptsProcessed} receipts, ${totalAmount:N2}");

                            // 3. Create Receipt Payment Allocations for VALID receipts only (with transaction)
                            progress?.Report("Creating receipt allocations...");
                            int allocationCount = 0;
                            
                            foreach (var growerPayment in validGrowers)
                            {
                                // Only process receipts WITHOUT errors AND with positive amounts
                                var validReceiptDetails = growerPayment.ReceiptDetails
                                    .Where(rd => string.IsNullOrEmpty(rd.ErrorMessage))
                                    .ToList();

                                // Log skipped receipts with zero amounts
                                var skippedReceipts = validReceiptDetails
                                    .Where(rd => rd.CalculatedAdvanceAmount <= 0)
                                    .ToList();

                                if (skippedReceipts.Any())
                                {
                                    var skippedCount = skippedReceipts.Count;
                                    var skippedAmount = skippedReceipts.Sum(rd => rd.CalculatedAdvanceAmount);
                                    progress?.Report($"⚠️ Skipping {skippedCount} receipts with zero/negative amounts (${skippedAmount:N2})");
                                    
                                    foreach (var skippedReceipt in skippedReceipts)
                                    {
                                        var reason = skippedReceipt.CalculatedAdvanceAmount == 0 
                                            ? "Zero calculated amount" 
                                            : "Negative calculated amount";
                                        
                                        Logger.Info($"Skipped Receipt {skippedReceipt.ReceiptNumber} - {reason} (${skippedReceipt.CalculatedAdvanceAmount:N2})");
                                        progress?.Report($"  • Receipt {skippedReceipt.ReceiptNumber}: {reason} (${skippedReceipt.CalculatedAdvanceAmount:N2})");
                                    }
                                }

                                // Process only receipts with positive amounts
                                foreach (var receiptDetail in validReceiptDetails.Where(rd => rd.CalculatedAdvanceAmount > 0))
                                {
                                    var receipt = await _receiptService.GetReceiptByNumberAsync(receiptDetail.ReceiptNumber);
                                    if (receipt == null)
                                    {
                                        errors.Add($"Receipt {receiptDetail.ReceiptNumber} not found.");
                                        continue;
                                    }

                                    var allocation = new ReceiptPaymentAllocation
                                    {
                                        ReceiptId = receipt.ReceiptId,
                                        PaymentBatchId = createdBatch.PaymentBatchId,
                                        PaymentTypeId = paymentType.PaymentTypeId,
                                        PriceScheduleId = (int)receiptDetail.PriceRecordId,
                                        PricePerPound = receiptDetail.CalculatedAdvancePrice,
                                        QuantityPaid = receiptDetail.NetWeight,
                                        AmountPaid = receiptDetail.CalculatedAdvanceAmount,
                                        Status = "Pending", // Matches Draft batch status
                                        AllocatedAt = DateTime.Now
                                    };
                                    
                                    await _receiptService.CreateReceiptPaymentAllocationAsync(allocation, connection, transaction);
                                    allocationCount++;
                                }
                            }
                            
                            progress?.Report($"Created {allocationCount} receipt allocations");

                            // Commit the transaction - all or nothing!
                            transaction.Commit();
                            progress?.Report("All database operations committed successfully.");

                            bool overallSuccess = !errors.Any();
                            if (createdBatch != null)
                            {
                                LogAnEvent(EVT_TYPE_ADVANCE_DETERMINE_COMPLETE, 
                                    $"Sequence {sequenceNumber} confirmed draft created. Batch: {createdBatch.PaymentBatchId}, " +
                                    $"Allocations: {allocationCount}, Success: {overallSuccess}");
                            }

                            return (overallSuccess, errors, createdBatch, existingCalculation);
                        }
                        catch (Exception transactionEx)
                        {
                            transaction.Rollback();
                            errors.Add($"Transaction rolled back: {transactionEx.Message}");
                            Logger.Error("Payment draft creation failed, all changes rolled back", transactionEx);
                            return (false, errors, null, existingCalculation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Critical error: {ex.Message}");
                Logger.Error($"Error creating confirmed draft for Sequence {sequenceNumber}", ex);
                return (false, errors, null, existingCalculation);
            }
        }

        // --- Actual Payment Run ---
        public async Task<(bool Success, List<string> Errors, PaymentBatch? CreatedBatch)> ProcessAdvancePaymentRunAsync(
            int sequenceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            // Updated signature to accept lists
            List<int>? excludeGrowerIds = null,
            List<string>? excludePayGroupIds = null,
            List<int>? productIds = null,
            List<int>? processIds = null,
            IProgress<string>? progress = null)
        {
            var errors = new List<string>();
            PaymentBatch? createdBatch = null;
            bool overallSuccess = false;

            try
            {
                progress?.Report("Starting payment run...");
                LogAnEvent(EVT_TYPE_START_ADVANCE_DETERMINE, $"Sequence {sequenceNumber} determination started.");

                // 1. Create a new Payment Batch record
                // Get payment type by sequence number to get the correct PaymentTypeId
                var paymentType = await GetPaymentTypeBySequenceNumberAsync(sequenceNumber);
                if (paymentType == null)
                {
                    errors.Add($"Payment type with sequence number {sequenceNumber} not found.");
                    return (false, errors, null);
                }
                
                createdBatch = await _paymentBatchService.CreatePaymentBatchAsync(
                    paymentType.PaymentTypeId, 
                    paymentDate, 
                    cropYear,
                    $"Sequence {sequenceNumber} Payment Run");
                progress?.Report($"Created Payment Batch: {createdBatch.PaymentBatchId}");

                // 2. Perform Calculation (using the refactored method)
                progress?.Report("Calculating payment details...");
                var parameters = new TestRunInputParameters
                {
                    SequenceNumber = sequenceNumber,
                    PaymentDate = paymentDate, // Use actual payment date for consistency? Or cutoff? Using paymentDate for now.
                    CutoffDate = cutoffDate,
                    CropYear = cropYear,
                    ExcludeGrowerIds = excludeGrowerIds,
                    ExcludePayGroupIds = excludePayGroupIds,
                    ProductIds = productIds,
                    ProcessIds = processIds
                };
                var calculationResult = await CalculateAdvancePaymentDetailsAsync(parameters, progress);

                // Add calculation errors to the main error list
                errors.AddRange(calculationResult.GeneralErrors);
                foreach(var gp in calculationResult.GrowerPayments.Where(g => g.HasErrors))
                {
                    errors.AddRange(gp.ErrorMessages.Select(e => $"Grower {gp.GrowerNumber}: {e}"));
                }

                if (!calculationResult.GrowerPayments.Any())
                {
                    progress?.Report("No eligible growers/receipts found after calculation.");
                    LogAnEvent(EVT_TYPE_ADVANCE_NO_RECEIPTS, $"No eligible receipts/growers for Sequence {sequenceNumber}.");
                    // Still considered a success if no errors occurred during calculation attempt
                    overallSuccess = !errors.Any();
                    return (overallSuccess, errors, createdBatch);
                }

                // 3. Process Database Updates based on Calculation Results
                progress?.Report("Applying calculated payments to database...");
                int growerCount = 0;
                int totalGrowers = calculationResult.GrowerPayments.Count;

                foreach (var growerPayment in calculationResult.GrowerPayments)
                {
                    growerCount++;
                    var growerNumber = growerPayment.GrowerNumber;
                    var growerAccountEntries = new List<Account>();
                    bool growerDbSuccess = true; // Track DB success separately for this grower

                    // Skip grower if they had calculation errors preventing DB updates
                    // (e.g., if grower wasn't found, they wouldn't be in this list)
                    // We might still process receipts that *were* calculated successfully for a grower,
                    // even if other receipts for the same grower had errors.

                    progress?.Report($"Applying updates for Grower {growerNumber} ({growerCount}/{totalGrowers})...");

                    // Get Grower details again (needed for CreateAccountEntry, maybe optimize later)
                    var grower = await _growerService.GetGrowerByNumberAsync(growerNumber);
                    if (grower == null)
                    {
                        // This shouldn't happen if calculation succeeded, but check defensively
                        errors.Add($"Grower {growerNumber} not found during DB update phase. Skipping.");
                        continue;
                    }

                    // Filter out zero amount receipts and log skipped ones
                    var validReceiptDetails = growerPayment.ReceiptDetails
                        .Where(rd => string.IsNullOrEmpty(rd.ErrorMessage))
                        .ToList();

                    // Log skipped receipts with zero amounts
                    var skippedReceipts = validReceiptDetails
                        .Where(rd => rd.CalculatedAdvanceAmount <= 0)
                        .ToList();

                    if (skippedReceipts.Any())
                    {
                        var skippedCount = skippedReceipts.Count;
                        var skippedAmount = skippedReceipts.Sum(rd => rd.CalculatedAdvanceAmount);
                        progress?.Report($"⚠️ Skipping {skippedCount} receipts with zero/negative amounts for Grower {growerNumber} (${skippedAmount:N2})");
                        
                        foreach (var skippedReceipt in skippedReceipts)
                        {
                            var reason = skippedReceipt.CalculatedAdvanceAmount == 0 
                                ? "Zero calculated amount" 
                                : "Negative calculated amount";
                            
                            Logger.Info($"Skipped Receipt {skippedReceipt.ReceiptNumber} for Grower {growerNumber} - {reason} (${skippedReceipt.CalculatedAdvanceAmount:N2})");
                            progress?.Report($"  • Receipt {skippedReceipt.ReceiptNumber}: {reason} (${skippedReceipt.CalculatedAdvanceAmount:N2})");
                        }
                    }

                    // Process only receipts with positive amounts
                    foreach (var receiptDetail in validReceiptDetails.Where(rd => rd.CalculatedAdvanceAmount > 0))
                    {
                        try
                        {
                            // Get the full receipt to access ReceiptId and other properties
                            var receipt = await _receiptService.GetReceiptByNumberAsync(receiptDetail.ReceiptNumber);
                            if (receipt == null)
                            {
                                errors.Add($"Receipt {receiptDetail.ReceiptNumber} not found during payment processing.");
                                growerDbSuccess = false;
                                continue;
                            }

                            // Create payment allocation record (MODERN APPROACH)
                            var allocation = new ReceiptPaymentAllocation
                            {
                                ReceiptId = receipt.ReceiptId,
                                PaymentBatchId = createdBatch.PaymentBatchId,
                                PaymentTypeId = paymentType.PaymentTypeId, // Use the correct PaymentTypeId from sequence
                                PriceScheduleId = (int)receiptDetail.PriceRecordId, // Price schedule used for this payment
                                PricePerPound = receiptDetail.CalculatedAdvancePrice,
                                QuantityPaid = receiptDetail.NetWeight,
                                AmountPaid = receiptDetail.CalculatedAdvanceAmount,
                                AllocatedAt = DateTime.Now
                            };
                            
                            await _receiptService.CreateReceiptPaymentAllocationAsync(allocation);
                            progress?.Report($"Created allocation for Receipt {receiptDetail.ReceiptNumber}");

                            // Create Account entry for the advance payment
                            if (receiptDetail.CalculatedAdvanceAmount > 0) // Use calculated amount
                            {
                                growerAccountEntries.Add(CreateAccountEntry(
                                    receipt, // Use the full receipt object
                                    GetAdvanceAccountType(paymentType.SequenceNumber),
                                    receiptDetail.CalculatedAdvancePrice, // Use calculated price
                                    grower.CurrencyCode ?? "CAD",
                                    cropYear,
                                    createdBatch.PaymentBatchId,
                                    paymentDate));
                            }

                            // Create Account entries for premium and deduction (only on first payment)
                            if (paymentType.SequenceNumber == 1)
                            {
                                if (receiptDetail.CalculatedPremiumAmount > 0)
                                {
                                     growerAccountEntries.Add(CreateAccountEntry(
                                        receipt,
                                        AccTypePremium,
                                        receiptDetail.CalculatedPremiumPrice, // Use calculated price
                                        grower.CurrencyCode ?? "CAD",
                                        cropYear,
                                        createdBatch.PaymentBatchId,
                                        paymentDate));
                                }
                                if (receiptDetail.CalculatedDeductionAmount != 0) // Use calculated amount
                                {
                                     growerAccountEntries.Add(CreateAccountEntry(
                                        receipt,
                                        AccTypeDeduction,
                                        receiptDetail.CalculatedMarketingDeduction, // Use calculated rate
                                        grower.CurrencyCode ?? "CAD",
                                        cropYear,
                                        createdBatch.PaymentBatchId,
                                        paymentDate));
                                }
                            }

                            // Mark the price record advance as used
                            await _priceService.MarkAdvancePriceAsUsedAsync(receiptDetail.PriceRecordId, paymentType.SequenceNumber);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error applying DB update for Receipt {receiptDetail.ReceiptNumber} (Grower {growerNumber}): {ex.Message}");
                            growerDbSuccess = false;
                            Logger.Error($"Error applying DB update for Receipt {receiptDetail.ReceiptNumber} (Grower {growerNumber})", ex);
                        }
                    } // End foreach receiptDetail

                    // Save Account entries for the grower (if DB updates were successful so far for this grower)
                    if (growerDbSuccess && growerAccountEntries.Any())
                    {
                        bool accountSaveSuccess = await _accountService.CreatePaymentAccountEntriesAsync(growerAccountEntries);
                        if (!accountSaveSuccess)
                        {
                            errors.Add($"Failed to save account entries for Grower {growerNumber}.");
                            growerDbSuccess = false; // Mark grower as failed if account save fails
                            LogAnEvent(EVT_TYPE_ADVANCE_ACCOUNT_SAVE_FAIL, $"Failed account save for Grower {growerNumber}, Batch {createdBatch.PaymentBatchId}.");
                        }
                    }
                    else if (!growerDbSuccess)
                    {
                         LogAnEvent(EVT_TYPE_ADVANCE_GROWER_FAIL, $"Skipped/Failed account save for Grower {growerNumber} due to previous DB errors, Batch {createdBatch.PaymentBatchId}.");
                    }
                    // If growerDbSuccess is false here, it means some part of the DB update failed for this grower.

                } // End foreach growerPayment

                // Update batch with final totals
                if (createdBatch != null)
                {
                    decimal totalAmount = calculationResult.GrowerPayments.Sum(gp => gp.TotalCalculatedPayment);
                    int totalGrowersProcessed = calculationResult.GrowerPayments.Count;
                    int totalReceiptsProcessed = calculationResult.GrowerPayments.Sum(gp => gp.ReceiptCount);

                    await _paymentBatchService.UpdatePaymentBatchTotalsAsync(
                        createdBatch.PaymentBatchId,
                        totalGrowersProcessed,
                        totalReceiptsProcessed,
                        totalAmount);

                    // Mark batch as processed
                    await _paymentBatchService.MarkBatchAsProcessedAsync(
                        createdBatch.PaymentBatchId, 
                        "System"); // TODO: Get actual user from context
                    progress?.Report($"Batch {createdBatch.PaymentBatchId} marked as processed.");
                }

                overallSuccess = !errors.Any(); // Overall success depends on *any* errors occurring (calc or DB)
                progress?.Report($"Payment run database updates complete. Success: {overallSuccess}");
                if (createdBatch != null)
                {
                    LogAnEvent(EVT_TYPE_ADVANCE_DETERMINE_COMPLETE, $"Sequence {sequenceNumber} determination complete. Batch: {createdBatch.PaymentBatchId}, Success: {overallSuccess}, Errors: {errors.Count}");
                }

            }
            catch (Exception ex)
            {
                errors.Add($"Critical error during payment run: {ex.Message}");
                Logger.Error($"Critical error during payment run Sequence {sequenceNumber}", ex);
                overallSuccess = false;
                // Consider rollback logic if needed (e.g., delete PostBatch record?)
            }

            return (overallSuccess, errors, createdBatch);
        }


        // --- Test Payment Run ---
        public async Task<TestRunResult> PerformAdvancePaymentTestRunAsync(
            int sequenceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            List<int>? excludeGrowerIds = null,
            List<string>? excludePayGroupIds = null,
            List<int>? productIds = null,
            List<int>? processIds = null,
            IProgress<string>? progress = null)
        {
            var parameters = new TestRunInputParameters
            {
                SequenceNumber = sequenceNumber,
                PaymentDate = paymentDate,
                CutoffDate = cutoffDate,
                CropYear = cropYear,
                ExcludeGrowerIds = excludeGrowerIds ?? new List<int>(),
                ExcludePayGroupIds = excludePayGroupIds,
                ProductIds = productIds,
                ProcessIds = processIds
                // TODO: Populate description lists if needed/possible here
            };

            // Call the core calculation logic
            return await CalculateAdvancePaymentDetailsAsync(parameters, progress);
        }


        // --- Core Calculation Logic (Used by both Actual and Test Run) ---
        private async Task<TestRunResult> CalculateAdvancePaymentDetailsAsync(
            TestRunInputParameters parameters,
            IProgress<string>? progress = null)
        {
            var result = new TestRunResult { InputParameters = parameters };
            var generalErrors = result.GeneralErrors; // Shortcut for adding errors
            var growerPayments = result.GrowerPayments; // Shortcut for adding grower results

            try
            {
                progress?.Report("Starting calculation simulation...");

                // 1. Get eligible receipts (Moved from ProcessAdvancePaymentRunAsync)
                progress?.Report("Fetching eligible receipts for simulation...");
                
                // Convert exclude logic to include logic for grower filtering
                // If excludeGrowerIds is empty, it means all growers are selected (include all)
                // If excludeGrowerIds has values, it means specific growers are excluded (include the rest)
                List<int>? includeGrowerIds = null;
                if (parameters.ExcludeGrowerIds?.Any() == true)
                {
                    // Get all growers and exclude the ones in the exclude list
                    var allGrowers = await _growerService.GetAllGrowersAsync();
                    includeGrowerIds = allGrowers
                        .Where(g => !parameters.ExcludeGrowerIds.Contains(g.GrowerId))
                        .Select(g => g.GrowerId)
                        .ToList();
                }
                
                var eligibleReceipts = await _receiptService.GetReceiptsForAdvancePaymentAsync(
                    parameters.SequenceNumber,
                    parameters.CutoffDate,
                    includeGrowerIds, // Use calculated include list
                    null, // includePayGroupIds - not used in current implementation
                    null, // excludeGrowerIds - now handled by includeGrowerIds
                    parameters.ExcludePayGroupIds,
                    parameters.ProductIds,
                    parameters.ProcessIds,
                    parameters.CropYear);

                if (!eligibleReceipts.Any())
                {
                    progress?.Report("No eligible receipts found for this simulation.");
                    // No error, just no results
                    return result;
                }
                progress?.Report($"Found {eligibleReceipts.Count} eligible receipts for simulation.");

                // 2. Group receipts by GrowerID (Modern DB)
                var receiptsByGrower = eligibleReceipts.GroupBy(r => r.GrowerId);

                // 3. Process each grower (Logic to be moved next)
                int growerCount = 0;
                int totalGrowers = receiptsByGrower.Count();

                // 3. Process each grower (Moved from ProcessAdvancePaymentRunAsync)
                foreach (var growerGroup in receiptsByGrower)
                {
                    growerCount++;
                    var growerId = growerGroup.Key;
                    var growerReceipts = growerGroup.ToList();

                    var currentGrowerPayment = new TestRunGrowerPayment
                    {
                        GrowerId = growerId,
                        // Optionally: GrowerNumber = ... (if needed for reporting)
                        // GrowerName, Currency, IsOnHold will be populated below
                    };

                    progress?.Report($"Simulating GrowerID {growerId} ({growerCount}/{totalGrowers})...");

                    // Get Grower details (assume new method GetGrowerByIdAsync exists)
                    var grower = await _growerService.GetGrowerByIdAsync(growerId);
                    if (grower == null)
                    {
                        // Log error for this grower in the result, but continue processing others
                        generalErrors.Add($"GrowerID {growerId} not found during simulation. Skipping their receipts.");
                        // We don't add this grower to growerPayments list if they aren't found
                        continue; // Skip this grower
                    }
                    // Populate from Grower object
                    currentGrowerPayment.GrowerNumber = grower.GrowerNumber ?? string.Empty;
                    currentGrowerPayment.GrowerName = grower.GrowerName ?? string.Empty;
                    currentGrowerPayment.Currency = grower.CurrencyCode ?? "CAD";
                    currentGrowerPayment.IsOnHold = grower.IsOnHold;

                    // SIMPLIFIED: Skip processing entirely if grower is on hold
                    if (grower.IsOnHold)
                    {
                        progress?.Report($"GrowerID {growerId} ({grower.GrowerName}) is ON HOLD - skipping all receipts.");
                        continue; // Skip to next grower - don't add to results at all
                    }

                    // 4. Process each receipt for the grower (Moved from ProcessAdvancePaymentRunAsync)
                    foreach (var receipt in growerReceipts)
                    {
                        var receiptDetail = new TestRunReceiptDetail
                        {
                            ReceiptNumber = decimal.TryParse(receipt.ReceiptNumber, out var num) ? num : 0,
                            ReceiptDate = receipt.ReceiptDate,
                            Product = receipt.Product ?? string.Empty,
                            Process = receipt.Process ?? string.Empty,
                            Grade = receipt.Grade.ToString(),
                            NetWeight = receipt.Net
                        };

                        try
                        {
                            // Fix #4: Determine if this is a Fresh process (PROC_CLASS = 1)
                            receiptDetail.IsFresh = await _processClassificationService.IsFreshProcessAsync(receipt.Process ?? string.Empty);
                            receiptDetail.ProcessClass = await _processClassificationService.GetProcessClassAsync(receipt.Process ?? string.Empty);

                            decimal calculatedAdvancePrice = 0;
                            decimal premiumPrice = 0;
                            decimal marketingDeduction = 0;
                            decimal priceScheduleId = 0;

                            // Find the relevant price record ID first
                            priceScheduleId = await _priceService.FindPriceRecordIdAsync(receipt.Product ?? string.Empty, receipt.Process ?? string.Empty, receipt.ReceiptDate);
                            if (priceScheduleId == 0)
                            {
                                receiptDetail.ErrorMessage = $"No price record found (Product: {receipt.Product}, Process: {receipt.Process}, Date: {receipt.ReceiptDate.ToShortDateString()}).";
                                // Don't add here - it will be added in the finally block to avoid duplicate counting
                                continue; // Skip calculation for this receipt
                            }
                            receiptDetail.PriceRecordId = priceScheduleId;

                            // Fix #5: Calculate running cumulative price using max() logic
                            // Get the running cumulative price up to current advance
                            // Mirrors legacy: RunAdvPrice(n) which uses max() to prevent backward pricing
                            var currentRunningPrice = await GetRunningAdvancePriceAsync(
                                receipt.Product ?? string.Empty,
                                receipt.Process ?? string.Empty,
                                receipt.ReceiptDate,
                                parameters.SequenceNumber,
                                (grower.CurrencyCode ?? "CAD")[0],
                                grower.PriceLevel,
                                receipt.Grade,
                                priceScheduleId);

                            // ================================================================
                            // GENERIC CALCULATION LOGIC - SUPPORTS UNLIMITED PAYMENT TYPES!
                            // ================================================================
                            // Calculate what has already been paid from ReceiptPaymentAllocations
                            // This replaces the hardcoded if/else chains for advances 1, 2, 3
                            // Now works for any payment type based on sequence number!
                            
                            decimal alreadyPaid = 0;
                            
                            // Get payment type to determine if this is the first payment in sequence
                            var paymentType = await GetPaymentTypeBySequenceNumberAsync(parameters.SequenceNumber);
                            if (paymentType == null)
                            {
                                receiptDetail.ErrorMessage = $"Payment type with sequence number {parameters.SequenceNumber} not found.";
                                continue;
                            }
                            
                            if (paymentType.SequenceNumber == 1)
                            {
                                // First payment in sequence - nothing previously paid
                                calculatedAdvancePrice = currentRunningPrice;
                                
                                // Premium and deduction only on first payment
                                premiumPrice = await _priceService.GetTimePremiumAsync(
                                    receipt.Product ?? string.Empty,
                                    receipt.Process ?? string.Empty,
                                    receipt.ReceiptDate,
                                    receipt.ReceiptDate.TimeOfDay,
                                    (grower.CurrencyCode ?? "CAD")[0]);
                                    
                                marketingDeduction = await _priceService.GetMarketingDeductionAsync(receipt.Product ?? string.Empty);
                            }
                            else
                            {
                                // Subsequent payments (2, 3, 4, 5, ..., FINAL) - calculate incremental payment
                                // Get cumulative price per pound already paid from ReceiptPaymentAllocations
                                alreadyPaid = await GetAlreadyPaidCumulativePriceAsync(receipt.ReceiptId, parameters.SequenceNumber);
                                
                                // Calculate incremental payment: Current cumulative - Already paid
                                // The currentRunningPrice already has max() protection from GetRunningAdvancePriceAsync
                                // So we just subtract what was already paid
                                calculatedAdvancePrice = currentRunningPrice - alreadyPaid;
                            }

                            // Ensure calculated price isn't negative (defensive programming)
                            calculatedAdvancePrice = Math.Max(0, calculatedAdvancePrice);

                            // Store calculated values in the receipt detail
                            receiptDetail.CalculatedAdvancePrice = calculatedAdvancePrice;
                            receiptDetail.CalculatedPremiumPrice = (paymentType.SequenceNumber == 1) ? premiumPrice : 0; // Only on first payment
                            receiptDetail.CalculatedMarketingDeduction = (paymentType.SequenceNumber == 1) ? marketingDeduction : 0; // Only on first payment

                            // Fix #6: Use RoundMoney for AwayFromZero rounding (matches legacy XBase round())
                            receiptDetail.CalculatedAdvanceAmount = RoundMoney(receipt.Net * receiptDetail.CalculatedAdvancePrice);
                            receiptDetail.CalculatedPremiumAmount = RoundMoney(receipt.Net * receiptDetail.CalculatedPremiumPrice);
                            receiptDetail.CalculatedDeductionAmount = RoundMoney(receipt.Net * receiptDetail.CalculatedMarketingDeduction);

                        }
                        catch (Exception ex)
                        {
                            receiptDetail.ErrorMessage = $"Calculation error: {ex.Message}";
                            Logger.Error($"Error simulating Receipt {receipt.ReceiptNumber} for GrowerId {growerId}", ex);
                        }
                        finally
                        {
                            currentGrowerPayment.ReceiptDetails.Add(receiptDetail);
                        }
                    } // End foreach receipt

                    // Add the processed grower payment details to the main result list
                    growerPayments.Add(currentGrowerPayment);

                } // End foreach grower

                progress?.Report($"Calculation simulation complete. Processed {growerPayments.Count}/{totalGrowers} growers found.");

            }
            catch (Exception ex)
            {
                generalErrors.Add($"Critical error during calculation simulation: {ex.Message}");
                Logger.Error($"Critical error during calculation simulation Sequence {parameters.SequenceNumber}", ex);
                // No rollback needed as it's just calculation
            }

            return result;
        }


        // --- Helper Methods ---

        // Helper method to create an Account entry (Only used by Actual Run now)
        private Account CreateAccountEntry(Receipt receipt, string accountType, decimal unitPrice, string currency, int year, decimal batchId, DateTime entryDate)
        {
            // Calculate dollars and potentially GST
            // Fix #6: Use RoundMoney for AwayFromZero rounding (matches legacy)
            decimal dollars = RoundMoney(receipt.Net * unitPrice);
            decimal gstEst = 0; // TODO: Implement GST calculation based on grower.ChgGst and product settings

            return new Account
            {
                Number = receipt.GrowerNumber ?? string.Empty,
                Date = entryDate, // Use the payment run date
                Type = accountType,
                // Class = ???, // Determine Class if needed
                Product = receipt.Product ?? string.Empty,
                Process = receipt.Process ?? string.Empty,
                Grade = receipt.Grade,
                Lbs = receipt.Net,
                UnitPrice = unitPrice,
                Dollars = dollars,
                // Description = ???, // Set description if needed
                Year = year,
                AcctUnique = 0, // Need a way to generate unique IDs
                Currency = currency,
                // ChgGst = ???, // Based on grower/product
                // GstRate = ???, // Based on tax rules
                GstEst = gstEst,
                // NonGstEst = ???, // Calculate if needed
                AdvNo = GetAdvanceNumberFromType(accountType), // Helper needed
                AdvBat = batchId,
                FinBat = 0 // Not final batch
                // Set other fields like QaddOp etc. if not handled by INSERT trigger/default
            };
        }

        /// <summary>
        /// Gets the advance number from an account type code.
        /// SUPPORTS UNLIMITED ADVANCES: Parses any "ADVn" format (ADV1, ADV2, ADV3, ADV4, ...)
        /// </summary>
        /// <param name="accountType">Account type code (e.g., "ADV1", "ADV2", "ADV4")</param>
        /// <returns>Advance number, or 0 if not an advance payment type</returns>
        private decimal GetAdvanceNumberFromType(string accountType)
        {
            // Handle dynamic advance types: ADV1, ADV2, ADV3, ADV4, ADV5, etc.
            if (accountType?.StartsWith("ADV", StringComparison.OrdinalIgnoreCase) == true && accountType.Length > 3)
            {
                if (int.TryParse(accountType.Substring(3), out int advNum))
                {
                    return advNum;
                }
            }
            return 0; // Not an advance payment type
        }

        /// <summary>
        /// Gets the account type code for a given advance number.
        /// SUPPORTS UNLIMITED ADVANCES: Generates "ADVn" format dynamically (ADV1, ADV2, ADV3, ADV4, ...)
        /// </summary>
        /// <param name="advanceNumber">Advance number (1, 2, 3, 4, 5, ...)</param>
        /// <returns>Account type code (e.g., "ADV1", "ADV2", "ADV4")</returns>
        /// <exception cref="ArgumentOutOfRangeException">If advance number is less than 1</exception>
        private string GetAdvanceAccountType(int advanceNumber)
        {
            if (advanceNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(advanceNumber), 
                    "Advance number must be 1 or greater");
            }
            
            // Dynamically generate account type: ADV1, ADV2, ADV3, ADV4, ADV5, ...
            return $"ADV{advanceNumber}";
        }

        /// <summary>
        /// Calculates the cumulative running advance price up to the specified advance number.
        /// Uses max() to ensure the price never goes backward (mirrors legacy RunAdvPrice).
        /// This prevents negative advance payments when price table has decreasing values.
        /// 
        /// SUPPORTS UNLIMITED ADVANCES: Works for any advance number (1, 2, 3, 4, 5, ...)
        /// </summary>
        /// <param name="product">Product code</param>
        /// <param name="process">Process code</param>
        /// <param name="receiptDate">Receipt date</param>
        /// <param name="upToSequenceNumber">Calculate cumulative price up to this sequence number</param>
        /// <param name="currency">Grower currency (C/U)</param>
        /// <param name="priceLevel">Grower price level (1-3)</param>
        /// <param name="grade">Receipt grade</param>
        /// <param name="priceScheduleId">priceScheduledId from PriceDetails</param>
        /// <returns>Running cumulative maximum price</returns>
        private async Task<decimal> GetRunningAdvancePriceAsync(
            string product,
            string process,
            DateTime receiptDate,
            int upToSequenceNumber,
            char currency,
            int priceLevel,
            decimal grade,
            decimal priceScheduleId)
        {
            try
            {
                decimal runningMax = 0;

                // Get all payment types with sequence numbers up to the specified sequence
                var allPaymentTypes = await _paymentTypeService.GetAllPaymentTypesAsync();
                var relevantPaymentTypes = allPaymentTypes
                    .Where(pt => pt.SequenceNumber <= upToSequenceNumber && pt.IsActive)
                    .OrderBy(pt => pt.SequenceNumber)
                    .ToList();

                foreach (var paymentType in relevantPaymentTypes)
                {
                    var advPrice = await _priceService.GetAdvancePriceAsync(
                        product,
                        process,
                        receiptDate,
                        paymentType.SequenceNumber, // Use sequence number instead of hardcoded advance number
                        currency,
                        priceLevel,
                        grade,
                        priceScheduleId);

                    // Key: Use Math.Max to prevent backward pricing
                    // Mirrors legacy: nReturn := max( CurAdvPrice( n ), nReturn )
                    runningMax = Math.Max(advPrice, runningMax);
                }

                return runningMax;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating running advance price for sequence {upToSequenceNumber}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Rounds a monetary amount to 2 decimal places using AwayFromZero rounding.
        /// This matches the legacy XBase round() function behavior.
        /// Fix #6: Ensures consistent rounding between legacy and modern systems.
        /// </summary>
        /// <param name="amount">The amount to round</param>
        /// <returns>Amount rounded to 2 decimal places</returns>
        private static decimal RoundMoney(decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// [OBSOLETE - LEGACY METHOD]
        /// This method was used in the original hardcoded implementation for advances 1-3.
        /// It has been replaced by GetAlreadyPaidCumulativePriceAsync which queries
        /// ReceiptPaymentAllocations table directly and supports unlimited advances.
        /// 
        /// Kept for reference only. New code should use GetAlreadyPaidCumulativePriceAsync.
        /// </summary>
        [Obsolete("Use GetAlreadyPaidCumulativePriceAsync instead - supports unlimited advances")]
        private async Task<decimal> GetPreviousAdvancePrice(decimal receiptNumber, int advanceNumberToGet)
        {
            // LEGACY: This queried Daily table for ADV_PR1, ADV_PR2 columns
            // NEW: Use GetAlreadyPaidCumulativePriceAsync which queries ReceiptPaymentAllocations
            var receipt = await _receiptService.GetReceiptByNumberAsync(receiptNumber);
            if (receipt == null) return 0;

            // Hardcoded for advance 1-2 only (limited implementation)
            if (advanceNumberToGet == 1)
                return receipt.AdvPr1 ?? 0;
            if (advanceNumberToGet == 2)
                return receipt.AdvPr2 ?? 0;

            return 0; // Can't handle advance 3+
        }

        /// <summary>
        /// Gets the cumulative price per pound already paid for a receipt from previous payment sequences.
        /// Uses SequenceNumber instead of hardcoded advance numbers for flexible payment ordering.
        /// </summary>
        /// <param name="receiptId">The receipt ID to query</param>
        /// <param name="currentSequenceNumber">Current sequence number being processed</param>
        /// <returns>Sum of all prices per pound paid in previous payment sequences</returns>
        private async Task<decimal> GetAlreadyPaidCumulativePriceAsync(int receiptId, int currentSequenceNumber)
        {
            try
            {
                var allocations = await _receiptService.GetReceiptPaymentAllocationsAsync(receiptId);
                
                // Get all payment types with sequence numbers less than current
                var previousPaymentTypes = await GetPreviousPaymentSequencesAsync(currentSequenceNumber);
                var previousPaymentTypeIds = previousPaymentTypes.Select(pt => pt.PaymentTypeId).ToList();
                
                // Sum prices from previous payment sequences only
                return allocations
                    .Where(a => previousPaymentTypeIds.Contains(a.PaymentTypeId))
                    .Sum(a => a.PricePerPound);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting already paid cumulative price for Receipt {receiptId}, Sequence {currentSequenceNumber}: {ex.Message}", ex);
                return 0;
            }
        }

        /// <summary>
        /// Gets all payment types that have sequence numbers less than the specified sequence.
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

        // Placeholder for event logging - replace with actual implementation
        private void LogAnEvent(string eventType, string message)
        {
            Logger.Info($"Event: {eventType} - {message}");
            // Replace with call to a proper event logging service if available
        }
         // Placeholder constants for event types
        private const string EVT_TYPE_START_ADVANCE_DETERMINE = "ADV_START";
        private const string EVT_TYPE_ADVANCE_NO_RECEIPTS = "ADV_NORCPT";
        private const string EVT_TYPE_ADVANCE_GROWER_NOT_FOUND = "ADV_NOGROW";
        private const string EVT_TYPE_ADVANCE_ACCOUNT_SAVE_FAIL = "ADV_ACCTSAVEFAIL";
        private const string EVT_TYPE_ADVANCE_GROWER_FAIL = "ADV_GROWFAIL";
        private const string EVT_TYPE_ADVANCE_DETERMINE_COMPLETE = "ADV_COMPLETE";

        // ======================================================================
        // BATCH DETAIL METHODS
        // ======================================================================

        /// <summary>
        /// Gets grower-level payment summary for a specific payment batch
        /// </summary>
        public async Task<List<GrowerPaymentSummary>> GetGrowerPaymentsForBatchAsync(int batchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            g.GrowerId,
                            g.GrowerNumber,
                            g.FullName AS GrowerName,
                            COUNT(DISTINCT rpa.ReceiptId) AS ReceiptCount,
                            SUM(rpa.QuantityPaid) AS TotalWeight,
                            SUM(rpa.AmountPaid) AS TotalAmount,
                            ISNULL(CONCAT(cs.SeriesCode, '-', c.ChequeNumber), 'Not Generated') AS ChequeNumber,
                            g.IsOnHold,
                            g.PaymentMethodId,
                            ISNULL(pm.MethodName, 'Cheque') AS PaymentMethodName
                        FROM ReceiptPaymentAllocations rpa
                        INNER JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                        INNER JOIN Growers g ON r.GrowerId = g.GrowerId
                        LEFT JOIN PaymentMethods pm ON g.PaymentMethodId = pm.PaymentMethodId
                        LEFT JOIN Cheques c ON c.PaymentBatchId = rpa.PaymentBatchId AND c.GrowerId = g.GrowerId
                        LEFT JOIN ChequeSeries cs ON c.ChequeSeriesId = cs.ChequeSeriesId
                        WHERE rpa.PaymentBatchId = @BatchId 
                            AND (rpa.Status IS NULL OR rpa.Status != 'Voided')
                            AND (c.Status IS NULL OR c.Status != 'Voided')
                        GROUP BY 
                            g.GrowerId, g.GrowerNumber, g.FullName, g.IsOnHold, 
                            g.PaymentMethodId, pm.MethodName, cs.SeriesCode, c.ChequeNumber
                        ORDER BY g.GrowerNumber";

                    var parameters = new { BatchId = batchId };
                    var result = (await connection.QueryAsync<GrowerPaymentSummary>(sql, parameters)).ToList();

                    Logger.Info($"Retrieved {result.Count} grower payment summaries for batch {batchId}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting grower payments for batch {batchId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all receipt allocations for a specific payment batch with full details
        /// </summary>
        public async Task<List<ReceiptPaymentAllocation>> GetBatchAllocationsAsync(int batchId)
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
                            rpa.QuantityPaid,
                            rpa.AmountPaid,
                            rpa.AllocatedAt,
                            rpa.Status,
                            rpa.ModifiedAt,
                            rpa.ModifiedBy,
                            r.ReceiptNumber,
                            r.ReceiptDate,
                            r.FinalWeight,
                            r.Grade,
                            g.GrowerId,
                            g.FullName AS GrowerName,
                            g.GrowerNumber,
                            p.ProductName,
                            pr.ProcessName,
                            pc.ClassName AS PriceClassName,
                            pt.TypeName AS PaymentTypeName,
                            pb.BatchNumber
                        FROM ReceiptPaymentAllocations rpa
                        INNER JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                        INNER JOIN Growers g ON r.GrowerId = g.GrowerId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        LEFT JOIN Processes pr ON r.ProcessId = pr.ProcessId
                        LEFT JOIN PriceClasses pc ON r.PriceClassId = pc.PriceClassId
                        INNER JOIN PaymentTypes pt ON rpa.PaymentTypeId = pt.PaymentTypeId
                        INNER JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        WHERE rpa.PaymentBatchId = @BatchId
                          AND (rpa.Status IS NULL OR rpa.Status != 'Voided')
                        ORDER BY g.GrowerNumber, r.ReceiptDate, r.ReceiptNumber";

                    var parameters = new { BatchId = batchId };
                    var result = (await connection.QueryAsync<ReceiptPaymentAllocation>(sql, parameters)).ToList();

                    Logger.Info($"Retrieved {result.Count} receipt allocations for batch {batchId}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting batch allocations for batch {batchId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Calculate analytics for a payment batch
        /// </summary>
        public async Task<PaymentBatchAnalytics> CalculateBatchAnalyticsAsync(int batchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get grower payment summaries for analytics
                    var growerPayments = await GetGrowerPaymentsForBatchAsync(batchId);
                    
                    if (!growerPayments.Any())
                    {
                        Logger.Warn($"No grower payments found for batch {batchId}");
                        return new PaymentBatchAnalytics();
                    }

                    var analytics = new PaymentBatchAnalytics();

                    // Calculate basic statistics
                    var payments = growerPayments.Where(gp => gp.TotalAmount > 0).ToList();
                    
                    if (payments.Any())
                    {
                        analytics.AveragePaymentPerGrower = payments.Average(p => p.TotalAmount);
                        analytics.LargestPayment = payments.Max(p => p.TotalAmount);
                        analytics.TotalWeight = payments.Sum(p => p.TotalWeight);
                        
                        var minPayment = payments.Min(p => p.TotalAmount);
                        analytics.PaymentRange = $"${minPayment:N2} - ${analytics.LargestPayment:N2}";
                    }

                    // Calculate payment distribution buckets
                    analytics.PaymentDistribution = CalculatePaymentDistribution(payments);

                    // Calculate product breakdown
                    analytics.ProductBreakdown = await CalculateProductBreakdownAsync(batchId);

                    // Calculate payment range buckets
                    analytics.PaymentRangeBuckets = CalculatePaymentRangeBuckets(payments);

                    // Calculate anomalies
                    analytics.AnomalyCount = CalculateAnomalies(payments);

                    // Add comparison note
                    analytics.ComparisonNote = await GetComparisonNoteAsync(batchId, analytics);

                    Logger.Info($"Calculated analytics for batch {batchId}: {payments.Count} growers, ${analytics.AveragePaymentPerGrower:N2} avg payment");
                    return analytics;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating analytics for batch {batchId}: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Calculate payment distribution buckets for charting
        /// </summary>
        private List<PaymentDistributionBucket> CalculatePaymentDistribution(List<GrowerPaymentSummary> payments)
        {
            if (!payments.Any()) return new List<PaymentDistributionBucket>();

            var buckets = new List<PaymentDistributionBucket>();
            var totalAmount = payments.Sum(p => p.TotalAmount);

            // Define payment ranges
            var ranges = new[]
            {
                (0m, 100m, "Under $100"),
                (100m, 500m, "$100 - $500"),
                (500m, 1000m, "$500 - $1,000"),
                (1000m, 2500m, "$1,000 - $2,500"),
                (2500m, 5000m, "$2,500 - $5,000"),
                (5000m, decimal.MaxValue, "Over $5,000")
            };

            foreach (var (min, max, label) in ranges)
            {
                var bucketPayments = payments.Where(p => p.TotalAmount >= min && p.TotalAmount < max).ToList();
                if (bucketPayments.Any())
                {
                    buckets.Add(new PaymentDistributionBucket
                    {
                        Range = label,
                        GrowerCount = bucketPayments.Count,
                        TotalAmount = bucketPayments.Sum(p => p.TotalAmount),
                        Percentage = totalAmount > 0 ? (bucketPayments.Sum(p => p.TotalAmount) / totalAmount) * 100 : 0
                    });
                }
            }

            return buckets;
        }

        /// <summary>
        /// Calculate product breakdown for the batch
        /// </summary>
        private async Task<List<WPFGrowerApp.DataAccess.Models.ProductBreakdown>> CalculateProductBreakdownAsync(int batchId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT 
                            ISNULL(p.ProductName, 'Unknown Product') AS ProductName,
                            SUM(rpa.QuantityPaid) AS Weight,
                            SUM(rpa.AmountPaid) AS Amount,
                            COUNT(DISTINCT rpa.ReceiptId) AS ReceiptCount
                        FROM ReceiptPaymentAllocations rpa
                        INNER JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                        LEFT JOIN Products p ON r.ProductId = p.ProductId
                        WHERE rpa.PaymentBatchId = @BatchId
                          AND (rpa.Status IS NULL OR rpa.Status != 'Voided')
                        GROUP BY ISNULL(p.ProductName, 'Unknown Product')
                        ORDER BY Amount DESC";

                    var parameters = new { BatchId = batchId };
                    var result = (await connection.QueryAsync<WPFGrowerApp.DataAccess.Models.ProductBreakdown>(sql, parameters)).ToList();

                    // Calculate percentages
                    var totalAmount = result.Sum(r => r.Amount);
                    foreach (var item in result)
                    {
                        item.Percentage = totalAmount > 0 ? (item.Amount / totalAmount) * 100 : 0;
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating product breakdown for batch {batchId}: {ex.Message}", ex);
                return new List<WPFGrowerApp.DataAccess.Models.ProductBreakdown>();
            }
        }

        /// <summary>
        /// Calculate payment range buckets for grower distribution
        /// </summary>
        private List<PaymentRangeBucket> CalculatePaymentRangeBuckets(List<GrowerPaymentSummary> payments)
        {
            if (!payments.Any()) return new List<PaymentRangeBucket>();

            var buckets = new List<PaymentRangeBucket>();
            var totalGrowers = payments.Count;

            // Define grower count ranges
            var ranges = new[]
            {
                (0m, 100m, "$0 - $100"),
                (100m, 500m, "$100 - $500"),
                (500m, 1000m, "$500 - $1,000"),
                (1000m, 2500m, "$1,000 - $2,500"),
                (2500m, 5000m, "$2,500 - $5,000"),
                (5000m, decimal.MaxValue, "Over $5,000")
            };

            foreach (var (min, max, label) in ranges)
            {
                var count = payments.Count(p => p.TotalAmount >= min && p.TotalAmount < max);
                if (count > 0)
                {
                    buckets.Add(new PaymentRangeBucket
                    {
                        RangeLabel = label,
                        MinAmount = min,
                        MaxAmount = max == decimal.MaxValue ? decimal.MaxValue : max,
                        GrowerCount = count,
                        Percentage = (count / (decimal)totalGrowers) * 100
                    });
                }
            }

            return buckets;
        }

        /// <summary>
        /// Calculate anomalies in payment data
        /// </summary>
        private int CalculateAnomalies(List<GrowerPaymentSummary> payments)
        {
            if (payments.Count < 3) return 0; // Need at least 3 payments for statistical analysis

            var amounts = payments.Select(p => p.TotalAmount).ToList();
            var mean = amounts.Average();
            var standardDeviation = CalculateStandardDeviation(amounts, mean);
            var threshold = mean + (2 * standardDeviation); // 2 standard deviations

            return payments.Count(p => p.TotalAmount > threshold);
        }

        /// <summary>
        /// Calculate standard deviation
        /// </summary>
        private decimal CalculateStandardDeviation(List<decimal> values, decimal mean)
        {
            var variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
            return (decimal)Math.Sqrt((double)variance);
        }

        /// <summary>
        /// Get comparison note with previous batch
        /// </summary>
        private async Task<string> GetComparisonNoteAsync(int batchId, PaymentBatchAnalytics analytics)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var sql = @"
                        SELECT TOP 1 
                            BatchNumber,
                            TotalAmount,
                            COUNT(DISTINCT g.GrowerId) AS GrowerCount
                        FROM PaymentBatches pb
                        LEFT JOIN ReceiptPaymentAllocations rpa ON pb.PaymentBatchId = rpa.PaymentBatchId
                        LEFT JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                        LEFT JOIN Growers g ON r.GrowerId = g.GrowerId
                        WHERE pb.PaymentBatchId < @BatchId
                        GROUP BY pb.PaymentBatchId, pb.BatchNumber, pb.TotalAmount
                        ORDER BY pb.PaymentBatchId DESC";

                    var parameters = new { BatchId = batchId };
                    var prevBatch = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, parameters);

                    if (prevBatch != null)
                    {
                        decimal prevAmount = prevBatch.TotalAmount ?? 0m;
                        int prevGrowerCount = prevBatch.GrowerCount ?? 0;
                        string prevBatchNumber = prevBatch.BatchNumber ?? "";

                        var currentAmount = analytics.PaymentDistribution.Sum(p => p.TotalAmount);
                        var currentGrowerCount = analytics.PaymentDistribution.Sum(p => p.GrowerCount);

                        var amountChange = prevAmount > 0 ? ((currentAmount - prevAmount) / prevAmount) * 100 : 0;
                        var growerChange = prevGrowerCount > 0 ? ((currentGrowerCount - prevGrowerCount) / (decimal)prevGrowerCount) * 100 : 0;

                        return $"Compared to {prevBatchNumber}: " +
                               $"{(amountChange >= 0 ? "+" : "")}{amountChange:F1}% amount, " +
                               $"{(growerChange >= 0 ? "+" : "")}{growerChange:F1}% growers";
                    }

                    return "No previous batch data available for comparison";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting comparison note for batch {batchId}: {ex.Message}", ex);
                return "Unable to compare with previous batches";
            }
        }

    }
}
