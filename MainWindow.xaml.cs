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
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            // Configure services
            ServiceConfiguration.ConfigureServices();
            
            // Get MainViewModel from service provider
            _viewModel = ServiceConfiguration.GetService<MainViewModel>();
            DataContext = _viewModel;
            
            // Subscribe to menu item clicked event
            MainMenu.MenuItemClicked += MainMenu_MenuItemClicked;
        }

        private void MainMenu_MenuItemClicked(object sender, MenuItemClickedEventArgs e)
        {
            // Handle menu item clicks
            switch (e.MenuItem)
            {
                case "Dashboard":
                    _viewModel.CurrentViewModel = new DashboardViewModel();
                    break;
                case "Growers":
                    // Show grower search dialog before loading grower view
                    var searchView = new GrowerSearchView();
                    if (searchView.ShowDialog() == true && searchView.SelectedGrowerNumber.HasValue)
                    {
                        var growerViewModel = ServiceConfiguration.GetService<GrowerViewModel>();
                        growerViewModel.LoadGrowerAsync(searchView.SelectedGrowerNumber.Value);
                        _viewModel.CurrentViewModel = growerViewModel;
                    }
                    break;
                case "Import":
                    // Show import view
                    var importViewModel = ServiceConfiguration.GetService<ImportViewModel>();
                    _viewModel.CurrentViewModel = importViewModel;
                    break;
                case "Reports":
                    // Show reports view
                    _viewModel.CurrentViewModel = new ReportsViewModel();
                    break;
                // Add other menu items as needed
            }
        }

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
