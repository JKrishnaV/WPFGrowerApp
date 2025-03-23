using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.ViewModels
{
    public class GrowerSearchViewModel : INotifyPropertyChanged
    {
        private readonly GrowerService _growerService;
        private string _searchTerm;
        private ObservableCollection<GrowerSearchResult> _searchResults;
        private bool _isSearching;

        public GrowerSearchViewModel(GrowerService growerService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            SearchResults = new ObservableCollection<GrowerSearchResult>();
            SearchCommand = new AsyncRelayCommand(SearchCommandExecute, CanExecuteSearchCommand);
            NewGrowerCommand = new RelayCommand(NewGrowerCommandExecute);

            // Load all growers when the view is initialized
            LoadAllGrowersAsync();
        }

        // Add this new method to handle the New Grower button click
        private void NewGrowerCommandExecute(object parameter)
        {
            // Create a new GrowerViewModel with a blank grower
            var growerViewModel = new GrowerViewModel(_growerService);
            growerViewModel.CreateNewGrower();
            
            // Create and show the GrowerView
            var growerView = new Views.GrowerView();
            growerView.DataContext = growerViewModel;
            
            // Get the parent window and close it
            if (parameter is System.Windows.Window parentWindow)
            {
                // Create a new window to host the GrowerView
                var window = new System.Windows.Window
                {
                    Title = "New Grower",
                    Content = growerView,
                    SizeToContent = System.Windows.SizeToContent.WidthAndHeight,
                    MinWidth = 700,
                    MinHeight = 600,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen
                };
                
                parentWindow.Close();
                window.Show();
            }
        }

        // Add this new method to load all growers
        private async void LoadAllGrowersAsync()
        {
            try
            {
                IsSearching = true;
                SearchResults.Clear();

                // Call the search method with empty string to get all growers
                var results = await _growerService.SearchGrowersAsync("");

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
        public ICommand NewGrowerCommand { get; }

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
