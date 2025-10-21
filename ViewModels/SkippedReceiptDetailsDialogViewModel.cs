using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WPFGrowerApp.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Commands;
using MaterialDesignThemes.Wpf;

namespace WPFGrowerApp.ViewModels
{
    public class SkippedReceiptDetailsDialogViewModel : INotifyPropertyChanged
    {
        private readonly IDialogService _dialogService;
        private ObservableCollection<SkippedReceiptDetail> _skippedReceiptDetails = new ObservableCollection<SkippedReceiptDetail>();
        private string _title = "Skipped Receipt Details";
        private int _totalCount;

        public SkippedReceiptDetailsDialogViewModel(
            ObservableCollection<SkippedReceiptDetail> skippedReceiptDetails,
            IDialogService dialogService)
        {
            _skippedReceiptDetails = skippedReceiptDetails ?? new ObservableCollection<SkippedReceiptDetail>();
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _totalCount = _skippedReceiptDetails.Count;

            CloseCommand = new RelayCommand(CloseDialog);
            
            // Debug: Log the count to verify data is being passed
            System.Diagnostics.Debug.WriteLine($"SkippedReceiptDetailsDialogViewModel: {_skippedReceiptDetails.Count} receipts loaded");
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int TotalCount
        {
            get => _totalCount;
            set => SetProperty(ref _totalCount, value);
        }

        public ObservableCollection<SkippedReceiptDetail> SkippedReceiptDetails
        {
            get => _skippedReceiptDetails;
            set => SetProperty(ref _skippedReceiptDetails, value);
        }

        public bool HasSkippedReceipts => SkippedReceiptDetails.Any();

        public string SummaryText
        {
            get
            {
                if (!HasSkippedReceipts) return "No skipped receipts to display.";

                var skippedByReason = SkippedReceiptDetails
                    .GroupBy(s => s.Reason)
                    .Select(g => $"{g.Key}: {g.Count()} receipt(s)")
                    .ToList();

                return $"Skipped Receipt Details ({TotalCount} total):\n\n" + string.Join("\n", skippedByReason);
            }
        }

        public ICommand CloseCommand { get; }

        public event EventHandler? DialogClosed;

        private void CloseDialog(object? parameter)
        {
            // Close the Material Design dialog by setting the result
            DialogHost.CloseDialogCommand.Execute(null, null);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
