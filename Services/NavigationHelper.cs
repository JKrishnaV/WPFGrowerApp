using System;
using System.Threading.Tasks;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Static helper for navigation between views
    /// </summary>
    public static class NavigationHelper
    {
        /// <summary>
        /// Static event for navigation requests
        /// </summary>
        public static event Action<Type, string>? NavigationRequested;

        /// <summary>
        /// Navigate to Dashboard
        /// </summary>
        public static void NavigateToDashboard()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(DashboardViewModel), "Dashboard");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigate to Payment Management Hub
        /// </summary>
        public static void NavigateToPaymentManagement()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(PaymentManagementHubViewModel), "Payment Management");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigate to Advance Cheques
        /// </summary>
        public static void NavigateToAdvanceCheques()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(AdvanceChequeViewModel), "Advance Cheques");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigate to Enhanced Payment Distribution
        /// </summary>
        public static void NavigateToEnhancedPaymentDistribution()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(EnhancedPaymentDistributionViewModel), "Enhanced Payment Distribution");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigate to Enhanced Cheque Preparation
        /// </summary>
        public static void NavigateToEnhancedChequePreparation()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(EnhancedChequePreparationViewModel), "Enhanced Cheque Preparation");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigate to Import Files
        /// </summary>
        public static void NavigateToImportFiles()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(ImportViewModel), "Import Files");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigate to Batch Management
        /// </summary>
        public static void NavigateToBatchManagement()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(BatchManagementViewModel), "Batch Management");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Navigate to Import Hub
        /// </summary>
        public static void NavigateToImportHub()
        {
            try
            {
                NavigationRequested?.Invoke(typeof(ImportHubViewModel), "Import Hub");
            }
            catch (Exception ex)
            {
                // Log error if needed
                Logger.Error($"Navigation error: {ex.Message}", ex);
            }
        }
    }
}
