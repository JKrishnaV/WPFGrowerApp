using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents filter options for the Payment Summary Report.
    /// Contains all filtering criteria for data selection and report customization.
    /// </summary>
    public class ReportFilterOptions : INotifyPropertyChanged
    {
        // ======================================================================
        // DATE RANGE FILTERS
        // ======================================================================
        
        private DateTime _periodStart = DateTime.Now.AddMonths(-1);
        private DateTime _periodEnd = DateTime.Now;
        private bool _useCustomDateRange;
        private string _dateRangePreset = "Last Month";

        public DateTime PeriodStart
        {
            get => _periodStart;
            set
            {
                if (_periodStart != value)
                {
                    _periodStart = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime PeriodEnd
        {
            get => _periodEnd;
            set
            {
                if (_periodEnd != value)
                {
                    _periodEnd = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UseCustomDateRange
        {
            get => _useCustomDateRange;
            set
            {
                if (_useCustomDateRange != value)
                {
                    _useCustomDateRange = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DateRangePreset
        {
            get => _dateRangePreset;
            set
            {
                if (_dateRangePreset != value)
                {
                    _dateRangePreset = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // GROWER FILTERS
        // ======================================================================
        
        private List<int> _selectedGrowerIds = new();
        private List<string> _selectedProvinces = new();
        private List<string> _selectedCities = new();
        private List<string> _selectedPaymentGroups = new();
        private bool _includeInactiveGrowers;
        private bool _includeOnHoldGrowers;

        public List<int> SelectedGrowerIds
        {
            get => _selectedGrowerIds;
            set
            {
                if (_selectedGrowerIds != value)
                {
                    _selectedGrowerIds = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> SelectedProvinces
        {
            get => _selectedProvinces;
            set
            {
                if (_selectedProvinces != value)
                {
                    _selectedProvinces = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> SelectedCities
        {
            get => _selectedCities;
            set
            {
                if (_selectedCities != value)
                {
                    _selectedCities = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> SelectedPaymentGroups
        {
            get => _selectedPaymentGroups;
            set
            {
                if (_selectedPaymentGroups != value)
                {
                    _selectedPaymentGroups = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeInactiveGrowers
        {
            get => _includeInactiveGrowers;
            set
            {
                if (_includeInactiveGrowers != value)
                {
                    _includeInactiveGrowers = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeOnHoldGrowers
        {
            get => _includeOnHoldGrowers;
            set
            {
                if (_includeOnHoldGrowers != value)
                {
                    _includeOnHoldGrowers = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // PAYMENT STATUS FILTERS
        // ======================================================================
        
        private List<string> _selectedPaymentStatuses = new();
        private List<string> _selectedPaymentMethods = new();
        private bool _includeZeroBalanceGrowers = true;
        private decimal _minimumPaymentAmount;
        private decimal _maximumPaymentAmount;

        public List<string> SelectedPaymentStatuses
        {
            get => _selectedPaymentStatuses;
            set
            {
                if (_selectedPaymentStatuses != value)
                {
                    _selectedPaymentStatuses = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> SelectedPaymentMethods
        {
            get => _selectedPaymentMethods;
            set
            {
                if (_selectedPaymentMethods != value)
                {
                    _selectedPaymentMethods = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeZeroBalanceGrowers
        {
            get => _includeZeroBalanceGrowers;
            set
            {
                if (_includeZeroBalanceGrowers != value)
                {
                    _includeZeroBalanceGrowers = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal MinimumPaymentAmount
        {
            get => _minimumPaymentAmount;
            set
            {
                if (_minimumPaymentAmount != value)
                {
                    _minimumPaymentAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal MaximumPaymentAmount
        {
            get => _maximumPaymentAmount;
            set
            {
                if (_maximumPaymentAmount != value)
                {
                    _maximumPaymentAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // PRODUCT AND PROCESS FILTERS
        // ======================================================================
        
        private List<int> _selectedProductIds = new();
        private List<int> _selectedProcessIds = new();
        private List<string> _selectedGrades = new();
        private bool _includeAllProducts = true;
        private bool _includeAllProcesses = true;

        public List<int> SelectedProductIds
        {
            get => _selectedProductIds;
            set
            {
                if (_selectedProductIds != value)
                {
                    _selectedProductIds = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<int> SelectedProcessIds
        {
            get => _selectedProcessIds;
            set
            {
                if (_selectedProcessIds != value)
                {
                    _selectedProcessIds = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> SelectedGrades
        {
            get => _selectedGrades;
            set
            {
                if (_selectedGrades != value)
                {
                    _selectedGrades = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeAllProducts
        {
            get => _includeAllProducts;
            set
            {
                if (_includeAllProducts != value)
                {
                    _includeAllProducts = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeAllProcesses
        {
            get => _includeAllProcesses;
            set
            {
                if (_includeAllProcesses != value)
                {
                    _includeAllProcesses = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // REPORT OPTIONS
        // ======================================================================
        
        private bool _includeContactInfo = true;
        private bool _includeProductDetails = true;
        private bool _includeAuditInfo;
        private bool _includeCharts = true;
        private bool _includeSummaryStatistics = true;
        private string _sortBy = "GrowerName";
        private bool _sortAscending = true;
        private int _maxResults = 1000;

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

        public string SortBy
        {
            get => _sortBy;
            set
            {
                if (_sortBy != value)
                {
                    _sortBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                if (_sortAscending != value)
                {
                    _sortAscending = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxResults
        {
            get => _maxResults;
            set
            {
                if (_maxResults != value)
                {
                    _maxResults = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // CALCULATED PROPERTIES
        // ======================================================================
        
        public string DateRangeDisplay => $"{PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}";
        
        public bool HasGrowerFilters => SelectedGrowerIds.Count > 0 || 
                                      SelectedProvinces.Count > 0 || 
                                      SelectedCities.Count > 0 || 
                                      SelectedPaymentGroups.Count > 0;
        
        public bool HasPaymentFilters => SelectedPaymentStatuses.Count > 0 || 
                                        SelectedPaymentMethods.Count > 0 || 
                                        MinimumPaymentAmount > 0 || 
                                        MaximumPaymentAmount > 0;
        
        public bool HasProductFilters => !IncludeAllProducts || 
                                        !IncludeAllProcesses || 
                                        SelectedProductIds.Count > 0 || 
                                        SelectedProcessIds.Count > 0 || 
                                        SelectedGrades.Count > 0;

        // ======================================================================
        // HELPER METHODS
        // ======================================================================
        
        public void SetDateRangePreset(string preset)
        {
            DateRangePreset = preset;
            UseCustomDateRange = false;
            
            var now = DateTime.Now;
            switch (preset)
            {
                case "This Month":
                    PeriodStart = new DateTime(now.Year, now.Month, 1);
                    PeriodEnd = now;
                    break;
                case "Last Month":
                    var lastMonth = now.AddMonths(-1);
                    PeriodStart = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    PeriodEnd = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;
                case "This Quarter":
                    var quarter = (now.Month - 1) / 3;
                    PeriodStart = new DateTime(now.Year, quarter * 3 + 1, 1);
                    PeriodEnd = now;
                    break;
                case "This Year":
                    PeriodStart = new DateTime(now.Year, 1, 1);
                    PeriodEnd = now;
                    break;
                case "Last Year":
                    PeriodStart = new DateTime(now.Year - 1, 1, 1);
                    PeriodEnd = new DateTime(now.Year - 1, 12, 31);
                    break;
                case "Custom":
                    UseCustomDateRange = true;
                    break;
            }
        }
        
        public void ClearAllFilters()
        {
            SelectedGrowerIds.Clear();
            SelectedProvinces.Clear();
            SelectedCities.Clear();
            SelectedPaymentGroups.Clear();
            SelectedPaymentStatuses.Clear();
            SelectedPaymentMethods.Clear();
            SelectedProductIds.Clear();
            SelectedProcessIds.Clear();
            SelectedGrades.Clear();
            
            IncludeInactiveGrowers = false;
            IncludeOnHoldGrowers = false;
            IncludeZeroBalanceGrowers = true;
            IncludeAllProducts = true;
            IncludeAllProcesses = true;
            
            MinimumPaymentAmount = 0;
            MaximumPaymentAmount = 0;
            
            SortBy = "GrowerName";
            SortAscending = true;
            MaxResults = 1000;
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
