using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for searching, viewing, voiding, and reissuing cheques
    /// </summary>
    public class ChequeManagementViewModel : ViewModelBase
    {
        private readonly IChequeGenerationService _chequeService;
        private readonly IChequePrintingService _printingService;
        private readonly IGrowerService _growerService;
        private readonly IDialogService _dialogService;

        // Properties
        private ObservableCollection<Cheque> _cheques;
        private Cheque? _selectedCheque;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private string _searchChequeNumber = string.Empty;
        private int? _searchGrowerNumber;
        private DateTime? _searchDateFrom;
        private DateTime? _searchDateTo;
        private string _searchStatus = "All";

        public ObservableCollection<Cheque> Cheques
        {
            get => _cheques;
            set => SetProperty(ref _cheques, value);
        }

        public Cheque? SelectedCheque
        {
            get => _selectedCheque;
            set => SetProperty(ref _selectedCheque, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string SearchChequeNumber
        {
            get => _searchChequeNumber;
            set => SetProperty(ref _searchChequeNumber, value);
        }

        public int? SearchGrowerNumber
        {
            get => _searchGrowerNumber;
            set => SetProperty(ref _searchGrowerNumber, value);
        }

        public DateTime? SearchDateFrom
        {
            get => _searchDateFrom;
            set => SetProperty(ref _searchDateFrom, value);
        }

        public DateTime? SearchDateTo
        {
            get => _searchDateTo;
            set => SetProperty(ref _searchDateTo, value);
        }

        public string SearchStatus
        {
            get => _searchStatus;
            set => SetProperty(ref _searchStatus, value);
        }

        public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string>
        {
            "All",
            "Issued",
            "Cleared",
            "Voided",
            "Stopped"
        };

        // Commands
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ViewChequeCommand { get; }
        public ICommand VoidChequeCommand { get; }
        public ICommand ReissueChequeCommand { get; }
        public ICommand PrintChequeCommand { get; }
        public ICommand RefreshCommand { get; }

        public ChequeManagementViewModel(
            IChequeGenerationService chequeService,
            IChequePrintingService printingService,
            IGrowerService growerService,
            IDialogService dialogService)
        {
            _chequeService = chequeService ?? throw new ArgumentNullException(nameof(chequeService));
            _printingService = printingService ?? throw new ArgumentNullException(nameof(printingService));
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            _cheques = new ObservableCollection<Cheque>();

            // Initialize commands
            SearchCommand = new RelayCommand(async o => await SearchChequesAsync());
            ClearSearchCommand = new RelayCommand(o => ClearSearch());
            ViewChequeCommand = new RelayCommand(async o => await ViewChequeDetailsAsync(), o => SelectedCheque != null);
            VoidChequeCommand = new RelayCommand(async o => await VoidChequeAsync(), o => CanVoidCheque());
            ReissueChequeCommand = new RelayCommand(async o => await ReissueChequeAsync(), o => CanReissueCheque());
            PrintChequeCommand = new RelayCommand(async o => await PrintChequeAsync(), o => SelectedCheque != null);
            RefreshCommand = new RelayCommand(async o => await RefreshAsync());

            // Initialize with recent cheques
            _ = InitializeAsync();
        }

        // ==============================================================
        // INITIALIZATION
        // ==============================================================

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;

                // Load recent cheques (last 100)
                await SearchChequesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing ChequeManagementViewModel", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading cheques: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ==============================================================
        // SEARCH
        // ==============================================================

        private async Task SearchChequesAsync()
        {
            try
            {
                IsLoading = true;

                // If searching by cheque number
                if (!string.IsNullOrWhiteSpace(SearchChequeNumber))
                {
                    var cheques = await _chequeService.SearchChequesByNumberAsync(SearchChequeNumber);
                    UpdateChequesList(cheques);
                }
                // If searching by grower
                else if (SearchGrowerNumber.HasValue)
                {
                    var cheques = await _chequeService.GetGrowerChequesAsync(SearchGrowerNumber.Value);
                    UpdateChequesList(cheques);
                }
                // Default: show recent cheques (you may want to add date range search to service)
                else
                {
                    // For now, show unprinted cheques
                    var cheques = await _chequeService.GetUnprintedChequesAsync();
                    UpdateChequesList(cheques);
                }

                Logger.Info($"Search returned {Cheques.Count} cheques");
            }
            catch (Exception ex)
            {
                Logger.Error("Error searching cheques", ex);
                await _dialogService.ShowMessageBoxAsync($"Error searching: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateChequesList(List<Cheque> cheques)
        {
            // Apply status filter
            if (SearchStatus != "All")
            {
                cheques = cheques.Where(c => c.Status == SearchStatus).ToList();
            }

            // Apply date range filter
            if (SearchDateFrom.HasValue)
            {
                cheques = cheques.Where(c => c.ChequeDate >= SearchDateFrom.Value).ToList();
            }

            if (SearchDateTo.HasValue)
            {
                cheques = cheques.Where(c => c.ChequeDate <= SearchDateTo.Value).ToList();
            }

            Cheques.Clear();
            foreach (var cheque in cheques)
            {
                Cheques.Add(cheque);
            }
        }

        private void ClearSearch()
        {
            SearchChequeNumber = string.Empty;
            SearchGrowerNumber = null;
            SearchDateFrom = null;
            SearchDateTo = null;
            SearchStatus = "All";
            _ = SearchChequesAsync();
        }

        // ==============================================================
        // COMMANDS
        // ==============================================================

        private async Task RefreshAsync()
        {
            await SearchChequesAsync();
        }

        private async Task ViewChequeDetailsAsync()
        {
            if (SelectedCheque == null)
                return;

            var message = $"Cheque Details:\n\n" +
                         $"Cheque Number: {SelectedCheque.DisplayChequeNumber}\n" +
                         $"Grower: {SelectedCheque.GrowerName}\n" +
                         $"Amount: ${SelectedCheque.ChequeAmount:N2}\n" +
                         $"Date: {SelectedCheque.ChequeDate:yyyy-MM-dd}\n" +
                         $"Status: {SelectedCheque.Status}\n" +
                         $"Currency: {SelectedCheque.CurrencyCode}\n\n" +
                         $"Payee: {SelectedCheque.PayeeName}\n" +
                         $"Memo: {SelectedCheque.Memo}\n\n" +
                         $"Created: {SelectedCheque.CreatedAt:g} by {SelectedCheque.CreatedBy}";

            if (SelectedCheque.PrintedAt.HasValue)
            {
                message += $"\nPrinted: {SelectedCheque.PrintedAt:g} by {SelectedCheque.PrintedBy}";
            }

            if (SelectedCheque.IsVoided)
            {
                message += $"\n\nVOIDED: {SelectedCheque.VoidedDate:g}\n" +
                          $"Reason: {SelectedCheque.VoidedReason}\n" +
                          $"By: {SelectedCheque.VoidedBy}";
            }

            await _dialogService.ShowMessageBoxAsync(message, "Cheque Details");
        }

        private async Task VoidChequeAsync()
        {
            if (SelectedCheque == null)
                return;

            try
            {
                // Confirm
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"Grower: {SelectedCheque.GrowerName}\n" +
                    $"Amount: ${SelectedCheque.ChequeAmount:N2}\n\n" +
                    $"Do you want to void this cheque?",
                    $"Void Cheque {SelectedCheque.DisplayChequeNumber}?");

                if (confirm != true)
                    return;

                // Get reason
                var reason = await _dialogService.ShowInputDialogAsync(
                    "Enter reason for voiding:",
                    "Void Reason");

                if (string.IsNullOrWhiteSpace(reason))
                {
                    await _dialogService.ShowMessageBoxAsync("Void reason is required.", "Required");
                    return;
                }

                // Ask about accounting reversal
                var reverseAccounting = await _dialogService.ShowConfirmationAsync(
                    "Do you want to reverse the accounting entries?\n\n" +
                    "YES: Removes A/P records (grower can be paid again at new rate)\n" +
                    "NO: Keeps A/P records (grower already paid, just voiding cheque)",
                    "Reverse Accounting Entries?");

                IsLoading = true;

                // Void the cheque
                var voidedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var success = await _chequeService.VoidChequeAsync(
                    SelectedCheque.ChequeId,
                    reason,
                    voidedBy,
                    reverseAccounting == true);

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Cheque {SelectedCheque.DisplayChequeNumber} has been voided.",
                        "Cheque Voided");

                    // Refresh list
                    await SearchChequesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error voiding cheque", ex);
                await _dialogService.ShowMessageBoxAsync($"Error voiding cheque: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReissueChequeAsync()
        {
            if (SelectedCheque == null)
                return;

            try
            {
                // Confirm
                var confirm = await _dialogService.ShowConfirmationAsync(
                    $"This will:\n" +
                    $"• Void the original cheque\n" +
                    $"• Create a new cheque for the same amount\n\n" +
                    $"Original: {SelectedCheque.DisplayChequeNumber} (${SelectedCheque.ChequeAmount:N2})\n" +
                    $"Grower: {SelectedCheque.GrowerName}\n\n" +
                    $"Continue?",
                    $"Reissue Cheque {SelectedCheque.DisplayChequeNumber}?");

                if (confirm != true)
                    return;

                IsLoading = true;

                // Reissue the cheque (defaults to today's date)
                var reissuedBy = App.CurrentUser?.Username ?? "SYSTEM";
                var newCheque = await _chequeService.ReissueChequeAsync(
                    SelectedCheque.ChequeId,
                    DateTime.Today,
                    reissuedBy);

                await _dialogService.ShowMessageBoxAsync(
                    $"Cheque reissued successfully!\n\n" +
                    $"Original: #{SelectedCheque.ChequeNumber}\n" +
                    $"New: #{newCheque.ChequeNumber}\n" +
                    $"Amount: ${newCheque.ChequeAmount:N2}",
                    "Cheque Reissued");

                // Refresh list
                await SearchChequesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Error reissuing cheque", ex);
                await _dialogService.ShowMessageBoxAsync($"Error reissuing cheque: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PrintChequeAsync()
        {
            if (SelectedCheque == null)
                return;

            try
            {
                IsLoading = true;

                var printed = await _printingService.PrintChequeAsync(SelectedCheque);

                if (printed)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Cheque {SelectedCheque.DisplayChequeNumber} printed successfully.",
                        "Printed");

                    // Refresh to show updated PrintedAt status
                    await SearchChequesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error printing cheque", ex);
                await _dialogService.ShowMessageBoxAsync($"Error printing: {ex.Message}", "Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ==============================================================
        // HELPER METHODS
        // ==============================================================

        private bool CanVoidCheque()
        {
            return SelectedCheque != null && SelectedCheque.CanBeVoided;
        }

        private bool CanReissueCheque()
        {
            return SelectedCheque != null && SelectedCheque.IsVoided;
        }
    }
}


