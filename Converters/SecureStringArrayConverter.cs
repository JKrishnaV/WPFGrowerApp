using System;
using System.Globalization;
using System.Security;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converts multiple SecureString inputs (from PasswordBoxes) into an object array 
    /// suitable for passing as a CommandParameter via MultiBinding.
    /// </summary>
    public class SecureStringArrayConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // The values array will contain the SecureString from each PasswordBox binding
            // We just return the array as is, the ViewModel command will handle it.
            // We could potentially clone the SecureStrings here if needed, but passing
            // the original references is usually fine for immediate command execution.
            return values.Clone(); // Clone to avoid potential issues with the original array being modified
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            // Not needed for one-way binding to CommandParameter
            throw new NotImplementedException();
        }
    }
}
