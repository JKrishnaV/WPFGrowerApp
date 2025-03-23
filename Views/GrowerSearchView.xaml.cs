using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Models;
using WPFGrowerApp.DataAccess.Services;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerSearchView.xaml
    /// </summary>
    public partial class GrowerSearchView : Window
    {
        private readonly GrowerSearchViewModel _viewModel;

        public decimal? SelectedGrowerNumber { get; private set; }

        public GrowerSearchView(GrowerSearchViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            
            // Set focus to the search box
            Loaded += (s, e) => SearchTextBox.Focus();
            
            // Pass the window instance to the NewGrowerCommand
            _viewModel.NewGrowerCommand = new RelayCommand(param => NewGrowerCommand_Execute(this));
        }
        
        private void NewGrowerCommand_Execute(Window window)
        {
            // Create a new GrowerViewModel with a blank grower
            var growerService = new GrowerService(new DataAccess.Repositories.GrowerRepository(
                new DataAccess.DapperConnectionManager()));
            var growerViewModel = new GrowerViewModel(growerService);
            growerViewModel.CreateNewGrower();
            
            // Create and show the GrowerView
            var growerView = new GrowerView();
            growerView.DataContext = growerViewModel;
            
            // Create a new window to host the GrowerView
            var newWindow = new Window
            {
                Title = "New Grower",
                Content = growerView,
                SizeToContent = SizeToContent.WidthAndHeight,
                MinWidth = 700,
                MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            
            window.Close();
            newWindow.Show();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _viewModel.SearchCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is handled by the binding to the IsEnabled property of the Select button
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsDataGrid.SelectedItem is GrowerSearchResult selectedGrower)
            {
                SelectedGrowerNumber = selectedGrower.GrowerNumber;
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
