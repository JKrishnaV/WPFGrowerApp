using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for EnhancedPaymentDistributionView.xaml
    /// </summary>
    public partial class EnhancedPaymentDistributionView : UserControl
    {
        public EnhancedPaymentDistributionView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            
            // Set focus on load
            Loaded += (s, e) => Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F1:
                    if (DataContext is EnhancedPaymentDistributionViewModel viewModel)
                    {
                        viewModel.ShowHelpCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                
                case Key.F5:
                    if (DataContext is EnhancedPaymentDistributionViewModel viewModel2)
                    {
                        viewModel2.RefreshCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
            }
        }

        private void SetToRemaining_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AdvanceDeductionItem deduction)
            {
                deduction.SetToRemaining();
            }
        }

        private void ClearDeduction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is AdvanceDeductionItem deduction)
            {
                deduction.ClearDeduction();
            }
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            // Close the popup when a menu item is clicked
            if (ExportMenuToggle != null)
            {
                ExportMenuToggle.IsChecked = false;
            }
        }
    }
}
