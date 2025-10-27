using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for grower-specific deduction dialog
    /// </summary>
    public class GrowerDeductionViewModel : ViewModelBase
    {
        private readonly GrowerPaymentSelection _growerSelection;
        private readonly List<AdvanceCheque> _outstandingAdvances;
        private decimal _deductFromThisTransaction;
        private decimal _remainingDeductions;
        private decimal _netPaymentAmount;

        public GrowerDeductionViewModel(GrowerPaymentSelection growerSelection, List<AdvanceCheque> outstandingAdvances)
        {
            _growerSelection = growerSelection;
            _outstandingAdvances = outstandingAdvances;
            
            // Initialize deduction amounts
            _deductFromThisTransaction = growerSelection.DeductFromThisTransaction;
            _remainingDeductions = growerSelection.RemainingDeductions;
            _netPaymentAmount = growerSelection.ConsolidatedAmount - _deductFromThisTransaction;

            // Initialize commands
            ApplyCommand = new RelayCommand(p => ApplyAsync(), p => CanApply());
            CancelCommand = new RelayCommand(p => Cancel());
        }

        #region Properties

        public string GrowerName => _growerSelection.GrowerName;
        public string GrowerNumber => _growerSelection.GrowerNumber;
        public decimal GrossAmount => _growerSelection.ConsolidatedAmount;
        public decimal TotalOutstandingAdvances => _outstandingAdvances.Sum(a => a.CurrentAdvanceAmount);

        public decimal DeductFromThisTransaction
        {
            get => _deductFromThisTransaction;
            set
            {
                if (SetProperty(ref _deductFromThisTransaction, value))
                {
                    // Validate the deduction amount
                    if (value < 0)
                        _deductFromThisTransaction = 0;
                    else if (value > TotalOutstandingAdvances)
                        _deductFromThisTransaction = TotalOutstandingAdvances;
                    
                    // Update calculated values
                    RemainingDeductions = TotalOutstandingAdvances - _deductFromThisTransaction;
                    NetPaymentAmount = GrossAmount - _deductFromThisTransaction;
                }
            }
        }

        public decimal RemainingDeductions
        {
            get => _remainingDeductions;
            set => SetProperty(ref _remainingDeductions, value);
        }

        public decimal NetPaymentAmount
        {
            get => _netPaymentAmount;
            set => SetProperty(ref _netPaymentAmount, value);
        }

        public ObservableCollection<AdvanceCheque> OutstandingAdvances => new ObservableCollection<AdvanceCheque>(_outstandingAdvances);

        // Display properties
        public string GrossAmountDisplay => GrossAmount.ToString("C");
        public string TotalOutstandingAdvancesDisplay => TotalOutstandingAdvances.ToString("C");
        public string DeductFromThisTransactionDisplay => DeductFromThisTransaction.ToString("C");
        public string RemainingDeductionsDisplay => RemainingDeductions.ToString("C");
        public string NetPaymentAmountDisplay => NetPaymentAmount.ToString("C");

        #endregion

        #region Commands

        public ICommand ApplyCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Command Methods

        private void ApplyAsync()
        {
            try
            {
                // Update the grower selection with new values
                _growerSelection.DeductFromThisTransaction = DeductFromThisTransaction;
                _growerSelection.RemainingDeductions = RemainingDeductions;
                // Note: NetPaymentAmount is now computed automatically, no need to set it

                // Close dialog with success
                DialogResult = true;
            }
            catch (Exception ex)
            {
                // Handle error
                System.Diagnostics.Debug.WriteLine($"Error applying deductions: {ex.Message}");
            }
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        private bool CanApply()
        {
            return DeductFromThisTransaction >= 0 && DeductFromThisTransaction <= TotalOutstandingAdvances;
        }

        #endregion

        #region Dialog Result

        public bool? DialogResult { get; set; }

        #endregion
    }
}
