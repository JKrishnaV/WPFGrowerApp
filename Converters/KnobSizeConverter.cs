using System;
using System.Globalization;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    public class KnobSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                // Ensure the knob size is always positive and slightly smaller than the height
                return Math.Max(0, height - 3);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}