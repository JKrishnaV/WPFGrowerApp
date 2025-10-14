using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for DepotView.xaml
    /// </summary>
    public partial class DepotView : UserControl
    {
        public DepotView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Auto-focus for keyboard shortcuts
            Loaded += (s, e) => Focus();
        }

        private void DepotsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.DepotViewModel viewModel)
            {
                viewModel.ViewDepotCommand?.Execute(viewModel.SelectedDepot);
            }
        }

        private void ViewDepot_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DataAccess.Models.Depot depot)
            {
                if (DataContext is ViewModels.DepotViewModel viewModel)
                {
                    viewModel.ViewDepotCommand?.Execute(depot);
                }
            }
        }

        private void EditDepot_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DataAccess.Models.Depot depot)
            {
                if (DataContext is ViewModels.DepotViewModel viewModel)
                {
                    viewModel.EditDepotCommand?.Execute(depot);
                }
            }
        }

        private void DeleteDepot_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DataAccess.Models.Depot depot)
            {
                if (DataContext is ViewModels.DepotViewModel viewModel)
                {
                    viewModel.DeleteDepotCommand?.Execute(depot);
                }
            }
        }
    }
}