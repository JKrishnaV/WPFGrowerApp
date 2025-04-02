
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
            InitializeComponent();
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

                }
            }
        }
    }
}
