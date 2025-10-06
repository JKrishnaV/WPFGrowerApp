using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Views;

namespace WPFGrowerApp.ViewModels
{
    public class ReceiptViewModel : ViewModelBase
    {
        private readonly IReceiptService _receiptService;
        private readonly IGrowerService _growerService;
        private readonly IProductService _productService;
        private readonly IProcessService _processService;
        private readonly IDepotService _depotService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Receipt> _receipts;
        private Receipt _selectedReceipt;
        private bool _isLoading;
        private string _statusMessage;
        private string _searchText;
        
        // Filter properties
        private DateTime? _startDate;
        private DateTime? _endDate;
        private bool _showVoidedReceipts;

        // Pagination properties
        private int _currentPage;
        private int _pageSize;
        private int _totalPages;
        private int _totalRecords;
        private List<Receipt> _allReceipts; // Cache all filtered receipts

        // Statistics
        private int _totalReceiptCount;
        private decimal _totalGrossWeight;
        private decimal _totalNetWeight;

        public ReceiptViewModel(
            IReceiptService receiptService,
            IGrowerService growerService,
            IProductService productService,
            IProcessService processService,
            IDepotService depotService,
            IDialogService dialogService)
        {
            Infrastructure.Logging.Logger.Info("ReceiptViewModel - Constructor starting");
            
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _processService = processService ?? throw new ArgumentNullException(nameof(processService));
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            Receipts = new ObservableCollection<Receipt>();
            _allReceipts = new List<Receipt>();

            // Initialize pagination
            _currentPage = 1;
            _pageSize = 100; // Default to 100 records per page

            // Initialize date range to show all data from the past year
            // This provides a better default view when historical data exists
            StartDate = new DateTime(2023, 1, 1);
            EndDate = new DateTime(2023, 12, 31);

            Infrastructure.Logging.Logger.Info($"ReceiptViewModel - Date range initialized: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}");
            Infrastructure.Logging.Logger.Info($"ReceiptViewModel - Default page size: {_pageSize}");

            // Initialize commands
            LoadReceiptsCommand = new RelayCommand(async p => await LoadReceiptsAsync(), p => !IsLoading);
            AddReceiptCommand = new RelayCommand(async p => await AddReceiptAsync(), p => !IsLoading);
            EditReceiptCommand = new RelayCommand(async p => await EditReceiptAsync(), p => SelectedReceipt != null && !IsLoading);
            DeleteReceiptCommand = new RelayCommand(async p => await DeleteReceiptAsync(), p => SelectedReceipt != null && !IsLoading);
            VoidReceiptCommand = new RelayCommand(async p => await VoidReceiptAsync(), p => SelectedReceipt != null && !SelectedReceipt.IsVoidedModern && !IsLoading);
            ViewReceiptDetailsCommand = new RelayCommand(async p => await ViewReceiptDetailsAsync(), p => SelectedReceipt != null);
            RefreshCommand = new RelayCommand(async p => await LoadReceiptsAsync(), p => !IsLoading);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            
            // Pagination commands
            FirstPageCommand = new RelayCommand(p => GoToFirstPage(), p => CanGoToPreviousPage);
            PreviousPageCommand = new RelayCommand(p => GoToPreviousPage(), p => CanGoToPreviousPage);
            NextPageCommand = new RelayCommand(p => GoToNextPage(), p => CanGoToNextPage);
            LastPageCommand = new RelayCommand(p => GoToLastPage(), p => CanGoToNextPage);
            
            Infrastructure.Logging.Logger.Info("ReceiptViewModel - Constructor completed");
        }

        public async Task InitializeAsync()
        {
            Infrastructure.Logging.Logger.Info("ReceiptViewModel.InitializeAsync - Starting initialization");
            await LoadReceiptsAsync();
            Infrastructure.Logging.Logger.Info("ReceiptViewModel.InitializeAsync - Initialization completed");
        }

        #region Properties

        public ObservableCollection<Receipt> Receipts
        {
            get => _receipts;
            set => SetProperty(ref _receipts, value);
        }

