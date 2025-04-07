using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services; // For IDialogService (assuming it exists)

namespace WPFGrowerApp.ViewModels
{
    public class ProductViewModel : ViewModelBase
    {
        private readonly IProductService _productService;
        private readonly IDialogService _dialogService; // Assuming a dialog service exists
        private ObservableCollection<Product> _products;
        private Product _selectedProduct;
        private bool _isEditing;
        private bool _isLoading;

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                // Set IsEditing based on whether a product is selected
                bool productSelected = value != null;
                if (SetProperty(ref _selectedProduct, value))
                {
                    IsEditing = productSelected; // Set IsEditing true if selected, false if null
                }
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            private set // Make setter private as it's controlled internally now
            {
                if (SetProperty(ref _isEditing, value))
                {
                    // Raise CanExecuteChanged for buttons affected by IsEditing state
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged(); 
                    ((RelayCommand)NewCommand).RaiseCanExecuteChanged(); 
                }
            }
        }
        
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoadProductsCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public ProductViewModel(IProductService productService, IDialogService dialogService)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); // Handle potential null

            Products = new ObservableCollection<Product>();

            // Use RelayCommand for all, adjusting signatures and CanExecute logic
            LoadProductsCommand = new RelayCommand(async (param) => await LoadProductsAsync(param), (param) => true); 
            NewCommand = new RelayCommand(AddNewProduct, CanAddNew); // Use new CanExecute
            SaveCommand = new RelayCommand(async (param) => await SaveProductAsync(param), CanSaveCancelDelete); // Use combined CanExecute
            DeleteCommand = new RelayCommand(async (param) => await DeleteProductAsync(param), CanSaveCancelDelete); // Use combined CanExecute
            CancelCommand = new RelayCommand(CancelEdit, CanSaveCancelDelete); // Use combined CanExecute

            // Load products on initialization
            _ = LoadProductsAsync(null); 
        }

        // Adjusted signature to accept object parameter
        private async Task LoadProductsAsync(object parameter)
        {
            IsLoading = true;
            try
            {
                var products = await _productService.GetAllProductsAsync();
                Products = new ObservableCollection<Product>(products.OrderBy(p => p.Description));
                SelectedProduct = null; // Clear selection after loading
                IsEditing = false;
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error loading products.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error loading products: {ex.Message}", "Error"); // Use async
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Adjusted signature to accept object parameter
        private void AddNewProduct(object parameter)
        {
            SelectedProduct = new Product(); // Create a new, empty product object
            IsEditing = true; // Set editing true when adding new
            // CanExecuteChanged is handled by the IsEditing setter now
        }

        // CanExecute for New button
        private bool CanAddNew(object parameter)
        {
            // Can add new only if NOT currently editing/viewing a selected product
            return SelectedProduct == null; 
        }

        // Combined CanExecute for Save, Cancel, Delete
        private bool CanSaveCancelDelete(object parameter)
        {
            // Can perform these actions only if a product is selected
            return SelectedProduct != null;
        }
        
        // Removed CanDelete - logic combined into CanSaveCancelDelete
        // Removed CanSaveOrCancel - logic combined into CanSaveCancelDelete

        // Adjusted signature to accept object parameter
        private async Task SaveProductAsync(object parameter)
        {
            // Check if a product is selected (which implies IsEditing is true now)
            if (SelectedProduct == null) return; 

            // Basic Validation (Add more robust validation as needed)
            if (string.IsNullOrWhiteSpace(SelectedProduct.ProductId) || SelectedProduct.ProductId.Length > 2)
            {
                 await _dialogService?.ShowMessageBoxAsync("Product ID cannot be empty and must be 1 or 2 characters.", "Validation Error"); // Use async
                 return;
            }
             if (string.IsNullOrWhiteSpace(SelectedProduct.Description))
            {
                 await _dialogService?.ShowMessageBoxAsync("Description cannot be empty.", "Validation Error"); // Use async
                 return;
            }

            IsLoading = true;
            bool success = false;
            try
            {
                // Determine if it's an Add or Update
                var existingProduct = await _productService.GetProductByIdAsync(SelectedProduct.ProductId);

                if (existingProduct == null) 
                {
                    // Add new product
                    success = await _productService.AddProductAsync(SelectedProduct);
                    if(success) await _dialogService?.ShowMessageBoxAsync("Product added successfully.", "Success"); // Use async
                }
                else
                {
                    // Update existing product
                    success = await _productService.UpdateProductAsync(SelectedProduct);
                     if(success) await _dialogService?.ShowMessageBoxAsync("Product updated successfully.", "Success"); // Use async
                }

                if (success)
                {
                    // IsEditing will be set to false automatically when LoadProductsAsync clears SelectedProduct
                    await LoadProductsAsync(null); // Pass null parameter
                }
                else
                {
                     await _dialogService?.ShowMessageBoxAsync("Failed to save the product.", "Error"); // Use async
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error saving product {SelectedProduct.ProductId}.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error saving product: {ex.Message}", "Error"); // Use async
            }
             finally
            {
                IsLoading = false;
            }
        }

        // Adjusted signature to accept object parameter
        private async Task DeleteProductAsync(object parameter)
        {
             if (!CanSaveCancelDelete(parameter)) return; // Use combined CanExecute

            // Use the new ShowConfirmationDialog method
            var confirm = await _dialogService?.ShowConfirmationDialogAsync($"Are you sure you want to delete product '{SelectedProduct.Description}' ({SelectedProduct.ProductId})?", "Confirm Delete"); // Use async
            
            // ShowConfirmationDialog returns true for Yes, false otherwise
            if (confirm != true) return; 

            IsLoading = true;
            try
            {
                // Assuming logical delete by setting QDEL fields in the service
                bool success = await _productService.DeleteProductAsync(SelectedProduct.ProductId, App.CurrentUser?.Username ?? "SYSTEM"); 

                if (success)
                {
                    await _dialogService?.ShowMessageBoxAsync("Product deleted successfully.", "Success"); // Use async
                    await LoadProductsAsync(null); // Pass null parameter
                }
                else
                {
                    await _dialogService?.ShowMessageBoxAsync("Failed to delete the product.", "Error"); // Use async
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error deleting product {SelectedProduct.ProductId}.", ex);
                await _dialogService?.ShowMessageBoxAsync($"Error deleting product: {ex.Message}", "Error"); // Use async
            }
             finally
            {
                IsLoading = false;
            }
        }

        // Adjusted signature to accept object parameter
        private void CancelEdit(object parameter)
        {
            if (!CanSaveCancelDelete(parameter)) return; // Use combined CanExecute

            // Simply clear selection, which will trigger IsEditing = false and reload
            SelectedProduct = null; 
            // No need to explicitly set IsEditing or call LoadProductsAsync here, 
            // as setting SelectedProduct = null handles it via the property setter.
        }
    }
}
