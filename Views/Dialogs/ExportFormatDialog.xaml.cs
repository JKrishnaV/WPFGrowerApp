using System.Windows;

namespace WPFGrowerApp.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for ExportFormatDialog.xaml
    /// </summary>
    public partial class ExportFormatDialog : Window
    {
        public string SelectedFormat { get; private set; } = "Excel";

        public ExportFormatDialog()
        {
            InitializeComponent();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            // Determine selected format
            if (ExcelRadio.IsChecked == true)
                SelectedFormat = "Excel";
            else if (PdfRadio.IsChecked == true)
                SelectedFormat = "PDF";
            else if (CsvRadio.IsChecked == true)
                SelectedFormat = "CSV";
            else if (WordRadio.IsChecked == true)
                SelectedFormat = "Word";

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
