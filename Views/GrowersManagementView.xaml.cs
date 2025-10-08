using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowersManagementView.xaml
    /// </summary>
    public partial class GrowersManagementView : UserControl
    {
        public GrowersManagementView()
        {
            InitializeComponent();
        }

        private void GrowersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Handle double-click to view grower details
            if (GrowersDataGrid.SelectedItem != null)
            {
                // This will trigger the ViewGrowerDetailsCommand in the ViewModel
                // The command binding will handle the navigation
            }
        }
    }
}
