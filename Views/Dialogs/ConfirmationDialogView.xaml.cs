using System.Windows.Controls;
using System.Windows; // Required for Visibility

namespace WPFGrowerApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ConfirmationDialogView.xaml
    /// </summary>
    public partial class ConfirmationDialogView : UserControl
    {
        public ConfirmationDialogView()
        {
            InitializeComponent();
        }

        public void SetContent(string message, string title = null)
        {
            MessageTextBlock.Text = message;
            if (!string.IsNullOrEmpty(title))
            {
                TitleTextBlock.Text = title;
                TitleTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                TitleTextBlock.Visibility = Visibility.Collapsed;
            }
        }
    }
}
