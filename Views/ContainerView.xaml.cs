using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ContainerView.xaml
    /// </summary>
    public partial class ContainerView : UserControl
    {
        public ContainerView()
        {
            InitializeComponent();
        }

        private void ContainersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ContainerViewModel viewModel && viewModel.SelectedContainer != null)
            {
                viewModel.ViewContainerCommand.Execute(viewModel.SelectedContainer);
            }
        }

        private void ViewContainer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ContainerType container)
            {
                if (DataContext is ContainerViewModel viewModel)
                {
                    viewModel.ViewContainerCommand.Execute(container);
                }
            }
        }

        private void EditContainer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ContainerType container)
            {
                if (DataContext is ContainerViewModel viewModel)
                {
                    viewModel.EditContainerCommand.Execute(container);
                }
            }
        }

        private void DeleteContainer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ContainerType container)
            {
                if (DataContext is ContainerViewModel viewModel)
                {
                    viewModel.DeleteContainerCommand.Execute(container);
                }
            }
        }
    }
}
