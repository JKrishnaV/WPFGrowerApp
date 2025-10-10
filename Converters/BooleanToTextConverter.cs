using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converts boolean values to text strings.
    /// Supports parameter format: "TrueText|FalseText" (e.g., "Running|Ready")
    /// </summary>
    public class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string paramString && paramString.Contains("|"))
                {
                    var parts = paramString.Split('|');
                    if (parts.Length >= 2)
                    {
                        return boolValue ? parts[0] : parts[1];
                    }
                }
                
                // Default behavior
                return boolValue ? "True" : "False";
            }
            
            return "False";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("BooleanToTextConverter does not support ConvertBack");
        }
    }
}

