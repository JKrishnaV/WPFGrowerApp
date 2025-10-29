using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents export options for the Payment Summary Report.
    /// Contains configuration settings for different export formats.
    /// </summary>
    public class ExportOptions : INotifyPropertyChanged
    {
        // ======================================================================
        // EXPORT FORMAT
        // ======================================================================
        
        private string _exportFormat = "PDF";
        private string _fileName = string.Empty;
        private string _filePath = string.Empty;
        private bool _includeCharts = true;
        private bool _includeSummaryStatistics = true;
        private bool _includeDetailedData = true;
        private bool _includeContactInfo = true;
        private bool _includeProductDetails = true;
        private bool _includeAuditInfo = true;

        public string ExportFormat
        {
            get => _exportFormat;
            set
            {
                if (_exportFormat != value)
                {
                    _exportFormat = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeCharts
        {
            get => _includeCharts;
            set
            {
                if (_includeCharts != value)
                {
                    _includeCharts = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeSummaryStatistics
        {
            get => _includeSummaryStatistics;
            set
            {
                if (_includeSummaryStatistics != value)
                {
                    _includeSummaryStatistics = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeDetailedData
        {
            get => _includeDetailedData;
            set
            {
                if (_includeDetailedData != value)
                {
                    _includeDetailedData = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeContactInfo
        {
            get => _includeContactInfo;
            set
            {
                if (_includeContactInfo != value)
                {
                    _includeContactInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeProductDetails
        {
            get => _includeProductDetails;
            set
            {
                if (_includeProductDetails != value)
                {
                    _includeProductDetails = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeAuditInfo
        {
            get => _includeAuditInfo;
            set
            {
                if (_includeAuditInfo != value)
                {
                    _includeAuditInfo = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // PDF SPECIFIC OPTIONS
        // ======================================================================
        
        private string _pdfTitle = string.Empty;
        private string _pdfAuthor = string.Empty;
        private string _pdfSubject = string.Empty;
        private bool _pdfPasswordProtected;
        private string _pdfPassword = string.Empty;
        private bool _pdfIncludeWatermark;
        private string _pdfWatermarkText = string.Empty;
        private string _pdfPageSize = "A4";
        private string _pdfOrientation = "Portrait";

        public string PdfTitle
        {
            get => _pdfTitle;
            set
            {
                if (_pdfTitle != value)
                {
                    _pdfTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PdfAuthor
        {
            get => _pdfAuthor;
            set
            {
                if (_pdfAuthor != value)
                {
                    _pdfAuthor = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PdfSubject
        {
            get => _pdfSubject;
            set
            {
                if (_pdfSubject != value)
                {
                    _pdfSubject = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PdfPasswordProtected
        {
            get => _pdfPasswordProtected;
            set
            {
                if (_pdfPasswordProtected != value)
                {
                    _pdfPasswordProtected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PdfPassword
        {
            get => _pdfPassword;
            set
            {
                if (_pdfPassword != value)
                {
                    _pdfPassword = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool PdfIncludeWatermark
        {
            get => _pdfIncludeWatermark;
            set
            {
                if (_pdfIncludeWatermark != value)
                {
                    _pdfIncludeWatermark = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PdfWatermarkText
        {
            get => _pdfWatermarkText;
            set
            {
                if (_pdfWatermarkText != value)
                {
                    _pdfWatermarkText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PdfPageSize
        {
            get => _pdfPageSize;
            set
            {
                if (_pdfPageSize != value)
                {
                    _pdfPageSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public string PdfOrientation
        {
            get => _pdfOrientation;
            set
            {
                if (_pdfOrientation != value)
                {
                    _pdfOrientation = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // EXCEL SPECIFIC OPTIONS
        // ======================================================================
        
        private bool _excelIncludeMultipleSheets = true;
        private bool _excelIncludePivotTables = true;
        private bool _excelIncludeCharts = true;
        private bool _excelAutoFitColumns = true;
        private string _excelSheetName = "Payment Summary";

        public bool ExcelIncludeMultipleSheets
        {
            get => _excelIncludeMultipleSheets;
            set
            {
                if (_excelIncludeMultipleSheets != value)
                {
                    _excelIncludeMultipleSheets = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ExcelIncludePivotTables
        {
            get => _excelIncludePivotTables;
            set
            {
                if (_excelIncludePivotTables != value)
                {
                    _excelIncludePivotTables = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ExcelIncludeCharts
        {
            get => _excelIncludeCharts;
            set
            {
                if (_excelIncludeCharts != value)
                {
                    _excelIncludeCharts = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ExcelAutoFitColumns
        {
            get => _excelAutoFitColumns;
            set
            {
                if (_excelAutoFitColumns != value)
                {
                    _excelAutoFitColumns = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExcelSheetName
        {
            get => _excelSheetName;
            set
            {
                if (_excelSheetName != value)
                {
                    _excelSheetName = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // CSV SPECIFIC OPTIONS
        // ======================================================================
        
        private string _csvDelimiter = ",";
        private bool _csvIncludeHeaders = true;
        private string _csvEncoding = "UTF-8";
        private bool _csvQuoteAllFields;

        public string CsvDelimiter
        {
            get => _csvDelimiter;
            set
            {
                if (_csvDelimiter != value)
                {
                    _csvDelimiter = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CsvIncludeHeaders
        {
            get => _csvIncludeHeaders;
            set
            {
                if (_csvIncludeHeaders != value)
                {
                    _csvIncludeHeaders = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CsvEncoding
        {
            get => _csvEncoding;
            set
            {
                if (_csvEncoding != value)
                {
                    _csvEncoding = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CsvQuoteAllFields
        {
            get => _csvQuoteAllFields;
            set
            {
                if (_csvQuoteAllFields != value)
                {
                    _csvQuoteAllFields = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // WORD SPECIFIC OPTIONS
        // ======================================================================
        
        private string _wordTemplate = string.Empty;
        private bool _wordIncludeCharts = true;
        private bool _wordIncludeTables = true;
        private string _wordDocumentTitle = string.Empty;

        public string WordTemplate
        {
            get => _wordTemplate;
            set
            {
                if (_wordTemplate != value)
                {
                    _wordTemplate = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool WordIncludeCharts
        {
            get => _wordIncludeCharts;
            set
            {
                if (_wordIncludeCharts != value)
                {
                    _wordIncludeCharts = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool WordIncludeTables
        {
            get => _wordIncludeTables;
            set
            {
                if (_wordIncludeTables != value)
                {
                    _wordIncludeTables = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WordDocumentTitle
        {
            get => _wordDocumentTitle;
            set
            {
                if (_wordDocumentTitle != value)
                {
                    _wordDocumentTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // GENERAL OPTIONS
        // ======================================================================
        
        private bool _openAfterExport = true;
        private bool _showExportProgress = true;
        private string _exportDescription = string.Empty;

        public bool OpenAfterExport
        {
            get => _openAfterExport;
            set
            {
                if (_openAfterExport != value)
                {
                    _openAfterExport = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool ShowExportProgress
        {
            get => _showExportProgress;
            set
            {
                if (_showExportProgress != value)
                {
                    _showExportProgress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExportDescription
        {
            get => _exportDescription;
            set
            {
                if (_exportDescription != value)
                {
                    _exportDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // HELPER METHODS
        // ======================================================================
        
        public string GetFullFilePath()
        {
            if (string.IsNullOrEmpty(FilePath) || string.IsNullOrEmpty(FileName))
                return string.Empty;
                
            return System.IO.Path.Combine(FilePath, FileName);
        }
        
        public void SetDefaultFileName(string baseName)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var extension = GetFileExtension();
            FileName = $"{baseName}_{timestamp}{extension}";
        }
        
        public string GetFileExtension()
        {
            return ExportFormat.ToUpper() switch
            {
                "PDF" => ".pdf",
                "EXCEL" => ".xlsx",
                "CSV" => ".csv",
                "WORD" => ".docx",
                _ => ".txt"
            };
        }
        
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(ExportFormat))
                return false;
                
            if (string.IsNullOrEmpty(FileName))
                return false;
                
            if (PdfPasswordProtected && string.IsNullOrEmpty(PdfPassword))
                return false;
                
            return true;
        }

        // ======================================================================
        // INotifyPropertyChanged Implementation
        // ======================================================================
        
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
