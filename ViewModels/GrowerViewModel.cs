using System;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Interfaces;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Views;
using WPFGrowerApp.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;

using WPFGrowerApp.Services; // Added for IDialogService
using System.Collections; // For IEnumerable
using System.ComponentModel; // For INotifyDataErrorInfo
using System.Linq; // For Linq extensions
using System.Runtime.CompilerServices; // For CallerMemberName

namespace WPFGrowerApp.ViewModels
{
    // Implement INotifyDataErrorInfo
    public class GrowerViewModel : ViewModelBase, INotifyDataErrorInfo 
    {
        private readonly IGrowerService _growerService;
        private readonly IPayGroupService _payGroupService;
        private readonly IDialogService _dialogService; // Added DialogService field
        private Grower _currentGrower;
        private bool _isLoading;
        private bool _isSaving;
        private string _statusMessage;
        private List<PayGroup> _payGroups;
        private List<string> _provinces;
        private List<PriceClass> _priceClasses;
        private readonly Dictionary<string, List<string>> _errorsByPropertyName = new Dictionary<string, List<string>>(); // For INotifyDataErrorInfo

        // Inject services including IDialogService
        public GrowerViewModel(IGrowerService growerService, IPayGroupService payGroupService, IDialogService dialogService)
        {
            _growerService = growerService ?? throw new ArgumentNullException(nameof(growerService));
            _payGroupService = payGroupService ?? throw new ArgumentNullException(nameof(payGroupService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService)); 

            // Initialize commands FIRST
            SaveCommand = new RelayCommand(SaveCommandExecuteAsync, CanExecuteSaveCommand); 
            NewCommand = new RelayCommand(NewCommandExecute);
            SearchCommand = new RelayCommand(SearchCommandExecuteAsync); // Will become async
            CancelCommand = new RelayCommand(CancelCommandExecuteAsync); // Will become async

            // Now set the initial grower, which triggers validation safely
            CurrentGrower = new Grower(); 

            // Initialization (loading dropdowns etc.) is handled by InitializeAsync
        }

        // Call this method after constructing the ViewModel
        public async Task InitializeAsync()
        {
            IsLoading = true;
            StatusMessage = "Initializing...";
            try
            {
                await LoadPayGroupsAsync();
                await LoadProvincesAsync();
                await LoadPriceClassesAsync();
                StatusMessage = "Ready.";
            }
            catch (Exception ex)
            {
                // Logging should happen in the service layer
                StatusMessage = $"Initialization failed: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Initialization failed: {ex.Message}", "Initialization Error");
                Infrastructure.Logging.Logger.Error("Initialization failed in GrowerViewModel", ex);
            }
            finally
            {
                IsLoading = false;
            }
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

        public List<PriceClass> PriceClasses
        {
            get => _priceClasses;
            set
            {
                if (_priceClasses != value)
                {
                    _priceClasses = value;
                    OnPropertyChanged();
                }
            }
        }

        private async Task LoadPayGroupsAsync()
        {
            PayGroups = (await _payGroupService.GetAllPayGroupsAsync()).ToList();
        }

        private async Task LoadProvincesAsync()
        {
            try
            {
                Provinces = await _growerService.GetUniqueProvincesAsync();
            }
            catch (Exception ex)
            {
                // Logged in service, re-throw to be caught by InitializeAsync
                StatusMessage = $"Error loading provinces: {ex.Message}"; // Keep status for now
                Infrastructure.Logging.Logger.Error("Error loading provinces in GrowerViewModel", ex);
                throw; 
            }
        }

