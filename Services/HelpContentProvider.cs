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

            // Receipt Detail View Help
            _helpContents["ReceiptDetailView"] = new HelpContent(
                title: "Receipt Detail Help",
                content: @"The Receipt Detail screen provides comprehensive information about a specific receipt, including all related data and management capabilities.

**Key Features:**

• **Receipt Details Tab**: View and edit basic receipt information including receipt number, date, time, and audit information.

• **Grower & Product Tab**: Manage grower selection, depot assignment, and product information including process, variety, and price class.

• **Weights & Measurements Tab**: Enter and calculate weight measurements including gross weight, tare weight, net weight, dock percentage, and final weight.

• **Quality Control Tab**: Manage quality information including grade assignment, quality checking, and void information if applicable.

• **Payment Allocations Tab**: View all payment allocations associated with this receipt, including batch numbers, payment types, and amounts.

• **Audit History Tab**: Review complete audit trail of all changes made to this receipt, including who made changes and when.

**Receipt Management:**

• **View Mode**: Read-only access to receipt information with ability to export and print
• **Edit Mode**: Full editing capabilities for all receipt fields
• **New Receipt**: Create new receipts with default values and validation

**Weight Calculations:**

The system automatically calculates:
• **Net Weight** = Gross Weight - Tare Weight
• **Dock Weight** = Net Weight × (Dock Percentage ÷ 100)
• **Final Weight** = Net Weight - Dock Weight

**Quality Control:**

• **Grade Assignment**: Assign quality grades (1-3) to receipts
• **Quality Checking**: Mark receipts as quality checked with timestamp and user
• **Void Management**: Void receipts with reason and audit trail

**Payment Integration:**

• View all payment allocations for this receipt
• See payment batch information and amounts
• Track payment status and allocation details

**Export & Print Features:**

• **Export to Excel**: Generate detailed Excel report with all receipt information
• **Export to PDF**: Create professional PDF receipt document
• **Print**: Send receipt directly to printer with formatted layout

**Validation & Error Handling:**

• Real-time validation of all input fields
• Comprehensive error messages with field highlighting
• Unsaved changes tracking with confirmation dialogs
• Audit trail for all modifications",

                quickTips: @"• Use the summary cards to quickly see key receipt metrics
• Switch between View and Edit modes using the toggle button
• Weight calculations update automatically as you enter values
• Use the 'Recalculate Weights' button to refresh calculations
• Quality check receipts to mark them as verified
• Export to Excel for detailed analysis and reporting
• All changes are tracked in the audit history
• Use the search in Payment Allocations to find specific payments
• Filter audit history by change type for focused review
• Void receipts only when absolutely necessary as this action is permanent",

                keyboardShortcuts: @"F1 - Show this help
F5 - Refresh receipt data
Tab - Navigate between fields and tabs
Shift+Tab - Navigate backwards
Enter - Save changes (when in edit mode)
Esc - Cancel changes (when in edit mode)
Ctrl+S - Save receipt (when in edit mode)
Ctrl+E - Toggle edit mode
Ctrl+P - Print receipt
Ctrl+Shift+E - Export to Excel
Ctrl+Shift+P - Export to PDF"
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

            // Process Types Management Help
            _helpContents["ProcessView"] = new HelpContent(
                title: "Process Types Management Help",
                content: @"The Process Types Management screen allows you to create, view, edit, and manage process types used throughout the system.

**Key Features:**

• **View All Process Types**: See a complete list of all process types with their ID, description, code, default grade, and process class.

• **Search & Filter**: 
  - Search by description, code, ID, or process class
  - Real-time filtering as you type
  - Clear filters to reset search

• **Add New Process Types**: Create new process types with unique identifiers and descriptions.

• **Edit Process Types**: Modify existing process type information including description, code, default grade, and process class.

• **View Process Types**: View process type details in read-only mode without making changes.

• **Delete Process Types**: Remove process types from the system (use with caution).

**Process Type Fields:**

• **Process ID**: Unique numeric identifier for the process type
• **Process Code**: Short code (up to 8 characters) for the process
• **Description**: Full description of the process type (up to 19 characters)
• **Default Grade**: Default grade assigned (1-3, where 1 is highest quality)
• **Process Class**: Classification number (1-4) used for reporting and categorization

**Process Classifications:**

Process classes are used for reporting and categorization:
• **Class 1**: Primary processing operations
• **Class 2**: Secondary processing operations  
• **Class 3**: Quality control processes
• **Class 4**: Administrative processes

**Grade System:**

The default grade system works as follows:
• **Grade 1**: Premium quality (highest value)
• **Grade 2**: Standard quality (normal value)
• **Grade 3**: Lower quality (reduced value)

**Validation Rules:**

• **Process ID**: Must be a positive integer
• **Process Code**: Required, maximum 8 characters
• **Description**: Required, maximum 19 characters
• **Default Grade**: Must be between 1 and 3
• **Process Class**: Must be between 1 and 4

**Best Practices:**

• Use descriptive process codes that are easy to remember
• Keep descriptions concise but clear
• Assign appropriate default grades based on typical quality
• Use process classes consistently for reporting purposes
• Avoid deleting process types that are already in use",

                quickTips: @"• Double-click any process type row to view details
• Use the search box to quickly find specific process types
• Process types are used throughout the system for pricing and reporting
• Be careful when deleting process types as they may be referenced elsewhere
• The statistics card shows the total number of process types
• All fields have validation to ensure data integrity
• Use the refresh button to reload data from the database
• Process types are sorted alphabetically by description by default",

                keyboardShortcuts: @"F1 - Show this help
F5 - Refresh process types list
Enter - Execute search (when in search box)
Tab - Navigate between fields and controls
Esc - Close dialogs
Double-click - View process type details
Ctrl+N - Add new process type (when not in dialog)"
            );

            // Depot Management Help
            _helpContents["DepotView"] = new HelpContent(
                title: "Depot Management Help",
                content: @"The Depot Management screen allows you to create, view, edit, and manage depots used throughout the system.

**Key Features:**

• **View All Depots**: See a complete list of all depots with their ID, name, and code.

• **Search & Filter**: 
  - Search by depot name, code, or ID
  - Real-time filtering as you type
  - Clear filters to reset search

• **Add New Depots**: Create new depots with unique identifiers and names.

• **Edit Depots**: Modify existing depot information including name and code.

• **View Depots**: View depot details in read-only mode without making changes.

• **Delete Depots**: Remove depots from the system (use with caution).

**Depot Fields:**

• **Depot ID**: Unique numeric identifier for the depot
• **Depot Name**: Descriptive name for the depot (max 12 characters)
• **Depot Code**: Short code for the depot (max 8 characters)

**Validation Rules:**

• **Depot ID**: Must be a positive integer
• **Depot Name**: Required, maximum 12 characters
• **Depot Code**: Required, maximum 8 characters

**Best Practices:**

• Use descriptive depot names that clearly identify the location
• Keep depot codes short and memorable
• Avoid deleting depots that are already in use
• Use consistent naming conventions across all depots",

                quickTips: @"• Double-click any depot row to view details
• Use the search box to quickly find specific depots
• Depots are used throughout the system for location tracking
• Keep depot names concise but descriptive
• Use consistent naming conventions for better organization",

                keyboardShortcuts: "F1 - Show Help\nF5 - Refresh Data\nEsc - Close Dialog"
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

