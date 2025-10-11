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

            // Payment Batch Detail Help
            _helpContents["PaymentBatchDetailView"] = new HelpContent(
                title: "Payment Batch Detail Help",
                content: @"The Payment Batch Detail screen provides comprehensive information about a specific payment batch, including detailed analytics and insights.

**Key Features:**

• **Grower Payments Tab**: View summary information for each grower including payment amounts, receipt counts, and cheque details.

• **Receipt Allocations Tab**: See detailed breakdown of all receipt allocations within the batch, with filtering by grower.

• **Cheques Tab**: Review all cheques generated for this batch, including status and printing information.

• **Analytics Tab**: Access statistical analysis including payment distributions, product breakdowns, and anomaly detection.

• **Batch Info Tab**: View batch creation parameters, filters applied, and system information.

**Analytics Insights:**

• **Quick Stats**: Key metrics including average payment per grower, largest payment, and total weight.

• **Payment Distribution**: Visual breakdown of payment amounts across different ranges.

• **Product Breakdown**: Analysis of payments by product type and contribution percentages.

• **Anomaly Detection**: Statistical analysis to identify unusual payment patterns.

• **Comparison Data**: Comparison with previous batches to identify trends and changes.

**Chart Information:**

Charts require sufficient data to display meaningful insights:
• Payment distribution charts need multiple growers with different payment amounts
• Product breakdown charts require multiple product types with varying amounts
• Range analysis charts need growers distributed across different payment ranges

When charts show 'No Data' messages, this indicates the batch has insufficient data points for meaningful analysis.

**Export & Print Features:**

• **Export to Excel**: Generates a multi-sheet Excel workbook containing:
  - Sheet 1: Batch Summary with key metrics and analytics
  - Sheet 2: Grower Payments with full details
  - Sheet 3: Receipt Allocations with all payment details
  - Sheet 4: Cheques with status and dates
  - Sheet 5: Analytics data tables

• **Export to PDF**: Creates a professional PDF report with:
  - Batch summary and financial metrics
  - Grower payments table
  - Analytics overview with statistics
  - Professional formatting for distribution

• **Print**: Sends batch report directly to printer with formatted layout including batch summary and grower payments table.",
                quickTips: @"• Use the search box in Grower Payments to quickly find specific growers
• Filter Receipt Allocations by grower to focus on specific allocations
• Analytics charts automatically show/hide based on available data
• Anomaly detection helps identify unusual payment patterns
• Comparison notes show changes from previous batches
• Click Export to Excel for complete batch data in multi-sheet workbook
• Click Export to PDF for professional summary report with analytics
• Click Print for immediate printing of batch report",
                keyboardShortcuts: @"F1 - Show this help
F5 - Refresh all batch data
Tab - Navigate between tabs and controls
Shift+Tab - Navigate backwards
Enter - Execute search (when in search boxes)
Esc - Close dialogs and popups"
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

