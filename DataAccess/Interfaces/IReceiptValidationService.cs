using System;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for receipt validation operations
    /// </summary>
    public interface IReceiptValidationService
    {
        /// <summary>
        /// Validate receipt data comprehensively
        /// </summary>
        /// <param name="receipt">The receipt to validate</param>
        /// <returns>Validation result with errors and warnings</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateReceiptDataAsync(Receipt receipt);

        /// <summary>
        /// Validate weight data
        /// </summary>
        /// <param name="grossWeight">Gross weight</param>
        /// <param name="tareWeight">Tare weight</param>
        /// <param name="dockPercentage">Dock percentage</param>
        /// <returns>Validation result</returns>
        WPFGrowerApp.Models.ValidationResult ValidateWeights(decimal grossWeight, decimal tareWeight, decimal dockPercentage);

        /// <summary>
        /// Validate grade range
        /// </summary>
        /// <param name="grade">Grade value</param>
        /// <returns>Validation result</returns>
        WPFGrowerApp.Models.ValidationResult ValidateGradeRange(int grade);

        /// <summary>
        /// Validate date and time consistency
        /// </summary>
        /// <param name="date">Receipt date</param>
        /// <param name="time">Receipt time</param>
        /// <returns>Validation result</returns>
        WPFGrowerApp.Models.ValidationResult ValidateDateTimeConsistency(DateTime date, TimeSpan time);

        /// <summary>
        /// Validate for duplicate receipt
        /// </summary>
        /// <param name="receiptNumber">Receipt number</param>
        /// <param name="date">Receipt date</param>
        /// <param name="excludeReceiptId">Receipt ID to exclude from duplicate check (for updates)</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateDuplicateReceiptAsync(string receiptNumber, DateTime date, int? excludeReceiptId = null);

        /// <summary>
        /// Validate grower is active
        /// </summary>
        /// <param name="growerId">Grower ID</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateGrowerActiveAsync(int growerId);

        /// <summary>
        /// Validate product is active
        /// </summary>
        /// <param name="productId">Product ID</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateProductActiveAsync(int productId);

        /// <summary>
        /// Validate process is active
        /// </summary>
        /// <param name="processId">Process ID</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateProcessActiveAsync(int processId);

        /// <summary>
        /// Validate depot is active
        /// </summary>
        /// <param name="depotId">Depot ID</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateDepotActiveAsync(int depotId);

        /// <summary>
        /// Validate price class is active
        /// </summary>
        /// <param name="priceClassId">Price class ID</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidatePriceClassActiveAsync(int priceClassId);

        /// <summary>
        /// Validate receipt number format
        /// </summary>
        /// <param name="receiptNumber">Receipt number</param>
        /// <returns>Validation result</returns>
        WPFGrowerApp.Models.ValidationResult ValidateReceiptNumberFormat(string receiptNumber);

        /// <summary>
        /// Validate business rules
        /// </summary>
        /// <param name="receipt">The receipt to validate</param>
        /// <returns>Validation result</returns>
        Task<WPFGrowerApp.Models.ValidationResult> ValidateBusinessRulesAsync(Receipt receipt);
    }
}
