using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converts boolean values to brush colors
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Brushes.LightGreen : Brushes.LightCoral;
            }
            return Brushes.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
