using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.Infrastructure;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for adding or editing a container type.
    /// </summary>
    public partial class ContainerEntryViewModel : ObservableValidator
    {
        private readonly ContainerTypeService _containerService;
        private readonly IDialogService _dialogService;
        private readonly string _currentUser;
        private readonly bool _isEditMode;
        private readonly int _originalContainerId;

        [ObservableProperty]
        private string _windowTitle;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Container ID is required")]
        [Range(1, 20, ErrorMessage = "Container ID must be between 1 and 20")]
        private int _containerId;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Description is required")]
        [MaxLength(30, ErrorMessage = "Description cannot exceed 30 characters")]
        private string _description = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Short code is required")]
        [MaxLength(6, ErrorMessage = "Short code cannot exceed 6 characters")]
        [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "Short code can only contain letters, numbers, and hyphens")]
        private string _shortCode = string.Empty;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(0, 999, ErrorMessage = "Tare weight must be between 0 and 999")]
        private decimal? _tareWeight;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(0, 9999.99, ErrorMessage = "Value must be between 0 and 9999.99")]
        private decimal? _value;

        [ObservableProperty]
        private bool _inUse;

        [ObservableProperty]
        private bool _isSaving;

        public ContainerEntryViewModel(
            ContainerTypeService containerService,
            IDialogService dialogService,
            string currentUser,
            ContainerType? containerToEdit = null,
            bool isEditMode = false)
        {
            _containerService = containerService ?? throw new ArgumentNullException(nameof(containerService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _currentUser = currentUser ?? "SYSTEM";
            _isEditMode = isEditMode;

            if (containerToEdit != null)
            {
                ContainerId = containerToEdit.ContainerId;
                Description = containerToEdit.Description;
                ShortCode = containerToEdit.ShortCode;
                TareWeight = containerToEdit.TareWeight;
                Value = containerToEdit.Value;
                InUse = containerToEdit.InUse;
                _originalContainerId = containerToEdit.ContainerId;
            }
            else
            {
                // Default values for new container
                InUse = true;
                _originalContainerId = 0;
            }

            WindowTitle = _isEditMode 
                ? $"Edit Container Type - ID# {ContainerId}" 
                : "Add New Container Type";
        }

        /// <summary>
        /// Saves the container type (create or update).
        /// </summary>
        [RelayCommand]
        private async Task Save()
        {
            // Validate all properties
            ValidateAllProperties();

            if (HasErrors)
            {
                await _dialogService.ShowMessageBoxAsync(
                    "Please fix the validation errors before saving.",
                    "Validation Error");
                return;
            }

            try
            {
                IsSaving = true;

                var containerType = new ContainerType
                {
                    ContainerId = ContainerId,
                    Description = Description.Trim(),
                    ShortCode = ShortCode.Trim().ToUpper(),
                    TareWeight = TareWeight,
                    Value = Value,
                    InUse = InUse
                };

                bool success;

                if (_isEditMode)
                {
                    success = await _containerService.UpdateAsync(containerType, _currentUser);
                }
                else
                {
                    success = await _containerService.CreateAsync(containerType, _currentUser);
                }

                if (success)
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Container type {(_isEditMode ? "updated" : "created")} successfully!",
                        "Success");

                    RequestClose?.Invoke(true);
                }
                else
                {
                    await _dialogService.ShowMessageBoxAsync(
                        $"Failed to {(_isEditMode ? "update" : "create")} container type. Please try again.",
                        "Error");
                }
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate container ID or short code
                await _dialogService.ShowMessageBoxAsync(
                    ex.Message,
                    "Duplicate Error");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync(
                    $"Error saving container type:\n{ex.Message}",
                    "Error");
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Cancels the operation and closes the window.
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        /// <summary>
        /// Event to request the view to close the window.
        /// </summary>
        public event Action<bool>? RequestClose;

        /// <summary>
        /// Gets whether the Container ID field should be read-only.
        /// Container ID cannot be changed in edit mode.
        /// </summary>
        public bool IsContainerIdReadOnly => _isEditMode;
    }
}
