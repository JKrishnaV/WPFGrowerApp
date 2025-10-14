using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Views
{
    public partial class ProductView : UserControl
    {
        public ProductView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Set focus to the UserControl when loaded so keyboard shortcuts work
            Loaded += (s, e) => Focus();
        }

        private void ProductsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ProductViewModel viewModel)
            {
                viewModel.ViewProductCommand?.Execute(viewModel.SelectedProduct);
            }
        }

        private void ViewProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Product product && DataContext is ProductViewModel viewModel)
            {
                viewModel.SelectedProduct = product;
                viewModel.ViewProductCommand?.Execute(product);
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Product product && DataContext is ProductViewModel viewModel)
            {
                viewModel.SelectedProduct = product;
                viewModel.EditProductCommand?.Execute(product);
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Product product && DataContext is ProductViewModel viewModel)
            {
                viewModel.SelectedProduct = product;
                viewModel.DeleteProductCommand?.Execute(product);
            }
        }
    }
}
