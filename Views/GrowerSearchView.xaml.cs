using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.Services;
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
            _viewModel = ServiceConfiguration.GetService<GrowerSearchViewModel>();
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
            if (GrowersDataGrid.SelectedItem != null)
            {
                var selectedGrower = GrowersDataGrid.SelectedItem as GrowerSearchResult;
                if (selectedGrower != null)
                {
                    SelectedGrowerNumber = selectedGrower.GrowerNumber;
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedGrowerNumber.HasValue)
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void NewGrowerButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedGrowerNumber = 0; // This will indicate a new grower
            DialogResult = true;
            Close();
        }

        private void GrowersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedGrowerNumber.HasValue)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
