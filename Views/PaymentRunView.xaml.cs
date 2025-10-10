using System.Windows.Controls;
using System.Windows.Input; // Added for MouseButtonEventArgs
using System.Windows.Media; // Added for VisualTreeHelper
using System.Windows; // Added for DependencyObject
using WPFGrowerApp.Helpers;
using System.Linq; // Added for Cast and ToList
using WPFGrowerApp.ViewModels; // Added for PaymentRunViewModel

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PaymentRunView.xaml
    /// </summary>
    public partial class PaymentRunView : UserControl
    {
        public PaymentRunView()
        {
            InitializeComponent(); // Restored InitializeComponent call
            ThemeHelper.EnableThemeSupport(this);
        }

        // Event handler to allow deselecting the last item in a multi-select ListBox
        // Note: This handler is now attached to the ListBox itself in XAML,
        // but the event source (sender) will be the original element (e.g., TextBlock within ListBoxItem).
        // We need to find the ListBoxItem first.
        private void ListBoxItem_PreviewMouseLeftButtonDown_Toggle(object sender, MouseButtonEventArgs e)
        {
            // Find the ListBoxItem that was clicked
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is ListBoxItem))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null) return; // Click was not on a ListBoxItem container

            ListBoxItem item = (ListBoxItem)dep;

            // Find the parent ListBox
            var listBox = FindParent<ListBox>(item);
            if (listBox != null && listBox.SelectionMode == SelectionMode.Extended) // Check if multi-select is enabled
            {
                // If the clicked item is the only selected item, deselect it
                if (item.IsSelected && listBox.SelectedItems.Count == 1)
                {
                    listBox.UnselectAll();
                    e.Handled = true; // Prevent the default selection behavior
                }
                // Let the default behavior handle adding/removing selections otherwise
            }
        }

        // Helper method to find an ancestor of a specific type
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return default(T);

            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        // Handle selection changes in Run Log
        private void RunLogListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the ViewModel with selected items
            var viewModel = DataContext as PaymentRunViewModel;
            if (viewModel != null)
            {
                if (RunLogListBox.SelectedItems.Count > 0)
                {
                    viewModel.SelectedRunLogItems = RunLogListBox.SelectedItems.Cast<string>().ToList();
                }
                else
                {
                    viewModel.SelectedRunLogItems.Clear();
                }
            }
        }
    }
}
