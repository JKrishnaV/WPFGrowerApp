using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Views.Reports
{
    /// <summary>
    /// Interaction logic for GrowerPerformanceChart.xaml
    /// </summary>
    public partial class GrowerPerformanceChart : UserControl
    {
        public GrowerPerformanceChart()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly System.Windows.DependencyProperty ChartDataProperty =
            System.Windows.DependencyProperty.Register(
                "ChartData", 
                typeof(System.Collections.Generic.List<WPFGrowerApp.DataAccess.Models.GrowerPerformanceChart>), 
                typeof(GrowerPerformanceChart),
                new System.Windows.PropertyMetadata(null, OnChartDataChanged));

        public List<WPFGrowerApp.DataAccess.Models.GrowerPerformanceChart> ChartData
        {
            get => (List<WPFGrowerApp.DataAccess.Models.GrowerPerformanceChart>)GetValue(ChartDataProperty);
            set => SetValue(ChartDataProperty, value);
        }

        #endregion

        #region Calculated Properties

        public int GrowerCount => ChartData?.Count ?? 0;
        public decimal AveragePayment => ChartData?.Count > 0 ? ChartData.Average(x => x.TotalPayments) : 0;
        public decimal HighestPayment => ChartData?.Max(x => x.TotalPayments) ?? 0;
        public string TopPerformer => ChartData?.OrderByDescending(x => x.TotalPayments).FirstOrDefault()?.GrowerDisplayName ?? "N/A";

        #endregion

        #region Event Handlers

        private static void OnChartDataChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (d is GrowerPerformanceChart chart)
            {
                chart.UpdateChartData();
            }
        }

        private void UpdateChartData()
        {
            // Trigger property change notifications for calculated properties
            OnPropertyChanged(nameof(GrowerCount));
            OnPropertyChanged(nameof(AveragePayment));
            OnPropertyChanged(nameof(HighestPayment));
            OnPropertyChanged(nameof(TopPerformer));
        }

        private void OnPropertyChanged(string propertyName)
        {
            // This would typically implement INotifyPropertyChanged
            // For now, we'll use a simple approach
        }

        #endregion
    }
}
