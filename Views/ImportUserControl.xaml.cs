using System.Windows.Controls;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ImportUserControl.xaml
    /// </summary>
    public partial class ImportUserControl : UserControl
    {
        public ImportUserControl()
        {
            InitializeComponent();
            DataContext = ServiceConfiguration.GetService<ImportViewModel>();
        }
    }
}
