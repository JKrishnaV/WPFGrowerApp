using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converts string values to boolean for radio button bindings.
    /// Compares the bound string value with the ConverterParameter.
    /// </summary>
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && parameter is string paramValue)
            {
                return string.Equals(stringValue, paramValue, StringComparison.OrdinalIgnoreCase);
            }
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter is string paramValue)
            {
                return paramValue;
            }
            
            return null;
        }
    }
}

