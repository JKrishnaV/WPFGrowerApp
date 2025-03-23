using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Models;

namespace WPFGrowerApp.ViewModels
{
    public class GrowerViewModel : INotifyPropertyChanged
    {
        private readonly GrowerService _growerService;
        private Grower _currentGrower;
        private bool _isLoading;
        private bool _isSaving;
        private bool _isNew;
        private List<string> _existingGrowerNames;
        private List<string> _existingChequeNames;

        public GrowerViewModel(GrowerService growerService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            
            SaveCommand = new AsyncRelayCommand(SaveCommandExecute, CanExecuteSaveCommand);
            NewCommand = new RelayCommand(NewCommandExecute);
            
            // Load existing grower and cheque names for uniqueness validation
            LoadExistingNamesAsync();
        }
        
        private async void LoadExistingNamesAsync()
        {
            try
            {
                var growers = await _growerService.GetAllGrowersAsync();
                _existingGrowerNames = growers.Select(g => g.GrowerName).ToList();
                _existingChequeNames = growers.Select(g => g.ChequeName).ToList();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading grower data: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task LoadGrowerAsync(decimal growerNumber)
        {
            try
            {
                IsLoading = true;
                _isNew = false;
                
                CurrentGrower = await _growerService.GetGrowerAsync(growerNumber);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading grower: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        public void CreateNewGrower()
        {
            CurrentGrower = new Grower();
            _isNew = true;
        }

        private async Task SaveAsync()
        {
            try
            {
                IsSaving = true;
                
                // Validate uniqueness of Grower Name and Cheque Name
                if (!ValidateUniqueness())
                {
                    return;
                }
                
                if (_isNew)
                {
                    // For new growers, get the next available grower number
                    var nextGrowerNumber = await _growerService.GetNextGrowerNumberAsync();
                    CurrentGrower.GrowerNumber = nextGrowerNumber;
                    
                    await _growerService.AddGrowerAsync(CurrentGrower);
                    System.Windows.MessageBox.Show($"Grower {CurrentGrower.GrowerNumber} created successfully.", "Success",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                else
                {
                    await _growerService.UpdateGrowerAsync(CurrentGrower);
                    System.Windows.MessageBox.Show($"Grower {CurrentGrower.GrowerNumber} updated successfully.", "Success",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                
                // Update the lists of existing names
                if (!_existingGrowerNames.Contains(CurrentGrower.GrowerName))
                {
                    _existingGrowerNames.Add(CurrentGrower.GrowerName);
                }
                
                if (!_existingChequeNames.Contains(CurrentGrower.ChequeName))
                {
                    _existingChequeNames.Add(CurrentGrower.ChequeName);
                }
                
                _isNew = false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving grower: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }
        
        private bool ValidateUniqueness()
        {
            // Skip validation if lists aren't loaded yet
            if (_existingGrowerNames == null || _existingChequeNames == null)
            {
                return true;
            }
            
            bool isValid = true;
            string errorMessage = "";
            
            // For existing growers, we need to exclude the current grower from the uniqueness check
            var growerNamesToCheck = _isNew ? _existingGrowerNames : 
                _existingGrowerNames.Where(n => n != CurrentGrower.GrowerName).ToList();
                
            var chequeNamesToCheck = _isNew ? _existingChequeNames : 
                _existingChequeNames.Where(n => n != CurrentGrower.ChequeName).ToList();
            
            // Check Grower Name uniqueness
            if (!string.IsNullOrWhiteSpace(CurrentGrower.GrowerName) && 
                growerNamesToCheck.Contains(CurrentGrower.GrowerName))
            {
                errorMessage += "A grower with this name already exists.\n";
                isValid = false;
            }
            
            // Check Cheque Name uniqueness
            if (!string.IsNullOrWhiteSpace(CurrentGrower.ChequeName) && 
                chequeNamesToCheck.Contains(CurrentGrower.ChequeName))
            {
                errorMessage += "A grower with this cheque name already exists.";
                isValid = false;
            }
            
            if (!isValid)
            {
                System.Windows.MessageBox.Show(errorMessage, "Validation Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
            
            return isValid;
        }

        private void NewCommandExecute(object parameter)
        {
            CreateNewGrower();
        }

        private bool CanExecuteSaveCommand(object parameter)
        {
            return !IsLoading && !IsSaving && CurrentGrower != null && string.IsNullOrEmpty(CurrentGrower.Error);
        }

        private async Task SaveCommandExecute(object parameter)
        {
            await SaveAsync();
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
                    CommandManager.InvalidateRequerySuggested();
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
                    CommandManager.InvalidateRequerySuggested();
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
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand NewCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
