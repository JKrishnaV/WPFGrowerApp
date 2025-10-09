using System.Collections.Generic;

namespace WPFGrowerApp.Services
{
    /// <summary>
    /// Provides contextual help content for different views in the application
    /// </summary>
    public class HelpContentProvider : IHelpContentProvider
    {
        private readonly Dictionary<string, HelpContent> _helpContents;

        public HelpContentProvider()
        {
            _helpContents = new Dictionary<string, HelpContent>();
            InitializeHelpContents();
        }

        /// <summary>
        /// Gets help content for a specific view
        /// </summary>
        public HelpContent GetHelpContent(string viewName)
        {
            if (_helpContents.TryGetValue(viewName, out var content))
            {
                return content;
            }

            // Return default help if view-specific help is not found
            return new HelpContent(
                "Help",
                "Help content for this screen is not yet available. Please contact your system administrator for assistance.",
                null,
                "F1 - Show Help\nEsc - Close Dialog"
            );
        }

        /// <summary>
        /// Initialize all help contents for different views
        /// </summary>
        private void InitializeHelpContents()
        {
            // User Management Help
            _helpContents["UserManagement"] = new HelpContent(
                title: "User Management Help",
                content: @"The User Management screen allows administrators to create, edit, and manage user accounts for the Grower Application.

**Key Features:**

• View All Users: See a complete list of all users in the system with their details including username, full name, email, and last login date.

• Search Users: Use the search bar to quickly find users by their username, full name, or email address.

• Add New Users: Create new user accounts with username, password, email, and role assignment.

• Edit Users: Modify existing user details including full name, email, role, and active status.

• Delete Users: Remove user accounts (except your own) from the system.

• User Statistics: View real-time counts of total, active, and inactive users.

**User Status:**
• Active (Green Badge): User can log in and access the system
• Inactive (Gray Badge): User account is disabled and cannot log in

**Security Features:**
• Password complexity requirements enforced
• Account lockout after failed login attempts
• Admin role cannot be modified for the 'admin' user
• Users cannot delete or deactivate their own account",

                quickTips: @"• Double-click any user row to quickly edit that user
• Use the Enter key in the search box to execute a search
• Inactive users appear grayed out in the list
• The refresh button updates the user list with the latest data
• All changes are logged with your username for audit purposes
• When editing users, an asterisk (*) indicates unsaved changes",

                keyboardShortcuts: @"F1 - Show this help
Enter - Execute search (when in search box)
Esc - Close dialogs
F5 - Refresh user list (when refresh button is focused)
Tab - Navigate between fields
Shift+Tab - Navigate backwards between fields"
            );

            // Add more help contents for other views here as they are created
            _helpContents["Dashboard"] = new HelpContent(
                title: "Dashboard Help",
                content: "The Dashboard provides an overview of key information and quick access to common tasks.",
                quickTips: "• Use the navigation menu to access different modules\n• The dashboard updates automatically with real-time data",
                keyboardShortcuts: "F1 - Show Help"
            );
        }

        /// <summary>
        /// Add or update help content for a view (useful for dynamic content)
        /// </summary>
        public void AddOrUpdateHelpContent(string viewName, HelpContent content)
        {
            _helpContents[viewName] = content;
        }
    }
}

