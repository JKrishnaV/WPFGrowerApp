using System;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Views;
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

        public GrowerViewModel(IGrowerService growerService, IPayGroupService payGroupService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            CurrentGrower = new Grower();
            SaveCommand = new RelayCommand(SaveCommandExecute, CanExecuteSaveCommand);
            NewCommand = new RelayCommand(NewCommandExecute);
            SearchCommand = new RelayCommand(SearchCommandExecute);
            LoadPayGroupsAsync().ConfigureAwait(false);
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

        private async Task LoadPayGroupsAsync()
        {
            PayGroups = await _payGroupService.GetPayGroupsAsync();
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
            SearchGrower();
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

                bool success = await _growerService.SaveGrowerAsync(CurrentGrower);
                
                if (success)
                {
                    StatusMessage = "Grower saved successfully.";
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
            CurrentGrower = new Grower();
            StatusMessage = "Created new grower.";
        }

        private void SearchGrower()
        {
            var searchView = new GrowerSearchView();
            
            if (searchView.ShowDialog() == true && searchView.SelectedGrowerNumber.HasValue)
            {
                LoadGrowerAsync(searchView.SelectedGrowerNumber.Value);
            }
        }
    }
}
