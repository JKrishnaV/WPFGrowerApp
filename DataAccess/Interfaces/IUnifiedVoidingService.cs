using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for unified voiding of all payment types
    /// </summary>
    public interface IUnifiedVoidingService
    {
        /// <summary>
        /// Voids a payment of any type
        /// </summary>
        /// <param name="request">The voiding request</param>
        /// <returns>Voiding result with details</returns>
        Task<VoidingResult> VoidPaymentAsync(PaymentVoidRequest request);

        /// <summary>
        /// Voids a regular batch payment
        /// </summary>
        /// <param name="chequeId">The cheque ID to void</param>
        /// <param name="reason">The reason for voiding</param>
        /// <param name="voidedBy">The user voiding the payment</param>
        /// <returns>Voiding result</returns>
        Task<VoidingResult> VoidRegularBatchPaymentAsync(int chequeId, string reason, string voidedBy);

        /// <summary>
        /// Voids an advance cheque
        /// </summary>
        /// <param name="advanceChequeId">The advance cheque ID to void</param>
        /// <param name="reason">The reason for voiding</param>
        /// <param name="voidedBy">The user voiding the advance</param>
        /// <returns>Voiding result</returns>
        Task<VoidingResult> VoidAdvanceChequeAsync(int advanceChequeId, string reason, string voidedBy);

        /// <summary>
        /// Voids a consolidated payment
        /// </summary>
        /// <param name="chequeId">The consolidated cheque ID to void</param>
        /// <param name="reason">The reason for voiding</param>
        /// <param name="voidedBy">The user voiding the payment</param>
        /// <returns>Voiding result</returns>
        Task<VoidingResult> VoidConsolidatedPaymentAsync(int chequeId, string reason, string voidedBy);

        /// <summary>
        /// Gets voiding history for a payment
        /// </summary>
        /// <param name="entityType">The entity type (Regular, Advance, Consolidated)</param>
        /// <param name="entityId">The entity ID</param>
        /// <returns>List of voiding records</returns>
        Task<List<PaymentAuditLog>> GetVoidingHistoryAsync(string entityType, int entityId);

        /// <summary>
        /// Checks if a payment can be voided
        /// </summary>
        /// <param name="entityType">The entity type</param>
        /// <param name="entityId">The entity ID</param>
        /// <returns>True if the payment can be voided</returns>
        Task<bool> CanVoidPaymentAsync(string entityType, int entityId);

        /// <summary>
        /// Gets voiding statistics
        /// </summary>
        /// <param name="startDate">Start date filter</param>
        /// <param name="endDate">End date filter</param>
        /// <returns>Voiding statistics</returns>
        Task<VoidingStatistics> GetVoidingStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Reverses a voiding operation
        /// </summary>
        /// <param name="voidingId">The voiding record ID to reverse</param>
        /// <param name="reason">The reason for reversal</param>
        /// <param name="reversedBy">The user reversing the voiding</param>
        /// <returns>True if successful</returns>
        Task<bool> ReverseVoidingAsync(int voidingId, string reason, string reversedBy);

        /// <summary>
        /// Gets all voided payments within a date range
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="entityType">Optional entity type filter</param>
        /// <returns>List of voided payments</returns>
        Task<List<VoidedPayment>> GetVoidedPaymentsAsync(DateTime startDate, DateTime endDate, string entityType = null);
    }
}
