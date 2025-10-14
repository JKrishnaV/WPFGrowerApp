using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.ViewModels.Dialogs;

namespace WPFGrowerApp.ViewModels
{
    public class PriceViewModel : ViewModelBase
    {
        private readonly IPriceService _priceService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IServiceProvider _serviceProvider;

        // Collections
        private ObservableCollection<PriceDisplayItem> _prices;
        private ObservableCollection<PriceDisplayItem> _filteredPrices;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Process> _processes;
        
        // Filters
        private string _searchText = string.Empty;
        private Product? _filterProduct;
        private Process? _filterProcess;
        private string _filterLockStatus = "All";
        
        // UI State
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private string _lastUpdated;
        private PriceDisplayItem? _selectedPrice;

        public ObservableCollection<PriceDisplayItem> Prices
        {
            get => _prices;
            set => SetProperty(ref _prices, value);
        }

        public ObservableCollection<PriceDisplayItem> FilteredPrices
        {
            get => _filteredPrices;
            set => SetProperty(ref _filteredPrices, value);
        }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Process> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        public ObservableCollection<string> LockStatusOptions { get; } = new ObservableCollection<string>
        {
            "All",
            "Unlocked Only",
            "Any Locked"
        };

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterPrices();
                }
            }
        }

        public Product? FilterProduct
        {
            get => _filterProduct;
            set
            {
                if (SetProperty(ref _filterProduct, value))
                {
                    FilterPrices();
                }
            }
        }

        public Process? FilterProcess
        {
            get => _filterProcess;
            set
            {
                if (SetProperty(ref _filterProcess, value))
                {
                    FilterPrices();
                }
            }
        }

        public string FilterLockStatus
        {
            get => _filterLockStatus;
            set
            {
                if (SetProperty(ref _filterLockStatus, value))
                {
                    FilterPrices();
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

        public PriceDisplayItem? SelectedPrice
        {
            get => _selectedPrice;
            set => SetProperty(ref _selectedPrice, value);
        }

        // Computed properties
        public int LockedPricesCount => FilteredPrices?.Count(p => p.IsAnyLocked) ?? 0;

        // Commands
        public ICommand AddPriceCommand { get; }
        public ICommand EditPriceCommand { get; }
        public ICommand ViewPriceCommand { get; }
        public ICommand DeletePriceCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public PriceViewModel(
            IPriceService priceService,
            IProductService productService,
            IProcessService processService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IServiceProvider serviceProvider)
        {
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize collections
            _prices = new ObservableCollection<PriceDisplayItem>();
            _filteredPrices = new ObservableCollection<PriceDisplayItem>();
            _products = new ObservableCollection<Product>();
            _processes = new ObservableCollection<Process>();

            // Initialize commands
            AddPriceCommand = new RelayCommand(async o => await AddPriceAsync());
            EditPriceCommand = new RelayCommand(async o => await EditPriceAsync(o as PriceDisplayItem));
            ViewPriceCommand = new RelayCommand(async o => await ViewPriceAsync(o as PriceDisplayItem));
            DeletePriceCommand = new RelayCommand(async o => await DeletePriceAsync(o as PriceDisplayItem));
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());
            SearchCommand = new RelayCommand(o => FilterPrices());
            ClearFiltersCommand = new RelayCommand(o => ClearFilters());
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettingsExecute);
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);

            // Load data
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading prices...";

                // Load products and processes for filters
                await LoadProductsAsync();
                await LoadProcessesAsync();

                // Load prices
                await LoadPricesAsync();

                StatusMessage = "Ready";
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing PriceViewModel", ex);
                StatusMessage = "Error loading data";
                await _dialogService.ShowMessageBoxAsync($"Error loading prices: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                Products.Clear();
                
                // Add "All" option
                Products.Add(new Product { ProductId = 0, Description = "All Products" });
                
                foreach (var product in products.OrderBy(p => p.Description))
                {
                    Products.Add(product);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading products", ex);
            }
        }

        private async Task LoadProcessesAsync()
        {
            try
            {
                var processes = await _processService.GetAllProcessesAsync();
                Processes.Clear();
                
                // Add "All" option
                Processes.Add(new Process { ProcessId = 0, Description = "All Processes" });
                
                foreach (var process in processes.OrderBy(p => p.Description))
                {
                    Processes.Add(process);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading processes", ex);
            }
        }

        private async Task LoadPricesAsync()
        {
            try
            {
                var prices = await _priceService.GetAllAsync();
                var products = (await _productService.GetAllProductsAsync()).ToList();
                var processes = (await _processService.GetAllProcessesAsync()).ToList();

                Prices.Clear();
                foreach (var p in prices)
                {
                    var productName = products.FirstOrDefault(prod => prod.ProductId == p.ProductId)?.Description ?? string.Empty;
                    var processName = processes.FirstOrDefault(proc => proc.ProcessId == p.ProcessId)?.Description ?? string.Empty;
                    Prices.Add(new PriceDisplayItem
                    {
                        Price = p,
                        ProductName = productName,
                        ProcessName = processName
                    });
                }

                FilterPrices();
                OnPropertyChanged(nameof(LockedPricesCount));
                
                Logger.Info($"Loaded {Prices.Count} prices");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading prices", ex);
                throw;
            }
        }

        private void FilterPrices()
        {
            if (Prices == null)
            {
                FilteredPrices = new ObservableCollection<PriceDisplayItem>();
                return;
            }

            var filtered = Prices.AsEnumerable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(p =>
                    (p.ProductName?.ToLower().Contains(searchLower) == true) ||
                    (p.ProcessName?.ToLower().Contains(searchLower) == true));
            }

            // Apply product filter
            if (FilterProduct != null && FilterProduct.ProductId > 0)
            {
                filtered = filtered.Where(p => p.Price.ProductId == FilterProduct.ProductId);
            }

            // Apply process filter
            if (FilterProcess != null && FilterProcess.ProcessId > 0)
            {
                filtered = filtered.Where(p => p.Price.ProcessId == FilterProcess.ProcessId);
            }

            // Apply lock status filter
            if (FilterLockStatus == "Unlocked Only")
            {
                filtered = filtered.Where(p => !p.IsAnyLocked);
            }
            else if (FilterLockStatus == "Any Locked")
            {
                filtered = filtered.Where(p => p.IsAnyLocked);
            }

            FilteredPrices = new ObservableCollection<PriceDisplayItem>(filtered);
            OnPropertyChanged(nameof(LockedPricesCount));
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            FilterProduct = Products?.FirstOrDefault(p => p.ProductId == 0);
            FilterProcess = Processes?.FirstOrDefault(p => p.ProcessId == 0);
            FilterLockStatus = "All";
            StatusMessage = "Filters cleared";
        }

        private async Task RefreshAsync()
        {
            await LoadPricesAsync();
            StatusMessage = "Data refreshed";
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
        }

        private async Task AddPriceAsync()
        {
            try
            {
                // Create dialog view model for new price
                var dialogViewModel = new PriceEditDialogViewModel(
                    null,
                    Products.Where(p => p.ProductId > 0).ToList(),
                    Processes.Where(p => p.ProcessId > 0).ToList(),
                    _dialogService,
                    false);

                // Show the dialog
                await _dialogService.ShowDialogAsync(dialogViewModel);

                // If price saved, reload the list
                if (dialogViewModel.WasSaved)
                {
                    IsLoading = true;
                    
                    // Save the new price
                    var newPriceId = await _priceService.CreateAsync(dialogViewModel.PriceData);

                    if (newPriceId > 0)
                    {
                        await LoadPricesAsync();
                        StatusMessage = $"Price created successfully (ID: {newPriceId})";
                        await _dialogService.ShowMessageBoxAsync("Price created successfully.", "Success");
                        Logger.Info($"Price Management: Created new price with ID {newPriceId}");
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync("Failed to create price. Please try again.", "Error");
                        StatusMessage = "Failed to create price";
                    }

                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error creating new price", ex);
                await _dialogService.ShowMessageBoxAsync("An unexpected error occurred while creating the price.", "Error");
                IsLoading = false;
            }
        }

        private async Task ViewPriceAsync(PriceDisplayItem? priceItem)
        {
            if (priceItem == null)
                return;

            try
            {
                // Load the full price data
                var fullPrice = await _priceService.GetByIdAsync(priceItem.Price.PriceID);

                if (fullPrice == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Unable to load price data.", "Error");
                    return;
                }

                // Create dialog view model in read-only mode
                var dialogViewModel = new PriceEditDialogViewModel(
                    fullPrice,
                    Products.Where(p => p.ProductId > 0).ToList(),
                    Processes.Where(p => p.ProcessId > 0).ToList(),
                    _dialogService,
                    true); // Read-only

                // Show the dialog
                await _dialogService.ShowDialogAsync(dialogViewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Error viewing price", ex);
                await _dialogService.ShowMessageBoxAsync($"Error viewing price: {ex.Message}", "Error");
            }
        }

        private async Task EditPriceAsync(PriceDisplayItem? priceItem)
        {
            if (priceItem == null)
                return;

            // Check if price is locked
            if (priceItem.IsAnyLocked)
            {
                await _dialogService.ShowMessageBoxAsync(
                    $"This price record cannot be edited because it has been used for payments.\n\n{priceItem.LockStatusTooltip}",
                    "Price Locked");
                return;
            }

            try
            {
                // Load the full price data
                var fullPrice = await _priceService.GetByIdAsync(priceItem.Price.PriceID);

                if (fullPrice == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Unable to load price data.", "Error");
                    return;
                }

                // Create dialog view model for editing
                var dialogViewModel = new PriceEditDialogViewModel(
                    fullPrice,
                    Products.Where(p => p.ProductId > 0).ToList(),
                    Processes.Where(p => p.ProcessId > 0).ToList(),
                    _dialogService,
                    false);

                // Show the dialog
                await _dialogService.ShowDialogAsync(dialogViewModel);

                // If price saved, update it
                if (dialogViewModel.WasSaved)
                {
                    IsLoading = true;

                    var success = await _priceService.UpdateAsync(dialogViewModel.PriceData);

                    if (success)
                    {
                        await LoadPricesAsync();
                        StatusMessage = $"Price ID {priceItem.Price.PriceID} updated successfully";
                        await _dialogService.ShowMessageBoxAsync($"Price updated successfully.", "Success");
                        Logger.Info($"Price Management: Updated price ID {priceItem.Price.PriceID}");
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync("Failed to update price. Please try again.", "Error");
                        StatusMessage = "Failed to update price";
                    }

                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error editing price", ex);
                await _dialogService.ShowMessageBoxAsync("An unexpected error occurred while updating the price.", "Error");
                IsLoading = false;
            }
        }

        private async Task DeletePriceAsync(PriceDisplayItem? priceItem)
        {
            if (priceItem == null)
                return;

            // Check if price is locked
            if (priceItem.IsAnyLocked)
            {
                await _dialogService.ShowMessageBoxAsync(
                    $"This price record cannot be deleted because it has been used for payments.\n\n{priceItem.LockStatusTooltip}",
                    "Price Locked");
                return;
            }

            try
            {
                var confirm = await _dialogService.ShowConfirmationDialogAsync(
                    "Confirm Delete",
                    $"Are you sure you want to delete this price record?\n\n" +
                    $"Product: {priceItem.ProductName}\n" +
                    $"Process: {priceItem.ProcessName}\n" +
                    $"Effective Date: {priceItem.Price.From:d}");

                if (confirm)
                {
                    await _priceService.DeleteAsync(priceItem.Price.PriceID);
                    await LoadPricesAsync();

                    // Show success message
                    StatusMessage = $"Price ID {priceItem.Price.PriceID} deleted successfully";
                    await _dialogService.ShowMessageBoxAsync(
                        "Price record deleted successfully!",
                        "Success");

                    Logger.Info($"Price ID {priceItem.Price.PriceID} deleted successfully");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error deleting price", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting price: {ex.Message}", "Error");
            }
        }

        private void NavigateToDashboardExecute(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to dashboard...";

                // Get the MainViewModel from the MainWindow
                if (Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // Execute the dashboard navigation command
                    if (mainViewModel.NavigateToDashboardCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToDashboardCommand.Execute(null);
                        StatusMessage = "Navigated to Dashboard";
                    }
                    else
                    {
                        StatusMessage = "Unable to navigate to Dashboard";
                    }
                }
                else
                {
                    StatusMessage = "Navigation service not available";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to dashboard", ex);
                StatusMessage = "Navigation failed";
            }
        }

        private void NavigateToSettingsExecute(object parameter)
        {
            try
            {
                StatusMessage = "Navigating to settings...";

                // Get the MainViewModel from the MainWindow
                if (Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // Execute the settings navigation command
                    if (mainViewModel.NavigateToSettingsCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToSettingsCommand.Execute(null);
                        StatusMessage = "Navigated to Settings";
                    }
                    else
                    {
                        StatusMessage = "Unable to navigate to Settings";
                    }
                }
                else
                {
                    StatusMessage = "Navigation service not available";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to settings", ex);
                StatusMessage = "Navigation failed";
            }
        }

        private async void ShowHelpExecute(object parameter)
        {
            try
            {
                // Get help content for Price Management view
                var helpContent = _helpContentProvider.GetHelpContent("PriceManagement");

                // Create help dialog ViewModel
                var helpViewModel = new HelpDialogViewModel(
                    helpContent.Title,
                    helpContent.Content,
                    helpContent.QuickTips,
                    helpContent.KeyboardShortcuts
                );

                // Show the help dialog
                await _dialogService.ShowDialogAsync(helpViewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Error showing help", ex);
                await _dialogService.ShowMessageBoxAsync("Unable to display help content at this time.", "Error");
            }
        }
    }
}
