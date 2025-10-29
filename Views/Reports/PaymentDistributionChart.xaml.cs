using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Views.Reports
{
    /// <summary>
    /// Interaction logic for PaymentDistributionChart.xaml
    /// </summary>
    public partial class PaymentDistributionChart : UserControl
    {
        public PaymentDistributionChart()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public static readonly System.Windows.DependencyProperty ChartDataProperty =
            System.Windows.DependencyProperty.Register(
                "ChartData", 
                typeof(System.Collections.Generic.List<WPFGrowerApp.DataAccess.Models.PaymentDistributionChart>), 
                typeof(PaymentDistributionChart),
                new System.Windows.PropertyMetadata(null, OnChartDataChanged));

        public List<WPFGrowerApp.DataAccess.Models.PaymentDistributionChart> ChartData
        {
            get => (List<WPFGrowerApp.DataAccess.Models.PaymentDistributionChart>)GetValue(ChartDataProperty);
            set => SetValue(ChartDataProperty, value);
        }

        #endregion

        #region Calculated Properties

        public decimal TotalValue => ChartData?.Sum(x => x.Value) ?? 0;
        public decimal AdvanceTotal => ChartData?.Where(x => x.Category.Contains("Advance")).Sum(x => x.Value) ?? 0;
        public decimal FinalTotal => ChartData?.Where(x => x.Category.Contains("Final")).Sum(x => x.Value) ?? 0;
        public int PaymentCount => ChartData?.Sum(x => x.Count) ?? 0;

        #endregion

        #region Event Handlers

        private static void OnChartDataChanged(System.Windows.DependencyObject d, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (d is PaymentDistributionChart chart)
            {
                chart.UpdateChartData();
            }
        }

        private void UpdateChartData()
        {
            // Trigger property change notifications for calculated properties
            OnPropertyChanged(nameof(TotalValue));
            OnPropertyChanged(nameof(AdvanceTotal));
            OnPropertyChanged(nameof(FinalTotal));
            OnPropertyChanged(nameof(PaymentCount));
        }

        private void OnPropertyChanged(string propertyName)
        {
            // This would typically implement INotifyPropertyChanged
            // For now, we'll use a simple approach
        }

        #endregion
    }
}
