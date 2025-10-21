using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Dapper;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    public class PaymentDistributionViewModel : ViewModelBase
    {
        private readonly IPaymentDistributionService _paymentDistributionService;
        private readonly IPaymentBatchService _paymentBatchService;
        private readonly string _connectionString = "Server=localhost;Database=BerryFarmsModern;Integrated Security=true;TrustServerCertificate=true;";

        private ObservableCollection<PaymentBatch> _availableBatches = new();
        private ObservableCollection<PaymentDistribution> _distributions = new();
        private ObservableCollection<PaymentDistributionItem> _distributionItems = new();
        private ObservableCollection<string> _paymentMethods = new() { "Cheque", "Electronic", "Both" };

        private string _searchText = string.Empty;
        private bool _isByGrower = true;
        private bool _isByBatch = false;
        private bool _isAllPending = false;
        private string _selectedPaymentMethod = "Cheque";
        private bool _isHybridMode = false;
        private PaymentDistribution? _selectedDistribution;
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private bool _showNextSteps = false;

        public PaymentDistributionViewModel(
            IPaymentDistributionService paymentDistributionService,
            IPaymentBatchService paymentBatchService)
        {
            _paymentDistributionService = paymentDistributionService;
            _paymentBatchService = paymentBatchService;

            InitializeCommands();
            _ = LoadDataAsync();
        }

        #region Properties

        public ObservableCollection<PaymentBatch> AvailableBatches
        {
            get => _availableBatches;
            set => SetProperty(ref _availableBatches, value);
        }

        public ObservableCollection<PaymentDistribution> Distributions
        {
            get => _distributions;
            set => SetProperty(ref _distributions, value);
        }

        public ObservableCollection<PaymentDistributionItem> DistributionItems
        {
            get => _distributionItems;
            set => SetProperty(ref _distributionItems, value);
        }

        public ObservableCollection<string> PaymentMethods
        {
            get => _paymentMethods;
            set => SetProperty(ref _paymentMethods, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public bool IsByGrower
        {
            get => _isByGrower;
            set => SetProperty(ref _isByGrower, value);
        }

        public bool IsByBatch
        {
            get => _isByBatch;
            set => SetProperty(ref _isByBatch, value);
        }

        public bool IsAllPending
        {
            get => _isAllPending;
            set => SetProperty(ref _isAllPending, value);
        }

        public string SelectedPaymentMethod
        {
            get => _selectedPaymentMethod;
            set => SetProperty(ref _selectedPaymentMethod, value);
        }

        public bool IsHybridMode
        {
            get => _isHybridMode;
            set => SetProperty(ref _isHybridMode, value);
        }

        public PaymentDistribution? SelectedDistribution
        {
            get => _selectedDistribution;
            set => SetProperty(ref _selectedDistribution, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool ShowNextSteps
        {
            get => _showNextSteps;
            set => SetProperty(ref _showNextSteps, value);
        }

        #endregion

        #region Commands

        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand NavigateToPaymentManagementCommand { get; private set; } = null!;
        public ICommand SearchCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand GeneratePaymentsCommand { get; private set; } = null!;
        public ICommand PreviewCommand { get; private set; } = null!;
        public ICommand CancelCommand { get; private set; } = null!;
        public ICommand ViewDistributionCommand { get; private set; } = null!;
        public ICommand NavigateToChequePreparationCommand { get; private set; } = null!;
        public ICommand NavigateToElectronicPaymentsCommand { get; private set; } = null!;
        public ICommand NavigateToPaymentStatusCommand { get; private set; } = null!;

        #endregion

        #region Command Implementations

        private void InitializeCommands()
        {
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToPaymentManagementCommand = new RelayCommand(NavigateToPaymentManagementExecute);
            SearchCommand = new RelayCommand(async (param) => await SearchAsync());
            ClearFiltersCommand = new RelayCommand(async (param) => await ClearFiltersAsync());
            RefreshCommand = new RelayCommand(async (param) => await LoadDataAsync());
            GeneratePaymentsCommand = new RelayCommand(async (param) => await GeneratePaymentsAsync(), (param) => CanGeneratePayments());
            PreviewCommand = new RelayCommand(async (param) => await PreviewDistributionAsync(), (param) => CanPreviewDistribution());
            CancelCommand = new RelayCommand((param) => Cancel());
            ViewDistributionCommand = new RelayCommand(async (param) => await ViewDistributionAsync(param));
            NavigateToChequePreparationCommand = new RelayCommand(NavigateToChequePreparationExecute);
            NavigateToElectronicPaymentsCommand = new RelayCommand(NavigateToElectronicPaymentsExecute);
            NavigateToPaymentStatusCommand = new RelayCommand(NavigateToPaymentStatusExecute);
        }

        private void NavigateToDashboardExecute(object? parameter)
        {
            try
            {
                // Get the main window's view model and navigate to dashboard
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (mainViewModel.NavigateToDashboardCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToDashboardCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to dashboard from PaymentDistributionViewModel", ex);
            }
        }

        private void NavigateToPaymentManagementExecute(object? parameter)
        {
            try
            {
                // Get the main window's view model and navigate to payment management hub
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    if (mainViewModel.NavigateToPaymentManagementCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToPaymentManagementCommand.Execute(null);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to payment management from PaymentDistributionViewModel", ex);
            }
        }

        private async Task SearchAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Searching...";

                var batches = await _paymentDistributionService.GetAvailableBatchesAsync();
                
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    batches = batches.Where(b => 
                        b.BatchNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                        b.PaymentTypeName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                AvailableBatches.Clear();
                foreach (var batch in batches)
                {
                    AvailableBatches.Add(batch);
                }

                StatusMessage = $"Found {AvailableBatches.Count} batches";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error searching batches: {ex.Message}", ex);
                StatusMessage = "Error searching batches";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ClearFiltersAsync()
        {
            SearchText = string.Empty;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading data...";

                var batches = await _paymentDistributionService.GetAvailableBatchesAsync();
                var distributions = await _paymentDistributionService.GetDistributionsAsync();

                AvailableBatches.Clear();
                foreach (var batch in batches)
                {
                    AvailableBatches.Add(batch);
                }

                Distributions.Clear();
                foreach (var distribution in distributions)
                {
                    Distributions.Add(distribution);
                }

                StatusMessage = $"Loaded {AvailableBatches.Count} batches and {Distributions.Count} distributions";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading data: {ex.Message}", ex);
                StatusMessage = "Error loading data";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GeneratePaymentsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Generating payments...";

                var selectedBatches = AvailableBatches.Where(b => b.IsSelected).ToList();
                if (!selectedBatches.Any())
                {
                    StatusMessage = "Please select at least one batch";
                    return;
                }

                // Check for existing distributions to prevent duplicates
                foreach (var batch in selectedBatches)
                {
                    var hasExisting = await _paymentDistributionService.HasExistingDistributionsAsync(batch.PaymentBatchId);
                    if (hasExisting)
                    {
                        StatusMessage = $"Batch {batch.BatchNumber} already has payment distributions. Please select a different batch.";
                        return;
                    }
                }

                var items = await CreateDistributionItemsAsync(selectedBatches);
                var distribution = new PaymentDistribution
                {
                    DistributionType = GetDistributionType(),
                    PaymentMethod = SelectedPaymentMethod,
                    TotalAmount = items.Sum(i => i.Amount),
                    TotalGrowers = items.Select(i => i.GrowerId).Distinct().Count(),
                    TotalBatches = selectedBatches.Count,
                    Items = items
                };

                var createdDistribution = await _paymentDistributionService.CreateDistributionAsync(distribution);
                
                if (await _paymentDistributionService.GeneratePaymentsAsync(createdDistribution.DistributionId, App.CurrentUser?.Username ?? "SYSTEM"))
                {
                    StatusMessage = $"Successfully generated payments for distribution {createdDistribution.DistributionNumber}";
                    ShowNextSteps = true; // Show the next steps panel
                    await LoadDataAsync();
                }
                else
                {
                    StatusMessage = "Error generating payments";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error generating payments: {ex.Message}", ex);
                StatusMessage = "Error generating payments";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PreviewDistributionAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading preview...";

                var selectedBatches = AvailableBatches.Where(b => b.IsSelected).ToList();
                if (!selectedBatches.Any())
                {
                    StatusMessage = "Please select at least one batch";
                    return;
                }

                var items = await CreateDistributionItemsAsync(selectedBatches);
                
                DistributionItems.Clear();
                foreach (var item in items)
                {
                    DistributionItems.Add(item);
                }
                
                // Trigger property changed notification for the collection
                OnPropertyChanged(nameof(DistributionItems));

                var totalAmount = DistributionItems.Sum(i => i.Amount);
                StatusMessage = $"Preview: {DistributionItems.Count} items, Total: {totalAmount:C}";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error previewing distribution: {ex.Message}", ex);
                StatusMessage = "Error previewing distribution";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Cancel()
        {
            // Clear selections and reset
            foreach (var batch in AvailableBatches)
            {
                batch.IsSelected = false;
            }
            DistributionItems.Clear();
            StatusMessage = "Cancelled";
        }

        private async Task ViewDistributionAsync(object parameter)
        {
            if (parameter is PaymentDistribution distribution)
            {
                SelectedDistribution = distribution;
                // Load distribution items
                var fullDistribution = await _paymentDistributionService.GetDistributionByIdAsync(distribution.DistributionId);
                if (fullDistribution?.Items != null)
                {
                    DistributionItems.Clear();
                    foreach (var item in fullDistribution.Items)
                    {
                        DistributionItems.Add(item);
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        private string GetDistributionType()
        {
            if (IsByGrower) return "ByGrower";
            if (IsByBatch) return "ByBatch";
            if (IsAllPending) return "AllPending";
            return "ByGrower";
        }

        private async Task<List<PaymentDistributionItem>> CreateDistributionItemsAsync(List<PaymentBatch> selectedBatches)
        {
            var items = new List<PaymentDistributionItem>();

            if (IsByGrower)
            {
                // Group by grower and consolidate
                var allGrowers = new List<GrowerPaymentInfo>();
                foreach (var batch in selectedBatches)
                {
                    var growers = await GetGrowersFromBatchAsync(batch);
                    allGrowers.AddRange(growers);
                }

                var growerGroups = allGrowers
                    .GroupBy(g => g.GrowerId)
                    .ToList();

                foreach (var group in growerGroups)
                {
                    var grower = group.First();
                    var totalAmount = group.Sum(g => g.Amount);
                    var batches = group.Select(g => g.BatchNumber).Distinct().ToList();

                    // For consolidated payments, use the first receipt ID as the primary one
                    items.Add(new PaymentDistributionItem
                    {
                        GrowerId = grower.GrowerId,
                        GrowerName = grower.GrowerName,
                        GrowerNumber = grower.GrowerNumber,
                        PaymentBatchId = group.First().PaymentBatchId,
                        ReceiptId = group.First().ReceiptId, // Use the first receipt ID for consolidated payments
                        Amount = totalAmount,
                        PaymentMethod = IsHybridMode ? "Cheque" : SelectedPaymentMethod, // Default to Cheque in hybrid mode, user can change
                        Status = "Draft",
                        CreatedAt = DateTime.Now,
                        CreatedBy = App.CurrentUser?.Username ?? "SYSTEM",
                        BatchNumber = string.Join(", ", batches)
                    });
                }
            }
            else if (IsByBatch)
            {
                // Individual items per batch
                foreach (var batch in selectedBatches)
                {
                    var growers = await GetGrowersFromBatchAsync(batch);
                    foreach (var grower in growers)
                    {
                        items.Add(new PaymentDistributionItem
                        {
                            GrowerId = grower.GrowerId,
                            GrowerName = grower.GrowerName,
                            GrowerNumber = grower.GrowerNumber,
                            PaymentBatchId = batch.PaymentBatchId,
                            ReceiptId = grower.ReceiptId,
                            BatchNumber = batch.BatchNumber,
                            Amount = grower.Amount,
                            PaymentMethod = IsHybridMode ? "Cheque" : SelectedPaymentMethod, // Default to Cheque in hybrid mode, user can change
                            Status = "Draft",
                            CreatedAt = DateTime.Now,
                            CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                        });
                    }
                }
            }

            return items;
        }

        private async Task<List<GrowerPaymentInfo>> GetGrowersFromBatchAsync(PaymentBatch batch)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    var sql = @"
                        SELECT 
                            g.GrowerId,
                            g.FullName AS GrowerName,
                            g.GrowerNumber,
                            rpa.AmountPaid AS Amount,
                            pb.BatchNumber,
                            pb.PaymentBatchId,
                            rpa.ReceiptId
                        FROM ReceiptPaymentAllocations rpa
                        INNER JOIN Receipts r ON rpa.ReceiptId = r.ReceiptId
                        INNER JOIN Growers g ON r.GrowerId = g.GrowerId
                        INNER JOIN PaymentBatches pb ON rpa.PaymentBatchId = pb.PaymentBatchId
                        WHERE pb.PaymentBatchId = @PaymentBatchId
                        AND rpa.Status = 'Posted'";
                    
                    var result = await connection.QueryAsync<GrowerPaymentInfo>(sql, new { PaymentBatchId = batch.PaymentBatchId });
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting growers from batch {batch.BatchNumber}: {ex.Message}", ex);
                return new List<GrowerPaymentInfo>();
            }
        }

        private bool CanGeneratePayments()
        {
            return AvailableBatches.Any(b => b.IsSelected) && !IsLoading;
        }

        private bool CanPreviewDistribution()
        {
            return AvailableBatches.Any(b => b.IsSelected) && !IsLoading;
        }

        #endregion


        private void NavigateToChequePreparationExecute(object? parameter)
        {
            try
            {
                // Navigate to cheque preparation module
                // This would typically use a navigation service or event
                StatusMessage = "Navigating to Cheque Preparation...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to cheque preparation: {ex.Message}";
            }
        }

        private void NavigateToElectronicPaymentsExecute(object? parameter)
        {
            try
            {
                // Navigate to electronic payments module
                // This would typically use a navigation service or event
                StatusMessage = "Navigating to Electronic Payments...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to electronic payments: {ex.Message}";
            }
        }

        private void NavigateToPaymentStatusExecute(object? parameter)
        {
            try
            {
                // Navigate to payment status dashboard
                // This would typically use a navigation service or event
                StatusMessage = "Navigating to Payment Status Dashboard...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error navigating to payment status: {ex.Message}";
            }
        }
    }

    // Helper class for grower payment information
    public class GrowerPaymentInfo
    {
        public int GrowerId { get; set; }
        public string GrowerName { get; set; } = string.Empty;
        public string GrowerNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int PaymentBatchId { get; set; }
        public int ReceiptId { get; set; }
    }
}
