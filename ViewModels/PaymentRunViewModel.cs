using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models; // For PostBatch
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services; // For IDialogService

namespace WPFGrowerApp.ViewModels
{
    public class PaymentRunViewModel : ViewModelBase, IProgress<string>
    {
        private readonly IPaymentService _paymentService;
        private readonly IDialogService _dialogService;
        // Add other services if needed (e.g., IGrowerService for lookups)

        private int _advanceNumber = 1;
        private DateTime _paymentDate = DateTime.Today;
        private DateTime _cutoffDate = DateTime.Today;
        private int _cropYear = DateTime.Today.Year;
        private decimal? _includeGrowerId;
        private string _includePayGroup;
        private decimal? _excludeGrowerId;
        private string _excludePayGroup;
        private string _productId;
        private string _processId;
        private bool _isRunning;
        private string _statusMessage;
        private ObservableCollection<string> _runLog;
        private PostBatch _lastRunBatch;
        private List<string> _lastRunErrors;

        public PaymentRunViewModel(IPaymentService paymentService, IDialogService dialogService)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            RunLog = new ObservableCollection<string>();
            LastRunErrors = new List<string>();
        }

        // Properties for UI Binding
        public int AdvanceNumber
        {
            get => _advanceNumber;
            set => SetProperty(ref _advanceNumber, value);
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
        }

        public DateTime CutoffDate
        {
            get => _cutoffDate;
            set => SetProperty(ref _cutoffDate, value);
        }

        public int CropYear
        {
            get => _cropYear;
            set => SetProperty(ref _cropYear, value);
        }

        public decimal? IncludeGrowerId
        {
            get => _includeGrowerId;
            set => SetProperty(ref _includeGrowerId, value);
        }

        public string IncludePayGroup
        {
            get => _includePayGroup;
            set => SetProperty(ref _includePayGroup, value);
        }
         public decimal? ExcludeGrowerId
        {
            get => _excludeGrowerId;
            set => SetProperty(ref _excludeGrowerId, value);
        }

        public string ExcludePayGroup
        {
            get => _excludePayGroup;
            set => SetProperty(ref _excludePayGroup, value);
        }

         public string ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ProcessId
        {
            get => _processId;
            set => SetProperty(ref _processId, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                SetProperty(ref _isRunning, value);
                // Trigger CanExecuteChanged for commands
                ((RelayCommand)StartPaymentRunCommand).RaiseCanExecuteChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> RunLog
        {
            get => _runLog;
            private set => SetProperty(ref _runLog, value);
        }

         public PostBatch LastRunBatch
        {
            get => _lastRunBatch;
            private set => SetProperty(ref _lastRunBatch, value);
        }

        public List<string> LastRunErrors
        {
            get => _lastRunErrors;
            private set => SetProperty(ref _lastRunErrors, value);
        }


        // Commands
        public ICommand StartPaymentRunCommand => new RelayCommand(async o => await StartPaymentRunAsync(), o => !IsRunning);

        // IProgress<string> implementation
        public void Report(string value)
        {
            // Ensure updates run on the UI thread
            App.Current.Dispatcher.Invoke(() =>
            {
                RunLog.Add($"{DateTime.Now:HH:mm:ss} - {value}");
                // Optionally limit log size
                if (RunLog.Count > 200) RunLog.RemoveAt(0);
            });
        }

        private async Task StartPaymentRunAsync()
        {
            IsRunning = true;
            RunLog.Clear();
            LastRunErrors.Clear();
            LastRunBatch = null;
            StatusMessage = "Starting payment run...";
            Report($"Initiating Advance {AdvanceNumber} payment run...");
            Report($"Parameters: PaymentDate={PaymentDate:d}, CutoffDate={CutoffDate:d}, CropYear={CropYear}");
            // Log other parameters if set

            try
            {
                var (success, errors, createdBatch) = await _paymentService.ProcessAdvancePaymentRunAsync(
                    AdvanceNumber,
                    PaymentDate,
                    CutoffDate,
                    CropYear,
                    IncludeGrowerId,
                    IncludePayGroup,
                    ExcludeGrowerId,
                    ExcludePayGroup,
                    ProductId,
                    ProcessId,
                    this); // Pass this ViewModel as IProgress<string>

                LastRunBatch = createdBatch;
                LastRunErrors = errors ?? new List<string>();

                if (success)
                {
                    StatusMessage = $"Payment run completed successfully for Batch {createdBatch?.PostBat}.";
                    Report("Payment run finished successfully.");
                    _dialogService.ShowMessageBox($"Advance {AdvanceNumber} payment run completed successfully for Batch {createdBatch?.PostBat}.", "Payment Run Complete");
                }
                else
                {
                    StatusMessage = $"Payment run completed with errors for Batch {createdBatch?.PostBat}. Check log.";
                    Report("Payment run finished with errors:");
                    foreach(var error in LastRunErrors)
                    {
                        Report($"ERROR: {error}");
                    }
                     _dialogService.ShowMessageBox($"The payment run encountered errors. Please review the run log.\nBatch ID: {createdBatch?.PostBat}", "Payment Run Errors");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Payment run failed with a critical error.";
                Report($"CRITICAL ERROR: {ex.Message}");
                Logger.Error($"Critical error during payment run execution", ex);
                _dialogService.ShowMessageBox($"A critical error occurred: {ex.Message}", "Payment Run Failed");
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
