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

            // Price Management Help
            _helpContents["PriceManagement"] = new HelpContent(
                title: "Price Management Help",
                content: @"The Price Management screen allows you to create, view, edit, and manage pricing structures for products and processes.

**Key Features:**

• **View All Prices**: See a complete list of all price records with product, process, effective dates, and Level 1 Grade 1 prices displayed.

• **Search & Filter**: 
  - Search by product or process name
  - Filter by specific product
  - Filter by specific process
  - Filter by lock status (All/Unlocked Only/Any Locked)

• **Add New Prices**: Create new price records with all 36 price points across 3 levels, 3 grades, and 4 payment types (Advance 1, 2, 3, and Final).

• **Edit Prices**: Modify existing unlocked price records. Locked prices cannot be edited as they have been used in payments.

• **View Prices**: View price details in read-only mode without making changes.

• **Delete Prices**: Remove unlocked price records from the system.

**Pricing Structure:**

The system uses a comprehensive pricing matrix:

• **3 Price Levels**: Level 1 (standard), Level 2, Level 3
• **3 Grades**: Grade 1 (premium), Grade 2 (standard), Grade 3 (lower quality)
• **4 Payment Types**: 
  - Advance 1: First advance payment
  - Advance 2: Second advance payment
  - Advance 3: Third advance payment
  - Final: Final payment

**Price Validation Rules:**

• **Non-Negative**: All prices must be zero or positive
• **Progressive Advances**: 
  - Advance 2 must be ≥ Advance 1
  - Advance 3 must be ≥ Advance 2
• **Final Price Rule**: Final payment must be ≥ highest advance (max of A1, A2, A3)

**Lock Status:**

Price records are locked when they have been used in payments:
• 🔒 Red Lock: Advance 1 used
• 🔒 Orange Lock: Advance 2 used
• 🔒 Yellow Lock: Advance 3 used
• 🔓 Green Lock Check: Final payment used

Locked prices appear with a light orange background and cannot be edited or deleted.

**Time Premium:**

Optional time-based premium for early deliveries:
• Enable/disable time premium checkbox
• Set cutoff time (e.g., 10:10 AM)
• Specify Canadian premium amount
• Growers delivering before the cutoff receive the premium",

                quickTips: @"• Double-click any price row to view price details
• Use product and process filters to narrow down the list quickly
• Lock status filter helps find editable vs. locked prices
• Red borders on price fields indicate validation errors
• All prices are in Canadian dollars
• Most growers use Price Level 1
• Set prices to 0 for payment types not used
• Time premium is optional and applies to early morning deliveries
• Price validation happens in real-time as you type
• The warning banner appears if you try to cancel with unsaved changes
• Statistics show total and locked price counts
• Clear Filters button resets all filters at once",

                keyboardShortcuts: @"F1 - Show this help
F5 - Refresh price list
Enter - Execute search (when in search box)
Tab - Navigate between price entry fields
Esc - Close dialogs
Ctrl+Tab - Switch between price level tabs (Level 1/2/3)"
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