        private async Task LoadPriceClassesAsync()
        {
            try
            {
                // Load price classes from database using connection string
                var connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"]?.ConnectionString;
                
                if (string.IsNullOrEmpty(connectionString))
                {
                    Infrastructure.Logging.Logger.Error("Connection string 'DefaultConnection' not found");
                    PriceClasses = new List<PriceClass>();
                    return;
                }
                
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var sql = "SELECT PriceClassId, ClassCode, ClassName, Description, IsActive FROM PriceClasses WHERE IsActive = 1 ORDER BY PriceClassId";
                    PriceClasses = (await Dapper.SqlMapper.QueryAsync<PriceClass>(connection, sql)).ToList();
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error loading price classes", ex);
                PriceClasses = new List<PriceClass>();
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
            // Also check IsLoading and HasErrors
            return !IsSaving && !IsLoading && !HasErrors; 
        }

        // Renamed and made async
        private async Task SaveCommandExecuteAsync(object parameter)
        {
            await SaveGrowerAsync(); // Await the async operation
        }

        private void NewCommandExecute(object parameter)
        {
            // Ensure not loading/saving before starting a new one
            if (IsLoading || IsSaving) return; 
            CreateNewGrower();
        }

        // Renamed and made async
        private async Task SearchCommandExecuteAsync(object parameter)
        {
            // This command on the GrowerViewModel itself is likely redundant now.
            // The initial search and load is handled by MainViewModel.
            // If a re-search functionality is needed from the GrowerView, 
            // it would likely involve navigating back or showing the dialog differently.
            // For now, let's clear the implementation or make it do nothing specific.
            // Option 1: Do nothing
             await Task.CompletedTask; // Placeholder if command needs to exist

            // Option 2: Show search again (but loading is handled by MainViewModel's navigation)
            // var (dialogResult, selectedGrowerNumber) = _dialogService.ShowGrowerSearchDialog();
            // if (dialogResult == true && selectedGrowerNumber.HasValue) {
            //    // Maybe raise an event or message for MainViewModel to handle the load?
            // }
        }

        // Renamed and made async
        private async Task CancelCommandExecuteAsync(object parameter)
        {
             // Ensure not loading/saving before cancelling
            if (IsLoading || IsSaving) return;

            if (CurrentGrower != null && !string.IsNullOrEmpty(CurrentGrower.GrowerNumber))
            {
                // If we're editing an existing grower, reload it to discard changes
                await LoadGrowerAsync(CurrentGrower.GrowerNumber); // Await the reload
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
                    // When the whole grower changes, re-validate all properties
                    ValidateGrower(); 
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrencyDisplay));
                    // Update command state as errors might have changed
                    SaveCommand.RaiseCanExecuteChanged(); 
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

        // Changed return type from async void to async Task
    public async Task LoadGrowerAsync(string growerNumber)
        {
            // Prevent concurrent loads
            if (IsLoading) return; 

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
                // Logging should happen in service layer
                StatusMessage = $"Error loading grower: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error loading grower: {ex.Message}", "Loading Error");
                Infrastructure.Logging.Logger.Error($"Error loading grower {growerNumber} in ViewModel", ex);
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

                // Validation is now handled by INotifyDataErrorInfo
                // Check HasErrors before attempting to save
                ValidateGrower(); // Ensure validation is run
                if (HasErrors)
                {
                    await _dialogService.ShowMessageBoxAsync("Please correct the validation errors before saving.", "Validation Error");
                    IsSaving = false;
                    return;
                }

                // SaveGrowerAsync now returns bool but throws on error
                await _growerService.SaveGrowerAsync(CurrentGrower);
                
                // If SaveGrowerAsync throws, the catch block below handles it
                StatusMessage = "Grower saved successfully.";
                await _dialogService.ShowMessageBoxAsync("Grower saved successfully.", "Save Success");
            }
            catch (Exception ex)
            {
                // Logging should happen in service layer
                StatusMessage = $"Error saving grower: {ex.Message}";
                await _dialogService.ShowMessageBoxAsync($"Error saving grower: {ex.Message}", "Save Error");
                Infrastructure.Logging.Logger.Error($"Error saving grower {CurrentGrower?.GrowerNumber} in ViewModel", ex);
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
                GrowerNumber = string.Empty, // This will be set by the database
                Currency = 'C', // Default to CAD
                PayGroup = "1", // Default pay group
                PriceLevel = 1, // Default price level
                ContractLimit = 0, // Default contract limit
                OnHold = false,
                ChargeGST = false
            };
            StatusMessage = "Created new grower.";
            // Clear previous errors when creating a new grower
            ClearErrors();
            ValidateGrower(); // Validate the new default grower
            SaveCommand.RaiseCanExecuteChanged();
        }

