using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ProcessView.xaml
    /// </summary>
    public partial class ProcessView : UserControl
    {
        public ProcessView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // CRITICAL: Auto-focus for immediate keyboard shortcuts
            Loaded += (s, e) => Focus();
        }

        private void ProcessesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ProcessViewModel viewModel && viewModel.SelectedProcess != null)
            {
                // Execute the ViewProcessCommand directly with the selected process
                viewModel.ViewProcessCommand?.Execute(viewModel.SelectedProcess);
            }
        }

        private void ViewProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Process process && DataContext is ProcessViewModel viewModel)
            {
                viewModel.ViewProcessCommand?.Execute(process);
            }
        }

        private void EditProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Process process && DataContext is ProcessViewModel viewModel)
            {
                viewModel.EditProcessCommand?.Execute(process);
            }
        }

        private void DeleteProcess_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Process process && DataContext is ProcessViewModel viewModel)
            {
                viewModel.DeleteProcessCommand?.Execute(process);
            }
        }
    }
}
