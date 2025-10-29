using System.Collections.Generic;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.DataAccess.Interfaces
{
    /// <summary>
    /// Interface for Payment Summary Report Service.
    /// Provides methods for generating comprehensive payment reports and analysis.
    /// </summary>
    public interface IPaymentSummaryReportService
    {
        /// <summary>
        /// Generates a complete Payment Summary Report based on the provided filter options.
        /// </summary>
        /// <param name="options">Filter options for the report</param>
        /// <returns>Complete Payment Summary Report with all data and charts</returns>
        Task<PaymentSummaryReport> GenerateReportAsync(ReportFilterOptions options);

        /// <summary>
        /// Gets detailed payment information for individual growers.
        /// </summary>
        /// <param name="options">Filter options for the data</param>
        /// <returns>List of grower payment details</returns>
        Task<List<GrowerPaymentDetail>> GetGrowerPaymentDetailsAsync(ReportFilterOptions options);

        /// <summary>
        /// Gets summary statistics for the payment report.
        /// </summary>
        /// <param name="options">Filter options for the statistics</param>
        /// <returns>Payment Summary Report with statistics only</returns>
        Task<PaymentSummaryReport> GetSummaryStatisticsAsync(ReportFilterOptions options);

        /// <summary>
        /// Gets payment distribution data for chart visualization.
        /// </summary>
        /// <param name="options">Filter options for the data</param>
        /// <returns>List of payment distribution chart data</returns>
        Task<List<PaymentDistributionChart>> GetPaymentDistributionDataAsync(ReportFilterOptions options);

        /// <summary>
        /// Gets monthly trend data for chart visualization.
        /// </summary>
        /// <param name="options">Filter options for the data</param>
        /// <returns>List of monthly trend chart data</returns>
        Task<List<MonthlyTrendChart>> GetMonthlyTrendDataAsync(ReportFilterOptions options);

        /// <summary>
        /// Gets top performing growers based on payment amounts.
        /// </summary>
        /// <param name="options">Filter options for the data</param>
        /// <param name="count">Number of top performers to return (default: 10)</param>
        /// <returns>List of top performing growers</returns>
        Task<List<GrowerPerformanceChart>> GetTopPerformersAsync(ReportFilterOptions options, int count = 10);
    }
}
