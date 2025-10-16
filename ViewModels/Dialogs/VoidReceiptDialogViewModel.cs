using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    public class VoidReceiptDialogViewModel : INotifyPropertyChanged
    {
        private readonly IReceiptVoidService _receiptVoidService;
        private readonly string _receiptNumber;

        private string _warningMessage = string.Empty;
        private string _batchNumber = string.Empty;
        private decimal _amountVoided;
        private List<string> _affectedGrowers = new();
        private string _voidReason = string.Empty;
        private bool _isProcessing;
        private string _statusMessage = string.Empty;

        public VoidReceiptDialogViewModel(IReceiptVoidService receiptVoidService, string receiptNumber)
        {
            _receiptVoidService = receiptVoidService;
            _receiptNumber = receiptNumber;

            InitializeCommands();
            LoadReceiptInfo();
        }

        #region Properties

        public string WarningMessage
        {
            get => _warningMessage;
            set => SetProperty(ref _warningMessage, value);
        }

        public string BatchNumber
        {
            get => _batchNumber;
            set => SetProperty(ref _batchNumber, value);
        }

        public decimal AmountVoided
        {
            get => _amountVoided;
            set => SetProperty(ref _amountVoided, value);
        }

        public List<string> AffectedGrowers
        {
            get => _affectedGrowers;
            set => SetProperty(ref _affectedGrowers, value);
        }

        public string AffectedGrowersText => string.Join(", ", AffectedGrowers);

        public string VoidReason
        {
            get => _voidReason;
            set => SetProperty(ref _voidReason, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Commands

        public ICommand VoidReceiptCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        #endregion

        #region Command Implementations

        private void InitializeCommands()
        {
            VoidReceiptCommand = new RelayCommand(async (param) => await VoidReceiptAsync(), (param) => CanVoidReceipt());
            CancelCommand = new RelayCommand((param) => Cancel());
        }

        private async Task VoidReceiptAsync()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Voiding receipt...";

                var result = await _receiptVoidService.VoidReceiptWithCascadingAsync(
                    _receiptNumber, 
                    VoidReason, 
                    App.CurrentUser?.Username ?? "SYSTEM");

                if (result.Success)
                {
                    StatusMessage = $"Successfully voided receipt {_receiptNumber}";
                    if (result.BatchReverted)
                    {
                        StatusMessage += $" and reverted batch {result.BatchNumber} to Draft status";
                    }
                    
                    // Close dialog with success
                    OnVoidCompleted?.Invoke(true, result);
                }
                else
                {
                    StatusMessage = $"Error voiding receipt: {result.ErrorMessage}";
                    OnVoidCompleted?.Invoke(false, result);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error voiding receipt {_receiptNumber}: {ex.Message}", ex);
                StatusMessage = $"Error voiding receipt: {ex.Message}";
                OnVoidCompleted?.Invoke(false, null);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void Cancel()
        {
            OnVoidCompleted?.Invoke(false, null);
        }

        #endregion

        #region Helper Methods

        private async Task LoadReceiptInfo()
        {
            try
            {
                var impact = await _receiptVoidService.AnalyzeReceiptVoidImpactAsync(_receiptNumber);
                
                WarningMessage = impact.WarningMessage;
                BatchNumber = impact.BatchNumber;
                AmountVoided = impact.AmountVoided;
                AffectedGrowers = impact.AffectedGrowers;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error loading receipt info for {_receiptNumber}: {ex.Message}", ex);
                WarningMessage = "Error loading receipt information";
            }
        }

        private bool CanVoidReceipt()
        {
            return !string.IsNullOrWhiteSpace(VoidReason) && !IsProcessing;
        }

        #endregion

        #region Events

        public event Action<bool, VoidReceiptResult>? OnVoidCompleted;

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
