using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Service interface for payment method data operations.
    /// </summary>
    public interface IPaymentMethodService : IDatabaseService
    {
        /// <summary>
        /// Gets all payment methods.
        /// </summary>
        /// <returns>List of all payment methods</returns>
        Task<List<PaymentMethod>> GetAllPaymentMethodsAsync();

        /// <summary>
        /// Gets a payment method by ID.
        /// </summary>
        /// <param name="paymentMethodId">The payment method ID</param>
        /// <returns>The payment method, or null if not found</returns>
        Task<PaymentMethod> GetPaymentMethodByIdAsync(int paymentMethodId);

        /// <summary>
        /// Creates a new payment method.
        /// </summary>
        /// <param name="paymentMethod">The payment method to create</param>
        /// <returns>The ID of the created payment method</returns>
        Task<int> CreatePaymentMethodAsync(PaymentMethod paymentMethod);

        /// <summary>
        /// Updates an existing payment method.
        /// </summary>
        /// <param name="paymentMethod">The payment method to update</param>
        /// <returns>True if the update was successful</returns>
        Task<bool> UpdatePaymentMethodAsync(PaymentMethod paymentMethod);

        /// <summary>
        /// Deletes a payment method.
        /// </summary>
        /// <param name="paymentMethodId">The payment method ID to delete</param>
        /// <returns>True if the deletion was successful</returns>
        Task<bool> DeletePaymentMethodAsync(int paymentMethodId);
    }
}
