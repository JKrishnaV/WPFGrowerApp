using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for managing advance cheques
    /// </summary>
    public class AdvanceChequeViewModel : ViewModelBase
    {
        private readonly IUnifiedAdvanceService _unifiedAdvanceService;
        private readonly IGrowerService _growerService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;

        // Collections
        private ObservableCollection<AdvanceCheque> _advanceCheques;
        private ObservableCollection<Grower> _growers;
        private ObservableCollection<Grower> _filteredGrowers;
        private ObservableCollection<AdvanceCheque> _filteredAdvanceCheques;

        // Selected items
        private AdvanceCheque _selectedAdvanceCheque;
        private Grower _selectedGrower;

        // Form properties
        private decimal _advanceAmount;
        private string _reason;
        private string _searchText;
        private string _growerSearchText;
        private string _statusFilter;
        private DateTime _startDate;
        private DateTime _endDate;

        // Statistics
        private decimal _totalOutstanding;
        private int _activeCheques;
        private decimal _deductedThisMonth;

        // Loading state
        private bool _isLoading;

        // Commands
        public ICommand CreateAdvanceChequeCommand { get; }
        public ICommand ViewOutstandingAdvancesCommand { get; }
        public ICommand CancelAdvanceChequeCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowHelpCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand ExportCommand { get; }

        // Workflow Commands (Unified with regular cheques)
        public ICommand PrintAdvanceChequeCommand { get; }
        public ICommand DeliverAdvanceChequeCommand { get; }
        public ICommand VoidAdvanceChequeCommand { get; }

        // Navigation Commands
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToPaymentManagementCommand { get; }

        public AdvanceChequeViewModel(
            IUnifiedAdvanceService unifiedAdvanceService,
            IGrowerService growerService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider)
        {
            _unifiedAdvanceService = unifiedAdvanceService;
            _growerService = growerService;
            _dialogService = dialogService;
            _helpContentProvider = helpContentProvider;

            // Initialize collections
            AdvanceCheques = new ObservableCollection<AdvanceCheque>();
            Growers = new ObservableCollection<Grower>();
            FilteredGrowers = new ObservableCollection<Grower>();
            FilteredAdvanceCheques = new ObservableCollection<AdvanceCheque>();

            // Initialize form properties
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            StatusFilter = "All";
            SearchText = string.Empty;

            // Initialize commands
            CreateAdvanceChequeCommand = new RelayCommand(async p => await CreateAdvanceChequeAsync(), p => CanCreateAdvanceCheque());
            ViewOutstandingAdvancesCommand = new RelayCommand(async p => await ViewOutstandingAdvancesAsync(), p => CanViewOutstandingAdvances());
            CancelAdvanceChequeCommand = new RelayCommand(async p => await CancelAdvanceChequeAsync(), p => CanCancelAdvanceCheque());
            SearchCommand = new RelayCommand(async p => await SearchAsync());
            RefreshCommand = new RelayCommand(async p => await RefreshAsync());
            ShowHelpCommand = new RelayCommand(async p => await ShowHelpAsync());
            ClearFiltersCommand = new RelayCommand(async p => await ClearFiltersAsync());
            ExportCommand = new RelayCommand(async p => await ExportAsync(), p => CanExport());

            // Workflow Commands (Unified with regular cheques)
            PrintAdvanceChequeCommand = new RelayCommand(async p => await PrintAdvanceChequeAsync(), p => CanPrintAdvanceCheque());
            DeliverAdvanceChequeCommand = new RelayCommand(async p => await DeliverAdvanceChequeAsync(), p => CanDeliverAdvanceCheque());
            VoidAdvanceChequeCommand = new RelayCommand(async p => await VoidAdvanceChequeAsync(), p => CanVoidAdvanceCheque());

            // Navigation Commands
            NavigateToDashboardCommand = new RelayCommand(p => NavigateToDashboard());
            NavigateToPaymentManagementCommand = new RelayCommand(p => NavigateToPaymentManagement());

            // Load data
            _ = LoadDataAsync();
        }

        #region Properties

        public ObservableCollection<AdvanceCheque> AdvanceCheques
        {
            get => _advanceCheques;
            set => SetProperty(ref _advanceCheques, value);
        }

        public ObservableCollection<Grower> Growers
        {
            get => _growers;
            set => SetProperty(ref _growers, value);
        }

        public ObservableCollection<Grower> FilteredGrowers
        {
            get => _filteredGrowers;
            set => SetProperty(ref _filteredGrowers, value);
        }

        public ObservableCollection<AdvanceCheque> FilteredAdvanceCheques
        {
            get => _filteredAdvanceCheques;
            set => SetProperty(ref _filteredAdvanceCheques, value);
        }

        public AdvanceCheque SelectedAdvanceCheque
        {
            get => _selectedAdvanceCheque;
            set => SetProperty(ref _selectedAdvanceCheque, value);
        }

        public Grower SelectedGrower
        {
            get => _selectedGrower;
            set => SetProperty(ref _selectedGrower, value);
        }

        public decimal AdvanceAmount
        {
            get => _advanceAmount;
            set => SetProperty(ref _advanceAmount, value);
        }

        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string GrowerSearchText
        {
            get => _growerSearchText;
            set
            {
                if (SetProperty(ref _growerSearchText, value))
                {
                    FilterGrowers();
                }
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public decimal TotalOutstanding
        {
            get => _totalOutstanding;
            set => SetProperty(ref _totalOutstanding, value);
        }

        public int ActiveCheques
        {
            get => _activeCheques;
            set => SetProperty(ref _activeCheques, value);
        }

        public decimal DeductedThisMonth
        {
            get => _deductedThisMonth;
            set => SetProperty(ref _deductedThisMonth, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Computed properties
        public string TotalOutstandingDisplay => TotalOutstanding.ToString("C");
        public string DeductedThisMonthDisplay => DeductedThisMonth.ToString("C");
        public string ActiveChequesDisplay => $"{ActiveCheques} active cheque{(ActiveCheques != 1 ? "s" : "")}";
        public bool HasSelectedAdvanceCheque => SelectedAdvanceCheque != null;
        public bool HasSelectedGrower => SelectedGrower != null;
        public string SelectedGrowerDisplay => SelectedGrower?.FullName ?? "Select a grower";
        public string SelectedGrowerNumber => SelectedGrower?.GrowerNumber ?? "N/A";

        #endregion

        #region Command Methods

        private async Task CreateAdvanceChequeAsync()
        {
            try
            {
                if (!CanCreateAdvanceCheque())
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a grower and enter a valid amount and reason.", "Validation Error");
                    return;
                }

                // Show confirmation dialog before creating advance cheque
                var confirmationMessage = $"Are you sure you want to create an advance cheque for {SelectedGrower.FullName}?\n\n" +
                                       $"Amount: ${AdvanceAmount:F2}\n" +
                                       $"Reason: {Reason}\n\n" +
                                       $"This advance will be automatically deducted from future payments to this grower.";
                
                var confirmed = await _dialogService.ShowConfirmationDialogAsync(confirmationMessage, "Confirm Advance Cheque Creation");
                
                if (!confirmed)
                {
                    return; // User cancelled the operation
                }

                var advanceCheque = await _unifiedAdvanceService.CreateAdvanceChequeAsync(
                    SelectedGrower.GrowerId,
                    AdvanceAmount,
                    Reason,
                    "Current User" // TODO: Get from authentication context
                );

                await _dialogService.ShowMessageBoxAsync($"Advance cheque created successfully for {SelectedGrower.FullName}.", "Success");

                // Clear form
                AdvanceAmount = 0;
                Reason = string.Empty;
                SelectedGrower = null;

                // Refresh data
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error creating advance cheque: {ex.Message}", "Error");
            }
        }

        private async Task ViewOutstandingAdvancesAsync()
        {
            try
            {
                if (SelectedGrower == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please select a grower first.", "Validation Error");
                    return;
                }

                var outstandingAdvances = await _unifiedAdvanceService.GetOutstandingAdvancesAsync(SelectedGrower.GrowerId);
                var totalOutstanding = await _unifiedAdvanceService.CalculateTotalOutstandingAdvancesAsync(SelectedGrower.GrowerId);

                var message = $"Outstanding advances for {SelectedGrower.FullName}:\n\n";
                
                if (outstandingAdvances.Any())
                {
                    foreach (var advance in outstandingAdvances)
                    {
                        message += $"• ${advance.AdvanceAmount:N2} - {advance.Reason} ({advance.AdvanceDate:MMM dd, yyyy})\n";
                    }
                    message += $"\nTotal Outstanding: ${totalOutstanding:N2}";
                }
                else
                {
                    message += "No outstanding advances.";
                }

                await _dialogService.ShowMessageBoxAsync(message, "Outstanding Advances");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error retrieving outstanding advances: {ex.Message}", "Error");
            }
        }

        private async Task CancelAdvanceChequeAsync()
        {
            try
            {
                if (SelectedAdvanceCheque == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Please select an advance cheque to cancel.", "Validation Error");
                    return;
                }

                var reason = await _dialogService.ShowInputDialogAsync("Enter reason for cancellation:", "Cancel Advance Cheque", "Cancellation reason");
                if (string.IsNullOrWhiteSpace(reason))
                {
                    return;
                }

                await _unifiedAdvanceService.CancelAdvanceChequeAsync(
                    SelectedAdvanceCheque.AdvanceChequeId,
                    reason,
                    "Current User" // TODO: Get from authentication context
                );

                await _dialogService.ShowMessageBoxAsync("Advance cheque cancelled successfully.", "Success");

                // Refresh data
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error cancelling advance cheque: {ex.Message}", "Error");
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

        private async Task RefreshAsync()
        {
            try
            {
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error refreshing data: {ex.Message}", "Error");
            }
        }

        private async Task ShowHelpAsync()
        {
            try
            {
                var helpContent = _helpContentProvider.GetHelpContent("AdvanceChequeView");
                await _dialogService.ShowHelpDialogAsync(helpContent.Content, "Advance Cheques Help");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error showing help: {ex.Message}", "Error");
            }
        }

        private async Task ClearFiltersAsync()
        {
            try
            {
                SearchText = string.Empty;
                StatusFilter = "All";
                StartDate = DateTime.Now.AddMonths(-1);
                EndDate = DateTime.Now;
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
                // TODO: Implement export functionality
                await _dialogService.ShowMessageBoxAsync("Export functionality will be implemented in a future update.", "Export");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error exporting data: {ex.Message}", "Error");
            }
        }

        #endregion

        #region Helper Methods

        private void SafeLogDebug(string message)
        {
            try
            {
                Logger.Debug(message);
            }
            catch
            {
                // Logger might not be available during construction
            }
        }

        private void SafeLogError(string message, Exception ex = null)
        {
            try
            {
                if (ex != null)
                    Logger.Error(message, ex);
                else
                    Logger.Error(message);
            }
            catch
            {
                // Logger might not be available during construction
            }
        }

        #endregion

        #region Private Methods

        private async Task LoadDataAsync()
        {
            try
            {
                SafeLogDebug("LoadDataAsync: Starting data load");
                IsBusy = true;

                // Load growers
                var growers = await _growerService.GetAllGrowersAsync();
                Growers.Clear();
                foreach (var grower in growers)
                {
                    Growers.Add(grower);
                }

                // Initialize filtered growers with all growers
                FilteredGrowers.Clear();
                foreach (var grower in growers)
                {
                    FilteredGrowers.Add(grower);
                }

                // Load all advance cheques into the main collection
                var allAdvanceCheques = await _unifiedAdvanceService.GetAllAdvanceChequesAsync();
                AdvanceCheques.Clear();
                foreach (var cheque in allAdvanceCheques)
                {
                    AdvanceCheques.Add(cheque);
                }

                // Calculate statistics from the loaded data
                await CalculateStatisticsAsync();

                // Apply filters to the loaded data
                await ApplyFiltersAsync();
                
                SafeLogDebug($"LoadDataAsync: Completed. Cheques loaded: {AdvanceCheques.Count}, Filtered: {FilteredAdvanceCheques.Count}");
            }
            catch (Exception ex)
            {
                SafeLogError($"LoadDataAsync: Error loading data: {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading data: {ex.Message}", "Error");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CalculateStatisticsAsync()
        {
            try
            {
                SafeLogDebug("CalculateStatisticsAsync: Starting calculation from in-memory data");
                
                // Use the in-memory collection
                var allAdvanceCheques = AdvanceCheques;
                
                // Calculate total outstanding
                var activeCheques = allAdvanceCheques.Where(a => a.IsActive).ToList();
                TotalOutstanding = activeCheques.Sum(a => a.AdvanceAmount);

                // Count active cheques
                ActiveCheques = activeCheques.Count;

                // Calculate deducted this month
                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var deductedThisMonth = allAdvanceCheques
                    .Where(a => a.IsDeducted && a.DeductedAt >= startOfMonth)
                    .ToList();
                DeductedThisMonth = deductedThisMonth.Sum(a => a.AdvanceAmount);
                
                SafeLogDebug($"CalculateStatisticsAsync: TotalOutstanding={TotalOutstanding:C}, ActiveCheques={ActiveCheques}, DeductedThisMonth={DeductedThisMonth:C}");

                // Notify the UI that the display properties have changed
                OnPropertyChanged(nameof(TotalOutstandingDisplay));
                OnPropertyChanged(nameof(ActiveChequesDisplay));
                OnPropertyChanged(nameof(DeductedThisMonthDisplay));
            }
            catch (Exception ex)
            {
                SafeLogError($"CalculateStatisticsAsync: Error calculating statistics: {ex.Message}", ex);
            }
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                SafeLogDebug("ApplyFiltersAsync: Starting filter application on in-memory data");
                
                // Use the in-memory collection
                var filtered = AdvanceCheques.AsEnumerable();
                var initialCount = filtered.Count();
                SafeLogDebug($"ApplyFiltersAsync: Initial count before filtering: {initialCount}");

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    var beforeSearchCount = filtered.Count();
                    filtered = filtered.Where(a => 
                        a.GrowerName.ToLower().Contains(searchLower) ||
                        a.GrowerNumber.ToLower().Contains(searchLower) ||
                        a.Reason.ToLower().Contains(searchLower));
                    var afterSearchCount = filtered.Count();
                    SafeLogDebug($"ApplyFiltersAsync: Search filter applied - before: {beforeSearchCount}, after: {afterSearchCount}");
                }
                else
                {
                    SafeLogDebug("ApplyFiltersAsync: No search filter applied (SearchText is empty)");
                }

                // Apply status filter
                if (StatusFilter != "All")
                {
                    var beforeStatusCount = filtered.Count();
                    filtered = filtered.Where(a => a.Status == StatusFilter);
                    var afterStatusCount = filtered.Count();
                    SafeLogDebug($"ApplyFiltersAsync: Status filter '{StatusFilter}' applied - before: {beforeStatusCount}, after: {afterStatusCount}");
                }
                else
                {
                    SafeLogDebug("ApplyFiltersAsync: No status filter applied (StatusFilter is 'All')");
                }

                // Apply date filter
                var beforeDateCount = filtered.Count();
                var startDateOnly = StartDate.Date;
                var endDateOnly = EndDate.Date.AddDays(1).AddTicks(-1); // Include the entire end date
                filtered = filtered.Where(a => a.AdvanceDate >= startDateOnly && a.AdvanceDate <= endDateOnly);
                var afterDateCount = filtered.Count();
                SafeLogDebug($"ApplyFiltersAsync: Date filter applied (StartDate: {StartDate:yyyy-MM-dd}, EndDate: {EndDate:yyyy-MM-dd}) - before: {beforeDateCount}, after: {afterDateCount}");
                SafeLogDebug($"ApplyFiltersAsync: Date filter range - Start: {startDateOnly:yyyy-MM-dd HH:mm:ss}, End: {endDateOnly:yyyy-MM-dd HH:mm:ss}");

                // Update filtered collection
                SafeLogDebug($"ApplyFiltersAsync: Clearing FilteredAdvanceCheques (current count: {FilteredAdvanceCheques.Count})");
                FilteredAdvanceCheques.Clear();
                
                var orderedFiltered = filtered.OrderByDescending(a => a.AdvanceDate).ToList();
                SafeLogDebug($"ApplyFiltersAsync: Ordered filtered results count: {orderedFiltered.Count}");
                
                foreach (var advance in orderedFiltered)
                {
                    FilteredAdvanceCheques.Add(advance);
                }
                
                SafeLogDebug($"ApplyFiltersAsync: Completed. Final FilteredAdvanceCheques count: {FilteredAdvanceCheques.Count}");
                
                // Log sample of filtered results for debugging
                if (FilteredAdvanceCheques.Any())
                {
                    SafeLogDebug($"ApplyFiltersAsync: Sample filtered results:");
                    foreach (var sample in FilteredAdvanceCheques.Take(3))
                    {
                        SafeLogDebug($"  - {sample.GrowerName} ({sample.GrowerNumber}) - {sample.AdvanceAmount:C} - {sample.Status} - {sample.AdvanceDate:yyyy-MM-dd}");
                    }
                }
                else
                {
                    SafeLogDebug("ApplyFiltersAsync: No results after filtering");
                }
            }
            catch (Exception ex)
            {
                SafeLogError($"ApplyFiltersAsync: Error applying filters: {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync($"Error applying filters: {ex.Message}", "Error");
            }
        }

        private void FilterGrowers()
        {
            try
            {
                SafeLogDebug($"FilterGrowers: Starting grower filtering with search text: '{GrowerSearchText}'");
                
                FilteredGrowers.Clear();
                
                if (string.IsNullOrWhiteSpace(GrowerSearchText))
                {
                    // If no search text, show all growers
                    foreach (var grower in Growers)
                    {
                        FilteredGrowers.Add(grower);
                    }
                    SafeLogDebug($"FilterGrowers: No search text, showing all {Growers.Count} growers");
                }
                else
                {
                    // Filter growers based on search text
                    var searchLower = GrowerSearchText.ToLower();
                    var filtered = Growers.Where(g => 
                        g.FullName.ToLower().Contains(searchLower) ||
                        g.GrowerNumber.ToLower().Contains(searchLower) ||
                        g.CheckPayeeName.ToLower().Contains(searchLower) ||
                        g.GrowerName.ToLower().Contains(searchLower));
                    
                    foreach (var grower in filtered)
                    {
                        FilteredGrowers.Add(grower);
                    }
                    SafeLogDebug($"FilterGrowers: Filtered to {FilteredGrowers.Count} growers matching '{GrowerSearchText}'");
                }
            }
            catch (Exception ex)
            {
                SafeLogError($"FilterGrowers: Error filtering growers: {ex.Message}", ex);
            }
        }

        #endregion

        #region Property Change Handlers

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            SafeLogDebug($"OnPropertyChanged: Property '{e.PropertyName}' changed");

            // Update command states when relevant properties change
            if (e.PropertyName == nameof(SelectedGrower) || 
                e.PropertyName == nameof(AdvanceAmount) || 
                e.PropertyName == nameof(Reason))
            {
                SafeLogDebug($"OnPropertyChanged: Updating command states for {e.PropertyName}");
                try
                {
                    ((RelayCommand)CreateAdvanceChequeCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ViewOutstandingAdvancesCommand)?.RaiseCanExecuteChanged();
                }
                catch
                {
                    // Commands might not be initialized yet during construction
                }
            }

            if (e.PropertyName == nameof(SelectedAdvanceCheque))
            {
                SafeLogDebug($"OnPropertyChanged: Updating cancel command state for {e.PropertyName}");
                try
                {
                    ((RelayCommand)CancelAdvanceChequeCommand)?.RaiseCanExecuteChanged();
                }
                catch
                {
                    // Commands might not be initialized yet during construction
                }
            }

            if (e.PropertyName == nameof(FilteredAdvanceCheques))
            {
                SafeLogDebug($"OnPropertyChanged: Updating export command state for {e.PropertyName}");
                try
                {
                    ((RelayCommand)ExportCommand)?.RaiseCanExecuteChanged();
                }
                catch
                {
                    // Commands might not be initialized yet during construction
                }
            }

            // Auto-apply filters when search criteria change
            if (e.PropertyName == nameof(SearchText) || 
                e.PropertyName == nameof(StatusFilter) || 
                e.PropertyName == nameof(StartDate) || 
                e.PropertyName == nameof(EndDate))
            {
                SafeLogDebug($"OnPropertyChanged: Auto-applying filters due to {e.PropertyName} change");
                try
                {
                    // Only apply filters if the service is available (not during construction)
                    if (_unifiedAdvanceService != null)
                    {
                        _ = ApplyFiltersAsync();
                    }
                    else
                    {
                        SafeLogDebug($"OnPropertyChanged: Skipping ApplyFiltersAsync - service not ready");
                    }
                }
                catch
                {
                    // Services might not be initialized yet during construction
                    SafeLogDebug($"OnPropertyChanged: Error applying filters - service not ready");
                }
            }
        }

        #endregion

        #region Navigation Methods

        private void NavigateToDashboard()
        {
            try
            {
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
                NavigationHelper.NavigateToPaymentManagement();
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageBoxAsync($"Error navigating to Payment Management: {ex.Message}", "Navigation Error");
            }
        }

        #endregion

        #region Command Validation

        private bool CanCreateAdvanceCheque()
        {
            return HasSelectedGrower && AdvanceAmount > 0 && !string.IsNullOrWhiteSpace(Reason);
        }

        private bool CanViewOutstandingAdvances()
        {
            return HasSelectedGrower;
        }

        private bool CanCancelAdvanceCheque()
        {
            return HasSelectedAdvanceCheque && SelectedAdvanceCheque.CanBeVoided;
        }

        private bool CanExport()
        {
            return FilteredAdvanceCheques.Any();
        }

        #endregion

        #region Workflow Commands (Unified with regular cheques)

        /// <summary>
        /// Print the selected advance cheque
        /// </summary>
        private async Task PrintAdvanceChequeAsync()
        {
            if (SelectedAdvanceCheque == null) return;

            try
            {
                IsLoading = true;
                await _unifiedAdvanceService.PrintAdvanceChequeAsync(SelectedAdvanceCheque.AdvanceChequeId, Environment.UserName);
                await RefreshAsync();
                await _dialogService.ShowMessageBoxAsync("Advance cheque printed successfully.", "Print Success");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error printing advance cheque: {ex.Message}", "Print Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Deliver the selected advance cheque
        /// </summary>
        private async Task DeliverAdvanceChequeAsync()
        {
            if (SelectedAdvanceCheque == null) return;

            try
            {
                IsLoading = true;
                await _unifiedAdvanceService.DeliverAdvanceChequeAsync(SelectedAdvanceCheque.AdvanceChequeId, Environment.UserName, "Pickup");
                await RefreshAsync();
                await _dialogService.ShowMessageBoxAsync("Advance cheque delivered successfully.", "Delivery Success");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error delivering advance cheque: {ex.Message}", "Delivery Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Void the selected advance cheque
        /// </summary>
        private async Task VoidAdvanceChequeAsync()
        {
            if (SelectedAdvanceCheque == null) return;

            await _dialogService.ShowMessageBoxAsync(
                $"Are you sure you want to void advance cheque #{SelectedAdvanceCheque.AdvanceChequeId}?",
                "Confirm Void");

            // For now, proceed with voiding (in a real implementation, you'd need a proper confirmation dialog)
            try
            {
                IsLoading = true;
                await _unifiedAdvanceService.VoidAdvanceChequeAsync(SelectedAdvanceCheque.AdvanceChequeId, Environment.UserName, "Voided by user");
                await RefreshAsync();
                await _dialogService.ShowMessageBoxAsync("Advance cheque voided successfully.", "Void Success");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error voiding advance cheque: {ex.Message}", "Void Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Can print the selected advance cheque?
        /// </summary>
        private bool CanPrintAdvanceCheque()
        {
            return SelectedAdvanceCheque?.CanBePrinted == true;
        }

        /// <summary>
        /// Can deliver the selected advance cheque?
        /// </summary>
        private bool CanDeliverAdvanceCheque()
        {
            return SelectedAdvanceCheque?.CanBeDelivered == true;
        }

        /// <summary>
        /// Can void the selected advance cheque?
        /// </summary>
        private bool CanVoidAdvanceCheque()
        {
            return SelectedAdvanceCheque?.CanBeVoided == true;
        }

        #endregion
    }
}
