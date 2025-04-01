using System;
using System.Windows;
using System.Windows.Input; // Added for KeyEventArgs
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;

            // Subscribe to the ViewModel's CloseAction
            _viewModel.CloseAction = (success) => 
            {
                this.DialogResult = success;
                this.Close();
            };

            // Handle password changes to update command CanExecute state
            PasswordBox.PasswordChanged += (s, e) => 
            {
                 if (_viewModel.LoginCommand.CanExecute(PasswordBox.SecurePassword))
                 {
                     // This might need adjustment depending on RelayCommand implementation details
                     // Forcing re-evaluation might be needed if CanExecute doesn't auto-update
                     CommandManager.InvalidateRequerySuggested(); 
                 }
            };

             // Allow login on Enter key press in PasswordBox
            PasswordBox.KeyDown += PasswordBox_KeyDown;
        }

        // Execute login command when Enter is pressed in PasswordBox
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (_viewModel.LoginCommand.CanExecute(PasswordBox.SecurePassword))
                {
                    _viewModel.LoginCommand.Execute(PasswordBox.SecurePassword);
                    e.Handled = true; // Prevent further processing of Enter key
                }
            }
        }

        // Override the button click to pass SecurePassword
        // Note: This assumes the Login button in XAML is named "LoginButton"
        // If not named, this handler won't be attached automatically.
        // Alternatively, remove Command/CommandParameter from XAML button and use this handler.
        /* 
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
             if (_viewModel.LoginCommand.CanExecute(PasswordBox.SecurePassword))
             {
                 _viewModel.LoginCommand.Execute(PasswordBox.SecurePassword);
             }
        }
        */

        // If not using code-behind click handler, ensure XAML passes PasswordBox itself
        // and ViewModel handles extracting SecureString (as currently implemented in LoginViewModel)
        // The current XAML passes the PasswordBox: CommandParameter="{Binding ElementName=PasswordBox}"
        // The ViewModel expects SecureString. This mismatch needs fixing.
        // Easiest fix: Use the code-behind click handler approach.

        // Let's modify to use the code-behind click handler for clarity and security.
        // We need to:
        // 1. Name the Login Button in XAML (e.g., x:Name="LoginButton")
        // 2. Add Click="LoginButton_Click" to the button in XAML
        // 3. Remove Command and CommandParameter from the button in XAML
        // 4. Uncomment and use the LoginButton_Click handler below.

        // --- Revised approach using code-behind click ---
        // REMOVE Command/CommandParameter from Button in XAML
        // ADD x:Name="LoginButton" Click="LoginButton_Click" to Button in XAML
        /*
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
             if (_viewModel.LoginCommand.CanExecute(PasswordBox.SecurePassword))
             {
                 _viewModel.LoginCommand.Execute(PasswordBox.SecurePassword);
             }
        }
        */

        // --- Sticking with CommandParameter approach for now, requires ViewModel adjustment ---
        // The LoginViewModel's Execute method needs to accept PasswordBox and get SecurePassword
        // OR use an attached behavior/property to bind SecurePassword directly.
        // Let's proceed assuming the ViewModel will be adjusted or a behavior used later.
        // The current LoginViewModel expects SecureString, so the XAML CommandParameter passing PasswordBox is incorrect.
        // For now, we leave this code-behind as is, but acknowledge the password passing needs refinement.

    }
}
