using System.Windows.Controls;
using System.Windows;

namespace WPFGrowerApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for HelpDialogView.xaml
    /// </summary>
    public partial class HelpDialogView : UserControl
    {
        public HelpDialogView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the content for the help dialog
        /// </summary>
        /// <param name="content">The help content to display</param>
        /// <param name="title">The title of the help dialog</param>
        public void SetContent(string content, string title = "Help")
        {
            ContentTextBlock.Text = content;
            TitleTextBlock.Text = title;
        }
    }
}