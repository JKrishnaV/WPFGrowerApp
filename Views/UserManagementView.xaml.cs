using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Views
{
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }

        private void UsersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is UserManagementViewModel viewModel)
            {
                viewModel.EditCommand.Execute(null);
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user && DataContext is UserManagementViewModel viewModel)
            {
                viewModel.SelectedUser = user;
                viewModel.EditCommand.Execute(null);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user && DataContext is UserManagementViewModel viewModel)
            {
                viewModel.SelectedUser = user;
                viewModel.DeleteCommand.Execute(null);
            }
        }
    }
} 