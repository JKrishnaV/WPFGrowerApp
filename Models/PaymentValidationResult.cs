using System.Collections.Generic;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Results of payment calculation validation
    /// </summary>
    public class PaymentValidationResult
    {
        public bool HasErrors { get; set; }
        public bool HasWarnings { get; set; }
        public List<ValidationIssue> Errors { get; set; } = new();
        public List<ValidationIssue> Warnings { get; set; } = new();
        public int TotalReceipts { get; set; }
        public int ValidReceipts { get; set; }
        public int InvalidReceipts { get; set; }
    }

    /// <summary>
    /// Represents a validation issue found in a receipt
    /// </summary>
    public class ValidationIssue
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public string GrowerNumber { get; set; } = string.Empty;
        public string GrowerName { get; set; } = string.Empty;
        public ValidationIssueType IssueType { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of validation issues that can occur
    /// </summary>
    public enum ValidationIssueType
    {
        MissingProduct,
        MissingProcess,
        MissingPriceSchedule,
        InvalidProductProcess,
        MissingPaymentType,
        CalculationError,
        Other
    }
}

