using Microsoft.Extensions.DependencyInjection;
using System;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized; // For NotifyCollectionChangedEventArgs
using System.ComponentModel; // For INotifyPropertyChanged (already in ViewModelBase)
using System.Diagnostics; // Added for Process
using System.IO;
using System.Linq; // Added for LINQ Select
using System.Reflection; // Added for Assembly
using System.Threading.Tasks;
using System.Windows; // For MessageBox (if needed, though DialogService is preferred)
using System.Windows.Input;
using WPFGrowerApp.Commands; // Added for RelayCommand
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
        private readonly IPayGroupService _payGroupService; // Added PayGroupService
        private readonly IDialogService _dialogService;
        private ReportViewer _reportViewer; // To hold the instance from the View
        private List<Grower> _allGrowers; // Store all fetched growers AFTER filtering for pagination
        private List<PayGroup> _allPayGroups; // Store all fetched pay groups
        private bool _isUpdatingSelection = false; // Flag for Select/Deselect All re-entrancy
        private bool _isLoadReportDataAsyncBusy = false; // For busy state management

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

        // --- On Hold Filter ---
        public List<string> OnHoldFilterOptions { get; } = new List<string> { "Show All", "Show On Hold Only", "Show Not On Hold" };

        private string _selectedOnHoldFilter = "Show All";
        public string SelectedOnHoldFilter
        {
            get => _selectedOnHoldFilter;
            set
            {
                if (SetProperty(ref _selectedOnHoldFilter, value))
                {
                    CurrentPage = 1; // Reset page on filter change
                    _ = LoadReportDataAsync(); // Reload and re-apply filter/pagination
                }
            }
        }

        // --- Pay Group Filter ---
        public ObservableCollection<PayGroup> FilteredPayGroups { get; } = new ObservableCollection<PayGroup>();
        public ObservableCollection<object> SelectedPayGroups { get; } = new ObservableCollection<object>();

        private string _payGroupSearchText;
        public string PayGroupSearchText
        {
            get => _payGroupSearchText;
            set
            {
                if (SetProperty(ref _payGroupSearchText, value))
                {
                    FilterPayGroups(); // Update the filtered list when search text changes
                }
            }
        }


        // --- Column Visibility ---
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
        public ICommand ExportAllExcelCommand { get; }
        public ICommand ExportAllCsvCommand { get; }
        public ICommand SelectAllPayGroupsCommand { get; } // Added PayGroup commands
        public ICommand DeselectAllPayGroupsCommand { get; } // Added PayGroup commands


        public GrowerReportViewModel(IGrowerService growerService, IPayGroupService payGroupService, IDialogService dialogService) // Added payGroupService
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService)); // Store payGroupService
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _allGrowers = new List<Grower>();
            _allPayGroups = new List<PayGroup>(); // Initialize pay group list
            PagedGrowerData = new ObservableCollection<Grower>();

            // Commands
            LoadReportCommand = new RelayCommand(async _ => await LoadReportDataAsync(), _ => !IsBusy);
            EmailReportCommand = new RelayCommand(async _ => await EmailReportAsync(), _ => !IsBusy && _reportViewer != null && PagedGrowerData.Any());

            // Pagination Commands
            FirstPageCommand = new RelayCommand(_ => GoToFirstPage(), _ => CanGoToPreviousPage());
            PreviousPageCommand = new RelayCommand(_ => GoToPreviousPage(), _ => CanGoToPreviousPage());
            NextPageCommand = new RelayCommand(_ => GoToNextPage(), _ => CanGoToNextPage());
            LastPageCommand = new RelayCommand(_ => GoToLastPage(), _ => CanGoToNextPage());
            ExportAllPdfCommand = new RelayCommand(async _ => await ExportAllAsync(WriterFormat.PDF), _ => CanExport());
            ExportAllExcelCommand = new RelayCommand(async _ => await ExportAllAsync(WriterFormat.Excel), _ => CanExport());
            ExportAllCsvCommand = new RelayCommand(async _ => await ExportAllAsync(WriterFormat.CSV), _ => CanExport());
            SelectAllPayGroupsCommand = new RelayCommand(_ => SelectAllPayGroups(), _ => FilteredPayGroups.Any()); // Wrapped in lambda
            DeselectAllPayGroupsCommand = new RelayCommand(_ => DeselectAllPayGroups(), _ => SelectedPayGroups.Any()); // Wrapped in lambda

            // Subscribe to selection changes
            SelectedPayGroups.CollectionChanged += SelectedPayGroups_CollectionChanged;

            // Don't load initial data here, wait for viewer to be set and InitializeAsync
            UpdatePageInfo(); // Set initial page info

            _ = InitializeAsync(); // Start async initialization
        }


        private async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                _allPayGroups = (await _payGroupService.GetAllPayGroupsAsync()).OrderBy(pg => pg.Description).ToList();
                FilterPayGroups(); // Populate the FilteredPayGroups initially
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error initializing Grower Report ViewModel (loading pay groups)", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading pay groups: {ex.Message}", "Initialization Error");
            }
            finally
            {
                IsBusy = false;
            }
        }


        // Method for the View to pass the ReportViewer instance
        public void SetReportViewer(ReportViewer reportViewer)
        {
            _reportViewer = reportViewer;
            // Data loading is now triggered by InitializeAsync completion or filter changes
            // _ = LoadReportDataAsync(); // Don't load here anymore

            ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged();
            ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged();
        }


        private void UpdatePageInfo()
        {
            if (TotalPages > 0)
                PageInfo = $"Page {CurrentPage} of {TotalPages}";
            else
                PageInfo = "Page 0 of 0";
        }


        public async Task LoadReportDataAsync() // Made public
        {
            if (_reportViewer == null) return; // Don't load if viewer isn't set
            if(_isLoadReportDataAsyncBusy) return; // Prevent re-entrancy
            try
            {
                IsBusy = true;
                _isLoadReportDataAsyncBusy = true; 
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
                    IsBusy = false; // Ensure IsBusy is reset
                    _isLoadReportDataAsyncBusy = false;
                    return;
                }

                // Fetch ALL data first
                var growerSearchResults = await _growerService.GetAllGrowersForListAsync();

                // Apply filtering BEFORE mapping and pagination
                IEnumerable<GrowerSearchResult> filteredResults = growerSearchResults;

                // Text Filter
                if (!string.IsNullOrWhiteSpace(FilterText))
                {
                    string lowerFilter = FilterText.ToLower();
                    filteredResults = filteredResults.Where(g =>
                        (g.GrowerName != null && g.GrowerName.ToLower().Contains(lowerFilter)) ||
                        g.GrowerNumber.ToString().Contains(lowerFilter) // Simple number check
                    );
                }

                // On Hold Filter
                switch (SelectedOnHoldFilter)
                {
                    case "Show On Hold Only":
                        filteredResults = filteredResults.Where(g => g.IsOnHold);
                        break;
                    case "Show Not On Hold":
                        filteredResults = filteredResults.Where(g => !g.IsOnHold);
                        break;
                        // Default "Show All" does nothing
                }

                // Pay Group Filter
                if (SelectedPayGroups.Any())
                {
                    // Get the PayGroupIds from the selected objects
                    var selectedPayGroupIds = SelectedPayGroups.OfType<PayGroup>().Select(pg => pg.GroupCode).ToList();
                    if (selectedPayGroupIds.Any())
                    {
                        // Filter growers whose PayGroup is in the selected list
                        filteredResults = filteredResults.Where(g => g.PayGroup != null && selectedPayGroupIds.Contains(g.PayGroup));
                    }
                }


                // Map the FINAL filtered results to Grower model and store for pagination
                _allGrowers = filteredResults.Select(gsr => new Grower
                {
                    GrowerId = gsr.GrowerId,
                    GrowerNumber = gsr.GrowerNumber,
                    FullName = gsr.GrowerName,
                    CheckPayeeName = gsr.ChequeName,
                    City = gsr.City,
                    Province = gsr.Province,
                    PhoneNumber = gsr.Phone,
                    MobileNumber = gsr.Phone2,
                    Email = gsr.Email,
                    Notes = gsr.Notes?.Trim(),
                    IsActive = gsr.IsActive,
                    IsOnHold = gsr.IsOnHold,
                    PaymentGroupId = 1 // Default value, would need to be resolved from PayGroup string
                    // Note: Acres, PayGroup, Phone2, OnHold properties don't exist in new Grower model
                }).ToList(); // Store the filtered results

                //if _allGrowers has no data, add a empty record
                if (_allGrowers == null || !_allGrowers.Any())
                {
                    _allGrowers = new List<Grower>
                    {
                        new Grower
                        {
                            GrowerId = 0,
                            GrowerNumber = string.Empty,
                            FullName = "No growers found with current filters",
                            CheckPayeeName = string.Empty,
                            City = string.Empty,
                            Province = string.Empty,
                            PhoneNumber = string.Empty,
                            MobileNumber = string.Empty,
                            Notes = string.Empty,
                            IsOnHold = false
                        }
                    };
                    Infrastructure.Logging.Logger.Info("No growers found with current filters - adding empty placeholder record");
                }

                // Calculate total pages based on the FILTERED list
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
                _isLoadReportDataAsyncBusy = false; 
                // Raise CanExecuteChanged for all relevant commands
                ((RelayCommand)LoadReportCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)EmailReportCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)FirstPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)PreviousPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)NextPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)LastPageCommand)?.RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllPdfCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ExportAllExcelCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)ExportAllCsvCommand)?.RaiseCanExecuteChanged();
                    ((RelayCommand)SelectAllPayGroupsCommand)?.RaiseCanExecuteChanged(); // Update PayGroup commands
                    ((RelayCommand)DeselectAllPayGroupsCommand)?.RaiseCanExecuteChanged(); // Update PayGroup commands
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
                    // Consider showing a user-friendly error via _dialogService
            }
        }


        // --- Pay Group Filtering/Selection Logic ---

        private void FilterPayGroups()
        {
            FilteredPayGroups.Clear();
            if (_allPayGroups == null) return;

            IEnumerable<PayGroup> groupsToShow = _allPayGroups;

            if (!string.IsNullOrWhiteSpace(PayGroupSearchText))
            {
                string lowerSearch = PayGroupSearchText.ToLower();
                groupsToShow = groupsToShow.Where(pg =>
                    (pg.Description != null && pg.Description.ToLower().Contains(lowerSearch)) ||
                    (pg.GroupCode != null && pg.GroupCode.ToLower().Contains(lowerSearch)));
            }

            foreach (var group in groupsToShow.OrderBy(pg => pg.Description))
            {
                FilteredPayGroups.Add(group);
            }
            // Update CanExecute for Select All after filtering
            ((RelayCommand)SelectAllPayGroupsCommand)?.RaiseCanExecuteChanged();
        }

        private void SelectAllPayGroups()
        {
            if (_isUpdatingSelection) return; // Prevent re-entrancy
            _isUpdatingSelection = true;

            try
            {
                // Clear existing selections first to avoid duplicates and trigger change notification once
                SelectedPayGroups.Clear();
                // Add all items currently visible in the filtered list
                foreach (var item in FilteredPayGroups)
                {
                    SelectedPayGroups.Add(item);
                }
                CurrentPage = 1;
                _ = LoadReportDataAsync();
            }
            finally
            {
                _isUpdatingSelection = false;
                ((RelayCommand)DeselectAllPayGroupsCommand)?.RaiseCanExecuteChanged(); // Update Deselect All state
                ((RelayCommand)SelectAllPayGroupsCommand)?.RaiseCanExecuteChanged(); // Update Select All state (might be disabled if all selected)
            }
            // Data reload is triggered by CollectionChanged handler
        }

        private void DeselectAllPayGroups()
        {
            if (_isUpdatingSelection) return; // Prevent re-entrancy
            _isUpdatingSelection = true;

            try
            {
                SelectedPayGroups.Clear();
                CurrentPage = 1;
                _ = LoadReportDataAsync();
            }
            finally
            {
                _isUpdatingSelection = false;
                ((RelayCommand)DeselectAllPayGroupsCommand)?.RaiseCanExecuteChanged(); // Update Deselect All state
                ((RelayCommand)SelectAllPayGroupsCommand)?.RaiseCanExecuteChanged(); // Update Select All state
            }
            // Data reload is triggered by CollectionChanged handler
        }

        private void SelectedPayGroups_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Prevent reloading data if the change was triggered by SelectAll/DeselectAll methods
            if (_isUpdatingSelection) return;

            // Update CanExecute for Deselect All command
            ((RelayCommand)DeselectAllPayGroupsCommand)?.RaiseCanExecuteChanged();
            ((RelayCommand)SelectAllPayGroupsCommand)?.RaiseCanExecuteChanged(); // Also update Select All

            // Reload data when user manually changes selection
            CurrentPage = 1;
            _ = LoadReportDataAsync();
        }

        // --- End Pay Group Logic ---


        private async Task EmailReportAsync()
        {
            if (!CanExport()) // Use CanExport which checks _allGrowers
            {
                await _dialogService.ShowMessageBoxAsync("Report is not ready or no data available to email.", "Email Error");
                return;
            }

            try
            {
                IsBusy = true;
                ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
                // Disable other buttons during export
                ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged();

                Infrastructure.Logging.Logger.Info("Starting Email export using ReportWriter with current filters...");

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

                // --- Set data source on ReportWriter (using the currently filtered _allGrowers) ---
                var dataSourceForExport = new BoldReports.Windows.ReportDataSource
                {
                    Name = "GrowerDataSet", // Ensure this matches the DataSet name in your RDLC
                    Value = _allGrowers // Use the currently filtered list
                };
                reportWriter.DataSources.Add(dataSourceForExport);

                // --- Set parameters on ReportWriter ---
                // Use the current visibility settings for the exported report
                List<BoldReports.Windows.ReportParameter> writerParameters = new List<BoldReports.Windows.ReportParameter>
                {
                    new BoldReports.Windows.ReportParameter { Name = "ShowPhoneNumber", Values = new List<string> { ShowPhoneNumber ? "True" : "False" } },
                    new BoldReports.Windows.ReportParameter { Name = "ShowAddress", Values = new List<string> { ShowAddress ? "True" : "False" } },
                    new BoldReports.Windows.ReportParameter { Name = "ShowCity", Values = new List<string> { ShowCity ? "True" : "False" } }
                    // Add other parameters if the RDLC requires them
                };
                reportWriter.SetParameters(writerParameters);
                Infrastructure.Logging.Logger.Info($"Parameters set for Email export using ReportWriter.");
                // 1. Choose format (PDF for email)
                WriterFormat format = WriterFormat.PDF;
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
                Infrastructure.Logging.Logger.Info("Email export process finished.");
                IsBusy = false;
                // Re-enable buttons
                ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged();
            }
        }

        // --- CanExecute Helper for Exports ---
        // Export should be possible if not busy and the filtered list (_allGrowers) has data
        private bool CanExport() => !IsBusy && _allGrowers != null && _allGrowers.Any();


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
            if (!CanExport()) // Use updated CanExport
            {
                await _dialogService.ShowMessageBoxAsync("No data available to export based on current filters.", "Export Error");
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
                // Update all export command states and Email command
                ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllPdfCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllExcelCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ExportAllCsvCommand).RaiseCanExecuteChanged();

                Infrastructure.Logging.Logger.Info($"Starting {format} export to {savePath} using ReportWriter with current filters...");

                // --- Get Report Definition Stream ---
                Stream reportStream = GetReportDefinitionStream(); // Ensure this method is robust
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

                // --- Set data source on ReportWriter (using the currently filtered _allGrowers) ---
                var dataSourceForExport = new BoldReports.Windows.ReportDataSource
                {
                    Name = "GrowerDataSet", // Ensure this matches the DataSet name in your RDLC
                    Value = _allGrowers // Use the currently filtered list
                };
                reportWriter.DataSources.Add(dataSourceForExport); // Add data source first

                // --- Set parameters on ReportWriter ---
                List<BoldReports.Windows.ReportParameter> writerParameters = new List<BoldReports.Windows.ReportParameter>
                {
                    new BoldReports.Windows.ReportParameter { Name = "ShowPhoneNumber", Values = new List<string> { ShowPhoneNumber ? "True" : "False" } },
                    new BoldReports.Windows.ReportParameter { Name = "ShowAddress", Values = new List<string> { ShowAddress ? "True" : "False" } },
                    new BoldReports.Windows.ReportParameter { Name = "ShowCity", Values = new List<string> { ShowCity ? "True" : "False" } }
                    // Add other parameters if needed
                };
                reportWriter.SetParameters(writerParameters); // Set parameters AFTER data source
                Infrastructure.Logging.Logger.Info($"Parameters set for {format} export using ReportWriter.");


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
                Infrastructure.Logging.Logger.Info($"{format} export process finished.");
                IsBusy = false;
                // Re-enable buttons
                ((RelayCommand)EmailReportCommand).RaiseCanExecuteChanged();
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
