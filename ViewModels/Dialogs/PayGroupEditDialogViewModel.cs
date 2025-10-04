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
                PayGroupData = new PayGroup
                {
                    GroupCode = "",
                    GroupName = "",
                    Description = "",
                    DefaultPriceLevel = 1, // Default to Level 1
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = "SYSTEM"
                };
                IsEditMode = false;
                Title = "Add New Payment Group";
            }
            else
            {
                // Edit Mode - Create a copy to avoid modifying the original until save
                PayGroupData = new PayGroup
                {
                    PaymentGroupId = payGroup.PaymentGroupId,
                    GroupCode = payGroup.GroupCode,
                    GroupName = payGroup.GroupName,
                    Description = payGroup.Description,
                    DefaultPriceLevel = payGroup.DefaultPriceLevel,
                    IsActive = payGroup.IsActive,
                    CreatedAt = payGroup.CreatedAt,
                    CreatedBy = payGroup.CreatedBy
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
                    case nameof(PayGroupData.GroupCode):
                        if (string.IsNullOrWhiteSpace(PayGroupData.GroupCode))
                            result = "Group Code cannot be empty.";
                        else if (PayGroupData.GroupCode.Length > 10 && !IsEditMode)
                             result = "Group Code cannot exceed 10 characters.";
                        break;
                    case nameof(PayGroupData.GroupName):
                        if (string.IsNullOrWhiteSpace(PayGroupData.GroupName))
                            result = "Group Name cannot be empty.";
                        else if (PayGroupData.GroupName.Length > 100) 
                             result = "Group Name cannot exceed 100 characters.";
                        break;
                    case nameof(PayGroupData.Description):
                        if (PayGroupData.Description != null && PayGroupData.Description.Length > 500) 
                             result = "Description cannot exceed 500 characters.";
                        break;
                    case nameof(PayGroupData.DefaultPriceLevel):
                        if (PayGroupData.DefaultPriceLevel.HasValue)
                        {
                            if (PayGroupData.DefaultPriceLevel < 1 || PayGroupData.DefaultPriceLevel > 3)
                                result = "Default Price Level must be between 1 and 3.";
                        }
                        break;
                }
                // Update CanSave when validation state changes
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                return result;
            }
        }
    }
}
