using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using WPFGrowerApp.DataAccess.Models;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Enhanced cheque item model for unified cheque preparation view
    /// </summary>
    public class ChequeItem : INotifyPropertyChanged
    {
        private int _chequeId;
        private string _chequeNumber;
        private int _growerId;
        private string _growerName;
        private string _growerNumber;
        private decimal _amount;
        private DateTime _chequeDate;
        private string _status;
        private ChequePaymentType _paymentType;
        private bool _isConsolidated;
        private bool _isFromDistribution;
        private bool _isAdvanceCheque;
        private int? _advanceChequeId;
        private int? _paymentBatchId;
        private string _batchNumber;
        private string _consolidatedFromBatches;
        private string _sourceBatches;
        private List<BatchBreakdown> _batchBreakdowns;
        private List<AdvanceDeduction> _advanceDeductions;
        private string _advanceReason;
        private bool _isSelected;
        private bool _canBeVoided;
        private bool _canBePrinted;
        private bool _canBeIssued;

        public int ChequeId
        {
            get => _chequeId;
            set => SetProperty(ref _chequeId, value);
        }

        public string ChequeNumber
        {
            get => _chequeNumber;
            set => SetProperty(ref _chequeNumber, value);
        }

        public int GrowerId
        {
            get => _growerId;
            set => SetProperty(ref _growerId, value);
        }

        public string GrowerName
        {
            get => _growerName;
            set => SetProperty(ref _growerName, value);
        }

        public string GrowerNumber
        {
            get => _growerNumber;
            set => SetProperty(ref _growerNumber, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public DateTime ChequeDate
        {
            get => _chequeDate;
            set => SetProperty(ref _chequeDate, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ChequePaymentType PaymentType
        {
            get => _paymentType;
            set => SetProperty(ref _paymentType, value);
        }

        [Obsolete("Use IsFromDistribution instead. This property will be removed in a future version.")]
        public bool IsConsolidated
        {
            get => _isConsolidated;
            set => SetProperty(ref _isConsolidated, value);
        }

        public bool IsFromDistribution
        {
            get => _isFromDistribution;
            set => SetProperty(ref _isFromDistribution, value);
        }

        public bool IsAdvanceCheque
        {
            get => _isAdvanceCheque;
            set => SetProperty(ref _isAdvanceCheque, value);
        }

        public int? AdvanceChequeId
        {
            get => _advanceChequeId;
            set => SetProperty(ref _advanceChequeId, value);
        }

        public int? PaymentBatchId
        {
            get => _paymentBatchId;
            set => SetProperty(ref _paymentBatchId, value);
        }

        /// <summary>
        /// The type of cheque - computed based on cheque properties
        /// </summary>
        public string ChequeType => IsAdvanceCheque ? "Advance" : IsFromDistribution ? "Distribution" : "Regular";

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        [Obsolete("Use SourceBatches instead. This property will be removed in a future version.")]
        public string ConsolidatedFromBatches
        {
            get => _consolidatedFromBatches;
            set => SetProperty(ref _consolidatedFromBatches, value);
        }

        public string SourceBatches
        {
            get => _sourceBatches;
            set => SetProperty(ref _sourceBatches, value);
        }

        public List<BatchBreakdown> BatchBreakdowns
        {
            get => _batchBreakdowns;
            set => SetProperty(ref _batchBreakdowns, value);
        }

        public List<AdvanceDeduction> AdvanceDeductions
        {
            get => _advanceDeductions;
            set => SetProperty(ref _advanceDeductions, value);
        }

        public string AdvanceReason
        {
            get => _advanceReason;
            set => SetProperty(ref _advanceReason, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool CanBeVoided
        {
            get => _canBeVoided;
            set => SetProperty(ref _canBeVoided, value);
        }

        public bool CanBePrinted
        {
            get => _canBePrinted;
            set => SetProperty(ref _canBePrinted, value);
        }

        public bool CanBeIssued
        {
            get => _canBeIssued;
            set => SetProperty(ref _canBeIssued, value);
        }

        // Display properties
        public string AmountDisplay => Amount.ToString("C");
        public string DateDisplay => ChequeDate.ToString("MMM dd, yyyy");
        public string TypeDisplay => GetTypeDisplay();
        public string TypeIcon => GetTypeIcon();
        public string StatusDisplay => Status;
        public string GrowerDisplay => $"{GrowerNumber} - {GrowerName}";
        public string SourceBatchesDisplay => GetSourceBatchesDisplay();
        public string DeductionDetailsDisplay => GetDeductionDetailsDisplay();
        public decimal TotalDeductions => AdvanceDeductions?.Sum(d => d.DeductionAmount) ?? 0;
        public decimal NetAmount => Amount; // Amount is already net after deductions
        public string NetAmountDisplay => NetAmount.ToString("C");

        public ChequeItem()
        {
            BatchBreakdowns = new List<BatchBreakdown>();
            AdvanceDeductions = new List<AdvanceDeduction>();
        }

        public ChequeItem(Cheque cheque) : this()
        {
            if (cheque == null) return;

            ChequeId = cheque.ChequeId;
            ChequeNumber = cheque.ChequeNumber ?? string.Empty;
            GrowerId = cheque.GrowerId;
            GrowerName = cheque.GrowerName ?? "Unknown";
            GrowerNumber = cheque.GrowerNumber ?? "N/A";
            Amount = cheque.ChequeAmount;
            ChequeDate = cheque.ChequeDate;
            Status = cheque.Status ?? "Unknown";
            IsConsolidated = cheque.IsConsolidated;
            IsFromDistribution = cheque.IsFromDistribution;
            IsAdvanceCheque = cheque.IsAdvanceCheque;
            AdvanceChequeId = cheque.AdvanceChequeId;
            PaymentBatchId = cheque.PaymentBatchId;
            BatchNumber = cheque.BatchNumber ?? string.Empty;
            ConsolidatedFromBatches = cheque.ConsolidatedFromBatches ?? string.Empty;
            SourceBatches = cheque.SourceBatches ?? string.Empty;
            CanBeVoided = cheque.CanBeVoided;
            CanBePrinted = cheque.CanBePrinted;
            CanBeIssued = cheque.CanBeIssued;

            // Determine payment type
            if (IsAdvanceCheque)
                PaymentType = ChequePaymentType.Advance;
            else if (IsFromDistribution)
                PaymentType = ChequePaymentType.Regular; // Distribution payments are treated as regular for now
            else
                PaymentType = ChequePaymentType.Regular;
        }

        private string GetTypeDisplay()
        {
            if (IsFromDistribution)
                return "Distribution Payment";
            
            return PaymentType switch
            {
                ChequePaymentType.Regular => "Regular Payment",
                ChequePaymentType.Advance => "Advance Cheque",
                _ => "Unknown"
            };
        }

        private string GetTypeIcon()
        {
            if (IsFromDistribution)
                return "🔗";
            
            return PaymentType switch
            {
                ChequePaymentType.Regular => "📄",
                ChequePaymentType.Advance => "💰",
                _ => "📄"
            };
        }

        private string GetSourceBatchesDisplay()
        {
            // For distribution cheques, use BatchBreakdowns or SourceBatches
            if (IsFromDistribution && BatchBreakdowns != null && BatchBreakdowns.Any())
            {
                var batchNumbers = BatchBreakdowns.Select(b => b.BatchNumber).ToList();
                return string.Join(", ", batchNumbers);
            }
            
            // Fallback to SourceBatches if available
            if (IsFromDistribution && !string.IsNullOrEmpty(SourceBatches))
            {
                return SourceBatches;
            }
            
            // For regular payments, use BatchNumber if available
            if (!IsFromDistribution && !IsAdvanceCheque && !string.IsNullOrEmpty(BatchNumber))
            {
                return BatchNumber;
            }
            
            // For advance cheques or if no batch information
            return "N/A";
        }

        private string GetDeductionDetailsDisplay()
        {
            if (AdvanceDeductions == null || !AdvanceDeductions.Any())
                return "No deductions";

            var deductionAmounts = AdvanceDeductions.Select(d => d.AmountDisplay).ToList();
            return $"Deductions: {string.Join(", ", deductionAmounts)}";
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
}
