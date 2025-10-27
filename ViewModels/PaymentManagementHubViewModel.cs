using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using System.Collections.Generic;
using System.Linq;

namespace WPFGrowerApp.ViewModels
{
    public class PaymentManagementHubViewModel : ViewModelBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IChequeService _chequeService;
        private readonly IElectronicPaymentService _electronicPaymentService;
        private readonly IPaymentBatchService _paymentBatchService;
        private readonly IServiceProvider _serviceProvider;

        // Static event for navigation requests
        public static event Action<Type, string>? NavigationRequested;

        private bool _isLoading;
        private string _statusMessage;
        private int _pendingPayments;
        private int _activeBatches;
        private int _chequesReady;
        private int _electronicPayments;
        private ObservableCollection<RecentActivityItem> _recentActivities;

        public PaymentManagementHubViewModel(
            IPaymentService paymentService,
            IChequeService chequeService,
            IElectronicPaymentService electronicPaymentService,
            IPaymentBatchService paymentBatchService,
            IServiceProvider serviceProvider)
        {
            _paymentService = paymentService;
            _chequeService = chequeService;
            _electronicPaymentService = electronicPaymentService;
            _paymentBatchService = paymentBatchService;
            _serviceProvider = serviceProvider;

            // Initialize commands
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard);
            NavigateToPaymentRunCommand = new RelayCommand(NavigateToPaymentRun);
            NavigateToPaymentGroupsCommand = new RelayCommand(NavigateToPaymentGroups);
            NavigateToDistributionCommand = new RelayCommand(NavigateToDistribution);
            NavigateToBatchesCommand = new RelayCommand(NavigateToBatches);
            NavigateToChequePreparationCommand = new RelayCommand(NavigateToChequePreparation);
            NavigateToChequeDeliveryCommand = new RelayCommand(NavigateToChequeDelivery);
            NavigateToChequeReviewCommand = new RelayCommand(NavigateToChequeReview);
            NavigateToElectronicPaymentsCommand = new RelayCommand(NavigateToElectronicPayments);
            NavigateToFinalPaymentsCommand = new RelayCommand(NavigateToFinalPayments);
            NavigateToStatusDashboardCommand = new RelayCommand(NavigateToStatusDashboard);
            NavigateToReconciliationCommand = new RelayCommand(NavigateToReconciliation);
            NavigateToAdvanceChequesCommand = new RelayCommand(NavigateToAdvanceCheques);
            NavigateToEnhancedPaymentDistributionCommand = new RelayCommand(NavigateToEnhancedPaymentDistribution);
            NavigateToEnhancedChequePreparationCommand = new RelayCommand(NavigateToEnhancedChequePreparation);
            RefreshCommand = new RelayCommand(async (p) => await RefreshDataAsync());
            ShowHelpCommand = new RelayCommand(ShowHelp);

            // Initialize properties
            StatusMessage = "Ready";
            IsLoading = false;
            RecentActivities = new ObservableCollection<RecentActivityItem>();

