using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converts a string value to Visibility.Visible if it's not null or empty,
    /// otherwise Visibility.Collapsed.
    /// </summary>
    public class NotNullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for one-way binding
            throw new NotImplementedException();
        }
    }
}
