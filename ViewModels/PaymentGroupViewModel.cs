using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.ViewModels.Dialogs;
using WPFGrowerApp.Views.Dialogs;

namespace WPFGrowerApp.ViewModels
{
    public class PaymentGroupViewModel : ViewModelBase
    {
        private readonly IPayGroupService _payGroupService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IServiceProvider _serviceProvider;
        
        private ObservableCollection<PayGroup> _payGroups = new();
        private ObservableCollection<PayGroup> _filteredPayGroups = new();
        private PayGroup? _selectedPayGroup;
        private string _searchText = string.Empty;
        private string _statusFilter = "All";
        private string _statusMessage = "Ready";
        private string _lastUpdated = string.Empty;
        private bool _isLoading;
        private bool _isDialogOpen;

        public ObservableCollection<PayGroup> PayGroups
        {
            get => _payGroups;
            set => SetProperty(ref _payGroups, value);
        }

        public ObservableCollection<PayGroup> FilteredPayGroups
        {
            get => _filteredPayGroups;
            set => SetProperty(ref _filteredPayGroups, value);
        }

        public PayGroup? SelectedPayGroup
        {
            get => _selectedPayGroup;
            set => SetProperty(ref _selectedPayGroup, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterPayGroups();
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
                    FilterPayGroups();
                }
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

        public string LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        // Commands
        public ICommand AddPayGroupCommand { get; }
        public ICommand EditPayGroupCommand { get; }
        public ICommand ViewPayGroupCommand { get; }
        public ICommand DeletePayGroupCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public PaymentGroupViewModel(
            IPayGroupService payGroupService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IServiceProvider serviceProvider)
        {
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize commands
            AddPayGroupCommand = new RelayCommand(async _ => await AddPayGroupAsync());
            EditPayGroupCommand = new RelayCommand(async param => await EditPayGroupAsync(param as PayGroup), param => param != null);
            ViewPayGroupCommand = new RelayCommand(async param => await ViewPayGroupAsync(param as PayGroup), param => param != null);
            DeletePayGroupCommand = new RelayCommand(async param => await DeletePayGroupAsync(param as PayGroup), param => param != null);
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            SearchCommand = new RelayCommand(_ => FilterPayGroups());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            NavigateToDashboardCommand = new RelayCommand(_ => NavigateToDashboard());
            NavigateToSettingsCommand = new RelayCommand(_ => NavigateToSettings());
            ShowHelpCommand = new RelayCommand(_ => ShowHelp());

            // Load data
            _ = InitializeAsync();
        }

        public async Task InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PaymentGroupViewModel: Starting initialization");
                IsLoading = true;
                StatusMessage = "Loading payment groups...";
                await LoadPayGroupsAsync();
                System.Diagnostics.Debug.WriteLine($"PaymentGroupViewModel: Loaded {PayGroups.Count} payment groups, {FilteredPayGroups.Count} filtered");
                StatusMessage = "Ready";
                LastUpdated = DateTime.Now.ToString("MMM dd, yyyy HH:mm:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing PaymentGroupViewModel: {ex.Message}");
                StatusMessage = "Error loading payment groups";
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine("PaymentGroupViewModel: Initialization completed");
            }
        }

        private async Task LoadPayGroupsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("PaymentGroupViewModel: Starting to load payment groups from service");
                var payGroups = await _payGroupService.GetAllPayGroupsAsync();
                System.Diagnostics.Debug.WriteLine($"PaymentGroupViewModel: Retrieved {payGroups.Count()} payment groups from service");
                
                PayGroups.Clear();
                
                foreach (var payGroup in payGroups.OrderBy(pg => pg.GroupName))
                {
                    PayGroups.Add(payGroup);
                }

                System.Diagnostics.Debug.WriteLine($"PaymentGroupViewModel: Added {PayGroups.Count} payment groups to collection");

