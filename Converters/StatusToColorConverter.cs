using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converter that converts a status string to a background color brush
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "active" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                    "inactive" => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // Gray
                    "hold" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                    "pending" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // Default Gray
                };
            }
            
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // Default Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
