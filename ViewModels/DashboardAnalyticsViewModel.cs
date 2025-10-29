using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using System.Windows;
using System.Globalization;

namespace WPFGrowerApp.ViewModels
{
    public class DashboardAnalyticsViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentBatchService _paymentBatchService;
        private readonly IDialogService _dialogService;

        // Data Collections
        private ObservableCollection<Grower> _growers;
        private ObservableCollection<PaymentBatch> _paymentBatches;
        private ObservableCollection<Payment> _recentPayments;

        // Dashboard Data
        private DashboardSummary _dashboardSummary;
        private ObservableCollection<ChartDataPoint> _monthlyPaymentTrend;
        private ObservableCollection<ChartDataPoint> _growerPerformanceData;
        private ObservableCollection<ChartDataPoint> _paymentMethodDistribution;
        private ObservableCollection<ChartDataPoint> _provinceDistribution;
        private ObservableCollection<ChartDataPoint> _priceLevelDistribution;

        // Filter Properties
        private DateTime _startDate = DateTime.Now.AddMonths(-12);
        private DateTime _endDate = DateTime.Now;
        private string _selectedProvince = "All";
        private string _selectedPriceLevel = "All";
        private bool _includeOnHoldGrowers = true;

        // UI State Properties
        private bool _showFinancialSummary = true;
        private bool _showGrowerAnalytics = true;
        private bool _showPaymentTrends = true;
        private bool _showDistributionCharts = true;
        private bool _showRecentActivity = true;

        // Export Properties
        private string _selectedExportFormat = "PDF";
        private bool _isExporting;

