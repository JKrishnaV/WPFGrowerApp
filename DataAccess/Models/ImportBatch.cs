using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    public class ImportBatch : INotifyPropertyChanged
    {
        // Primary Key
        private int _importBatchId;
        
        // Core Properties (matching database schema)
        private string _batchNumber = string.Empty;
        private DateTime _importDate;
        private int _depotId;
        private int _totalReceipts;
        private decimal _totalGrossWeight;
        private decimal _totalNetWeight;
        private string _status = string.Empty;
        
        // Audit Fields
        private DateTime _importedAt;
        private string? _importedBy;
        private string? _notes;
        private DateTime _createdAt;
        private string _createdBy = string.Empty;
        private DateTime? _modifiedAt;
        private string? _modifiedBy;
        private DateTime? _deletedAt;
        private string? _deletedBy;
        
        // Multi-Batch Support
        private string? _originalBatchNumber;
        private string? _sourceFileName;
        private bool _isFromMultiBatchFile;
        private int? _batchGroupId;
        
        // Additional properties for compatibility
        private int _noTrans;
        private int _voids;
        private int _lowId;
        private int _highId;
        private string _lowReceipt = string.Empty;
        private string _highReceipt = string.Empty;
        private DateTime _lowDate;
        private DateTime _highDate;

        // Modern property - ImportBatchId (primary key in ImportBatches table)
        public int ImportBatchId
        {
            get => _importBatchId;
            set => SetProperty(ref _importBatchId, value);
        }

        // Core Properties
        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public DateTime ImportDate
        {
            get => _importDate;
            set => SetProperty(ref _importDate, value);
        }

        public int DepotId
        {
            get => _depotId;
            set => SetProperty(ref _depotId, value);
        }

        public int TotalReceipts
        {
            get => _totalReceipts;
            set => SetProperty(ref _totalReceipts, value);
        }

        public decimal TotalGrossWeight
        {
            get => _totalGrossWeight;
            set => SetProperty(ref _totalGrossWeight, value);
        }

        public decimal TotalNetWeight
        {
            get => _totalNetWeight;
            set => SetProperty(ref _totalNetWeight, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        // Audit Fields
        public DateTime ImportedAt
        {
            get => _importedAt;
            set => SetProperty(ref _importedAt, value);
        }

        public string? ImportedBy
        {
            get => _importedBy;
            set => SetProperty(ref _importedBy, value);
        }

        public string? Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public string CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public DateTime? ModifiedAt
        {
            get => _modifiedAt;
            set => SetProperty(ref _modifiedAt, value);
        }

        public string? ModifiedBy
        {
            get => _modifiedBy;
            set => SetProperty(ref _modifiedBy, value);
        }

        public DateTime? DeletedAt
        {
            get => _deletedAt;
            set => SetProperty(ref _deletedAt, value);
        }

        public string? DeletedBy
        {
            get => _deletedBy;
            set => SetProperty(ref _deletedBy, value);
        }

        public string? OriginalBatchNumber
        {
            get => _originalBatchNumber;
            set => SetProperty(ref _originalBatchNumber, value);
        }

        public string? SourceFileName
        {
            get => _sourceFileName;
            set => SetProperty(ref _sourceFileName, value);
        }

        public bool IsFromMultiBatchFile
        {
            get => _isFromMultiBatchFile;
            set => SetProperty(ref _isFromMultiBatchFile, value);
        }

        public int? BatchGroupId
        {
            get => _batchGroupId;
            set => SetProperty(ref _batchGroupId, value);
        }
        
        // Additional properties for compatibility
        public int NoTrans
        {
            get => _noTrans;
            set => SetProperty(ref _noTrans, value);
        }
        
        public int Voids
        {
            get => _voids;
            set => SetProperty(ref _voids, value);
        }
        
        public int LowId
        {
            get => _lowId;
            set => SetProperty(ref _lowId, value);
        }
        
        public int HighId
        {
            get => _highId;
            set => SetProperty(ref _highId, value);
        }
        
        public string LowReceipt
        {
            get => _lowReceipt;
            set => SetProperty(ref _lowReceipt, value);
        }
        
        public string HighReceipt
        {
            get => _highReceipt;
            set => SetProperty(ref _highReceipt, value);
        }
        
        public DateTime LowDate
        {
            get => _lowDate;
            set => SetProperty(ref _lowDate, value);
        }
        
        public DateTime HighDate
        {
            get => _highDate;
            set => SetProperty(ref _highDate, value);
        }

        // Navigation property for display purposes
        private string? _depotName;
        [NotMapped]
        public string? DepotName 
        { 
            get => _depotName; 
            set => SetProperty(ref _depotName, value); 
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName!));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}