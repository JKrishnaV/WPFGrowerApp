﻿using System;
using System.Collections.Generic; // Added for List
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents container information parsed from the import file.
    /// </summary>
    public class ContainerInfo
    {
        public string Type { get; set; }
        public int InCount { get; set; }
        public int OutCount { get; set; }
    }

    public class Receipt : INotifyPropertyChanged
    {
        // ======================================================================
        // MODERN PROPERTIES - Map to Receipts table (33 columns)
        // ======================================================================
        
        private int _receiptIdModern;
        private string _receiptNumberModern;
        private DateTime _receiptDateModern;
        private TimeSpan? _receiptTimeModern;
        
        private int _growerIdModern;
        private int _productIdModern;
        private int _processIdModern;
        private int? _processTypeIdModern;
        private int? _varietyIdModern;
        private int _depotIdModern;
        private int? _containerIdModern;
        
        private decimal _grossWeightModern;
        private decimal _tareWeightModern;
        private decimal _netWeightModern;
        private decimal _dockPercentageModern;
        private decimal _dockWeightModern;
        private decimal _finalWeightModern;
        
        private byte _gradeModern;
        private int _priceClassIdModern;
        private int _priceAreaIdModern;
        
        private bool _isVoidedModern;
        private string _voidedReasonModern;
        private DateTime? _voidedAtModern;
        private string _voidedByModern;
        
        private int? _importBatchIdModern;
        
        private DateTime _createdAtModern;
        private string _createdByModern;
        private DateTime? _modifiedAtModern;
        private string _modifiedByModern;
        private DateTime? _qualityCheckedAtModern;
        private string _qualityCheckedByModern;
        private DateTime? _deletedAtModern;
        private string _deletedByModern;

        // Modern Properties
        public int ReceiptId { get => _receiptIdModern; set => SetProperty(ref _receiptIdModern, value); }
        public string ReceiptNumberModern { get => _receiptNumberModern; set => SetProperty(ref _receiptNumberModern, value); }
        public DateTime ReceiptDate { get => _receiptDateModern; set => SetProperty(ref _receiptDateModern, value); }
        public TimeSpan? ReceiptTime { get => _receiptTimeModern; set => SetProperty(ref _receiptTimeModern, value); }
        
        public int GrowerId { get => _growerIdModern; set => SetProperty(ref _growerIdModern, value); }
        public int ProductId { get => _productIdModern; set => SetProperty(ref _productIdModern, value); }
        public int ProcessId { get => _processIdModern; set => SetProperty(ref _processIdModern, value); }
        public int? ProcessTypeId { get => _processTypeIdModern; set => SetProperty(ref _processTypeIdModern, value); }
        public int? VarietyId { get => _varietyIdModern; set => SetProperty(ref _varietyIdModern, value); }
        public int DepotId { get => _depotIdModern; set => SetProperty(ref _depotIdModern, value); }
        public int? ContainerId { get => _containerIdModern; set => SetProperty(ref _containerIdModern, value); }
        
        public decimal GrossWeight { get => _grossWeightModern; set => SetProperty(ref _grossWeightModern, value); }
        public decimal TareWeight { get => _tareWeightModern; set => SetProperty(ref _tareWeightModern, value); }
        public decimal NetWeight { get => _netWeightModern; set => SetProperty(ref _netWeightModern, value); }
        public decimal DockPercentage { get => _dockPercentageModern; set => SetProperty(ref _dockPercentageModern, value); }
        public decimal DockWeight { get => _dockWeightModern; set => SetProperty(ref _dockWeightModern, value); }
        public decimal FinalWeight { get => _finalWeightModern; set => SetProperty(ref _finalWeightModern, value); }
        
        public byte GradeModern { get => _gradeModern; set => SetProperty(ref _gradeModern, value); }
        public int PriceClassId { get => _priceClassIdModern; set => SetProperty(ref _priceClassIdModern, value); }
        public int PriceAreaId { get => _priceAreaIdModern; set => SetProperty(ref _priceAreaIdModern, value); }
        
        public bool IsVoidedModern { get => _isVoidedModern; set => SetProperty(ref _isVoidedModern, value); }
        public string VoidedReason { get => _voidedReasonModern; set => SetProperty(ref _voidedReasonModern, value); }
        public DateTime? VoidedAt { get => _voidedAtModern; set => SetProperty(ref _voidedAtModern, value); }
        public string VoidedBy { get => _voidedByModern; set => SetProperty(ref _voidedByModern, value); }
        
        public int? ImportBatchId { get => _importBatchIdModern; set => SetProperty(ref _importBatchIdModern, value); }
        
        public DateTime CreatedAt { get => _createdAtModern; set => SetProperty(ref _createdAtModern, value); }
        public string CreatedBy { get => _createdByModern; set => SetProperty(ref _createdByModern, value); }
        public DateTime? ModifiedAt { get => _modifiedAtModern; set => SetProperty(ref _modifiedAtModern, value); }
        public string ModifiedBy { get => _modifiedByModern; set => SetProperty(ref _modifiedByModern, value); }
        public DateTime? QualityCheckedAt { get => _qualityCheckedAtModern; set => SetProperty(ref _qualityCheckedAtModern, value); }
        public string QualityCheckedBy { get => _qualityCheckedByModern; set => SetProperty(ref _qualityCheckedByModern, value); }
        public DateTime? DeletedAt { get => _deletedAtModern; set => SetProperty(ref _deletedAtModern, value); }
        public string DeletedBy { get => _deletedByModern; set => SetProperty(ref _deletedByModern, value); }

        // ======================================================================
        // LEGACY PROPERTIES - For backward compatibility with Daily table
        // These are marked [NotMapped] and will be gradually phased out
        // ======================================================================
        
        // Existing private fields...
        private string _depot;
        private string _product;
        private decimal _receiptNumber;
        private decimal _growerNumber;
        private decimal _gross;
        private decimal _tare;
        private decimal _net;
        private decimal _grade;
        private string _process;
        private DateTime _date;
        private decimal _dayUniq;
        private decimal _impBatch;
        private decimal _finBatch;
        private decimal _dockPercent;
        private bool _isVoid;
        private decimal _thePrice;
        private decimal _priceSource;
        private string _prNote1;
        private string _npNote1;
        private string _fromField;
        private bool _imported;
        private string _containerErrors;

        // Fields related to payment processing (from Daily table)
        private decimal? _advPr1;
        private decimal? _advPrid1;
        private decimal? _postBat1;
        private decimal? _advPr2;
        private decimal? _advPrid2;
        private decimal? _postBat2;
        private decimal? _advPr3;
        private decimal? _advPrid3;
        private decimal? _postBat3;
        private decimal? _premPrice;
        private decimal? _lastAdvpb;

        // Other fields from Daily table
        private decimal? _oriNet;
        private string _certified;
        private string _variety;
        private string _time; // DB Time column (HH:mm)
        private decimal? _finPrice;
        private decimal? _finPrId;
        // Add other missing fields like LONG_PROD, LONG_PROC, INFO_TEMP, RECPTLTR, EDITED, UNIQ_IMBAT if needed


        // New fields from CSV analysis
        private string _timeIn; // Raw time string from CSV
        private string _gradeId; // Raw grade ID string from CSV (e.g., "FR1")
        private string _voided; // Raw voided string from CSV
        private DateTime? _addDate;
        private string _addBy;
        private DateTime? _editDate;
        private string _editBy;
        private string _editReason;
        private List<ContainerInfo> _containerData = new List<ContainerInfo>(); // Container details

        // Legacy properties with [NotMapped] attribute
        [NotMapped]
        public string Depot { get => _depot; set => SetProperty(ref _depot, value); }
        [NotMapped]
        public string Product { get => _product; set => SetProperty(ref _product, value); }
        [NotMapped]
        public decimal ReceiptNumber { get => _receiptNumber; set => SetProperty(ref _receiptNumber, value); }
        [NotMapped]
        public decimal GrowerNumber { get => _growerNumber; set => SetProperty(ref _growerNumber, value); }
        [NotMapped]
        public decimal Gross { get => _gross; set => SetProperty(ref _gross, value); }
        [NotMapped]
        public decimal Tare { get => _tare; set => SetProperty(ref _tare, value); }
        [NotMapped]
        public decimal Net { get => _net; set => SetProperty(ref _net, value); }
        [NotMapped]
        public decimal Grade { get => _grade; set => SetProperty(ref _grade, value); }
        [NotMapped]
        public string Process { get => _process; set => SetProperty(ref _process, value); }
        [NotMapped]
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
        [NotMapped]
        public decimal DayUniq { get => _dayUniq; set => SetProperty(ref _dayUniq, value); }
        [NotMapped]
        public decimal ImpBatch { get => _impBatch; set => SetProperty(ref _impBatch, value); }
        [NotMapped]
        public decimal FinBatch { get => _finBatch; set => SetProperty(ref _finBatch, value); }
        [NotMapped]
        public decimal DockPercent { get => _dockPercent; set => SetProperty(ref _dockPercent, value); }
        [NotMapped]
        public bool IsVoid { get => _isVoid; set => SetProperty(ref _isVoid, value); }
        [NotMapped]
        public decimal ThePrice { get => _thePrice; set => SetProperty(ref _thePrice, value); }
        [NotMapped]
        public decimal PriceSource { get => _priceSource; set => SetProperty(ref _priceSource, value); }
        [NotMapped]
        public string PrNote1 { get => _prNote1; set => SetProperty(ref _prNote1, value); }
        [NotMapped]
        public string NpNote1 { get => _npNote1; set => SetProperty(ref _npNote1, value); }
        [NotMapped]
        public string FromField { get => _fromField; set => SetProperty(ref _fromField, value); }
        [NotMapped]
        public bool Imported { get => _imported; set => SetProperty(ref _imported, value); }
        [NotMapped]
        public string ContainerErrors { get => _containerErrors; set => SetProperty(ref _containerErrors, value); }

        // New Properties from CSV/Derived
        [NotMapped]
        public string TimeIn { get => _timeIn; set => SetProperty(ref _timeIn, value); }
        [NotMapped]
        public string GradeId { get => _gradeId; set => SetProperty(ref _gradeId, value); }
        [NotMapped]
        public string Voided { get => _voided; set => SetProperty(ref _voided, value); }
        [NotMapped]
        public DateTime? AddDate { get => _addDate; set => SetProperty(ref _addDate, value); }
        [NotMapped]
        public string AddBy { get => _addBy; set => SetProperty(ref _addBy, value); }
        [NotMapped]
        public DateTime? EditDate { get => _editDate; set => SetProperty(ref _editDate, value); }
        [NotMapped]
        public string EditBy { get => _editBy; set => SetProperty(ref _editBy, value); }
        [NotMapped]
        public string EditReason { get => _editReason; set => SetProperty(ref _editReason, value); }
        [NotMapped]
        public List<ContainerInfo> ContainerData { get => _containerData; set => SetProperty(ref _containerData, value); }

        // Payment Processing Properties
        [NotMapped]
        public decimal? AdvPr1 { get => _advPr1; set => SetProperty(ref _advPr1, value); }
        [NotMapped]
        public decimal? AdvPrid1 { get => _advPrid1; set => SetProperty(ref _advPrid1, value); }
        [NotMapped]
        public decimal? PostBat1 { get => _postBat1; set => SetProperty(ref _postBat1, value); }
        [NotMapped]
        public decimal? AdvPr2 { get => _advPr2; set => SetProperty(ref _advPr2, value); }
        [NotMapped]
        public decimal? AdvPrid2 { get => _advPrid2; set => SetProperty(ref _advPrid2, value); }
        [NotMapped]
        public decimal? PostBat2 { get => _postBat2; set => SetProperty(ref _postBat2, value); }
        [NotMapped]
        public decimal? AdvPr3 { get => _advPr3; set => SetProperty(ref _advPr3, value); }
        [NotMapped]
        public decimal? AdvPrid3 { get => _advPrid3; set => SetProperty(ref _advPrid3, value); }
        [NotMapped]
        public decimal? PostBat3 { get => _postBat3; set => SetProperty(ref _postBat3, value); }
        [NotMapped]
        public decimal? PremPrice { get => _premPrice; set => SetProperty(ref _premPrice, value); }
        [NotMapped]
        public decimal? LastAdvpb { get => _lastAdvpb; set => SetProperty(ref _lastAdvpb, value); }

        // Added Properties for Missing Daily Columns
        [NotMapped]
        public decimal? OriNet { get => _oriNet; set => SetProperty(ref _oriNet, value); }
        [NotMapped]
        public string Certified { get => _certified; set => SetProperty(ref _certified, value); }
        [NotMapped]
        public string Variety { get => _variety; set => SetProperty(ref _variety, value); }
        [NotMapped]
        public string Time { get => _time; set => SetProperty(ref _time, value); } // Maps to TIME column
        [NotMapped]
        public decimal? FinPrice { get => _finPrice; set => SetProperty(ref _finPrice, value); }
        [NotMapped]
        public decimal? FinPrId { get => _finPrId; set => SetProperty(ref _finPrId, value); }
        // Add other missing properties here if needed


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
