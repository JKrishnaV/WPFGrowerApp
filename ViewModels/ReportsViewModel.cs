using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Commands;
using System.Threading.Tasks;
using System.Windows;

namespace WPFGrowerApp.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private readonly GrowerService _growerService;
        private ObservableCollection<Grower> _growers;
        private ObservableCollection<PieChartData> _provinceDistribution;
        private ObservableCollection<PieChartData> _priceLevelDistribution;
        private ObservableCollection<PieChartData> _payGroupDistribution;
        private string _selectedReportType;
        private string _exportFormat;
        private bool _isExporting;
        private bool _isLoading;

        public ReportsViewModel()
        {
            _growerService = new GrowerService();
            IsLoading = true;
            
            // Load real data from database
            LoadGrowerDataAsync().ConfigureAwait(false);
            
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
        
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
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
        
        private async Task LoadGrowerDataAsync()
        {
            try
            {
                IsLoading = true;
                var growers = await _growerService.GetAllGrowersAsync();
                
                if (growers != null && growers.Any())
                {
                    // Load complete grower details for each grower
                        var detailedGrowerTasks = growers.Select(g => _growerService.GetGrowerByNumberAsync(g.GrowerNumber.ToString()));
                    var detailedGrowers = await Task.WhenAll(detailedGrowerTasks);
                    
                    Growers = new ObservableCollection<Grower>(detailedGrowers.Where(g => g != null));
                    
                    // Update distribution data
                    GenerateProvinceDistribution();
                    GeneratePriceLevelDistribution();
                    GeneratePayGroupDistribution();
                }
                else
                {
                    Growers = new ObservableCollection<Grower>();
                    MessageBox.Show("No growers found in the database.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading grower data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Growers = new ObservableCollection<Grower>();
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        private void GenerateProvinceDistribution()
        {
            if (Growers == null || !Growers.Any())
            {
                ProvinceDistribution = new ObservableCollection<PieChartData>();
                return;
            }

            var provinceGroups = Growers
                .GroupBy(g => g.Prov ?? "Unknown")
                .Select(g => new PieChartData
                {
                    Category = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(g => g.Value)
                .ToList();
            
            ProvinceDistribution = new ObservableCollection<PieChartData>(provinceGroups);
        }
        
        private void GeneratePriceLevelDistribution()
        {
            if (Growers == null || !Growers.Any())
            {
                PriceLevelDistribution = new ObservableCollection<PieChartData>();
                return;
            }

            var priceLevelGroups = Growers
                .GroupBy(g => g.PriceLevel)
                .Select(g => new PieChartData
                {
                    Category = $"Level {g.Key}",
                    Value = g.Count()
                })
                .OrderBy(g => g.Category)
                .ToList();
            
            PriceLevelDistribution = new ObservableCollection<PieChartData>(priceLevelGroups);
        }
        
        private void GeneratePayGroupDistribution()
        {
            if (Growers == null || !Growers.Any())
            {
                PayGroupDistribution = new ObservableCollection<PieChartData>();
                return;
            }

            var payGroupGroups = Growers
                .GroupBy(g => g.PayGroup ?? "Unknown")
                .Select(g => new PieChartData
                {
                    Category = $"Group {g.Key}",
                    Value = g.Count()
                })
                .OrderBy(g => g.Category)
                .ToList();
            
            PayGroupDistribution = new ObservableCollection<PieChartData>(payGroupGroups);
        }
        
        private bool CanExportReport(object parameter)
        {
            return !IsExporting && Growers != null && Growers.Any();
        }
        
        private void ExportReport(object parameter)
        {
            if (Growers == null || !Growers.Any())
            {
                System.Windows.MessageBox.Show("No data available to export.", 
                    "Export Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

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
