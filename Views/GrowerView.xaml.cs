using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Models;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    public partial class GrowerView : UserControl
    {
        public GrowerView()
        {
            InitializeComponent();
            
            // Add InverseBooleanConverter to resources
            Resources.Add("InverseBooleanConverter", new InverseBooleanConverter());
            
            // Add validation style
            AddValidationStyle();
            
            // Subscribe to the Loaded event to set up validation
            Loaded += GrowerView_Loaded;
        }
        
        private void GrowerView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set up validation for the TextBoxes
            SetupValidation();
        }
        
        private void SetupValidation()
        {
            // Get all TextBoxes in the view
            var textBoxes = FindVisualChildren<TextBox>(this);
            
            foreach (var textBox in textBoxes)
            {
                // Add validation error style
                textBox.Style = Resources["ValidationTextBoxStyle"] as Style;
            }
        }
        
        private void AddValidationStyle()
        {
            // Create a style for TextBoxes with validation
            var style = new Style(typeof(TextBox), Application.Current.Resources["ModernTextBoxStyle"] as Style);
            
            // Add a trigger for validation errors
            var trigger = new Trigger
            {
                Property = Validation.HasErrorProperty,
                Value = true
            };
            
            // Set the border brush to red when there's an error
            trigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Colors.Red)));
            
            // Add a tooltip with the error message
            var tooltipSetter = new Setter(
                ToolTipProperty,
                new Binding("(Validation.Errors)[0].ErrorContent") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) }
            );
            trigger.Setters.Add(tooltipSetter);
            
            // Add the trigger to the style
            style.Triggers.Add(trigger);
            
            // Add the style to the resources
            Resources.Add("ValidationTextBoxStyle", style);
        }
        
        // Helper method to find all controls of a specific type
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
    
    // Converter to invert boolean values for radio buttons
    public class InverseBooleanConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
}
