using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.DataAccess.Models;
using System.Collections.ObjectModel;
using System.Linq;

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
            try
            {
                IsLoading = true;

                // Load grower statistics
                var growers = await _growerService.GetAllGrowersAsync();
                TotalGrowers = growers.Count();
                ActiveGrowers = growers.Count(g => !g.IsOnHold);

                // Load receipt statistics
                var receipts = await _receiptService.GetReceiptsAsync();
                TotalReceipts = receipts.Count();
                PendingReceipts = receipts.Count(r => !r.IsVoided);

                // Load product statistics
                var products = await _productService.GetAllProductsAsync();
                TotalProducts = products.Count();

                // Load import statistics (last 30 days)
                var recentDate = System.DateTime.Now.AddDays(-30);
                var importBatches = await _importBatchService.GetImportBatchesAsync(recentDate);
                RecentImports = importBatches.Count();

                // Calculate pending payments (simplified - could be enhanced)
                PendingPayments = receipts.Count(r => !r.IsVoided);
            }
            catch (System.Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error loading dashboard data", ex);
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
