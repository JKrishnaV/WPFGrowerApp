using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Services;

namespace WPFGrowerApp.ViewModels
{
    public class GrowerSearchViewModel : INotifyPropertyChanged
    {
        private readonly IGrowerService _growerService;
        private string _searchTerm;
        private ObservableCollection<Models.GrowerSearchResult> _searchResults;
        private bool _isSearching;

        public GrowerSearchViewModel(IGrowerService growerService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            SearchResults = new ObservableCollection<Models.GrowerSearchResult>();
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

                // Call the service to get all growers
                var results = await _growerService.GetAllGrowersAsync();

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

        // Modify the SearchAsync method to use the service
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
                var results = await _growerService.SearchGrowersAsync(SearchTerm);

                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error searching for growers: {ex.Message}", "Search Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

        public ObservableCollection<Models.GrowerSearchResult> SearchResults
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
