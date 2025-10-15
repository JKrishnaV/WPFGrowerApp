using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converter to get validation error message for a specific field from the validation errors dictionary.
    /// </summary>
    public class ValidationErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dictionary<string, string> errors && parameter is string fieldName)
            {
                return errors.ContainsKey(fieldName) ? errors[fieldName] : string.Empty;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to check if a specific field has validation errors.
    /// </summary>
    public class HasValidationErrorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Dictionary<string, string> errors && parameter is string fieldName)
            {
                return errors.ContainsKey(fieldName);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
