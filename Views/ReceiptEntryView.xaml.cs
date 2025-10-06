using System.Windows;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    public partial class ReceiptEntryView : Window
    {
        public ReceiptEntryView()
        {
            InitializeComponent();
            
            // Subscribe to DataContextChanged to handle when ViewModel is set
            DataContextChanged += ReceiptEntryView_DataContextChanged;
        }

        private void ReceiptEntryView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            // Handle dialog result from ViewModel
            if (e.NewValue is ReceiptEntryViewModel viewModel)
            {
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(ReceiptEntryViewModel.DialogResult))
                    {
                        // Setting DialogResult will automatically close the window
                        DialogResult = viewModel.DialogResult;
                    }
                };
            }
        }
    }
}
