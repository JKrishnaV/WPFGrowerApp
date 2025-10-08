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
using WPFGrowerApp.ViewModels.Dialogs;
using System.Windows;

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
        private bool _canEditRole;
        private ObservableCollection<User> _filteredUsers;
        private ObservableCollection<Role> _roles;
        private Role _selectedRole;
        private string _statusMessage = "Ready";
        private string _lastUpdated;

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
            EditCommand = new RelayCommand(EditExecute, CanEditExecute);
            SaveCommand = new RelayCommand(SaveExecuteAsync, CanSaveExecute);
            DeleteCommand = new RelayCommand(DeleteExecuteAsync, CanDeleteExecute);
            CancelCommand = new RelayCommand(CancelExecute, CanCancelExecute);
            SearchCommand = new RelayCommand(SearchExecute);
            ExportCommand = new RelayCommand(ExportExecute);
            RefreshCommand = new RelayCommand(RefreshExecute);
            ClearSearchCommand = new RelayCommand(ClearSearchExecute);
            NavigateToDashboardCommand = new RelayCommand(NavigateToDashboardExecute);

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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        // Computed properties for statistics
        public int ActiveUsersCount => Users?.Count(u => u.IsActive) ?? 0;
        public int InactiveUsersCount => Users?.Count(u => !u.IsActive) ?? 0;

    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand NavigateToDashboardCommand { get; }

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
                
                // Notify UI that statistics have changed
                OnPropertyChanged(nameof(ActiveUsersCount));
                OnPropertyChanged(nameof(InactiveUsersCount));
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

        private async void NewUserExecute(object parameter)
        {
            try
            {
                // Create dialog view model for new user
                var dialogViewModel = new UserEditDialogViewModel(null, Roles, _dialogService);

                // Show the dialog
                await _dialogService.ShowDialogAsync(dialogViewModel);

                // If user saved, create the new user
                if (dialogViewModel.WasSaved)
                {
                    IsLoading = true;
                    var newUser = dialogViewModel.UserData;
                    
                    // Validate that username doesn't already exist
                    var existingUser = await _userService.GetUserByUsernameAsync(newUser.Username);
                    if (existingUser != null)
                    {
                        await _dialogService.ShowMessageBoxAsync("Username already exists. Please choose a different username.", "Error");
                        IsLoading = false;
                        return;
                    }

                    // Create the user with the password
                    var success = await _userService.CreateUserAsync(newUser, dialogViewModel.Password);

                    if (success)
                    {
                        await LoadUsersAsync();
                        StatusMessage = $"User '{newUser.Username}' created successfully";
                        await _dialogService.ShowMessageBoxAsync($"User '{newUser.Username}' created successfully.", "Success");
                        
                        Logger.Info($"User Management: Created user '{newUser.Username}' by {App.CurrentUser?.Username}");
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync("Failed to create user. Please try again.", "Error");
                        StatusMessage = "Failed to create user";
                    }
                    
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error creating new user", ex);
                await _dialogService.ShowMessageBoxAsync("An unexpected error occurred while creating the user.", "Error");
                IsLoading = false;
            }
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
                    var deletedUsername = SelectedUser.Username;
                    var success = await _userService.DeleteUserAsync(SelectedUser.UserId);
                    if (success)
                    {
                        Users.Remove(SelectedUser);
                        FilterUsers(); // Update filtered users
                        SelectedUser = null;
                        
                        // Update statistics
                        OnPropertyChanged(nameof(ActiveUsersCount));
                        OnPropertyChanged(nameof(InactiveUsersCount));
                        
                        StatusMessage = $"User '{deletedUsername}' deleted successfully";
                        await _dialogService.ShowMessageBoxAsync("User deleted successfully.", "Success");
                        
                        Logger.Info($"User Management: Deleted user '{deletedUsername}' by {App.CurrentUser?.Username}");
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync("Failed to delete user. Please try again.", "Error");
                        StatusMessage = "Failed to delete user";
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
                
                StatusMessage = "Ready";
                LastUpdated = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                Logger.Error("Error loading roles", ex);
                ErrorMessage = "Failed to load roles. Please try again.";
                StatusMessage = "Error loading roles";
            }
        }

        private async void NavigateToDashboardExecute(object parameter)
        {
            try
            {
                StatusMessage = "Navigation to dashboard...";
                
                // Get the MainViewModel from the MainWindow
                if (Application.Current?.MainWindow?.DataContext is MainViewModel mainViewModel)
                {
                    // Execute the dashboard navigation command
                    if (mainViewModel.NavigateToDashboardCommand?.CanExecute(null) == true)
                    {
                        mainViewModel.NavigateToDashboardCommand.Execute(null);
                        StatusMessage = "Navigated to Dashboard";
                    }
                    else
                    {
                        StatusMessage = "Unable to navigate to Dashboard";
                    }
                }
                else
                {
                    StatusMessage = "Navigation service not available";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error navigating to dashboard", ex);
                StatusMessage = "Navigation failed";
            }
        }

        private bool CanEditExecute(object parameter)
        {
            return SelectedUser != null;
        }

        private async void EditExecute(object parameter)
        {
            if (SelectedUser == null) return;

            try
            {
                // Create dialog view model for editing
                var dialogViewModel = new UserEditDialogViewModel(SelectedUser, Roles, _dialogService);

                // Show the dialog
                await _dialogService.ShowDialogAsync(dialogViewModel);

                // If user saved, update the user
                if (dialogViewModel.WasSaved)
                {
                    IsLoading = true;
                    var updatedUser = dialogViewModel.UserData;

                    // Prevent deactivating own account
                    if (updatedUser.Username != null && 
                        updatedUser.Username.Equals(App.CurrentUser?.Username, StringComparison.OrdinalIgnoreCase) && 
                        !updatedUser.IsActive)
                    {
                        await _dialogService.ShowMessageBoxAsync("You cannot deactivate your own account.", "Warning");
                        IsLoading = false;
                        return;
                    }

                    // Update the user
                    var success = await _userService.UpdateUserAsync(updatedUser);

                    if (success)
                    {
                        // Update the user in the collection
                        var existingUser = Users.FirstOrDefault(u => u.UserId == updatedUser.UserId);
                        if (existingUser != null)
                        {
                            var index = Users.IndexOf(existingUser);
                            Users[index] = updatedUser;
                        }

                        await LoadUsersAsync();
                        StatusMessage = $"User '{updatedUser.Username}' updated successfully";
                        await _dialogService.ShowMessageBoxAsync($"User '{updatedUser.Username}' updated successfully.", "Success");
                        
                        Logger.Info($"User Management: Updated user '{updatedUser.Username}' by {App.CurrentUser?.Username}");
                    }
                    else
                    {
                        await _dialogService.ShowMessageBoxAsync("Failed to update user. Please try again.", "Error");
                        StatusMessage = "Failed to update user";
                    }
                    
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error editing user", ex);
                await _dialogService.ShowMessageBoxAsync("An unexpected error occurred while updating the user.", "Error");
                IsLoading = false;
            }
        }

        private void ExportExecute(object parameter)
        {
            StatusMessage = "Export functionality not yet implemented";
            _dialogService.ShowMessageBoxAsync("Export functionality will be implemented to export users to CSV/Excel format.", "Export Users");
        }

        private void RefreshExecute(object parameter)
        {
            _ = LoadUsersAsync();
            _ = LoadRolesAsync();
        }

        private void SearchExecute(object parameter)
        {
            FilterUsers();
            StatusMessage = $"Search completed - {FilteredUsers.Count} users found";
        }

        private void ClearSearchExecute(object parameter)
        {
            SearchText = string.Empty;
            StatusMessage = "Search cleared";
        }
    }
}
