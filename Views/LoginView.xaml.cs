using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;
        private readonly PasswordBox _passwordBox;

        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Get reference to PasswordBox from XAML
            _passwordBox = this.FindName("PasswordBox") as PasswordBox;
            if (_passwordBox == null)
            {
                throw new InvalidOperationException("PasswordBox control not found in XAML");
            }

            // Subscribe to the ViewModel's CloseAction
            _viewModel.CloseAction = (success) => 
            {
                this.DialogResult = success;
                this.Close();
            };

            // Handle password changes to update command CanExecute state
            _passwordBox.PasswordChanged += (s, e) => 
            {
                if (_viewModel.LoginCommand.CanExecute(_passwordBox.SecurePassword))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            };

            // Allow login on Enter key press in PasswordBox
            _passwordBox.KeyDown += PasswordBox_KeyDown;
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_viewModel.LoginCommand.CanExecute(_passwordBox.SecurePassword))
                {
                    _viewModel.LoginCommand.Execute(_passwordBox.SecurePassword);
                    e.Handled = true;
                }
            }
        }
    }
}
