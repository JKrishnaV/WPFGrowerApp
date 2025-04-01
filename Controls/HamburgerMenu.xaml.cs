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
        // MenuItemClicked event removed - Navigation handled by ViewModel Commands

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

        // MenuButton_Click event handler removed
    }

    // MenuItemClickedEventArgs class removed
}
