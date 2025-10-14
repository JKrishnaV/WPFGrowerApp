using System;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PaymentGroupView.xaml
    /// </summary>
    public partial class PaymentGroupView : UserControl
    {
        private PaymentGroupViewModel _viewModel;

        public PaymentGroupView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            this.Loaded += PaymentGroupView_Loaded;
        }

        private void PaymentGroupView_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as PaymentGroupViewModel;
            if (_viewModel == null)
            {
                MessageBox.Show("ViewModel is not set correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void PayGroupsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_viewModel?.SelectedPayGroup != null && _viewModel.ViewPayGroupCommand.CanExecute(_viewModel.SelectedPayGroup))
            {
                _viewModel.ViewPayGroupCommand.Execute(_viewModel.SelectedPayGroup);
            }
        }

        private async void ViewPayGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PayGroup payGroup)
            {
                if (_viewModel?.ViewPayGroupCommand.CanExecute(payGroup) == true)
                {
                    _viewModel.ViewPayGroupCommand.Execute(payGroup);
                }
            }
        }

        private async void EditPayGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PayGroup payGroup)
            {
                if (_viewModel?.EditPayGroupCommand.CanExecute(payGroup) == true)
                {
                    _viewModel.EditPayGroupCommand.Execute(payGroup);
                }
            }
        }

        private async void DeletePayGroup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PayGroup payGroup)
            {
                if (_viewModel?.DeletePayGroupCommand.CanExecute(payGroup) == true)
                {
                    _viewModel.DeletePayGroupCommand.Execute(payGroup);
                }
            }
        }
    }
}