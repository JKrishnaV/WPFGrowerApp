using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Converters
{
    /// <summary>
    /// Converter that calculates sum values from a collection of PaymentDistributionChart items.
    /// Used for displaying summary statistics in the Payment Distribution Chart.
    /// </summary>
    public class CollectionSumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable collection || parameter is not string param)
                return 0;

            try
            {
                var items = collection.Cast<PaymentDistributionChart>().ToList();
                
                return param switch
                {
                    "Value" => items.Sum(x => x.Value),
                    "Advance" => items.Where(x => x.Category.Contains("Advance")).Sum(x => x.Value),
                    "Final" => items.Where(x => x.Category.Contains("Final")).Sum(x => x.Value),
                    "Count" => items.Sum(x => x.Count),
                    _ => 0
                };
            }
            catch
            {
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that calculates summary values from a collection of GrowerPerformanceChart items.
    /// Used for displaying summary statistics in the Grower Performance Chart.
    /// </summary>
    public class GrowerPerformanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable collection || parameter is not string param)
                return 0;

            try
            {
                var items = collection.Cast<GrowerPerformanceChart>().ToList();
                
                return param switch
                {
                    "TopPerformer" => items.OrderByDescending(x => x.TotalPayments).FirstOrDefault()?.GrowerDisplayName ?? "N/A",
                    "HighestPayment" => items.Max(x => x.TotalPayments),
                    "AveragePayment" => items.Any() ? items.Average(x => x.TotalPayments) : 0,
                    "GrowerCount" => items.Count,
                    _ => 0
                };
            }
            catch
            {
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that calculates summary values from a collection of MonthlyTrendChart items.
    /// Used for displaying summary statistics in the Monthly Trend Chart.
    /// </summary>
    public class MonthlyTrendConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not IEnumerable collection || parameter is not string param)
                return 0;

            try
            {
                var items = collection.Cast<MonthlyTrendChart>().ToList();
                
                return param switch
                {
                    "PeakMonth" => items.OrderByDescending(x => x.TotalPayments).FirstOrDefault()?.MonthDisplay ?? "N/A",
                    "PeakAmount" => items.Max(x => x.TotalPayments),
                    "AverageMonthly" => items.Any() ? items.Average(x => x.TotalPayments) : 0,
                    "TotalPayments" => items.Sum(x => x.TotalPayments),
                    _ => 0
                };
            }
            catch
            {
                return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
