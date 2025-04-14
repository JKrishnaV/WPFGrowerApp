using System.Windows.Controls;
using WPFGrowerApp.ViewModels;
using BoldReports.UI.Xaml; // For ReportViewer 
// Note: Correct namespace for ReportExportEventArgs needs verification based on installed BoldReports version.
// using BoldReports.Windows; // Or other BoldReports namespace?

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerReportView.xaml
    /// </summary>
    public partial class GrowerReportView : UserControl
    {
        public GrowerReportView()
        {
            InitializeComponent();
            this.DataContextChanged += GrowerReportView_DataContextChanged;
        }

        private void GrowerReportView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is GrowerReportViewModel vm)
            {
                vm.SetReportViewer(ReportViewer);
            }
        }

        private void ReportViewer_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is GrowerReportViewModel viewModel)
            {
              // viewModel.SetReportViewer(ReportViewer);
             }
         }

        // Removed ReportViewer_ReportExportBegin and ReportViewer_ReportExportEnd event handlers
        // as the events are not available in WPF and we are using a custom export button.
     }
 }
