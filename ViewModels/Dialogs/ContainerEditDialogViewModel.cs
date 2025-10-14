using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;
using WPFGrowerApp.Infrastructure.Logging;
using MaterialDesignThemes.Wpf;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    /// <summary>
    /// ViewModel for the container edit/view dialog.
    /// </summary>
    public class ContainerEditDialogViewModel : ViewModelBase
    {
        private readonly IDialogService _dialogService;
        private readonly bool _isReadOnly;

        private ContainerType _containerData;
        private bool _isSaving;
        private string _windowTitle;

        public ContainerType ContainerData
        {
            get => _containerData;
            set => SetProperty(ref _containerData, value);
        }

        public bool IsSaving
        {
            get => _isSaving;
            set => SetProperty(ref _isSaving, value);
        }

        public string WindowTitle
        {
            get => _windowTitle;
            set => SetProperty(ref _windowTitle, value);
        }

        public bool IsReadOnly => _isReadOnly;

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ContainerEditDialogViewModel(ContainerType container, bool isReadOnly, IDialogService dialogService)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _isReadOnly = isReadOnly;
            
            // Create a copy of the container data
            _containerData = new ContainerType
            {
                ContainerId = container.ContainerId,
                ContainerCode = container.ContainerCode,
                ContainerName = container.ContainerName,
                TareWeight = container.TareWeight,
                Value = container.Value,
                IsActive = container.IsActive,
                DisplayOrder = container.DisplayOrder,
                CreatedAt = container.CreatedAt,
                CreatedBy = container.CreatedBy,
                ModifiedAt = container.ModifiedAt,
                ModifiedBy = container.ModifiedBy,
                DeletedAt = container.DeletedAt,
                DeletedBy = container.DeletedBy
            };

            WindowTitle = isReadOnly ? $"View Container - {container.ContainerCode}" : 
                                     (container.ContainerId == 0 ? "Add New Container" : $"Edit Container - {container.ContainerCode}");

            // Initialize commands
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !_isReadOnly);
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private async Task SaveAsync()
        {
            if (!ValidateContainer()) return;

            try
            {
                IsSaving = true;
                // The actual save logic will be handled by the parent ViewModel
                // This dialog just validates and closes with the data
                DialogHost.Close("RootDialogHost", true);
            }
            catch (InvalidOperationException ex)
            {
                // DialogHost is not open - ignore this error as the dialog is already closed
                Logger.Warn("DialogHost.Close called when dialog was not open (Save)", ex);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowMessageBoxAsync($"Error saving container: {ex.Message}", "Error");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void Cancel()
        {
            try
            {
                DialogHost.Close("RootDialogHost", false);
            }
            catch (InvalidOperationException ex)
            {
                // DialogHost is not open - ignore this error as the dialog is already closed
                Logger.Warn("DialogHost.Close called when dialog was not open (Cancel)", ex);
            }
        }

        /// <summary>
        /// Validates the container data.
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool ValidateContainer()
        {
            if (string.IsNullOrWhiteSpace(ContainerData.ContainerCode))
            {
                _dialogService.ShowMessageBoxAsync("Container Code is required.", "Validation Error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ContainerData.ContainerName))
            {
                _dialogService.ShowMessageBoxAsync("Container Name is required.", "Validation Error");
                return false;
            }

            if (ContainerData.ContainerCode.Length > 10)
            {
                _dialogService.ShowMessageBoxAsync("Container Code cannot exceed 10 characters.", "Validation Error");
                return false;
            }

            if (ContainerData.ContainerName.Length > 100)
            {
                _dialogService.ShowMessageBoxAsync("Container Name cannot exceed 100 characters.", "Validation Error");
                return false;
            }

            if (ContainerData.TareWeight.HasValue && ContainerData.TareWeight.Value < 0)
            {
                _dialogService.ShowMessageBoxAsync("Tare Weight cannot be negative.", "Validation Error");
                return false;
            }

            if (ContainerData.Value.HasValue && ContainerData.Value.Value < 0)
            {
                _dialogService.ShowMessageBoxAsync("Container Value cannot be negative.", "Validation Error");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the formatted display text for the container.
        /// </summary>
        public string DisplayText => $"[{ContainerData.ContainerId}] {ContainerData.ContainerCode} - {ContainerData.ContainerName}";
    }
}
