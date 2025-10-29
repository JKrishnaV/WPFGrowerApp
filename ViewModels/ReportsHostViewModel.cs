using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    public class ReportsHostViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;
        private ViewModelBase _currentReportViewModel;

        // Navigation event for replacing the entire view
        public static event Action<Type, string>? NavigationRequested;

        public ViewModelBase CurrentReportViewModel
        {
            get => _currentReportViewModel;
            set 
            {
                Logger.Info($"CurrentReportViewModel setter called - Old: {_currentReportViewModel?.GetType().Name}, New: {value?.GetType().Name}");
                SetProperty(ref _currentReportViewModel, value);
                Logger.Info($"SetProperty completed - CurrentReportViewModel is now: {_currentReportViewModel?.GetType().Name}");
                
                // Manually trigger PropertyChanged to ensure it's fired
                OnPropertyChanged(nameof(CurrentReportViewModel));
                Logger.Info($"Manual OnPropertyChanged called for CurrentReportViewModel");
            }
        }

        // Commands for navigating between report sub-views
        public ICommand NavigateToCardsCommand { get; }

        public ReportsHostViewModel(IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            NavigateToCardsCommand = new RelayCommand(ExecuteNavigateToCards, CanNavigate);

            // Set default view to show the cards view
            ExecuteNavigateToCards(null);
        }

        private bool CanNavigate(object parameter)
        {
            // Add logic here if navigation should sometimes be disabled
            return true;
        }

        private void ExecuteNavigateToCards(object parameter)
        {
            try
            {
                // Resolve the new ReportsCardsViewModel
                CurrentReportViewModel = _serviceProvider.GetRequiredService<ReportsCardsViewModel>();
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Reports Cards", ex);
            }
        }

        /// <summary>
        /// Navigate to a specific report based on the ReportCard
        /// </summary>
        public void NavigateToSpecificReport(ReportCard report)
        {
            if (report == null) return;

            try
            {
                Logger.Info($"Navigating to specific report: {report.Title}");

                switch (report.ReportType)
                {
                    case ReportType.PieChart:
                        NavigateToPieChartReport(report.ReportSubType);
                        break;
                    case ReportType.GrowerDetail:
                        NavigateToGrowerDetailReport();
                        break;
                    case ReportType.PaymentSummary:
                        NavigateToPaymentSummaryReport();
                        break;
                    case ReportType.PaymentTest:
                        NavigateToPaymentTestReport();
                        break;
                    case ReportType.GrowerPerformance:
                        NavigateToGrowerPerformanceReport();
                        break;
                    case ReportType.MonthlyTrend:
                        NavigateToMonthlyTrendReport();
                        break;
                    case ReportType.PaymentDistribution:
                        NavigateToPaymentDistributionReport();
                        break;
                    default:
                        Logger.Warn($"Unknown report type: {report.ReportType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to specific report {report.Title}", ex);
            }
        }

        private void NavigateToPieChartReport(string subType)
        {
            try
            {
                var reportsViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
                CurrentReportViewModel = reportsViewModel;
                
                // Set the specific report type based on subType after DataContext is established
                switch (subType)
                {
                    case "Province":
                        reportsViewModel.SelectedReportType = "Province Distribution";
                        break;
                    case "PriceLevel":
                        reportsViewModel.SelectedReportType = "Price Level Distribution";
                        break;
                    case "PayGroup":
                        reportsViewModel.SelectedReportType = "Pay Group Distribution";
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to Pie Chart report ({subType})", ex);
            }
        }

        private void NavigateToGrowerDetailReport()
        {
            try
            {
                var growerReportViewModel = _serviceProvider.GetRequiredService<GrowerReportViewModel>();
                CurrentReportViewModel = growerReportViewModel;
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Grower Detail Report", ex);
            }
        }

        private void NavigateToPaymentSummaryReport()
        {
            try
            {
                Logger.Info("Starting navigation to Payment Summary Report");
                // Instead of setting CurrentReportViewModel, trigger navigation to replace the entire view
                NavigationRequested?.Invoke(typeof(PaymentSummaryReportViewModel), "Payment Summary Report");
                Logger.Info("Navigation event triggered for Payment Summary Report");
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Payment Summary Report", ex);
            }
        }

        private void NavigateToPaymentTestReport()
        {
            try
            {
                var reportsViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
                CurrentReportViewModel = reportsViewModel;
                reportsViewModel.SelectedReportType = "Payment Summary Test & Validation";
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Payment Test Report", ex);
            }
        }

        private void NavigateToGrowerPerformanceReport()
        {
            try
            {
                // For now, navigate to the main reports view
                // This could be enhanced to show a specific grower performance view
                var reportsViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
                CurrentReportViewModel = reportsViewModel;
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Grower Performance Report", ex);
            }
        }

        private void NavigateToMonthlyTrendReport()
        {
            try
            {
                // For now, navigate to the main reports view
                // This could be enhanced to show a specific monthly trend view
                var reportsViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
                CurrentReportViewModel = reportsViewModel;
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Monthly Trend Report", ex);
            }
        }

        private void NavigateToPaymentDistributionReport()
        {
             try
            {
                // For now, navigate to the main reports view
                // This could be enhanced to show a specific payment distribution view
                var reportsViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
                CurrentReportViewModel = reportsViewModel;
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Payment Distribution Report", ex);
            }
        }
    }
}
