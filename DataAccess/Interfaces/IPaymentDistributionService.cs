using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    public interface IPaymentDistributionService
    {
        Task<IEnumerable<PaymentBatch>> GetAvailableBatchesAsync();
        Task<PaymentDistribution> CreateDistributionAsync(PaymentDistribution distribution);
        Task<bool> GeneratePaymentsAsync(int distributionId, string generatedBy);
        Task<IEnumerable<PaymentDistribution>> GetDistributionsAsync();
        Task<PaymentDistribution> GetDistributionByIdAsync(int distributionId);
        Task<bool> ProcessDistributionAsync(int distributionId, string processedBy);
        Task<bool> VoidDistributionAsync(int distributionId, string reason, string voidedBy);
        Task<bool> HasExistingDistributionsAsync(int paymentBatchId);
        Task<List<PaymentDistribution>> GetAllDistributionsAsync();
        Task<ChequeAuditTrail> GetChequeAuditTrailAsync(string chequeNumber);
        Task<PaymentDistribution> GenerateCompletePaymentDistributionAsync(PaymentDistribution distribution, string generatedBy, List<int> selectedBatchIds = null);
        Task UpdateBatchProcessingStatusAsync(List<int> batchIds, List<int> processedGrowerIds, string processedBy);
    }
}
