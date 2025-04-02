using System;
using System.Security; // For SecureString
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models; 
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Properties; // Added for Settings

namespace WPFGrowerApp.ViewModels
{
    public class LoginViewModel : ViewModelBase // Assuming ViewModelBase provides INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private string _username;
        private string _errorMessage;
        private bool _isLoggingIn;
        private User _authenticatedUser; // Added field to store the user

        // Action to be called by the View to close the window
        public Action<bool> CloseAction { get; set; }

        // Public property to expose the authenticated user
        public User AuthenticatedUser => _authenticatedUser; 

        public LoginViewModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            LoginCommand = new RelayCommand(LoginExecuteAsync, CanLoginExecute);

            // Load last username from settings
            try
            {
                Username = Properties.Settings.Default.LastUsername;
                Logger.Info($"Loaded last username: {Username}");
            }
            catch (Exception ex)
            {
                // Log error but don't prevent startup
                Logger.Error("Failed to load LastUsername setting.", ex);
                Username = string.Empty; // Default to empty if loading fails
            }
        }

        public string Username
        {
            get => _username;
            set 
            {
                if (SetProperty(ref _username, value))
                {
                    // Also notify the command when Username changes
                    ((RelayCommand)LoginCommand).RaiseCanExecuteChanged();
                }
            }
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
            // Corrected: Parameter is the PasswordBox from XAML binding
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            return !IsLoggingIn && 
                   !string.IsNullOrWhiteSpace(Username) && 
                   passwordBox != null && 
                   passwordBox.SecurePassword != null && 
                   passwordBox.SecurePassword.Length > 0;
        }

        private async Task LoginExecuteAsync(object parameter)
        {
            // Corrected: Parameter is the PasswordBox from XAML binding
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            if (passwordBox == null || passwordBox.SecurePassword == null) return; 

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
                    _authenticatedUser = user; // Store the authenticated user
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
