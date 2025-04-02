using System; // Added for ArgumentNullException
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
        // Keep reference to ViewModel if needed for event handlers
        private readonly GrowerSearchViewModel _viewModel; 

        public decimal? SelectedGrowerNumber { get; private set; }

        // Inject the ViewModel
        public GrowerSearchView(GrowerSearchViewModel viewModel) 
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Initialize the ViewModel asynchronously after the view is loaded
            Loaded += async (s, e) => 
            {
                await _viewModel.InitializeAsync();
                SearchTextBox.Focus(); // Set focus after initialization
            };
            
            // Set focus to the search box (moved to Loaded event)
            Loaded += (s, e) => SearchTextBox.Focus();
        }

        // Event Handlers for Custom Window Controls
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            // Optional: Change icon based on state if needed (requires more logic)
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        // --- End of Custom Window Control Handlers ---

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
