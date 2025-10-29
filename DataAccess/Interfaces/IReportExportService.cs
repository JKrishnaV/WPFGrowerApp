using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for Report Export Service.
    /// Provides methods for exporting Payment Summary Reports to various formats.
    /// </summary>
    public interface IReportExportService
    {
        /// <summary>
        /// Exports the Payment Summary Report to PDF format.
        /// </summary>
        /// <param name="report">The report data to export</param>
        /// <param name="options">Export configuration options</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportToPdfAsync(PaymentSummaryReport report, ExportOptions options);

        /// <summary>
        /// Exports the Payment Summary Report to Excel format.
        /// </summary>
        /// <param name="report">The report data to export</param>
        /// <param name="options">Export configuration options</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportToExcelAsync(PaymentSummaryReport report, ExportOptions options);

        /// <summary>
        /// Exports the Payment Summary Report to CSV format.
        /// </summary>
        /// <param name="report">The report data to export</param>
        /// <param name="options">Export configuration options</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportToCsvAsync(PaymentSummaryReport report, ExportOptions options);

        /// <summary>
        /// Exports the Payment Summary Report to Word format.
        /// </summary>
        /// <param name="report">The report data to export</param>
        /// <param name="options">Export configuration options</param>
        /// <returns>Task representing the export operation</returns>
        Task ExportToWordAsync(PaymentSummaryReport report, ExportOptions options);

        /// <summary>
        /// Gets the list of supported export formats.
        /// </summary>
        /// <returns>List of supported format names</returns>
        List<string> GetSupportedFormats();

        /// <summary>
        /// Validates export options before processing.
        /// </summary>
        /// <param name="options">Export options to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        bool ValidateExportOptions(ExportOptions options);
    }
}
