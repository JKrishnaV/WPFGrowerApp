using System.Windows;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ContainerEntryWindow.xaml
    /// </summary>
    public partial class ContainerEntryWindow : Window
    {
        public ContainerEntryWindow()
        {
            InitializeComponent();

            // Subscribe to RequestClose event from ViewModel
            Loaded += (s, e) =>
            {
                if (DataContext is ContainerEntryViewModel viewModel)
                {
                    viewModel.RequestClose += (dialogResult) =>
                    {
                        DialogResult = dialogResult;
                        Close();
                    };
                }
            };
        }
    }
}
