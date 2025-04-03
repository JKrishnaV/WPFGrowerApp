using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging; // Assuming Logger is available

namespace WPFGrowerApp.DataAccess.Services
{
    public class PaymentService : BaseDatabaseService, IPaymentService
    {
        private readonly IReceiptService _receiptService;
        private readonly IPriceService _priceService;
        private readonly IAccountService _accountService;
        private readonly IPostBatchService _postBatchService;
        private readonly IGrowerService _growerService; // Needed for grower details like currency, GST status

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
            IGrowerService growerService)
        {
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _postBatchService = postBatchService ?? throw new ArgumentNullException(nameof(postBatchService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
        }

        public async Task<(bool Success, List<string> Errors, PostBatch CreatedBatch)> ProcessAdvancePaymentRunAsync(
            int advanceNumber,
            DateTime paymentDate,
            DateTime cutoffDate,
            int cropYear,
            decimal? includeGrowerId = null,
            string includePayGroup = null,
            decimal? excludeGrowerId = null,
            string excludePayGroup = null,
            string productId = null,
            string processId = null,
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

                // 2. Get eligible receipts
                progress?.Report("Fetching eligible receipts...");
                var eligibleReceipts = await _receiptService.GetReceiptsForAdvancePaymentAsync(
                    advanceNumber, cutoffDate, includeGrowerId, includePayGroup,
                    excludeGrowerId, excludePayGroup, productId, processId);

                if (!eligibleReceipts.Any())
                {
                    progress?.Report("No eligible receipts found for this payment run.");
                    LogAnEvent(EVT_TYPE_ADVANCE_NO_RECEIPTS, $"No eligible receipts for Advance {advanceNumber}.");
                    return (true, errors, createdBatch); // Successful run, just no receipts
                }
                progress?.Report($"Found {eligibleReceipts.Count} eligible receipts.");

                // 3. Group receipts by Grower
                var receiptsByGrower = eligibleReceipts.GroupBy(r => r.GrowerNumber);

                // 4. Process each grower
                int growerCount = 0;
                int totalGrowers = receiptsByGrower.Count();

                foreach (var growerGroup in receiptsByGrower)
                {
                    growerCount++;
                    var growerNumber = growerGroup.Key;
                    var growerReceipts = growerGroup.ToList();
                    var growerAccountEntries = new List<Account>();
                    bool growerSuccess = true;

                    progress?.Report($"Processing Grower {growerNumber} ({growerCount}/{totalGrowers})...");

                    // Get Grower details (needed for currency, GST etc.)
                    var grower = await _growerService.GetGrowerByNumberAsync(growerNumber);
                    if (grower == null)
                    {
                        errors.Add($"Grower {growerNumber} not found. Skipping receipts.");
                        LogAnEvent(EVT_TYPE_ADVANCE_GROWER_NOT_FOUND, $"Grower {growerNumber} not found during Advance {advanceNumber}.");
                        continue; // Skip this grower
                    }

                    // 5. Process each receipt for the grower
                    foreach (var receipt in growerReceipts)
                    {
                        try
                        {
                            decimal calculatedAdvancePrice = 0;
                            decimal premiumPrice = 0;
                            decimal marketingDeduction = 0;
                            decimal priceRecordId = 0;

                            // Find the relevant price record ID first
                            priceRecordId = await _priceService.FindPriceRecordIdAsync(receipt.Product, receipt.Process, receipt.Date);
                            if (priceRecordId == 0)
                            {
                                errors.Add($"No price record found for Receipt {receipt.ReceiptNumber} (Product: {receipt.Product}, Process: {receipt.Process}, Date: {receipt.Date.ToShortDateString()}). Skipping.");
                                growerSuccess = false;
                                continue; // Skip this receipt if no base price record
                            }

                            // Calculate price for the current advance
                            var currentAdvanceBasePrice = await _priceService.GetAdvancePriceAsync(receipt.Product, receipt.Process, receipt.Date, advanceNumber);

                            // Adjust price based on previous advances paid
                            if (advanceNumber == 1)
                            {
                                calculatedAdvancePrice = currentAdvanceBasePrice;
                                // Calculate premium and deduction only on the first advance payment for a receipt
                                premiumPrice = await _priceService.GetTimePremiumAsync(receipt.Product, receipt.Process, receipt.Date, receipt.Date.TimeOfDay); // Assuming TimeOfDay is sufficient
                                marketingDeduction = await _priceService.GetMarketingDeductionAsync(receipt.Product);
                            }
                            else if (advanceNumber == 2)
                            {
                                // Need ADV_PR1 from the receipt (assuming it was updated previously)
                                var prevAdv1Price = await GetPreviousAdvancePrice(receipt.ReceiptNumber, 1); // Helper needed
                                calculatedAdvancePrice = currentAdvanceBasePrice - prevAdv1Price;
                            }
                            else if (advanceNumber == 3)
                            {
                                // Need ADV_PR1 and ADV_PR2
                                var prevAdv1Price = await GetPreviousAdvancePrice(receipt.ReceiptNumber, 1);
                                var prevAdv2Price = await GetPreviousAdvancePrice(receipt.ReceiptNumber, 2);
                                calculatedAdvancePrice = currentAdvanceBasePrice - prevAdv1Price - prevAdv2Price;
                            }

                            // Ensure calculated price isn't negative (can happen if prices decrease)
                            calculatedAdvancePrice = Math.Max(0, calculatedAdvancePrice);

                            // Create Account entry for the advance payment
                            if (calculatedAdvancePrice > 0)
                            {
                                growerAccountEntries.Add(CreateAccountEntry(receipt, GetAdvanceAccountType(advanceNumber), calculatedAdvancePrice, grower.Currency.ToString(), cropYear, createdBatch.PostBat, paymentDate)); // Convert char to string
                            }

                            // Create Account entries for premium and deduction (only on first advance)
                            if (advanceNumber == 1)
                            {
                                if (premiumPrice > 0)
                                {
                                    growerAccountEntries.Add(CreateAccountEntry(receipt, AccTypePremium, premiumPrice, grower.Currency.ToString(), cropYear, createdBatch.PostBat, paymentDate)); // Convert char to string
                                }
                                if (marketingDeduction != 0) // Deductions are often negative rates
                                {
                                    growerAccountEntries.Add(CreateAccountEntry(receipt, AccTypeDeduction, marketingDeduction, grower.Currency.ToString(), cropYear, createdBatch.PostBat, paymentDate)); // Convert char to string
                                }
                            }

                            // Update the Daily record with calculated details and batch ID
                            bool updateSuccess = await _receiptService.UpdateReceiptAdvanceDetailsAsync(
                                receipt.ReceiptNumber, advanceNumber, createdBatch.PostBat,
                                calculatedAdvancePrice, priceRecordId, premiumPrice);

                            if (!updateSuccess)
                            {
                                errors.Add($"Failed to update advance details for Receipt {receipt.ReceiptNumber}.");
                                growerSuccess = false;
                                // Decide whether to rollback grower or continue? For now, continue but log error.
                            }
                            else
                            {
                                // Mark the price record advance as used
                                await _priceService.MarkAdvancePriceAsUsedAsync(priceRecordId, advanceNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error processing Receipt {receipt.ReceiptNumber} for Grower {growerNumber}: {ex.Message}");
                            growerSuccess = false;
                            // Log detailed error
                            Logger.Error($"Error processing Receipt {receipt.ReceiptNumber} for Grower {growerNumber}", ex);
                        }
                    } // End foreach receipt

                    // 6. Save Account entries for the grower (if successful so far)
                    if (growerSuccess && growerAccountEntries.Any())
                    {
                        bool accountSaveSuccess = await _accountService.CreatePaymentAccountEntriesAsync(growerAccountEntries);
                        if (!accountSaveSuccess)
                        {
                            errors.Add($"Failed to save account entries for Grower {growerNumber}.");
                            // Consider rollback logic for this grower's receipt updates? Complex.
                            LogAnEvent(EVT_TYPE_ADVANCE_ACCOUNT_SAVE_FAIL, $"Failed account save for Grower {growerNumber}, Batch {createdBatch.PostBat}.");
                        }
                    }
                    else if (!growerSuccess)
                    {
                         LogAnEvent(EVT_TYPE_ADVANCE_GROWER_FAIL, $"Skipped account save for Grower {growerNumber} due to previous errors, Batch {createdBatch.PostBat}.");
                    }

                } // End foreach grower

                overallSuccess = !errors.Any();
                progress?.Report($"Payment run processing complete. Success: {overallSuccess}");
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

        // Helper method to create an Account entry
        private Account CreateAccountEntry(Receipt receipt, string accountType, decimal unitPrice, string currency, int year, decimal batchId, DateTime entryDate)
        {
            // Calculate dollars and potentially GST
            decimal dollars = Math.Round(receipt.Net * unitPrice, 2);
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
