using System.Windows.Controls;
using System.Windows; // Required for Visibility

namespace WPFGrowerApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for MessageDialogView.xaml
    /// </summary>
    public partial class MessageDialogView : UserControl
    {
        public MessageDialogView()
        {
            InitializeComponent();
        }

        // Dependency Properties or simple properties to set Title and Message
        // For simplicity, we might just set the TextBlocks directly from DialogService
        // Or create a simple ViewModel for this dialog if more complexity is needed.

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
