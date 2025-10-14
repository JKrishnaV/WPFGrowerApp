using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using WPFGrowerApp.ViewModels.Dialogs;
using WPFGrowerApp.Views;

namespace WPFGrowerApp.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IServiceProvider _serviceProvider;

        // Collections
        private ObservableCollection<Product> _products;
        private ObservableCollection<Product> _filteredProducts;
        
        // Filters
        private string _searchText = string.Empty;
        
        // UI State
        private bool _isLoading;
        private string _statusMessage = "Ready";
        private string _lastUpdated = string.Empty;
        private Product? _selectedProduct;
        private bool _isDialogOpen = false;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Product> FilteredProducts
        {
            get => _filteredProducts;
            set => SetProperty(ref _filteredProducts, value);
        }

        public Product? SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterProducts();
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
        public ICommand AddProductCommand { get; }
        public ICommand EditProductCommand { get; }
        public ICommand ViewProductCommand { get; }
        public ICommand DeleteProductCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ShowHelpCommand { get; }

        public ProductViewModel(
            IProductService productService,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IServiceProvider serviceProvider)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize collections
            _products = new ObservableCollection<Product>();
            _filteredProducts = new ObservableCollection<Product>();

            // Initialize commands
            AddProductCommand = new RelayCommand(async o => await AddProductAsync());
            EditProductCommand = new RelayCommand(async o => await EditProductAsync(o as Product));
            ViewProductCommand = new RelayCommand(async o => await ViewProductAsync(o as Product));
            DeleteProductCommand = new RelayCommand(async o => await DeleteProductAsync(o as Product));
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());
            SearchCommand = new RelayCommand(o => FilterProducts());
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
                Logger.Info("ProductViewModel: Starting initialization");
                IsLoading = true;
                StatusMessage = "Loading products...";

                // Load products
                await LoadProductsAsync();
                Logger.Info($"ProductViewModel: Loaded {Products.Count} products, {FilteredProducts.Count} filtered");

                StatusMessage = "Ready";
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing ProductViewModel", ex);
                StatusMessage = "Error loading data";
                await _dialogService.ShowMessageBoxAsync($"Error loading products: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
                Logger.Info("ProductViewModel: Initialization completed");
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                Logger.Info("ProductViewModel: Starting to load products from service");
                var products = await _productService.GetAllProductsAsync();
                Logger.Info($"ProductViewModel: Retrieved {products.Count()} products from service");
                
                Products.Clear();
                
                foreach (var product in products.OrderBy(p => p.Description))
                {
                    Products.Add(product);
                }

                Logger.Info($"ProductViewModel: Added {Products.Count} products to collection");

                // Update filtered products
                FilterProducts();
                Logger.Info($"ProductViewModel: Filtered products count: {FilteredProducts.Count}");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading products", ex);
                throw;
            }
        }

        private async Task RefreshAsync()
        {
            await InitializeAsync();
        }

        private void FilterProducts()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredProducts = new ObservableCollection<Product>(Products);
            }
            else
            {
                var searchLower = SearchText.ToLower();
                var filtered = Products.Where(p =>
                    (p.Description?.ToLower().Contains(searchLower) ?? false) ||
                    (p.ProductCode?.ToLower().Contains(searchLower) ?? false) ||
                    (p.ShortDescription?.ToLower().Contains(searchLower) ?? false) ||
                    (p.Variety?.ToLower().Contains(searchLower) ?? false)
                ).ToList();

                FilteredProducts = new ObservableCollection<Product>(filtered);
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            StatusMessage = "Ready";
        }

        private async Task AddProductAsync()
        {
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var newProduct = new Product
                {
                    ProductId = 0,
                    ProductCode = string.Empty,
                    Description = string.Empty,
                    ShortDescription = string.Empty,
                    Variety = string.Empty,
                    Deduct = 0,
                    ChargeGst = false
                };

                var dialogViewModel = new ProductEditDialogViewModel(newProduct, false, _dialogService);
                var result = await DialogHost.Show(new ProductEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.ProductData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Adding product...";
                    
                    try
                    {
                        bool success = await _productService.AddProductAsync(dialogViewModel.ProductData);
                        
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Product added successfully.", "Success");
                            await LoadProductsAsync();
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Failed to add the product.", "Error");
                            StatusMessage = "Failed to add product";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error adding product.", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error adding product: {ex.Message}", "Error");
                        StatusMessage = "Error adding product";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task ViewProductAsync(Product? product)
        {
            if (product == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new ProductEditDialogViewModel(product, true, _dialogService);
                await DialogHost.Show(new ProductEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task EditProductAsync(Product? product)
        {
            if (product == null) return;
            if (_isDialogOpen) return; // Prevent multiple dialogs

            try
            {
                _isDialogOpen = true;
                var dialogViewModel = new ProductEditDialogViewModel(product, false, _dialogService);
                var result = await DialogHost.Show(new ProductEditDialogView { DataContext = dialogViewModel }, "RootDialogHost");

                if (result is bool boolResult && boolResult && dialogViewModel.ProductData != null)
                {
                    IsLoading = true;
                    StatusMessage = "Updating product...";
                    
                    try
                    {
                        bool success = await _productService.UpdateProductAsync(dialogViewModel.ProductData);
                        
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Product updated successfully.", "Success");
                            await LoadProductsAsync();
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Failed to update the product.", "Error");
                            StatusMessage = "Failed to update product";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error updating product {dialogViewModel.ProductData.ProductId}.", ex);
                        await _dialogService.ShowMessageBoxAsync($"Error updating product: {ex.Message}", "Error");
                        StatusMessage = "Error updating product";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            }
            finally
            {
                _isDialogOpen = false;
            }
        }

        private async Task DeleteProductAsync(Product? product)
        {
            if (product == null) return;

            var confirm = await _dialogService.ShowConfirmationDialogAsync(
                $"Are you sure you want to delete product '{product.Description}' ({product.ProductCode})?", 
                "Confirm Delete");
            
            if (confirm != true) return;

            IsLoading = true;
            StatusMessage = "Deleting product...";
            
            try
            {
                bool success = await _productService.DeleteProductAsync(product.ProductId, App.CurrentUser?.Username ?? "SYSTEM");

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync("Product deleted successfully.", "Success");
                    await LoadProductsAsync();
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to delete the product.", "Error");
                    StatusMessage = "Failed to delete product";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error deleting product {product.ProductId}.", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting product: {ex.Message}", "Error");
                StatusMessage = "Error deleting product";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void NavigateToDashboardExecute(object? parameter)
        {
            try
            {
                Logger.Info("NavigateToDashboardExecute called - navigating to Dashboard");
                
                // Get the MainViewModel instance that's bound to the UI
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    Logger.Info($"MainViewModel instance: {mainViewModel.GetHashCode()}");
                    
                    // Navigate to Dashboard using the MainViewModel's navigation system
                    await mainViewModel.NavigateToAsync<DashboardViewModel>("Dashboard");
                    
                    Logger.Info($"NavigateToDashboardExecute completed successfully. CurrentViewModel: {mainViewModel.CurrentViewModel?.GetType().Name}");
                }
                else
                {
                    Logger.Error("Could not access MainViewModel from MainWindow DataContext");
                    await _dialogService.ShowMessageBoxAsync("Navigation error: Could not access main application.", "Navigation Error");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Dashboard", ex);
                await _dialogService.ShowMessageBoxAsync($"Error navigating to Dashboard: {ex.Message}", "Navigation Error");
            }
        }

        private async void NavigateToSettingsExecute(object? parameter)
        {
            try
            {
                Logger.Info("NavigateToSettingsExecute called - navigating to Settings");
                
                // Get the MainViewModel instance that's bound to the UI
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    Logger.Info($"MainViewModel instance: {mainViewModel.GetHashCode()}");
                    
                    // Navigate to Settings using the MainViewModel's navigation system
                    await mainViewModel.NavigateToAsync<SettingsHostViewModel>("Settings");
                    
                    Logger.Info($"NavigateToSettingsExecute completed successfully. CurrentViewModel: {mainViewModel.CurrentViewModel?.GetType().Name}");
                }
                else
                {
                    Logger.Error("Could not access MainViewModel from MainWindow DataContext");
                    await _dialogService.ShowMessageBoxAsync("Navigation error: Could not access main application.", "Navigation Error");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Settings", ex);
                await _dialogService.ShowMessageBoxAsync($"Error navigating to Settings: {ex.Message}", "Navigation Error");
            }
        }

        private void ShowHelpExecute(object? parameter)
        {
            var helpMessage = @"Product Management Help

Keyboard Shortcuts:
- F5: Refresh product list
- F1: Show this help

Actions:
- Add New: Create a new product
- View: View product details (read-only)
- Edit: Modify product information
- Delete: Remove product from system

Search:
- Type in search box to filter products
- Search looks in Description, Code, Short Description, and Variety fields
- Press Enter or click Search button
- Click Clear to reset search";

            _dialogService?.ShowMessageBoxAsync(helpMessage, "Help");
        }
    }
}
