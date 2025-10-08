using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.ViewModels.Dialogs;
using MaterialDesignThemes.Wpf;

namespace WPFGrowerApp.Views.Dialogs
{
    public partial class UserEditDialogView : UserControl
    {
        public UserEditDialogView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserEditDialogViewModel viewModel)
            {
                viewModel.Password = PasswordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserEditDialogViewModel viewModel)
            {
                viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }

    }
}

