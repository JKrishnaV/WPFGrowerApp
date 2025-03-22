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
