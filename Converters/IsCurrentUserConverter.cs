using System;
using System.Globalization;
using System.Windows.Data;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converts a User to a boolean indicating if they are the currently logged-in user.
    /// Returns true if the user is the current user, false otherwise.
    /// </summary>
    public class IsCurrentUserConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is User user && App.CurrentUser != null)
            {
                // Compare by UserId for more reliable comparison
                if (user.UserId > 0 && user.UserId == App.CurrentUser.UserId)
                {
                    return true;
                }
                
                // Fallback to username comparison if UserId is not set
                if (!string.IsNullOrEmpty(user.Username) && !string.IsNullOrEmpty(App.CurrentUser.Username))
                {
                    return user.Username.Equals(App.CurrentUser.Username, StringComparison.OrdinalIgnoreCase);
                }
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("IsCurrentUserConverter is a one-way converter.");
        }
    }

    /// <summary>
    /// Inverse version - returns true if the user is NOT the currently logged-in user.
    /// Useful for enabling/disabling delete buttons.
    /// </summary>
    public class IsNotCurrentUserConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is User user && App.CurrentUser != null)
            {
                // Compare by UserId for more reliable comparison
                if (user.UserId > 0 && user.UserId == App.CurrentUser.UserId)
                {
                    return false;
                }
                
                // Fallback to username comparison if UserId is not set
                if (!string.IsNullOrEmpty(user.Username) && !string.IsNullOrEmpty(App.CurrentUser.Username))
                {
                    return !user.Username.Equals(App.CurrentUser.Username, StringComparison.OrdinalIgnoreCase);
                }
            }
            
            return true; // If we can't determine, allow the action
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("IsNotCurrentUserConverter is a one-way converter.");
        }
    }
}

