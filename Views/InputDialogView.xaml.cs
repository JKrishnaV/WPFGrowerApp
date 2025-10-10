using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for InputDialogView.xaml
    /// </summary>
    public partial class InputDialogView : UserControl
    {
        public InputDialogView()
        {
            InitializeComponent();
            Loaded += InputDialogView_Loaded;
        }

        private void InputDialogView_Loaded(object sender, RoutedEventArgs e)
        {
            // Focus the input textbox when the dialog loads
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !IsMultiline)
            {
                if (DataContext is InputDialogViewModel viewModel)
                {
                    viewModel.OkCommand.Execute(null);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (DataContext is InputDialogViewModel viewModel)
                {
                    viewModel.CancelCommand.Execute(null);
                }
                e.Handled = true;
            }
        }

        private bool IsMultiline => DataContext is InputDialogViewModel vm && vm.IsMultiline;
    }
}

