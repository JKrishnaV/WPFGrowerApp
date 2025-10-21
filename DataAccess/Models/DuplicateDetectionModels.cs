using System;
using System.Collections.Generic;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Result of analyzing receipts for duplicates
    /// </summary>
    public class ImportAnalysisResult
    {
        public int TotalReceipts { get; set; }
        public List<Receipt> NewReceipts { get; set; } = new List<Receipt>();
        public List<Receipt> DuplicateReceipts { get; set; } = new List<Receipt>();
        public List<Receipt> SoftDeletedReceipts { get; set; } = new List<Receipt>();
        public string Summary => $"Total: {TotalReceipts}, New: {NewReceipts.Count}, Duplicates: {DuplicateReceipts.Count}, Soft-deleted: {SoftDeletedReceipts.Count}";
    }

    /// <summary>
    /// Analysis of batch conflicts when same batch number is used
    /// </summary>
    public class BatchConflictAnalysis
    {
        public string FileName { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public int TotalReceipts { get; set; }
        public bool BatchExists { get; set; }
        public List<Receipt> NewReceipts { get; set; } = new List<Receipt>();
        public List<ReceiptConflict> Conflicts { get; set; } = new List<ReceiptConflict>();
        public string Summary { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a conflict between a new receipt and an existing receipt
    /// </summary>
    public class ReceiptConflict
    {
        public Receipt NewReceipt { get; set; } = new Receipt();
        public Receipt ExistingReceipt { get; set; } = new Receipt();
        public ConflictType ConflictType { get; set; }
        public List<string> ResolutionOptions { get; set; } = new List<string>();
    }

    /// <summary>
    /// Types of conflicts between receipts
    /// </summary>
    public enum ConflictType
    {
        ExactDuplicate,
        DataMismatch,
        DateMismatch,
        SoftDeleted
    }

    /// <summary>
    /// Result of presenting conflict resolution options
    /// </summary>
    public class ConflictResolutionResult
    {
        public bool CanProceed { get; set; }
        public bool RequiresUserDecision { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
        public List<ConflictResolutionOption> Options { get; set; } = new List<ConflictResolutionOption>();
        public BatchConflictAnalysis? Analysis { get; set; }
    }

    /// <summary>
    /// A single conflict resolution option
    /// </summary>
    public class ConflictResolutionOption
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public bool Recommended { get; set; }
    }

    /// <summary>
    /// Result of selective import with duplicate handling
    /// </summary>
    public class SelectiveImportResult
    {
        public int ImportBatchId { get; set; }
        public int TotalReceipts { get; set; }
        public bool RequiresUserDecision { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<Receipt> ImportedReceipts { get; set; } = new List<Receipt>();
        public List<Receipt> UpdatedReceipts { get; set; } = new List<Receipt>();
        public List<Receipt> SkippedReceipts { get; set; } = new List<Receipt>();
        public ImportAnalysisResult? Analysis { get; set; }
    }

    /// <summary>
    /// Strategies for handling duplicate receipts
    /// </summary>
    public enum DuplicateHandlingStrategy
    {
        SkipDuplicates,
        UpdateDuplicates,
        AskUser
    }

    /// <summary>
    /// Comprehensive import report
    /// </summary>
    public class ImportReport
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime ImportDate { get; set; }
        public int TotalReceiptsInFile { get; set; }
        public int SuccessfullyImported { get; set; }
        public int UpdatedExisting { get; set; }
        public int SkippedDuplicates { get; set; }
        public int FailedImports { get; set; }
        public List<Receipt> NewReceipts { get; set; } = new List<Receipt>();
        public List<Receipt> UpdatedReceipts { get; set; } = new List<Receipt>();
        public List<Receipt> SkippedReceipts { get; set; } = new List<Receipt>();
        public List<ImportError> FailedReceipts { get; set; } = new List<ImportError>();
        public string Summary => $"Import completed: {SuccessfullyImported} new, {UpdatedExisting} updated, {SkippedDuplicates} skipped, {FailedImports} failed";
    }

    /// <summary>
    /// Represents an import error
    /// </summary>
    public class ImportError
    {
        public string ReceiptNumber { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of smart import with conflict resolution
    /// </summary>
    public class SmartImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int TotalBatches { get; set; }
        public List<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();
        public List<BatchConflictAnalysis> Conflicts { get; set; } = new List<BatchConflictAnalysis>();
        public List<SelectiveImportResult> ImportResults { get; set; } = new List<SelectiveImportResult>();
    }
}
