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

            // Advance Cheques Help
            _helpContents["AdvanceChequeView"] = new HelpContent(
                title: "Advance Cheques Help",
                content: @"The Advance Cheques screen allows you to create, view, and manage advance payments issued to growers. These advances are automatically deducted from future payments.

**Key Features:**

• **Create Advance Cheques**: Issue advance payments to growers for various reasons such as equipment purchases, fuel costs, or emergency needs.

• **View Outstanding Advances**: See all active advance cheques that haven't been deducted yet.

• **Track Deductions**: Monitor when and how much has been deducted from advance cheques.

• **Cancel Advances**: Cancel advance cheques that haven't been deducted yet.

• **Search & Filter**: 
  - Search by grower name, number, or reason
  - Filter by status (Active, Deducted, Cancelled)
  - Filter by date range
  - Real-time filtering as you type

**Advance Cheque Fields:**

• **Grower**: Select the grower receiving the advance
• **Amount**: The advance amount (must be greater than 0)
• **Reason**: Description of why the advance is being issued
• **Date**: When the advance was created
• **Status**: Current status (Active, Deducted, Cancelled)

**Status Meanings:**

• **Active**: Advance is outstanding and will be deducted from next payment
• **Deducted**: Advance has been fully deducted from grower payments
• **Cancelled**: Advance was cancelled before being deducted

**Automatic Deduction Process:**

• When a grower receives a regular payment, any outstanding advances are automatically deducted
• Deductions are applied in chronological order (oldest advances first)
• If the payment amount is less than the outstanding advances, partial deductions are made
• The system tracks all deductions for audit purposes

**Best Practices:**

• Use clear, descriptive reasons for advances
• Monitor outstanding advances regularly
• Consider the grower's payment history before issuing large advances
• Keep advance amounts reasonable relative to expected payments
• Document the business need for each advance

**Workflow Steps:**

1. **Select Grower**: Choose the grower from the dropdown list
2. **Enter Amount**: Specify the advance amount
3. **Add Reason**: Provide a clear reason for the advance
4. **Create Advance**: Click 'Create Advance' to issue the advance
5. **Monitor Status**: Track the advance status and deductions
6. **Cancel if Needed**: Cancel advances that are no longer needed

**Statistics Display:**

• **Total Outstanding**: Sum of all active advance amounts
• **Active Cheques**: Number of outstanding advances
• **Deducted This Month**: Total amount deducted in the current month",
                
                quickTips: @"• Double-click any advance cheque to view details
• Use the search box to quickly find specific advances
• Check outstanding advances before issuing new ones
• Advances are automatically deducted from next payments
• Use descriptive reasons for better tracking
• Monitor the statistics cards for system overview
• Export data for reporting and analysis",
                
                keyboardShortcuts: "F1 - Show Help\nF5 - Refresh Data\nEsc - Close Dialog\nTab - Navigate between fields"
            );

            // Enhanced Payment Distribution Help
            _helpContents["EnhancedPaymentDistributionView"] = new HelpContent(
                title: "Enhanced Payment Distribution Help",
                content: @"The Enhanced Payment Distribution screen allows you to manage payment distribution with advanced consolidation features. You can choose between regular batch payments and consolidated payments across multiple batches.

**Key Features:**

• **Payment Method Selection**: Choose between regular batch payments and consolidated payments for each grower.

• **Batch Selection**: Select multiple payment batches for consolidation opportunities.

• **Grower Payment Selections**: View and manage payment selections for each grower across selected batches.

• **Consolidation Preview**: Preview consolidated payments before generating cheques.

• **Hybrid Payment Processing**: Mix regular and consolidated payments within the same set of batches.

• **Search & Filter**: 
  - Search by grower name or batch number
  - Filter by view mode (By Grower, By Batch)
  - Filter by payment method
  - Real-time filtering as you type

**Payment Types:**

• **Regular Batch Payment**: Standard payment within a single batch
• **Consolidated Payment**: Payment combining amounts from multiple batches for a single grower

**Workflow Steps:**

1. **Select Batches**: Choose the payment batches to work with
2. **Review Growers**: See all growers appearing in the selected batches
3. **Choose Payment Method**: Select regular or consolidated payment for each grower
4. **Preview Distribution**: Review the payment distribution before generating
5. **Generate Cheques**: Create cheques based on the selected payment methods

**Consolidation Benefits:**

• **Reduced Cheque Count**: Fewer cheques to print and manage
• **Simplified Tracking**: Single payment per grower across multiple batches
• **Cost Savings**: Reduced printing and processing costs
• **Better Cash Flow**: Consolidated payments for growers

**Statistics Display:**

• **Available Batches**: Number of draft batches available for processing
• **Total Growers**: Number of growers in the selected batches
• **Total Amount**: Sum of all payment amounts
• **Consolidation Opportunities**: Number of growers that can be consolidated

**Best Practices:**

• Review consolidation opportunities before making selections
• Consider grower preferences for payment method
• Use consolidation for growers with multiple small payments
• Keep regular batch payments for growers with single large payments
• Monitor outstanding advances when making payment decisions

**Advanced Features:**

• **Automatic Deduction**: Outstanding advances are automatically deducted from payments
• **Payment Method Recommendations**: System suggests optimal payment method per grower
• **Batch Status Management**: Tracks batch status changes after consolidation
• **Audit Trail**: Complete tracking of all payment decisions and changes",
                
                quickTips: @"• Use the search box to quickly find specific growers or batches
• Check consolidation opportunities to reduce cheque count
• Preview consolidated payments before generating cheques
• Consider outstanding advances when selecting payment methods
• Use the statistics cards to monitor system overview
• Export data for reporting and analysis
• Enable consolidation to see consolidation opportunities
• Review payment method recommendations for each grower",
                
                keyboardShortcuts: "F1 - Show Help\nF5 - Refresh Data\nEsc - Close Dialog\nTab - Navigate between fields"
            );

            // Enhanced Cheque Preparation Help
            _helpContents["EnhancedChequePreparationView"] = new HelpContent(
                title: "Enhanced Cheque Preparation Help",
                content: @"The Enhanced Cheque Preparation screen allows you to manage and prepare all types of cheques (regular, advance, and consolidated) in a unified interface. You can print, preview, void, and manage cheques across all payment types.

**Key Features:**

• **Unified Cheque Management**: View and manage regular, advance, and consolidated cheques in one place.

• **Advanced Filtering**: Filter cheques by type, status, grower, date range, and more.

• **Bulk Operations**: Select multiple cheques for batch operations like printing, voiding, or exporting.

• **Type-Specific Actions**: Different actions available based on cheque type and status.

• **Grouping Options**: Group cheques by type for better organization.

• **Comprehensive Statistics**: View statistics for each cheque type and total amounts.

**Cheque Types:**

• **Regular Cheques**: Standard batch payment cheques
• **Advance Cheques**: Advance payment cheques issued to growers
• **Consolidated Cheques**: Cheques combining payments from multiple batches

**Available Actions:**

• **Print Selected**: Print multiple selected cheques
• **Preview Selected**: Preview multiple selected cheques
• **Preview Single**: Preview a single selected cheque
• **Generate PDF**: Generate PDF files for selected cheques
• **Void Selected**: Void multiple selected cheques
• **Void Single**: Void a single selected cheque
• **Stop Payment**: Stop payment on selected cheques
• **Reprint Selected**: Reprint selected cheques
• **Export Data**: Export cheque data for reporting

**Filtering Options:**

• **Search**: Search by cheque number, grower name, or amount
• **Cheque Number**: Filter by specific cheque number
• **Grower Number**: Filter by grower number
• **Status**: Filter by cheque status (Generated, Printed, Delivered, Voided)
• **Type**: Filter by payment type (Regular, Advance, Consolidated)
• **Date Range**: Filter by cheque date range

**Statistics Display:**

• **Total Cheques**: Total number of cheques in the system
• **Regular Cheques**: Count and amount of regular cheques
• **Advance Cheques**: Count and amount of advance cheques
• **Consolidated Cheques**: Count and amount of consolidated cheques
• **Total Amount**: Sum of all cheque amounts

**Grouping Features:**

• **Group by Type**: Organize cheques by payment type
• **Ungroup**: Return to flat list view
• **Type Icons**: Visual indicators for each cheque type

**Workflow Steps:**

1. **Filter Cheques**: Use search and filter options to find specific cheques
2. **Select Cheques**: Choose cheques for batch operations
3. **Preview**: Preview cheques before printing
4. **Print**: Print selected cheques
5. **Manage**: Void, stop payment, or reprint as needed
6. **Export**: Export data for reporting and analysis

**Best Practices:**

• Preview cheques before printing to ensure accuracy
• Use bulk operations for efficiency when processing multiple cheques
• Group by type to better organize different cheque types
• Monitor statistics to track system activity
• Use appropriate filters to focus on specific cheque sets
• Export data regularly for backup and reporting purposes

**Type-Specific Information:**

• **Regular Cheques**: Show deduction details if advances were deducted
• **Advance Cheques**: Display advance reason and status
• **Consolidated Cheques**: Show source batches and breakdown

**Status Management:**

• **Generated**: Cheques ready for printing
• **Printed**: Cheques that have been printed
• **Delivered**: Cheques that have been delivered to growers
• **Voided**: Cheques that have been voided

**Advanced Features:**

• **Automatic Deduction Display**: Shows advance deductions on regular cheques
• **Source Batch Tracking**: Tracks which batches were consolidated
• **Audit Trail**: Complete tracking of all cheque operations
• **Performance Optimization**: Virtualization for large cheque lists
• **Keyboard Shortcuts**: F1 for help, F5 for refresh",
                
                quickTips: @"• Use the search box to quickly find specific cheques
• Group by type to organize different cheque types
• Preview cheques before printing to ensure accuracy
• Use bulk operations for efficiency with multiple cheques
• Check statistics cards for system overview
• Export data regularly for backup and reporting
• Use appropriate filters to focus on specific cheque sets
• Monitor deduction details on regular cheques
• Track source batches on consolidated cheques
• Use keyboard shortcuts for faster navigation",
                
                keyboardShortcuts: "F1 - Show Help\nF5 - Refresh Data\nEsc - Close Dialog\nTab - Navigate between fields"
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

