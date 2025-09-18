using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFGrowerApp.Converters // Assuming converters are in this namespace
{
    /// <summary>
    /// Converts an object comparison to a boolean.
    /// Used for binding RadioButton IsChecked to a selected item in a list.
    /// </summary>
    public class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the bound value equals the converter parameter (the item itself)
            return Equals(value, parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the RadioButton is checked (value is true), return the parameter (the item)
            // Otherwise, return Binding.DoNothing to avoid changing the source unnecessarily
            return value is bool isChecked && isChecked ? parameter : Binding.DoNothing;
        }
    }
}
