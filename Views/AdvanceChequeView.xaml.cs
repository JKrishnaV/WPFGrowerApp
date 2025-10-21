using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for AdvanceChequeView.xaml
    /// </summary>
    public partial class AdvanceChequeView : UserControl
    {
        public AdvanceChequeView()
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
                    if (DataContext is AdvanceChequeViewModel viewModel)
                    {
                        viewModel.ShowHelpCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
                
                case Key.F5:
                    if (DataContext is AdvanceChequeViewModel viewModel2)
                    {
                        viewModel2.RefreshCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;
            }
        }
    }
}