                // Update filtered pay groups
                FilterPayGroups();
                System.Diagnostics.Debug.WriteLine($"PaymentGroupViewModel: Filtered payment groups count: {FilteredPayGroups.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading payment groups: {ex.Message}");
                throw;
            }
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }

        private void FilterPayGroups()
        {
            var baseFilter = PayGroups.AsEnumerable();

            // Apply status filter
            switch (StatusFilter)
            {
                case "Active":
                    baseFilter = baseFilter.Where(pg => pg.IsActive);
                    break;
                case "Inactive":
                    baseFilter = baseFilter.Where(pg => !pg.IsActive);
                    break;
                case "All":
                default:
                    // No status filtering - show all records
                    break;
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                baseFilter = baseFilter.Where(pg =>
                    (pg.GroupName?.ToLower().Contains(searchLower) ?? false) ||
                    (pg.GroupCode?.ToLower().Contains(searchLower) ?? false) ||
                    (pg.Description?.ToLower().Contains(searchLower) ?? false) ||
                    (pg.PaymentGroupId.ToString().Contains(searchLower))
                );
            }

            FilteredPayGroups = new ObservableCollection<PayGroup>(baseFilter);
            StatusMessage = $"Showing {FilteredPayGroups.Count} of {PayGroups.Count} payment groups";
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusFilter = "All";
            StatusMessage = "Ready";
        }

        private async Task AddPayGroupAsync()
        {
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var newPayGroup = new PayGroup
                {
                    PaymentGroupId = 0,
                    GroupCode = string.Empty,
                    GroupName = string.Empty,
                    Description = string.Empty,
                    DefaultPriceLevel = 1, // Default to Level 1
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = App.CurrentUser?.Username ?? "SYSTEM"
                };

                var dialogViewModel = new PayGroupEditDialogViewModel(newPayGroup, false, _dialogService);
                var result = await DialogHost.Show(new PayGroupEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.PayGroupData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Adding payment group...";
                    
                    try
                    {
                        bool success = await _payGroupService.AddPayGroupAsync(dialogViewModel.PayGroupData);
                        
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Payment group added successfully.", "Success");
                            await LoadPayGroupsAsync();
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Failed to add the payment group.", "Error");
                            StatusMessage = "Failed to add payment group";
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error adding payment group: {ex.Message}");
                        await _dialogService.ShowMessageBoxAsync($"Error adding payment group: {ex.Message}", "Error");
                        StatusMessage = "Error adding payment group";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding payment group: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync("Error", $"Failed to add payment group: {ex.Message}");
                StatusMessage = "Error adding payment group";
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task ViewPayGroupAsync(PayGroup? payGroup)
        {
            if (payGroup == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new PayGroupEditDialogViewModel(payGroup, true, _dialogService);
                await DialogHost.Show(new PayGroupEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error viewing payment group: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync("Error", $"Failed to view payment group: {ex.Message}");
                StatusMessage = "Error viewing payment group";
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task EditPayGroupAsync(PayGroup? payGroup)
        {
            if (payGroup == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new PayGroupEditDialogViewModel(payGroup, false, _dialogService);
                var result = await DialogHost.Show(new PayGroupEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.PayGroupData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Updating payment group...";
                    
                    try
                    {
                        bool success = await _payGroupService.UpdatePayGroupAsync(dialogViewModel.PayGroupData);
                        
                        if (success)
                        {
                            System.Diagnostics.Debug.WriteLine($"EditPayGroupAsync: Successfully updated payment group {dialogViewModel.PayGroupData.PaymentGroupId}");
                            await _dialogService.ShowMessageBoxAsync("Payment group updated successfully.", "Success");
                            await LoadPayGroupsAsync();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"EditPayGroupAsync: Failed to update payment group {dialogViewModel.PayGroupData.PaymentGroupId}. UpdatePayGroupAsync returned false - no rows were affected by the update operation.");
                            await _dialogService.ShowMessageBoxAsync("Failed to update the payment group.", "Error");
                            StatusMessage = "Failed to update payment group";
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating payment group {dialogViewModel.PayGroupData.PaymentGroupId}: {ex.Message}");
                        await _dialogService.ShowMessageBoxAsync($"Error updating payment group: {ex.Message}", "Error");
                        StatusMessage = "Error updating payment group";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error editing payment group: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync("Error", $"Failed to edit payment group: {ex.Message}");
                StatusMessage = "Error editing payment group";
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task DeletePayGroupAsync(PayGroup payGroup)
        {
            if (payGroup == null) return;

            var confirm = await _dialogService.ShowConfirmationDialogAsync(
                $"Are you sure you want to delete payment group '{payGroup.GroupName}' ({payGroup.GroupCode})?", 
                "Confirm Delete");
            
            if (confirm != true) return;

            IsLoading = true;
            StatusMessage = "Deleting payment group...";
            
            try
            {
                bool success = await _payGroupService.DeletePayGroupAsync(payGroup.PaymentGroupId.ToString());

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync("Payment group deleted successfully.", "Success");
                    await LoadPayGroupsAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to delete the payment group.", "Error");
                    StatusMessage = "Failed to delete payment group";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting payment group: {ex.Message}");
                await _dialogService.ShowMessageBoxAsync($"Error deleting payment group: {ex.Message}", "Error");
                StatusMessage = "Error deleting payment group";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateToDashboard()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Navigate to dashboard - implementation depends on MainWindow structure
                    System.Diagnostics.Debug.WriteLine("Navigate to Dashboard requested");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to dashboard: {ex.Message}");
                StatusMessage = "Error navigating to dashboard";
            }
        }

        private void NavigateToSettings()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    // Navigate to settings - implementation depends on MainWindow structure
                    System.Diagnostics.Debug.WriteLine("Navigate to Settings requested");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to settings: {ex.Message}");
                StatusMessage = "Error navigating to settings";
            }
        }

        private async void ShowHelp()
        {
            try
            {
                await _dialogService.ShowMessageBoxAsync(
                    "Payment Groups Help\n\n" +
                    "• Use the search box to find payment groups by name, code, or description\n" +
                    "• Use the status filter to show All, Active, or Inactive payment groups\n" +
                    "• Click 'Add New' to create a new payment group\n" +
                    "• Double-click a row to edit the payment group\n" +
                    "• Right-click for additional options like delete\n" +
                    "• Use F5 to refresh the data\n" +
                    "• Use F1 to show this help", 
                    "Payment Groups Help");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing help: {ex.Message}");
                StatusMessage = "Error showing help";
            }
        }
    }
}