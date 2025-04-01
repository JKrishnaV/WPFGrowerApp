using System.Windows.Controls;
using System.Windows.Controls;
using WPFGrowerApp.ViewModels;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ImportUserControl.xaml
    /// </summary>
    public partial class ImportUserControl : UserControl
    {
        // Parameterless constructor is required for instantiation via DataTemplate
        public ImportUserControl()
        {
            InitializeComponent();
            // DataContext will be set automatically by WPF based on the DataTemplate
            // in ViewMappings.xaml when the bound content is an ImportViewModel.
        }
    }
}
