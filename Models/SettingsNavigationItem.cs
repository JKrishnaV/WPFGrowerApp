using System;

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

        public SettingsNavigationItem(string displayName, Type viewModelType)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            ViewModelType = viewModelType ?? throw new ArgumentNullException(nameof(viewModelType));
        }
    }
}
