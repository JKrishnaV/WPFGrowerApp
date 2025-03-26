using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.ViewModels
{
    public class GrowerSearchViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        private string _searchText;
        private ObservableCollection<GrowerSearchResult> _searchResults;
        private bool _isSearching;

        public GrowerSearchViewModel(IGrowerService growerService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            SearchResults = new ObservableCollection<GrowerSearchResult>();
            SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);
            LoadAllGrowersAsync().ConfigureAwait(false);
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
            return !IsSearching;
        }

        private void ExecuteSearch(object parameter)
        {
            SearchAsync().ConfigureAwait(false);
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
                // Handle error appropriately
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
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
                // Handle error appropriately
                System.Diagnostics.Debug.WriteLine($"Load error: {ex.Message}");
            }
            finally
            {
                IsSearching = false;
            }
        }
    }
}
