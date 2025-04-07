using System;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services; // For IDialogService

namespace WPFGrowerApp.ViewModels
{
    public class ChangePasswordViewModel : ViewModelBase
    {
        private readonly IUserService _userService;
        private readonly IDialogService _dialogService;
        private string _username;
        private string _statusMessage;
        private string _errorMessage;
        private bool _isBusy;

        public ChangePasswordViewModel(IUserService userService, IDialogService dialogService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Get current username from the static property
            Username = App.CurrentUser?.Username ?? "Unknown User"; 

            ChangePasswordCommand = new RelayCommand(ChangePasswordExecuteAsync, CanChangePasswordExecute);
        }

        public string Username
        {
            get => _username;
            private set => SetProperty(ref _username, value); // Read-only from VM perspective
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }
         public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    ((RelayCommand)ChangePasswordCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public ICommand ChangePasswordCommand { get; }

        // Parameter will be an array/tuple of SecureStrings: {current, new, confirm}
        private bool CanChangePasswordExecute(object parameter)
        {
             if (IsBusy || !(parameter is object[] securePasswords) || securePasswords.Length != 3)
                 return false;

             var currentPassword = securePasswords[0] as SecureString;
             var newPassword = securePasswords[1] as SecureString;
             var confirmPassword = securePasswords[2] as SecureString;

             return currentPassword != null && currentPassword.Length > 0 &&
                    newPassword != null && newPassword.Length > 0 &&
                    confirmPassword != null && confirmPassword.Length > 0;
        }

        private async Task ChangePasswordExecuteAsync(object parameter)
        {
            if (!(parameter is object[] securePasswords) || securePasswords.Length != 3) return;

            var currentSecure = securePasswords[0] as SecureString;
            var newSecure = securePasswords[1] as SecureString;
            var confirmSecure = securePasswords[2] as SecureString;

            if (currentSecure == null || newSecure == null || confirmSecure == null) return;

            IsBusy = true;
            ErrorMessage = null;
            StatusMessage = null;

            IntPtr currentPtr = IntPtr.Zero;
            IntPtr newPtr = IntPtr.Zero;
            IntPtr confirmPtr = IntPtr.Zero;
            string currentPlain = null;
            string newPlain = null;
            string confirmPlain = null;

            try
            {
                // Securely get plain text passwords temporarily
                currentPtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(currentSecure);
                currentPlain = System.Runtime.InteropServices.Marshal.PtrToStringUni(currentPtr);
                newPtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(newSecure);
                newPlain = System.Runtime.InteropServices.Marshal.PtrToStringUni(newPtr);
                confirmPtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(confirmSecure);
                confirmPlain = System.Runtime.InteropServices.Marshal.PtrToStringUni(confirmPtr);

                // 1. Basic Validation
                if (string.IsNullOrWhiteSpace(newPlain) || string.IsNullOrWhiteSpace(confirmPlain))
                {
                    ErrorMessage = "New password fields cannot be empty.";
                    return;
                }
                if (newPlain != confirmPlain)
                {
                    ErrorMessage = "New password and confirmation password do not match.";
                    return;
                }

                // 2. Complexity Validation
                var complexityError = ValidatePasswordComplexity(newPlain);
                if (!string.IsNullOrEmpty(complexityError))
                {
                    ErrorMessage = complexityError;
                    return;
                }

                // 3. Verify Current Password
                StatusMessage = "Verifying current password...";
                var authenticatedUser = await _userService.AuthenticateAsync(Username, currentPlain);
                if (authenticatedUser == null)
                {
                    ErrorMessage = "Incorrect current password.";
                    return;
                }

                // 4. Set New Password
                StatusMessage = "Setting new password...";
                bool success = await _userService.SetPasswordAsync(Username, newPlain);

                if (success)
                {
                    StatusMessage = "Password changed successfully.";
                    // Optionally clear fields via messaging or direct view interaction if needed
                    await _dialogService.ShowMessageBoxAsync("Password changed successfully.", "Success"); // Use async
                }
                else
                {
                    ErrorMessage = "Failed to update password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error changing password for user '{Username}'.", ex);
                ErrorMessage = "An unexpected error occurred while changing the password.";
            }
            finally
            {
                // Securely clear plain text passwords
                if (currentPtr != IntPtr.Zero) System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(currentPtr);
                if (newPtr != IntPtr.Zero) System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(newPtr);
                if (confirmPtr != IntPtr.Zero) System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(confirmPtr);
                currentPlain = null; newPlain = null; confirmPlain = null;

                IsBusy = false;
            }
        }

        private string ValidatePasswordComplexity(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                return "Password must be at least 8 characters long.";
            }
            if (!password.Any(char.IsUpper))
            {
                return "Password must contain at least one uppercase letter.";
            }
            if (!password.Any(char.IsLower))
            {
                return "Password must contain at least one lowercase letter.";
            }
            if (!password.Any(char.IsDigit))
            {
                return "Password must contain at least one digit.";
            }
            // Example: Check for at least one special character
            if (!Regex.IsMatch(password, @"[\W_]")) // \W is non-word character (includes underscore with _)
            {
                 return "Password must contain at least one special character.";
            }

            return null; // Password meets complexity rules
        }
    }
}
