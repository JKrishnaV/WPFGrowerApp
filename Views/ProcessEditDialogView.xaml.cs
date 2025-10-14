using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ProcessEditDialogView.xaml
    /// </summary>
    public partial class ProcessEditDialogView : UserControl
    {
        public ProcessEditDialogView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }
    }
}
