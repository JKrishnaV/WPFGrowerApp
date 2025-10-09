using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.DataAccess.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    // Inherit from ViewModelBase
    public class DashboardViewModel : ViewModelBase 
    {
        private readonly GrowerService _growerService;
        private readonly ReceiptService _receiptService;
        private readonly ProductService _productService;
        private readonly ImportBatchService _importBatchService;
        
        private int _totalGrowers;
        private int _activeGrowers;
        private int _totalReceipts;
        private int _pendingReceipts;
        private int _totalProducts;
        private int _recentImports;
        private int _pendingPayments;
        private bool _isLoading;

        public DashboardViewModel()
        {
            _growerService = new GrowerService();
            _receiptService = new ReceiptService();
            _productService = new ProductService();
            _importBatchService = new ImportBatchService();
            
            IsLoading = true;
            _ = LoadDashboardDataAsync();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalGrowers
        {
            get => _totalGrowers;
            set => SetProperty(ref _totalGrowers, value);
        }

        public int ActiveGrowers
        {
            get => _activeGrowers;
            set => SetProperty(ref _activeGrowers, value);
        }

        public int TotalReceipts
        {
            get => _totalReceipts;
            set => SetProperty(ref _totalReceipts, value);
        }

        public int PendingReceipts
        {
            get => _pendingReceipts;
            set => SetProperty(ref _pendingReceipts, value);
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value);
        }

        public int RecentImports
        {
            get => _recentImports;
            set => SetProperty(ref _recentImports, value);
        }

        public int PendingPayments
        {
            get => _pendingPayments;
            set => SetProperty(ref _pendingPayments, value);
        }

        private async Task LoadDashboardDataAsync()
        {
            var totalStopwatch = Stopwatch.StartNew();
            Logger.Info("=== Dashboard Loading Started ===");

            try
            {
                IsLoading = true;

                // Load all statistics in parallel for maximum performance
                var sw1 = Stopwatch.StartNew();
                var sw2 = Stopwatch.StartNew();
                var sw3 = Stopwatch.StartNew();
                var sw4 = Stopwatch.StartNew();
                var sw5 = Stopwatch.StartNew();
                var sw6 = Stopwatch.StartNew();
                var sw7 = Stopwatch.StartNew();

                // Create all tasks (they'll run in parallel)
                var totalGrowersTask = Task.Run(async () => 
                {
                    sw1.Restart();
                    var result = await _growerService.GetTotalGrowersCountAsync();
                    sw1.Stop();
                    Logger.Info($"Dashboard: Total Growers loaded = {result} ({sw1.ElapsedMilliseconds}ms)");
                    return result;
                });

                var activeGrowersTask = Task.Run(async () => 
                {
                    sw2.Restart();
                    var result = await _growerService.GetActiveGrowersCountAsync();
                    sw2.Stop();
                    Logger.Info($"Dashboard: Active Growers loaded = {result} ({sw2.ElapsedMilliseconds}ms)");
                    return result;
                });

                var totalReceiptsTask = Task.Run(async () => 
                {
                    sw3.Restart();
                    var result = await _receiptService.GetTotalReceiptsCountAsync();
                    sw3.Stop();
                    Logger.Info($"Dashboard: Total Receipts loaded = {result} ({sw3.ElapsedMilliseconds}ms)");
                    return result;
                });

                var pendingReceiptsTask = Task.Run(async () => 
                {
                    sw4.Restart();
                    var result = await _receiptService.GetPendingReceiptsCountAsync();
                    sw4.Stop();
                    Logger.Info($"Dashboard: Pending Receipts loaded = {result} ({sw4.ElapsedMilliseconds}ms)");
                    return result;
                });

                var totalProductsTask = Task.Run(async () => 
                {
                    sw5.Restart();
                    var result = await _productService.GetTotalProductsCountAsync();
                    sw5.Stop();
                    Logger.Info($"Dashboard: Total Products loaded = {result} ({sw5.ElapsedMilliseconds}ms)");
                    return result;
                });

                var recentImportsTask = Task.Run(async () => 
                {
                    sw6.Restart();
                    var recentDate = System.DateTime.Now.AddDays(-30);
                    var result = await _importBatchService.GetRecentImportsCountAsync(recentDate);
                    sw6.Stop();
                    Logger.Info($"Dashboard: Recent Imports (30 days) loaded = {result} ({sw6.ElapsedMilliseconds}ms)");
                    return result;
                });

                var pendingPaymentsTask = Task.Run(async () => 
                {
                    sw7.Restart();
                    // Reuse pending receipts for now (could be enhanced with separate query)
                    var result = await _receiptService.GetPendingReceiptsCountAsync();
                    sw7.Stop();
                    Logger.Info($"Dashboard: Pending Payments loaded = {result} ({sw7.ElapsedMilliseconds}ms)");
                    return result;
                });

                // Wait for all tasks to complete in parallel
                await Task.WhenAll(
                    totalGrowersTask,
                    activeGrowersTask,
                    totalReceiptsTask,
                    pendingReceiptsTask,
                    totalProductsTask,
                    recentImportsTask,
                    pendingPaymentsTask
                );

                // Assign results to properties
                TotalGrowers = await totalGrowersTask;
                ActiveGrowers = await activeGrowersTask;
                TotalReceipts = await totalReceiptsTask;
                PendingReceipts = await pendingReceiptsTask;
                TotalProducts = await totalProductsTask;
                RecentImports = await recentImportsTask;
                PendingPayments = await pendingPaymentsTask;

                totalStopwatch.Stop();
                Logger.Info($"=== Dashboard Loading Completed in {totalStopwatch.ElapsedMilliseconds}ms ===");
            }
            catch (System.Exception ex)
            {
                totalStopwatch.Stop();
                Logger.Error($"Error loading dashboard data (failed after {totalStopwatch.ElapsedMilliseconds}ms)", ex);
                
                // Set default values on error
                TotalGrowers = 0;
                ActiveGrowers = 0;
                TotalReceipts = 0;
                PendingReceipts = 0;
                TotalProducts = 0;
                RecentImports = 0;
                PendingPayments = 0;
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
