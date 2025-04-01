using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using WPFGrowerApp.Controls;
using WPFGrowerApp.Services;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Views;

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
            InitializeComponent();
            
            // ServiceConfiguration calls removed - DI container handles this

            // Set DataContext to the injected ViewModel
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // Event subscription removed - Navigation is now handled by MainViewModel commands
        }

        // MainMenu_MenuItemClicked event handler removed

        private void MenuToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle the hamburger menu with animation
            MainMenu.ToggleMenu();
            MainMenu.IsMenuExpanded = !MainMenu.IsMenuExpanded;

            if (MainMenu.IsMenuExpanded)
            {
                // Expand menu
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 250,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                MainMenu.BeginAnimation(FrameworkElement.WidthProperty, animation);
            }
            else
            {
                // Collapse menu
                var animation = new DoubleAnimation
                {
                    From = 250,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                MainMenu.BeginAnimation(FrameworkElement.WidthProperty, animation);
            }
        }

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
