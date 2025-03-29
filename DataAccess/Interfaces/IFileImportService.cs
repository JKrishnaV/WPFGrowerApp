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
        /// </summary>
        Task<IEnumerable<Receipt>> ReadReceiptsFromFileAsync(
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
    }
} 