using System;
using System.Collections.Generic;
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
    /// ViewModel for cheque delivery operations.
    /// Manages the delivery of approved cheques with simple tracking.
    /// </summary>
    public class ChequeDeliveryViewModel : ViewModelBase
    {
        private readonly IChequeService _chequeService;
        private readonly WPFGrowerApp.Services.ChequePdfGenerator _pdfGenerator;

        private ObservableCollection<Cheque> _cheques = new();
        private bool _isLoading;
        private string _statusMessage = string.Empty;

        public ChequeDeliveryViewModel(IChequeService chequeService, WPFGrowerApp.Services.ChequePdfGenerator pdfGenerator)
        {
            _chequeService = chequeService;
            _pdfGenerator = pdfGenerator;
            InitializeCommands();
            _ = LoadChequesAsync();
        }

        #region Properties

        public ObservableCollection<Cheque> Cheques
        {
            get => _cheques;
            set 
            { 
                SetProperty(ref _cheques, value);
                RefreshComputedProperties();
            }
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

        public decimal TotalAmount => _cheques?.Sum(c => c.ChequeAmount) ?? 0;

        public int SelectedCount => _cheques?.Count(c => c.IsSelected) ?? 0;

        #endregion

        #region Commands

        public ICommand SelectAllCommand { get; private set; } = null!;
        public ICommand ClearSelectionCommand { get; private set; } = null!;
        public ICommand RecordDeliveryCommand { get; private set; } = null!;
        public ICommand RecordSingleDeliveryCommand { get; private set; } = null!;
        public ICommand EmergencyReprintCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand ShowHelpCommand { get; private set; } = null!;
        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand NavigateToPaymentManagementCommand { get; private set; } = null!;

        #endregion

        #region Command Implementations

        private async Task LoadChequesAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading cheques ready for delivery...";

                // Load cheques that are approved for delivery (status = "Delivered" but not yet physically delivered)
                // For now, we'll load "Printed" cheques as they're ready for delivery after review
                var cheques = await _chequeService.GetChequesByStatusAsync("Printed");
                Cheques.Clear();
                
                foreach (var cheque in cheques)
                {
                    Cheques.Add(cheque);
                }

                // Set up collection change notification for computed properties
                Cheques.CollectionChanged += (s, e) => RefreshComputedProperties();

                StatusMessage = $"Loaded {cheques.Count} cheques ready for delivery";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading cheques: {ex.Message}";
                Logger.Error("Error loading cheques for delivery", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SelectAllAsync(object parameter)
        {
            foreach (var cheque in Cheques)
            {
                cheque.IsSelected = true;
            }
            StatusMessage = $"Selected all {Cheques.Count} cheques";
        }

        private async Task ClearSelectionAsync(object parameter)
        {
            foreach (var cheque in Cheques)
            {
                cheque.IsSelected = false;
            }
            StatusMessage = "Cleared selection";
        }

        private async Task RecordDeliveryAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "No cheques selected for delivery";
                    return;
                }

                // Show delivery method dialog
                var deliveryMethod = await ShowDeliveryMethodDialogAsync();
                if (string.IsNullOrEmpty(deliveryMethod))
                {
                    StatusMessage = "Delivery recording cancelled";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Recording delivery for {selectedCheques.Count} cheques...";

                // Record delivery (updates status to "Delivered")
                var chequeIds = selectedCheques.Select(c => c.ChequeId).ToList();
                await _chequeService.MarkChequesAsDeliveredAsync(chequeIds, deliveryMethod, App.CurrentUser?.Username ?? "SYSTEM");

                // Update local collection
                foreach (var cheque in selectedCheques)
                {
                    cheque.Status = "Delivered";
                    cheque.DeliveredAt = DateTime.Now;
                    cheque.DeliveryMethod = deliveryMethod;
                }

                StatusMessage = $"Successfully recorded delivery for {selectedCheques.Count} cheques via {deliveryMethod}";
                
                // Refresh to remove delivered cheques
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error recording delivery: {ex.Message}";
                Logger.Error("Error recording delivery", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RecordSingleDeliveryAsync(object parameter)
        {
            if (parameter is not Cheque cheque)
            {
                StatusMessage = "Invalid cheque for delivery recording";
                return;
            }

            try
            {
                // Show delivery method dialog
                var deliveryMethod = await ShowDeliveryMethodDialogAsync();
                if (string.IsNullOrEmpty(deliveryMethod))
                {
                    StatusMessage = "Delivery recording cancelled";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Recording delivery for cheque {cheque.ChequeNumber}...";

                // Record delivery
                await _chequeService.MarkChequesAsDeliveredAsync(
                    new List<int> { cheque.ChequeId }, 
                    deliveryMethod, 
                    App.CurrentUser?.Username ?? "SYSTEM");

                // Update local collection
                cheque.Status = "Delivered";
                cheque.DeliveredAt = DateTime.Now;
                cheque.DeliveryMethod = deliveryMethod;

                StatusMessage = $"Successfully recorded delivery for cheque {cheque.ChequeNumber} via {deliveryMethod}";
                
                // Refresh to remove delivered cheque
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error recording delivery: {ex.Message}";
                Logger.Error($"Error recording delivery for cheque {cheque.ChequeNumber}", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task EmergencyReprintAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "No cheques selected for emergency reprint";
                    return;
                }

                // Show confirmation with warning
                var confirm = System.Windows.MessageBox.Show(
                    $"EMERGENCY REPRINT\n\n" +
                    $"This is an emergency reprint of {selectedCheques.Count} cheque(s).\n\n" +
                    $"⚠️ WARNING: This should only be used in emergency situations.\n" +
                    $"The original cheques may still be valid.\n\n" +
                    $"Continue with emergency reprint?",
                    "Emergency Reprint Confirmation",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (confirm != System.Windows.MessageBoxResult.Yes)
                    return;

                // Get reason
                var reasonDialog = new WPFGrowerApp.Controls.InputBoxDialog(
                    "Emergency Reprint Reason Required",
                    "Please enter the reason for emergency reprint:",
                    "Reason is required for audit purposes");

                if (reasonDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(reasonDialog.Answer))
                {
                    StatusMessage = "Emergency reprint cancelled - reason is required.";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Emergency reprinting {selectedCheques.Count} cheques...";

                // Generate reprint PDF with watermark
                var pdfBytes = await _pdfGenerator.GenerateBatchChequeReprintPdfAsync(selectedCheques, reasonDialog.Answer);
                
                // Save PDF to desktop
                var fileName = $"EMERGENCY_REPRINT_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var filePath = System.IO.Path.Combine(desktopPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                // Log reprint activity
                var chequeIds = selectedCheques.Select(c => c.ChequeId).ToList();
                await _chequeService.LogReprintActivityAsync(chequeIds, reasonDialog.Answer, App.CurrentUser?.Username ?? "SYSTEM");

                StatusMessage = $"Emergency reprint generated: {fileName}";
                
                // Open the file
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error during emergency reprint: {ex.Message}";
                Logger.Error("Error during emergency reprint", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RefreshAsync(object parameter)
        {
            await LoadChequesAsync();
        }

        private void ShowHelpExecute(object? parameter)
        {
            System.Windows.MessageBox.Show(
                "Cheque Delivery Help:\n\n" +
                "1. Record delivery of approved cheques\n" +
                "2. Select delivery method (Mail, Pickup, Courier)\n" +
                "3. Emergency reprint only for critical situations\n" +
                "4. Use F5 to refresh the list\n\n" +
                "Note: Delivery tracking tables are hidden for simplicity.",
                "Cheque Delivery Help",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        private async Task<string?> ShowDeliveryMethodDialogAsync()
        {
            // Simple delivery method selection dialog
            var dialog = new WPFGrowerApp.Controls.InputBoxDialog(
                "Delivery Method",
                "Select delivery method:",
                "Mail, Pickup, or Courier");

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                var method = dialog.Answer.Trim();
                if (method.Equals("Mail", StringComparison.OrdinalIgnoreCase) ||
                    method.Equals("Pickup", StringComparison.OrdinalIgnoreCase) ||
                    method.Equals("Courier", StringComparison.OrdinalIgnoreCase))
                {
                    return method;
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Please enter a valid delivery method: Mail, Pickup, or Courier",
                        "Invalid Delivery Method",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            return null;
        }

        private void InitializeCommands()
        {
            SelectAllCommand = new RelayCommand(async (param) => await SelectAllAsync(param));
            ClearSelectionCommand = new RelayCommand(async (param) => await ClearSelectionAsync(param));
            RecordDeliveryCommand = new RelayCommand(async (param) => await RecordDeliveryAsync(param));
            RecordSingleDeliveryCommand = new RelayCommand(async (param) => await RecordSingleDeliveryAsync(param));
            EmergencyReprintCommand = new RelayCommand(async (param) => await EmergencyReprintAsync(param));
            RefreshCommand = new RelayCommand(async (param) => await RefreshAsync(param));
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);
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
                Logger.Error("Error navigating to Dashboard", ex);
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
                Logger.Error("Error navigating to Payment Management", ex);
            }
        }

        private void RefreshComputedProperties()
        {
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(SelectedCount));
        }

        #endregion
    }
}
