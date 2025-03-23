using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.DataAccess;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Models;


namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerSearchView.xaml
    /// </summary>
    public partial class GrowerSearchView : Window
    {
        private readonly GrowerSearchViewModel _viewModel;

        public decimal? SelectedGrowerNumber { get; private set; }

        public GrowerSearchView()
        {
            InitializeComponent();
            _viewModel = new GrowerSearchViewModel(new DatabaseService());
            DataContext = _viewModel;
            
            // Set focus to the search box
            Loaded += (s, e) => SearchTextBox.Focus();
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
