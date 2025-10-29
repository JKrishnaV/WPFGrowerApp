using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the Payment Summary Report with comprehensive data binding and command handling.
    /// Implements MVVM pattern with modern async/await patterns and proper error handling.
    /// </summary>
    public partial class PaymentSummaryReportViewModel : ViewModelBase
    {
        #region Private Fields

        private readonly IPaymentSummaryReportService _reportService;
        private readonly IReportExportService _exportService;
        private readonly IGrowerService _growerService;

        private PaymentSummaryReport _currentReport;
        private ReportFilterOptions _filterOptions;
        private ObservableCollection<GrowerPaymentDetail> _growerDetails;
        private ObservableCollection<PaymentDistributionChart> _paymentDistribution;
        private ObservableCollection<MonthlyTrendChart> _monthlyTrends;
        private ObservableCollection<GrowerPerformanceChart> _topPerformers;

        // UI State Properties
        private bool _isLoading;
        private bool _isExporting;
        private string _selectedExportFormat;
        private bool _showContactInfo;
        private bool _showProductDetails;
        private bool _showAuditInfo;
        private bool _showCharts;
        private bool _showSummaryStatistics;
        private string _statusMessage;
        private double _exportProgress;

        // Filter Properties
        private DateTime _periodStart;
        private DateTime _periodEnd;
        private string _selectedDateRangePreset;
        private ObservableCollection<int> _selectedGrowerIds;
        private ObservableCollection<string> _selectedProvinces;
        private ObservableCollection<string> _selectedCities;
        private ObservableCollection<string> _selectedPaymentGroups;
        private bool _includeInactiveGrowers;
        private bool _includeOnHoldGrowers;
        private bool _includeZeroBalanceGrowers;

        // Available Options
        private ObservableCollection<string> _availableExportFormats;
        private ObservableCollection<string> _availableDateRangePresets;
        private ObservableCollection<string> _availableProvinces;
        private ObservableCollection<string> _availableCities;
        private ObservableCollection<string> _availablePaymentGroups;
        private ObservableCollection<string> _availableSortOptions;

        #endregion

        #region Constructor

        public PaymentSummaryReportViewModel()
        {
            // Initialize services
            var paymentTypeService = new PaymentTypeService();
            var receiptService = new ReceiptService(paymentTypeService);
            _reportService = new PaymentSummaryReportService(
                new GrowerService(),
                receiptService,
                new PaymentService(
                    receiptService,
                    new PriceService(),
                    new AccountService(),
                    new PaymentBatchService(),
                    new GrowerService(),
                    new ProcessClassificationService(),
                    paymentTypeService
                ),
                new AccountService()
            );
            _exportService = new ReportExportService();
            _growerService = new GrowerService();

            // Initialize collections
            _growerDetails = new ObservableCollection<GrowerPaymentDetail>();
            _paymentDistribution = new ObservableCollection<PaymentDistributionChart>();
            _monthlyTrends = new ObservableCollection<MonthlyTrendChart>();
            _topPerformers = new ObservableCollection<GrowerPerformanceChart>();
            _selectedGrowerIds = new ObservableCollection<int>();
            _selectedProvinces = new ObservableCollection<string>();
            _selectedCities = new ObservableCollection<string>();
            _selectedPaymentGroups = new ObservableCollection<string>();

            // Initialize available options
            InitializeAvailableOptions();

            // Initialize filter options
            InitializeFilterOptions();

            // Initialize commands
            InitializeCommands();

            // Set default values
            SetDefaultValues();

            // Load initial data
            LoadInitialDataAsync().ConfigureAwait(false);
        }

        #endregion

        #region Public Properties

        public PaymentSummaryReport CurrentReport
        {
            get => _currentReport;
            set
            {
                if (_currentReport != value)
                {
                    _currentReport = value;
                    OnPropertyChanged();
                }
            }
        }

        public ReportFilterOptions FilterOptions
        {
            get => _filterOptions;
            set
            {
                if (_filterOptions != value)
                {
                    _filterOptions = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<GrowerPaymentDetail> GrowerDetails
        {
            get => _growerDetails;
            set
            {
                if (_growerDetails != value)
                {
                    _growerDetails = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<PaymentDistributionChart> PaymentDistribution
        {
            get => _paymentDistribution;
            set
            {
                if (_paymentDistribution != value)
                {
                    _paymentDistribution = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MonthlyTrendChart> MonthlyTrends
        {
            get => _monthlyTrends;
            set
            {
                if (_monthlyTrends != value)
                {
                    _monthlyTrends = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<GrowerPerformanceChart> TopPerformers
        {
            get => _topPerformers;
            set
            {
                if (_topPerformers != value)
                {
                    _topPerformers = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region UI State Properties

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                if (_isExporting != value)
                {
                    _isExporting = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string SelectedExportFormat
        {
            get => _selectedExportFormat;
            set
            {
                if (_selectedExportFormat != value)
                {
                    _selectedExportFormat = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowContactInfo
        {
            get => _showContactInfo;
            set
            {
                if (_showContactInfo != value)
                {
                    _showContactInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowProductDetails
        {
            get => _showProductDetails;
            set
            {
                if (_showProductDetails != value)
                {
                    _showProductDetails = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowAuditInfo
        {
            get => _showAuditInfo;
            set
            {
                if (_showAuditInfo != value)
                {
                    _showAuditInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowCharts
        {
            get => _showCharts;
            set
            {
                if (_showCharts != value)
                {
                    _showCharts = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowSummaryStatistics
        {
            get => _showSummaryStatistics;
            set
            {
                if (_showSummaryStatistics != value)
                {
                    _showSummaryStatistics = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public double ExportProgress
        {
            get => _exportProgress;
            set
            {
                if (_exportProgress != value)
                {
                    _exportProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Filter Properties

        public DateTime PeriodStart
        {
            get => _periodStart;
            set
            {
                if (_periodStart != value)
                {
                    _periodStart = value;
                    OnPropertyChanged();
                    FilterOptions.PeriodStart = value;
                }
            }
        }

        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set
            {
                if (_periodEnd != value)
                {
                    _periodEnd = value;
                    OnPropertyChanged();
                    FilterOptions.PeriodEnd = value;
                }
            }
        }

        public string SelectedDateRangePreset
        {
            get => _selectedDateRangePreset;
            set
            {
                if (_selectedDateRangePreset != value)
                {
                    _selectedDateRangePreset = value;
                    OnPropertyChanged();
                    ApplyDateRangePreset(value);
                }
            }
        }

        public ObservableCollection<int> SelectedGrowerIds
        {
            get => _selectedGrowerIds;
            set
            {
                if (_selectedGrowerIds != value)
                {
                    _selectedGrowerIds = value;
                    OnPropertyChanged();
                    FilterOptions.SelectedGrowerIds = value.ToList();
                }
            }
        }

        public ObservableCollection<string> SelectedProvinces
        {
            get => _selectedProvinces;
            set
            {
                if (_selectedProvinces != value)
                {
                    _selectedProvinces = value;
                    OnPropertyChanged();
                    FilterOptions.SelectedProvinces = value.ToList();
                }
            }
        }

        public ObservableCollection<string> SelectedCities
        {
            get => _selectedCities;
            set
            {
                if (_selectedCities != value)
                {
                    _selectedCities = value;
                    OnPropertyChanged();
                    FilterOptions.SelectedCities = value.ToList();
                }
            }
        }

        public ObservableCollection<string> SelectedPaymentGroups
        {
            get => _selectedPaymentGroups;
            set
            {
                if (_selectedPaymentGroups != value)
                {
                    _selectedPaymentGroups = value;
                    OnPropertyChanged();
                    FilterOptions.SelectedPaymentGroups = value.ToList();
                }
            }
        }

        public bool IncludeInactiveGrowers
        {
            get => _includeInactiveGrowers;
            set
            {
                if (_includeInactiveGrowers != value)
                {
                    _includeInactiveGrowers = value;
                    OnPropertyChanged();
                    FilterOptions.IncludeInactiveGrowers = value;
                }
            }
        }

        public bool IncludeOnHoldGrowers
        {
            get => _includeOnHoldGrowers;
            set
            {
                if (_includeOnHoldGrowers != value)
                {
                    _includeOnHoldGrowers = value;
                    OnPropertyChanged();
                    FilterOptions.IncludeOnHoldGrowers = value;
                }
            }
        }

        public bool IncludeZeroBalanceGrowers
        {
            get => _includeZeroBalanceGrowers;
            set
            {
                if (_includeZeroBalanceGrowers != value)
                {
                    _includeZeroBalanceGrowers = value;
                    OnPropertyChanged();
                    FilterOptions.IncludeZeroBalanceGrowers = value;
                }
            }
        }

        #endregion

        #region Available Options

        public ObservableCollection<string> AvailableExportFormats
        {
            get => _availableExportFormats;
            set
            {
                if (_availableExportFormats != value)
                {
                    _availableExportFormats = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailableDateRangePresets
        {
            get => _availableDateRangePresets;
            set
            {
                if (_availableDateRangePresets != value)
                {
                    _availableDateRangePresets = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailableProvinces
        {
            get => _availableProvinces;
            set
            {
                if (_availableProvinces != value)
                {
                    _availableProvinces = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailableCities
        {
            get => _availableCities;
            set
            {
                if (_availableCities != value)
                {
                    _availableCities = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailablePaymentGroups
        {
            get => _availablePaymentGroups;
            set
            {
                if (_availablePaymentGroups != value)
                {
                    _availablePaymentGroups = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> AvailableSortOptions
        {
            get => _availableSortOptions;
            set
            {
                if (_availableSortOptions != value)
                {
                    _availableSortOptions = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand GenerateReportCommand { get; private set; }
        public ICommand ExportReportCommand { get; private set; }
        public ICommand RefreshDataCommand { get; private set; }
        public ICommand ClearFiltersCommand { get; private set; }
        public ICommand ToggleColumnVisibilityCommand { get; private set; }
        public ICommand ApplyFiltersCommand { get; private set; }
        
        // Navigation Commands
        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand NavigateToReportsCommand { get; private set; } = null!;

        #endregion

        #region Command Implementations

        private async Task GenerateReport(object parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Generating report...";

                var report = await _reportService.GenerateReportAsync(FilterOptions);
                
                CurrentReport = report;
                GrowerDetails = new ObservableCollection<GrowerPaymentDetail>(report.GrowerDetails);
                PaymentDistribution = new ObservableCollection<PaymentDistributionChart>(report.PaymentDistribution);
                MonthlyTrends = new ObservableCollection<MonthlyTrendChart>(report.MonthlyTrends);
                TopPerformers = new ObservableCollection<GrowerPerformanceChart>(report.TopPerformers);

                StatusMessage = $"Report generated successfully with {report.TotalGrowers} growers";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating report: {ex.Message}", ex);
                StatusMessage = $"Error generating report: {ex.Message}";
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanGenerateReport(object parameter)
        {
            return !IsLoading && !IsExporting;
        }

        private async void ExportReport(object parameter)
        {
            if (CurrentReport == null)
            {
                MessageBox.Show("No report data available to export. Please generate a report first.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsExporting = true;
                ExportProgress = 0;
                StatusMessage = "Preparing export...";

                var exportOptions = new ExportOptions
                {
                    ExportFormat = SelectedExportFormat,
                    IncludeCharts = ShowCharts,
                    IncludeSummaryStatistics = ShowSummaryStatistics,
                    IncludeDetailedData = true,
                    IncludeContactInfo = ShowContactInfo,
                    IncludeProductDetails = ShowProductDetails,
                    IncludeAuditInfo = ShowAuditInfo
                };

                exportOptions.SetDefaultFileName($"PaymentSummaryReport_{DateTime.Now:yyyyMMdd_HHmmss}");

                ExportProgress = 25;
                StatusMessage = $"Exporting to {SelectedExportFormat}...";

                switch (SelectedExportFormat.ToUpper())
                {
                    case "PDF":
                        await _exportService.ExportToPdfAsync(CurrentReport, exportOptions);
                        break;
                    case "EXCEL":
                        await _exportService.ExportToExcelAsync(CurrentReport, exportOptions);
                        break;
                    case "CSV":
                        await _exportService.ExportToCsvAsync(CurrentReport, exportOptions);
                        break;
                    case "WORD":
                        await _exportService.ExportToWordAsync(CurrentReport, exportOptions);
                        break;
                    default:
                        throw new ArgumentException($"Unsupported export format: {SelectedExportFormat}");
                }

                ExportProgress = 100;
                StatusMessage = $"Export completed successfully";

                if (exportOptions.OpenAfterExport)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exportOptions.GetFullFilePath(),
                        UseShellExecute = true
                    });
                }

                MessageBox.Show($"Report exported successfully to:\n{exportOptions.GetFullFilePath()}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error exporting report: {ex.Message}", ex);
                StatusMessage = $"Export failed: {ex.Message}";
                MessageBox.Show($"Error exporting report: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsExporting = false;
                ExportProgress = 0;
            }
        }

        private bool CanExportReport(object parameter)
        {
            return !IsLoading && !IsExporting && CurrentReport != null;
        }

        private async Task RefreshData(object parameter)
        {
            await GenerateReport(parameter);
        }

        private void ClearFilters(object parameter)
        {
            FilterOptions.ClearAllFilters();
            SelectedDateRangePreset = "Last Month";
            IncludeInactiveGrowers = false;
            IncludeOnHoldGrowers = false;
            IncludeZeroBalanceGrowers = true;
            ShowContactInfo = true;
            ShowProductDetails = true;
            ShowAuditInfo = false;
            ShowCharts = true;
            ShowSummaryStatistics = true;
        }

        private void ToggleColumnVisibility(object parameter)
        {
            if (parameter is string columnName)
            {
                switch (columnName.ToLower())
                {
                    case "contact":
                        ShowContactInfo = !ShowContactInfo;
                        break;
                    case "product":
                        ShowProductDetails = !ShowProductDetails;
                        break;
                    case "audit":
                        ShowAuditInfo = !ShowAuditInfo;
                        break;
                    case "charts":
                        ShowCharts = !ShowCharts;
                        break;
                    case "summary":
                        ShowSummaryStatistics = !ShowSummaryStatistics;
                        break;
                }
            }
        }

        private async Task ApplyFilters(object parameter)
        {
            await GenerateReport(parameter);
        }

        private void NavigateToDashboardExecute(object? parameter)
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (mainViewModel.NavigateToDashboardCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToDashboardCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error silently or log it
                Logger.Error($"Error navigating to Dashboard: {ex.Message}", ex);
            }
        }

        private void NavigateToReportsExecute(object? parameter)
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (mainViewModel.NavigateToReportsCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToReportsCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error silently or log it
                Logger.Error($"Error navigating to Reports: {ex.Message}", ex);
            }
        }

        #endregion

        #region Private Helper Methods

        private void InitializeAvailableOptions()
        {
            AvailableExportFormats = new ObservableCollection<string> { "PDF", "Excel", "CSV", "Word" };
            AvailableDateRangePresets = new ObservableCollection<string> 
            { 
                "This Month", "Last Month", "This Quarter", "This Year", "Last Year", "Custom" 
            };
            AvailableSortOptions = new ObservableCollection<string>
            {
                "Grower Name", "Grower Number", "Total Payments", "Outstanding Balance", 
                "Province", "City", "Payment Status"
            };
        }

        private void InitializeFilterOptions()
        {
            FilterOptions = new ReportFilterOptions();
            FilterOptions.SetDateRangePreset("Last Month");
        }

        private void InitializeCommands()
        {
            GenerateReportCommand = new RelayCommand(GenerateReport, CanGenerateReport);
            ExportReportCommand = new RelayCommand(ExportReport, CanExportReport);
            RefreshDataCommand = new RelayCommand(RefreshData, CanGenerateReport);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ToggleColumnVisibilityCommand = new RelayCommand(ToggleColumnVisibility);
            ApplyFiltersCommand = new RelayCommand(ApplyFilters, CanGenerateReport);
            
            // Navigation Commands
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToReportsCommand = new RelayCommand(NavigateToReportsExecute);
        }

        private void SetDefaultValues()
        {
            SelectedExportFormat = "PDF";
            ShowContactInfo = true;
            ShowProductDetails = true;
            ShowAuditInfo = false;
            ShowCharts = true;
            ShowSummaryStatistics = true;
            IncludeInactiveGrowers = false;
            IncludeOnHoldGrowers = false;
            IncludeZeroBalanceGrowers = true;
            SelectedDateRangePreset = "Last Month";
            StatusMessage = "Ready to generate report";
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading initial data...";

                // Load available filter options
                await LoadAvailableFilterOptionsAsync();

                // Generate initial report
                await GenerateReport(null);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading initial data: {ex.Message}", ex);
                StatusMessage = $"Error loading data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadAvailableFilterOptionsAsync()
        {
            try
            {
                var growers = await _growerService.GetAllGrowersAsync();
                
                var provinces = growers.Where(g => !string.IsNullOrEmpty(g.Prov))
                                      .Select(g => g.Prov)
                                      .Distinct()
                                      .OrderBy(p => p)
                                      .ToList();

                var cities = growers.Where(g => !string.IsNullOrEmpty(g.City))
                                  .Select(g => g.City)
                                  .Distinct()
                                  .OrderBy(c => c)
                                  .ToList();

                AvailableProvinces = new ObservableCollection<string>(provinces);
                AvailableCities = new ObservableCollection<string>(cities);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading filter options: {ex.Message}", ex);
            }
        }

        private void ApplyDateRangePreset(string preset)
        {
            FilterOptions.SetDateRangePreset(preset);
            PeriodStart = FilterOptions.PeriodStart;
            PeriodEnd = FilterOptions.PeriodEnd;
        }

        #endregion
    }
}
