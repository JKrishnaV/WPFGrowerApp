using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ImportUserControl.xaml
    /// </summary>
    public partial class ImportUserControl : UserControl
    {
        // Parameterless constructor is required for instantiation via DataTemplate
        public ImportUserControl()
        {
            InitializeComponent();
            // DataContext will be set automatically by WPF based on the DataTemplate
            // in ViewMappings.xaml when the bound content is an ImportViewModel.
            
            // Add keyboard shortcut for Ctrl+C to copy selected errors
            ErrorsListView.PreviewKeyDown += ErrorsListView_PreviewKeyDown;
        }

        private void ErrorsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Pass selected items to ViewModel for copy operation
            if (DataContext is ImportViewModel viewModel && sender is ListView listView)
            {
                viewModel.SelectedErrors = listView.SelectedItems.Cast<string>().ToList();
            }
        }

        private void ErrorsListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle Ctrl+C for copying
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (DataContext is ImportViewModel viewModel)
                {
                    viewModel.CopyErrorsCommand.Execute(null);
                    e.Handled = true;
                }
            }
            // Ctrl+A is already handled natively by ListView with SelectionMode="Extended"
        }
    }
}
