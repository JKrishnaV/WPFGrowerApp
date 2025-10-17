using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for payment reconciliation operations.
    /// Handles reconciliation reports, exception tracking, and resolution.
    /// </summary>
    public interface IPaymentReconciliationService : IDatabaseService
    {
        /// <summary>
        /// Reconciles a payment distribution and generates a reconciliation report.
        /// </summary>
        /// <param name="distributionId">The ID of the distribution to reconcile.</param>
        /// <returns>Reconciliation report with discrepancies and exceptions.</returns>
        Task<ReconciliationReport> ReconcileDistributionAsync(int distributionId);

        /// <summary>
        /// Gets all payment exceptions that require attention.
        /// </summary>
        /// <returns>List of payment exceptions.</returns>
        Task<List<PaymentException>> GetPaymentExceptionsAsync();

        /// <summary>
        /// Resolves a payment exception with a resolution note.
        /// </summary>
        /// <param name="exceptionId">The ID of the exception to resolve.</param>
        /// <param name="resolution">The resolution description.</param>
        /// <param name="resolvedBy">The user resolving the exception.</param>
        /// <returns>True if the resolution was successful.</returns>
        Task<bool> ResolveExceptionAsync(int exceptionId, string resolution, string resolvedBy);

        /// <summary>
        /// Gets payment exceptions for a specific distribution.
        /// </summary>
        /// <param name="distributionId">The ID of the distribution.</param>
        /// <returns>List of exceptions for the distribution.</returns>
        Task<List<PaymentException>> GetExceptionsByDistributionAsync(int distributionId);

        /// <summary>
        /// Creates a new payment exception.
        /// </summary>
        /// <param name="exception">The exception to create.</param>
        /// <returns>The created exception with assigned ID.</returns>
        Task<PaymentException> CreateExceptionAsync(PaymentException exception);

        /// <summary>
        /// Gets reconciliation statistics for reporting.
        /// </summary>
        /// <returns>Summary of reconciliation statuses and counts.</returns>
        Task<Dictionary<string, object>> GetReconciliationStatisticsAsync();

        /// <summary>
        /// Validates that a distribution is ready for final processing.
        /// </summary>
        /// <param name="distributionId">The ID of the distribution to validate.</param>
        /// <returns>List of validation issues, empty if ready.</returns>
        Task<List<string>> ValidateDistributionForCompletionAsync(int distributionId);

        /// <summary>
        /// Marks a distribution as completed after successful reconciliation.
        /// </summary>
        /// <param name="distributionId">The ID of the distribution.</param>
        /// <param name="completedBy">The user completing the distribution.</param>
        /// <returns>True if the completion was successful.</returns>
        Task<bool> MarkDistributionAsCompletedAsync(int distributionId, string completedBy);
    }
}
