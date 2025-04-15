using System; // Added missing using directive
using System.Security;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ChangePasswordView.xaml
    /// </summary>
    public partial class ChangePasswordView : UserControl
    {
        public ChangePasswordView()
        {
            InitializeComponent(); // This line might still show an error until rebuild
            // Subscribe to DataContextChanged to manage event subscription
            DataContextChanged += ChangePasswordView_DataContextChanged;
        }

        private void ChangePasswordView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from the old ViewModel, if any
            if (e.OldValue is ChangePasswordViewModel oldViewModel)
            {
                oldViewModel.PasswordChangeSuccess -= ViewModel_PasswordChangeSuccess;
            }

            // Subscribe to the new ViewModel, if any
            if (e.NewValue is ChangePasswordViewModel newViewModel)
            {
                newViewModel.PasswordChangeSuccess += ViewModel_PasswordChangeSuccess;
            }
        }

        // Event handler for successful password change
        private void ViewModel_PasswordChangeSuccess(object sender, EventArgs e)
        {
            // Clear the password boxes on the UI thread
            Dispatcher.Invoke(() =>
            {
                // These lines might still show errors until rebuild
                CurrentPasswordBox.Clear();
                NewPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
            });
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel viewModel)
            {
                // Create an array containing the SecureString from each PasswordBox
                var securePasswords = new SecureString[] {
                    CurrentPasswordBox.SecurePassword,
                    NewPasswordBox.SecurePassword,
                    ConfirmPasswordBox.SecurePassword
                };

                // Check if the command can execute with the parameters
                if (viewModel.ChangePasswordCommand.CanExecute(securePasswords))
                {
                    // Execute the command
                    viewModel.ChangePasswordCommand.Execute(securePasswords);

                    // Clearing fields is now handled by the ViewModel_PasswordChangeSuccess event handler
                }
            }
        }
    } // Added missing closing brace for the class
} // Added missing closing brace for the namespace
