using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.DataAccess.Exceptions;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;
using System.Windows.Data; // Added for ICollectionView

namespace WPFGrowerApp.ViewModels
{
    // Inherit from ViewModelBase
    public class ImportViewModel : ViewModelBase
    {
        private readonly IFileImportService _fileImportService;
        private readonly IImportBatchProcessor _importBatchProcessor;
        private readonly ValidationService _validationService;
        private readonly IDialogService _dialogService;
        private readonly IDepotService _depotService; // Added DepotService
        private ObservableCollection<ImportFileInfo> _selectedFiles;
        // private string _depot; // Replaced by SelectedDepotId
        private bool _isImporting;
        private int _progress;
        private string _statusMessage;
        private ObservableCollection<string> _errors;
        private ImportBatch _currentBatch; // Stores the batch for the current/last file processed

        // New properties for Depot ComboBox
        private ObservableCollection<Depot> _depots;
    private Depot _selectedDepot;
    private int? _selectedDepotId; // To hold the ID for processing

        // Properties for Error Filtering
        private string _selectedFileNameFilter;
        private string _errorTextFilter;

        public ImportViewModel(
            IFileImportService fileImportService,
            IImportBatchProcessor importBatchProcessor,
            ValidationService validationService,
            IDialogService dialogService,
            IDepotService depotService) // Added IDepotService parameter
        {
            _fileImportService = fileImportService ?? throw new ArgumentNullException(nameof(fileImportService));
            _importBatchProcessor = importBatchProcessor ?? throw new ArgumentNullException(nameof(importBatchProcessor));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _depotService = depotService ?? throw new ArgumentNullException(nameof(depotService)); // Assign injected service
            _errors = new ObservableCollection<string>();
            _selectedFiles = new ObservableCollection<ImportFileInfo>();
            _depots = new ObservableCollection<Depot>(); // Initialize depots collection

            // Initialize Error Filtering
            _errors.CollectionChanged += (s, e) => FilteredErrors?.Refresh();
            FilteredErrors = CollectionViewSource.GetDefaultView(_errors);
            FilteredErrors.Filter = FilterErrorsPredicate;

            // Load depots when ViewModel is created
            _ = LoadDepotsAsync(); // Fire and forget (or handle completion if needed)
        }

        public ObservableCollection<ImportFileInfo> SelectedFiles
        {
            get => _selectedFiles;
            set => SetProperty(ref _selectedFiles, value);
        }

        // public string Depot // Original string property - replaced
        // {
        //     get => _depot;
        //     set => SetProperty(ref _depot, value);
        // }

        // New Properties for Depot ComboBox binding
        public ObservableCollection<Depot> Depots
        {
            get => _depots;
            set => SetProperty(ref _depots, value);
        }

        public Depot SelectedDepot
        {
            get => _selectedDepot;
            set
            {
                if (SetProperty(ref _selectedDepot, value))
                {
                    // Update the SelectedDepotId when the selection changes
                    SelectedDepotId = _selectedDepot?.DepotId;
                    // Trigger CanExecuteChanged for StartImportCommand as it depends on Depot selection
                    ((RelayCommand)StartImportCommand).RaiseCanExecuteChanged();
                }
            }
        }

        // Hidden property to store the selected DepotId for processing logic
        public int? SelectedDepotId
        {
            get => _selectedDepotId;
            private set => SetProperty(ref _selectedDepotId, value); // Keep private set if only updated internally
        }

