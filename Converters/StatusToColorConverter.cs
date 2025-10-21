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
                    // Unified status values for both regular and advance cheques
                    "generated" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                    "printed" => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
                    "delivered" => new SolidColorBrush(Color.FromRgb(139, 195, 74)), // Light Green
                    "voided" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                    "stopped" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                    // Legacy status values for backward compatibility
                    "active" => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                    "deducted" => new SolidColorBrush(Color.FromRgb(139, 195, 74)), // Light Green
                    "cancelled" => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
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
