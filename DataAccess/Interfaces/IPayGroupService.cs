using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Defines the contract for services handling PayGroup data operations.
    /// </summary>
    public interface IPayGroupService
    {
        /// <summary>
        /// Retrieves all PayGroup records asynchronously.
        /// </summary>
        /// <returns>A collection of PayGroup objects.</returns>
        Task<IEnumerable<PayGroup>> GetAllPayGroupsAsync();

        /// <summary>
        /// Retrieves all PaymentGroup records asynchronously (alias for GetAllPayGroupsAsync).
        /// </summary>
        /// <returns>A collection of PaymentGroup objects.</returns>
        Task<IEnumerable<PaymentGroup>> GetAllPaymentGroupsAsync();

        /// <summary>
        /// Retrieves a specific PayGroup record by its ID asynchronously.
        /// </summary>
        /// <param name="payGroupId">The ID of the PayGroup to retrieve.</param>
        /// <returns>The PayGroup object if found; otherwise, null.</returns>
        Task<PayGroup> GetPayGroupByIdAsync(string payGroupId);

        /// <summary>
        /// Adds a new PayGroup record asynchronously.
        /// </summary>
        /// <param name="payGroup">The PayGroup object to add.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        Task<bool> AddPayGroupAsync(PayGroup payGroup);

        /// <summary>
        /// Updates an existing PayGroup record asynchronously.
        /// </summary>
        /// <param name="payGroup">The PayGroup object with updated information.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        Task<bool> UpdatePayGroupAsync(PayGroup payGroup);

        /// <summary>
        /// Deletes a PayGroup record by its ID asynchronously.
        /// </summary>
        /// <param name="payGroupId">The ID of the PayGroup to delete.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        Task<bool> DeletePayGroupAsync(string payGroupId);
    }
}
