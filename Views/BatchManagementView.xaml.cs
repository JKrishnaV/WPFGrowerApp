using System;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for BatchManagementView.xaml
    /// </summary>
    public partial class BatchManagementView : UserControl
    {
        private BatchManagementViewModel _viewModel;

        public BatchManagementView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // CRITICAL: Auto-focus for immediate keyboard shortcuts
            Loaded += (s, e) => Focus();
        }

        private void BatchManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as BatchManagementViewModel;
            if (_viewModel == null)
            {
                MessageBox.Show("ViewModel is not set correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
    }
}