        public bool IsImporting
        {
            get => _isImporting;
            set
            {
                SetProperty(ref _isImporting, value);
                // Trigger CanExecuteChanged for commands that depend on IsImporting
                ((RelayCommand)StartImportCommand).RaiseCanExecuteChanged();
                ((RelayCommand)CancelImportCommand).RaiseCanExecuteChanged();
                ((RelayCommand)RevertImportCommand).RaiseCanExecuteChanged();
            }
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        // Properties for Error Filtering
        public string SelectedFileNameFilter
        {
            get => _selectedFileNameFilter;
            set
            {
                if (SetProperty(ref _selectedFileNameFilter, value))
                {
                    FilteredErrors?.Refresh(); // Refresh the view when filter changes
                }
            }
        }

        public string ErrorTextFilter
        {
            get => _errorTextFilter;
            set
            {
                if (SetProperty(ref _errorTextFilter, value))
                {
                    FilteredErrors?.Refresh(); // Refresh the view when filter changes
                }
            }
        }

        // Property to expose the filtered errors view
        public ICollectionView FilteredErrors { get; private set; }

        public ObservableCollection<string> Errors
        {
            get => _errors;
            // No public setter needed if only modified internally
            // set => SetProperty(ref _errors, value);
        }

        // Property to hold selected errors for copying
        public List<string> SelectedErrors { get; set; } = new List<string>();

        // Commands
        public ICommand LoadDepotsCommand => new RelayCommand(async o => await LoadDepotsAsync()); // Command to load depots
        public ICommand BrowseFilesCommand => new RelayCommand(o => BrowseFiles());
        public ICommand RemoveFileCommand => new RelayCommand(o => RemoveFile((ImportFileInfo)o), o => !IsImporting);
        public ICommand StartImportCommand => new RelayCommand(async o => await StartImportAsync(), o => !IsImporting && SelectedFiles.Any() && SelectedDepot != null); // Depend on SelectedDepot
        public ICommand CancelImportCommand => new RelayCommand(o => CancelImport(), o => IsImporting);
        public ICommand RevertImportCommand => new RelayCommand(async o => await RevertLastImportAsync(), o => !IsImporting && _currentBatch != null);
        public ICommand SelectAllErrorsCommand => new RelayCommand(o => SelectAllErrors());
        public ICommand CopyErrorsCommand => new RelayCommand(o => CopyErrors());


        // Method to load depots
        public async Task LoadDepotsAsync()
        {
            try
            {
                var depotList = await _depotService.GetAllDepotsAsync();
                Depots.Clear();
                foreach (var depot in depotList)
                {
                    Depots.Add(depot);
                }
                // Optionally select the first depot by default
                // SelectedDepot = Depots.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load depots", ex);
                await _dialogService.ShowMessageBoxAsync("Error loading depots. Please check connection or logs.", "Load Error");
            }
        }

        private void BrowseFiles()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All files (*.*)|*.*|CSV files (*.csv)|*.csv",
                FilterIndex = 1,
                Title = "Select Receipt Import Files",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                Errors.Clear();
                StatusMessage = string.Empty;
                _currentBatch = null;
                SelectedFiles.Clear();
                foreach (var file in dialog.FileNames)
                {
                    SelectedFiles.Add(new ImportFileInfo { FilePath = file, Status = "Pending" });
                }
                 ((RelayCommand)RevertImportCommand).RaiseCanExecuteChanged();
            }
        }

