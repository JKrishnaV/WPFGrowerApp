using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for payment reconciliation operations.
    /// Manages reconciliation reports and exception resolution.
    /// </summary>
    public class PaymentReconciliationViewModel : ViewModelBase
    {
        private readonly IPaymentReconciliationService _reconciliationService;
        private readonly IPaymentDistributionService _paymentDistributionService;

        private ObservableCollection<PaymentDistribution> _distributionsForReconciliation = new();
        private ReconciliationReport? _currentReport;
        private bool _isReconciling;

        public PaymentReconciliationViewModel(
            IPaymentReconciliationService reconciliationService,
            IPaymentDistributionService paymentDistributionService)
        {
            _reconciliationService = reconciliationService;
            _paymentDistributionService = paymentDistributionService;
            InitializeCommands();
            _ = LoadDistributionsAsync();
        }

        #region Properties

        public ObservableCollection<PaymentDistribution> DistributionsForReconciliation
        {
            get => _distributionsForReconciliation;
            set => SetProperty(ref _distributionsForReconciliation, value);
        }

        public ReconciliationReport? CurrentReport
        {
            get => _currentReport;
            set => SetProperty(ref _currentReport, value);
        }

        public bool IsReconciling
        {
            get => _isReconciling;
            set => SetProperty(ref _isReconciling, value);
        }

        #endregion

        #region Commands

        public ICommand ReconcileDistributionCommand { get; private set; } = null!;
        public ICommand GenerateReportCommand { get; private set; } = null!;
        public ICommand ExportReportCommand { get; private set; } = null!;
        public ICommand MarkAsCompleteCommand { get; private set; } = null!;
        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand NavigateToPaymentManagementCommand { get; private set; } = null!;

        #endregion

        #region Command Implementations

        private async Task LoadDistributionsAsync()
        {
            try
            {
                var distributions = await _paymentDistributionService.GetAllDistributionsAsync();
                DistributionsForReconciliation.Clear();
                
                foreach (var distribution in distributions.Where(d => d.Status == "Finalized"))
                {
                    DistributionsForReconciliation.Add(distribution);
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task ReconcileDistributionAsync(object parameter)
        {
            try
            {
                if (parameter is PaymentDistribution distribution)
                {
                    IsReconciling = true;
                    CurrentReport = await _reconciliationService.ReconcileDistributionAsync(distribution.PaymentDistributionId);
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
            finally
            {
                IsReconciling = false;
            }
        }

        private async Task GenerateReportAsync(object parameter)
        {
            // Implementation for generating reconciliation reports
            await Task.CompletedTask;
        }

        private async Task ExportReportAsync(object parameter)
        {
            // Implementation for exporting reports
            await Task.CompletedTask;
        }

        private async Task MarkAsCompleteAsync(object parameter)
        {
            try
            {
                if (parameter is PaymentDistribution distribution)
                {
                    await _reconciliationService.MarkDistributionAsCompletedAsync(
                        distribution.PaymentDistributionId, 
                        App.CurrentUser?.Username ?? "SYSTEM");
                    
                    await LoadDistributionsAsync();
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        #endregion

        #region Helper Methods

        private void InitializeCommands()
        {
            ReconcileDistributionCommand = new RelayCommand(async (param) => await ReconcileDistributionAsync(param));
            GenerateReportCommand = new RelayCommand(async (param) => await GenerateReportAsync(param));
            ExportReportCommand = new RelayCommand(async (param) => await ExportReportAsync(param));
            MarkAsCompleteCommand = new RelayCommand(async (param) => await MarkAsCompleteAsync(param));
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToPaymentManagementCommand = new RelayCommand(NavigateToPaymentManagementExecute);
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
                System.Diagnostics.Debug.WriteLine($"Error navigating to Dashboard: {ex.Message}");
            }
        }

        private void NavigateToPaymentManagementExecute(object? parameter)
        {
            try
            {
                if (System.Windows.Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (mainViewModel.NavigateToPaymentManagementCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToPaymentManagementCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle error silently or log it
                System.Diagnostics.Debug.WriteLine($"Error navigating to Payment Management: {ex.Message}");
            }
        }

        #endregion
    }
}
