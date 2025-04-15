using System.Windows; // Added for DependencyObject
using System.Windows.Controls;
using System.Windows.Input; // Added for MouseButtonEventArgs
using System.Windows.Media; // Added for VisualTreeHelper
using WPFGrowerApp.ViewModels;
using BoldReports.UI.Xaml; // For ReportViewer
// Note: Correct namespace for ReportExportEventArgs needs verification based on installed BoldReports version.
// using BoldReports.Windows; // Or other BoldReports namespace?

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerReportView.xaml
    /// </summary>
    public partial class GrowerReportView : UserControl
    {
        public GrowerReportView()
        {
            InitializeComponent();
            // Removed DataContextChanged handler setup
        }

        // Removed GrowerReportView_DataContextChanged handler

        private void ReportViewer_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Ensure DataContext and sender are valid
            if (DataContext is GrowerReportViewModel viewModel && sender is ReportViewer reportViewer)
            {
                viewModel.SetReportViewer(reportViewer); // Pass the viewer instance
                _ = viewModel.LoadReportDataAsync(); // Trigger initial data load (fire-and-forget)
            }
            else
            {
                // Log or handle the case where DataContext is not the expected ViewModel
                // or sender is not the ReportViewer (shouldn't normally happen for this event)
                System.Diagnostics.Debug.WriteLine("ReportViewer_Loaded: DataContext is not GrowerReportViewModel or sender is not ReportViewer.");
            }
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
                    // Instead of UnselectAll, toggle the specific item's selection
                    item.IsSelected = false;
                    e.Handled = true; // Prevent the default selection behavior
                }
                else if (item.IsSelected)
                {
                     // If already selected (and not the only one), deselect it
                     item.IsSelected = false;
                     e.Handled = true;
                }
                // If not selected, let the default behavior handle selecting it.
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

        // Removed ReportViewer_ReportExportBegin and ReportViewer_ReportExportEnd event handlers
        // as the events are not available in WPF and we are using a custom export button.
     }
 }
