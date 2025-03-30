using System;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Views;
using WPFGrowerApp.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WPFGrowerApp.ViewModels
{
    public class GrowerViewModel : ViewModelBase
    {
        private readonly IGrowerService _growerService;
        private readonly IPayGroupService _payGroupService;
        private Grower _currentGrower;
        private bool _isLoading;
        private bool _isSaving;
        private string _statusMessage;
        private List<PayGroup> _payGroups;
        private List<string> _provinces;

        public GrowerViewModel(IGrowerService growerService, IPayGroupService payGroupService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            CurrentGrower = new Grower();
            SaveCommand = new RelayCommand(SaveCommandExecute, CanExecuteSaveCommand);
            NewCommand = new RelayCommand(NewCommandExecute);
            SearchCommand = new RelayCommand(SearchCommandExecute);
            CancelCommand = new RelayCommand(CancelCommandExecute);
            LoadPayGroupsAsync().ConfigureAwait(false);
            LoadProvincesAsync().ConfigureAwait(false);
        }

        public List<PayGroup> PayGroups
        {
            get => _payGroups;
            set
            {
                if (_payGroups != value)
                {
                    _payGroups = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> Provinces
        {
            get => _provinces;
            set
            {
                if (_provinces != value)
                {
                    _provinces = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LoadPayGroupsAsync()
        {
            PayGroups = await _payGroupService.GetPayGroupsAsync();
        }

        private async Task LoadProvincesAsync()
        {
            try
            {
                Provinces = await _growerService.GetUniqueProvincesAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading provinces: {ex.Message}";
            }
        }

        public string CurrencyDisplay
        {
            get
            {
                if (CurrentGrower == null) return "CAD";
                return CurrentGrower.Currency == 'U' ? "USD" : "CAD";
            }
            set
            {
                if (CurrentGrower != null)
                {
                    CurrentGrower.Currency = value == "USD" ? 'U' : 'C';
                    OnPropertyChanged();
                }
            }
        }

        private bool CanExecuteSaveCommand(object parameter)
        {
            return !IsSaving;
        }

        private void SaveCommandExecute(object parameter)
        {
            SaveGrowerAsync().ConfigureAwait(false);
        }

        private void NewCommandExecute(object parameter)
        {
            CreateNewGrower();
        }

        private void SearchCommandExecute(object parameter)
        {
            var searchView = new GrowerSearchView();
            
            if (searchView.ShowDialog() == true)
            {
                if (searchView.SelectedGrowerNumber.HasValue)
                {
                    if (searchView.SelectedGrowerNumber.Value == 0)
                    {
                        // Create new grower
                        CreateNewGrower();
                    }
                    else
                    {
                        // Load existing grower
                        LoadGrowerAsync(searchView.SelectedGrowerNumber.Value);
                    }
                }
            }
        }

        private void CancelCommandExecute(object parameter)
        {
            if (CurrentGrower != null && CurrentGrower.GrowerNumber > 0)
            {
                // If we're editing an existing grower, reload it to discard changes
                LoadGrowerAsync(CurrentGrower.GrowerNumber);
            }
            else
            {
                // If we're creating a new grower, clear the form
                CurrentGrower = new Grower();
                StatusMessage = "Operation cancelled.";
            }
        }

        public Grower CurrentGrower
        {
            get => _currentGrower;
            set
            {
                if (_currentGrower != value)
                {
                    _currentGrower = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrencyDisplay));
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                if (_isSaving != value)
                {
                    _isSaving = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public RelayCommand SaveCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand SearchCommand { get; }
        public RelayCommand CancelCommand { get; }

        public async void LoadGrowerAsync(decimal growerNumber)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading grower...";

                var grower = await _growerService.GetGrowerByNumberAsync(growerNumber);
                
                if (grower != null)
                {
                    CurrentGrower = grower;
                    StatusMessage = $"Grower {growerNumber} loaded successfully.";
                }
                else
                {
                    StatusMessage = $"Grower {growerNumber} not found.";
                    CurrentGrower = new Grower { GrowerNumber = growerNumber };
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading grower: {ex.Message}";
                MessageBox.Show($"Error loading grower: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task SaveGrowerAsync()
        {
            try
            {
                IsSaving = true;
                StatusMessage = "Saving grower...";

                // Validate required fields
                if (string.IsNullOrWhiteSpace(CurrentGrower.GrowerName))
                {
                    MessageBox.Show("Grower Name is required.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(CurrentGrower.ChequeName))
                {
                    MessageBox.Show("Cheque Name is required.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                bool success = await _growerService.SaveGrowerAsync(CurrentGrower);
                
                if (success)
                {
                    StatusMessage = "Grower saved successfully.";
                    MessageBox.Show("Grower saved successfully.", "Success", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusMessage = "Failed to save grower.";
                    MessageBox.Show("Failed to save grower.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving grower: {ex.Message}";
                MessageBox.Show($"Error saving grower: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void CreateNewGrower()
        {
            CurrentGrower = new Grower
            {
                GrowerNumber = 0, // This will be set by the database
                Currency = 'C', // Default to CAD
                PayGroup = "1", // Default pay group
                PriceLevel = 1, // Default price level
                ContractLimit = 0, // Default contract limit
                OnHold = false,
                ChargeGST = false
            };
            StatusMessage = "Created new grower.";
        }
    }
}
