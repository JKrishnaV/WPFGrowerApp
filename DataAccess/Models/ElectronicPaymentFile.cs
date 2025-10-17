using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents generated electronic payment files (NACHA/ACH format).
    /// Tracks bank files that contain multiple electronic payments.
    /// </summary>
    public class ElectronicPaymentFile : INotifyPropertyChanged
    {
        private int _fileId;
        private string _fileName = string.Empty;
        private string _fileFormat = string.Empty;
        private byte[] _fileContent = Array.Empty<byte>();
        private decimal _totalAmount;
        private int _totalPayments;
        private DateTime _generatedDate;
        private string _generatedBy = string.Empty;
        private string _status = string.Empty;
        private DateTime? _processedDate;
        private string? _processedBy;
        private string? _notes;

        public int FileId
        {
            get => _fileId;
            set => SetProperty(ref _fileId, value);
        }

        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        public string FileFormat
        {
            get => _fileFormat;
            set => SetProperty(ref _fileFormat, value);
        }

        public byte[] FileContent
        {
            get => _fileContent;
            set => SetProperty(ref _fileContent, value);
        }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        public int TotalPayments
        {
            get => _totalPayments;
            set => SetProperty(ref _totalPayments, value);
        }

        public DateTime GeneratedDate
        {
            get => _generatedDate;
            set => SetProperty(ref _generatedDate, value);
        }

        public string GeneratedBy
        {
            get => _generatedBy;
            set => SetProperty(ref _generatedBy, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public DateTime? ProcessedDate
        {
            get => _processedDate;
            set => SetProperty(ref _processedDate, value);
        }

        public string? ProcessedBy
        {
            get => _processedBy;
            set => SetProperty(ref _processedBy, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }


        // Helper properties
        public bool CanBeDownloaded => !string.IsNullOrEmpty(_fileName) && _fileContent.Length > 0;
        public bool CanBeProcessed => Status == "Generated" || Status == "Uploaded";
        public string FileSizeFormatted => FormatFileSize(_fileContent.Length);
        public string StatusDisplay => GetStatusDisplay();

        // Helper methods
        public string GetContentAsString()
        {
            return System.Text.Encoding.UTF8.GetString(_fileContent);
        }

        public byte[] GetContentAsBytes()
        {
            return _fileContent;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GetStatusDisplay()
        {
            return Status switch
            {
                "Generated" => "Ready for Upload",
                "Uploaded" => "Uploaded to Bank",
                "Processed" => "Processed by Bank",
                "Failed" => "Processing Failed",
                _ => Status
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
