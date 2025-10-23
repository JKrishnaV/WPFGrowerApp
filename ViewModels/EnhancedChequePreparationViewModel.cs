using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// Enhanced Cheque Preparation ViewModel with support for all three payment types
    /// </summary>
    public class EnhancedChequePreparationViewModel : ViewModelBase, IDisposable
    {
        private readonly IChequeService _chequeService;
        private readonly IAdvanceChequeService _advanceChequeService;
        private readonly ICrossBatchPaymentService _crossBatchPaymentService;
        private readonly IUnifiedVoidingService _unifiedVoidingService;
        private readonly WPFGrowerApp.Services.ChequePdfGenerator _pdfGenerator;
        private readonly WPFGrowerApp.Services.EnhancedChequePdfGenerator _enhancedPdfGenerator;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;

        // Auto-search functionality
        private CancellationTokenSource? _searchCancellationTokenSource;

        // Collections
        private ObservableCollection<ChequeItem> _chequeItems;
        private ObservableCollection<ChequeGroup> _chequeGroups;
        private ObservableCollection<ChequeItem> _filteredChequeItems;

        // Selected items
        private ChequeItem _selectedChequeItem;
        private ChequeGroup _selectedChequeGroup;

        // Form properties
        private string _searchText;
        private string _chequeNumberFilter;
        private string _growerNumberFilter;
        private string _statusFilter;
        private ChequePaymentType? _selectedTypeFilter;
        private DateTime _startDate;
        private DateTime _endDate;

        // Statistics
        private int _totalCheques;
        private decimal _totalAmount;
        private int _regularCheques;
        private int _advanceCheques;
        private int _consolidatedCheques;
        private decimal _regularAmount;
        private decimal _advanceAmount;
        private decimal _consolidatedAmount;

        // Commands
        public ICommand SelectAllCommand { get; }
        public ICommand ClearSelectionCommand { get; }
        public ICommand PrintSelectedCommand { get; }
        public ICommand PreviewSelectedCommand { get; }
        public ICommand PreviewSingleCommand { get; }
        public ICommand GeneratePdfCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand VoidSelectedCommand { get; }
        public ICommand VoidSingleCommand { get; }
        public ICommand StopPaymentCommand { get; }
        public ICommand StopSingleCommand { get; }
        public ICommand ReprintSelectedCommand { get; }
        public ICommand ViewCalculationDetailsCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand GroupByTypeCommand { get; }
        public ICommand UngroupCommand { get; }
        
        // Navigation Commands
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToPaymentManagementCommand { get; }

        public EnhancedChequePreparationViewModel(
            IChequeService chequeService,
            IAdvanceChequeService advanceChequeService,
            ICrossBatchPaymentService crossBatchPaymentService,
            IUnifiedVoidingService unifiedVoidingService,
            WPFGrowerApp.Services.ChequePdfGenerator pdfGenerator,
            WPFGrowerApp.Services.EnhancedChequePdfGenerator enhancedPdfGenerator,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider)
        {
            _chequeService = chequeService;
            _advanceChequeService = advanceChequeService;
            _crossBatchPaymentService = crossBatchPaymentService;
            _unifiedVoidingService = unifiedVoidingService;
            _pdfGenerator = pdfGenerator;
            _enhancedPdfGenerator = enhancedPdfGenerator;
            _dialogService = dialogService;
            _helpContentProvider = helpContentProvider;

            // Initialize collections
            ChequeItems = new ObservableCollection<ChequeItem>();
            ChequeGroups = new ObservableCollection<ChequeGroup>();
            FilteredChequeItems = new ObservableCollection<ChequeItem>();

            // Initialize form properties
            SearchText = string.Empty;
            ChequeNumberFilter = string.Empty;
            GrowerNumberFilter = string.Empty;
            StatusFilter = "All";
            SelectedTypeFilter = null; // null represents "All"
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now.AddYears(1); // Include future dates for advance cheques

            // Initialize commands
            SelectAllCommand = new RelayCommand(async p => await SelectAllAsync());
            ClearSelectionCommand = new RelayCommand(async p => await ClearSelectionAsync());
            PrintSelectedCommand = new RelayCommand(async p => await PrintSelectedAsync(), p => CanPrintSelected());
            PreviewSelectedCommand = new RelayCommand(async p => await PreviewSelectedAsync(), p => CanPreviewSelected());
            PreviewSingleCommand = new RelayCommand(async p => await PreviewSingleAsync(), p => CanPreviewSingle());
            GeneratePdfCommand = new RelayCommand(async p => await GeneratePdfAsync(), p => CanGeneratePdf());
            RefreshCommand = new RelayCommand(async p => await RefreshAsync());
            VoidSelectedCommand = new RelayCommand(async p => await VoidSelectedAsync(), p => CanVoidSelected());
            VoidSingleCommand = new RelayCommand(async p => await VoidSingleAsync(), p => CanVoidSingle());
            StopPaymentCommand = new RelayCommand(async p => await StopPaymentAsync(), p => CanStopPayment());
            StopSingleCommand = new RelayCommand(async p => await StopSingleAsync(), p => CanStopSingle());
            ReprintSelectedCommand = new RelayCommand(async p => await ReprintSelectedAsync(), p => CanReprintSelected());
            ViewCalculationDetailsCommand = new RelayCommand(async p => await ViewCalculationDetailsAsync(p));
            ShowHelpCommand = new RelayCommand(async p => await ShowHelpAsync());
            SearchCommand = new RelayCommand(async p => await SearchAsync());
            ClearFiltersCommand = new RelayCommand(async p => await ClearFiltersAsync());
            ExportCommand = new RelayCommand(async p => await ExportAsync(), p => CanExport());
            GroupByTypeCommand = new RelayCommand(async p => await GroupByTypeAsync());
            UngroupCommand = new RelayCommand(async p => await UngroupAsync());
            
            // Initialize navigation commands
            NavigateToDashboardCommand = new RelayCommand(p => NavigateToDashboard());
            NavigateToPaymentManagementCommand = new RelayCommand(p => NavigateToPaymentManagement());

            // Load initial data
            _ = LoadChequesAsync();
        }

        #region Properties

        public ObservableCollection<ChequeItem> ChequeItems
        {
            get => _chequeItems;
            set => SetProperty(ref _chequeItems, value);
        }

        public ObservableCollection<ChequeGroup> ChequeGroups
        {
            get => _chequeGroups;
            set => SetProperty(ref _chequeGroups, value);
        }

        public ObservableCollection<ChequeItem> FilteredChequeItems
        {
            get => _filteredChequeItems;
            set 
            {
                // Detach handlers from old collection
                if (_filteredChequeItems != null)
                {
                    _filteredChequeItems.CollectionChanged -= FilteredChequeItems_CollectionChanged;
                    foreach (var item in _filteredChequeItems)
                    {
                        item.PropertyChanged -= ChequeItem_PropertyChanged;
                    }
                }

                SetProperty(ref _filteredChequeItems, value);

                // Attach handlers to new collection
                if (_filteredChequeItems != null)
                {
                    _filteredChequeItems.CollectionChanged += FilteredChequeItems_CollectionChanged;
                    foreach (var item in _filteredChequeItems)
                    {
                        item.PropertyChanged += ChequeItem_PropertyChanged;
                    }
                }
            }
        }

        public ChequeItem SelectedChequeItem
        {
            get => _selectedChequeItem;
            set => SetProperty(ref _selectedChequeItem, value);
        }

        public ChequeGroup SelectedChequeGroup
        {
            get => _selectedChequeGroup;
            set => SetProperty(ref _selectedChequeGroup, value);
        }

        public string SearchText
        {
            get => _searchText;
            set 
            {
                if (SetProperty(ref _searchText, value))
                {
                    TriggerAutoSearch();
                }
            }
        }

        public string ChequeNumberFilter
        {
            get => _chequeNumberFilter;
            set 
            {
                if (SetProperty(ref _chequeNumberFilter, value))
                {
                    TriggerAutoSearch();
                }
            }
        }

        public string GrowerNumberFilter
        {
            get => _growerNumberFilter;
            set 
            {
                if (SetProperty(ref _growerNumberFilter, value))
                {
                    TriggerAutoSearch();
                }
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set 
            {
                if (SetProperty(ref _statusFilter, value))
                {
                    TriggerAutoSearch();
                }
            }
        }

        public ChequePaymentType? SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set 
            {
                if (SetProperty(ref _selectedTypeFilter, value))
                {
                    TriggerAutoSearch();
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set 
            {
                if (SetProperty(ref _startDate, value))
                {
                    TriggerAutoSearch();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set 
            {
                if (SetProperty(ref _endDate, value))
                {
                    TriggerAutoSearch();
                }
            }
        }

        public int TotalCheques
        {
            get => _totalCheques;
            set => SetProperty(ref _totalCheques, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public int RegularCheques
        {
            get => _regularCheques;
            set => SetProperty(ref _regularCheques, value);
        }

        public int AdvanceCheques
        {
            get => _advanceCheques;
            set => SetProperty(ref _advanceCheques, value);
        }

        public int ConsolidatedCheques
        {
            get => _consolidatedCheques;
            set => SetProperty(ref _consolidatedCheques, value);
        }

        public decimal RegularAmount
        {
            get => _regularAmount;
            set => SetProperty(ref _regularAmount, value);
        }

        public decimal AdvanceAmount
        {
            get => _advanceAmount;
            set => SetProperty(ref _advanceAmount, value);
        }

        public decimal ConsolidatedAmount
        {
            get => _consolidatedAmount;
            set => SetProperty(ref _consolidatedAmount, value);
        }

        // Computed properties
        public string TotalAmountDisplay => TotalAmount.ToString("C");
        public string RegularAmountDisplay => RegularAmount.ToString("C");
        public string AdvanceAmountDisplay => AdvanceAmount.ToString("C");
        public string ConsolidatedAmountDisplay => ConsolidatedAmount.ToString("C");
        public string RegularChequesDisplay => $"{RegularCheques} regular cheque{(RegularCheques != 1 ? "s" : "")}";
        public string AdvanceChequesDisplay => $"{AdvanceCheques} advance cheque{(AdvanceCheques != 1 ? "s" : "")}";
        public string ConsolidatedChequesDisplay => $"{ConsolidatedCheques} consolidated cheque{(ConsolidatedCheques != 1 ? "s" : "")}";
        public bool HasSelectedChequeItem => SelectedChequeItem != null;
        public bool HasSelectedChequeGroup => SelectedChequeGroup != null;
        public bool HasFilteredChequeItems => FilteredChequeItems.Any();
        public bool HasChequeGroups => ChequeGroups.Any();

        #endregion

        #region Command Methods

        private async Task SelectAllAsync()
        {
            try
            {
                foreach (var cheque in FilteredChequeItems)
                {
                    cheque.IsSelected = true;
                }
                RefreshCommandStates();
                await _dialogService.ShowMessageBoxAsync($"Selected {FilteredChequeItems.Count} cheques", "Selection Updated");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error selecting all cheques: {ex.Message}", "Error");
            }
        }

        private async Task ClearSelectionAsync()
        {
            try
            {
                foreach (var cheque in FilteredChequeItems)
                {
                    cheque.IsSelected = false;
                }
                RefreshCommandStates();
                await _dialogService.ShowMessageBoxAsync("Selection cleared", "Selection Updated");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error clearing selection: {ex.Message}", "Error");
            }
        }

        private async Task PrintSelectedAsync()
        {
            try
            {
                var selectedCheques = FilteredChequeItems.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select cheques to print", "Validation Error");
                    return;
                }

                var result = await _dialogService.ShowConfirmationDialogAsync(
                    $"Print {selectedCheques.Count} selected cheques?",
                    "Confirm Print");

                if (result)
                {
                    IsBusy = true;
                    
                    try
                    {
                        // Generate PDF using the enhanced PDF generator
                        var pdfBytes = await _enhancedPdfGenerator.GenerateBatchChequePdfAsync(selectedCheques);
                        var fileName = $"Cheques_Print_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        var filePath = System.IO.Path.Combine(desktopPath, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                        // Mark cheques as printed in database
                        var regularChequeIds = selectedCheques.Where(c => c.PaymentType == ChequePaymentType.Regular).Select(c => c.ChequeId).ToList();
                        var advanceChequeIds = selectedCheques.Where(c => c.PaymentType == ChequePaymentType.Advance).Select(c => c.AdvanceChequeId).ToList();
                        var consolidatedChequeIds = selectedCheques.Where(c => c.PaymentType == ChequePaymentType.Consolidated).Select(c => c.ChequeId).ToList();

                        var printedBy = App.CurrentUser?.Username ?? "SYSTEM";

                        // Mark regular cheques as printed
                        if (regularChequeIds.Any())
                        {
                            await _chequeService.MarkChequesAsPrintedAsync(regularChequeIds, printedBy);
                        }

                        // Mark advance cheques as printed
                        if (advanceChequeIds.Any())
                        {
                            foreach (var advanceId in advanceChequeIds.Where(id => id.HasValue))
                            {
                                await _advanceChequeService.PrintAdvanceChequeAsync(advanceId.Value, printedBy);
                            }
                        }

                        // Mark consolidated cheques as printed
                        if (consolidatedChequeIds.Any())
                        {
                            await _chequeService.MarkChequesAsPrintedAsync(consolidatedChequeIds, printedBy);
                        }

                        // Update PaymentDistributionItem status for printed cheques
                        await UpdatePaymentDistributionItemsForPrintedChequesAsync(selectedCheques, printedBy);

                        // Update local collection
                        foreach (var cheque in selectedCheques)
                        {
                            cheque.Status = "Printed";
                        }

                        await _dialogService.ShowMessageBoxAsync(
                            $"Successfully printed {selectedCheques.Count} cheques. File saved to: {fileName}", 
                            "Print Complete");

                        // Open the PDF file for immediate printing
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });

                        // Refresh to show updated status
                        await LoadChequesAsync();
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error printing cheques: {ex.Message}", "Error");
                IsBusy = false;
            }
        }

        private async Task PreviewSelectedAsync()
        {
            try
            {
                var selectedCheques = FilteredChequeItems.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select cheques to preview", "Validation Error");
                    return;
                }

                IsBusy = true;
                
                try
                {
                    // Generate preview PDF with watermark
                    var pdfBytes = await _enhancedPdfGenerator.GenerateBatchChequePreviewPdfAsync(selectedCheques);
                    var fileName = $"Cheques_Preview_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var filePath = System.IO.Path.Combine(desktopPath, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                    await _dialogService.ShowMessageBoxAsync(
                        $"Preview generated for {selectedCheques.Count} cheques. File saved to: {fileName}", 
                        "Preview Complete");

                    // Open the PDF file for preview
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                finally
                {
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error previewing cheques: {ex.Message}", "Error");
                IsBusy = false;
            }
        }

        private async Task PreviewSingleAsync()
        {
            try
            {
                if (SelectedChequeItem == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a cheque to preview", "Validation Error");
                    return;
                }

                IsBusy = true;
                
                try
                {
                    // Generate preview PDF with watermark for single cheque
                    var pdfBytes = await _enhancedPdfGenerator.GenerateSingleChequePreviewPdfAsync(SelectedChequeItem);
                    var fileName = $"Cheque_Preview_{SelectedChequeItem.ChequeNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var filePath = System.IO.Path.Combine(desktopPath, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                    await _dialogService.ShowMessageBoxAsync(
                        $"Preview generated for cheque {SelectedChequeItem.ChequeNumber}. File saved to: {fileName}", 
                        "Preview Complete");

                    // Open the PDF file for preview
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                finally
                {
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error previewing cheque: {ex.Message}", "Error");
                IsBusy = false;
            }
        }

        private async Task GeneratePdfAsync()
        {
            try
            {
                var selectedCheques = FilteredChequeItems.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select cheques to generate PDF", "Validation Error");
                    return;
                }

                // Implementation for generating PDF
                await _dialogService.ShowMessageBoxAsync("PDF generation functionality will be implemented in a future update", "Generate PDF");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error generating PDF: {ex.Message}", "Error");
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                await LoadChequesAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error refreshing data: {ex.Message}", "Error");
            }
        }

        private async Task VoidSelectedAsync()
        {
            try
            {
                var selectedCheques = FilteredChequeItems.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select cheques to void", "Validation Error");
                    return;
                }

                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for voiding:",
                    "Void Cheques");

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    var result = await _dialogService.ShowConfirmationDialogAsync(
                        $"Void {selectedCheques.Count} selected cheques?",
                        "Confirm Void");

                    if (result)
                    {
                        IsBusy = true;
                        var voidedCount = 0;
                        var failedCount = 0;
                        var voidedBy = App.CurrentUser?.Username ?? Environment.UserName ?? "SYSTEM";

                        foreach (var cheque in selectedCheques)
                        {
                            try
                            {
                                // Validate cheque data before voiding
                                if (cheque == null)
                                {
                                    failedCount++;
                                    continue;
                                }

                                var entityId = GetEntityId(cheque);
                                if (entityId <= 0)
                                {
                                    failedCount++;
                                    continue;
                                }

                                var voidRequest = new PaymentVoidRequest
                                {
                                    EntityType = "cheque", // Use generic "cheque" type to trigger auto-detection
                                    EntityId = entityId,
                                    Reason = reason,
                                    VoidedBy = voidedBy,
                                    ReverseDeductions = true,
                                    RestoreBatchStatus = true
                                };

                                var voidResult = await _unifiedVoidingService.VoidPaymentAsync(voidRequest);
                                
                                if (voidResult.Success)
                                {
                                    voidedCount++;
                                    try
                                    {
                                        Logger.Info($"Successfully voided {cheque.PaymentType} cheque {cheque.ChequeNumber}");
                                    }
                                    catch
                                    {
                                        // Logger might be unavailable, continue without logging
                                    }
                                }
                                else
                                {
                                    failedCount++;
                                    try
                                    {
                                        Logger.Error($"Failed to void {cheque.PaymentType} cheque {cheque.ChequeNumber}: {voidResult.Message}");
                                    }
                                    catch
                                    {
                                        // Logger might be unavailable, continue without logging
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                failedCount++;
                                try
                                {
                                    Logger.Error($"Error voiding {cheque.PaymentType} cheque {cheque.ChequeNumber}: {ex.Message}", ex);
                                }
                                catch
                                {
                                    // Logger might be unavailable, continue without logging
                                }
                            }
                        }

                        // Refresh the cheque list
                        await LoadChequesAsync();

                        // Show result message
                        if (voidedCount > 0 && failedCount == 0)
                        {
                            await _dialogService.ShowMessageBoxAsync(
                                $"Successfully voided {voidedCount} cheques.",
                                "Void Complete");
                        }
                        else if (voidedCount > 0 && failedCount > 0)
                        {
                            await _dialogService.ShowMessageBoxAsync(
                                $"Voided {voidedCount} cheques successfully, {failedCount} failed. Please check the logs for details.",
                                "Partial Success");
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync(
                                $"Failed to void any cheques. Please check the logs for details.",
                                "Void Failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error voiding cheques: {ex.Message}", "Error");
                Logger.Error($"Error in VoidSelectedAsync: {ex.Message}", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task VoidSingleAsync()
        {
            try
            {
                if (SelectedChequeItem == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a cheque to void", "Validation Error");
                    return;
                }

                // Capture cheque information before async operations
                var chequeToVoid = SelectedChequeItem;
                var chequeNumber = chequeToVoid.ChequeNumber;
                var paymentType = chequeToVoid.PaymentType;

                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for voiding:",
                    "Void Cheque");

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    var result = await _dialogService.ShowConfirmationDialogAsync(
                        $"Void cheque {chequeNumber}?",
                        "Confirm Void");

                    if (result)
                    {
                        IsBusy = true;
                        var voidedBy = App.CurrentUser?.Username ?? Environment.UserName ?? "SYSTEM";

                        try
                        {
                            var voidRequest = new PaymentVoidRequest
                            {
                                EntityType = "cheque", // Use generic "cheque" type to trigger auto-detection
                                EntityId = GetEntityId(chequeToVoid),
                                Reason = reason,
                                VoidedBy = voidedBy,
                                ReverseDeductions = true,
                                RestoreBatchStatus = true
                            };

                            var voidResult = await _unifiedVoidingService.VoidPaymentAsync(voidRequest);
                            
                            if (voidResult.Success)
                            {
                                Logger.Info($"Successfully voided {paymentType} cheque {chequeNumber}");
                                
                                // Refresh the cheque list
                                await LoadChequesAsync();
                                
                                await _dialogService.ShowMessageBoxAsync(
                                    $"Successfully voided cheque {chequeNumber}.",
                                    "Void Complete");
                            }
                            else
                            {
                                Logger.Error($"Failed to void {paymentType} cheque {chequeNumber}: {voidResult.Message}");
                                await _dialogService.ShowMessageBoxAsync(
                                    $"Failed to void cheque: {voidResult.Message}",
                                    "Void Failed");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error voiding {paymentType} cheque {chequeNumber}: {ex.Message}", ex);
                            await _dialogService.ShowMessageBoxAsync(
                                $"Error voiding cheque: {ex.Message}",
                                "Void Failed");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error voiding cheque: {ex.Message}", "Error");
                Logger.Error($"Error in VoidSingleAsync: {ex.Message}", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task StopPaymentAsync()
        {
            try
            {
                var selectedCheques = FilteredChequeItems.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select cheques to stop payment", "Validation Error");
                    return;
                }

                // Implementation for stopping payment
                await _dialogService.ShowMessageBoxAsync("Stop payment functionality will be implemented in a future update", "Stop Payment");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error stopping payment: {ex.Message}", "Error");
            }
        }

        private async Task StopSingleAsync()
        {
            try
            {
                if (SelectedChequeItem == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a cheque to stop payment", "Validation Error");
                    return;
                }

                // Implementation for stopping single payment
                await _dialogService.ShowMessageBoxAsync("Stop payment functionality will be implemented in a future update", "Stop Payment");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error stopping payment: {ex.Message}", "Error");
            }
        }

        private async Task ReprintSelectedAsync()
        {
            try
            {
                var selectedCheques = FilteredChequeItems.Where(c => c.IsSelected).ToList();
                if (!selectedCheques.Any())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select cheques to reprint", "Validation Error");
                    return;
                }

                // Implementation for reprinting selected cheques
                await _dialogService.ShowMessageBoxAsync("Reprint functionality will be implemented in a future update", "Reprint Cheques");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error reprinting cheques: {ex.Message}", "Error");
            }
        }

        private async Task ShowHelpAsync()
        {
            try
            {
                var helpContent = _helpContentProvider.GetHelpContent("EnhancedChequePreparationView");
                await _dialogService.ShowMessageBoxAsync(helpContent.Content, "Cheque Preparation Help");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error showing help: {ex.Message}", "Error");
            }
        }

        private async Task SearchAsync()
        {
            try
            {
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error searching: {ex.Message}", "Error");
            }
        }

        private async Task ClearFiltersAsync()
        {
            try
            {
                SearchText = string.Empty;
                ChequeNumberFilter = string.Empty;
                GrowerNumberFilter = string.Empty;
                StatusFilter = "All";
                SelectedTypeFilter = null; // null represents "All"
                StartDate = DateTime.Now.AddMonths(-1);
                EndDate = DateTime.Now.AddYears(1); // Include future dates for advance cheques
                await ApplyFiltersAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error clearing filters: {ex.Message}", "Error");
            }
        }

        private async Task ExportAsync()
        {
            try
            {
                // Implementation for exporting data
                await _dialogService.ShowMessageBoxAsync("Export functionality will be implemented in a future update", "Export");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error exporting data: {ex.Message}", "Error");
            }
        }

        private async Task GroupByTypeAsync()
        {
            try
            {
                await GroupChequesByTypeAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error grouping cheques: {ex.Message}", "Error");
            }
        }

        private async Task UngroupAsync()
        {
            try
            {
                ChequeGroups.Clear();
                await _dialogService.ShowMessageBoxAsync("Cheques ungrouped", "Ungroup Cheques");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error ungrouping cheques: {ex.Message}", "Error");
            }
        }

        #endregion

        #region Command CanExecute Methods

        private bool CanPrintSelected()
        {
            var selectedAndPrintable = FilteredChequeItems.Where(c => c.IsSelected && c.CanBePrinted).ToList();
            return selectedAndPrintable.Any() && !IsBusy;
        }

        private void RefreshCommandStates()
        {
            if (PrintSelectedCommand is RelayCommand printCmd)
                printCmd.RaiseCanExecuteChanged();
            if (PreviewSelectedCommand is RelayCommand previewCmd)
                previewCmd.RaiseCanExecuteChanged();
            if (VoidSelectedCommand is RelayCommand voidCmd)
                voidCmd.RaiseCanExecuteChanged();
        }

        private void FilteredChequeItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle items being added
            if (e.NewItems != null)
            {
                foreach (ChequeItem item in e.NewItems)
                {
                    item.PropertyChanged += ChequeItem_PropertyChanged;
                }
            }

            // Handle items being removed
            if (e.OldItems != null)
            {
                foreach (ChequeItem item in e.OldItems)
                {
                    item.PropertyChanged -= ChequeItem_PropertyChanged;
                }
            }

            RefreshCommandStates();
        }

        private void ChequeItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChequeItem.IsSelected))
            {
                RefreshCommandStates();
            }
        }

        private bool CanPreviewSelected()
        {
            return FilteredChequeItems.Any(c => c.IsSelected) && !IsBusy;
        }

        private bool CanPreviewSingle()
        {
            return HasSelectedChequeItem && !IsBusy;
        }

        private bool CanGeneratePdf()
        {
            return FilteredChequeItems.Any(c => c.IsSelected) && !IsBusy;
        }

        private bool CanVoidSelected()
        {
            return FilteredChequeItems.Any(c => c.IsSelected && c.CanBeVoided) && !IsBusy;
        }

        private bool CanVoidSingle()
        {
            return HasSelectedChequeItem && SelectedChequeItem.CanBeVoided && !IsBusy;
        }

        private bool CanStopPayment()
        {
            return FilteredChequeItems.Any(c => c.IsSelected) && !IsBusy;
        }

        private bool CanStopSingle()
        {
            return HasSelectedChequeItem && !IsBusy;
        }

        private bool CanReprintSelected()
        {
            return FilteredChequeItems.Any(c => c.IsSelected) && !IsBusy;
        }

        private bool CanExport()
        {
            return HasFilteredChequeItems && !IsBusy;
        }

        #endregion

        #region Private Methods

        private async Task LoadChequesAsync()
        {
            try
            {
                IsBusy = true;

                // Load regular cheques
                var regularCheques = await _chequeService.GetChequesByStatusAsync("Generated");
                
                // Load advance cheques
                var advanceCheques = await _advanceChequeService.GetAllAdvanceChequesAsync("Generated");
                
                // Load consolidated cheques
                var consolidatedCheques = await _chequeService.GetChequesByStatusAsync("Generated");

                // Convert to ChequeItem objects
                ChequeItems.Clear();
                foreach (var cheque in regularCheques)
                {
                    var chequeItem = new ChequeItem(cheque);
                    
                    // Load advance deductions for this cheque
                    var advanceDeductions = await _chequeService.GetAdvanceDeductionsByChequeNumberAsync(cheque.ChequeNumber);
                    chequeItem.AdvanceDeductions.Clear();
                    foreach (var deduction in advanceDeductions)
                    {
                        chequeItem.AdvanceDeductions.Add(deduction);
                    }
                    
                    ChequeItems.Add(chequeItem);
                }

                // Add advance cheques
                foreach (var advance in advanceCheques)
                {
                    var chequeItem = new ChequeItem
                    {
                        ChequeId = advance.AdvanceChequeId,
                        ChequeNumber = $"ADV-{advance.AdvanceChequeId}",
                        GrowerId = advance.GrowerId,
                        GrowerName = advance.GrowerName ?? "Unknown",
                        GrowerNumber = advance.GrowerNumber ?? "N/A",
                        Amount = advance.AdvanceAmount,
                        ChequeDate = advance.AdvanceDate,
                        Status = advance.Status ?? "Unknown",
                        PaymentType = ChequePaymentType.Advance,
                        IsAdvanceCheque = true,
                        AdvanceChequeId = advance.AdvanceChequeId,
                        AdvanceReason = advance.Reason ?? string.Empty,
                        CanBeVoided = advance.CanBeVoided,
                        CanBePrinted = advance.CanBePrinted,
                        CanBeIssued = false
                    };
                    ChequeItems.Add(chequeItem);
                }

                // Add consolidated cheques
                foreach (var cheque in consolidatedCheques.Where(c => c.IsConsolidated))
                {
                    var chequeItem = new ChequeItem(cheque);
                    chequeItem.PaymentType = ChequePaymentType.Consolidated;
                    
                    // Load advance deductions for this cheque
                    var advanceDeductions = await _chequeService.GetAdvanceDeductionsByChequeNumberAsync(cheque.ChequeNumber);
                    chequeItem.AdvanceDeductions.Clear();
                    foreach (var deduction in advanceDeductions)
                    {
                        chequeItem.AdvanceDeductions.Add(deduction);
                    }
                    
                    ChequeItems.Add(chequeItem);
                }

                // Apply filters first (this will populate FilteredChequeItems)
                await ApplyFiltersAsync();

                // Then calculate statistics from the filtered results
                await CalculateStatisticsAsync();
                
                // Force UI refresh by raising property change notifications again on UI thread
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    OnPropertyChanged(nameof(TotalCheques));
                    OnPropertyChanged(nameof(TotalAmount));
                    OnPropertyChanged(nameof(AdvanceCheques));
                    OnPropertyChanged(nameof(AdvanceAmount));
                    OnPropertyChanged(nameof(AdvanceChequesDisplay));
                    OnPropertyChanged(nameof(AdvanceAmountDisplay));
                    OnPropertyChanged(nameof(TotalAmountDisplay));
                });
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error loading cheques: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private Task CalculateStatisticsAsync()
        {
            try
            {
                // Calculate statistics from the filtered items (what's actually displayed in the UI)
                // This ensures the statistics match what the user sees in the table
                var itemsToCalculate = FilteredChequeItems.Any() ? FilteredChequeItems : ChequeItems;
                
                TotalCheques = itemsToCalculate.Count;
                TotalAmount = itemsToCalculate.Sum(c => c.Amount);
                RegularCheques = itemsToCalculate.Count(c => c.PaymentType == ChequePaymentType.Regular);
                AdvanceCheques = itemsToCalculate.Count(c => c.PaymentType == ChequePaymentType.Advance);
                ConsolidatedCheques = itemsToCalculate.Count(c => c.PaymentType == ChequePaymentType.Consolidated);
                RegularAmount = itemsToCalculate.Where(c => c.PaymentType == ChequePaymentType.Regular).Sum(c => c.Amount);
                AdvanceAmount = itemsToCalculate.Where(c => c.PaymentType == ChequePaymentType.Advance).Sum(c => c.Amount);
                ConsolidatedAmount = itemsToCalculate.Where(c => c.PaymentType == ChequePaymentType.Consolidated).Sum(c => c.Amount);
                
                // Explicitly raise property change notifications to ensure UI updates
                OnPropertyChanged(nameof(TotalCheques));
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(RegularCheques));
                OnPropertyChanged(nameof(AdvanceCheques));
                OnPropertyChanged(nameof(ConsolidatedCheques));
                OnPropertyChanged(nameof(RegularAmount));
                OnPropertyChanged(nameof(AdvanceAmount));
                OnPropertyChanged(nameof(ConsolidatedAmount));
                OnPropertyChanged(nameof(TotalAmountDisplay));
                OnPropertyChanged(nameof(RegularAmountDisplay));
                OnPropertyChanged(nameof(AdvanceAmountDisplay));
                OnPropertyChanged(nameof(ConsolidatedAmountDisplay));
                OnPropertyChanged(nameof(RegularChequesDisplay));
                OnPropertyChanged(nameof(AdvanceChequesDisplay));
                OnPropertyChanged(nameof(ConsolidatedChequesDisplay));
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating statistics: {ex.Message}", ex);
            }
            
            return Task.CompletedTask;
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                var filtered = ChequeItems.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    filtered = filtered.Where(c => 
                        (!string.IsNullOrEmpty(c.GrowerName) && c.GrowerName.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(c.GrowerNumber) && c.GrowerNumber.ToLower().Contains(searchLower)) ||
                        (!string.IsNullOrEmpty(c.ChequeNumber) && c.ChequeNumber.ToLower().Contains(searchLower)));
                }

                // Apply cheque number filter
                if (!string.IsNullOrWhiteSpace(ChequeNumberFilter))
                {
                    filtered = filtered.Where(c => !string.IsNullOrEmpty(c.ChequeNumber) && c.ChequeNumber.Contains(ChequeNumberFilter));
                }

                // Apply grower number filter
                if (!string.IsNullOrWhiteSpace(GrowerNumberFilter))
                {
                    filtered = filtered.Where(c => !string.IsNullOrEmpty(c.GrowerNumber) && c.GrowerNumber.Contains(GrowerNumberFilter));
                }

                // Apply status filter
                if (StatusFilter != "All")
                {
                    filtered = filtered.Where(c => !string.IsNullOrEmpty(c.Status) && c.Status == StatusFilter);
                }

                // Apply type filter
                if (SelectedTypeFilter.HasValue)
                {
                    filtered = filtered.Where(c => c.PaymentType == SelectedTypeFilter.Value);
                }

                // Apply date filter
                filtered = filtered.Where(c => c.ChequeDate >= StartDate && c.ChequeDate <= EndDate);

                // Update filtered collection
                FilteredChequeItems.Clear();
                var orderedCheques = filtered.OrderByDescending(c => c.ChequeDate).ToList();
                foreach (var cheque in orderedCheques)
                {
                    FilteredChequeItems.Add(cheque);
                }
                
                // Recalculate statistics based on filtered results
                await CalculateStatisticsAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error applying filters: {ex.Message}", "Error");
            }
        }

        private async Task GroupChequesByTypeAsync()
        {
            try
            {
                ChequeGroups.Clear();

                // Group by payment type
                var regularGroup = new ChequeGroup(ChequePaymentType.Regular, "Regular Payments");
                var advanceGroup = new ChequeGroup(ChequePaymentType.Advance, "Advance Cheques");
                var consolidatedGroup = new ChequeGroup(ChequePaymentType.Consolidated, "Consolidated Payments");

                foreach (var cheque in FilteredChequeItems)
                {
                    switch (cheque.PaymentType)
                    {
                        case ChequePaymentType.Regular:
                            regularGroup.AddCheque(cheque);
                            break;
                        case ChequePaymentType.Advance:
                            advanceGroup.AddCheque(cheque);
                            break;
                        case ChequePaymentType.Consolidated:
                            consolidatedGroup.AddCheque(cheque);
                            break;
                    }
                }

                if (regularGroup.Cheques.Any())
                    ChequeGroups.Add(regularGroup);
                if (advanceGroup.Cheques.Any())
                    ChequeGroups.Add(advanceGroup);
                if (consolidatedGroup.Cheques.Any())
                    ChequeGroups.Add(consolidatedGroup);

                await _dialogService.ShowMessageBoxAsync($"Grouped {ChequeGroups.Count} types with {FilteredChequeItems.Count} total cheques", "Grouped by Type");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error grouping cheques: {ex.Message}", "Error");
            }
        }

        #endregion

        #region Property Change Handlers

        protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Call base implementation to ensure proper property change notification
            base.OnPropertyChanged(propertyName);
            
            // Update command states when relevant properties change
            if (propertyName == nameof(SelectedChequeItem) || 
                propertyName == nameof(IsBusy))
            {
                ((RelayCommand)PreviewSingleCommand).RaiseCanExecuteChanged();
                ((RelayCommand)VoidSingleCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopSingleCommand).RaiseCanExecuteChanged();
            }

            if (propertyName == nameof(FilteredChequeItems) || 
                propertyName == nameof(IsBusy))
            {
                ((RelayCommand)PrintSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)PreviewSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)GeneratePdfCommand).RaiseCanExecuteChanged();
                ((RelayCommand)VoidSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopPaymentCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ReprintSelectedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportCommand).RaiseCanExecuteChanged();
            }

            // Auto-apply filters when search criteria change
            if (propertyName == nameof(SearchText) || 
                propertyName == nameof(ChequeNumberFilter) || 
                propertyName == nameof(GrowerNumberFilter) || 
                propertyName == nameof(StatusFilter) || 
                propertyName == nameof(SelectedTypeFilter) || 
                propertyName == nameof(StartDate) || 
                propertyName == nameof(EndDate))
            {
                _ = ApplyFiltersAsync();
            }
        }

        #endregion

        #region Navigation Methods

        private void NavigateToDashboard()
        {
            try
            {
                // Use the NavigationHelper for navigation
                NavigationHelper.NavigateToDashboard();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBoxAsync($"Error navigating to Dashboard: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToPaymentManagement()
        {
            try
            {
                // Use the NavigationHelper for navigation
                NavigationHelper.NavigateToPaymentManagement();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBoxAsync($"Error navigating to Payment Management: {ex.Message}", "Navigation Error");
            }
        }


        #endregion

        #region Auto-Search Functionality

        /// <summary>
        /// Triggers auto-search with debouncing to prevent excessive filtering
        /// </summary>
        private void TriggerAutoSearch()
        {
            // Cancel any existing search operation
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();

            // Create new cancellation token source
            _searchCancellationTokenSource = new CancellationTokenSource();

            // Start debounced search task
            _ = Task.Run(async () =>
            {
                try
                {
                    // Wait for 300ms to debounce
                    await Task.Delay(300, _searchCancellationTokenSource.Token);

                    // Apply filters on UI thread
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await ApplyFiltersAsync();
                    });
                }
                catch (OperationCanceledException)
                {
                    // Search was cancelled, ignore
                }
                catch (Exception ex)
                {
                    // Log error but don't show to user during auto-search
                    Logger.Error($"Auto-search error: {ex.Message}", ex);
                }
            });
        }

        /// <summary>
        /// Cleanup method for auto-search resources
        /// </summary>
        public void Dispose()
        {
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// View calculation details for a specific cheque
        /// </summary>
        private async Task ViewCalculationDetailsAsync(object parameter)
        {
            try
            {
                if (parameter is ChequeItem chequeItem)
                {
                    var dialogViewModel = new ViewModels.Dialogs.ChequeCalculationDialogViewModel(chequeItem);
                    var dialogView = new Views.Dialogs.InvoiceStyleChequeCalculationDialogView(dialogViewModel);
                    dialogView.Owner = Application.Current.MainWindow;
                    dialogView.ShowDialog();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a cheque to view calculation details.", "No Cheque Selected");
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error showing calculation details: {ex.Message}", "Error");
                Logger.Error($"Error in ViewCalculationDetailsAsync: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get the entity type for the void request based on payment type
        /// </summary>
        private string GetEntityType(ChequePaymentType paymentType)
        {
            return paymentType switch
            {
                ChequePaymentType.Regular => "Regular",
                ChequePaymentType.Advance => "Advance",
                ChequePaymentType.Consolidated => "Consolidated",
                _ => "Regular"
            };
        }

        /// <summary>
        /// Get the entity ID for the void request based on payment type
        /// </summary>
        private int GetEntityId(ChequeItem cheque)
        {
            return cheque.PaymentType switch
            {
                ChequePaymentType.Advance => cheque.AdvanceChequeId ?? 0,
                _ => cheque.ChequeId
            };
        }

        /// <summary>
        /// Update PaymentDistributionItem status for printed cheques
        /// </summary>
        private async Task UpdatePaymentDistributionItemsForPrintedChequesAsync(List<ChequeItem> printedCheques, string printedBy)
        {
            try
            {
                foreach (var cheque in printedCheques)
                {
                    // Only update for regular and consolidated cheques (not advance cheques)
                    if (cheque.PaymentType == ChequePaymentType.Regular || cheque.PaymentType == ChequePaymentType.Consolidated)
                    {
                        await UpdatePaymentDistributionItemStatusAsync(cheque.ChequeId, "Completed", printedBy);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the print operation
                System.Diagnostics.Debug.WriteLine($"Error updating payment distribution items: {ex.Message}");
            }
        }

        /// <summary>
        /// Update payment distribution item status when printing a cheque
        /// </summary>
        private async Task UpdatePaymentDistributionItemStatusAsync(int chequeId, string status, string updatedBy)
        {
            try
            {
                using var connection = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString);
                await connection.OpenAsync();

                // Find the payment distribution item for this cheque
                var findSql = @"
                    SELECT pdi.PaymentDistributionItemId, pdi.GrowerId, pdi.PaymentBatchId
                    FROM PaymentDistributionItems pdi
                    INNER JOIN Cheques c ON pdi.GrowerId = c.GrowerId AND pdi.PaymentBatchId = c.PaymentBatchId
                    WHERE c.ChequeId = @ChequeId";

                using var findCommand = new System.Data.SqlClient.SqlCommand(findSql, connection);
                findCommand.Parameters.AddWithValue("@ChequeId", chequeId);
                
                using var reader = await findCommand.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var distributionItemId = reader.GetInt32(reader.GetOrdinal("PaymentDistributionItemId"));
                    var growerId = reader.GetInt32(reader.GetOrdinal("GrowerId"));
                    var paymentBatchId = reader.GetInt32(reader.GetOrdinal("PaymentBatchId"));
                    
                    reader.Close();

                    // Update the payment distribution item status
                    var updateSql = @"
                        UPDATE PaymentDistributionItems 
                        SET Status = @Status,
                            ModifiedAt = @ModifiedAt,
                            ModifiedBy = @ModifiedBy
                        WHERE PaymentDistributionItemId = @PaymentDistributionItemId";

                    using var updateCommand = new System.Data.SqlClient.SqlCommand(updateSql, connection);
                    updateCommand.Parameters.AddWithValue("@Status", status);
                    updateCommand.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                    updateCommand.Parameters.AddWithValue("@ModifiedBy", updatedBy);
                    updateCommand.Parameters.AddWithValue("@PaymentDistributionItemId", distributionItemId);
                    
                    await updateCommand.ExecuteNonQueryAsync();
                    System.Diagnostics.Debug.WriteLine($"Updated payment distribution item {distributionItemId} status to {status} for grower {growerId} in batch {paymentBatchId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating payment distribution item status for cheque {chequeId}: {ex.Message}");
                // Don't throw - this is not critical for the print operation
            }
        }

        #endregion
    }
}
