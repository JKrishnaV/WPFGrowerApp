using System;
using System.Security; // For SecureString
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels
{
    public class LoginViewModel : ViewModelBase // Assuming ViewModelBase provides INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private string _username;
        private string _errorMessage;
        private bool _isLoggingIn;

        // Action to be called by the View to close the window
        public Action<bool> CloseAction { get; set; }

        public LoginViewModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            LoginCommand = new RelayCommand(LoginExecuteAsync, CanLoginExecute);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value); // Assuming SetProperty is in ViewModelBase
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            private set
            {
                if (SetProperty(ref _isLoggingIn, value))
                {
                    // Notify CanExecuteChanged for the command when IsLoggingIn changes
                    ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand LoginCommand { get; }

        private bool CanLoginExecute(object parameter)
        {
            // Parameter is expected to be the PasswordBox control itself
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            return !IsLoggingIn && 
                   !string.IsNullOrWhiteSpace(Username) && 
                   passwordBox != null && 
                   passwordBox.SecurePassword != null && 
                   passwordBox.SecurePassword.Length > 0;
        }

        private async Task LoginExecuteAsync(object parameter)
        {
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            if (passwordBox == null || passwordBox.SecurePassword == null) return; // Should not happen if CanExecute is correct

            var securePassword = passwordBox.SecurePassword; // Get SecureString here

            IsLoggingIn = true;
            ErrorMessage = null; // Clear previous errors

            // Convert SecureString to plain text string for authentication
            // IMPORTANT: This is the point where the password exists in memory as plain text.
            // Minimize its lifetime.
            IntPtr ptr = IntPtr.Zero;
            string plainPassword = null;
            try
            {
                ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                plainPassword = System.Runtime.InteropServices.Marshal.PtrToStringUni(ptr);

                var user = await _userService.AuthenticateAsync(Username, plainPassword);

                if (user != null)
                {
                    Logger.Info($"User '{Username}' logged in successfully.");
                    // Signal success and close the login window
                    CloseAction?.Invoke(true); 
                }
                else
                {
                    Logger.Warn($"Login failed for user '{Username}'.");
                    ErrorMessage = "Invalid username or password."; // Keep error generic for security
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"An unexpected error occurred during login for user '{Username}'.", ex);
                ErrorMessage = "An error occurred during login. Please try again.";
            }
            finally
            {
                // Securely clear the plain text password from memory
                if (ptr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
                plainPassword = null; // Explicitly nullify
                IsLoggingIn = false;
            }
        }
    }
}
