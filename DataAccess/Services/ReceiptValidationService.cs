using System;
using System.Linq;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Services
{
    /// <summary>
    /// Service for receipt validation operations
    /// </summary>
    public class ReceiptValidationService : IReceiptValidationService
    {
        private readonly IGrowerService _growerService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IDepotService _depotService;
        private readonly IReceiptService _receiptService;

        public ReceiptValidationService(
            IGrowerService growerService,
            IProductService productService,
            IProcessService processService,
            IDepotService depotService,
            IReceiptService receiptService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateReceiptDataAsync(Receipt receipt)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                Logger.Info($"Validating receipt data for ReceiptId: {receipt.ReceiptId}");

                // Validate required fields
                ValidateRequiredFields(receipt, result);

                // Validate field formats
                ValidateFieldFormats(receipt, result);

                // Validate weights
                var weightResult = ValidateWeights(receipt.GrossWeight, receipt.TareWeight, receipt.DockPercentage);
                foreach (var error in weightResult.Errors)
                {
                    result.AddError(error.FieldName, error.Message);
                }
                foreach (var warning in weightResult.Warnings)
                {
                    result.AddWarning(warning.FieldName, warning.Message);
                }

                // Validate grade
                var gradeResult = ValidateGradeRange(receipt.Grade);
                foreach (var error in gradeResult.Errors)
                {
                    result.AddError(error.FieldName, error.Message);
                }
                foreach (var warning in gradeResult.Warnings)
                {
                    result.AddWarning(warning.FieldName, warning.Message);
                }

                // Validate date/time consistency
                var dateTimeResult = ValidateDateTimeConsistency(receipt.ReceiptDate, receipt.ReceiptTime);
                foreach (var error in dateTimeResult.Errors)
                {
                    result.AddError(error.FieldName, error.Message);
                }
                foreach (var warning in dateTimeResult.Warnings)
                {
                    result.AddWarning(warning.FieldName, warning.Message);
                }

                // Validate receipt number format
                if (!string.IsNullOrEmpty(receipt.ReceiptNumber))
                {
                    var receiptNumberResult = ValidateReceiptNumberFormat(receipt.ReceiptNumber);
                    foreach (var error in receiptNumberResult.Errors)
                    {
                        result.AddError(error.FieldName, error.Message);
                    }
                    foreach (var warning in receiptNumberResult.Warnings)
                    {
                        result.AddWarning(warning.FieldName, warning.Message);
                    }
                }

                // Validate references
                await ValidateReferencesAsync(receipt, result);

                // Validate business rules
                var businessResult = await ValidateBusinessRulesAsync(receipt);
                foreach (var error in businessResult.Errors)
                {
                    result.AddError(error.FieldName, error.Message);
                }
                foreach (var warning in businessResult.Warnings)
                {
                    result.AddWarning(warning.FieldName, warning.Message);
                }

                // Check for duplicates
                if (!string.IsNullOrEmpty(receipt.ReceiptNumber))
                {
                    var duplicateResult = await ValidateDuplicateReceiptAsync(receipt.ReceiptNumber, receipt.ReceiptDate, receipt.ReceiptId);
                    foreach (var error in duplicateResult.Errors)
                    {
                        result.AddError(error.FieldName, error.Message);
                    }
                    foreach (var warning in duplicateResult.Warnings)
                    {
                        result.AddWarning(warning.FieldName, warning.Message);
                    }
                }

                result.IsValid = result.Errors.Count == 0;

                Logger.Info($"Receipt validation completed. Valid: {result.IsValid}, Errors: {result.Errors.Count}, Warnings: {result.Warnings.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating receipt data", ex);
                result.AddError("Validation", "An error occurred during validation");
            }

            return result;
        }

        public WPFGrowerApp.Models.ValidationResult ValidateWeights(decimal grossWeight, decimal tareWeight, decimal dockPercentage)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            // Validate gross weight
            if (grossWeight <= 0)
            {
                result.AddError("GrossWeight", "Gross weight must be greater than 0");
            }
            else if (grossWeight > 999999.99m)
            {
                result.AddError("GrossWeight", "Gross weight cannot exceed 999,999.99 lbs");
            }

            // Validate tare weight
            if (tareWeight < 0)
            {
                result.AddError("TareWeight", "Tare weight cannot be negative");
            }
            else if (tareWeight >= grossWeight)
            {
                result.AddError("TareWeight", "Tare weight must be less than gross weight");
            }

            // Validate dock percentage
            if (dockPercentage < 0)
            {
                result.AddError("DockPercentage", "Dock percentage cannot be negative");
            }
            else if (dockPercentage > 100)
            {
                result.AddError("DockPercentage", "Dock percentage cannot exceed 100%");
            }

            // Business rule: warn if dock percentage is unusually high
            if (dockPercentage > 20)
            {
                result.AddWarning("DockPercentage", "Dock percentage is unusually high (>20%). Please verify this is correct.");
            }

            return result;
        }

        public WPFGrowerApp.Models.ValidationResult ValidateGradeRange(int grade)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            if (grade < 1 || grade > 3)
            {
                result.AddError("Grade", "Grade must be between 1 and 3");
            }

            return result;
        }

        public WPFGrowerApp.Models.ValidationResult ValidateDateTimeConsistency(DateTime date, TimeSpan time)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            // Check if date is in the future
            if (date > DateTime.Today)
            {
                result.AddError("ReceiptDate", "Receipt date cannot be in the future");
            }

            // Check if date is too far in the past (more than 2 years)
            if (date < DateTime.Today.AddYears(-2))
            {
                result.AddWarning("ReceiptDate", "Receipt date is more than 2 years old. Please verify this is correct.");
            }

            // Check if time is reasonable (not midnight unless specifically intended)
            if (time == TimeSpan.Zero)
            {
                result.AddWarning("ReceiptTime", "Receipt time is set to midnight. Please verify this is correct.");
            }

            return result;
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateDuplicateReceiptAsync(string receiptNumber, DateTime date, int? excludeReceiptId = null)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                // Check if receipt number already exists for the same date
                var existingReceipts = await _receiptService.GetReceiptsAsync(date, date);
                var duplicate = existingReceipts.FirstOrDefault(r => 
                    r.ReceiptNumber == receiptNumber && 
                    r.ReceiptId != excludeReceiptId);

                if (duplicate != null)
                {
                    result.AddError("ReceiptNumber", $"Receipt number '{receiptNumber}' already exists for date {date:yyyy-MM-dd}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking for duplicate receipt", ex);
                result.AddError("ReceiptNumber", "Unable to verify receipt number uniqueness");
            }

            return result;
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateGrowerActiveAsync(int growerId)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                var grower = await _growerService.GetGrowerByIdAsync(growerId);
                if (grower == null)
                {
                    result.AddError("GrowerId", "Grower not found");
                }
                else if (grower.DeletedAt.HasValue)
                {
                    result.AddError("GrowerId", "Grower is inactive (deleted)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating grower {growerId}", ex);
                result.AddError("GrowerId", "Unable to validate grower");
            }

            return result;
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateProductActiveAsync(int productId)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null)
                {
                    result.AddError("ProductId", "Product not found");
                }
                else if (product.DeletedAt.HasValue)
                {
                    result.AddError("ProductId", "Product is inactive (deleted)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating product {productId}", ex);
                result.AddError("ProductId", "Unable to validate product");
            }

            return result;
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateProcessActiveAsync(int processId)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                var process = await _processService.GetProcessByIdAsync(processId);
                if (process == null)
                {
                    result.AddError("ProcessId", "Process not found");
                }
                else if (process.DeletedAt.HasValue)
                {
                    result.AddError("ProcessId", "Process is inactive (deleted)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating process {processId}", ex);
                result.AddError("ProcessId", "Unable to validate process");
            }

            return result;
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateDepotActiveAsync(int depotId)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                var depot = await _depotService.GetDepotByIdAsync(depotId);
                if (depot == null)
                {
                    result.AddError("DepotId", "Depot not found");
                }
                else if (depot.DeletedAt.HasValue)
                {
                    result.AddError("DepotId", "Depot is inactive (deleted)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating depot {depotId}", ex);
                result.AddError("DepotId", "Unable to validate depot");
            }

            return result;
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidatePriceClassActiveAsync(int priceClassId)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                // TODO: Implement price class validation when service is available
                // For now, just validate that it's a positive number
                if (priceClassId <= 0)
                {
                    result.AddError("PriceClassId", "Price class ID must be greater than 0");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating price class {priceClassId}", ex);
                result.AddError("PriceClassId", "Unable to validate price class");
            }

            return result;
        }

        public WPFGrowerApp.Models.ValidationResult ValidateReceiptNumberFormat(string receiptNumber)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            if (string.IsNullOrWhiteSpace(receiptNumber))
            {
                result.AddError("ReceiptNumber", "Receipt number is required");
                return result;
            }

            if (receiptNumber.Length > 20)
            {
                result.AddError("ReceiptNumber", "Receipt number cannot exceed 20 characters");
            }

            // Check for invalid characters (only alphanumeric and common separators allowed)
            if (!System.Text.RegularExpressions.Regex.IsMatch(receiptNumber, @"^[A-Za-z0-9\-_]+$"))
            {
                result.AddError("ReceiptNumber", "Receipt number contains invalid characters. Only letters, numbers, hyphens, and underscores are allowed.");
            }

            return result;
        }

        public async Task<WPFGrowerApp.Models.ValidationResult> ValidateBusinessRulesAsync(Receipt receipt)
        {
            var result = new WPFGrowerApp.Models.ValidationResult();

            try
            {
                // Business rule: Receipt cannot be voided if it has payments
                if (receipt.IsVoided)
                {
                    // TODO: Check if receipt has payment allocations
                    // This would require integration with payment services
                }

                // Business rule: Quality check cannot be done on voided receipts
                if (receipt.IsVoided && receipt.QualityCheckedAt.HasValue)
                {
                    result.AddError("QualityCheckedAt", "Quality check cannot be performed on voided receipts");
                }

                // Business rule: Receipt date should not be more than 30 days in the future
                if (receipt.ReceiptDate > DateTime.Today.AddDays(30))
                {
                    result.AddWarning("ReceiptDate", "Receipt date is more than 30 days in the future. Please verify this is correct.");
                }

                // Business rule: Dock percentage should be reasonable for the product type
                // This would require product-specific validation rules
                if (receipt.DockPercentage > 50)
                {
                    result.AddWarning("DockPercentage", "Dock percentage is very high (>50%). Please verify this is correct for the product type.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error validating business rules for receipt {receipt.ReceiptId}", ex);
                result.AddError("BusinessRules", "Unable to validate business rules");
            }

            return result;
        }

        #region Private Helper Methods

        private void ValidateRequiredFields(Receipt receipt, WPFGrowerApp.Models.ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(receipt.ReceiptNumber))
                result.AddError("ReceiptNumber", "Receipt number is required");

            if (receipt.GrowerId <= 0)
                result.AddError("GrowerId", "Grower is required");

            if (receipt.ProductId <= 0)
                result.AddError("ProductId", "Product is required");

            if (receipt.ProcessId <= 0)
                result.AddError("ProcessId", "Process is required");

            if (receipt.DepotId <= 0)
                result.AddError("DepotId", "Depot is required");

            if (receipt.PriceClassId <= 0)
                result.AddError("PriceClassId", "Price class is required");
        }

        private void ValidateFieldFormats(Receipt receipt, WPFGrowerApp.Models.ValidationResult result)
        {
            // Validate receipt number format
            if (!string.IsNullOrEmpty(receipt.ReceiptNumber))
            {
                var receiptNumberResult = ValidateReceiptNumberFormat(receipt.ReceiptNumber);
                foreach (var error in receiptNumberResult.Errors)
                {
                    result.AddError(error.FieldName, error.Message);
                }
                foreach (var warning in receiptNumberResult.Warnings)
                {
                    result.AddWarning(warning.FieldName, warning.Message);
                }
            }

            // Validate void reason if voided
            if (receipt.IsVoided && string.IsNullOrWhiteSpace(receipt.VoidedReason))
            {
                result.AddError("VoidedReason", "Void reason is required when receipt is voided");
            }
        }

        private async Task ValidateReferencesAsync(Receipt receipt, WPFGrowerApp.Models.ValidationResult result)
        {
            // Validate grower
            var growerResult = await ValidateGrowerActiveAsync(receipt.GrowerId);
            foreach (var error in growerResult.Errors)
            {
                result.AddError(error.FieldName, error.Message);
            }
            foreach (var warning in growerResult.Warnings)
            {
                result.AddWarning(warning.FieldName, warning.Message);
            }

            // Validate product
            var productResult = await ValidateProductActiveAsync(receipt.ProductId);
            foreach (var error in productResult.Errors)
            {
                result.AddError(error.FieldName, error.Message);
            }
            foreach (var warning in productResult.Warnings)
            {
                result.AddWarning(warning.FieldName, warning.Message);
            }

            // Validate process
            var processResult = await ValidateProcessActiveAsync(receipt.ProcessId);
            foreach (var error in processResult.Errors)
            {
                result.AddError(error.FieldName, error.Message);
            }
            foreach (var warning in processResult.Warnings)
            {
                result.AddWarning(warning.FieldName, warning.Message);
            }

            // Validate depot
            var depotResult = await ValidateDepotActiveAsync(receipt.DepotId);
            foreach (var error in depotResult.Errors)
            {
                result.AddError(error.FieldName, error.Message);
            }
            foreach (var warning in depotResult.Warnings)
            {
                result.AddWarning(warning.FieldName, warning.Message);
            }

            // Validate price class
            var priceClassResult = await ValidatePriceClassActiveAsync(receipt.PriceClassId);
            foreach (var error in priceClassResult.Errors)
            {
                result.AddError(error.FieldName, error.Message);
            }
            foreach (var warning in priceClassResult.Warnings)
            {
                result.AddWarning(warning.FieldName, warning.Message);
            }
        }

        #endregion
    }
}
