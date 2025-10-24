using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Model representing detailed calculation breakdown for cheques
    /// </summary>
    public class ChequeCalculationDetails : INotifyPropertyChanged
    {
        private ChequePaymentType _paymentType;
        private decimal _grossAmount;
        private decimal _totalDeductions;
        private decimal _netAmount;
        private List<CalculationLineItem> _lineItems;
        private List<AdvanceDeduction> _advanceDeductions;
        private List<BatchBreakdown> _batchBreakdowns;
        private string _calculationSummary;

        public ChequePaymentType PaymentType
        {
            get => _paymentType;
            set => SetProperty(ref _paymentType, value);
        }

        public decimal GrossAmount
        {
            get => _grossAmount;
            set => SetProperty(ref _grossAmount, value);
        }

        public decimal TotalDeductions
        {
            get => _totalDeductions;
            set => SetProperty(ref _totalDeductions, value);
        }

        public decimal NetAmount
        {
            get => _netAmount;
            set => SetProperty(ref _netAmount, value);
        }

        public List<CalculationLineItem> LineItems
        {
            get => _lineItems;
            set => SetProperty(ref _lineItems, value);
        }

        public List<AdvanceDeduction> AdvanceDeductions
        {
            get => _advanceDeductions;
            set => SetProperty(ref _advanceDeductions, value);
        }

        public List<BatchBreakdown> BatchBreakdowns
        {
            get => _batchBreakdowns;
            set => SetProperty(ref _batchBreakdowns, value);
        }

        public string CalculationSummary
        {
            get => _calculationSummary;
            set => SetProperty(ref _calculationSummary, value);
        }

        // Display properties
        public string GrossAmountDisplay => GrossAmount.ToString("C");
        public string TotalDeductionsDisplay => TotalDeductions.ToString("C");
        public string NetAmountDisplay => NetAmount.ToString("C");
        public string PaymentTypeDisplay => GetPaymentTypeDisplay();
        public bool HasDeductions => TotalDeductions > 0;
        public bool HasMultipleBatches => BatchBreakdowns?.Count > 1;

        public ChequeCalculationDetails()
        {
            LineItems = new List<CalculationLineItem>();
            AdvanceDeductions = new List<AdvanceDeduction>();
            BatchBreakdowns = new List<BatchBreakdown>();
        }

        public ChequeCalculationDetails(ChequeItem chequeItem) : this()
        {
            PaymentType = chequeItem.PaymentType;
            GrossAmount = CalculateGrossAmount(chequeItem);
            TotalDeductions = chequeItem.TotalDeductions;
            NetAmount = chequeItem.NetAmount;
            AdvanceDeductions = chequeItem.AdvanceDeductions?.ToList() ?? new List<AdvanceDeduction>();
            BatchBreakdowns = chequeItem.BatchBreakdowns?.ToList() ?? new List<BatchBreakdown>();
            
            BuildLineItems();
            BuildCalculationSummary();
        }

        private decimal CalculateGrossAmount(ChequeItem chequeItem)
        {
            // For advance cheques, the amount is the advance amount
            if (chequeItem.PaymentType == ChequePaymentType.Advance)
            {
                return chequeItem.Amount;
            }

            // For regular and consolidated cheques, gross amount includes deductions
            return chequeItem.Amount + chequeItem.TotalDeductions;
        }

        private void BuildLineItems()
        {
            LineItems.Clear();

            switch (PaymentType)
            {
                case ChequePaymentType.Regular:
                    BuildRegularChequeLineItems();
                    break;
                case ChequePaymentType.Advance:
                    BuildAdvanceChequeLineItems();
                    break;
                case ChequePaymentType.Distribution:
                    BuildConsolidatedChequeLineItems();
                    break;
            }
        }

        private void BuildRegularChequeLineItems()
        {
            // Add batch payment amount
            LineItems.Add(new CalculationLineItem
            {
                Description = "Batch Payment Amount",
                Amount = GrossAmount,
                IsSubtotal = false
            });

            // Add advance deductions if any
            if (AdvanceDeductions.Any())
            {
                foreach (var deduction in AdvanceDeductions)
                {
                    LineItems.Add(new CalculationLineItem
                    {
                        Description = $"Advance Deduction (Batch {deduction.BatchNumber})",
                        Amount = -deduction.DeductionAmount,
                        IsSubtotal = false
                    });
                }
            }

            // Add net amount
            LineItems.Add(new CalculationLineItem
            {
                Description = "Net Cheque Amount",
                Amount = NetAmount,
                IsSubtotal = true
            });
        }

        private void BuildAdvanceChequeLineItems()
        {
            // Add advance amount
            LineItems.Add(new CalculationLineItem
            {
                Description = "Advance Payment Amount",
                Amount = GrossAmount,
                IsSubtotal = false
            });

            // Add net amount (same as gross for advance cheques)
            LineItems.Add(new CalculationLineItem
            {
                Description = "Net Cheque Amount",
                Amount = NetAmount,
                IsSubtotal = true
            });
        }

        private void BuildConsolidatedChequeLineItems()
        {
            // Add individual batch amounts
            if (BatchBreakdowns.Any())
            {
                foreach (var batch in BatchBreakdowns)
                {
                    LineItems.Add(new CalculationLineItem
                    {
                        Description = $"Batch {batch.BatchNumber} Amount",
                        Amount = batch.Amount,
                        IsSubtotal = false
                    });
                }
            }
            else
            {
                LineItems.Add(new CalculationLineItem
                {
                    Description = "Consolidated Payment Amount",
                    Amount = GrossAmount,
                    IsSubtotal = false
                });
            }

            // Add advance deductions if any
            if (AdvanceDeductions.Any())
            {
                foreach (var deduction in AdvanceDeductions)
                {
                    LineItems.Add(new CalculationLineItem
                    {
                        Description = $"Advance Deduction (Batch {deduction.BatchNumber})",
                        Amount = -deduction.DeductionAmount,
                        IsSubtotal = false
                    });
                }
            }

            // Add net amount
            LineItems.Add(new CalculationLineItem
            {
                Description = "Net Cheque Amount",
                Amount = NetAmount,
                IsSubtotal = true
            });
        }

        private void BuildCalculationSummary()
        {
            var summary = new List<string>();

            switch (PaymentType)
            {
                case ChequePaymentType.Regular:
                    summary.Add("Regular batch payment");
                    if (HasDeductions)
                        summary.Add($"with {AdvanceDeductions.Count} advance deduction(s)");
                    break;

                case ChequePaymentType.Advance:
                    summary.Add("Advance payment");
                    break;

                case ChequePaymentType.Distribution:
                    summary.Add($"Consolidated payment from {BatchBreakdowns.Count} batch(es)");
                    if (HasDeductions)
                        summary.Add($"with {AdvanceDeductions.Count} advance deduction(s)");
                    break;
            }

            CalculationSummary = string.Join(" ", summary);
        }

        private string GetPaymentTypeDisplay()
        {
            return PaymentType switch
            {
                ChequePaymentType.Regular => "Regular Payment",
                ChequePaymentType.Advance => "Advance Payment",
                ChequePaymentType.Distribution => "Consolidated Payment",
                _ => "Unknown"
            };
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
    /// Model representing a line item in the calculation breakdown
    /// </summary>
    public class CalculationLineItem : INotifyPropertyChanged
    {
        private string _description;
        private decimal _amount;
        private bool _isSubtotal;

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

        public bool IsSubtotal
        {
            get => _isSubtotal;
            set => SetProperty(ref _isSubtotal, value);
        }

        // Display properties
        public string AmountDisplay => Amount.ToString("C");
        public string FormattedDescription => IsSubtotal ? $"**{Description}**" : Description;

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
