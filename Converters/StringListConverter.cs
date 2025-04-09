using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converts a List<string> to a comma-separated string for display.
    /// Returns a fallback value if the list is null or empty.
    /// </summary>
    public class StringListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<string> stringList)
            {
                var list = stringList.ToList();
                if (list.Any())
                {
                    return string.Join(", ", list);
                }
            }

            // Fallback value if list is null, empty, or not a list of strings
            return parameter ?? "N/A"; // Use parameter as fallback, default to "N/A"
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Not needed for one-way binding
            throw new NotImplementedException();
        }
    }
}
