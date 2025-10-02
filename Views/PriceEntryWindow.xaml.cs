using System.Windows;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PriceEntryWindow.xaml
    /// </summary>
    public partial class PriceEntryWindow : Window
    {
        public PriceEntryWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set focus to first input when window loads
            if (DataContext is PriceEntryViewModel viewModel)
            {
                // Subscribe to property changes to close window on save
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(PriceEntryViewModel.DialogResult))
                    {
                        if (viewModel.DialogResult)
                        {
                            DialogResult = true;
                            Close();
                        }
                        else
                        {
                            DialogResult = false;
                            Close();
                        }
                    }
                };
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
