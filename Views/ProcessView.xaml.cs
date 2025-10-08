using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ProcessView.xaml
    /// </summary>
    public partial class ProcessView : UserControl
    {
        public ProcessView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }
    }
}
