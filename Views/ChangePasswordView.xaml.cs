  /// </summary>
using System.Security; // Added for SecureString
using System.Windows; // Added for RoutedEventArgs
using System.Windows.Controls;
using WPFGrowerApp.ViewModels; // Added for ViewModel reference

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ChangePasswordView.xaml
    /// </summary>
    public partial class ChangePasswordView : UserControl
    {
        public ChangePasswordView()
        {
            InitializeComponent();
        }

        // Removed password visibility toggle handlers

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

                    // Optionally clear password boxes after attempting change
                    // (ViewModel might handle this via status messages or events)
                    // CurrentPasswordBox.Clear();
                    // NewPasswordBox.Clear();
                    // ConfirmPasswordBox.Clear();
                }
            }
        }
    }
}
