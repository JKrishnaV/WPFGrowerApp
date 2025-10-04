using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Views;
using System.Linq;

namespace WPFGrowerApp.ViewModels
{
    public partial class PriceViewModel : ViewModelBase
    {
        private readonly IPriceService _priceService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IDialogService _dialogService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<PriceDisplayItem> _prices;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ViewPriceCommand))]
        [NotifyCanExecuteChangedFor(nameof(EditPriceCommand))]
        [NotifyCanExecuteChangedFor(nameof(DeletePriceCommand))]
        private PriceDisplayItem _selectedPrice;

        public PriceViewModel(
            IPriceService priceService, 
            IProductService productService, 
            IProcessService processService,
            IDialogService dialogService,
            IServiceProvider serviceProvider)
        {
            _priceService = priceService;
            _productService = productService;
            _processService = processService;
            _dialogService = dialogService;
            _serviceProvider = serviceProvider;
            LoadPrices();
        }

        private async void LoadPrices()
        {
            var prices = await _priceService.GetAllAsync();
            var products = await _productService.GetAllProductsAsync();
            var processes = await _processService.GetAllProcessesAsync();

            var displayItems = prices.Select(p => new PriceDisplayItem
            {
                Price = p,
                ProductName = products.FirstOrDefault(prod => prod.ProductId == p.Product)?.Description,
                ProcessName = processes.FirstOrDefault(proc => proc.ProcessId == p.Process)?.Description
            }).ToList();

            Prices = new ObservableCollection<PriceDisplayItem>(displayItems);
        }

        [RelayCommand]
        private void AddPrice()
        {
            try
            {
                // Create the PriceEntryViewModel with dependencies
                var viewModel = new PriceEntryViewModel(
                    _priceService,
                    _productService,
                    _processService,
                    _dialogService);

                var window = new PriceEntryWindow
                {
                    DataContext = viewModel,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    // Reload the price list
                    LoadPrices();
                    Infrastructure.Logging.Logger.Info("New price added successfully");
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error opening add price dialog", ex);
                _ = _dialogService.ShowMessageBoxAsync($"Error opening price entry: {ex.Message}", "Error");
            }
        }

        private bool CanEditPrice() => 
            SelectedPrice != null && !SelectedPrice.IsAnyLocked;

        [RelayCommand(CanExecute = nameof(CanViewPrice))]
        private async Task ViewPrice()
        {
            if (SelectedPrice == null)
            {
                _ = _dialogService.ShowMessageBoxAsync("Please select a price record to view.", "No Selection");
                return;
            }

            try
            {
                // Load the full price data with all 72 price values
                var fullPrice = await _priceService.GetByIdAsync(SelectedPrice.Price.PriceID);
                
                if (fullPrice == null)
                {
                    _ = _dialogService.ShowMessageBoxAsync("Unable to load price data.", "Error");
                    return;
                }
                
                // Create the PriceEntryViewModel in read-only mode
                var viewModel = new PriceEntryViewModel(
                    _priceService,
                    _productService,
                    _processService,
                    _dialogService,
                    fullPrice,
                    isReadOnly: true);

                var window = new PriceEntryWindow
                {
                    DataContext = viewModel,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error opening view price dialog", ex);
                _ = _dialogService.ShowMessageBoxAsync($"Error opening price view: {ex.Message}", "Error");
            }
        }

        private bool CanViewPrice() => SelectedPrice != null;

        [RelayCommand(CanExecute = nameof(CanEditPrice))]
        private async Task EditPrice()
        {
            if (SelectedPrice == null)
            {
                _ = _dialogService.ShowMessageBoxAsync("Please select a price record to edit.", "No Selection");
                return;
            }

            // Check if price is locked
            if (SelectedPrice.IsAnyLocked)
            {
                _ = _dialogService.ShowMessageBoxAsync(
                    $"This price record cannot be edited because it has been used for payments.\n\n{SelectedPrice.LockStatusTooltip}", 
                    "Price Locked");
                return;
            }

            try
            {
                // Load the full price data with all 72 price values
                var fullPrice = await _priceService.GetByIdAsync(SelectedPrice.Price.PriceID);
                
                if (fullPrice == null)
                {
                    _ = _dialogService.ShowMessageBoxAsync("Unable to load price data.", "Error");
                    return;
                }
                
                // Create the PriceEntryViewModel with the selected price
                var viewModel = new PriceEntryViewModel(
                    _priceService,
                    _productService,
                    _processService,
                    _dialogService,
                    fullPrice);

                var window = new PriceEntryWindow
                {
                    DataContext = viewModel,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    // Reload the price list
                    LoadPrices();
                    Infrastructure.Logging.Logger.Info($"Price ID {SelectedPrice.Price.PriceID} updated successfully");
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error opening edit price dialog", ex);
                _ = _dialogService.ShowMessageBoxAsync($"Error opening price entry: {ex.Message}", "Error");
            }
        }

        private bool CanDeletePrice() => 
            SelectedPrice != null && !SelectedPrice.IsAnyLocked;

        [RelayCommand(CanExecute = nameof(CanDeletePrice))]
        private async Task DeletePrice()
        {
            if (SelectedPrice == null)
            {
                await _dialogService.ShowMessageBoxAsync("Please select a price record to delete.", "No Selection");
                return;
            }

            // Check if price is locked
            if (SelectedPrice.IsAnyLocked)
            {
                await _dialogService.ShowMessageBoxAsync(
                    $"This price record cannot be deleted because it has been used for payments.\n\n{SelectedPrice.LockStatusTooltip}", 
                    "Price Locked");
                return;
            }

            try
            {
                var confirm = await _dialogService.ShowConfirmationDialogAsync(
                    "Confirm Delete",
                    $"Are you sure you want to delete this price record?\n\n" +
                    $"Product: {SelectedPrice.ProductName}\n" +
                    $"Process: {SelectedPrice.ProcessName}\n" +
                    $"Effective Date: {SelectedPrice.Price.From:d}");

                if (confirm)
                {
                    await _priceService.DeleteAsync(SelectedPrice.Price.PriceID);
                    LoadPrices();
                    
                    // Show success message
                    await _dialogService.ShowMessageBoxAsync(
                        "Price record deleted successfully!", 
                        "Success");
                    
                    Infrastructure.Logging.Logger.Info($"Price ID {SelectedPrice.Price.PriceID} deleted successfully");
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error deleting price", ex);
                await _dialogService.ShowMessageBoxAsync($"Error deleting price: {ex.Message}", "Error");
            }
        }
    }
}
