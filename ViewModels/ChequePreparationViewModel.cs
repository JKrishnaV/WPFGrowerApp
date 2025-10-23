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
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for cheque preparation operations.
    /// Manages the preparation and printing of cheques with real bank-format PDF generation.
    /// </summary>
    public class ChequePreparationViewModel : ViewModelBase
    {
        private readonly IChequeService _chequeService;
        private readonly WPFGrowerApp.Services.ChequePdfGenerator _pdfGenerator;
        private readonly IUnifiedVoidingService _unifiedVoidingService;

        private ObservableCollection<Cheque> _cheques = new();
        private bool _isLoading;
        private string _statusMessage = string.Empty;
        private string _searchText = string.Empty;
        private string _chequeNumberFilter = string.Empty;
        private string _growerNumberFilter = string.Empty;
        private string _statusFilter = "All";
        private Cheque? _selectedCheque;
        private DateTime _lastUpdated = DateTime.Now;

        public ChequePreparationViewModel(IChequeService chequeService, WPFGrowerApp.Services.ChequePdfGenerator pdfGenerator, IUnifiedVoidingService unifiedVoidingService)
        {
            Logger.Info("ChequePreparationViewModel constructor called");
            _chequeService = chequeService;
            _pdfGenerator = pdfGenerator;
            _unifiedVoidingService = unifiedVoidingService;
            InitializeCommands();
            Logger.Info("ChequePreparationViewModel initialized, starting LoadChequesAsync");
            _ = LoadChequesAsync();
        }

        #region Properties

        public ObservableCollection<Cheque> Cheques
        {
            get => _cheques;
            set 
            { 
                // Detach handlers from old collection
                if (_cheques != null)
                {
                    _cheques.CollectionChanged -= Cheques_CollectionChanged;
                    foreach (var cheque in _cheques)
                    {
                        cheque.PropertyChanged -= Cheque_PropertyChanged;
                    }
                    Logger.Info($"Detached PropertyChanged handlers from {_cheques.Count} cheques");
                }

                SetProperty(ref _cheques, value);

                // Attach handlers to new collection
                if (_cheques != null)
                {
                    _cheques.CollectionChanged += Cheques_CollectionChanged;
                    foreach (var cheque in _cheques)
                    {
                        cheque.PropertyChanged += Cheque_PropertyChanged;
                    }
                    Logger.Info($"Attached PropertyChanged handlers to {_cheques.Count} cheques");
                }

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

        public decimal SelectedAmount => _cheques?.Where(c => c.IsSelected).Sum(c => c.ChequeAmount) ?? 0;

        public int SelectedCount => _cheques?.Count(c => c.IsSelected) ?? 0;

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string ChequeNumberFilter
        {
            get => _chequeNumberFilter;
            set => SetProperty(ref _chequeNumberFilter, value);
        }

        public string GrowerNumberFilter
        {
            get => _growerNumberFilter;
            set => SetProperty(ref _growerNumberFilter, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        public Cheque? SelectedCheque
        {
            get => _selectedCheque;
            set => SetProperty(ref _selectedCheque, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        #endregion

        #region Commands

        public ICommand SelectAllCommand { get; private set; } = null!;
        public ICommand ClearSelectionCommand { get; private set; } = null!;
        public ICommand PrintSelectedCommand { get; private set; } = null!;
        public ICommand PreviewSelectedCommand { get; private set; } = null!;
        public ICommand PreviewSingleCommand { get; private set; } = null!;
        public ICommand GeneratePdfCommand { get; private set; } = null!;
        public ICommand RefreshCommand { get; private set; } = null!;
        public ICommand VoidSelectedCommand { get; private set; } = null!;
        public ICommand VoidSingleCommand { get; private set; } = null!;
        public ICommand StopPaymentCommand { get; private set; } = null!;
        public ICommand StopSingleCommand { get; private set; } = null!;
        public ICommand ReprintSelectedCommand { get; private set; } = null!;
        public ICommand ShowHelpCommand { get; private set; } = null!;
        public ICommand NavigateToDashboardCommand { get; private set; } = null!;
        public ICommand NavigateToPaymentManagementCommand { get; private set; } = null!;
        public ICommand SearchCommand { get; private set; } = null!;
        public ICommand ClearFiltersCommand { get; private set; } = null!;

        #endregion

        #region Command Implementations

        private async Task LoadChequesAsync()
        {
            try
            {
                Logger.Info("LoadChequesAsync started");
                IsLoading = true;
                StatusMessage = "Loading cheques ready for preparation...";

                var cheques = await _chequeService.GetChequesByStatusAsync("Generated");
                Logger.Info($"Retrieved {cheques.Count} cheques from service");
                
                // Create new collection and set it via property setter to trigger handler attachment
                var newCheques = new ObservableCollection<Cheque>();
                foreach (var cheque in cheques)
                {
                    newCheques.Add(cheque);
                }
                
                // This will trigger the Cheques property setter which attaches handlers
                Cheques = newCheques;
                Logger.Info($"LoadChequesAsync completed: {cheques.Count} cheques loaded and handlers attached");

                StatusMessage = $"Loaded {cheques.Count} cheques ready for preparation";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading cheques: {ex.Message}";
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

        private async Task PrintSelectedAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "No cheques selected for printing";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Printing {selectedCheques.Count} cheques...";

                // Generate real bank-format PDF files for the cheques
                var pdfBytes = await _pdfGenerator.GenerateBatchChequePdfAsync(selectedCheques);
                var fileName = $"Cheques_Print_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var filePath = System.IO.Path.Combine(desktopPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                // Mark cheques as printed in database (updates status to "Printed")
                var chequeIds = selectedCheques.Select(c => c.ChequeId).ToList();
                await _chequeService.MarkChequesAsPrintedAsync(chequeIds, App.CurrentUser?.Username ?? "SYSTEM");

                // Update local collection
                foreach (var cheque in selectedCheques)
                {
                    cheque.Status = "Printed";
                    cheque.PrintedDate = DateTime.Now;
                }

                StatusMessage = $"Successfully printed {selectedCheques.Count} cheques. File saved to: {fileName}";
                
                // Open the PDF file for immediate printing
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });

                // Refresh to show updated status
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error printing cheques: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PreviewSelectedAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "No cheques selected for preview";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Generating preview for {selectedCheques.Count} cheques...";

                var pdfBytes = await _pdfGenerator.GenerateBatchChequePreviewPdfAsync(selectedCheques);
                
                // Open PDF in default viewer for preview
                var tempFileName = $"Cheques_Preview_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), tempFileName);
                await System.IO.File.WriteAllBytesAsync(tempFilePath, pdfBytes);

                // Open the PDF with the default application
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFilePath,
                    UseShellExecute = true
                });

                StatusMessage = $"Preview opened for {selectedCheques.Count} cheques";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening preview: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PreviewSingleAsync(object parameter)
        {
            try
            {
                if (parameter is not Cheque cheque)
                {
                    StatusMessage = "Invalid cheque for preview";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Generating preview for cheque {cheque.ChequeNumber}...";

                var pdfBytes = await _pdfGenerator.GenerateSingleChequePreviewPdfAsync(cheque);
                
                // Open PDF in default viewer for preview
                var tempFileName = $"Cheque_Preview_{cheque.ChequeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var tempFilePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), tempFileName);
                await System.IO.File.WriteAllBytesAsync(tempFilePath, pdfBytes);

                // Open the PDF with the default application
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempFilePath,
                    UseShellExecute = true
                });

                StatusMessage = $"Preview opened for cheque {cheque.ChequeNumber}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error opening preview: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task GeneratePdfAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "No cheques selected for PDF generation";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Generating PDF for {selectedCheques.Count} cheques...";

                var pdfBytes = await _pdfGenerator.GenerateBatchChequePdfAsync(selectedCheques);
                
                // Save PDF to file
                var fileName = $"Cheques_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                StatusMessage = $"PDF generated successfully: {fileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating PDF: {ex.Message}";
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

        private async Task VoidSelectedAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "Please select cheques to void.";
                    return;
                }

                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to void {selectedCheques.Count} selected cheque(s)? This action cannot be undone.",
                    "Confirm Void Cheques",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return;

                // Show reason dialog
                var reasonDialog = new WPFGrowerApp.Controls.InputBoxDialog(
                    "Void Reason Required",
                    "Please enter the reason for voiding these cheques:",
                    "Reason is required for audit purposes");

                if (reasonDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(reasonDialog.Answer))
                {
                    StatusMessage = "Void operation cancelled - reason is required.";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Voiding {selectedCheques.Count} cheques...";

                // Use UnifiedVoidingService for comprehensive voiding
                var voidingResults = new List<VoidingResult>();
                foreach (var cheque in selectedCheques)
                {
                    var voidingRequest = new PaymentVoidRequest
                    {
                        EntityType = "Regular",
                        EntityId = cheque.ChequeId,
                        Reason = reasonDialog.Answer,
                        VoidedBy = Environment.UserName
                    };
                    
                    var voidingResult = await _unifiedVoidingService.VoidPaymentAsync(voidingRequest);
                    voidingResults.Add(voidingResult);
                }

                // Check results
                var successfulVoids = voidingResults.Count(r => r.Success);
                var failedVoids = voidingResults.Count(r => !r.Success);
                
                if (failedVoids > 0)
                {
                    var failedMessages = voidingResults.Where(r => !r.Success).Select(r => r.Message);
                    StatusMessage = $"Voided {successfulVoids} cheques, {failedVoids} failed. Errors: {string.Join("; ", failedMessages)}";
                }
                else
                {
                    StatusMessage = $"Successfully voided {successfulVoids} cheques.";
                }
                
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error voiding cheques: {ex.Message}";
                Logger.Error($"Error voiding cheques: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task VoidSingleAsync(object parameter)
        {
            if (parameter is not Cheque cheque) return;

            try
            {
                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to void cheque {cheque.ChequeNumber}? This action cannot be undone.",
                    "Confirm Void Cheque",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return;

                // Show reason dialog
                var reasonDialog = new WPFGrowerApp.Controls.InputBoxDialog(
                    "Void Reason Required",
                    "Please enter the reason for voiding this cheque:",
                    "Reason is required for audit purposes");

                if (reasonDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(reasonDialog.Answer))
                {
                    StatusMessage = "Void operation cancelled - reason is required.";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Voiding cheque {cheque.ChequeNumber}...";

                // Use UnifiedVoidingService for comprehensive voiding
                var voidingRequest = new PaymentVoidRequest
                {
                    EntityType = "Regular",
                    EntityId = cheque.ChequeId,
                    Reason = reasonDialog.Answer,
                    VoidedBy = Environment.UserName
                };
                
                var voidingResult = await _unifiedVoidingService.VoidPaymentAsync(voidingRequest);

                if (voidingResult.Success)
                {
                    StatusMessage = $"Successfully voided cheque {cheque.ChequeNumber}.";
                }
                else
                {
                    StatusMessage = $"Failed to void cheque {cheque.ChequeNumber}: {voidingResult.Message}";
                }
                
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error voiding cheque: {ex.Message}";
                Logger.Error($"Error voiding cheque {cheque.ChequeNumber}: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StopPaymentAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "Please select cheques to stop payment.";
                    return;
                }

                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to stop payment on {selectedCheques.Count} selected cheque(s)? This will prevent the cheques from being cashed.",
                    "Confirm Stop Payment",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return;

                // Show reason dialog
                var reasonDialog = new WPFGrowerApp.Controls.InputBoxDialog(
                    "Stop Payment Reason Required",
                    "Please enter the reason for stopping payment on these cheques:",
                    "Reason is required for audit purposes");

                if (reasonDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(reasonDialog.Answer))
                {
                    StatusMessage = "Stop payment operation cancelled - reason is required.";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Stopping payment on {selectedCheques.Count} cheques...";

                var chequeIds = selectedCheques.Select(c => c.ChequeId).ToList();
                await _chequeService.StopPaymentAsync(chequeIds, reasonDialog.Answer, Environment.UserName);

                StatusMessage = $"Successfully stopped payment on {selectedCheques.Count} cheques.";
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error stopping payment: {ex.Message}";
                Logger.Error($"Error stopping payment: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task StopSingleAsync(object parameter)
        {
            if (parameter is not Cheque cheque) return;

            try
            {
                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to stop payment on cheque {cheque.ChequeNumber}? This will prevent the cheque from being cashed.",
                    "Confirm Stop Payment",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return;

                // Show reason dialog
                var reasonDialog = new WPFGrowerApp.Controls.InputBoxDialog(
                    "Stop Payment Reason Required",
                    "Please enter the reason for stopping payment on this cheque:",
                    "Reason is required for audit purposes");

                if (reasonDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(reasonDialog.Answer))
                {
                    StatusMessage = "Stop payment operation cancelled - reason is required.";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Stopping payment on cheque {cheque.ChequeNumber}...";

                await _chequeService.StopPaymentAsync(new List<int> { cheque.ChequeId }, reasonDialog.Answer, Environment.UserName);

                StatusMessage = $"Successfully stopped payment on cheque {cheque.ChequeNumber}.";
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error stopping payment: {ex.Message}";
                Logger.Error($"Error stopping payment on cheque {cheque.ChequeNumber}: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReprintSelectedAsync(object parameter)
        {
            try
            {
                var selectedCheques = Cheques.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    StatusMessage = "Please select cheques to reprint.";
                    return;
                }

                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to reprint {selectedCheques.Count} selected cheque(s)? This will generate new PDFs.",
                    "Confirm Reprint Cheques",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result != System.Windows.MessageBoxResult.Yes)
                    return;

                // Show reason dialog
                var reasonDialog = new WPFGrowerApp.Controls.InputBoxDialog(
                    "Reprint Reason Required",
                    "Please enter the reason for reprinting these cheques:",
                    "Reason is required for audit purposes");

                if (reasonDialog.ShowDialog() != true || string.IsNullOrWhiteSpace(reasonDialog.Answer))
                {
                    StatusMessage = "Reprint operation cancelled - reason is required.";
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Reprinting {selectedCheques.Count} cheques...";

                // Generate new PDFs with reprint watermark
                var pdfBytes = await _pdfGenerator.GenerateBatchChequeReprintPdfAsync(selectedCheques, reasonDialog.Answer);
                var fileName = $"Cheques_Reprint_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var filePath = System.IO.Path.Combine(desktopPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                // Log reprint activity
                var chequeIds = selectedCheques.Select(c => c.ChequeId).ToList();
                await _chequeService.LogReprintActivityAsync(chequeIds, reasonDialog.Answer, Environment.UserName);

                StatusMessage = $"Successfully reprinted {selectedCheques.Count} cheques. File saved to: {fileName}";
                
                // Open the file
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reprinting cheques: {ex.Message}";
                Logger.Error($"Error reprinting cheques: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ShowHelpExecute(object? parameter)
        {
            // TODO: Implement help content for Cheque Preparation
            System.Windows.MessageBox.Show(
                "Cheque Preparation Help:\n\n" +
                "1. Select cheques to prepare for printing\n" +
                "2. Preview cheques to verify format\n" +
                "3. Print cheques (updates status to 'Printed')\n" +
                "4. Use reprint for corrections\n" +
                "5. Void or stop payment if needed",
                "Cheque Preparation Help",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        private void InitializeCommands()
        {
            SelectAllCommand = new RelayCommand(async (param) => await SelectAllAsync(param));
            ClearSelectionCommand = new RelayCommand(async (param) => await ClearSelectionAsync(param));
            PrintSelectedCommand = new RelayCommand(async (param) => await PrintSelectedAsync(param));
            PreviewSelectedCommand = new RelayCommand(async (param) => await PreviewSelectedAsync(param));
            PreviewSingleCommand = new RelayCommand(async (param) => await PreviewSingleAsync(param));
            GeneratePdfCommand = new RelayCommand(async (param) => await GeneratePdfAsync(param));
            RefreshCommand = new RelayCommand(async (param) => await RefreshAsync(param));
            VoidSelectedCommand = new RelayCommand(async (param) => await VoidSelectedAsync(param));
            VoidSingleCommand = new RelayCommand(async (param) => await VoidSingleAsync(param));
            StopPaymentCommand = new RelayCommand(async (param) => await StopPaymentAsync(param));
            StopSingleCommand = new RelayCommand(async (param) => await StopSingleAsync(param));
            ReprintSelectedCommand = new RelayCommand(async (param) => await ReprintSelectedAsync(param));
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboard);
            NavigateToPaymentManagementCommand = new RelayCommand(NavigateToPaymentManagement);
            SearchCommand = new RelayCommand(async (param) => await SearchAsync(param));
            ClearFiltersCommand = new RelayCommand(ClearFiltersExecute);
        }

        private void Cheques_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Logger.Info($"Cheques_CollectionChanged: Action={e.Action}, NewItems={e.NewItems?.Count ?? 0}, OldItems={e.OldItems?.Count ?? 0}");
            
            // Handle items being added
            if (e.NewItems != null)
            {
                foreach (WPFGrowerApp.DataAccess.Models.Cheque cheque in e.NewItems)
                {
                    cheque.PropertyChanged += Cheque_PropertyChanged;
                }
                Logger.Info($"Added PropertyChanged handlers to {e.NewItems.Count} new cheques");
            }

            // Handle items being removed
            if (e.OldItems != null)
            {
                foreach (WPFGrowerApp.DataAccess.Models.Cheque cheque in e.OldItems)
                {
                    cheque.PropertyChanged -= Cheque_PropertyChanged;
                }
                Logger.Info($"Removed PropertyChanged handlers from {e.OldItems.Count} removed cheques");
            }

            RefreshComputedProperties();
        }

        private void Cheque_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Log all property changes to see what's happening
            Logger.Info($"Cheque_PropertyChanged: Property='{e.PropertyName}' on {sender?.GetType().Name}");
            
            // Only refresh computed properties when IsSelected or ChequeAmount changes
            if (e.PropertyName == nameof(WPFGrowerApp.DataAccess.Models.Cheque.IsSelected) ||
                e.PropertyName == nameof(WPFGrowerApp.DataAccess.Models.Cheque.ChequeAmount))
            {
                // Log property changes to track statistics updates
                Logger.Info($"Relevant property changed: {e.PropertyName} - refreshing computed properties");
                RefreshComputedProperties();
            }
        }

        private void RefreshComputedProperties()
        {
            var selectedCount = SelectedCount;
            var totalAmount = TotalAmount;
            var selectedAmount = SelectedAmount;
            
            // Log computed property values to track statistics updates
            Logger.Info($"RefreshComputedProperties: SelectedCount={selectedCount}, TotalAmount={totalAmount:C}, SelectedAmount={selectedAmount:C}");
            
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedAmount));
        }

        private void NavigateToDashboard(object? parameter)
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

        private void NavigateToPaymentManagement(object? parameter)
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

        private async Task SearchAsync(object? parameter)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Searching cheques...";

                // Apply filters and search
                var filteredCheques = await ApplyFiltersAsync();
                
                Cheques.Clear();
                foreach (var cheque in filteredCheques)
                {
                    Cheques.Add(cheque);
                }

                StatusMessage = $"Found {filteredCheques.Count} cheques matching search criteria";
                LastUpdated = DateTime.Now;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching cheques: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearFiltersExecute(object? parameter)
        {
            SearchText = string.Empty;
            ChequeNumberFilter = string.Empty;
            GrowerNumberFilter = string.Empty;
            StatusFilter = "All";
            
            // Reload all cheques
            _ = LoadChequesAsync();
        }

        private async Task<List<Cheque>> ApplyFiltersAsync()
        {
            var allCheques = await _chequeService.GetChequesByStatusAsync("Generated");
            var filtered = allCheques.AsEnumerable();

            // Apply status filter
            if (StatusFilter != "All")
            {
                filtered = filtered.Where(c => c.Status == StatusFilter);
            }

            // Apply search text filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(c => 
                    (c.ChequeNumber?.ToLower().Contains(searchLower) ?? false) ||
                    (c.GrowerName?.ToLower().Contains(searchLower) ?? false) ||
                    c.ChequeAmount.ToString().Contains(searchLower));
            }

            // Apply cheque number filter
            if (!string.IsNullOrWhiteSpace(ChequeNumberFilter))
            {
                filtered = filtered.Where(c => 
                    c.ChequeNumber?.Contains(ChequeNumberFilter) ?? false);
            }

            // Apply grower number filter
            if (!string.IsNullOrWhiteSpace(GrowerNumberFilter))
            {
                filtered = filtered.Where(c => 
                    c.GrowerId.ToString().Contains(GrowerNumberFilter));
            }

            return filtered.ToList();
        }

        #endregion
    }
}
