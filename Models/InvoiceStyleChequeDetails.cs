using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing invoice-style cheque calculation details
    /// </summary>
    public class InvoiceStyleChequeDetails : INotifyPropertyChanged
    {
        private ChequeHeaderInfo _header;
        private PaymentSummary _summary;
        private List<PaymentBatchDetail> _paymentBatches;
        private List<AdvancePaymentRun> _advanceRuns;
        private List<DeductionDetail> _deductions;
        private PaymentHistory _history;
        private string _memo;

        public ChequeHeaderInfo Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public PaymentSummary Summary
        {
            get => _summary;
            set => SetProperty(ref _summary, value);
        }

        public List<PaymentBatchDetail> PaymentBatches
        {
            get => _paymentBatches;
            set => SetProperty(ref _paymentBatches, value);
        }

        public List<AdvancePaymentRun> AdvanceRuns
        {
            get => _advanceRuns;
            set => SetProperty(ref _advanceRuns, value);
        }

        public List<DeductionDetail> Deductions
        {
            get => _deductions;
            set => SetProperty(ref _deductions, value);
        }

        public PaymentHistory History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }

        public string Memo
        {
            get => _memo;
            set => SetProperty(ref _memo, value);
        }

        // Display properties
        public string CompanyName => "BERRY FARMS MANAGEMENT SYSTEM";
        public string DocumentTitle => "Cheque Calculation Details";
        public DateTime GeneratedDate => DateTime.Now;

        public InvoiceStyleChequeDetails()
        {
            Header = new ChequeHeaderInfo();
            Summary = new PaymentSummary();
            PaymentBatches = new List<PaymentBatchDetail>();
            AdvanceRuns = new List<AdvancePaymentRun>();
            Deductions = new List<DeductionDetail>();
            History = new PaymentHistory();
            Memo = string.Empty;
        }

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

    /// <summary>
    /// Header information for the invoice-style cheque details
    /// </summary>
    public class ChequeHeaderInfo : INotifyPropertyChanged
    {
        private string _chequeNumber;
        private DateTime _chequeDate;
        private string _growerInfo;
        private string _payeeName;
        private int _fiscalYear;
        private string _status;

        public string ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        public DateTime ChequeDate
        {
            get => _chequeDate;
            set => SetProperty(ref _chequeDate, value);
        }

        public string GrowerInfo
        {
            get => _growerInfo;
            set => SetProperty(ref _growerInfo, value);
        }

        public string PayeeName
        {
            get => _payeeName;
            set => SetProperty(ref _payeeName, value);
        }

        public int FiscalYear
        {
            get => _fiscalYear;
            set => SetProperty(ref _fiscalYear, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        // Display properties
        public string DateDisplay => ChequeDate.ToString("MMMM dd, yyyy");
        public string FiscalYearDisplay => $"Fiscal Year: {FiscalYear}";

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

    /// <summary>
    /// Payment summary information
    /// </summary>
    public class PaymentSummary : INotifyPropertyChanged
    {
        private decimal _totalGrossPayments;
        private decimal _totalDeductions;
        private decimal _netChequeAmount;

        public decimal TotalGrossPayments
        {
            get => _totalGrossPayments;
            set => SetProperty(ref _totalGrossPayments, value);
        }

        public decimal TotalDeductions
        {
            get => _totalDeductions;
            set => SetProperty(ref _totalDeductions, value);
        }

        public decimal NetChequeAmount
        {
            get => _netChequeAmount;
            set => SetProperty(ref _netChequeAmount, value);
        }

        // Display properties
        public string TotalGrossPaymentsDisplay => TotalGrossPayments.ToString("C");
        public string TotalDeductionsDisplay => TotalDeductions.ToString("C");
        public string NetChequeAmountDisplay => NetChequeAmount.ToString("C");

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

    /// <summary>
    /// Payment batch detail information
    /// </summary>
    public class PaymentBatchDetail : INotifyPropertyChanged
    {
        private string _batchNumber;
        private DateTime _batchDate;
        private string _paymentType;
        private List<ReceiptLineItem> _receipts;
        private decimal _batchSubtotal;

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public DateTime BatchDate
        {
            get => _batchDate;
            set => SetProperty(ref _batchDate, value);
        }

        public string PaymentType
        {
            get => _paymentType;
            set => SetProperty(ref _paymentType, value);
        }

        public List<ReceiptLineItem> Receipts
        {
            get => _receipts;
            set => SetProperty(ref _receipts, value);
        }

        public decimal BatchSubtotal
        {
            get => _batchSubtotal;
            set => SetProperty(ref _batchSubtotal, value);
        }

        // Display properties
        public string BatchDateDisplay => BatchDate.ToString("MMMM dd, yyyy");
        public string BatchSubtotalDisplay => BatchSubtotal.ToString("C");
        public int ReceiptCount => Receipts?.Count ?? 0;

        public PaymentBatchDetail()
        {
            Receipts = new List<ReceiptLineItem>();
        }

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

    /// <summary>
    /// Individual receipt line item
    /// </summary>
    public class ReceiptLineItem : INotifyPropertyChanged
    {
        private string _receiptNumber;
        private string _batchNumber;
        private string _productName;
        private string _processName;
        private string _grade;
        private decimal _weight;
        private decimal _pricePerPound;
        private decimal _amount;
        private string _advancePayment;

        public string ReceiptNumber
        {
            get => _receiptNumber;
            set => SetProperty(ref _receiptNumber, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public string ProductName
        {
            get => _productName;
            set => SetProperty(ref _productName, value);
        }

        public string ProcessName
        {
            get => _processName;
            set => SetProperty(ref _processName, value);
        }

        public string Grade
        {
            get => _grade;
            set => SetProperty(ref _grade, value);
        }

        public decimal Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }

        public decimal PricePerPound
        {
            get => _pricePerPound;
            set => SetProperty(ref _pricePerPound, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string AdvancePayment
        {
            get => _advancePayment;
            set => SetProperty(ref _advancePayment, value);
        }

        // Display properties
        public string WeightDisplay => $"{Weight:N2} lbs";
        public string PricePerPoundDisplay => $"{PricePerPound:C2}/lb";
        public string AmountDisplay => Amount.ToString("C");

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

    /// <summary>
    /// Advance payment run information
    /// </summary>
    public class AdvancePaymentRun : INotifyPropertyChanged
    {
        private DateTime _runDate;
        private string _runNumber;
        private decimal _amount;

        public DateTime RunDate
        {
            get => _runDate;
            set => SetProperty(ref _runDate, value);
        }

        public string RunNumber
        {
            get => _runNumber;
            set => SetProperty(ref _runNumber, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        // Display properties
        public string RunDateDisplay => RunDate.ToString("yyyy-MM-dd");
        public string AmountDisplay => Amount.ToString("C");

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

    /// <summary>
    /// Deduction detail information
    /// </summary>
    public class DeductionDetail : INotifyPropertyChanged
    {
        private string _type;
        private string _description;
        private decimal _amount;

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        // Display properties
        public string AmountDisplay => Amount.ToString("C");

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

    /// <summary>
    /// Payment history information
    /// </summary>
    public class PaymentHistory : INotifyPropertyChanged
    {
        private List<PaymentHistoryItem> _payments;
        private decimal _seasonTotal;

        public List<PaymentHistoryItem> Payments
        {
            get => _payments;
            set => SetProperty(ref _payments, value);
        }

        public decimal SeasonTotal
        {
            get => _seasonTotal;
            set => SetProperty(ref _seasonTotal, value);
        }

        // Display properties
        public string SeasonTotalDisplay => SeasonTotal.ToString("C");
        public int PaymentCount => Payments?.Count ?? 0;

        public PaymentHistory()
        {
            Payments = new List<PaymentHistoryItem>();
        }

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

    /// <summary>
    /// Individual payment history item
    /// </summary>
    public class PaymentHistoryItem : INotifyPropertyChanged
    {
        private DateTime _date;
        private string _chequeNumber;
        private string _batchNumber;
        private decimal _amount;

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public string ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        // Display properties
        public string DateDisplay => Date.ToString("yyyy-MM-dd");
        public string AmountDisplay => Amount.ToString("C");

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