            // Load initial data
            _ = LoadDataAsync();
        }

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public int PendingPayments
        {
            get => _pendingPayments;
            set
            {
                _pendingPayments = value;
                OnPropertyChanged();
            }
        }

        public int ActiveBatches
        {
            get => _activeBatches;
            set
            {
                _activeBatches = value;
                OnPropertyChanged();
            }
        }

        public int ChequesReady
        {
            get => _chequesReady;
            set
            {
                _chequesReady = value;
                OnPropertyChanged();
            }
        }

        public int ElectronicPayments
        {
            get => _electronicPayments;
            set
            {
                _electronicPayments = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<RecentActivityItem> RecentActivities
        {
            get => _recentActivities;
            set
            {
                _recentActivities = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToPaymentRunCommand { get; }
        public ICommand NavigateToPaymentGroupsCommand { get; }
        public ICommand NavigateToDistributionCommand { get; }
        public ICommand NavigateToBatchesCommand { get; }
        public ICommand NavigateToChequePreparationCommand { get; }
        public ICommand NavigateToChequeDeliveryCommand { get; }
        public ICommand NavigateToChequeReviewCommand { get; }
        public ICommand NavigateToElectronicPaymentsCommand { get; }
        public ICommand NavigateToFinalPaymentsCommand { get; }
        public ICommand NavigateToStatusDashboardCommand { get; }
        public ICommand NavigateToReconciliationCommand { get; }
        public ICommand NavigateToAdvanceChequesCommand { get; }
        public ICommand NavigateToEnhancedPaymentDistributionCommand { get; }
        public ICommand NavigateToEnhancedChequePreparationCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowHelpCommand { get; }

        #endregion

        #region Command Implementations

        private async void NavigateToDashboard(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Dashboard...";
                NavigationRequested?.Invoke(typeof(DashboardViewModel), "Dashboard");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Dashboard: {ex.Message}";
            }
        }

        private async void NavigateToPaymentRun(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Payment Run...";
                // Use the static navigation event
                NavigationRequested?.Invoke(typeof(PaymentRunViewModel), "Payment Run");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Payment Run: {ex.Message}";
            }
        }

        private async void NavigateToPaymentGroups(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Payment Groups...";
                NavigationRequested?.Invoke(typeof(PaymentGroupViewModel), "Payment Groups");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Payment Groups: {ex.Message}";
            }
        }

        private async void NavigateToDistribution(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Payment Distribution...";
                NavigationRequested?.Invoke(typeof(PaymentDistributionViewModel), "Payment Distribution");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Payment Distribution: {ex.Message}";
            }
        }

        private async void NavigateToBatches(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Payment Batches...";
                NavigationRequested?.Invoke(typeof(PaymentBatchViewModel), "Payment Batches");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Payment Batches: {ex.Message}";
            }
        }

        private async void NavigateToChequePreparation(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Cheque Preparation...";
                NavigationRequested?.Invoke(typeof(ChequePreparationViewModel), "Cheque Preparation");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Cheque Preparation: {ex.Message}";
            }
        }

        private async void NavigateToChequeDelivery(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Cheque Delivery...";
                NavigationRequested?.Invoke(typeof(ChequeDeliveryViewModel), "Cheque Delivery");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Cheque Delivery: {ex.Message}";
            }
        }

        private async void NavigateToChequeReview(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Cheque Review...";
                NavigationRequested?.Invoke(typeof(ChequeReviewViewModel), "Cheque Review");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Cheque Review: {ex.Message}";
            }
        }

        private async void NavigateToElectronicPayments(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Electronic Payments...";
                NavigationRequested?.Invoke(typeof(ElectronicPaymentProcessingViewModel), "Electronic Payments");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Electronic Payments: {ex.Message}";
            }
        }

        private async void NavigateToFinalPayments(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Final Payments...";
                NavigationRequested?.Invoke(typeof(FinalPaymentViewModel), "Final Payments");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Final Payments: {ex.Message}";
            }
        }

        private async void NavigateToStatusDashboard(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Payment Status Dashboard...";
                NavigationRequested?.Invoke(typeof(PaymentStatusDashboardViewModel), "Payment Status Dashboard");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Payment Status Dashboard: {ex.Message}";
            }
        }

        private async void NavigateToReconciliation(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Payment Reconciliation...";
                NavigationRequested?.Invoke(typeof(PaymentReconciliationViewModel), "Payment Reconciliation");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Payment Reconciliation: {ex.Message}";
            }
        }

        private async Task RefreshDataAsync()
        {
            IsLoading = true;
            StatusMessage = "Refreshing payment data...";

            try
            {
                await LoadDataAsync();
                StatusMessage = "Data refreshed successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowHelp(object parameter)
        {
            StatusMessage = "Opening help documentation...";
            // Help functionality can be implemented here
        }

        #endregion

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading payment statistics...";

                // TODO: Implement actual service calls when methods are available
                // For now, use mock data
                await Task.Delay(500); // Simulate async operation

                // Mock data - replace with actual service calls when available
                PendingPayments = 1815;
                ActiveBatches = 45;
                ChequesReady = 12;
                ElectronicPayments = 3;

                // Load recent activities
                LoadRecentActivities();

                StatusMessage = "Payment statistics loaded successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Navigation Methods

        private void NavigateToAdvanceCheques(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Advance Cheques...";
                NavigationRequested?.Invoke(typeof(AdvanceChequeViewModel), "Advance Cheques");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Advance Cheques: {ex.Message}";
            }
        }

        private void NavigateToEnhancedPaymentDistribution(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Enhanced Payment Distribution...";
                NavigationRequested?.Invoke(typeof(EnhancedPaymentDistributionViewModel), "Enhanced Payment Distribution");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Enhanced Payment Distribution: {ex.Message}";
            }
        }

        private void NavigateToEnhancedChequePreparation(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to Enhanced Cheque Preparation...";
                NavigationRequested?.Invoke(typeof(EnhancedChequePreparationViewModel), "Enhanced Cheque Preparation");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to Enhanced Cheque Preparation: {ex.Message}";
            }
        }

        #endregion

        #region Recent Activities

        private void LoadRecentActivities()
        {
            RecentActivities.Clear();
            
            // Mock recent activities - replace with actual data from services
            RecentActivities.Add(new RecentActivityItem
            {
                IconKind = "CheckCircle",
                IconColor = "#4CAF50",
                Description = "Payment batch #2024-001 completed successfully",
                Timestamp = "2 minutes ago",
                Status = "Completed"
            });
            
            RecentActivities.Add(new RecentActivityItem
            {
                IconKind = "ClockOutline",
                IconColor = "#FF6B35",
                Description = "Cheque preparation started for 25 growers",
                Timestamp = "15 minutes ago",
                Status = "In Progress"
            });
            
            RecentActivities.Add(new RecentActivityItem
            {
                IconKind = "Bank",
                IconColor = "#1976D2",
                Description = "Electronic payment file generated",
                Timestamp = "1 hour ago",
                Status = "Ready"
            });
            
            RecentActivities.Add(new RecentActivityItem
            {
                IconKind = "AlertCircle",
                IconColor = "#F44336",
                Description = "Payment validation failed for 3 growers",
                Timestamp = "2 hours ago",
                Status = "Error"
            });
            
            RecentActivities.Add(new RecentActivityItem
            {
                IconKind = "PackageVariant",
                IconColor = "#FFC107",
                Description = "New payment batch created",
                Timestamp = "3 hours ago",
                Status = "Created"
            });
        }

        #endregion

    }

    // Recent Activity Item Model
    public class RecentActivityItem
    {
        public string IconKind { get; set; } = string.Empty;
        public string IconColor { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
