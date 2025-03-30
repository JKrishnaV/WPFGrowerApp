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

namespace WPFGrowerApp.ViewModels
{
    public class ImportViewModel : INotifyPropertyChanged
    {
        private readonly IFileImportService _fileImportService;
        private readonly IImportBatchProcessor _importBatchProcessor;
        private readonly ValidationService _validationService;
        private ObservableCollection<ImportFileInfo> _selectedFiles;
        private string _depot;
        private bool _isImporting;
        private int _progress;
        private string _statusMessage;
        private ObservableCollection<string> _errors;
        private ImportBatch _currentBatch;

        public event PropertyChangedEventHandler PropertyChanged;

        public ImportViewModel(
            IFileImportService fileImportService,
            IImportBatchProcessor importBatchProcessor,
            ValidationService validationService)
        {
            _fileImportService = fileImportService ?? throw new ArgumentNullException(nameof(fileImportService));
            _importBatchProcessor = importBatchProcessor ?? throw new ArgumentNullException(nameof(importBatchProcessor));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _errors = new ObservableCollection<string>();
            _selectedFiles = new ObservableCollection<ImportFileInfo>();
        }

        public ObservableCollection<ImportFileInfo> SelectedFiles
        {
            get => _selectedFiles;
            set
            {
                _selectedFiles = value;
                OnPropertyChanged(nameof(SelectedFiles));
            }
        }

        public string Depot
        {
            get => _depot;
            set
            {
                _depot = value;
                OnPropertyChanged(nameof(Depot));
            }
        }

        public bool IsImporting
        {
            get => _isImporting;
            set
            {
                _isImporting = value;
                OnPropertyChanged(nameof(IsImporting));
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public ObservableCollection<string> Errors
        {
            get => _errors;
            set
            {
                _errors = value;
                OnPropertyChanged(nameof(Errors));
            }
        }

        public ICommand BrowseFilesCommand => new RelayCommand(o => BrowseFiles());
        public ICommand RemoveFileCommand => new RelayCommand(o => RemoveFile((ImportFileInfo)o));
        public ICommand StartImportCommand => new RelayCommand(o => StartImport(), o => !IsImporting && SelectedFiles.Any() && !string.IsNullOrEmpty(Depot));
        public ICommand CancelImportCommand => new RelayCommand(o => CancelImport(), o => IsImporting);

        private void BrowseFiles()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select Receipt Import Files",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var file in dialog.FileNames)
                {
                    if (!SelectedFiles.Any(f => f.FilePath == file))
                    {
                        SelectedFiles.Add(new ImportFileInfo
                        {
                            FilePath = file,
                            Status = "Pending"
                        });
                    }
                }
            }
        }

        private void RemoveFile(ImportFileInfo file)
        {
            if (file != null && !IsImporting)
            {
                SelectedFiles.Remove(file);
            }
        }

        private async void StartImport()
        {
            try
            {
                IsImporting = true;
                Errors.Clear();
                StatusMessage = "Starting import process...";
                Progress = 0;

                var totalFiles = SelectedFiles.Count;
                var processedFiles = 0;
                var failedFiles = 0;

                foreach (var file in SelectedFiles)
                {
                    try
                    {
                        file.Status = "Processing...";
                        file.Progress = 0;

                        // Validate file format
                        if (!await _fileImportService.ValidateFileFormatAsync(file.FilePath))
                        {
                            file.Status = "Invalid format";
                            Errors.Add($"Invalid file format: {System.IO.Path.GetFileName(file.FilePath)}");
                            failedFiles++;
                            continue;
                        }

                        // Start import batch
                        _currentBatch = await _importBatchProcessor.StartImportBatchAsync(
                            Depot, 
                            System.IO.Path.GetFileName(file.FilePath));

                        // Validate import batch
                        await _validationService.ValidateImportBatchAsync(_currentBatch);

                        // Read receipts from file
                        var receipts = await _fileImportService.ReadReceiptsFromFileAsync(
                            file.FilePath,
                            new Progress<int>(p => file.Progress = p),
                            CancellationToken.None);

                        // Validate each receipt
                        var validationErrors = new List<string>();
                        foreach (var receipt in receipts)
                        {
                            try
                            {
                                await _validationService.ValidateReceiptAsync(receipt);
                            }
                            catch (ImportValidationException ex)
                            {
                                validationErrors.AddRange(ex.ValidationErrors);
                            }
                        }

                        if (validationErrors.Any())
                        {
                            file.Status = "Validation failed";
                            foreach (var error in validationErrors)
                            {
                                Errors.Add($"{System.IO.Path.GetFileName(file.FilePath)}: {error}");
                            }
                            failedFiles++;
                            continue;
                        }

                        // Process receipts
                        var (success, processingErrors) = await _importBatchProcessor.ProcessReceiptsAsync(
                            _currentBatch,
                            receipts,
                            new Progress<int>(p => file.Progress = p),
                            CancellationToken.None);

                        if (!success)
                        {
                            file.Status = "Processing failed";
                            foreach (var error in processingErrors)
                            {
                                Errors.Add($"{System.IO.Path.GetFileName(file.FilePath)}: {error}");
                            }
                            failedFiles++;
                            continue;
                        }

                        // Finalize batch
                        await _importBatchProcessor.FinalizeBatchAsync(_currentBatch);

                        file.Status = "Completed";
                        file.Progress = 100;
                        processedFiles++;
                    }
                    catch (ImportValidationException ex)
                    {
                        Logger.Error($"Validation error in file {file.FilePath}", ex);
                        file.Status = "Validation error";
                        foreach (var error in ex.ValidationErrors)
                        {
                            Errors.Add($"{System.IO.Path.GetFileName(file.FilePath)}: {error}");
                        }
                        failedFiles++;
                    }
                    catch (ImportProcessingException ex)
                    {
                        Logger.Error($"Processing error in file {file.FilePath}", ex);
                        file.Status = "Processing error";
                        Errors.Add($"Error processing {System.IO.Path.GetFileName(file.FilePath)}: {ex.Message}");
                        failedFiles++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Unexpected error processing file {file.FilePath}", ex);
                        file.Status = "Error";
                        Errors.Add($"Unexpected error processing {System.IO.Path.GetFileName(file.FilePath)}: {ex.Message}");
                        failedFiles++;
                    }

                    // Update overall progress
                    Progress = (int)((processedFiles * 100.0) / totalFiles);
                }

                StatusMessage = $"Import completed. {processedFiles} of {totalFiles} files processed successfully. {failedFiles} files failed.";
            }
            catch (Exception ex)
            {
                Logger.Error("Error during import process", ex);
                StatusMessage = $"Error during import: {ex.Message}";
                Errors.Add($"Import failed: {ex.Message}");
            }
            finally
            {
                IsImporting = false;
            }
        }

        private void CancelImport()
        {
            if (_currentBatch != null)
            {
                _importBatchProcessor.RollbackBatchAsync(_currentBatch).ConfigureAwait(false);
            }
            IsImporting = false;
            StatusMessage = "Import cancelled.";
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ImportFileInfo : INotifyPropertyChanged
    {
        private string _filePath;
        private string _status;
        private int _progress;

        public event PropertyChangedEventHandler PropertyChanged;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 