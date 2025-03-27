using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WPFGrowerApp.Controls
{
    /// <summary>
    /// Interaction logic for HamburgerMenu.xaml
    /// </summary>
    public partial class HamburgerMenu : UserControl
    {
        // Event that will be raised when a menu item is clicked
        public event EventHandler<MenuItemClickedEventArgs> MenuItemClicked;

        // Storyboards for animations
        private Storyboard _fadeInStoryboard;
        private Storyboard _fadeOutStoryboard;
        
        // Property to track if menu is visible
        private bool _isMenuVisible = true;
        public bool IsMenuVisible
        {
            get { return _isMenuVisible; }
        }
        private bool _isMenuExpanded = true;
        // Property to track if menu is expanded
        public bool IsMenuExpanded
        {
            get { return _isMenuExpanded; }
            set
            {
                _isMenuExpanded = value;
            }
        }
        public HamburgerMenu()
        {
            InitializeComponent();
            
            // Load storyboards from resources
            _fadeInStoryboard = (Storyboard)FindResource("MenuFadeIn");
            _fadeOutStoryboard = (Storyboard)FindResource("MenuFadeOut");
            
            // Set initial opacity to 1
            this.Opacity = 1.0;
        }

        // Method to toggle menu visibility with animation
        public void ToggleMenu()
        {
            if (_isMenuVisible)
            {
                // Start fade out animation
                _fadeOutStoryboard.Begin(this);
            }
            else
            {
                // Start fade in animation
                _fadeInStoryboard.Begin(this);
            }

            _isMenuVisible = !_isMenuVisible;
        }


        // Event handler for menu button clicks
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                this.ToggleMenu();
                _isMenuExpanded = !_isMenuExpanded;

                var animation = new DoubleAnimation
                {
                    From = 250,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                this.BeginAnimation(FrameworkElement.WidthProperty, animation);                

                // Get the menu item name from the button name (remove "Button" suffix)
                string menuItem = button.Name.Replace("Button", "");
                
                // Raise the event
                MenuItemClicked?.Invoke(this, new MenuItemClickedEventArgs(menuItem));
            }
        }
    }

    // Custom event args for menu item clicks
    public class MenuItemClickedEventArgs : EventArgs
    {
        public string MenuItem { get; }

        public MenuItemClickedEventArgs(string menuItem)
        {
            MenuItem = menuItem;
        }
    }
}
