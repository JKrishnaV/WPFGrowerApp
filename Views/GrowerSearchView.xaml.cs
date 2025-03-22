using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.ViewModels;
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

        public GrowerSearchView(IGrowerService growerService)
        {
            InitializeComponent();
            _viewModel = new GrowerSearchViewModel(growerService);
            DataContext = _viewModel;
            
            // Set focus to the search box
            Loaded += (s, e) => SearchTextBox.Focus();
        }

        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _viewModel.SearchCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ResultsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // This is handled by the binding to the IsEnabled property of the Select button
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsDataGrid.SelectedItem is Models.GrowerSearchResult selectedGrower)
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
