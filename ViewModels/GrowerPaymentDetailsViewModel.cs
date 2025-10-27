using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.Models;
using WPFGrowerApp.Commands;

namespace WPFGrowerApp.ViewModels
{
    public class GrowerPaymentDetailsViewModel : INotifyPropertyChanged
    {
        private readonly ICrossBatchPaymentService _crossBatchPaymentService;
        private readonly IUnifiedAdvanceService _unifiedAdvanceService;
        private readonly IPaymentBatchService _paymentBatchService;
        private readonly IReceiptService _receiptService;

        private GrowerPaymentSelection _growerSelection;
        private List<int> _selectedBatchIds;

        public GrowerPaymentDetailsViewModel(
            GrowerPaymentSelection growerSelection,
            List<int> selectedBatchIds,
            ICrossBatchPaymentService crossBatchPaymentService,
            IUnifiedAdvanceService unifiedAdvanceService,
            IPaymentBatchService paymentBatchService,
            IReceiptService receiptService)
        {
            _growerSelection = growerSelection;
            _selectedBatchIds = selectedBatchIds;
            _crossBatchPaymentService = crossBatchPaymentService;
            _unifiedAdvanceService = unifiedAdvanceService;
            _paymentBatchService = paymentBatchService;
            _receiptService = receiptService;

            ReceiptBreakdowns = new ObservableCollection<ReceiptDetailDto>();
            AdvanceCheques = new ObservableCollection<AdvanceChequeDetail>();
            BatchInformation = new ObservableCollection<BatchInfo>();

            CloseCommand = new RelayCommand(p => Close());
        }

        private void Close()
        {
            // Find and close the window
            var window = Application.Current.Windows.OfType<Views.GrowerPaymentDetailsDialog>().FirstOrDefault();
            window?.Close();
        }

        #region Properties

        public string GrowerName => _growerSelection.GrowerName;
        public string GrowerNumber => _growerSelection.GrowerNumber;
        public string GrowerDisplay => $"{GrowerName} ({GrowerNumber})";

        public decimal TotalGrossAmount => _growerSelection.ConsolidatedAmount;
        public decimal DeductFromThisTransaction => _growerSelection.DeductFromThisTransaction;
        public decimal NetPaymentAmount => _growerSelection.NetPaymentAmount;
        public decimal RemainingDeductions => _growerSelection.RemainingDeductions;
        public decimal OutstandingAdvances => _growerSelection.OutstandingAdvances;

        public string TotalGrossAmountDisplay => TotalGrossAmount.ToString("C");
        public string DeductFromThisTransactionDisplay => DeductFromThisTransaction.ToString("C");
        public string NetPaymentAmountDisplay => NetPaymentAmount.ToString("C");
        public string RemainingDeductionsDisplay => RemainingDeductions.ToString("C");
        public string OutstandingAdvancesDisplay => OutstandingAdvances.ToString("C");

        public int SelectedBatchesCount => _selectedBatchIds.Count;
        public int TotalReceipts => ReceiptBreakdowns.Count;
        public decimal TotalWeight => ReceiptBreakdowns.Sum(r => r.FinalWeight);
        public decimal AveragePricePerPound => TotalWeight > 0 ? ReceiptBreakdowns.Sum(r => r.TotalAmountPaid) / TotalWeight : 0;
        public int AdvanceChequesCount => AdvanceCheques.Count;

        public string TotalWeightDisplay => $"{TotalWeight:N2} lbs";
        public string AveragePricePerPoundDisplay => AveragePricePerPound.ToString("C2") + "/lb";
        public string PaymentDateDisplay => DateTime.Now.ToString("MMM dd, yyyy");

        public ObservableCollection<ReceiptDetailDto> ReceiptBreakdowns { get; }
        public ObservableCollection<AdvanceChequeDetail> AdvanceCheques { get; }
        public ObservableCollection<BatchInfo> BatchInformation { get; }

        #endregion

        #region Commands

        public ICommand CloseCommand { get; }

        #endregion

        #region Methods

        public async Task LoadDataAsync()
        {
            try
            {
                // Load receipt breakdowns
                await LoadReceiptBreakdownsAsync();

                // Load advance cheques
                await LoadAdvanceChequesAsync();

                // Load batch information
                await LoadBatchInformationAsync();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error loading grower payment details: {ex.Message}", ex);
            }
        }

