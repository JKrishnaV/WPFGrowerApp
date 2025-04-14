using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics; // Added for Process
using System.IO;
using System.Linq; // Added for LINQ Select
using System.Reflection; // Added for Assembly
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using System.Windows; // For MessageBox (if needed, though DialogService is preferred)
using BoldReports.UI.Xaml; // For ReportViewer
using BoldReports.Windows; // Added for ReportDataSource, ReportParameter
using BoldReports.Writer; // Added for WriterFormat, ReportWriter
using WPFGrowerApp.DataAccess.Interfaces; // For IGrowerService etc.
using WPFGrowerApp.DataAccess.Models; // For Grower model
using WPFGrowerApp.Models; // Added for GrowerSearchResult
using WPFGrowerApp.Services; // For IDialogService
using Microsoft.Win32; // Added for SaveFileDialog

namespace WPFGrowerApp.ViewModels
{
    public class GrowerReportViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        // Removed duplicate _growerService definition
        private readonly IDialogService _dialogService;
        private ReportViewer _reportViewer; // To hold the instance from the View
        private List<Grower> _allGrowers; // Store all fetched growers for pagination

        // --- Pagination Properties ---
        private const int PageSize = 25; // Records per page

        private int _currentPage = 1;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    UpdatePagedData();
                    UpdatePageInfo();
                    // Update CanExecute for commands
                    ((RelayCommand)FirstPageCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)PreviousPageCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)NextPageCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)LastPageCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private int _totalPages = 0;
        public int TotalPages
        {
            get => _totalPages;
            private set => SetProperty(ref _totalPages, value);
        }

        private string _pageInfo;
        public string PageInfo
        {
            get => _pageInfo;
            private set => SetProperty(ref _pageInfo, value);
        }
        // --- End Pagination Properties ---


        // Report Path property
        public string ReportPath
        {
            get
            {
                try
                {
                    // First try to find the report as a physical file
                    string assemblyLocation = Assembly.GetExecutingAssembly().Location;
                    string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                    string reportPath = Path.Combine(assemblyDirectory, "Reports", "GrowerReportNew.rdlc");

                    if (File.Exists(reportPath))
                    {
                        Infrastructure.Logging.Logger.Info($"Found report file at: {reportPath}");
                        return reportPath;
                    }

                    // If physical file not found, try as embedded resource
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "WPFGrowerApp.Reports.GrowerReportNew.rdlc";

                    if (assembly.GetManifestResourceNames().Contains(resourceName))
                    {
                        Infrastructure.Logging.Logger.Info($"Found embedded report resource: {resourceName}");
                        return resourceName;
                    }

                    Infrastructure.Logging.Logger.Error($"Report not found at {reportPath} or as embedded resource {resourceName}");
                    return null;
                }
                catch (Exception ex)
                {
                    Infrastructure.Logging.Logger.Error("Error getting report path", ex);
                    return null;
                }
            }
        }

        // Report Data Source (Now for the paged view)
        private ObservableCollection<Grower> _pagedGrowerData;
        public ObservableCollection<Grower> PagedGrowerData
        {
            get => _pagedGrowerData;
            set => SetProperty(ref _pagedGrowerData, value);
        }

        // Example properties for filtering/parameters
        private string _filterText;
        public string FilterText
        {
            get => _filterText;
            set
            {
                // Reset to page 1 when filter changes
                if (SetProperty(ref _filterText, value))
                {
                    CurrentPage = 1; // Reset page on filter change
                    _ = LoadReportDataAsync(); // Reload and re-apply filter/pagination
                }
            }
        }

        // Example properties for column selection (could be more complex)
        private bool _showPhoneNumber = true;
        public bool ShowPhoneNumber
        {
            get => _showPhoneNumber;
            set
            {
                if (SetProperty(ref _showPhoneNumber, value))
                {
                    try
                    {
                        RefreshReportViewer();
                    }
                    catch (Exception ex)
                    {
                        Infrastructure.Logging.Logger.Error("Error updating ShowPhoneNumber parameter", ex);
                    }
                }
            }
        }

        // Add similar properties for other columns defined as parameters
        private bool _showAddress = true;
        public bool ShowAddress
        {
            get => _showAddress;
            set
            {
                if (SetProperty(ref _showAddress, value))
                {
                    RefreshReportViewer();
                }
            }
        }

        private bool _showCity = true;
        public bool ShowCity
        {
            get => _showCity;
            set
            {
                if (SetProperty(ref _showCity, value))
                {
                    RefreshReportViewer();
                }
            }
        }
        // Add more parameter properties if needed...


        public ICommand LoadReportCommand { get; }
        public ICommand EmailReportCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand ExportAllPdfCommand { get; }
        public ICommand ExportAllExcelCommand { get; } // Added Excel command
        public ICommand ExportAllCsvCommand { get; } // Added CSV command
                                                     // Export/Print are usually handled by the viewer's toolbar

        public GrowerReportViewModel(IGrowerService growerService, IDialogService dialogService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _allGrowers = new List<Grower>();
            PagedGrowerData = new ObservableCollection<Grower>();

            // Use RelayCommand for async methods as well
            LoadReportCommand = new RelayCommand(async _ => await LoadReportDataAsync(), _ => !IsBusy);
            EmailReportCommand = new RelayCommand(async _ => await EmailReportAsync(), _ => !IsBusy && _reportViewer != null && PagedGrowerData.Any());

            // Pagination Commands
            FirstPageCommand = new RelayCommand(_ => GoToFirstPage(), _ => CanGoToPreviousPage());
            PreviousPageCommand = new RelayCommand(_ => GoToPreviousPage(), _ => CanGoToPreviousPage());
            NextPageCommand = new RelayCommand(_ => GoToNextPage(), _ => CanGoToNextPage());
            LastPageCommand = new RelayCommand(_ => GoToLastPage(), _ => CanGoToNextPage());
            ExportAllPdfCommand = new RelayCommand(async _ => await ExportAllAsync(WriterFormat.PDF), _ => CanExport()); // Changed to generic method
            ExportAllExcelCommand = new RelayCommand(async _ => await ExportAllAsync(WriterFormat.Excel), _ => CanExport()); // Added Excel init
            ExportAllCsvCommand = new RelayCommand(async _ => await ExportAllAsync(WriterFormat.CSV), _ => CanExport()); // Added CSV init


            // Don't load initial data here, wait for viewer to be set
            UpdatePageInfo(); // Set initial page info
        }

        // Method for the View to pass the ReportViewer instance
        public void SetReportViewer(ReportViewer reportViewer)
        {
            _reportViewer = reportViewer;
            _ = LoadReportDataAsync(); // Load data when viewer is ready

            ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged(); // Added CanExecute update
            ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged(); // Added CanExecute update
        }

        private void UpdatePageInfo()
        {
            if (TotalPages > 0)
                PageInfo = $"Page {CurrentPage} of {TotalPages}";
            else
                PageInfo = "Page 0 of 0";
        }


        private async Task LoadReportDataAsync()
        {
            if (_reportViewer == null) return; // Don't load if viewer isn't set

            try
            {
                IsBusy = true;
                // Raise CanExecuteChanged for all relevant commands
                ((RelayCommand)LoadReportCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)EmailReportCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)FirstPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)PreviousPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)NextPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)LastPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllPdfCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand)?.RaiseCanExecuteChanged(); // Added CanExecute update
                ((RelayCommand)ExportAllCsvCommand)?.RaiseCanExecuteChanged(); // Added CanExecute update


                // Verify report path is valid
                if (string.IsNullOrEmpty(ReportPath))
                {
                    await _dialogService.ShowMessageBoxAsync("Report file not found. Please verify the report is properly embedded in the project.", "Report Error");
                    return;
                }

                // Fetch data
                var growerSearchResults = await _growerService.GetAllGrowersAsync();

                // Apply filtering from ViewModel property BEFORE mapping
                IEnumerable<GrowerSearchResult> filteredResults = growerSearchResults;
                if (!string.IsNullOrWhiteSpace(FilterText))
                {
                    string lowerFilter = FilterText.ToLower();
                    filteredResults = growerSearchResults.Where(g =>
                        (g.GrowerName != null && g.GrowerName.ToLower().Contains(lowerFilter)) ||
                        g.GrowerNumber.ToString().Contains(lowerFilter) // Simple number check
                    );
                }

                // Map GrowerSearchResult to Grower and store in the full list
                _allGrowers = filteredResults.Select(gsr => new Grower
                {
                    GrowerNumber = gsr.GrowerNumber,
                    GrowerName = gsr.GrowerName,
                    ChequeName = gsr.ChequeName,
                    City = gsr.City,
                    Phone = gsr.Phone
                    // Map other properties if needed, otherwise they'll be default
                }).ToList(); // Store all results

                // Calculate total pages
                TotalPages = (int)Math.Ceiling((double)_allGrowers.Count / PageSize);
                if (TotalPages == 0) TotalPages = 1; // Ensure at least one page even if empty

                // Ensure CurrentPage is valid (e.g., after filtering reduces pages)
                if (CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                }
                if (CurrentPage < 1)
                {
                    CurrentPage = 1;
                }

                // Update the paged data for the current page
                UpdatePagedData(); // This will also call RefreshReportViewer
                UpdatePageInfo();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error loading grower report data", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading report data: {ex.Message}", "Report Error");
            }
            finally
            {
                IsBusy = false;
                // Raise CanExecuteChanged for all relevant commands
                ((RelayCommand)LoadReportCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)EmailReportCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)FirstPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)PreviousPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)NextPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)LastPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllPdfCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand)?.RaiseCanExecuteChanged(); // Added CanExecute update
                ((RelayCommand)ExportAllCsvCommand)?.RaiseCanExecuteChanged(); // Added CanExecute update
            }
        }

        private void UpdatePagedData()
        {
            if (_allGrowers == null) return;

            try
            {
                var pagedItems = _allGrowers
                                    .Skip((CurrentPage - 1) * PageSize)
                                    .Take(PageSize)
                                    .ToList();

                PagedGrowerData.Clear();
                foreach (var item in pagedItems)
                {
                    PagedGrowerData.Add(item);
                }

                // Refresh the report viewer with the new page of data
                RefreshReportViewer();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error updating paged data", ex);
                // Optionally show error to user via DialogService
            }
        }

        private void RefreshReportViewer()
        {
            if (_reportViewer == null) return;

            try
            {
                // Use PagedGrowerData for the report
                var dataSource = new BoldReports.Windows.ReportDataSource
                {
                    Name = "GrowerDataSet", // Ensure this matches the DataSet name in your RDLC
                    Value = this.PagedGrowerData // Bind to the paged collection
                };
                _reportViewer.DataSources.Clear();
                _reportViewer.DataSources.Add(dataSource);

                // Create parameters list with properly formatted boolean values
                var parameters = new List<BoldReports.Windows.ReportParameter>
                {
                    new BoldReports.Windows.ReportParameter
                    {
                        Name = "ShowPhoneNumber",
                        Values = new List<string> { ShowPhoneNumber ? "True" : "False" }
                    },
                    new BoldReports.Windows.ReportParameter
                    {
                        Name = "ShowAddress",
                        Values = new List<string> { ShowAddress ? "True" : "False" }
                    },
                    new BoldReports.Windows.ReportParameter
                    {
                        Name = "ShowCity",
                        Values = new List<string> { ShowCity ? "True" : "False" }
                    }
                };

                // Set parameters
                _reportViewer.SetParameters(parameters);

                // Refresh the report
                _reportViewer.RefreshReport();
                Infrastructure.Logging.Logger.Info("Bold Report Viewer refreshed with parameters: " +
                    $"ShowPhoneNumber={ShowPhoneNumber}, ShowAddress={ShowAddress}, ShowCity={ShowCity}");
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error refreshing Bold Report Viewer", ex);
            }
        }


        // Remove async as Export is likely synchronous
        private async Task EmailReportAsync() // Keep async for DialogService calls
        {
            if (_reportViewer == null)
            {
                await _dialogService.ShowMessageBoxAsync("Report viewer is not ready.", "Email Error");
                return;
            }

            // No longer need to store/restore originalDataSource for this method

            try
            {
                IsBusy = true;
                ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();

                Infrastructure.Logging.Logger.Info("Starting Email export using ReportWriter...");

                // --- Get Report Definition Stream ---
                Stream reportStream = GetReportDefinitionStream();
                if (reportStream == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Could not load report definition for email.", "Email Error");
                    IsBusy = false; // Reset busy state
                    ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
                    return; // Exit if report definition not found
                }

                // --- Create and configure ReportWriter ---
                ReportWriter reportWriter = new ReportWriter(reportStream);
                reportWriter.ReportProcessingMode = BoldReports.Writer.ProcessingMode.Local; // Use Writer's enum

                // --- Set full data source on ReportWriter ---
                var fullDataSource = new BoldReports.Windows.ReportDataSource
                {
                    Name = "GrowerDataSet", // Ensure this matches the DataSet name in your RDLC
                    Value = _allGrowers // Use the full list
                };
                reportWriter.DataSources.Add(fullDataSource);

                // --- Set parameters on ReportWriter (if needed) ---
                // Assuming parameters defined in RDLC are sufficient for ReportWriter export.
                // See ExportAllPdfAsync for example if dynamic parameters are needed via BoldReports.Web.ReportParameter.
                // Commenting out parameter setting for ReportWriter - Assuming RDLC handles parameters.
                // List<BoldReports.Windows.ReportParameter> writerParameters = new List<BoldReports.Windows.ReportParameter>
                // {
                //     new BoldReports.Windows.ReportParameter { Name = "ShowPhoneNumber", Values = new List<string> { ShowPhoneNumber ? "True" : "False" } },
                //     new BoldReports.Windows.ReportParameter { Name = "ShowAddress", Values = new List<string> { ShowAddress ? "True" : "False" } },
                //     new BoldReports.Windows.ReportParameter { Name = "ShowCity", Values = new List<string> { ShowCity ? "True" : "False" } }
                //     // Add other parameters if needed
                // };
                // reportWriter.SetParameters(writerParameters); // ReportWriter might not support parameters this way in WPF

                // 1. Choose format (e.g., PDF)
                WriterFormat format = WriterFormat.PDF; // Or Excel, Word, etc.
                string fileExtension = format.ToString().ToLower();

                // 2. Define temporary file path
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"GrowerReport_{DateTime.Now:yyyyMMddHHmmss}.{fileExtension}");

                // 3. Export report programmatically using Bold Reports API
                // Create a stream and use the correct ExportFormat enum
                bool exportSuccess = false;
                try
                {
                    // Export using ReportWriter instance
                    using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                    {
                        // Use ReportWriter's Save method and its WriterFormat enum
                        reportWriter.Save(stream, format);
                        exportSuccess = true;
                    }
                }
                catch (Exception exportEx)
                {
                    Infrastructure.Logging.Logger.Error($"Error during report export to {tempFilePath}", exportEx);
                    await _dialogService.ShowMessageBoxAsync($"Failed to export report for email: {exportEx.Message}", "Email Error");
                    // Don't return, need to execute finally block
                }

                // Close the report definition stream if it's disposable (like FileStream)
                reportStream?.Dispose();


                if (!exportSuccess || !File.Exists(tempFilePath)) // Check success flag and file existence
                {
                    await _dialogService.ShowMessageBoxAsync("Failed to export the report file for emailing.", "Email Error");
                    // Don't return, need to execute finally block
                }
                else
                {
                    Infrastructure.Logging.Logger.Info($"Report exported successfully to {tempFilePath} for emailing.");
                    // 4. Create and show email draft
                    string mailto = $"mailto:?subject=Grower Report&attachment=\"{tempFilePath}\"";
                    System.Diagnostics.Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
                    // Note: Temp file cleanup might be desired, but often left to OS temp mechanisms.
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error preparing report for email", ex);
                await _dialogService.ShowMessageBoxAsync($"Error preparing report for email: {ex.Message}", "Email Error");
            }
            finally
            {
                // No need to restore data source as we didn't change the main _reportViewer
                Infrastructure.Logging.Logger.Info("Email export process finished.");
                IsBusy = false;
                ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged(); // Added CanExecute update
                ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged(); // Added CanExecute update
            }
        } // End of EmailReportAsync method

        // --- CanExecute Helper for Exports ---
        private bool CanExport() => !IsBusy && _reportViewer != null && _allGrowers != null && _allGrowers.Any();


        // --- Pagination Command Implementations ---
        private bool CanGoToPreviousPage() => !IsBusy && CurrentPage > 1;
        private bool CanGoToNextPage() => !IsBusy && CurrentPage < TotalPages;

        private void GoToFirstPage()
        {
            if (CanGoToPreviousPage()) CurrentPage = 1;
        }
        private void GoToPreviousPage()
        {
            if (CanGoToPreviousPage()) CurrentPage--;
        }
        private void GoToNextPage()
        {
            if (CanGoToNextPage()) CurrentPage++;
        }
        private void GoToLastPage()
        {
            if (CanGoToNextPage()) CurrentPage = TotalPages;
        }
        // --- End Pagination Command Implementations ---

        // Removed PrepareForFullExport, RestoreAfterFullExport, and _originalDataSourceBeforeExport
        // as they were intended for built-in export events which are not available in WPF.

        // Generic Export Method
        private async Task ExportAllAsync(WriterFormat format)
        {
            if (!CanExport()) // Use helper method
            {
                await _dialogService.ShowMessageBoxAsync("Report viewer is not ready or no data available to export.", "Export Error");
                return;
            }

            string fileExtension = format.ToString().ToLower();
            string filter = $"{format.ToString().ToUpper()} Files (*.{fileExtension})|*.{fileExtension}";
            string defaultFileName = $"GrowerReport_All_{DateTime.Now:yyyyMMdd}.{fileExtension}";

            // Configure save file dialog
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                Title = $"Save Full Grower Report as {format.ToString().ToUpper()}"
            };

            // Show save file dialog
            bool? result = saveFileDialog.ShowDialog();

            // Process save file dialog box results
            if (result != true)
            {
                // User cancelled
                return;
            }

            string savePath = saveFileDialog.FileName;

            // No longer need to store/restore originalDataSource for this method

            try
            {
                IsBusy = true;
                // Update all export command states
                ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged();

                Infrastructure.Logging.Logger.Info($"Starting {format} export to {savePath} using ReportWriter...");

                // --- Get Report Definition Stream ---
                Stream reportStream = GetReportDefinitionStream();
                if (reportStream == null)
                {
                    await _dialogService.ShowMessageBoxAsync("Could not load report definition.", "Export Error");
                    IsBusy = false; // Reset busy state
                                    // Update all export command states
                    ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged();
                    return; // Exit if report definition not found
                }

                // --- Create and configure ReportWriter ---
                ReportWriter reportWriter = new ReportWriter(reportStream);
                // Explicitly qualify ProcessingMode for ReportWriter
                reportWriter.ReportProcessingMode = BoldReports.Writer.ProcessingMode.Local;

                // --- Set full data source on ReportWriter ---
                var fullDataSource = new BoldReports.Windows.ReportDataSource
                {
                    Name = "GrowerDataSet", // Ensure this matches the DataSet name in your RDLC
                    Value = _allGrowers // Use the full list
                };

                List<BoldReports.Windows.ReportParameter> writerParameters = new List<BoldReports.Windows.ReportParameter>
                   {
                       new BoldReports.Windows.ReportParameter { Name = "ShowPhoneNumber", Values = new List<string> { ShowPhoneNumber ? "True" : "False" } },
                       new BoldReports.Windows.ReportParameter { Name = "ShowAddress", Values = new List<string> { ShowAddress ? "True" : "False" } },
                       new BoldReports.Windows.ReportParameter { Name = "ShowCity", Values = new List<string> { ShowCity ? "True" : "False" } }
                       // Add other parameters if needed
                   };
                reportWriter.SetParameters(writerParameters); // Set parameters AFTER data source
                Infrastructure.Logging.Logger.Info($"Parameters set for {format} export using ReportWriter.");

                reportWriter.DataSources.Add(fullDataSource);
                
                bool exportSuccess = false;
                try
                {
                    // Export using ReportWriter instance
                    using (var stream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                    {
                        reportWriter.Save(stream, format); // Use the passed format
                        exportSuccess = true;
                    }
                }
                catch (Exception exportEx)
                {
                    Infrastructure.Logging.Logger.Error($"Error during report export to {savePath}", exportEx);
                    await _dialogService.ShowMessageBoxAsync($"Failed to export report: {exportEx.Message}", "Export Error");
                    // Don't return here, still need to restore data source in finally
                }

                if (exportSuccess)
                {
                    Infrastructure.Logging.Logger.Info($"Report exported successfully to {savePath}");
                    await _dialogService.ShowMessageBoxAsync($"Report successfully exported to:\n{savePath}", "Export Complete");
                }
            }
            catch (Exception ex)
            {
                // Catch potential errors before export (e.g., setting data source)
                Infrastructure.Logging.Logger.Error("Error preparing report for PDF export", ex);
                await _dialogService.ShowMessageBoxAsync($"Error preparing report for export: {ex.Message}", "Export Error");
            }
            finally
            {
                // No need to restore data source as we didn't change the main _reportViewer
                Infrastructure.Logging.Logger.Info($"{format} export process finished.");
                IsBusy = false;
                // Update all export command states
                ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged();
            }
        }

        // Helper method to get the report definition stream
        private Stream GetReportDefinitionStream()
        {
            // This logic assumes ReportPath holds either:
            // 1. A relative path to the RDLC file from the assembly location.
            // 2. The full embedded resource name.
            string reportDefinitionPath = ReportPath;
            if (string.IsNullOrEmpty(reportDefinitionPath))
            {
                Infrastructure.Logging.Logger.Error("ReportWriter: ReportPath property is null or empty.");
                return null;
            }

            try
            {
                // Try embedded resource first (common scenario)
                var assembly = Assembly.GetExecutingAssembly();
                if (assembly.GetManifestResourceNames().Contains(reportDefinitionPath))
                {
                    Infrastructure.Logging.Logger.Info($"ReportWriter: Loading report definition from embedded resource: {reportDefinitionPath}");
                    return assembly.GetManifestResourceStream(reportDefinitionPath);
                }

                // If not embedded, try as a file path relative to assembly
                string assemblyLocation = assembly.Location;
                string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                string reportFullPath = Path.Combine(assemblyDirectory, reportDefinitionPath);

                if (File.Exists(reportFullPath))
                {
                    Infrastructure.Logging.Logger.Info($"ReportWriter: Loading report definition from file: {reportFullPath}");
                    return new FileStream(reportFullPath, FileMode.Open, FileAccess.Read);
                }


                Infrastructure.Logging.Logger.Error($"ReportWriter: Report definition not found as embedded resource ({reportDefinitionPath}) or file ({reportFullPath}).");
                return null;
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("ReportWriter: Error getting report definition stream", ex);
                return null;
            }
        }

    }
}
