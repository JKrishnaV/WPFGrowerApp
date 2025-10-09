using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service for managing payment types (Advance 1, Advance 2, Advance 3, Final, etc.)
    /// </summary>
    public interface IPaymentTypeService
    {
        /// <summary>
        /// Get all active payment types
        /// </summary>
        Task<List<PaymentType>> GetAllPaymentTypesAsync();

        /// <summary>
        /// Get payment type by ID
        /// </summary>
        Task<PaymentType?> GetPaymentTypeByIdAsync(int paymentTypeId);

        /// <summary>
        /// Get payment type by code (ADV1, ADV2, ADV3, FINAL, etc.)
        /// </summary>
        Task<PaymentType?> GetPaymentTypeByCodeAsync(string paymentTypeCode);

        /// <summary>
        /// Get advance payment types only (ADV1, ADV2, ADV3)
        /// </summary>
        Task<List<PaymentType>> GetAdvancePaymentTypesAsync();
    }
}


