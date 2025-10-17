using System.Windows;

namespace WPFGrowerApp.Controls
{
    /// <summary>
    /// Interaction logic for InputBoxDialog.xaml
    /// </summary>
    public partial class InputBoxDialog : Window
    {
        public string Answer { get; set; } = string.Empty;
        public string Title { get; set; } = "Input Required";
        public string Message { get; set; } = "Please enter a value:";
        public string Hint { get; set; } = "Enter value here";

        public InputBoxDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        public InputBoxDialog(string title, string message, string hint = "Enter value here")
        {
            Title = title;
            Message = message;
            Hint = hint;
            
            InitializeComponent();
            DataContext = this;
            
            // Focus on the text box when dialog opens
            Loaded += (s, e) => InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Answer))
            {
                MessageBox.Show("Please enter a value.", "Input Required", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InputTextBox.Focus();
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
