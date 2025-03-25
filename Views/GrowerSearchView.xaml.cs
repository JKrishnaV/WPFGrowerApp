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
            // Selection handling is done through binding to the Select button's IsEnabled property
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (GrowersDataGrid.SelectedItem is GrowerSearchResult selectedGrower)
            {
                SelectedGrowerNumber = selectedGrower.GrowerNumber;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a grower first.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
