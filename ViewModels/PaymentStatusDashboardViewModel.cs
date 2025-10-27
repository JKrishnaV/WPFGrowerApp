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
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for the payment status dashboard.
    /// Provides overview of all payment processing activities.
    /// </summary>
    public class PaymentStatusDashboardViewModel : ViewModelBase
    {
        private readonly IChequeService _chequeService;
        private readonly IElectronicPaymentService _electronicPaymentService;
        private readonly IPaymentDistributionService _paymentDistributionService;
        private readonly IChequeDeliveryService _chequeDeliveryService;

        private PaymentStatusSummary _statusSummary = new();
        private ObservableCollection<PaymentDistribution> _recentDistributions = new();
        private ObservableCollection<Cheque> _pendingCheques = new();
        private ObservableCollection<ElectronicPayment> _pendingElectronicPayments = new();
        private ObservableCollection<ChequeDelivery> _recentDeliveries = new();

        public PaymentStatusDashboardViewModel(
            IChequeService chequeService,
            IElectronicPaymentService electronicPaymentService,
            IPaymentDistributionService paymentDistributionService,
            IChequeDeliveryService chequeDeliveryService)
        {
            _chequeService = chequeService;
            _electronicPaymentService = electronicPaymentService;
            _paymentDistributionService = paymentDistributionService;
            _chequeDeliveryService = chequeDeliveryService;
            InitializeCommands();
            _ = LoadDashboardDataAsync();
        }

        #region Properties

        public PaymentStatusSummary StatusSummary
        {
            get => _statusSummary;
            set => SetProperty(ref _statusSummary, value);
        }

        public ObservableCollection<PaymentDistribution> RecentDistributions
        {
            get => _recentDistributions;
            set => SetProperty(ref _recentDistributions, value);
        }

        public ObservableCollection<Cheque> PendingCheques
        {
            get => _pendingCheques;
            set => SetProperty(ref _pendingCheques, value);
        }

        public ObservableCollection<ElectronicPayment> PendingElectronicPayments
        {
            get => _pendingElectronicPayments;
            set => SetProperty(ref _pendingElectronicPayments, value);
        }

        public ObservableCollection<ChequeDelivery> RecentDeliveries
        {
            get => _recentDeliveries;
            set => SetProperty(ref _recentDeliveries, value);
        }

        #endregion

        #region Commands

        public ICommand RefreshDashboardCommand { get; private set; } = null!;
        public ICommand ViewDistributionDetailsCommand { get; private set; } = null!;
        public ICommand ViewChequeDetailsCommand { get; private set; } = null!;
        public ICommand ViewElectronicPaymentDetailsCommand { get; private set; } = null!;
        public ICommand ViewDeliveryDetailsCommand { get; private set; } = null!;
        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand NavigateToPaymentManagementCommand { get; private set; } = null!;

        #endregion

        #region Command Implementations

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                await LoadStatusSummaryAsync();
                await LoadRecentDistributionsAsync();
                await LoadPendingChequesAsync();
                await LoadPendingElectronicPaymentsAsync();
                await LoadRecentDeliveriesAsync();
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadStatusSummaryAsync()
        {
            try
            {
                var distributions = await _paymentDistributionService.GetAllDistributionsAsync();
                var cheques = await _chequeService.GetAllChequesAsync();
                var electronicPayments = await _electronicPaymentService.GetAllElectronicPaymentsAsync();

                StatusSummary = new PaymentStatusSummary
                {
                    TotalDistributions = distributions.Count(),
                    TotalCheques = cheques.Count(),
                    TotalElectronicPayments = electronicPayments.Count(),
                    ChequesGenerated = cheques.Count(c => c.Status == "Generated"),
                    ChequesPrinted = cheques.Count(c => c.Status == "Printed"),
                    ChequesIssued = cheques.Count(c => c.Status == "Delivered"),
                    ChequesCleared = cheques.Count(c => c.Status == "Delivered"),
                    ElectronicPaymentsGenerated = electronicPayments.Count(ep => ep.Status == "Generated"),
                    ElectronicPaymentsProcessed = electronicPayments.Count(ep => ep.Status == "Processed"),
                    ElectronicPaymentsFailed = electronicPayments.Count(ep => ep.Status == "Failed")
                };
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadRecentDistributionsAsync()
        {
            try
            {
                var distributions = await _paymentDistributionService.GetAllDistributionsAsync();
                RecentDistributions.Clear();
                
                foreach (var distribution in distributions
                    .OrderByDescending(d => d.DistributionDate)
                    .Take(10))
                {
                    RecentDistributions.Add(distribution);
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadPendingChequesAsync()
        {
            try
            {
                var cheques = await _chequeService.GetChequesByStatusAsync("Generated");
                PendingCheques.Clear();
                
                foreach (var cheque in cheques.Take(10))
                {
                    PendingCheques.Add(cheque);
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadPendingElectronicPaymentsAsync()
        {
            try
            {
                var payments = await _electronicPaymentService.GetPendingElectronicPaymentsAsync();
                PendingElectronicPayments.Clear();
                
                foreach (var payment in payments.Take(10))
                {
                    PendingElectronicPayments.Add(payment);
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task LoadRecentDeliveriesAsync()
        {
            try
            {
                var deliveries = await _chequeDeliveryService.GetAllDeliveriesAsync();
                RecentDeliveries.Clear();
                
                foreach (var delivery in deliveries
                    .OrderByDescending(d => d.CreatedAt)
                    .Take(10))
                {
                    RecentDeliveries.Add(delivery);
                }
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }

        private async Task RefreshDashboardAsync(object parameter)
        {
            await LoadDashboardDataAsync();
        }

        private async Task ViewDistributionDetailsAsync(object parameter)
        {
            // Implementation for viewing distribution details
            await Task.CompletedTask;
        }

        private async Task ViewChequeDetailsAsync(object parameter)
        {
            // Implementation for viewing cheque details
            await Task.CompletedTask;
        }

        private async Task ViewElectronicPaymentDetailsAsync(object parameter)
        {
            // Implementation for viewing electronic payment details
            await Task.CompletedTask;
        }

        private async Task ViewDeliveryDetailsAsync(object parameter)
        {
            // Implementation for viewing delivery details
            await Task.CompletedTask;
        }

        #endregion

        #region Helper Methods

        private void InitializeCommands()
        {
            RefreshDashboardCommand = new RelayCommand(async (param) => await RefreshDashboardAsync(param));
            ViewDistributionDetailsCommand = new RelayCommand(async (param) => await ViewDistributionDetailsAsync(param));
            ViewChequeDetailsCommand = new RelayCommand(async (param) => await ViewChequeDetailsAsync(param));
            ViewElectronicPaymentDetailsCommand = new RelayCommand(async (param) => await ViewElectronicPaymentDetailsAsync(param));
            ViewDeliveryDetailsCommand = new RelayCommand(async (param) => await ViewDeliveryDetailsAsync(param));
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
                Logger.Error($"Error navigating to Dashboard: {ex.Message}", ex);
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
                Logger.Error($"Error navigating to Payment Management: {ex.Message}", ex);
            }
        }

        #endregion
    }
}