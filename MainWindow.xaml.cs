using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WPFGrowerApp.Controls;
using WPFGrowerApp.Services;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Views;
using WPFGrowerApp.Infrastructure.Logging; // Added for Logger
using MaterialDesignThemes.Wpf;
using WPFGrowerApp.ViewModels.Dialogs;

namespace WPFGrowerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // No need for a private field if only used in constructor
        // private readonly MainViewModel _viewModel; 

        // Inject MainViewModel via constructor
        public MainWindow(MainViewModel viewModel) 
        {
            Logger.Info("MainWindow constructor starting."); // Added Log
            try
            {
                InitializeComponent();
                Logger.Info("InitializeComponent completed."); // Added Log
            }
            catch (Exception ex)
            {
                Logger.Fatal("Exception during InitializeComponent in MainWindow.", ex); // Added Log
                // Rethrow or handle appropriately - maybe show message box and shutdown?
                MessageBox.Show($"A critical error occurred initializing the main window UI: {ex.Message}", "UI Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(-1);
                throw; // Rethrow to ensure constructor failure is clear if not shutting down
            }
            
            // ServiceConfiguration calls removed - DI container handles this

            // Set DataContext to the injected ViewModel
            Logger.Info("Setting MainWindow DataContext."); // Added Log
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            Logger.Info("MainWindow DataContext set."); // Added Log

            // Removed PropertyChanged subscription - Animation handled in XAML

            // Event subscription removed - Navigation is now handled by MainViewModel commands

            // Add Loaded event handler for further logging
            this.Loaded += MainWindow_Loaded;
            Logger.Info("MainWindow constructor finished."); // Added Log
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("MainWindow Loaded event fired."); // Added Log
            // Add any other checks or logging needed after the window is fully loaded and rendered
        }

        // MainMenu_MenuItemClicked event handler removed

        // MenuToggleButton_Click event handler removed (now handled by Command in ViewModel)

        // Removed MainViewModel_PropertyChanged handler and AnimateMenuColumn method

        // Add these methods to MainWindow.xaml.cs
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeButton.Content = "\uE922"; // Maximize icon
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "\uE923"; // Restore icon
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void HeaderArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                this.DragMove();
            }
        }



    }
}