        private async Task LoadReceiptBreakdownsAsync()
        {
            try
            {
                ReceiptBreakdowns.Clear();

                // Get detailed receipt information for this grower across selected batches
                var receiptDetails = await _receiptService.GetReceiptDetailsForGrowerAsync(
                    _growerSelection.GrowerId, _selectedBatchIds);

                foreach (var receipt in receiptDetails)
                {
                    ReceiptBreakdowns.Add(receipt);
                }

                OnPropertyChanged(nameof(TotalReceipts));
                OnPropertyChanged(nameof(TotalWeight));
                OnPropertyChanged(nameof(TotalWeightDisplay));
                OnPropertyChanged(nameof(AveragePricePerPound));
                OnPropertyChanged(nameof(AveragePricePerPoundDisplay));
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error loading receipt breakdowns: {ex.Message}", ex);
            }
        }

        private async Task LoadAdvanceChequesAsync()
        {
            try
            {
                AdvanceCheques.Clear();

                // Get outstanding advances for this grower
                var outstandingAdvances = await _unifiedAdvanceService.GetOutstandingAdvancesAsync(_growerSelection.GrowerId);

                // Track remaining deduction amount to apply across cheques
                decimal remainingDeductionToApply = DeductFromThisTransaction;

                foreach (var advance in outstandingAdvances)
                {
                    // Calculate how much of the deduction applies to this specific cheque
                    decimal appliedToThisCheque = 0;
                    decimal remainingAfterTransaction = advance.CurrentAdvanceAmount;

                    if (remainingDeductionToApply > 0)
                    {
                        // Apply deduction to this cheque up to its remaining balance
                        appliedToThisCheque = Math.Min(advance.CurrentAdvanceAmount, remainingDeductionToApply);
                        remainingAfterTransaction = advance.CurrentAdvanceAmount - appliedToThisCheque;
                        
                        // Reduce the remaining deduction amount for the next cheques
                        remainingDeductionToApply -= appliedToThisCheque;
                    }

                    // AdvanceAmount is the original amount, CurrentAdvanceAmount is the remaining balance
                    var detail = new AdvanceChequeDetail
                    {
                        ChequeNumber = advance.ChequeNumber?.ToString() ?? $"ADV-{advance.AdvanceChequeId:D6}",
                        AdvanceDate = advance.AdvanceDate,
                        OriginalAmount = advance.AdvanceAmount,
                        RemainingAmount = advance.CurrentAdvanceAmount,
                        AppliedThisTransaction = appliedToThisCheque,
                        RemainingAfterTransaction = remainingAfterTransaction,
                        Status = advance.Status
                    };

                    AdvanceCheques.Add(detail);
                }

                OnPropertyChanged(nameof(AdvanceChequesCount));
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error loading advance cheques: {ex.Message}", ex);
            }
        }

        private async Task LoadBatchInformationAsync()
        {
            try
            {
                BatchInformation.Clear();

                // Get payments for this grower across selected batches to get batch info
                var payments = await _crossBatchPaymentService.GetGrowerPaymentsAcrossBatchesAsync(
                    _growerSelection.GrowerId, _selectedBatchIds);

                foreach (var payment in payments)
                {
                    var info = new BatchInfo
                    {
                        BatchNumber = payment.BatchNumber,
                        BatchDate = payment.BatchDate,
                        PaymentMethod = "Cheque",
                        Status = payment.Status
                    };

                    BatchInformation.Add(info);
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error($"Error loading batch information: {ex.Message}", ex);
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    #region Data Models

    public class ReceiptBreakdown
    {
        public string ReceiptNumber { get; set; }
        public string ProductName { get; set; }
        public decimal Weight { get; set; }
        public decimal PricePerPound { get; set; }
        public decimal Amount { get; set; }
        public string BatchNumber { get; set; }
        public DateTime PaymentDate { get; set; }

        public string WeightDisplay => $"{Weight:N2}";
        public string PricePerPoundDisplay => PricePerPound.ToString("C2");
        public string AmountDisplay => Amount.ToString("C");
        public string PaymentDateDisplay => PaymentDate.ToString("MMM dd, yyyy");
    }

    public class AdvanceChequeDetail
    {
        public string ChequeNumber { get; set; }
        public DateTime AdvanceDate { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal AppliedThisTransaction { get; set; }
        public decimal RemainingAfterTransaction { get; set; }
        public string Status { get; set; }

        public string AdvanceDateDisplay => AdvanceDate.ToString("MMM dd, yyyy");
        public string OriginalAmountDisplay => OriginalAmount.ToString("C");
        public string RemainingAmountDisplay => RemainingAmount.ToString("C");
        public string AppliedThisTransactionDisplay => AppliedThisTransaction.ToString("C");
        public string RemainingAfterTransactionDisplay => RemainingAfterTransaction.ToString("C");
    }

    public class BatchInfo
    {
        public string BatchNumber { get; set; }
        public DateTime BatchDate { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }

        public string BatchDateDisplay => BatchDate.ToString("MMM dd, yyyy");
    }

    #endregion
}
