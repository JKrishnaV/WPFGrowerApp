using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services; // Added for IDialogService
using System.Threading;
using WPFGrowerApp.Infrastructure.Logging; // Added for Logger

namespace WPFGrowerApp.ViewModels
{
    public class GrowerSearchViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        private readonly IDialogService _dialogService; // Added
        private string _searchText;
        private ObservableCollection<GrowerSearchResult> _searchResults;
        private bool _isSearching;
        private CancellationTokenSource _debounceTokenSource;
        private readonly int _debounceDelayMs = 500; 

        // Inject IDialogService
        public GrowerSearchViewModel(IGrowerService growerService, IDialogService dialogService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); // Store service
            SearchResults = new ObservableCollection<GrowerSearchResult>();
            // Use async RelayCommand
            SearchCommand = new RelayCommand(ExecuteSearchAsync, CanExecuteSearch); 
            // Initialization moved to InitializeAsync
        }

        // Call this after construction
        public async Task InitializeAsync()
        {
            await LoadAllGrowersAsync();
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    
                    // Implement debounce for auto-search
                    DebounceSearch();
                }
            }
        }

        public ObservableCollection<GrowerSearchResult> SearchResults
        {
            get => _searchResults;
            set
            {
                if (_searchResults != value)
                {
                    _searchResults = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            set
            {
                if (_isSearching != value)
                {
                    _isSearching = value;
                    OnPropertyChanged();
                    (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand SearchCommand { get; }

        private bool CanExecuteSearch(object parameter)
        {
            // CanExecute should prevent search if already searching
            return !IsSearching; 
        }

        // Renamed to indicate async
        private async Task ExecuteSearchAsync(object parameter)
        {
            // Await the async search
            await SearchAsync(); 
        }

        private void DebounceSearch()
        {
            // Cancel previous debounce timer
            _debounceTokenSource?.Cancel();
            _debounceTokenSource = new CancellationTokenSource();
            
            var token = _debounceTokenSource.Token;
            
            Task.Delay(_debounceDelayMs, token).ContinueWith(task => 
            {
                if (!task.IsCanceled)
                {
                    // Execute search after delay if not canceled
                    // Use the async version
                    ExecuteSearchAsync(null).ConfigureAwait(false); // Fire and forget is okay for debounce trigger
                }
            }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure UI updates happen on UI thread
        }

        private async Task SearchAsync()
        {
            try
            {
                IsSearching = true;
                SearchResults.Clear();

                var results = await _growerService.SearchGrowersAsync(SearchText ?? string.Empty);
                
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during grower search for '{SearchText}': {ex.Message}", ex);
                await _dialogService.ShowMessageBoxAsync($"Search failed: {ex.Message}", "Search Error"); // Use async
            }
            finally
            {
                IsSearching = false;
            }
        }

        private async Task LoadAllGrowersAsync()
        {
            try
            {
                IsSearching = true;
                SearchResults.Clear();

                var results = await _growerService.GetAllGrowersAsync();
                
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading all growers: {ex.Message}", ex);
                 await _dialogService.ShowMessageBoxAsync($"Failed to load initial grower list: {ex.Message}", "Load Error"); // Use async
            }
            finally
            {
                IsSearching = false;
            }
        }
    }
    
}
