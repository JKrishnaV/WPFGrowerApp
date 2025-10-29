using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.DataAccess.Models
{
    /// <summary>
    /// Represents chart data for payment distribution visualization.
    /// Used for pie charts and bar charts showing payment breakdowns.
    /// </summary>
    public class PaymentDistributionChart : INotifyPropertyChanged
    {
        private string _category = string.Empty;
        private decimal _value;
        private string _description = string.Empty;
        private string _color = string.Empty;
        private int _count;
        private decimal _percentage;

        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Percentage
        {
            get => _percentage;
            set
            {
                if (_percentage != value)
                {
                    _percentage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Display formatting
        public string ValueDisplay => $"{Value:C2}";
        public string PercentageDisplay => $"{Percentage:F1}%";
        public string CountDisplay => $"{Count:N0}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents chart data for monthly trend analysis.
    /// Used for line charts showing payment trends over time.
    /// </summary>
    public class MonthlyTrendChart : INotifyPropertyChanged
    {
        private DateTime _month;
        private decimal _totalPayments;
        private decimal _advance1Amount;
        private decimal _advance2Amount;
        private decimal _advance3Amount;
        private decimal _finalPaymentAmount;
        private int _paymentCount;
        private int _growerCount;

        public DateTime Month
        {
            get => _month;
            set
            {
                if (_month != value)
                {
                    _month = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalPayments
        {
            get => _totalPayments;
            set
            {
                if (_totalPayments != value)
                {
                    _totalPayments = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance1Amount
        {
            get => _advance1Amount;
            set
            {
                if (_advance1Amount != value)
                {
                    _advance1Amount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance2Amount
        {
            get => _advance2Amount;
            set
            {
                if (_advance2Amount != value)
                {
                    _advance2Amount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Advance3Amount
        {
            get => _advance3Amount;
            set
            {
                if (_advance3Amount != value)
                {
                    _advance3Amount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal FinalPaymentAmount
        {
            get => _finalPaymentAmount;
            set
            {
                if (_finalPaymentAmount != value)
                {
                    _finalPaymentAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int PaymentCount
        {
            get => _paymentCount;
            set
            {
                if (_paymentCount != value)
                {
                    _paymentCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int GrowerCount
        {
            get => _growerCount;
            set
            {
                if (_growerCount != value)
                {
                    _growerCount = value;
                    OnPropertyChanged();
                }
            }
        }

        // Display formatting
        public string MonthDisplay => Month.ToString("MMM yyyy");
        public string TotalPaymentsDisplay => $"{TotalPayments:C2}";
        public string Advance1AmountDisplay => $"{Advance1Amount:C2}";
        public string Advance2AmountDisplay => $"{Advance2Amount:C2}";
        public string Advance3AmountDisplay => $"{Advance3Amount:C2}";
        public string FinalPaymentAmountDisplay => $"{FinalPaymentAmount:C2}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents chart data for grower performance analysis.
    /// Used for bar charts showing top performing growers.
    /// </summary>
    public class GrowerPerformanceChart : INotifyPropertyChanged
    {
        private int _growerId;
        private string _growerName = string.Empty;
        private string _growerNumber = string.Empty;
        private decimal _totalPayments;
        private decimal _totalReceipts;
        private int _receiptCount;
        private decimal _averagePaymentPerReceipt;
        private string _province = string.Empty;
        private int _rank;

        public int GrowerId
        {
            get => _growerId;
            set
            {
                if (_growerId != value)
                {
                    _growerId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GrowerName
        {
            get => _growerName;
            set
            {
                if (_growerName != value)
                {
                    _growerName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set
            {
                if (_growerNumber != value)
                {
                    _growerNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalPayments
        {
            get => _totalPayments;
            set
            {
                if (_totalPayments != value)
                {
                    _totalPayments = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalReceipts
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

        public int ReceiptCount
        {
            get => _receiptCount;
            set
            {
                if (_receiptCount != value)
                {
                    _receiptCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal AveragePaymentPerReceipt
        {
            get => _averagePaymentPerReceipt;
            set
            {
                if (_averagePaymentPerReceipt != value)
                {
                    _averagePaymentPerReceipt = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Province
        {
            get => _province;
            set
            {
                if (_province != value)
                {
                    _province = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Rank
        {
            get => _rank;
            set
            {
                if (_rank != value)
                {
                    _rank = value;
                    OnPropertyChanged();
                }
            }
        }

        // Display formatting
        public string TotalPaymentsDisplay => $"{TotalPayments:C2}";
        public string TotalReceiptsDisplay => $"{TotalReceipts:C2}";
        public string AveragePaymentPerReceiptDisplay => $"{AveragePaymentPerReceipt:C2}";
        public string RankDisplay => $"#{Rank}";
        public string GrowerDisplayName => $"{GrowerNumber} - {GrowerName}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
