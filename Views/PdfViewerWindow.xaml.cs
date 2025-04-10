using System.IO;
using System.Windows;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PdfViewerWindow.xaml
    /// </summary>
    public partial class PdfViewerWindow : Window
    {
        public PdfViewerWindow()
        {
            InitializeComponent();
            // Consider setting the Owner here if needed, e.g.:
            // this.Owner = Application.Current.MainWindow;
            this.Unloaded += PdfViewerWindow_Unloaded; // Clean up viewer resources
        }

        /// <summary>
        /// Loads a PDF document from a stream into the viewer.
        /// </summary>
        /// <param name="pdfStream">The stream containing the PDF data.</param>
        public void LoadPdf(Stream pdfStream)
        {
            if (pdfStream != null && pdfStream.Length > 0)
            {
                 // Reset position just in case
                pdfStream.Position = 0; 
                // Load the document stream
                pdfViewer.Load(pdfStream); 
            }
            else
            {
                MessageBox.Show("Could not load the PDF document because the stream was empty.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Loads a PDF document from a file path into the viewer.
        /// </summary>
        /// <param name="filePath">The path to the PDF file.</param>
        public void LoadPdf(string filePath)
        {
             if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                // Load the document from file path
                pdfViewer.Load(filePath);
            }
            else
            {
                 MessageBox.Show($"Could not load the PDF document. File not found or path is invalid: {filePath}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PdfViewerWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unload the document to release file handles/memory
            pdfViewer?.Unload(); 
        }
    }
}
