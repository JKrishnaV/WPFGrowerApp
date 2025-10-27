using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for advance deduction validation and reconciliation services
    /// </summary>
    public interface IAdvanceDeductionValidationService
    {
        /// <summary>
        /// Validate that advance balances are correct
        /// </summary>
        Task<AdvanceValidationResult> ValidateAdvanceBalancesAsync();

        /// <summary>
        /// Validate that deduction totals match parent advance totals
        /// </summary>
        Task<AdvanceValidationResult> ValidateDeductionTotalsAsync();

        /// <summary>
        /// Find orphaned deductions (deductions without valid parent advance)
        /// </summary>
        Task<AdvanceValidationResult> FindOrphanedDeductionsAsync();

        /// <summary>
        /// Reconcile advance amounts by fixing any discrepancies
        /// </summary>
        Task<ReconciliationResult> ReconcileAdvanceAmountsAsync();

        /// <summary>
        /// Get comprehensive validation report
        /// </summary>
        Task<ValidationReport> GetValidationReportAsync();
    }
}
