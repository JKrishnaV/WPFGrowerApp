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
        // All properties, fields, and methods related to 
        // IsMenuVisible, IsMenuExpanded, Storyboards, and ToggleMenu 
        // have been removed. The menu's visibility/width is now controlled 
        // by the MainViewModel's IsMenuOpen property via data binding in MainWindow.xaml.

        public HamburgerMenu()
        {
            InitializeComponent();
        }
    }
}
