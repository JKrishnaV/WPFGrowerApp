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
                (_viewModel.LoginCommand as Commands.RelayCommand)?.RaiseCanExecuteChanged();
            };

            // Allow login on Enter key press in PasswordBox
            _passwordBox.KeyDown += PasswordBox_KeyDown;

            // Add Loaded event handler for focus
            Loaded += LoginView_Loaded;
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            // Pre-fill password if remembered
            if (_viewModel.RememberPassword && !string.IsNullOrEmpty(_viewModel.RememberedPassword))
            {
                _passwordBox.Password = _viewModel.RememberedPassword;
            }

            // Set initial focus based on whether username is pre-filled
            if (this.FindName("UsernameTextBox") is TextBox usernameBox)
            {
                if (string.IsNullOrEmpty(usernameBox.Text))
                {
                    // Username is empty, focus it
                    usernameBox.Focus();
                }
                else
                {
                    // Username is pre-filled, focus password box
                    _passwordBox?.Focus(); // Use the cached _passwordBox reference
                }
            }
            else // Fallback if UsernameTextBox isn't found (shouldn't happen)
            {
                 _passwordBox?.Focus();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Set DialogResult to false (or null) to indicate cancellation/close without login
            this.DialogResult = false; 
            this.Close();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Pass the PasswordBox control itself, consistent with the Button's CommandParameter
                if (_viewModel.LoginCommand.CanExecute(_passwordBox)) 
                {
                    _viewModel.LoginCommand.Execute(_passwordBox); 
                    e.Handled = true;
                }
            }
        }
    }
}
