using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Collections.Generic;

namespace WPFGrowerApp.ViewModels
{
    public class UserManagementViewModel : ViewModelBase
    {
        private readonly IUserService _userService;
        private readonly IDialogService _dialogService;
        private User _selectedUser;
        private bool _isLoading;
        private bool _isNewUser;
        private string _editUsername;
        private string _editFullName;
        private string _editEmail;
        private bool _editIsActive;
        private string _errorMessage;
        private string _searchText;
        private bool _canEditRole ;
        private ObservableCollection<User> _filteredUsers;
        private ObservableCollection<Role> _roles;
        private Role _selectedRole;

        public UserManagementViewModel(IUserService userService, IDialogService dialogService)
        {
            // Check if current user is admin
            if (App.CurrentUser == null || !App.CurrentUser.IsAdmin)
            {
                throw new UnauthorizedAccessException("Only administrators can access user management.");
            }

            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Initialize commands
            NewCommand = new RelayCommand(NewUserExecute);
            SaveCommand = new RelayCommand(SaveExecuteAsync, CanSaveExecute);
            DeleteCommand = new RelayCommand(DeleteExecuteAsync, CanDeleteExecute);
            CancelCommand = new RelayCommand(CancelExecute, CanCancelExecute);

            // Initialize collections
            Users = new ObservableCollection<User>();
            _filteredUsers = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();

            // Setup property changed handler for search
            PropertyChanged += OnViewModelPropertyChanged;

            // Load users
            LoadUsersAsync().ConfigureAwait(false);

            // Load roles
            LoadRolesAsync().ConfigureAwait(false);
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SearchText))
            {
                FilterUsers();
            }
        }

        private void FilterUsers()
        {
            if (Users == null)
            {
                FilteredUsers = new ObservableCollection<User>();
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                FilteredUsers = new ObservableCollection<User>(Users);
            }
            else
            {
                var searchLower = SearchText.ToLower();
                FilteredUsers = new ObservableCollection<User>(
                    Users.Where(u => 
                        (u.Username?.ToLower().Contains(searchLower) == true) ||
                        (u.FullName?.ToLower().Contains(searchLower) == true) ||
                        (u.Email?.ToLower().Contains(searchLower) == true)
                    )
                );
            }
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            private set => SetProperty(ref _filteredUsers, value);
        }

        public ObservableCollection<User> Users { get; }

        public ObservableCollection<Role> Roles
        {
            get => _roles;
            private set => SetProperty(ref _roles, value);
        }

        public Role SelectedRole
        {
            get => _selectedRole;
            set
            {
                if (SetProperty(ref _selectedRole, value) && SelectedUser != null)
                {
                    SelectedUser.RoleId = value?.RoleId;
                }
            }
        }

        public bool CanEditRole
        {
            get => _canEditRole;
            private set => SetProperty(ref _canEditRole, value); // Make setter private
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    IsNewUser = false;
                    if (value != null)
                    {
                        // Load user details into edit form
                        EditUsername = value.Username;
                        EditFullName = value.FullName;
                        EditEmail = value.Email;
                        EditIsActive = value.IsActive;
                        SelectedRole = Roles.FirstOrDefault(r => r.RoleId == value.RoleId);
                        // Update CanEditRole based on the new SelectedUser
                        CanEditRole = !(value.Username != null && value.Username.Equals("admin", StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        // No user selected, allow editing role for a potential new user
                        CanEditRole = true; 
                    }
                    ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)CancelCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsNewUser
        {
            get => _isNewUser;
            set => SetProperty(ref _isNewUser, value);
        }

        public string EditUsername
        {
            get => _editUsername;
            set => SetProperty(ref _editUsername, value);
        }

        public string EditFullName
        {
            get => _editFullName;
            set => SetProperty(ref _editFullName, value);
        }

        public string EditEmail
        {
            get => _editEmail;
            set => SetProperty(ref _editEmail, value);
        }

        public bool EditIsActive
        {
            get => _editIsActive;
            set => SetProperty(ref _editIsActive, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task LoadUsersAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = null;
                var users = await _userService.GetAllUsersAsync();
                
                Users.Clear();
                foreach (var user in users.OrderBy(u => u.Username))
                {
                    Users.Add(user);
                }
                
                FilterUsers();
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading users", ex);
                ErrorMessage = "Failed to load users. Please try again.";
                await _dialogService.ShowMessageBoxAsync("Failed to load users. Please try again.", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NewUserExecute(object parameter)
        {
            SelectedUser = new User
            {
                IsActive = true,
                DateCreated = DateTime.Now
            };
            IsNewUser = true;
            // When creating a new user, the role should be editable
            CanEditRole = true; 
        }

        private bool CanSaveExecute(object parameter)
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(EditUsername) && !string.IsNullOrWhiteSpace(EditFullName);
        }

        private async Task SaveExecuteAsync(object parameter)
        {
            if (SelectedUser == null) return;

            try
            {
                IsLoading = true;
                ErrorMessage = null;

                // Validate input
                if (string.IsNullOrWhiteSpace(EditUsername))
                {
                    ErrorMessage = "Username is required.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(EditFullName))
                {
                    ErrorMessage = "Full name is required.";
                    return;
                }

                // Prevent deactivating own account
                if (SelectedUser.Username != null && SelectedUser.Username.Equals(App.CurrentUser?.Username, StringComparison.OrdinalIgnoreCase) && 
                    !EditIsActive)
                {
                    ErrorMessage = "You cannot deactivate your own account.";
                    return;
                }

                // Check if username exists (for new users)
                if (IsNewUser)
                {
                    var existingUser = await _userService.GetUserByUsernameAsync(EditUsername);
                    if (existingUser != null)
                    {
                        ErrorMessage = "Username already exists.";
                        return;
                    }
                }

                // Get password from PasswordBox if new user
                string password = null;
                if (IsNewUser)
                {
                    password = "tempPassword123!"; // Temporary for testing
                    
                    // Validate password complexity
                    var complexityError = ValidatePasswordComplexity(password);
                    if (!string.IsNullOrEmpty(complexityError))
                    {
                        ErrorMessage = complexityError;
                        return;
                    }
                }

                // Update user object
                SelectedUser.Username = EditUsername;
                SelectedUser.FullName = EditFullName;
                SelectedUser.Email = EditEmail;
                SelectedUser.IsActive = EditIsActive;

                bool success;
                if (IsNewUser)
                {
                    success = await _userService.CreateUserAsync(SelectedUser, password);
                }
                else
                {
                    success = await _userService.UpdateUserAsync(SelectedUser);
                }

                if (success)
                {
                    await LoadUsersAsync();
                    SelectedUser = null;
                    IsNewUser = false;
                    ClearFormFields();
                    await _dialogService.ShowMessageBoxAsync("User saved successfully.", "Success");

                    // Log the action
                    var action = IsNewUser ? "Created" : "Updated";
                    Logger.Info($"User Management: {action} user '{EditUsername}' by {App.CurrentUser?.Username}");
                }
                else
                {
                    ErrorMessage = "Failed to save user. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error saving user", ex);
                ErrorMessage = "An unexpected error occurred while saving the user.";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanDeleteExecute(object parameter)
        {
            return SelectedUser != null && !IsLoading && !IsNewUser;
        }

        private async Task DeleteExecuteAsync(object parameter)
        {
            if (SelectedUser == null) return;

            try
            {
                // Don't allow deleting your own account
                if (SelectedUser.Username.Equals(App.CurrentUser?.Username, StringComparison.OrdinalIgnoreCase))
                {
                    await _dialogService.ShowMessageBoxAsync("You cannot delete your own account.", "Warning");
                    return;
                }

                var result = await _dialogService.ShowConfirmationDialogAsync(
                    $"Are you sure you want to delete user '{SelectedUser.Username}'?",
                    "Confirm Delete");

                if (result)
                {
                    IsLoading = true;
                    var success = await _userService.DeleteUserAsync(SelectedUser.UserId);
                    if (success)
                    {
                        Users.Remove(SelectedUser);
                        SelectedUser = null;
                        await _dialogService.ShowMessageBoxAsync("User deleted successfully.", "Success");
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync("Failed to delete user. Please try again.", "Error");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error deleting user", ex);
                await _dialogService.ShowMessageBoxAsync("An unexpected error occurred while deleting the user.", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanCancelExecute(object parameter)
        {
            return SelectedUser != null && !IsLoading;
        }

        private void CancelExecute(object parameter)
        {
            SelectedUser = null;
            IsNewUser = false;
            ClearFormFields();
        }

        private void ClearFormFields()
        {
            EditUsername = string.Empty;
            EditFullName = string.Empty;
            EditEmail = string.Empty;
            EditIsActive = true;
            SelectedRole = null;
            ErrorMessage = null;
            CanEditRole = true; 
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

        private async Task LoadRolesAsync()
        {
            try
            {
                var roles = await _userService.GetAllRolesAsync();
                Roles.Clear();
                foreach (var role in roles.OrderBy(r => r.RoleName))
                {
                    Roles.Add(role);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading roles", ex);
                ErrorMessage = "Failed to load roles. Please try again.";
            }
        }
    }
}
