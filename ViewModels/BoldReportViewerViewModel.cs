using BoldReports.Windows; // Correct namespace found from code-behind
using System.Collections.Generic; // For List
using System.IO; // Add for Stream

namespace WPFGrowerApp.ViewModels
{
    // Assuming ViewModelBase provides INotifyPropertyChanged and is in WPFGrowerApp.ViewModels
    public class BoldReportViewerViewModel : ViewModelBase
    {
        private Stream _reportStream; // Changed from string path
        private List<ReportDataSource> _reportDataSources; // Use correct type
        private List<ReportParameter> _reportParameters; // Added to store parameters
        private string _reportTitle = "Report Viewer";

        // Changed from ReportPath to ReportStream
        public Stream ReportStream
        {
            get => _reportStream;
            set => SetProperty(ref _reportStream, value);
        }

        public List<ReportDataSource> ReportDataSources // Use correct type
        {
            get => _reportDataSources;
            set => SetProperty(ref _reportDataSources, value);
        }

        // Added property for parameters
        public List<ReportParameter> ReportParameters
        {
            get => _reportParameters;
            set => SetProperty(ref _reportParameters, value);
        }

        public string ReportTitle
        {
            get => _reportTitle;
            set => SetProperty(ref _reportTitle, value);
        }

        // Constructor accepting Stream, DataSources, and Parameters
        public BoldReportViewerViewModel(Stream reportStream, List<ReportDataSource> dataSources, List<ReportParameter> parameters = null) // Made parameters optional for now
        {
            ReportStream = reportStream; // Assign stream
            ReportDataSources = dataSources;
            ReportParameters = parameters ?? new List<ReportParameter>(); // Assign parameters or empty list
        }

        // Removed the other incomplete constructor
    }
}
