using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerListView.xaml
    /// </summary>
    public partial class GrowerListView : UserControl
    {
        public GrowerListView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Set focus to the UserControl when loaded so keyboard shortcuts work
            Loaded += (s, e) => Focus();
        }

        private void GrowersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is GrowerListViewModel viewModel && viewModel.SelectedGrower != null)
            {
                // Double-click to view grower details
                if (viewModel.ViewGrowerCommand.CanExecute(viewModel.SelectedGrower))
                {
                    viewModel.ViewGrowerCommand.Execute(viewModel.SelectedGrower);
                }
            }
        }

        private void ViewGrower_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.GrowerSearchResult grower)
            {
                if (DataContext is GrowerListViewModel viewModel)
                {
                    // Set the selected grower and execute view command
                    viewModel.SelectedGrower = grower;
                    if (viewModel.ViewGrowerCommand.CanExecute(grower))
                    {
                        viewModel.ViewGrowerCommand.Execute(grower);
                    }
                }
            }
        }

        private void EditGrower_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.GrowerSearchResult grower)
            {
                if (DataContext is GrowerListViewModel viewModel)
                {
                    // Set the selected grower and execute edit command
                    viewModel.SelectedGrower = grower;
                    if (viewModel.EditGrowerCommand.CanExecute(grower))
                    {
                        viewModel.EditGrowerCommand.Execute(grower);
                    }
                }
            }
        }

        private void DeleteGrower_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Models.GrowerSearchResult grower)
            {
                if (DataContext is GrowerListViewModel viewModel)
                {
                    // Set the selected grower and execute delete command
                    viewModel.SelectedGrower = grower;
                    if (viewModel.DeleteGrowerCommand.CanExecute(grower))
                    {
                        viewModel.DeleteGrowerCommand.Execute(grower);
                    }
                }
            }
        }
    }
}
