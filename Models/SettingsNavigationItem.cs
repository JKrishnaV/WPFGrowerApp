using System;
using MaterialDesignThemes.Wpf; // Added for PackIconKind

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Represents an item in the settings navigation list.
    /// </summary>
    public class SettingsNavigationItem
    {
        /// <summary>
        /// The display name shown in the navigation list.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The type of the ViewModel associated with this setting.
        /// </summary>
        public Type ViewModelType { get; }

        /// <summary>
        /// The Material Design icon kind for this setting item.
        /// </summary>
        public PackIconKind IconKind { get; } // Added IconKind property

        // Updated constructor to include IconKind
        public SettingsNavigationItem(string displayName, Type viewModelType, PackIconKind iconKind)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ViewModelType = viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
            IconKind = iconKind; // Assign IconKind
        }
    }
}
