using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IFileImportService
    {
        /// <summary>
        /// Validates the file format and structure
        /// </summary>
        Task<bool> ValidateFileFormatAsync(string filePath);

        /// <summary>
        /// Reads receipts from the specified file
        /// Returns tuple with valid receipts and list of error messages for skipped lines
        /// </summary>
        Task<(IEnumerable<Receipt> receipts, List<string> errors)> ReadReceiptsFromFileAsync(
            string filePath,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a collection of receipts before import
        /// </summary>
        Task<(bool IsValid, List<string> Errors)> ValidateReceiptsAsync(
            IEnumerable<Receipt> receipts,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the expected file format specification
        /// </summary>
        string GetFileFormatSpecification();

        /// <summary>
        /// Analyzes batch numbers in a file and groups receipts by their original batch number
        /// </summary>
        Task<Dictionary<string, List<Receipt>>> AnalyzeBatchNumbersAsync(string filePath, CancellationToken cancellationToken = default);
    }
} 