using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services; // Assuming IDialogService is here

namespace WPFGrowerApp.ViewModels
{
    // Inherit from the correct ViewModelBase used elsewhere in the project
    public class PaymentGroupViewModel : ViewModelBase 
    {
        private readonly IPayGroupService _payGroupService;
        private readonly IDialogService _dialogService; // For showing messages/confirmations
        private ObservableCollection<PayGroup> _payGroups;
        private PayGroup _selectedPayGroup;
        private bool _isLoading;

        public ObservableCollection<PayGroup> PayGroups
        {
            get => _payGroups;
            set => SetProperty(ref _payGroups, value);
        }

        public PayGroup SelectedPayGroup
        {
            get => _selectedPayGroup;
            set => SetProperty(ref _selectedPayGroup, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Commands (even if handled in code-behind, defining them here is good practice)
        public ICommand LoadPayGroupsCommand { get; }
        public ICommand AddPayGroupCommand { get; }
        public ICommand EditPayGroupCommand { get; }
        public ICommand DeletePayGroupCommand { get; }

        public PaymentGroupViewModel(IPayGroupService payGroupService, IDialogService dialogService)
        {
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            PayGroups = new ObservableCollection<PayGroup>();

            // Use the RelayCommand constructor overload for async methods
            LoadPayGroupsCommand = new RelayCommand(async _ => await LoadPayGroupsAsync());
            // Initialize other commands - actual implementation might be in code-behind as requested
            // AddPayGroupCommand = new RelayCommand(AddPayGroup);
            // EditPayGroupCommand = new RelayCommand(EditPayGroup, CanExecuteEditOrDelete); // Assuming parameter type matches
            // DeletePayGroupCommand = new RelayCommand(async param => await DeletePayGroupAsync(param as PayGroup), CanExecuteEditOrDelete); // Assuming parameter type matches

            // Load data initially
            _ = LoadPayGroupsAsync();
        }

        private async Task LoadPayGroupsAsync()
        {
            IsLoading = true;
            try
            {
                var groups = await _payGroupService.GetAllPayGroupsAsync();
                PayGroups.Clear();
                foreach (var group in groups.OrderBy(g => g.PayGroupId))
                {
                    PayGroups.Add(group);
                }
            }
            catch (Exception ex)
            {
                // Log error
                await _dialogService.ShowMessageBoxAsync("Error Loading Payment Groups", $"An error occurred: {ex.Message}"); // This call matches the interface
            }
            finally
            {
                IsLoading = false;
            }
        }

        // Placeholder methods for commands if needed later, or if refactoring from code-behind
        // private void AddPayGroup() { /* Logic to open add dialog */ }
        // private void EditPayGroup(PayGroup group) { /* Logic to open edit dialog */ }
        // private async Task DeletePayGroupAsync(PayGroup group) { /* Logic to confirm and delete */ }
        // private bool CanExecuteEditOrDelete(PayGroup group) => group != null;

    }

    // Removed the local BaseViewModel definition as it should use the shared one.
}
