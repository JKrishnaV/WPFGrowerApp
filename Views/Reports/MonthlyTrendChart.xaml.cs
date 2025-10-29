using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Views.Reports
{
    /// <summary>
    /// Interaction logic for MonthlyTrendChart.xaml
    /// </summary>
    public partial class MonthlyTrendChart : UserControl
    {
        public MonthlyTrendChart()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly System.Windows.DependencyProperty ChartDataProperty =
            System.Windows.DependencyProperty.Register(
                "ChartData", 
                typeof(System.Collections.Generic.List<WPFGrowerApp.DataAccess.Models.MonthlyTrendChart>), 
                typeof(MonthlyTrendChart),
                new System.Windows.PropertyMetadata(null, OnChartDataChanged));

        public List<WPFGrowerApp.DataAccess.Models.MonthlyTrendChart> ChartData
        {
            get => (List<WPFGrowerApp.DataAccess.Models.MonthlyTrendChart>)GetValue(ChartDataProperty);
            set => SetValue(ChartDataProperty, value);
        }

        #endregion

        #region Calculated Properties

        public decimal TotalPayments => ChartData?.Sum(x => x.TotalPayments) ?? 0;
        public decimal AverageMonthly => ChartData?.Count > 0 ? TotalPayments / ChartData.Count : 0;
        public string PeakMonth => ChartData?.OrderByDescending(x => x.TotalPayments).FirstOrDefault()?.MonthDisplay ?? "N/A";
        public decimal PeakAmount => ChartData?.Max(x => x.TotalPayments) ?? 0;

        #endregion

        #region Event Handlers

        private static void OnChartDataChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (d is MonthlyTrendChart chart)
            {
                chart.UpdateChartData();
            }
        }

        private void UpdateChartData()
        {
            // Trigger property change notifications for calculated properties
            OnPropertyChanged(nameof(TotalPayments));
            OnPropertyChanged(nameof(AverageMonthly));
            OnPropertyChanged(nameof(PeakMonth));
            OnPropertyChanged(nameof(PeakAmount));
        }

        private void OnPropertyChanged(string propertyName)
        {
            // This would typically implement INotifyPropertyChanged
            // For now, we'll use a simple approach
        }

        #endregion
    }
}
