using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents the main Payment Summary Report data model.
    /// Contains comprehensive financial information for growers and payment analysis.
    /// </summary>
    public class PaymentSummaryReport : INotifyPropertyChanged
    {
        // ======================================================================
        // REPORT METADATA
        // ======================================================================
        
        private DateTime _reportDate;
        private DateTime _periodStart;
        private DateTime _periodEnd;
        private string _reportTitle = string.Empty;
        private string _generatedBy = string.Empty;
        private string _reportDescription = string.Empty;

        public DateTime ReportDate
        {
            get => _reportDate;
            set
            {
                if (_reportDate != value)
                {
                    _reportDate = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public string ReportTitle
        {
            get => _reportTitle;
            set
            {
                if (_reportTitle != value)
                {
                    _reportTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GeneratedBy
        {
            get => _generatedBy;
            set
            {
                if (_generatedBy != value)
                {
                    _generatedBy = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ReportDescription
        {
            get => _reportDescription;
            set
            {
                if (_reportDescription != value)
                {
                    _reportDescription = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // SUMMARY STATISTICS
        // ======================================================================
        
        private int _totalGrowers;
        private decimal _totalReceiptsValue;
        private decimal _totalPaymentsMade;
        private decimal _outstandingBalance;
        private decimal _averagePaymentPerGrower;
        private int _totalReceipts;
        private decimal _totalWeight;

        public int TotalGrowers
        {
            get => _totalGrowers;
            set
            {
                if (_totalGrowers != value)
                {
                    _totalGrowers = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalReceiptsValue
        {
            get => _totalReceiptsValue;
            set
            {
                if (_totalReceiptsValue != value)
                {
                    _totalReceiptsValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalPaymentsMade
        {
            get => _totalPaymentsMade;
            set
            {
                if (_totalPaymentsMade != value)
                {
                    _totalPaymentsMade = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal OutstandingBalance
        {
            get => _outstandingBalance;
            set
            {
                if (_outstandingBalance != value)
                {
                    _outstandingBalance = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AveragePaymentPerGrower
        {
            get => _averagePaymentPerGrower;
            set
            {
                if (_averagePaymentPerGrower != value)
                {
                    _averagePaymentPerGrower = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalReceipts
        {
            get => _totalReceipts;
            set
            {
                if (_totalReceipts != value)
                {
                    _totalReceipts = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalWeight
        {
            get => _totalWeight;
            set
            {
                if (_totalWeight != value)
                {
                    _totalWeight = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // PAYMENT BREAKDOWN
        // ======================================================================
        
        private decimal _advance1Total;
        private decimal _advance2Total;
        private decimal _advance3Total;
        private decimal _finalPaymentTotal;
        private decimal _totalDeductions;
        private decimal _premiumTotal;

        // ======================================================================
        // ADVANCE CHEQUES & DEDUCTIONS STATISTICS
        // ======================================================================
        
        private int _totalAdvanceCheques;
        private decimal _totalAdvanceChequesAmount;
        private decimal _totalAdvanceChequesOutstanding;
        private int _totalAdvanceDeductions;
        private decimal _totalAdvanceDeductionsAmount;
        private int _activeAdvanceCheques;
        private int _fullyDeductedAdvanceCheques;

        public decimal Advance1Total
        {
            get => _advance1Total;
            set
            {
                if (_advance1Total != value)
                {
                    _advance1Total = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance2Total
        {
            get => _advance2Total;
            set
            {
                if (_advance2Total != value)
                {
                    _advance2Total = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance3Total
        {
            get => _advance3Total;
            set
            {
                if (_advance3Total != value)
                {
                    _advance3Total = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal FinalPaymentTotal
        {
            get => _finalPaymentTotal;
            set
            {
                if (_finalPaymentTotal != value)
                {
                    _finalPaymentTotal = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalDeductions
        {
            get => _totalDeductions;
            set
            {
                if (_totalDeductions != value)
                {
                    _totalDeductions = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal PremiumTotal
        {
            get => _premiumTotal;
            set
            {
                if (_premiumTotal != value)
                {
                    _premiumTotal = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // ADVANCE CHEQUES & DEDUCTIONS PROPERTIES
        // ======================================================================

        public int TotalAdvanceCheques
        {
            get => _totalAdvanceCheques;
            set
            {
                if (_totalAdvanceCheques != value)
                {
                    _totalAdvanceCheques = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalAdvanceChequesAmount
        {
            get => _totalAdvanceChequesAmount;
            set
            {
                if (_totalAdvanceChequesAmount != value)
                {
                    _totalAdvanceChequesAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalAdvanceChequesOutstanding
        {
            get => _totalAdvanceChequesOutstanding;
            set
            {
                if (_totalAdvanceChequesOutstanding != value)
                {
                    _totalAdvanceChequesOutstanding = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalAdvanceDeductions
        {
            get => _totalAdvanceDeductions;
            set
            {
                if (_totalAdvanceDeductions != value)
                {
                    _totalAdvanceDeductions = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalAdvanceDeductionsAmount
        {
            get => _totalAdvanceDeductionsAmount;
            set
            {
                if (_totalAdvanceDeductionsAmount != value)
                {
                    _totalAdvanceDeductionsAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ActiveAdvanceCheques
        {
            get => _activeAdvanceCheques;
            set
            {
                if (_activeAdvanceCheques != value)
                {
                    _activeAdvanceCheques = value;
                    OnPropertyChanged();
                }
            }
        }

        public int FullyDeductedAdvanceCheques
        {
            get => _fullyDeductedAdvanceCheques;
            set
            {
                if (_fullyDeductedAdvanceCheques != value)
                {
                    _fullyDeductedAdvanceCheques = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // GROWER DETAILS AND CHART DATA
        // ======================================================================
        
        private List<GrowerPaymentDetail> _growerDetails = new();
        private List<PaymentDistributionChart> _paymentDistribution = new();
        private List<MonthlyTrendChart> _monthlyTrends = new();
        private List<GrowerPerformanceChart> _topPerformers = new();

        public List<GrowerPaymentDetail> GrowerDetails
        {
            get => _growerDetails;
            set
            {
                if (_growerDetails != value)
                {
                    _growerDetails = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<PaymentDistributionChart> PaymentDistribution
        {
            get => _paymentDistribution;
            set
            {
                if (_paymentDistribution != value)
                {
                    _paymentDistribution = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<MonthlyTrendChart> MonthlyTrends
        {
            get => _monthlyTrends;
            set
            {
                if (_monthlyTrends != value)
                {
                    _monthlyTrends = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<GrowerPerformanceChart> TopPerformers
        {
            get => _topPerformers;
            set
            {
                if (_topPerformers != value)
                {
                    _topPerformers = value;
                    OnPropertyChanged();
                }
            }
        }

        // ======================================================================
        // CALCULATED PROPERTIES
        // ======================================================================
        
        public decimal TotalAdvancesPaid => Advance1Total + Advance2Total + Advance3Total;
        
        public decimal PaymentCompletionPercentage => TotalReceiptsValue > 0 
            ? (TotalPaymentsMade / TotalReceiptsValue) * 100 
            : 0;

        // Advance Cheques Calculated Properties
        public decimal AdvanceChequesRecoveryPercentage => TotalAdvanceChequesAmount > 0 
            ? (TotalAdvanceDeductionsAmount / TotalAdvanceChequesAmount) * 100 
            : 0;

        public decimal NetAdvanceChequesOutstanding => TotalAdvanceChequesAmount - TotalAdvanceDeductionsAmount;

        public string PeriodDisplay => $"{PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}";
        
        public string ReportSummary => $"Report for {TotalGrowers} growers covering {PeriodDisplay}";

        // ======================================================================
        // DISPLAY FORMATTING
        // ======================================================================
        
        public string TotalReceiptsValueDisplay => $"{TotalReceiptsValue:C2}";
        public string TotalPaymentsMadeDisplay => $"{TotalPaymentsMade:C2}";
        public string OutstandingBalanceDisplay => $"{OutstandingBalance:C2}";
        public string AveragePaymentPerGrowerDisplay => $"{AveragePaymentPerGrower:C2}";
        public string TotalWeightDisplay => $"{TotalWeight:N2} lbs";

        public string Advance1TotalDisplay => $"{Advance1Total:C2}";
        public string Advance2TotalDisplay => $"{Advance2Total:C2}";
        public string Advance3TotalDisplay => $"{Advance3Total:C2}";
        public string FinalPaymentTotalDisplay => $"{FinalPaymentTotal:C2}";
        public string TotalDeductionsDisplay => $"{TotalDeductions:C2}";
        public string PremiumTotalDisplay => $"{PremiumTotal:C2}";

        // Advance Cheques Display Formatting
        public string TotalAdvanceChequesAmountDisplay => $"{TotalAdvanceChequesAmount:C2}";
        public string TotalAdvanceChequesOutstandingDisplay => $"{TotalAdvanceChequesOutstanding:C2}";
        public string TotalAdvanceDeductionsAmountDisplay => $"{TotalAdvanceDeductionsAmount:C2}";
        public string NetAdvanceChequesOutstandingDisplay => $"{NetAdvanceChequesOutstanding:C2}";
        public string AdvanceChequesRecoveryPercentageDisplay => $"{AdvanceChequesRecoveryPercentage:F1}%";

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
