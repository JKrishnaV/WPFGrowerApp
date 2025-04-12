using System;
using System.ComponentModel;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using MaterialDesignThemes.Wpf; // Add this for DialogHost

namespace WPFGrowerApp.ViewModels.Dialogs
{
    public class PayGroupEditDialogViewModel : ViewModelBase, IDataErrorInfo
    {
        private PayGroup _payGroup;
        private bool _isEditMode;
        private string _title;

        public PayGroup PayGroupData
        {
            get => _payGroup;
            set => SetProperty(ref _payGroup, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        // Flag to indicate if the dialog was saved
        public bool WasSaved { get; private set; } = false;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public PayGroupEditDialogViewModel(PayGroup payGroup = null)
        {
            if (payGroup == null)
            {
                // Add Mode
                PayGroupData = new PayGroup();
                IsEditMode = false;
                Title = "Add New Payment Group";
            }
            else
            {
                // Edit Mode - Create a copy to avoid modifying the original until save
                PayGroupData = new PayGroup
                {
                    PayGroupId = payGroup.PayGroupId,
                    Description = payGroup.Description,
                    DefaultPayLevel = payGroup.DefaultPayLevel
                    // Copy other relevant properties if needed, but not audit fields
                };
                IsEditMode = true;
                Title = "Edit Payment Group";
            }

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel); // Cancel action might be handled by DialogHost closing
        }

        private void Save(object parameter)
        {
            // Perform final validation if needed
            if (!IsValid())
            {
                // Optionally show a message, though IDataErrorInfo should highlight fields
                return; 
            }
            WasSaved = true;
            // Explicitly close the dialog with 'true' indicating success/save
            DialogHost.CloseDialogCommand.Execute(true, null); 
        }

        private bool CanSave(object parameter)
        {
            // Basic validation check
            return IsValid();
        }
        
        private bool IsValid()
        {
             return !string.IsNullOrWhiteSpace(PayGroupData?.PayGroupId) &&
                    !string.IsNullOrWhiteSpace(PayGroupData?.Description) &&
                    Error == null; // Check IDataErrorInfo
        }

        private void Cancel(object parameter)
        {
            WasSaved = false;
            // Explicitly close the dialog with 'false' indicating cancellation
            DialogHost.CloseDialogCommand.Execute(false, null); 
        }

        // --- IDataErrorInfo Implementation for basic validation ---
        public string Error => null; // Overall entity error (not used here)

        public string this[string columnName]
        {
            get
            {
                string result = null;
                if (PayGroupData == null) return result;

                switch (columnName)
                {
                    case nameof(PayGroupData.PayGroupId):
                        if (string.IsNullOrWhiteSpace(PayGroupData.PayGroupId))
                            result = "Payment Group ID cannot be empty.";
                        else if (PayGroupData.PayGroupId.Length > 1 && !IsEditMode) // Example: Check length (adjust as needed)
                             result = "Payment Group ID cannot exceed 1 character."; // Assuming NVARCHAR(1)
                        break;
                    case nameof(PayGroupData.Description):
                        if (string.IsNullOrWhiteSpace(PayGroupData.Description))
                            result = "Description cannot be empty.";
                         else if (PayGroupData.Description.Length > 30) 
                             result = "Description cannot exceed 30 characters.";
                        break;
                    case nameof(PayGroupData.DefaultPayLevel):
                        // Add validation if needed (e.g., range check)
                        // if (PayGroupData.DefaultPayLevel < 0 || PayGroupData.DefaultPayLevel > 9)
                        //     result = "Default Pay Level must be between 0 and 9.";
                        break;
                }
                // Update CanSave when validation state changes
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                return result;
            }
        }
    }
}
