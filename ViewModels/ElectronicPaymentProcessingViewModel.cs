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
    /// ViewModel for electronic payment processing operations.
    /// Manages NACHA file generation and electronic payment processing.
    /// </summary>
    public class ElectronicPaymentProcessingViewModel : ViewModelBase
    {
        private readonly IElectronicPaymentService _electronicPaymentService;

        private ObservableCollection<ElectronicPayment> _electronicPayments = new();
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        public ElectronicPaymentProcessingViewModel(IElectronicPaymentService electronicPaymentService)
        {
            _electronicPaymentService = electronicPaymentService;
            InitializeCommands();
            _ = LoadElectronicPaymentsAsync();
        }

        #region Properties

        public ObservableCollection<ElectronicPayment> ElectronicPayments
        {
            get => _electronicPayments;
            set => SetProperty(ref _electronicPayments, value);
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

        #endregion

        #region Commands

        public ICommand SelectAllCommand { get; private set; } = null!;
        public ICommand ClearSelectionCommand { get; private set; } = null!;
        public ICommand GenerateNachaFileCommand { get; private set; } = null!;
        public ICommand MarkAsProcessedCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand NavigateToPaymentManagementCommand { get; private set; } = null!;

        #endregion

        #region Command Implementations

        private async Task LoadElectronicPaymentsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading electronic payments...";

                var payments = await _electronicPaymentService.GetPendingElectronicPaymentsAsync();
                ElectronicPayments.Clear();
                
                foreach (var payment in payments)
                {
                    ElectronicPayments.Add(payment);
                }

                StatusMessage = $"Loaded {payments.Count} electronic payments ready for processing";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading electronic payments: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SelectAllAsync(object parameter)
        {
            foreach (var payment in ElectronicPayments)
            {
                payment.IsSelected = true;
            }
            StatusMessage = $"Selected all {ElectronicPayments.Count} payments";
        }

        private async Task ClearSelectionAsync(object parameter)
        {
            foreach (var payment in ElectronicPayments)
            {
                payment.IsSelected = false;
            }
            StatusMessage = "Cleared selection";
        }

        private async Task GenerateNachaFileAsync(object parameter)
        {
            try
            {
                var selectedPayments = ElectronicPayments.Where(p => p.IsSelected).ToList();
                if (!selectedPayments.Any())
                {
                    StatusMessage = "No payments selected for NACHA file generation";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Generating NACHA file for {selectedPayments.Count} payments...";

                var paymentIds = selectedPayments.Select(p => p.ElectronicPaymentId).ToList();
                var nachaFileBytes = await _electronicPaymentService.GenerateNachaFileAsync(paymentIds);
                
                if (nachaFileBytes.Length > 0)
                {
                    // Save NACHA file
                    var fileName = $"NACHA_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, nachaFileBytes);

                    StatusMessage = $"NACHA file generated successfully: {fileName}";
                }
                else
                {
                    StatusMessage = "No NACHA file generated - no valid payments found";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating NACHA file: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task MarkAsProcessedAsync(object parameter)
        {
            try
            {
                var selectedPayments = ElectronicPayments.Where(p => p.IsSelected).ToList();
                if (!selectedPayments.Any())
                {
                    StatusMessage = "No payments selected for processing";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Processing {selectedPayments.Count} payments...";

                var paymentIds = selectedPayments.Select(p => p.ElectronicPaymentId).ToList();
                await _electronicPaymentService.MarkPaymentsAsProcessedAsync(paymentIds, App.CurrentUser?.Username ?? "SYSTEM");

                // Update local collection
                foreach (var payment in selectedPayments)
                {
                    payment.Status = "Processed";
                    payment.ProcessedAt = DateTime.Now;
                    payment.ProcessedBy = App.CurrentUser?.Username ?? "SYSTEM";
                }

                StatusMessage = $"Successfully processed {selectedPayments.Count} payments";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing payments: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshAsync(object parameter)
        {
            await LoadElectronicPaymentsAsync();
        }

        #endregion

        #region Helper Methods

        private void InitializeCommands()
        {
            SelectAllCommand = new RelayCommand(async (param) => await SelectAllAsync(param));
            ClearSelectionCommand = new RelayCommand(async (param) => await ClearSelectionAsync(param));
            GenerateNachaFileCommand = new RelayCommand(async (param) => await GenerateNachaFileAsync(param));
            MarkAsProcessedCommand = new RelayCommand(async (param) => await MarkAsProcessedAsync(param));
            RefreshCommand = new RelayCommand(async (param) => await RefreshAsync(param));
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
                StatusMessage = $"Error navigating to Dashboard: {ex.Message}";
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
                StatusMessage = $"Error navigating to Payment Management: {ex.Message}";
            }
        }

        #endregion
    }
}