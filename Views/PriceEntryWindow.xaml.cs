using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PriceEntryWindow.xaml
    /// </summary>
    public partial class PriceEntryWindow : Window
    {
        public PriceEntryWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set focus to first input when window loads
            if (DataContext is PriceEntryViewModel viewModel)
            {
                // Subscribe to property changes to close window on save
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(PriceEntryViewModel.DialogResult))
                    {
                        if (viewModel.DialogResult)
                        {
                            DialogResult = true;
                            Close();
                        }
                        else
                        {
                            DialogResult = false;
                            Close();
                        }
                    }
                };
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TextBox_PreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox && DataContext is PriceEntryViewModel viewModel)
            {
                // Determine which level/grade this TextBox belongs to based on its binding
                var bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
                if (bindingExpression?.ResolvedSourcePropertyName != null)
                {
                    var propertyName = bindingExpression.ResolvedSourcePropertyName;
                    
                    // Call the appropriate validation method based on the property name
                    if (propertyName.StartsWith("CL1G1"))
                        viewModel.ValidateL1G1();
                    else if (propertyName.StartsWith("CL1G2"))
                        viewModel.ValidateL1G2();
                    else if (propertyName.StartsWith("CL1G3"))
                        viewModel.ValidateL1G3();
                    else if (propertyName.StartsWith("CL2G1"))
                        viewModel.ValidateL2G1();
                    else if (propertyName.StartsWith("CL2G2"))
                        viewModel.ValidateL2G2();
                    else if (propertyName.StartsWith("CL2G3"))
                        viewModel.ValidateL2G3();
                    else if (propertyName.StartsWith("CL3G1"))
                        viewModel.ValidateL3G1();
                    else if (propertyName.StartsWith("CL3G2"))
                        viewModel.ValidateL3G2();
                    else if (propertyName.StartsWith("CL3G3"))
                        viewModel.ValidateL3G3();
                }
            }
        }
    }
}