        public Receipt SelectedReceipt
        {
            get => _selectedReceipt;
            set => SetProperty(ref _selectedReceipt, value);
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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadReceiptsAsync();
                }
            }
        }

        public DateTime? StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime? EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public bool ShowVoidedReceipts
        {
            get => _showVoidedReceipts;
            set
            {
                if (SetProperty(ref _showVoidedReceipts, value))
                {
                    _ = LoadReceiptsAsync();
                }
            }
        }

        public int TotalReceiptCount
        {
            get => _totalReceiptCount;
            set => SetProperty(ref _totalReceiptCount, value);
        }

        public decimal TotalGrossWeight
        {
            get => _totalGrossWeight;
            set => SetProperty(ref _totalGrossWeight, value);
        }

        public decimal TotalNetWeight
        {
            get => _totalNetWeight;
            set => SetProperty(ref _totalNetWeight, value);
        }

        // Pagination properties
        public int CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    Infrastructure.Logging.Logger.Info($"ReceiptViewModel - Page size changed to: {value}");
                    CurrentPage = 1; // Reset to first page when page size changes
                    UpdatePagedReceipts();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        public string PageInfo => $"Page {CurrentPage} of {TotalPages} ({TotalRecords} total records)";

        public bool CanGoToPreviousPage => CurrentPage > 1;
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        public List<int> PageSizeOptions { get; } = new List<int> { 50, 100, 200, 500, 1000, -1 }; // -1 means "All"

        #endregion

        #region Commands

        public ICommand LoadReceiptsCommand { get; }
        public ICommand AddReceiptCommand { get; }
        public ICommand EditReceiptCommand { get; }
        public ICommand DeleteReceiptCommand { get; }
        public ICommand VoidReceiptCommand { get; }
        public ICommand ViewReceiptDetailsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        // Pagination commands
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }

        #endregion

        #region Methods

        private async Task LoadReceiptsAsync()
        {
            if (IsLoading)
            {
                Infrastructure.Logging.Logger.Warn("LoadReceiptsAsync - Already loading, skipping");
                return;
            }

            Infrastructure.Logging.Logger.Info($"LoadReceiptsAsync - Starting to load receipts. Date range: {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}, ShowVoided: {ShowVoidedReceipts}");
            
            IsLoading = true;
            StatusMessage = "Loading receipts...";

            try
            {
                Infrastructure.Logging.Logger.Info("LoadReceiptsAsync - Calling ReceiptService.GetReceiptsAsync");
                // Load receipts by date range
                var allReceipts = await _receiptService.GetReceiptsAsync(StartDate, EndDate);
                
                Infrastructure.Logging.Logger.Info($"LoadReceiptsAsync - Retrieved {allReceipts.Count} receipts from service");

                // Filter by search text if provided (searches Receipt Number, Grower Name, and Grower ID)
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    Infrastructure.Logging.Logger.Info($"LoadReceiptsAsync - Applying search filter: '{SearchText}'");
                    var searchLower = SearchText.ToLower();
                    allReceipts = allReceipts.Where(r =>
                        r.ReceiptNumberModern?.ToLower().Contains(searchLower) == true ||
                        r.ReceiptNumber.ToString().Contains(searchLower) ||
                        r.GrowerName?.ToLower().Contains(searchLower) == true ||
                        r.GrowerId.ToString().Contains(searchLower) ||
                        r.GrowerNumber.ToString().Contains(searchLower)
                    ).ToList();
                    Infrastructure.Logging.Logger.Info($"LoadReceiptsAsync - After search filter: {allReceipts.Count} receipts");
                }

                // Filter voided receipts
                if (!ShowVoidedReceipts)
                {
                    var beforeCount = allReceipts.Count;
                    allReceipts = allReceipts.Where(r => !r.IsVoidedModern).ToList();
                    Infrastructure.Logging.Logger.Info($"LoadReceiptsAsync - Filtered out {beforeCount - allReceipts.Count} voided receipts. Remaining: {allReceipts.Count}");
                }

                // Order receipts
                allReceipts = allReceipts.OrderByDescending(r => r.ReceiptDate).ThenByDescending(r => r.ReceiptTime).ToList();

                // Cache all filtered receipts for pagination
                _allReceipts = allReceipts;
                TotalRecords = _allReceipts.Count;

                // Reset to first page
                CurrentPage = 1;

                // Apply pagination and update display
                UpdatePagedReceipts();

                Infrastructure.Logging.Logger.Info($"LoadReceiptsAsync - Completed successfully. Total: {TotalRecords}, Page Size: {PageSize}, Total Pages: {TotalPages}");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading receipts: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error loading receipts: {ex.Message}", "Load Error");
                Infrastructure.Logging.Logger.Error("LoadReceiptsAsync - Error loading receipts", ex);
            }
            finally
            {
                IsLoading = false;
                Infrastructure.Logging.Logger.Info("LoadReceiptsAsync - Finished (IsLoading = false)");
            }
        }

        private async Task AddReceiptAsync()
        {
            Infrastructure.Logging.Logger.Info("AddReceiptAsync - Starting add receipt operation");
            
            try
            {
                // Open receipt entry dialog for new receipt
                Infrastructure.Logging.Logger.Info("AddReceiptAsync - Creating ReceiptEntryViewModel");
                var entryViewModel = new ReceiptEntryViewModel(
                    _receiptService,
                    _growerService,
                    _productService,
                    _processService,
                    _depotService,
                    _dialogService);

                Infrastructure.Logging.Logger.Info("AddReceiptAsync - Initializing ReceiptEntryViewModel");
                await entryViewModel.InitializeAsync();

                Infrastructure.Logging.Logger.Info("AddReceiptAsync - Showing ReceiptEntryView dialog");
                var result = await _dialogService.ShowDialogAsync<ReceiptEntryView>(entryViewModel);

                if (result == true)
                {
                    Infrastructure.Logging.Logger.Info("AddReceiptAsync - Receipt saved successfully, reloading receipts");
                    await LoadReceiptsAsync();
                    StatusMessage = "Receipt added successfully";
                }
                else
                {
                    Infrastructure.Logging.Logger.Info("AddReceiptAsync - Dialog cancelled by user");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding receipt: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error adding receipt: {ex.Message}", "Add Error");
                Infrastructure.Logging.Logger.Error("AddReceiptAsync - Error adding receipt", ex);
            }
        }

        private async Task EditReceiptAsync()
        {
            if (SelectedReceipt == null)
            {
                Infrastructure.Logging.Logger.Warn("EditReceiptAsync - No receipt selected");
                return;
            }

            Infrastructure.Logging.Logger.Info($"EditReceiptAsync - Starting edit for Receipt #{SelectedReceipt.ReceiptNumberModern}");
            
            try
            {
                // Check if receipt is voided
                if (SelectedReceipt.IsVoidedModern)
                {
                    Infrastructure.Logging.Logger.Warn($"EditReceiptAsync - Cannot edit voided receipt #{SelectedReceipt.ReceiptNumberModern}");
                    await _dialogService.ShowMessageBoxAsync("Cannot edit a voided receipt.", "Edit Not Allowed");
                    return;
                }

                // Open receipt entry dialog for editing
                Infrastructure.Logging.Logger.Info("EditReceiptAsync - Creating ReceiptEntryViewModel with existing receipt");
                var entryViewModel = new ReceiptEntryViewModel(
                    _receiptService,
                    _growerService,
                    _productService,
                    _processService,
                    _depotService,
                    _dialogService,
                    SelectedReceipt);

                Infrastructure.Logging.Logger.Info("EditReceiptAsync - Initializing ReceiptEntryViewModel");
                await entryViewModel.InitializeAsync();

                Infrastructure.Logging.Logger.Info("EditReceiptAsync - Showing ReceiptEntryView dialog");
                var result = await _dialogService.ShowDialogAsync<ReceiptEntryView>(entryViewModel);

                if (result == true)
                {
                    Infrastructure.Logging.Logger.Info("EditReceiptAsync - Receipt updated successfully, reloading receipts");
                    await LoadReceiptsAsync();
                    StatusMessage = "Receipt updated successfully";
                }
                else
                {
                    Infrastructure.Logging.Logger.Info("EditReceiptAsync - Dialog cancelled by user");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error editing receipt: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error editing receipt: {ex.Message}", "Edit Error");
                Infrastructure.Logging.Logger.Error($"EditReceiptAsync - Error editing receipt #{SelectedReceipt?.ReceiptNumberModern}", ex);
            }
        }

        private async Task DeleteReceiptAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                var result = await _dialogService.ShowConfirmationAsync(
                    $"Are you sure you want to delete receipt {SelectedReceipt.ReceiptNumberModern}?\n\nThis will soft delete the receipt (it can be restored).",
                    "Confirm Delete");

                if (result == true)
                {
                    var receiptNumber = decimal.Parse(SelectedReceipt.ReceiptNumberModern ?? SelectedReceipt.ReceiptNumber.ToString());
                    var deleted = await _receiptService.DeleteReceiptAsync(receiptNumber);

                    if (deleted)
                    {
                        await LoadReceiptsAsync();
                        StatusMessage = "Receipt deleted successfully";
                    }
                    else
                    {
                        StatusMessage = "Failed to delete receipt";
                        await _dialogService.ShowMessageBoxAsync("Failed to delete receipt.", "Delete Error");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting receipt: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error deleting receipt: {ex.Message}", "Delete Error");
                Infrastructure.Logging.Logger.Error("Error deleting receipt in ReceiptViewModel", ex);
            }
        }

        private async Task VoidReceiptAsync()
        {
            if (SelectedReceipt == null || SelectedReceipt.IsVoidedModern) return;

            try
            {
                // Prompt for void reason
                var reason = await _dialogService.ShowInputDialogAsync(
                    "Please enter a reason for voiding this receipt:",
                    "Void Receipt");

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    var receiptNumber = decimal.Parse(SelectedReceipt.ReceiptNumberModern ?? SelectedReceipt.ReceiptNumber.ToString());
                    var voided = await _receiptService.VoidReceiptAsync(receiptNumber, reason);

                    if (voided)
                    {
                        await LoadReceiptsAsync();
                        StatusMessage = "Receipt voided successfully";
                    }
                    else
                    {
                        StatusMessage = "Failed to void receipt";
                        await _dialogService.ShowMessageBoxAsync("Failed to void receipt.", "Void Error");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error voiding receipt: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error voiding receipt: {ex.Message}", "Void Error");
                Infrastructure.Logging.Logger.Error("Error voiding receipt in ReceiptViewModel", ex);
            }
        }

        private async Task ViewReceiptDetailsAsync()
        {
            if (SelectedReceipt == null) return;

            try
            {
                // Get grower name
                var grower = await _growerService.GetGrowerByNumberAsync(SelectedReceipt.GrowerNumber);
                var growerName = grower?.GrowerName ?? "Unknown";

                // Build detailed message
                var details = $"Receipt Number: {SelectedReceipt.ReceiptNumberModern}\n" +
                             $"Date: {SelectedReceipt.ReceiptDate:yyyy-MM-dd}\n" +
                             $"Time: {SelectedReceipt.ReceiptTime}\n" +
                             $"Grower: {growerName} ({SelectedReceipt.GrowerNumber})\n" +
                             $"\n" +
                             $"Gross Weight: {SelectedReceipt.GrossWeight:N2} lbs\n" +
                             $"Tare Weight: {SelectedReceipt.TareWeight:N2} lbs\n" +
                             $"Net Weight: {SelectedReceipt.NetWeight:N2} lbs\n" +
                             $"Dock Percentage: {SelectedReceipt.DockPercentage:N2}%\n" +
                             $"Dock Weight: {SelectedReceipt.DockWeight:N2} lbs\n" +
                             $"Final Weight: {SelectedReceipt.FinalWeight:N2} lbs\n" +
                             $"\n" +
                             $"Grade: {SelectedReceipt.GradeModern}\n" +
                             $"Voided: {(SelectedReceipt.IsVoidedModern ? "Yes" : "No")}\n";

                if (SelectedReceipt.IsVoidedModern)
                {
                    details += $"Void Reason: {SelectedReceipt.VoidedReason}\n" +
                              $"Voided Date: {SelectedReceipt.VoidedAt:yyyy-MM-dd HH:mm}\n" +
                              $"Voided By: {SelectedReceipt.VoidedBy}\n";
                }

                details += $"\nCreated: {SelectedReceipt.CreatedAt:yyyy-MM-dd HH:mm} by {SelectedReceipt.CreatedBy}";

                if (SelectedReceipt.ModifiedAt.HasValue)
                {
                    details += $"\nModified: {SelectedReceipt.ModifiedAt:yyyy-MM-dd HH:mm} by {SelectedReceipt.ModifiedBy}";
                }

                await _dialogService.ShowMessageBoxAsync(details, "Receipt Details");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error viewing receipt details: {ex.Message}";
                Infrastructure.Logging.Logger.Error("Error viewing receipt details in ReceiptViewModel", ex);
            }
        }

        private void ClearFilters(object parameter)
        {
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now.Date;
            SearchText = string.Empty;
            ShowVoidedReceipts = false;
            _ = LoadReceiptsAsync();
        }

        #region Pagination Methods

        private void UpdatePagedReceipts()
        {
            Infrastructure.Logging.Logger.Info($"UpdatePagedReceipts - Page {CurrentPage}, Size: {PageSize}, Total Records: {TotalRecords}");

            // Calculate total pages
            if (PageSize == -1) // "All" option
            {
                TotalPages = 1;
                CurrentPage = 1;
            }
            else
            {
                TotalPages = TotalRecords == 0 ? 1 : (int)Math.Ceiling((double)TotalRecords / PageSize);
                
                // Ensure current page is within bounds
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }
                if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }
            }

            // Get the current page of receipts
            List<Receipt> pagedReceipts;
            if (PageSize == -1) // Show all
            {
                pagedReceipts = _allReceipts;
                Infrastructure.Logging.Logger.Info($"UpdatePagedReceipts - Showing all {_allReceipts.Count} receipts");
            }
            else
            {
                var skip = (CurrentPage - 1) * PageSize;
                pagedReceipts = _allReceipts.Skip(skip).Take(PageSize).ToList();
                Infrastructure.Logging.Logger.Info($"UpdatePagedReceipts - Showing {pagedReceipts.Count} receipts (skipped {skip})");
            }

            // Update the observable collection
            Receipts.Clear();
            foreach (var receipt in pagedReceipts)
            {
                Receipts.Add(receipt);
            }

            // Update statistics based on ALL receipts (not just current page)
            TotalReceiptCount = _allReceipts.Count;
            TotalGrossWeight = _allReceipts.Sum(r => r.GrossWeight);
            TotalNetWeight = _allReceipts.Sum(r => r.FinalWeight);

            // Update page info and status
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            
            StatusMessage = PageSize == -1 
                ? $"Showing all {TotalRecords} receipt(s)" 
                : $"Showing {pagedReceipts.Count} of {TotalRecords} receipt(s) - Page {CurrentPage} of {TotalPages}";

            Infrastructure.Logging.Logger.Info($"UpdatePagedReceipts - Updated. Displaying: {Receipts.Count}, Total: {TotalRecords}");
        }

        private void GoToFirstPage()
        {
            if (CurrentPage != 1)
            {
                Infrastructure.Logging.Logger.Info("GoToFirstPage - Navigating to first page");
                CurrentPage = 1;
                UpdatePagedReceipts();
            }
        }

        private void GoToPreviousPage()
        {
            if (CanGoToPreviousPage)
            {
                Infrastructure.Logging.Logger.Info($"GoToPreviousPage - From page {CurrentPage} to {CurrentPage - 1}");
                CurrentPage--;
                UpdatePagedReceipts();
            }
        }

        private void GoToNextPage()
        {
            if (CanGoToNextPage)
            {
                Infrastructure.Logging.Logger.Info($"GoToNextPage - From page {CurrentPage} to {CurrentPage + 1}");
                CurrentPage++;
                UpdatePagedReceipts();
            }
        }

        private void GoToLastPage()
        {
            if (CurrentPage != TotalPages)
            {
                Infrastructure.Logging.Logger.Info($"GoToLastPage - Navigating to last page ({TotalPages})");
                CurrentPage = TotalPages;
                UpdatePagedReceipts();
            }
        }

        #endregion

        #endregion
    }
}
