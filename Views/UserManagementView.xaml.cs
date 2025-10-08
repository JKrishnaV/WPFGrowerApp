using System.Windows.Controls;
using WPFGrowerApp.Helpers;

namespace WPFGrowerApp.Views
{
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
        }
    }
} 