        #region INotifyDataErrorInfo Implementation

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool HasErrors => _errorsByPropertyName.Any();

    public IEnumerable GetErrors(string? propertyName)
        {
            return _errorsByPropertyName.ContainsKey(propertyName ?? string.Empty) ? _errorsByPropertyName[propertyName ?? string.Empty] : Enumerable.Empty<string>();
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            // Also raise CanExecuteChanged for commands dependent on validation
            SaveCommand.RaiseCanExecuteChanged(); 
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errorsByPropertyName.ContainsKey(propertyName))
                _errorsByPropertyName[propertyName] = new List<string>();

            if (!_errorsByPropertyName[propertyName].Contains(error))
            {
                _errorsByPropertyName[propertyName].Add(error);
                // Log the specific validation error being added
                Infrastructure.Logging.Logger.Warn($"Validation Error Added - Property: {propertyName}, Error: {error}"); 
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                // Clear all errors
                var propertiesWithErrors = _errorsByPropertyName.Keys.ToList();
                _errorsByPropertyName.Clear();
                foreach (var propName in propertiesWithErrors)
                {
                    OnErrorsChanged(propName);
                }
            }
            else if (_errorsByPropertyName.ContainsKey(propertyName))
            {
                // Clear errors for a specific property
                _errorsByPropertyName.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        // Central validation logic
        private void ValidateGrower()
        {
            ClearErrors(); // Clear existing errors before re-validating

            if (CurrentGrower == null) 
            {
                Infrastructure.Logging.Logger.Debug("ValidateGrower: CurrentGrower is null, skipping validation.");
                return; 
            }

            // Log values being validated
            Infrastructure.Logging.Logger.Debug($"ValidateGrower: Validating GrowerName='{CurrentGrower.GrowerName}', ChequeName='{CurrentGrower.ChequeName}', Postal='{CurrentGrower.Postal}'");

            // Example Validations:
            if (string.IsNullOrWhiteSpace(CurrentGrower.GrowerName))
            {
                AddError(nameof(CurrentGrower.GrowerName), "Grower Name cannot be empty.");
            }
            // Add more rules for GrowerName if needed (e.g., length)

            if (string.IsNullOrWhiteSpace(CurrentGrower.ChequeName))
            {
                AddError(nameof(CurrentGrower.ChequeName), "Cheque Name cannot be empty.");
            }

            // Example: Validate Postal Code (simple Canadian format check) - Trim whitespace first
            string postalTrimmed = CurrentGrower.Postal?.Trim(); // Use null-conditional operator and Trim()
            if (!string.IsNullOrWhiteSpace(postalTrimmed) && 
                !System.Text.RegularExpressions.Regex.IsMatch(postalTrimmed, @"^[A-Za-z]\d[A-Za-z][ -]?\d[A-Za-z]\d$"))
            {
                 AddError(nameof(CurrentGrower.Postal), "Invalid Postal Code format.");
            }
            
            // Example: Validate Acres (must be non-negative)
            if (CurrentGrower.Acres < 0)
            {
                 AddError(nameof(CurrentGrower.Acres), "Acres cannot be negative.");
            }

            // Add other validation rules for properties like City, Prov, Phone, etc.
        }

        // It's often useful to trigger validation when individual bound properties change.
        // This requires modifying the Grower model or creating wrapper properties in the ViewModel.
        // For simplicity here, we call ValidateGrower() when the whole CurrentGrower changes or before saving.
        // A more robust implementation would validate individual properties as they change.

        #endregion

    } // End of GrowerViewModel class
} // End of namespace
