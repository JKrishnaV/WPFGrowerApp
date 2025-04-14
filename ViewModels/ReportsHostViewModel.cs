using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services; // Assuming IDialogService might be needed later

namespace WPFGrowerApp.ViewModels
{
    public class ReportsHostViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService; // Keep for potential future use
        private ViewModelBase _currentReportViewModel;

        public ViewModelBase CurrentReportViewModel
        {
            get => _currentReportViewModel;
            set => SetProperty(ref _currentReportViewModel, value);
        }

        // Commands for navigating between report sub-views
        public ICommand NavigateToGrowerReportCommand { get; }
        public ICommand NavigateToTestReportsCommand { get; }

        public ReportsHostViewModel(IServiceProvider serviceProvider, IDialogService dialogService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            NavigateToGrowerReportCommand = new RelayCommand(ExecuteNavigateToGrowerReport, CanNavigate);
            NavigateToTestReportsCommand = new RelayCommand(ExecuteNavigateToTestReports, CanNavigate);

            // Set default view
            ExecuteNavigateToGrowerReport(null); // Or TestReports, depending on desired default
        }

        private bool CanNavigate(object parameter)
        {
            // Add logic here if navigation should sometimes be disabled
            return true;
        }

        private void ExecuteNavigateToGrowerReport(object parameter)
        {
            try
            {
                // Resolve the GrowerReportViewModel using DI
                // Note: GrowerReportViewModel doesn't exist yet, this will cause an error until it's created and registered
                CurrentReportViewModel = _serviceProvider.GetRequiredService<GrowerReportViewModel>();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error navigating to Grower Report", ex);
                // Consider showing an error message via _dialogService if appropriate
                // await _dialogService.ShowMessageBoxAsync($"Error navigating to Grower Report: {ex.Message}", "Navigation Error");
            }
        }

        private void ExecuteNavigateToTestReports(object parameter)
        {
             try
            {
                // Resolve the existing ReportsViewModel (assuming it's registered)
                CurrentReportViewModel = _serviceProvider.GetRequiredService<ReportsViewModel>();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error navigating to Test Reports", ex);
                // Consider showing an error message via _dialogService if appropriate
                // await _dialogService.ShowMessageBoxAsync($"Error navigating to Test Reports: {ex.Message}", "Navigation Error");
            }
        }
    }
}
