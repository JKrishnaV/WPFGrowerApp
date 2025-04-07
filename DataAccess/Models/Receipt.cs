﻿using System;
using System.Collections.Generic; // Added for List
using System.ComponentModel;
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

        // Existing properties...
        public string Depot { get => _depot; set => SetProperty(ref _depot, value); }
        public string Product { get => _product; set => SetProperty(ref _product, value); }
        public decimal ReceiptNumber { get => _receiptNumber; set => SetProperty(ref _receiptNumber, value); }
        public decimal GrowerNumber { get => _growerNumber; set => SetProperty(ref _growerNumber, value); }
        public decimal Gross { get => _gross; set => SetProperty(ref _gross, value); }
        public decimal Tare { get => _tare; set => SetProperty(ref _tare, value); }
        public decimal Net { get => _net; set => SetProperty(ref _net, value); }
        public decimal Grade { get => _grade; set => SetProperty(ref _grade, value); }
        public string Process { get => _process; set => SetProperty(ref _process, value); }
        public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
        public decimal DayUniq { get => _dayUniq; set => SetProperty(ref _dayUniq, value); }
        public decimal ImpBatch { get => _impBatch; set => SetProperty(ref _impBatch, value); }
        public decimal FinBatch { get => _finBatch; set => SetProperty(ref _finBatch, value); }
        public decimal DockPercent { get => _dockPercent; set => SetProperty(ref _dockPercent, value); }
        public bool IsVoid { get => _isVoid; set => SetProperty(ref _isVoid, value); }
        public decimal ThePrice { get => _thePrice; set => SetProperty(ref _thePrice, value); }
        public decimal PriceSource { get => _priceSource; set => SetProperty(ref _priceSource, value); }
        public string PrNote1 { get => _prNote1; set => SetProperty(ref _prNote1, value); }
        public string NpNote1 { get => _npNote1; set => SetProperty(ref _npNote1, value); }
        public string FromField { get => _fromField; set => SetProperty(ref _fromField, value); }
        public bool Imported { get => _imported; set => SetProperty(ref _imported, value); }
        public string ContainerErrors { get => _containerErrors; set => SetProperty(ref _containerErrors, value); }

        // New Properties from CSV/Derived
        public string TimeIn { get => _timeIn; set => SetProperty(ref _timeIn, value); }
        public string GradeId { get => _gradeId; set => SetProperty(ref _gradeId, value); }
        public string Voided { get => _voided; set => SetProperty(ref _voided, value); }
        public DateTime? AddDate { get => _addDate; set => SetProperty(ref _addDate, value); }
        public string AddBy { get => _addBy; set => SetProperty(ref _addBy, value); }
        public DateTime? EditDate { get => _editDate; set => SetProperty(ref _editDate, value); }
        public string EditBy { get => _editBy; set => SetProperty(ref _editBy, value); }
        public string EditReason { get => _editReason; set => SetProperty(ref _editReason, value); }
        public List<ContainerInfo> ContainerData { get => _containerData; set => SetProperty(ref _containerData, value); }

        // Payment Processing Properties
        public decimal? AdvPr1 { get => _advPr1; set => SetProperty(ref _advPr1, value); }
        public decimal? AdvPrid1 { get => _advPrid1; set => SetProperty(ref _advPrid1, value); }
        public decimal? PostBat1 { get => _postBat1; set => SetProperty(ref _postBat1, value); }
        public decimal? AdvPr2 { get => _advPr2; set => SetProperty(ref _advPr2, value); }
        public decimal? AdvPrid2 { get => _advPrid2; set => SetProperty(ref _advPrid2, value); }
        public decimal? PostBat2 { get => _postBat2; set => SetProperty(ref _postBat2, value); }
        public decimal? AdvPr3 { get => _advPr3; set => SetProperty(ref _advPr3, value); }
        public decimal? AdvPrid3 { get => _advPrid3; set => SetProperty(ref _advPrid3, value); }
        public decimal? PostBat3 { get => _postBat3; set => SetProperty(ref _postBat3, value); }
        public decimal? PremPrice { get => _premPrice; set => SetProperty(ref _premPrice, value); }
        public decimal? LastAdvpb { get => _lastAdvpb; set => SetProperty(ref _lastAdvpb, value); }

        // Added Properties for Missing Daily Columns
        public decimal? OriNet { get => _oriNet; set => SetProperty(ref _oriNet, value); }
        public string Certified { get => _certified; set => SetProperty(ref _certified, value); }
        public string Variety { get => _variety; set => SetProperty(ref _variety, value); }
        public string Time { get => _time; set => SetProperty(ref _time, value); } // Maps to TIME column
        public decimal? FinPrice { get => _finPrice; set => SetProperty(ref _finPrice, value); }
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
