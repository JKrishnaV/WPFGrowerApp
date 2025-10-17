using System;
using System.Security; // For SecureString
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Properties; // Added for Settings
using MaterialDesignThemes.Wpf; // Added for PackIconKind

namespace WPFGrowerApp.ViewModels
{
    public class LoginViewModel : ViewModelBase // Assuming ViewModelBase provides INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private string _username;
        private string _errorMessage;
        private bool _isLoggingIn;
        private User _authenticatedUser; // Added field to store the user
        private bool _isPasswordVisible; // Added for password toggle
        private bool _rememberPassword; // Added for remember password functionality
        private string _rememberedPassword; // Added for storing remembered password

        // Action to be called by the View to close the window
        public Action<bool> CloseAction { get; set; }

        // Public property to expose the authenticated user
        public User AuthenticatedUser => _authenticatedUser;

        public LoginViewModel(IUserService userService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            LoginCommand = new RelayCommand(LoginExecuteAsync, CanLoginExecute);
            TogglePasswordVisibilityCommand = new RelayCommand(TogglePasswordVisibilityExecute); // Added command

            // Load last username and remember password setting from settings
            try
            {
                Username = Properties.Settings.Default.LastUsername;
                RememberPassword = Properties.Settings.Default.RememberPassword;
                _rememberedPassword = DecryptPassword(Properties.Settings.Default.RememberedPassword);
                Logger.Info($"Loaded last username: {Username}, RememberPassword: {RememberPassword}");
            }
            catch (Exception ex)
            {
                // Log error but don't prevent startup
                Logger.Error("Failed to load settings.", ex);
                Username = string.Empty; // Default to empty if loading fails
                RememberPassword = false; // Default to false if loading fails
                _rememberedPassword = string.Empty;
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

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                // Call SetProperty and raise PropertyChanged for PasswordToggleIconKind as well
                if (SetProperty(ref _isPasswordVisible, value))
                {
                    OnPropertyChanged(nameof(PasswordToggleIconKind));
                }
            }
        }

        // Property to return the correct icon kind based on visibility state
        public PackIconKind PasswordToggleIconKind => IsPasswordVisible ? PackIconKind.Eye : PackIconKind.EyeOff;

        public bool RememberPassword
        {
            get => _rememberPassword;
            set => SetProperty(ref _rememberPassword, value);
        }

        public string RememberedPassword
        {
            get => _rememberedPassword;
            set => SetProperty(ref _rememberedPassword, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand TogglePasswordVisibilityCommand { get; } // Added command property

        private bool CanLoginExecute(object parameter)
        {
            // Corrected: Parameter is the PasswordBox from XAML binding
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            // Check if the VisiblePasswordTextBox is being used (if implemented this way)
            // For now, assume PasswordBox is the source of truth for CanExecute
            return !IsLoggingIn &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   passwordBox != null &&
                   passwordBox.SecurePassword != null &&
                   passwordBox.SecurePassword.Length > 0;
        }

        private async Task LoginExecuteAsync(object parameter)
        {
            // Corrected: Parameter is the PasswordBox from XAML binding
            // The code-behind ensures PasswordBox.Password is updated before this might be called
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
                    
                    // Save username and remember password setting
                    try
                    {
                        Properties.Settings.Default.LastUsername = Username;
                        Properties.Settings.Default.RememberPassword = RememberPassword;
                        
                        // Save password if remember is enabled
                        if (RememberPassword)
                        {
                            Properties.Settings.Default.RememberedPassword = EncryptPassword(plainPassword);
                        }
                        else
                        {
                            Properties.Settings.Default.RememberedPassword = string.Empty;
                        }
                        
                        Properties.Settings.Default.Save();
                        Logger.Info($"Saved login settings: Username={Username}, RememberPassword={RememberPassword}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to save login settings.", ex);
                        // Don't prevent login if settings save fails
                    }
                    
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

        private void TogglePasswordVisibilityExecute(object parameter)
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        // Simple encryption/decryption for password storage
        // Note: This is basic obfuscation, not enterprise-grade security
        private string EncryptPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            try
            {
                // Simple base64 encoding with a basic key
                var key = "BerryFarms2024!";
                var bytes = Encoding.UTF8.GetBytes(password + key);
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            try
            {
                // Simple base64 decoding with key removal
                var key = "BerryFarms2024!";
                var bytes = Convert.FromBase64String(encryptedPassword);
                var decrypted = Encoding.UTF8.GetString(bytes);
                
                // Remove the key suffix
                if (decrypted.EndsWith(key))
                {
                    return decrypted.Substring(0, decrypted.Length - key.Length);
                }
                
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
