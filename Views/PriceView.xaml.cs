using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.Models;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    public partial class PriceView : UserControl
    {
        public PriceView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Auto-focus for immediate keyboard shortcuts (F1, F5)
            Loaded += (s, e) => Focus();
        }

        private void ViewPrice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PriceDisplayItem priceItem)
            {
                var viewModel = DataContext as PriceViewModel;
                viewModel?.ViewPriceCommand?.Execute(priceItem);
            }
        }

        private void EditPrice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PriceDisplayItem priceItem)
            {
                var viewModel = DataContext as PriceViewModel;
                viewModel?.EditPriceCommand?.Execute(priceItem);
            }
        }

        private void DeletePrice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PriceDisplayItem priceItem)
            {
                var viewModel = DataContext as PriceViewModel;
                viewModel?.DeletePriceCommand?.Execute(priceItem);
            }
        }

        private void PricesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as PriceViewModel;
            if (viewModel?.SelectedPrice != null)
            {
                viewModel.ViewPriceCommand?.Execute(viewModel.SelectedPrice);
            }
        }
    }
}
