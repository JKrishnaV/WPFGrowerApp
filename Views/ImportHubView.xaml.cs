using System;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ImportHubView.xaml
    /// </summary>
    public partial class ImportHubView : UserControl
    {
        private ImportHubViewModel _viewModel;

        public ImportHubView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // CRITICAL: Auto-focus for immediate keyboard shortcuts
            Loaded += (s, e) => Focus();
        }

        private void ImportHubView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as ImportHubViewModel;
            if (_viewModel == null)
            {
                MessageBox.Show("ViewModel is not set correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
    }
}
