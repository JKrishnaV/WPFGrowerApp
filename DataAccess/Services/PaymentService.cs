using System;
using System;
using System.Collections.Generic;
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
        private readonly IPostBatchService _postBatchService;
        private readonly IGrowerService _growerService; // Needed for grower details like currency, GST status
        private readonly IProcessClassificationService _processClassificationService; // For Fresh vs Non-Fresh tracking

        // Constants for Account Types (mirroring TT_ values from XBase++)
        private const string AccTypeAdvance1 = "ADV1"; // Placeholder - Use actual code
        private const string AccTypeAdvance2 = "ADV2"; // Placeholder
        private const string AccTypeAdvance3 = "ADV3"; // Placeholder
        private const string AccTypeDeduction = "DED";  // Placeholder
        private const string AccTypePremium = "PREM"; // Placeholder
        // Add other types as needed (e.g., Loan payments, Equity)

        public PaymentService(
            IReceiptService receiptService,
            IPriceService priceService,
            IAccountService accountService,
            IPostBatchService postBatchService,
            IGrowerService growerService,
            IProcessClassificationService processClassificationService)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _postBatchService = postBatchService ?? throw new ArgumentNullException(nameof(postBatchService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _processClassificationService = processClassificationService ?? throw new ArgumentNullException(nameof(processClassificationService));
        }

        // --- Actual Payment Run ---
        public async Task<(bool Success, List<string> Errors, PostBatch CreatedBatch)> ProcessAdvancePaymentRunAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            // Updated signature to accept lists
            List<decimal> excludeGrowerIds = null,
            List<string> excludePayGroupIds = null,
            List<string> productIds = null,
            List<string> processIds = null,
            IProgress<string> progress = null)
        {
            var errors = new List<string>();
            PostBatch createdBatch = null;
            bool overallSuccess = false;

            try
            {
                progress?.Report("Starting payment run...");
                LogAnEvent(EVT_TYPE_START_ADVANCE_DETERMINE, $"Advance {advanceNumber} determination started."); // Example logging

                // 1. Create a new Post Batch record
                var postBatchId = await _postBatchService.GetNextPostBatchIdAsync();
                string postType = $"ADV {advanceNumber}"; // Example post type
                createdBatch = await _postBatchService.CreatePostBatchAsync(paymentDate, cutoffDate, postType);
                progress?.Report($"Created Post Batch: {createdBatch.PostBat}");

                // 2. Perform Calculation (using the refactored method)
                progress?.Report("Calculating payment details...");
                var parameters = new TestRunInputParameters
                {
                    AdvanceNumber = advanceNumber,
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
                    LogAnEvent(EVT_TYPE_ADVANCE_NO_RECEIPTS, $"No eligible receipts/growers for Advance {advanceNumber}.");
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

                    // Process each successfully calculated receipt detail for DB updates
                    foreach (var receiptDetail in growerPayment.ReceiptDetails.Where(rd => string.IsNullOrEmpty(rd.ErrorMessage)))
                    {
                        try
                        {
                            // Create Account entry for the advance payment
                            if (receiptDetail.CalculatedAdvanceAmount > 0) // Use calculated amount
                            {
                                growerAccountEntries.Add(CreateAccountEntry(
                                    // Need the original Receipt object or its relevant fields
                                    new Receipt { GrowerNumber = growerNumber, Net = receiptDetail.NetWeight, Product = receiptDetail.Product, Process = receiptDetail.Process, Grade = Convert.ToDecimal(receiptDetail.Grade) }, // Reconstruct minimal Receipt
                                    GetAdvanceAccountType(advanceNumber),
                                    receiptDetail.CalculatedAdvancePrice, // Use calculated price
                                    grower.Currency.ToString(),
                                    cropYear,
                                    createdBatch.PostBat,
                                    paymentDate));
                            }

                            // Create Account entries for premium and deduction (only on first advance)
                            if (advanceNumber == 1)
                            {
                                if (receiptDetail.CalculatedPremiumAmount > 0)
                                {
                                     growerAccountEntries.Add(CreateAccountEntry(
                                        new Receipt { GrowerNumber = growerNumber, Net = receiptDetail.NetWeight, Product = receiptDetail.Product, Process = receiptDetail.Process, Grade = Convert.ToDecimal(receiptDetail.Grade) },
                                        AccTypePremium,
                                        receiptDetail.CalculatedPremiumPrice, // Use calculated price
                                        grower.Currency.ToString(),
                                        cropYear,
                                        createdBatch.PostBat,
                                        paymentDate));
                                }
                                if (receiptDetail.CalculatedDeductionAmount != 0) // Use calculated amount
                                {
                                     growerAccountEntries.Add(CreateAccountEntry(
                                        new Receipt { GrowerNumber = growerNumber, Net = receiptDetail.NetWeight, Product = receiptDetail.Product, Process = receiptDetail.Process, Grade = Convert.ToDecimal(receiptDetail.Grade) },
                                        AccTypeDeduction,
                                        receiptDetail.CalculatedMarketingDeduction, // Use calculated rate
                                        grower.Currency.ToString(),
                                        cropYear,
                                        createdBatch.PostBat,
                                        paymentDate));
                                }
                            }

                            // Update the Daily record with calculated details and batch ID
                            bool updateSuccess = await _receiptService.UpdateReceiptAdvanceDetailsAsync(
                                receiptDetail.ReceiptNumber,
                                advanceNumber,
                                createdBatch.PostBat,
                                receiptDetail.CalculatedAdvancePrice, // Use calculated price
                                receiptDetail.PriceRecordId,          // Use price record ID from calculation
                                receiptDetail.CalculatedPremiumPrice); // Use calculated premium price

                            if (!updateSuccess)
                            {
                                errors.Add($"Failed to update advance details for Receipt {receiptDetail.ReceiptNumber}.");
                                growerDbSuccess = false;
                            }
                            else
                            {
                                // Mark the price record advance as used (only if update succeeded)
                                await _priceService.MarkAdvancePriceAsUsedAsync(receiptDetail.PriceRecordId, advanceNumber);
                            }
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
                            LogAnEvent(EVT_TYPE_ADVANCE_ACCOUNT_SAVE_FAIL, $"Failed account save for Grower {growerNumber}, Batch {createdBatch.PostBat}.");
                        }
                    }
                    else if (!growerDbSuccess)
                    {
                         LogAnEvent(EVT_TYPE_ADVANCE_GROWER_FAIL, $"Skipped/Failed account save for Grower {growerNumber} due to previous DB errors, Batch {createdBatch.PostBat}.");
                    }
                    // If growerDbSuccess is false here, it means some part of the DB update failed for this grower.

                } // End foreach growerPayment

                overallSuccess = !errors.Any(); // Overall success depends on *any* errors occurring (calc or DB)
                progress?.Report($"Payment run database updates complete. Success: {overallSuccess}");
                LogAnEvent(EVT_TYPE_ADVANCE_DETERMINE_COMPLETE, $"Advance {advanceNumber} determination complete. Batch: {createdBatch.PostBat}, Success: {overallSuccess}, Errors: {errors.Count}");

            }
            catch (Exception ex)
            {
                errors.Add($"Critical error during payment run: {ex.Message}");
                Logger.Error($"Critical error during payment run Advance {advanceNumber}", ex);
                overallSuccess = false;
                // Consider rollback logic if needed (e.g., delete PostBatch record?)
            }

            return (overallSuccess, errors, createdBatch);
        }


        // --- Test Payment Run ---
        public async Task<TestRunResult> PerformAdvancePaymentTestRunAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            List<decimal> excludeGrowerIds = null,
            List<string> excludePayGroupIds = null,
            List<string> productIds = null,
            List<string> processIds = null,
            IProgress<string> progress = null)
        {
            var parameters = new TestRunInputParameters
            {
                AdvanceNumber = advanceNumber,
                PaymentDate = paymentDate,
                CutoffDate = cutoffDate,
                CropYear = cropYear,
                ExcludeGrowerIds = excludeGrowerIds,
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
            IProgress<string> progress = null)
        {
            var result = new TestRunResult { InputParameters = parameters };
            var generalErrors = result.GeneralErrors; // Shortcut for adding errors
            var growerPayments = result.GrowerPayments; // Shortcut for adding grower results

            try
            {
                progress?.Report("Starting calculation simulation...");

                // 1. Get eligible receipts (Moved from ProcessAdvancePaymentRunAsync)
                progress?.Report("Fetching eligible receipts for simulation...");
                // TODO: Update GetReceiptsForAdvancePaymentAsync signature if not already done
                var eligibleReceipts = await _receiptService.GetReceiptsForAdvancePaymentAsync(
                    parameters.AdvanceNumber,
                    parameters.CutoffDate,
                    null, // includeGrowerIds removed
                    null, // includePayGroupIds removed
                    parameters.ExcludeGrowerIds,
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

                // 2. Group receipts by Grower (Moved from ProcessAdvancePaymentRunAsync)
                var receiptsByGrower = eligibleReceipts.GroupBy(r => r.GrowerNumber);

                // 3. Process each grower (Logic to be moved next)
                int growerCount = 0;
                int totalGrowers = receiptsByGrower.Count();

                // 3. Process each grower (Moved from ProcessAdvancePaymentRunAsync)
                foreach (var growerGroup in receiptsByGrower)
                {
                    growerCount++;
                    var growerNumber = growerGroup.Key;
                    var growerReceipts = growerGroup.ToList();

                    var currentGrowerPayment = new TestRunGrowerPayment
                    {
                        GrowerNumber = growerNumber
                        // GrowerName, Currency, IsOnHold will be populated below
                    };

                    progress?.Report($"Simulating Grower {growerNumber} ({growerCount}/{totalGrowers})...");

                    // Get Grower details (GetGrowerByNumberAsync returns Grower)
                    var grower = await _growerService.GetGrowerByNumberAsync(growerNumber);
                    if (grower == null)
                    {
                        // Log error for this grower in the result, but continue processing others
                        generalErrors.Add($"Grower {growerNumber} not found during simulation. Skipping their receipts.");
                        // We don't add this grower to growerPayments list if they aren't found
                        continue; // Skip this grower
                    }
                    // Populate from Grower object
                    currentGrowerPayment.GrowerName = grower.GrowerName; // Corrected property name
                    currentGrowerPayment.Currency = grower.Currency.ToString(); // Currency is char in Grower
                    currentGrowerPayment.IsOnHold = grower.OnHold; // OnHold is bool in Grower

                    // 4. Process each receipt for the grower (Moved from ProcessAdvancePaymentRunAsync)
                    foreach (var receipt in growerReceipts)
                    {
                        var receiptDetail = new TestRunReceiptDetail
                        {
                            ReceiptNumber = receipt.ReceiptNumber,
                            ReceiptDate = receipt.Date,
                            Product = receipt.Product,
                            Process = receipt.Process,
                            Grade = receipt.Grade.ToString(), // Convert decimal Grade to string
                            NetWeight = receipt.Net
                        };

                        try
                        {
                            // Fix #4: Determine if this is a Fresh process (PROC_CLASS = 1)
                            // Mirrors legacy: aScan(aFresh, Daily->process) >= 1
                            receiptDetail.IsFresh = await _processClassificationService.IsFreshProcessAsync(receipt.Process);
                            receiptDetail.ProcessClass = await _processClassificationService.GetProcessClassAsync(receipt.Process);

                            decimal calculatedAdvancePrice = 0;
                            decimal premiumPrice = 0;
                            decimal marketingDeduction = 0;
                            decimal priceRecordId = 0;

                            // Find the relevant price record ID first
                            priceRecordId = await _priceService.FindPriceRecordIdAsync(receipt.Product, receipt.Process, receipt.Date);
                            if (priceRecordId == 0)
                            {
                                receiptDetail.ErrorMessage = $"No price record found (Product: {receipt.Product}, Process: {receipt.Process}, Date: {receipt.Date.ToShortDateString()}).";
                                currentGrowerPayment.ReceiptDetails.Add(receiptDetail);
                                continue; // Skip calculation for this receipt
                            }
                            receiptDetail.PriceRecordId = priceRecordId;

                            // Fix #5: Calculate running cumulative price using max() logic
                            // Get the running cumulative price up to current advance
                            // Mirrors legacy: RunAdvPrice(n) which uses max() to prevent backward pricing
                            var currentRunningPrice = await GetRunningAdvancePriceAsync(
                                receipt.Product,
                                receipt.Process,
                                receipt.Date,
                                parameters.AdvanceNumber,
                                grower.Currency,
                                grower.PriceLevel,
                                receipt.Grade);

                            // Calculate what has already been paid (cumulative with max logic)
                            decimal alreadyPaid = 0;

                            if (parameters.AdvanceNumber == 1)
                            {
                                // First advance - simple case
                                calculatedAdvancePrice = currentRunningPrice;
                                
                                // Get time premium using grower currency
                                premiumPrice = await _priceService.GetTimePremiumAsync(
                                    receipt.Product, 
                                    receipt.Process, 
                                    receipt.Date, 
                                    receipt.Date.TimeOfDay,
                                    grower.Currency);
                                    
                                marketingDeduction = await _priceService.GetMarketingDeductionAsync(receipt.Product);
                            }
                            else if (parameters.AdvanceNumber == 2)
                            {
                                // Advance 2: RunAdvPrice(2) - paid_adv1
                                var prevAdv1Price = await GetPreviousAdvancePrice(receipt.ReceiptNumber, 1);
                                alreadyPaid = prevAdv1Price;
                                calculatedAdvancePrice = currentRunningPrice - alreadyPaid;
                            }
                            else if (parameters.AdvanceNumber == 3)
                            {
                                // Advance 3: RunAdvPrice(3) - max(RunAdvPrice(2), paid_adv1)
                                // This mirrors GROW_AP.PRG line 420:
                                // nAdvance3 := Daily->(RunAdvPrice(3)) - max( Daily->(RunAdvPrice(2)), Daily->adv_pr1 )
                                var prevAdv1Price = await GetPreviousAdvancePrice(receipt.ReceiptNumber, 1);
                                
                                // Get running price through advance 2
                                var runningPriceAfterAdv2 = await GetRunningAdvancePriceAsync(
                                    receipt.Product,
                                    receipt.Process,
                                    receipt.Date,
                                    2,  // Up to advance 2
                                    grower.Currency,
                                    grower.PriceLevel,
                                    receipt.Grade);
                                
                                // Use max to handle cases where advance 2 wasn't paid or was less than advance 1
                                alreadyPaid = Math.Max(runningPriceAfterAdv2, prevAdv1Price);
                                calculatedAdvancePrice = currentRunningPrice - alreadyPaid;
                            }

                            // Ensure calculated price isn't negative (defensive programming)
                            calculatedAdvancePrice = Math.Max(0, calculatedAdvancePrice);

                            // Store calculated values in the receipt detail
                            receiptDetail.CalculatedAdvancePrice = calculatedAdvancePrice;
                            receiptDetail.CalculatedPremiumPrice = (parameters.AdvanceNumber == 1) ? premiumPrice : 0; // Only on first advance
                            receiptDetail.CalculatedMarketingDeduction = (parameters.AdvanceNumber == 1) ? marketingDeduction : 0; // Only on first advance

                            // Fix #6: Use RoundMoney for AwayFromZero rounding (matches legacy XBase round())
                            receiptDetail.CalculatedAdvanceAmount = RoundMoney(receipt.Net * receiptDetail.CalculatedAdvancePrice);
                            receiptDetail.CalculatedPremiumAmount = RoundMoney(receipt.Net * receiptDetail.CalculatedPremiumPrice);
                            receiptDetail.CalculatedDeductionAmount = RoundMoney(receipt.Net * receiptDetail.CalculatedMarketingDeduction);

                        }
                        catch (Exception ex)
                        {
                            receiptDetail.ErrorMessage = $"Calculation error: {ex.Message}";
                            Logger.Error($"Error simulating Receipt {receipt.ReceiptNumber} for Grower {growerNumber}", ex);
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
                Logger.Error($"Critical error during calculation simulation Advance {parameters.AdvanceNumber}", ex);
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
                Number = receipt.GrowerNumber,
                Date = entryDate, // Use the payment run date
                Type = accountType,
                // Class = ???, // Determine Class if needed
                Product = receipt.Product,
                Process = receipt.Process,
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

        // Helper to get advance number from account type
        private decimal GetAdvanceNumberFromType(string accountType)
        {
            if (accountType == AccTypeAdvance1) return 1;
            if (accountType == AccTypeAdvance2) return 2;
            if (accountType == AccTypeAdvance3) return 3;
            return 0; // Not an advance payment type
        }

        // Helper to get the account type string based on advance number
        private string GetAdvanceAccountType(int advanceNumber)
        {
            switch (advanceNumber)
            {
                case 1: return AccTypeAdvance1;
                case 2: return AccTypeAdvance2;
                case 3: return AccTypeAdvance3;
                default: throw new ArgumentOutOfRangeException(nameof(advanceNumber));
            }
        }

        /// <summary>
        /// Calculates the cumulative running advance price up to the specified advance number.
        /// Uses max() to ensure the price never goes backward (mirrors legacy RunAdvPrice).
        /// This prevents negative advance payments when price table has decreasing values.
        /// </summary>
        /// <param name="product">Product code</param>
        /// <param name="process">Process code</param>
        /// <param name="receiptDate">Receipt date</param>
        /// <param name="upToAdvanceNumber">Calculate cumulative price up to this advance (1, 2, or 3)</param>
        /// <param name="currency">Grower currency (C/U)</param>
        /// <param name="priceLevel">Grower price level (1-3)</param>
        /// <param name="grade">Receipt grade</param>
        /// <returns>Running cumulative maximum price</returns>
        private async Task<decimal> GetRunningAdvancePriceAsync(
            string product,
            string process,
            DateTime receiptDate,
            int upToAdvanceNumber,
            char currency,
            int priceLevel,
            decimal grade)
        {
            decimal runningMax = 0;

            for (int advNum = 1; advNum <= upToAdvanceNumber; advNum++)
            {
                var advPrice = await _priceService.GetAdvancePriceAsync(
                    product,
                    process,
                    receiptDate,
                    advNum,
                    currency,
                    priceLevel,
                    grade);

                // Key: Use Math.Max to prevent backward pricing
                // Mirrors legacy: nReturn := max( CurAdvPrice( n ), nReturn )
                runningMax = Math.Max(advPrice, runningMax);
            }

            return runningMax;
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

        // Helper to get previously paid advance price (needs implementation)
        private async Task<decimal> GetPreviousAdvancePrice(decimal receiptNumber, int advanceNumberToGet)
        {
            // This needs to query the Daily table for the specific receipt
            // and return the value from ADV_PR1 or ADV_PR2 column.
            var receipt = await _receiptService.GetReceiptByNumberAsync(receiptNumber);
            if (receipt == null) return 0;

            // TODO: Accessing ADV_PR1, ADV_PR2 etc. requires adding them to the Receipt model
            // or querying them directly here. Add these properties to Receipt.cs model first.
            // Example (assuming properties exist):
            // Access the newly added properties on the Receipt model
            if (advanceNumberToGet == 1)
            {
                return receipt.AdvPr1 ?? 0; // Use the AdvPr1 property
            }
            if (advanceNumberToGet == 2)
            {
                return receipt.AdvPr2 ?? 0; // Use the AdvPr2 property
            }

            return 0; // Should not happen if advanceNumberToGet is 1 or 2
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

    }
}
