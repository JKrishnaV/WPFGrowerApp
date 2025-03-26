using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private ObservableCollection<Grower> _growers;
        private ObservableCollection<PieChartData> _provinceDistribution;
        private ObservableCollection<PieChartData> _priceLevelDistribution;
        private ObservableCollection<PieChartData> _payGroupDistribution;
        private string _selectedReportType;
        private string _exportFormat;
        private bool _isExporting;

        public ReportsViewModel()
        {
            // In a real application, this would come from a database or service
            LoadSampleData();
            
            // Initialize chart data
            GenerateProvinceDistribution();
            GeneratePriceLevelDistribution();
            GeneratePayGroupDistribution();
            
            // Set default values
            SelectedReportType = "Province Distribution";
            ExportFormat = "PDF";
            
            // Initialize commands
            ExportReportCommand = new RelayCommand(ExportReport, CanExportReport);
        }
        
        public ObservableCollection<Grower> Growers
        {
            get => _growers;
            set
            {
                _growers = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<PieChartData> ProvinceDistribution
        {
            get => _provinceDistribution;
            set
            {
                _provinceDistribution = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<PieChartData> PriceLevelDistribution
        {
            get => _priceLevelDistribution;
            set
            {
                _priceLevelDistribution = value;
                OnPropertyChanged();
            }
        }
        
        public ObservableCollection<PieChartData> PayGroupDistribution
        {
            get => _payGroupDistribution;
            set
            {
                _payGroupDistribution = value;
                OnPropertyChanged();
            }
        }
        
        public string SelectedReportType
        {
            get => _selectedReportType;
            set
            {
                _selectedReportType = value;
                OnPropertyChanged();
            }
        }
        
        public string ExportFormat
        {
            get => _exportFormat;
            set
            {
                _exportFormat = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                _isExporting = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public ICommand ExportReportCommand { get; }
        
        private void LoadSampleData()
        {
            // In a real application, this would come from a database
            Growers = new ObservableCollection<Grower>
            {
                new Grower { GrowerNumber = 1, GrowerName = "Berry Farm Ltd", ChequeName = "Berry Farm Ltd", Address = "123 Farm Road", City = "Vancouver", Prov = "BC", Postal = "V6G 1Z9", Phone = "604-555-1234", Acres = 25, PayGroup = "1", PriceLevel = 1 },
                new Grower { GrowerNumber = 2, GrowerName = "Strawberry Fields", ChequeName = "Strawberry Fields Inc", Address = "456 Field Lane", City = "Surrey", Prov = "BC", Postal = "V3T 2W1", Phone = "604-555-5678", Acres = 15, PayGroup = "2", PriceLevel = 2 },
                new Grower { GrowerNumber = 3, GrowerName = "Blueberry Hills", ChequeName = "Blueberry Hills Co", Address = "789 Hill Ave", City = "Abbotsford", Prov = "BC", Postal = "V2S 7T6", Phone = "604-555-9012", Acres = 30, PayGroup = "1", PriceLevel = 1 },
                new Grower { GrowerNumber = 4, GrowerName = "Raspberry Valley", ChequeName = "Raspberry Valley LLC", Address = "321 Valley Blvd", City = "Chilliwack", Prov = "BC", Postal = "V2P 6H2", Phone = "604-555-3456", Acres = 20, PayGroup = "3", PriceLevel = 3 },
                new Grower { GrowerNumber = 5, GrowerName = "Cranberry Marsh", ChequeName = "Cranberry Marsh Inc", Address = "654 Marsh Road", City = "Richmond", Prov = "BC", Postal = "V6Y 2B3", Phone = "604-555-7890", Acres = 40, PayGroup = "2", PriceLevel = 2 },
                new Grower { GrowerNumber = 6, GrowerName = "Blackberry Patch", ChequeName = "Blackberry Patch Ltd", Address = "987 Patch Street", City = "Langley", Prov = "BC", Postal = "V3A 4R7", Phone = "604-555-2345", Acres = 18, PayGroup = "1", PriceLevel = 1 },
                new Grower { GrowerNumber = 7, GrowerName = "Cherry Orchard", ChequeName = "Cherry Orchard Co", Address = "159 Orchard Drive", City = "Kelowna", Prov = "BC", Postal = "V1Y 7N6", Phone = "250-555-6789", Acres = 35, PayGroup = "2", PriceLevel = 2 },
                new Grower { GrowerNumber = 8, GrowerName = "Apple Acres", ChequeName = "Apple Acres Inc", Address = "753 Apple Road", City = "Vernon", Prov = "BC", Postal = "V1T 8K2", Phone = "250-555-0123", Acres = 45, PayGroup = "3", PriceLevel = 3 },
                new Grower { GrowerNumber = 9, GrowerName = "Peach Grove", ChequeName = "Peach Grove Ltd", Address = "852 Grove Lane", City = "Penticton", Prov = "BC", Postal = "V2A 5R9", Phone = "250-555-4567", Acres = 22, PayGroup = "1", PriceLevel = 1 },
                new Grower { GrowerNumber = 10, GrowerName = "Plum Orchard", ChequeName = "Plum Orchard Co", Address = "426 Plum Street", City = "Summerland", Prov = "BC", Postal = "V0H 1Z7", Phone = "250-555-8901", Acres = 28, PayGroup = "2", PriceLevel = 2 },
                new Grower { GrowerNumber = 11, GrowerName = "Prairie Berries", ChequeName = "Prairie Berries Ltd", Address = "111 Prairie Road", City = "Calgary", Prov = "AB", Postal = "T2P 5M7", Phone = "403-555-2345", Acres = 50, PayGroup = "3", PriceLevel = 3 },
                new Grower { GrowerNumber = 12, GrowerName = "Mountain Fruits", ChequeName = "Mountain Fruits Inc", Address = "222 Mountain Ave", City = "Edmonton", Prov = "AB", Postal = "T5J 3N4", Phone = "780-555-6789", Acres = 38, PayGroup = "1", PriceLevel = 1 },
                new Grower { GrowerNumber = 13, GrowerName = "Valley Harvest", ChequeName = "Valley Harvest Co", Address = "333 Harvest Lane", City = "Winnipeg", Prov = "MB", Postal = "R3C 0V1", Phone = "204-555-0123", Acres = 42, PayGroup = "2", PriceLevel = 2 },
                new Grower { GrowerNumber = 14, GrowerName = "Eastern Orchards", ChequeName = "Eastern Orchards Ltd", Address = "444 Eastern Blvd", City = "Toronto", Prov = "ON", Postal = "M5V 2H1", Phone = "416-555-4567", Acres = 33, PayGroup = "3", PriceLevel = 3 },
                new Grower { GrowerNumber = 15, GrowerName = "Atlantic Berries", ChequeName = "Atlantic Berries Inc", Address = "555 Atlantic Road", City = "Halifax", Prov = "NS", Postal = "B3J 3K5", Phone = "902-555-8901", Acres = 27, PayGroup = "1", PriceLevel = 1 }
            };
        }
        
        private void GenerateProvinceDistribution()
        {
            var provinceGroups = Growers
                .GroupBy(g => g.Prov ?? "Unknown")
                .Select(g => new PieChartData
                {
                    Category = g.Key,
                    Value = g.Count()
                })
                .ToList();
            
            ProvinceDistribution = new ObservableCollection<PieChartData>(provinceGroups);
        }
        
        private void GeneratePriceLevelDistribution()
        {
            var priceLevelGroups = Growers
                .GroupBy(g => g.PriceLevel)
                .Select(g => new PieChartData
                {
                    Category = $"Level {g.Key}",
                    Value = g.Count()
                })
                .ToList();
            
            PriceLevelDistribution = new ObservableCollection<PieChartData>(priceLevelGroups);
        }
        
        private void GeneratePayGroupDistribution()
        {
            var payGroupGroups = Growers
                .GroupBy(g => g.PayGroup ?? "Unknown")
                .Select(g => new PieChartData
                {
                    Category = $"Group {g.Key}",
                    Value = g.Count()
                })
                .ToList();
            
            PayGroupDistribution = new ObservableCollection<PieChartData>(payGroupGroups);
        }
        
        private bool CanExportReport(object parameter)
        {
            return !IsExporting;
        }
        
        private void ExportReport(object parameter)
        {
            IsExporting = true;
            
            try
            {
                var exportService = new Services.ReportExportService();
                
                switch (ExportFormat)
                {
                    case "PDF":
                        if (SelectedReportType.Contains("Province") || 
                            SelectedReportType.Contains("Price Level") || 
                            SelectedReportType.Contains("Pay Group"))
                        {
                            // For chart reports, we would pass the chart element
                            exportService.ExportToPdf(Growers, SelectedReportType);
                        }
                        else
                        {
                            // For detail reports
                            exportService.ExportToPdf(Growers, "Grower Detail Report");
                        }
                        break;
                        
                    case "Excel":
                        exportService.ExportToExcel(Growers, SelectedReportType);
                        break;
                        
                    case "Word":
                        exportService.ExportToWord(Growers, SelectedReportType);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting report: {ex.Message}", 
                    "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsExporting = false;
            }
        }
    }
    
    public class PieChartData
    {
        public string Category { get; set; }
        public int Value { get; set; }
    }
}
