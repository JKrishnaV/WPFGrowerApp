using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.Models;


namespace WPFGrowerApp.ViewModels
{
    public class GrowerSearchViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _databaseService;
        private string _searchTerm;
        private ObservableCollection<GrowerSearchResult> _searchResults;
        private bool _isSearching;


        public GrowerSearchViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            SearchResults = new ObservableCollection<GrowerSearchResult>();
            SearchCommand = new AsyncRelayCommand(SearchCommandExecute, CanExecuteSearchCommand);

            // Load all growers when the view is initialized
            LoadAllGrowersAsync();
        }

        // Add this new method to load all growers
        private async void LoadAllGrowersAsync()
        {
            try
            {
                IsSearching = true;
                SearchResults.Clear();

                // Call a new method in DatabaseService to get all growers
                var results = await _databaseService.GetAllGrowersAsync();

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading growers: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsSearching = false;
            }
        }

        // Modify the SearchAsync method to filter the existing results if search term is provided
        private async Task SearchAsync()
        {
            try
            {
                IsSearching = true;

                if (string.IsNullOrWhiteSpace(SearchTerm))
                {
                    // If search term is empty, load all growers
                    await Task.Run(() => LoadAllGrowersAsync());
                    return;
                }

                SearchResults.Clear();
                var results = await _databaseService.SearchGrowersAsync(SearchTerm);

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                // Error handling...
            }
            finally
            {
                IsSearching = false;
            }
        }

        private bool CanExecuteSearchCommand(object parameter)
        {
            return !IsSearching;
        }

        private async Task SearchCommandExecute(object parameter)
        {
            await SearchAsync();
        }
        public string SearchTerm
        {
            get => _searchTerm;
            set
            {
                if (_searchTerm != value)
                {
                    _searchTerm = value;
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
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand SearchCommand { get; }

        //private async Task SearchAsync()
        //{
        //    if (string.IsNullOrWhiteSpace(SearchTerm))
        //        return;

        //    try
        //    {
        //        IsSearching = true;
        //        SearchResults.Clear();

        //        var results = await _databaseService.SearchGrowersAsync(SearchTerm);
                
        //        foreach (var result in results)
        //        {
        //            SearchResults.Add(result);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // In a production app, you would log this exception and show a user-friendly message
        //        System.Windows.MessageBox.Show($"Error searching for growers: {ex.Message}", "Search Error", 
        //            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        //    }
        //    finally
        //    {
        //        IsSearching = false;
        //    }
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, Task> _executeAsync;
        private readonly Predicate<object> _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<object, Task> executeAsync, Predicate<object> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _executeAsync(parameter);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

}
