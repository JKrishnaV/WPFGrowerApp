using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.ViewModels.Dialogs;
using Microsoft.Extensions.DependencyInjection; // Added for GetService

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// Import Hub ViewModel - Provides card-based navigation to Import Files and Batch Management
    /// </summary>
    public class ImportHubViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;
        private readonly IHelpContentProvider _helpContentProvider;
        private readonly IImportBatchService _importBatchService;
        private readonly IReceiptService _receiptService;

        private ObservableCollection<ImportNavigationCard> _navigationCards;
        private ImportNavigationCard _selectedCard;
        private ViewModelBase _currentViewModel;
        private string _statusMessage = "Ready";
        private bool _isLoading;

        public ImportHubViewModel(
            IServiceProvider serviceProvider,
            IDialogService dialogService,
            IHelpContentProvider helpContentProvider,
            IImportBatchService importBatchService,
            IReceiptService receiptService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _helpContentProvider = helpContentProvider ?? throw new ArgumentNullException(nameof(helpContentProvider));
            _importBatchService = importBatchService ?? throw new ArgumentNullException(nameof(importBatchService));
            _receiptService = receiptService ?? throw new ArgumentNullException(nameof(receiptService));

            // Initialize navigation cards
            _navigationCards = new ObservableCollection<ImportNavigationCard>();
            InitializeNavigationCards();

            // Initialize commands
            InitializeCommands();

            // Load initial data
            _ = LoadInitialDataAsync();
        }

        public ObservableCollection<ImportNavigationCard> NavigationCards
        {
            get => _navigationCards;
            set => SetProperty(ref _navigationCards, value);
        }

        public ImportNavigationCard SelectedCard
        {
            get => _selectedCard;
            set
            {
                if (SetProperty(ref _selectedCard, value))
                {
                    NavigateToCard(value);
                }
            }
        }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Commands
        public ICommand ShowHelpCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }
        public ICommand NavigateToDashboardCommand { get; private set; }
        public ICommand NavigateToCardCommand { get; private set; }

        private void InitializeNavigationCards()
        {
            NavigationCards.Clear();

            // Import Files Card
            NavigationCards.Add(new ImportNavigationCard
            {
                Title = "Import Files",
                Description = "Import receipt files and manage import process",
                Icon = PackIconKind.FileImport,
                ViewModelType = typeof(ImportViewModel),
                Color = "#2196F3", // Blue
                IsEnabled = true
            });

            // Batch Management Card
            NavigationCards.Add(new ImportNavigationCard
            {
                Title = "Batch Management",
                Description = "View, manage, and delete import batches",
                Icon = PackIconKind.PackageVariant,
                ViewModelType = typeof(BatchManagementViewModel),
                Color = "#4CAF50", // Green
                IsEnabled = true
            });

            Logger.Info($"Initialized {NavigationCards.Count} import navigation cards");
        }

        private void InitializeCommands()
        {
            ShowHelpCommand = new RelayCommand(ShowHelpExecute);
            RefreshCommand = new RelayCommand(async _ => await RefreshAsync());
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);
            NavigateToCardCommand = new RelayCommand(NavigateToCardExecute);
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading import data...";

                // Load batch statistics for cards
                await LoadBatchStatisticsAsync();

                StatusMessage = "Import hub ready";
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading import hub data", ex);
                StatusMessage = "Error loading data";
                await _dialogService.ShowMessageBoxAsync($"Error loading import data: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadBatchStatisticsAsync()
        {
            try
            {
                // Get recent batch statistics
                var recentBatches = await _importBatchService.GetImportBatchesAsync();
                var totalBatches = recentBatches.Count;
                var recentBatchCount = recentBatches.Count(b => b.ImportDate >= DateTime.Now.AddDays(-7));

                // Update batch management card with statistics
                var batchCard = NavigationCards.FirstOrDefault(c => c.Title == "Batch Management");
                if (batchCard != null)
                {
                    batchCard.Statistics = $"Total: {totalBatches} | Recent: {recentBatchCount}";
                }

                Logger.Info($"Loaded batch statistics: {totalBatches} total, {recentBatchCount} recent");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading batch statistics", ex);
                // Don't throw - this is not critical for the hub to function
            }
        }

        private void NavigateToCard(ImportNavigationCard card)
        {
            Logger.Info($"NavigateToCard called with card: {card?.Title ?? "null"}, IsEnabled: {card?.IsEnabled}");
            
            if (card == null || !card.IsEnabled) 
            {
                Logger.Warn($"Card is null or disabled: {card?.Title ?? "null"}");
                return;
            }

            try
            {
                Logger.Info($"Navigating to {card.Title}, ViewModelType: {card.ViewModelType.Name}");

                // Use the main navigation system instead of child views
                if (card.ViewModelType == typeof(ImportViewModel))
                {
                    // Navigate to Import Files view
                    NavigateToImportFiles();
                }
                else if (card.ViewModelType == typeof(BatchManagementViewModel))
                {
                    // Navigate to Batch Management view
                    NavigateToBatchManagement();
                }
                else
                {
                    Logger.Error($"Unknown ViewModel type: {card.ViewModelType.Name}");
                    StatusMessage = "Unknown navigation target";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error navigating to {card.Title}", ex);
                StatusMessage = "Navigation error";
                _dialogService.ShowMessageBoxAsync($"Error navigating to {card.Title}: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToCardExecute(object parameter)
        {
            Logger.Info($"NavigateToCardExecute called with parameter: {parameter?.GetType().Name ?? "null"}");
            
            if (parameter is ImportNavigationCard card)
            {
                Logger.Info($"Navigating to card: {card.Title}");
                NavigateToCard(card);
            }
            else
            {
                Logger.Warn($"Parameter is not ImportNavigationCard: {parameter?.GetType().Name ?? "null"}");
            }
        }

        private void NavigateToImportFiles()
        {
            try
            {
                Logger.Info("Navigating to Import Files view");
                
                // Use NavigationHelper to navigate to ImportViewModel
                NavigationHelper.NavigateToImportFiles();
                Logger.Info("Successfully requested navigation to Import Files");
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Import Files", ex);
                StatusMessage = "Navigation error";
                _dialogService.ShowMessageBoxAsync($"Error navigating to Import Files: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToBatchManagement()
        {
            try
            {
                Logger.Info("Navigating to Batch Management view");
                
                // Use NavigationHelper to navigate to BatchManagementViewModel
                NavigationHelper.NavigateToBatchManagement();
                Logger.Info("Successfully requested navigation to Batch Management");
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to Batch Management", ex);
                StatusMessage = "Navigation error";
                _dialogService.ShowMessageBoxAsync($"Error navigating to Batch Management: {ex.Message}", "Navigation Error");
            }
        }

        private void NavigateToDashboardExecute(object parameter)
        {
            try
            {
                // This would typically use a navigation service or event
                // For now, we'll just clear the current view
                CurrentViewModel = null;
                SelectedCard = null;
                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to dashboard", ex);
            }
        }

        private async Task RefreshAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Refreshing data...";

                await LoadBatchStatisticsAsync();

                StatusMessage = "Data refreshed";
            }
            catch (Exception ex)
            {
                Logger.Error("Error refreshing data", ex);
                StatusMessage = "Refresh failed";
                await _dialogService.ShowMessageBoxAsync($"Error refreshing data: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ShowHelpExecute(object parameter)
        {
            try
            {
                var helpContent = _helpContentProvider.GetHelpContent("ImportHubView");
                var helpViewModel = new HelpDialogViewModel(
                    helpContent.Title,
                    helpContent.Content,
                    helpContent.QuickTips,
                    helpContent.KeyboardShortcuts
                );
                await _dialogService.ShowDialogAsync(helpViewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Error showing help", ex);
                await _dialogService.ShowMessageBoxAsync("Unable to display help.", "Error");
            }
        }
    }

    /// <summary>
    /// Navigation card model for Import Hub
    /// </summary>
    public class ImportNavigationCard
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public PackIconKind Icon { get; set; }
        public Type ViewModelType { get; set; } = typeof(ViewModelBase);
        public string Color { get; set; } = "#757575";
        public bool IsEnabled { get; set; } = true;
        public string Statistics { get; set; } = string.Empty;
    }
}
