using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPFGrowerApp.Converters
{
    public class CollectionCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICollection collection)
            {
                return collection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (value is IEnumerable enumerable)
            {
                // Check if IEnumerable has any elements without iterating the whole thing if possible
                var enumerator = enumerable.GetEnumerator();
                bool hasItems = enumerator.MoveNext(); 
                // Dispose enumerator if it's disposable (important for some enumerables like database readers)
                (enumerator as IDisposable)?.Dispose(); 
                return hasItems ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed; // Default to collapsed if value is not a collection or enumerable
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException(); // Not needed for one-way binding
        }
    }
}
