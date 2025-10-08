using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using WPFGrowerApp.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    public class UserEditDialogViewModel : ViewModelBase, IDataErrorInfo
    {
        private User _user;
        private bool _isEditMode;
        private string _title;
        private string _username;
        private string _fullName;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private bool _isActive;
        private Role _selectedRole;
        private ObservableCollection<Role> _roles;
        private bool _canEditUsername;
        private bool _canEditRole;
        private bool _showPasswordFields;
        private bool _hasUnsavedChanges;
        private bool _showWarningBanner;
        private readonly User _originalUser;
        private readonly IDialogService _dialogService;

        public User UserData
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public Role SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public ObservableCollection<Role> Roles
        {
            get => _roles;
            set => SetProperty(ref _roles, value);
        }

        public bool CanEditUsername
        {
            get => _canEditUsername;
            private set => SetProperty(ref _canEditUsername, value);
        }

        public bool CanEditRole
        {
            get => _canEditRole;
            private set => SetProperty(ref _canEditRole, value);
        }

        public bool ShowPasswordFields
        {
            get => _showPasswordFields;
            private set => SetProperty(ref _showPasswordFields, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }
        
        public bool ShowWarningBanner
        {
            get => _showWarningBanner;
            set => SetProperty(ref _showWarningBanner, value);
        }

        // Flag to indicate if the dialog was saved
        public bool WasSaved { get; private set; } = false;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ConfirmDiscardCommand { get; }
        public ICommand CancelDiscardCommand { get; }

        public UserEditDialogViewModel(User user = null, ObservableCollection<Role> roles = null, IDialogService dialogService = null)
        {
            _dialogService = dialogService ?? App.ServiceProvider?.GetService(typeof(IDialogService)) as IDialogService;
            Roles = roles ?? new ObservableCollection<Role>();

            if (user == null || user.UserId == 0)
            {
                // Add Mode
                UserData = new User
                {
                    IsActive = true,
                    DateCreated = DateTime.Now
                };
                IsEditMode = false;
                Title = "Add New User";
                Username = string.Empty;
                FullName = string.Empty;
                Email = string.Empty;
                IsActive = true;
                CanEditUsername = true;
                CanEditRole = true;
                ShowPasswordFields = true; // Show password fields for new users
                _originalUser = null; // No original user for new users
            }
            else
            {
                // Edit Mode - Create a copy to avoid modifying the original until save
                UserData = new User
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    RoleId = user.RoleId,
                    IsActive = user.IsActive,
                    DateCreated = user.DateCreated,
                    LastLoginDate = user.LastLoginDate,
                    FailedLoginAttempts = user.FailedLoginAttempts,
                    IsLockedOut = user.IsLockedOut,
                    LastLockoutDate = user.LastLockoutDate
                };
                
                // Store original values for comparison
                _originalUser = new User
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    RoleId = user.RoleId,
                    IsActive = user.IsActive
                };
                
                IsEditMode = true;
                Title = "Edit User";
                Username = user.Username;
                FullName = user.FullName;
                Email = user.Email;
                IsActive = user.IsActive;
                
                // Don't allow editing username for existing users
                CanEditUsername = false;
                
                // Don't allow editing admin role
                CanEditRole = !(user.Username != null && user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase));
                
                // Don't show password fields for existing users (password reset would be separate)
                ShowPasswordFields = false;
                
                // Set selected role
                if (user.RoleId.HasValue && Roles != null)
                {
                    SelectedRole = Roles.FirstOrDefault(r => r.RoleId == user.RoleId.Value);
                }
            }

            // Subscribe to property changes to detect modifications
            PropertyChanged += OnPropertyChanged;

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            ConfirmDiscardCommand = new RelayCommand(ConfirmDiscard);
            CancelDiscardCommand = new RelayCommand(CancelDiscard);
        }

        private void Save(object parameter)
        {
            // Perform final validation
            if (!IsValid())
            {
                return;
            }

            // Update the user object with form values
            UserData.Username = Username;
            UserData.FullName = FullName;
            UserData.Email = Email;
            UserData.IsActive = IsActive;
            UserData.RoleId = SelectedRole?.RoleId;

            HasUnsavedChanges = false; // Clear unsaved changes flag
            WasSaved = true;
            
            // Close the dialog with true indicating success/save
            DialogHost.CloseDialogCommand.Execute(true, null);
        }

        private bool CanSave(object parameter)
        {
            return IsValid();
        }

        private bool IsValid()
        {
            // Check required fields
            if (string.IsNullOrWhiteSpace(Username))
                return false;

            if (string.IsNullOrWhiteSpace(FullName))
                return false;

            // Validate password for new users
            if (!IsEditMode)
            {
                if (string.IsNullOrWhiteSpace(Password))
                    return false;

                if (Password != ConfirmPassword)
                    return false;

                if (ValidatePasswordComplexity(Password) != null)
                    return false;
            }

            // Check for validation errors
            if (!string.IsNullOrEmpty(this[nameof(Username)]) ||
                !string.IsNullOrEmpty(this[nameof(FullName)]) ||
                !string.IsNullOrEmpty(this[nameof(Email)]) ||
                !string.IsNullOrEmpty(this[nameof(Password)]) ||
                !string.IsNullOrEmpty(this[nameof(ConfirmPassword)]))
            {
                return false;
            }

            return true;
        }

        private void Cancel(object parameter)
        {
            if (HasUnsavedChanges)
            {
                // Show warning banner when trying to cancel with unsaved changes
                ShowWarningBanner = true;
                // Don't close the dialog yet - let user see the warning
                return;
            }
            
            // No unsaved changes, close immediately
            HasUnsavedChanges = false;
            WasSaved = false;
            DialogHost.CloseDialogCommand.Execute(false, null);
        }

        private void ConfirmDiscard(object parameter)
        {
            // User confirmed they want to discard changes
            HasUnsavedChanges = false;
            ShowWarningBanner = false;
            WasSaved = false;
            DialogHost.CloseDialogCommand.Execute(false, null);
        }

        private void CancelDiscard(object parameter)
        {
            // User decided not to discard changes, hide warning banner
            ShowWarningBanner = false;
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
            if (!Regex.IsMatch(password, @"[\W_]"))
            {
                return "Password must contain at least one special character.";
            }

            return null;
        }

        // --- IDataErrorInfo Implementation for validation ---
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string result = null;

                switch (columnName)
                {
                    case nameof(Username):
                        if (string.IsNullOrWhiteSpace(Username))
                            result = "Username is required.";
                        else if (Username.Length < 3)
                            result = "Username must be at least 3 characters.";
                        else if (Username.Length > 50)
                            result = "Username cannot exceed 50 characters.";
                        else if (!Regex.IsMatch(Username, @"^[a-zA-Z0-9_]+$"))
                            result = "Username can only contain letters, numbers, and underscores.";
                        break;

                    case nameof(FullName):
                        if (string.IsNullOrWhiteSpace(FullName))
                            result = "Full name is required.";
                        else if (FullName.Length > 100)
                            result = "Full name cannot exceed 100 characters.";
                        break;

                    case nameof(Email):
                        if (!string.IsNullOrWhiteSpace(Email))
                        {
                            if (Email.Length > 100)
                                result = "Email cannot exceed 100 characters.";
                            else if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                                result = "Please enter a valid email address.";
                        }
                        break;

                    case nameof(Password):
                        if (!IsEditMode && ShowPasswordFields)
                        {
                            if (string.IsNullOrWhiteSpace(Password))
                                result = "Password is required.";
                            else
                            {
                                var complexityError = ValidatePasswordComplexity(Password);
                                if (complexityError != null)
                                    result = complexityError;
                            }
                        }
                        break;

                    case nameof(ConfirmPassword):
                        if (!IsEditMode && ShowPasswordFields)
                        {
                            if (string.IsNullOrWhiteSpace(ConfirmPassword))
                                result = "Please confirm your password.";
                            else if (Password != ConfirmPassword)
                                result = "Passwords do not match.";
                        }
                        break;
                }

                // Update CanSave when validation state changes
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                return result;
            }
        }

        // --- Unsaved Changes Tracking ---
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Don't track changes to HasUnsavedChanges itself
            if (e.PropertyName != nameof(HasUnsavedChanges))
            {
                CheckForUnsavedChanges();
            }
        }

        private void CheckForUnsavedChanges()
        {
            if (_originalUser == null) 
            {
                // For new users, check if any field has content
                HasUnsavedChanges = !string.IsNullOrWhiteSpace(Username) ||
                                   !string.IsNullOrWhiteSpace(FullName) ||
                                   !string.IsNullOrWhiteSpace(Email) ||
                                   !string.IsNullOrWhiteSpace(Password) ||
                                   !string.IsNullOrWhiteSpace(ConfirmPassword);
                return;
            }

            // For existing users, compare with original values
            HasUnsavedChanges = 
                Username != _originalUser.Username ||
                FullName != _originalUser.FullName ||
                Email != _originalUser.Email ||
                IsActive != _originalUser.IsActive ||
                SelectedRole?.RoleId != _originalUser.RoleId;
                
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"HasUnsavedChanges: {HasUnsavedChanges}");
            if (HasUnsavedChanges)
            {
                System.Diagnostics.Debug.WriteLine($"Changes detected: Username={Username != _originalUser.Username}, " +
                    $"FullName={FullName != _originalUser.FullName}, Email={Email != _originalUser.Email}, " +
                    $"IsActive={IsActive != _originalUser.IsActive}, Role={SelectedRole?.RoleId != _originalUser.RoleId}");
            }
        }

        // Method to handle dialog closing attempts (called from View)
        public bool HandleDialogClosing()
        {
            // Simply prevent closing if there are unsaved changes
            // User must explicitly use Cancel button to discard changes
            if (HasUnsavedChanges)
            {
                return false; // Prevent closing
            }
            
            HasUnsavedChanges = false;
            return true; // Allow closing
        }
    }
}

