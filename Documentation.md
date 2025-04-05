# WPF Grower Application Documentation

## Overview
This WPF application provides a modern interface for managing grower information in a berry farm management system. The application features a hamburger menu navigation system and a detailed Grower information screen that mimics all the fields from the original xBase application but with a modern UI design.

## Project Structure
The application follows the MVVM (Model-View-ViewModel) architecture for clean separation of concerns:

- **Models**: Data models representing business entities
- **Views**: User interface components
- **ViewModels**: Classes that handle the presentation logic and data binding
- **Controls**: Custom UI controls like the hamburger menu
- **Styles**: Application-wide styling resources

## Key Features
1. **Hamburger Menu Navigation**: Easy access to different sections of the application
2. **Grower Information Screen**: Comprehensive form with all fields from the original application
3. **Modern UI**: Material Design-inspired interface with consistent styling
4. **Data Binding**: Full MVVM implementation with property change notifications

## Screens
1. **Dashboard**: Welcome screen with quick access buttons
2. **Grower**: Detailed grower information form with all fields from the original application

## Implementation Details

### Models
- **Grower.cs**: Represents a grower entity with all properties from the original application

### ViewModels
- **MainViewModel.cs**: Handles the main window and navigation logic
- **GrowerViewModel.cs**: Manages the grower information screen data
- **DashboardViewModel.cs**: Handles the dashboard screen data

### Views
- **GrowerView.xaml**: UI for the grower information screen
- **DashboardView.xaml**: UI for the dashboard screen

### Controls
- **HamburgerMenu.xaml**: Custom control for the hamburger menu navigation

### Styling
- **Colors.xaml**: Color definitions for the application
- **Styles.xaml**: Style definitions for UI elements

## Getting Started
1. Open the solution in Visual Studio
2. Build the solution to restore NuGet packages
3. Run the application

## Development Notes
- The application uses .NET Framework and WPF
- The UI is designed to be responsive and adapt to different window sizes
- Sample data is provided for testing purposes

## Future Enhancements
- Add data persistence with a database
- Implement search functionality for growers
- Add reporting features
- Expand to include other aspects of farm management

## Payment Module: Advance Payment Run

This process calculates and posts advance payments (1st, 2nd, or 3rd) based on imported receipt data. It does *not* generate the final cheques; that is a subsequent step.

### Workflow:

1.  **Initiation (UI):**
    *   User navigates to the "Payment Run" view.
    *   User selects the desired **Advance Number** (1, 2, or 3).
    *   User sets the **Payment Date** (date for accounting entries) and **Cutoff Date** (receipts up to this date are included).
    *   User confirms the **Crop Year**.
    *   User can optionally filter by Grower ID, Pay Group, Product ID, or Process ID.
    *   User clicks the "Start Payment Run" button.

2.  **ViewModel (`PaymentRunViewModel`):**
    *   The `StartPaymentRunCommand` executes the `StartPaymentRunAsync` method.
    *   UI is updated to show "Running" status.
    *   Parameters are gathered from the UI.
    *   Calls `IPaymentService.ProcessAdvancePaymentRunAsync`.

3.  **Service Layer (`PaymentService`):**
    *   Calls `IPostBatchService` to create a new `PostBat` record and get a unique batch ID.
    *   Calls `IReceiptService.GetReceiptsForAdvancePaymentAsync` to fetch eligible `Daily` records based on:
        *   `CutoffDate`.
        *   Advance number (checking `POST_BAT1/2/3` is 0).
        *   `FIN_BAT = 0`.
        *   `ISVOID = 0`.
        *   `Grower.ONHOLD = 0`.
        *   Any user-specified filters.
    *   Groups fetched receipts by Grower Number.
    *   Loops through each grower:
        *   Fetches grower details (`IGrowerService`).
        *   Loops through each eligible receipt for the grower:
            *   Finds the relevant price record ID (`IPriceService.FindPriceRecordIdAsync`).
            *   Gets the base advance price for the current advance number (`IPriceService.GetAdvancePriceAsync`).
            *   Calculates the net advance amount to pay for *this* advance by subtracting previously paid amounts (`ADV_PR1`, `ADV_PR2` - requires fetching these from the `Daily` record via `IReceiptService`).
            *   If Advance 1, calculates time premium (`IPriceService.GetTimePremiumAsync`) and marketing deduction (`IPriceService.GetMarketingDeductionAsync`).
            *   Creates in-memory `Account` objects for the net advance, premium (if Adv 1), and deduction (if Adv 1), assigning the batch ID.
            *   Updates the `Daily` record via `IReceiptService.UpdateReceiptAdvanceDetailsAsync`, setting `ADV_PRN`, `ADV_PRIDN`, `POST_BATN`, `LAST_ADVPB`, and `PREM_PRICE` (if Adv 1).
            *   Marks the price record as used via `IPriceService.MarkAdvancePriceAsUsedAsync`.
        *   Saves the generated `Account` entries for the grower via `IAccountService.CreatePaymentAccountEntriesAsync`.
    *   Returns success status, errors, and the created `PostBatch` object.

4.  **ViewModel Wrap-up:**
    *   Updates UI with final status message and any errors.
    *   Displays confirmation/error dialog via `IDialogService`.
    *   Resets `IsRunning` state.

### Key Data Flow:

*   Input Parameters (UI) -> `PaymentRunViewModel`
*   `PaymentRunViewModel` -> `IPaymentService.ProcessAdvancePaymentRunAsync`
*   `PaymentService` uses:
    *   `IPostBatchService` (Create `PostBat`)
    *   `IReceiptService` (Get eligible `Daily`, Update `Daily`)
    *   `IGrowerService` (Get grower details)
    *   `IPriceService` (Get prices, Mark used)
    *   `IAccountService` (Create `Account` entries)
*   Result/Errors -> `PaymentRunViewModel` -> UI Display

This process prepares the `Account` table with payable entries, ready for the subsequent Cheque Generation step.