        public DashboardAnalyticsViewModel(
            IGrowerService growerService,
            IPaymentService paymentService,
            IPaymentBatchService paymentBatchService,
            IDialogService dialogService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _paymentBatchService = paymentBatchService ?? throw new ArgumentNullException(nameof(paymentBatchService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Initialize collections
            _growers = new ObservableCollection<Grower>();
            _paymentBatches = new ObservableCollection<PaymentBatch>();
            _recentPayments = new ObservableCollection<Payment>();
            _monthlyPaymentTrend = new ObservableCollection<ChartDataPoint>();
            _growerPerformanceData = new ObservableCollection<ChartDataPoint>();
            _paymentMethodDistribution = new ObservableCollection<ChartDataPoint>();
            _provinceDistribution = new ObservableCollection<ChartDataPoint>();
            _priceLevelDistribution = new ObservableCollection<ChartDataPoint>();

            // Initialize commands
            LoadDashboardDataCommand = new RelayCommand(async _ => await LoadDashboardDataAsync(), _ => !IsBusy);
            RefreshDataCommand = new RelayCommand(async _ => await LoadDashboardDataAsync(), _ => !IsBusy);
            ExportDashboardCommand = new RelayCommand(async _ => await ExportDashboardAsync(), _ => !IsBusy && !IsExporting);
            ToggleSectionCommand = new RelayCommand(ToggleSection);

            // Initialize filter options
            ProvinceOptions = new ObservableCollection<string> { "All" };
            PriceLevelOptions = new ObservableCollection<string> { "All" };
            ExportFormatOptions = new ObservableCollection<string> { "PDF", "Excel", "Word", "CSV" };

            // Load initial data
            _ = LoadDashboardDataAsync();
        }

        #region Properties

        public ObservableCollection<Grower> Growers
        {
            get => _growers;
            set => SetProperty(ref _growers, value);
        }

        public ObservableCollection<PaymentBatch> PaymentBatches
        {
            get => _paymentBatches;
            set => SetProperty(ref _paymentBatches, value);
        }

        public ObservableCollection<Payment> RecentPayments
        {
            get => _recentPayments;
            set => SetProperty(ref _recentPayments, value);
        }

        public DashboardSummary DashboardSummary
        {
            get => _dashboardSummary;
            set => SetProperty(ref _dashboardSummary, value);
        }

        public ObservableCollection<ChartDataPoint> MonthlyPaymentTrend
        {
            get => _monthlyPaymentTrend;
            set => SetProperty(ref _monthlyPaymentTrend, value);
        }

        public ObservableCollection<ChartDataPoint> GrowerPerformanceData
        {
            get => _growerPerformanceData;
            set => SetProperty(ref _growerPerformanceData, value);
        }

        public ObservableCollection<ChartDataPoint> PaymentMethodDistribution
        {
            get => _paymentMethodDistribution;
            set => SetProperty(ref _paymentMethodDistribution, value);
        }

        public ObservableCollection<ChartDataPoint> ProvinceDistribution
        {
            get => _provinceDistribution;
            set => SetProperty(ref _provinceDistribution, value);
        }

        public ObservableCollection<ChartDataPoint> PriceLevelDistribution
        {
            get => _priceLevelDistribution;
            set => SetProperty(ref _priceLevelDistribution, value);
        }

        #endregion

        #region Filter Properties

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadDashboardDataAsync();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = LoadDashboardDataAsync();
                }
            }
        }

        public string SelectedProvince
        {
            get => _selectedProvince;
            set
            {
                if (SetProperty(ref _selectedProvince, value))
                {
                    _ = LoadDashboardDataAsync();
                }
            }
        }

        public string SelectedPriceLevel
        {
            get => _selectedPriceLevel;
            set
            {
                if (SetProperty(ref _selectedPriceLevel, value))
                {
                    _ = LoadDashboardDataAsync();
                }
            }
        }

        public bool IncludeOnHoldGrowers
        {
            get => _includeOnHoldGrowers;
            set
            {
                if (SetProperty(ref _includeOnHoldGrowers, value))
                {
                    _ = LoadDashboardDataAsync();
                }
            }
        }

        public ObservableCollection<string> ProvinceOptions { get; }
        public ObservableCollection<string> PriceLevelOptions { get; }
        public ObservableCollection<string> ExportFormatOptions { get; }

        #endregion

        #region UI State Properties

        public bool ShowFinancialSummary
        {
            get => _showFinancialSummary;
            set => SetProperty(ref _showFinancialSummary, value);
        }

        public bool ShowGrowerAnalytics
        {
            get => _showGrowerAnalytics;
            set => SetProperty(ref _showGrowerAnalytics, value);
        }

        public bool ShowPaymentTrends
        {
            get => _showPaymentTrends;
            set => SetProperty(ref _showPaymentTrends, value);
        }

        public bool ShowDistributionCharts
        {
            get => _showDistributionCharts;
            set => SetProperty(ref _showDistributionCharts, value);
        }

        public bool ShowRecentActivity
        {
            get => _showRecentActivity;
            set => SetProperty(ref _showRecentActivity, value);
        }

        #endregion

        #region Export Properties

        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set => SetProperty(ref _selectedExportFormat, value);
        }

        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                SetProperty(ref _isExporting, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion

        #region Commands

        public ICommand LoadDashboardDataCommand { get; }
        public ICommand RefreshDataCommand { get; }
        public ICommand ExportDashboardCommand { get; }
        public ICommand ToggleSectionCommand { get; }

        #endregion

        #region Methods

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                IsBusy = true;

                // Load all data in parallel
                var growerTask = _growerService.GetAllGrowersAsync();
                var paymentBatchTask = _paymentBatchService.GetAllBatchesAsync();
                var paymentTask = Task.FromResult(new List<Payment>()); // TODO: Implement GetPaymentsByDateRangeAsync

                await Task.WhenAll(growerTask, paymentBatchTask, paymentTask);

                var growers = await growerTask;
                var paymentBatches = await paymentBatchTask;
                var payments = await paymentTask;

                // Apply filters
                var filteredGrowers = ApplyGrowerFilters(growers);
                var filteredPayments = ApplyPaymentFilters(payments);

                // Update collections
                Growers.Clear();
                foreach (var grower in filteredGrowers)
                {
                    Growers.Add(grower);
                }

                PaymentBatches.Clear();
                foreach (var batch in paymentBatches)
                {
                    PaymentBatches.Add(batch);
                }

                RecentPayments.Clear();
                foreach (var payment in filteredPayments.OrderByDescending(p => p.PaymentDate).Take(10))
                {
                    RecentPayments.Add(payment);
                }

                // Update filter options
                UpdateFilterOptions(growers);

                // Generate dashboard data
                GenerateDashboardSummary(filteredGrowers, filteredPayments, paymentBatches);
                GenerateMonthlyPaymentTrend(filteredPayments);
                GenerateGrowerPerformanceData(filteredGrowers, filteredPayments);
                GeneratePaymentMethodDistribution(filteredPayments);
                GenerateProvinceDistribution(filteredGrowers);
                GeneratePriceLevelDistribution(filteredGrowers);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error loading dashboard data: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private List<Grower> ApplyGrowerFilters(List<Grower> growers)
        {
            var filtered = growers.AsEnumerable();

            if (!IncludeOnHoldGrowers)
            {
                filtered = filtered.Where(g => !g.IsOnHold);
            }

            if (SelectedProvince != "All")
            {
                filtered = filtered.Where(g => g.Province == SelectedProvince);
            }

            if (SelectedPriceLevel != "All")
            {
                filtered = filtered.Where(g => g.PriceLevel.ToString() == SelectedPriceLevel);
            }

            return filtered.ToList();
        }

        private List<Payment> ApplyPaymentFilters(List<Payment> payments)
        {
            return payments.Where(p => p.PaymentDate >= StartDate && p.PaymentDate <= EndDate).ToList();
        }

        private void UpdateFilterOptions(List<Grower> growers)
        {
            // Update province options
            var provinces = growers.Select(g => g.Province).Distinct().OrderBy(p => p).ToList();
            ProvinceOptions.Clear();
            ProvinceOptions.Add("All");
            foreach (var province in provinces)
            {
                ProvinceOptions.Add(province);
            }

            // Update price level options
            var priceLevels = growers.Select(g => g.PriceLevel).Distinct().OrderBy(pl => pl).ToList();
            PriceLevelOptions.Clear();
            PriceLevelOptions.Add("All");
            foreach (var priceLevel in priceLevels)
            {
                PriceLevelOptions.Add(priceLevel.ToString());
            }
        }

        private void GenerateDashboardSummary(List<Grower> growers, List<Payment> payments, List<PaymentBatch> batches)
        {
            DashboardSummary = new DashboardSummary
            {
                TotalGrowers = growers.Count,
                ActiveGrowers = growers.Count(g => g.IsActive && !g.IsOnHold),
                OnHoldGrowers = growers.Count(g => g.IsOnHold),
                TotalPayments = payments.Count,
                TotalPaymentAmount = payments.Sum(p => p.Amount),
                AveragePaymentAmount = payments.Any() ? payments.Average(p => p.Amount) : 0,
                TotalBatches = batches.Count,
                CompletedBatches = batches.Count(b => b.Status == "Completed"),
                PendingBatches = batches.Count(b => b.Status == "Pending"),
                LastUpdated = DateTime.Now
            };
        }

        private void GenerateMonthlyPaymentTrend(List<Payment> payments)
        {
            MonthlyPaymentTrend.Clear();

            var monthlyData = payments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month)
                .Select(g => new ChartDataPoint
                {
                    Category = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Value = (double)g.Sum(p => p.Amount)
                })
                .ToList();

            foreach (var dataPoint in monthlyData)
            {
                MonthlyPaymentTrend.Add(dataPoint);
            }
        }

        private void GenerateGrowerPerformanceData(List<Grower> growers, List<Payment> payments)
        {
            GrowerPerformanceData.Clear();

            var growerPayments = payments
                .GroupBy(p => p.GrowerId)
                .Select(g => new
                {
                    GrowerId = g.Key,
                    TotalAmount = g.Sum(p => p.Amount),
                    PaymentCount = g.Count()
                })
                .OrderByDescending(gp => gp.TotalAmount)
                .Take(10)
                .ToList();

            foreach (var gp in growerPayments)
            {
                var grower = growers.FirstOrDefault(g => g.GrowerId == gp.GrowerId);
                GrowerPerformanceData.Add(new ChartDataPoint
                {
                    Category = grower?.FullName ?? $"Grower {gp.GrowerId}",
                    Value = (double)gp.TotalAmount
                });
            }
        }

        private void GeneratePaymentMethodDistribution(List<Payment> payments)
        {
            PaymentMethodDistribution.Clear();

            var methodData = payments
                .GroupBy(p => p.PaymentTypeId.ToString())
                .Select(g => new ChartDataPoint
                {
                    Category = g.Key,
                    Value = g.Count()
                })
                .ToList();

            foreach (var dataPoint in methodData)
            {
                PaymentMethodDistribution.Add(dataPoint);
            }
        }

        private void GenerateProvinceDistribution(List<Grower> growers)
        {
            ProvinceDistribution.Clear();

            var provinceData = growers
                .GroupBy(g => g.Province)
                .Select(g => new ChartDataPoint
                {
                    Category = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(g => g.Value)
                .ToList();

            foreach (var dataPoint in provinceData)
            {
                ProvinceDistribution.Add(dataPoint);
            }
        }

        private void GeneratePriceLevelDistribution(List<Grower> growers)
        {
            PriceLevelDistribution.Clear();

            var priceLevelData = growers
                .GroupBy(g => g.PriceLevel)
                .Select(g => new ChartDataPoint
                {
                    Category = $"Level {g.Key}",
                    Value = g.Count()
                })
                .OrderBy(g => g.Category)
                .ToList();

            foreach (var dataPoint in priceLevelData)
            {
                PriceLevelDistribution.Add(dataPoint);
            }
        }

        private void ToggleSection(object parameter)
        {
            if (parameter is string sectionName)
            {
                switch (sectionName)
                {
                    case "FinancialSummary":
                        ShowFinancialSummary = !ShowFinancialSummary;
                        break;
                    case "GrowerAnalytics":
                        ShowGrowerAnalytics = !ShowGrowerAnalytics;
                        break;
                    case "PaymentTrends":
                        ShowPaymentTrends = !ShowPaymentTrends;
                        break;
                    case "DistributionCharts":
                        ShowDistributionCharts = !ShowDistributionCharts;
                        break;
                    case "RecentActivity":
                        ShowRecentActivity = !ShowRecentActivity;
                        break;
                }
            }
        }

        private async Task ExportDashboardAsync()
        {
            try
            {
                IsExporting = true;

                var exportService = new DashboardExportService();
                await exportService.ExportDashboardAsync(
                    DashboardSummary,
                    MonthlyPaymentTrend,
                    GrowerPerformanceData,
                    PaymentMethodDistribution,
                    ProvinceDistribution,
                    PriceLevelDistribution,
                    RecentPayments,
                    SelectedExportFormat,
                    StartDate,
                    EndDate);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error exporting dashboard: {ex.Message}", "Export Error");
            }
            finally
            {
                IsExporting = false;
            }
        }

        #endregion
    }

    #region Data Models

    public class DashboardSummary
    {
        public int TotalGrowers { get; set; }
        public int ActiveGrowers { get; set; }
        public int OnHoldGrowers { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public decimal AveragePaymentAmount { get; set; }
        public int TotalBatches { get; set; }
        public int CompletedBatches { get; set; }
        public int PendingBatches { get; set; }
        public DateTime LastUpdated { get; set; }

        public string TotalPaymentAmountFormatted => TotalPaymentAmount.ToString("C");
        public string AveragePaymentAmountFormatted => AveragePaymentAmount.ToString("C");
        public double CompletionRate => TotalBatches > 0 ? (double)CompletedBatches / TotalBatches * 100 : 0;
    }

    public class ChartDataPoint
    {
        public string Category { get; set; }
        public double Value { get; set; }
        public string FormattedValue => Value.ToString("C");
    }

    #endregion
}