        private void RemoveFile(ImportFileInfo file)
        {
            if (file != null && !IsImporting)
            {
                SelectedFiles.Remove(file);
                if (_currentBatch != null && file.FilePath.EndsWith(_currentBatch.SourceFileName))
                {
                    Errors.Clear();
                    StatusMessage = string.Empty;
                    _currentBatch = null;
                    ((RelayCommand)RevertImportCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private async Task StartImportAsync()
        {
            IsImporting = true;
            Errors.Clear();
            StatusMessage = "Starting import process...";
            Progress = 0;
            _currentBatch = null; // Reset batch info

            var totalFiles = SelectedFiles.Count;
            var overallProcessedFiles = 0;
            var overallFailedFiles = 0;
            try
            {
                for (int fileIndex = 0; fileIndex < totalFiles; fileIndex++)
                {
                    var file = SelectedFiles[fileIndex];
                    ImportBatch batchForThisFile = null;

                    try
                    {
                        file.Status = "Processing...";
                        file.Progress = 0;
                        file.ProcessedReceiptCount = null; // Reset counts for the file
                        file.TotalReceiptCount = null;

                        if (!await _fileImportService.ValidateFileFormatAsync(file.FilePath))
                        {
                            file.Status = "Invalid format";
                            Errors.Add($"Invalid file format: {System.IO.Path.GetFileName(file.FilePath)}");
                            overallFailedFiles++;
                            continue;
                        }

                        // Use smart import with duplicate detection and conflict resolution
                        var smartImportResult = await _importBatchProcessor.StartSmartImportAsync(SelectedDepotId ?? 0, file.FilePath);
                        
                        if (!smartImportResult.Success)
                        {
                            file.Status = "Import failed";
                            Errors.Add($"Smart import failed for {System.IO.Path.GetFileName(file.FilePath)}: {smartImportResult.Message}");
                            overallFailedFiles++;
                            continue;
                        }

                        _currentBatch = smartImportResult.ImportBatches.FirstOrDefault(); // Track the first batch for UI

                        // Calculate totals from smart import results
                        var totalReceiptsInFile = smartImportResult.ImportResults.Sum(r => r.TotalReceipts);
                        var processedReceiptsInFile = smartImportResult.ImportResults.Sum(r => r.ImportedReceipts.Count + r.UpdatedReceipts.Count);
                        var fileHadErrors = false;

                        file.TotalReceiptCount = totalReceiptsInFile;
                        file.ProcessedReceiptCount = processedReceiptsInFile;

                        // Update progress
                        file.Progress = 100;
                        var overallProgress = (int)((overallProcessedFiles + 1) * 100.0 / totalFiles);
                        Progress = overallProgress;
                        
                        StatusMessage = $"Smart import completed for {System.IO.Path.GetFileName(file.FilePath)}: {processedReceiptsInFile} receipts processed";
                        
                        // Log conflicts if any
                        if (smartImportResult.Conflicts.Any())
                        {
                            foreach (var conflict in smartImportResult.Conflicts)
                            {
                                Logger.Info($"Batch conflict resolved for {conflict.BatchNumber}: {conflict.Summary}");
                            }
                        }

                        // Log import results
                        foreach (var importResult in smartImportResult.ImportResults)
                        {
                            if (!string.IsNullOrEmpty(importResult.Message))
                            {
                                Logger.Info($"Import result: {importResult.Message}");
                            }
                        }

                        file.Status = "Completed";
                        overallProcessedFiles++;
                    }
                    catch (ImportValidationException ex) {
                        Logger.Error($"Validation error for batch related to file {file.FilePath}", ex);
                        file.Status = "Batch validation error";
                        foreach (var error in ex.ValidationErrors) { Errors.Add($"{System.IO.Path.GetFileName(file.FilePath)}: Batch Error - {error}"); }
                        overallFailedFiles++;
                        if (batchForThisFile != null) await _importBatchProcessor.RollbackBatchAsync(batchForThisFile);
                    }
                    catch (Exception ex) {
                        Logger.Error($"Error processing file {file.FilePath}", ex);
                        file.Status = "Error";
                        Errors.Add($"Error processing file {System.IO.Path.GetFileName(file.FilePath)}: {ex.Message}");
                        overallFailedFiles++;
                        if (batchForThisFile != null) await _importBatchProcessor.RollbackBatchAsync(batchForThisFile);
                    }
                    Progress = (int)(((overallProcessedFiles + overallFailedFiles) * 100.0) / totalFiles);
                } // End file loop

                StatusMessage = $"Import process finished. {overallProcessedFiles} file(s) processed successfully, {overallFailedFiles} file(s) had errors.";
                 ((RelayCommand)RevertImportCommand).RaiseCanExecuteChanged(); // Update revert button state
            }
            catch (Exception ex) {
                Logger.Error("Error during import process", ex);
                StatusMessage = $"Error during import: {ex.Message}";
                Errors.Add($"Import failed: {ex.Message}");
            }
            finally {
                IsImporting = false;
            }
        }

        private void CancelImport()
        {
            // TODO: Implement CancellationToken for true cancellation
            if (_currentBatch != null)
            {
                // Consider asking for confirmation
                _importBatchProcessor.RollbackBatchAsync(_currentBatch).ConfigureAwait(false);
                StatusMessage = $"Import cancelled. Batch {_currentBatch.BatchNumber} rolled back.";
                _currentBatch = null;
                 ((RelayCommand)RevertImportCommand).RaiseCanExecuteChanged();
            } else {
                StatusMessage = "Import cancelled.";
            }
            IsImporting = false;
            foreach(var file in SelectedFiles) { if(file.Status == "Processing...") file.Status = "Cancelled"; }
        }

        private async Task RevertLastImportAsync()
        {
            if (_currentBatch == null) // Check _currentBatch which stores the last processed batch
            {
                await _dialogService.ShowMessageBoxAsync("No import batch available to revert.", "Revert Failed");
                return;
            }

            // TODO: Implement proper confirmation dialog in IDialogService
            await _dialogService.ShowMessageBoxAsync($"Revert all batches from file {System.IO.Path.GetFileName(_currentBatch.SourceFileName)}? This will revert all batches that were imported from this file.", "Confirm Revert");
            bool confirmed = true; // Placeholder - Replace with actual dialog result

            if (confirmed)
            {
                IsImporting = true; // Use IsImporting to disable buttons during revert
                StatusMessage = $"Reverting all batches from file {System.IO.Path.GetFileName(_currentBatch.SourceFileName)}...";
                try
                {
                    await _importBatchProcessor.RollbackBatchGroupAsync(_currentBatch);
                    StatusMessage = $"All batches from file {System.IO.Path.GetFileName(_currentBatch.SourceFileName)} successfully reverted.";
                    Errors.Clear(); // Clear errors related to the reverted batch

                    // Reset file status in SelectedFiles list
                    var fileInfoToReset = SelectedFiles.FirstOrDefault(f => f.FilePath.EndsWith(_currentBatch.SourceFileName)); // Basic match
                    if (fileInfoToReset != null)
                    {
                        fileInfoToReset.Status = "Reverted";
                        fileInfoToReset.Progress = 0;
                        fileInfoToReset.ProcessedReceiptCount = null; // Clear counts on revert
                        fileInfoToReset.TotalReceiptCount = null;
                    }
                    _currentBatch = null; // Clear the current/last batch info
                    Progress = 0; // Reset progress bar
                     ((RelayCommand)RevertImportCommand).RaiseCanExecuteChanged(); // Update button state
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error reverting batch {_currentBatch.BatchNumber}.";
                    Logger.Error($"Error reverting batch {_currentBatch.BatchNumber}", ex);
                    await _dialogService.ShowMessageBoxAsync($"Failed to revert batch: {ex.Message}", "Revert Error");
                }
                finally
                {
                    IsImporting = false;
                }
            }
        }

        private void SelectAllErrors()
        {
            // This is handled by the ListView's SelectionMode="Extended" and Ctrl+A keyboard input
            // The actual selection is done in the UI, this is just a placeholder for the command binding
        }

        private void CopyErrors()
        {
            if (SelectedErrors != null && SelectedErrors.Any())
            {
                try
                {
                    var errorText = string.Join(Environment.NewLine, SelectedErrors);
                    System.Windows.Clipboard.SetText(errorText);
                    StatusMessage = $"Copied {SelectedErrors.Count} error(s) to clipboard.";
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to copy errors to clipboard", ex);
                    StatusMessage = "Failed to copy errors to clipboard.";
                }
            }
        }

        private bool FilterErrorsPredicate(object item)
        {
            if (item is string error)
            {
                bool fileNameMatch = true;
                bool textMatch = true;

                // Check filename filter (assuming error format "FileName: Error Message")
                if (!string.IsNullOrEmpty(SelectedFileNameFilter) && SelectedFileNameFilter != "All Files") // Assuming "All Files" option might be added
                {
                    // Extract filename from error message if possible
                    var parts = error.Split(':');
                    if (parts.Length > 0)
                    {
                        // Compare extracted filename with selected filter filename
                        // Note: This relies on consistent error formatting and might need adjustment
                        // Ensure SelectedFileNameFilter is just the filename for comparison
                        var selectedFileNameOnly = System.IO.Path.GetFileName(SelectedFileNameFilter);
                        fileNameMatch = parts[0].Trim().EndsWith(selectedFileNameOnly, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        fileNameMatch = false; // Cannot determine filename from error format
                    }
                }

                // Check text filter
                if (!string.IsNullOrEmpty(ErrorTextFilter))
                {
                    textMatch = error.IndexOf(ErrorTextFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                return fileNameMatch && textMatch;
            }
            return false;
        }
    } // End of ImportViewModel class


    // Inherit from ViewModelBase
    public class ImportFileInfo : ViewModelBase
    {
        private string _filePath;
        private string _status;
        private int _progress;
        private int? _processedReceiptCount;
        private int? _totalReceiptCount; // Added total count

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public int? ProcessedReceiptCount
        {
            get => _processedReceiptCount;
            set => SetProperty(ref _processedReceiptCount, value);
        }
        public int? TotalReceiptCount // Added property
        {
            get => _totalReceiptCount;
            set => SetProperty(ref _totalReceiptCount, value);
        }
    }
}
