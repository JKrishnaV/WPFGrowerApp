using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace WPFGrowerApp.ViewModels
{
    public class ReportsCardsViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _isLoading;

        public ReportsCardsViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            InitializeCommands();
            InitializeReports();
        }

        private void InitializeCommands()
        {
            NavigateToReportCommand = new RelayCommand(NavigateToReport, CanNavigateToReport);
        }

        private void InitializeReports()
        {
            IsLoading = true;
            
            try
            {
                // Analytics Reports
                AnalyticsReports = new ObservableCollection<ReportCard>
                {
                    new ReportCard
                    {
                        Title = "Province Distribution",
                        Description = "Visual pie chart showing grower distribution across provinces",
                        IconKind = "ChartPie",
                        IconColor = "#4CAF50",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.PieChart,
                        ReportSubType = "Province"
                    },
                    new ReportCard
                    {
                        Title = "Price Level Distribution",
                        Description = "Chart showing distribution of price levels across growers",
                        IconKind = "ChartPie",
                        IconColor = "#2196F3",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.PieChart,
                        ReportSubType = "PriceLevel"
                    },
                    new ReportCard
                    {
                        Title = "Pay Group Distribution",
                        Description = "Visual breakdown of payment groups and their distribution",
                        IconKind = "ChartPie",
                        IconColor = "#FF9800",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.PieChart,
                        ReportSubType = "PayGroup"
                    },
                    new ReportCard
                    {
                        Title = "Grower Performance Chart",
                        Description = "Performance metrics and trends for individual growers",
                        IconKind = "ChartLine",
                        IconColor = "#9C27B0",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.GrowerPerformance
                    },
                    new ReportCard
                    {
                        Title = "Monthly Trend Analysis",
                        Description = "Monthly production and payment trends over time",
                        IconKind = "ChartLine",
                        IconColor = "#E91E63",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.MonthlyTrend
                    },
                    new ReportCard
                    {
                        Title = "Payment Distribution Chart",
                        Description = "Visual analysis of payment distributions and patterns",
                        IconKind = "ChartBar",
                        IconColor = "#607D8B",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.PaymentDistribution
                    }
                };

                // Financial Reports
                FinancialReports = new ObservableCollection<ReportCard>
                {
                    new ReportCard
                    {
                        Title = "Payment Summary Report",
                        Description = "Comprehensive summary of all payments and financial transactions",
                        IconKind = "CashMultiple",
                        IconColor = "#4CAF50",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.PaymentSummary
                    },
                    new ReportCard
                    {
                        Title = "Payment Test & Validation",
                        Description = "Test payment calculations and validate payment accuracy",
                        IconKind = "CheckCircle",
                        IconColor = "#FF9800",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.PaymentTest
                    },
                    new ReportCard
                    {
                        Title = "Grower Detail Report",
                        Description = "Detailed financial report for individual growers",
                        IconKind = "AccountDetails",
                        IconColor = "#2196F3",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.GrowerDetail
                    }
                };

                // Operational Reports
                OperationalReports = new ObservableCollection<ReportCard>
                {
                    new ReportCard
                    {
                        Title = "Receipt Analysis",
                        Description = "Analysis of receipt patterns and processing efficiency",
                        IconKind = "FileDocumentOutline",
                        IconColor = "#795548",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.ReceiptAnalysis
                    },
                    new ReportCard
                    {
                        Title = "Import Batch Report",
                        Description = "Report on import batches and data processing status",
                        IconKind = "Import",
                        IconColor = "#607D8B",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.ImportBatch
                    },
                    new ReportCard
                    {
                        Title = "System Audit Report",
                        Description = "Comprehensive system audit and data integrity report",
                        IconKind = "ShieldCheck",
                        IconColor = "#9C27B0",
                        StatusText = "Available",
                        StatusColor = "#4CAF50",
                        ReportType = ReportType.SystemAudit
                    }
                };

                Logger.Info("Reports cards initialized successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing reports cards", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ObservableCollection<ReportCard> AnalyticsReports { get; private set; }
        public ObservableCollection<ReportCard> FinancialReports { get; private set; }
        public ObservableCollection<ReportCard> OperationalReports { get; private set; }

        public ICommand NavigateToReportCommand { get; private set; }

        private bool CanNavigateToReport(object report)
        {
            return report != null && !IsLoading;
        }

        private void NavigateToReport(object report)
        {
            if (report is ReportCard reportCard)
            {
                NavigateToReportInternal(reportCard);
            }
        }

        private void NavigateToReportInternal(ReportCard report)
        {
            if (report == null) return;

            try
            {
                Logger.Info($"Navigating to report: {report.Title}");

                // Get the ReportsHostViewModel from the service provider
                var reportsHostViewModel = _serviceProvider.GetRequiredService<ReportsHostViewModel>();
                
                // Navigate to the specific report within the ReportsHostViewModel
                reportsHostViewModel.NavigateToSpecificReport(report);

                Logger.Info($"Successfully navigated to report: {report.Title}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to report {report.Title}", ex);
            }
        }
    }

    public class ReportCard
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconKind { get; set; }
        public string IconColor { get; set; }
        public string StatusText { get; set; }
        public string StatusColor { get; set; }
        public ReportType ReportType { get; set; }
        public string ReportSubType { get; set; }
    }

    public enum ReportType
    {
        PieChart,
        GrowerDetail,
        PaymentSummary,
        PaymentTest,
        GrowerPerformance,
        MonthlyTrend,
        PaymentDistribution,
        ReceiptAnalysis,
        ImportBatch,
        SystemAudit
    }
}
