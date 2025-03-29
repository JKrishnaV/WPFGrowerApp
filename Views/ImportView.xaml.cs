using System.Windows;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    public partial class ImportView : Window
    {
        public ImportView(ImportViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
} 