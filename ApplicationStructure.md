# WPF Application Structure

## Project Structure
- MainWindow.xaml - Main application window with hamburger menu
- App.xaml - Application definition and resources
- Views/
  - GrowerView.xaml - Grower information screen
  - DashboardView.xaml - Default view when application starts
- ViewModels/
  - MainViewModel.cs - Main window view model
  - GrowerViewModel.cs - Grower screen view model
- Models/
  - Grower.cs - Grower data model
- Styles/
  - Colors.xaml - Color definitions
  - Styles.xaml - Control styles
- Controls/
  - HamburgerMenu.xaml - Custom hamburger menu control

## UI Design Approach
1. Modern Material Design-inspired interface
2. Hamburger menu on the left side
3. Content area on the right to display selected view
4. Responsive layout that adapts to window size
5. Consistent color scheme and typography
6. Form fields organized in logical groups with clear labels
7. Modern input controls while preserving all original fields

## Color Scheme
- Primary: #3F51B5 (Indigo)
- Secondary: #FF4081 (Pink)
- Background: #FFFFFF (White)
- Surface: #F5F5F5 (Light Gray)
- Text: #212121 (Dark Gray)
- Accent: #FFC107 (Amber)

## Typography
- Primary Font: Segoe UI (Windows system font)
- Header Size: 20pt
- Subheader Size: 16pt
- Body Text Size: 12pt
- Label Size: 12pt

This structure follows the MVVM (Model-View-ViewModel) pattern for clean separation of concerns and maintainability.
