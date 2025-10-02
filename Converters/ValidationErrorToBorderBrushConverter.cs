using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converter that returns a red brush when validation error is true, otherwise transparent
    /// </summary>
    public class ValidationErrorToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool hasError && hasError)
            {
                return new SolidColorBrush(Colors.Red);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
