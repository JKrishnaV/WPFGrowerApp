using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace WPFGrowerApp.Infrastructure
{
    /// <summary>
    /// Extension methods for Receipt model to provide convenient access to process classification.
    /// </summary>
    public static class ReceiptExtensions
    {
        /// <summary>
        /// Determines if this receipt's process is classified as Fresh (PROC_CLASS = 1).
        /// Mirrors legacy: aScan(aFresh, Daily->process) >= 1
        /// </summary>
        /// <param name="receipt">The receipt to check</param>
        /// <returns>True if the process is Fresh, false otherwise</returns>
        public static async Task<bool> IsFreshAsync(this Receipt receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt.Process))
                return false;

            var classificationService = GetProcessClassificationService();
            return await classificationService.IsFreshProcessAsync(receipt.Process);
        }

        /// <summary>
        /// Gets the process class for this receipt (1=Fresh, 2=Processed, 3=Juice, 4=Other).
        /// Mirrors legacy: Process->proc_class
        /// </summary>
        /// <param name="receipt">The receipt to check</param>
        /// <returns>Process class integer</returns>
        public static async Task<int> GetProcessClassAsync(this Receipt receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt.Process))
                return 4; // Default to "Other" if no process specified

            var classificationService = GetProcessClassificationService();
            return await classificationService.GetProcessClassAsync(receipt.Process);
        }

        /// <summary>
        /// Gets the process classification name for this receipt.
        /// </summary>
        /// <param name="receipt">The receipt to check</param>
        /// <returns>Classification name (Fresh, Processed, Juice, Other), or null if not found</returns>
        public static async Task<string?> GetProcessClassificationNameAsync(this Receipt receipt)
        {
            if (string.IsNullOrWhiteSpace(receipt.Process))
                return null;

            var classificationService = GetProcessClassificationService();
            var processClass = await classificationService.GetProcessClassAsync(receipt.Process);
            return classificationService.GetProcessClassName(processClass);
        }

        /// <summary>
        /// Helper method to get the IProcessClassificationService from DI container.
        /// </summary>
        private static IProcessClassificationService GetProcessClassificationService()
        {
            return App.ServiceProvider.GetRequiredService<IProcessClassificationService>();
        }
    }
}
