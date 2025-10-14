using System;
using System.ComponentModel;
using System.Windows.Input;
using WPFGrowerApp.Commands;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    public class ProcessEditDialogViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly Process _originalProcess;
        private readonly IDialogService _dialogService;
        private readonly bool _isEditMode;
        private readonly bool _isReadOnly;

        private string _title = string.Empty;
        private int _processId;
        private string _processCode = string.Empty;
        private string _processName = string.Empty;
        private string _description = string.Empty;
        private bool _isActive = true;
        private int? _displayOrder;
        private int? _defaultGrade;
        private int? _processClass;
        private string _gradeName1 = string.Empty;
        private string _gradeName2 = string.Empty;
        private string _gradeName3 = string.Empty;
        private bool _hasUnsavedChanges;

        public Process ProcessData { get; private set; } = new Process();

        public bool IsEditMode => _isEditMode;
        public bool IsReadOnly => _isReadOnly;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public int ProcessId
        {
            get => _processId;
            set
            {
                if (SetProperty(ref _processId, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public string ProcessCode
        {
            get => _processCode;
            set
            {
                if (SetProperty(ref _processCode, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public string ProcessName
        {
            get => _processName;
            set
            {
                if (SetProperty(ref _processName, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (SetProperty(ref _isActive, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public int? DisplayOrder
        {
            get => _displayOrder;
            set
            {
                if (SetProperty(ref _displayOrder, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public int? DefaultGrade
        {
            get => _defaultGrade;
            set
            {
                if (SetProperty(ref _defaultGrade, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public int? ProcessClass
        {
            get => _processClass;
            set
            {
                if (SetProperty(ref _processClass, value))
                {
                    _hasUnsavedChanges = true;
                    OnPropertyChanged(nameof(Error));
                }
            }
        }

        public string GradeName1
        {
            get => _gradeName1;
            set
            {
                if (SetProperty(ref _gradeName1, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string GradeName2
        {
            get => _gradeName2;
            set
            {
                if (SetProperty(ref _gradeName2, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        public string GradeName3
        {
            get => _gradeName3;
            set
            {
                if (SetProperty(ref _gradeName3, value))
                {
                    _hasUnsavedChanges = true;
                }
            }
        }

        // Legacy compatibility properties
        public int DefGrade => DefaultGrade ?? 0;
        public int ProcClass => ProcessClass ?? 0;

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public ProcessEditDialogViewModel(Process process, bool isReadOnly, IDialogService dialogService)
        {
            _originalProcess = process ?? throw new ArgumentNullException(nameof(process));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _isEditMode = process.ProcessId > 0;
            _isReadOnly = isReadOnly;

            // Set title based on mode
            Title = isReadOnly ? "View Process Type" : (_isEditMode ? "Edit Process Type" : "Add New Process Type");

            // Initialize properties from process
            _processId = process.ProcessId;
            _processCode = process.ProcessCode ?? string.Empty;
            _processName = process.ProcessName ?? string.Empty;
            _description = process.Description ?? string.Empty;
            _isActive = process.IsActive;
            _displayOrder = process.DisplayOrder;
            _defaultGrade = process.DefaultGrade;
            _processClass = process.ProcessClass;
            _gradeName1 = process.GradeName1 ?? string.Empty;
            _gradeName2 = process.GradeName2 ?? string.Empty;
            _gradeName3 = process.GradeName3 ?? string.Empty;

            _hasUnsavedChanges = false;

            // Initialize commands
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel, param => true);
        }

        private bool CanSave(object parameter)
        {
            // Can save if not in read-only mode and there are no validation errors
            return !IsReadOnly && string.IsNullOrEmpty(Error);
        }

        private void Save(object parameter)
        {
            if (!CanSave(parameter))
                return;

            // Update the process data
            ProcessData = new Process
            {
                ProcessId = ProcessId,
                ProcessCode = ProcessCode?.Trim() ?? string.Empty,
                ProcessName = ProcessName?.Trim() ?? string.Empty,
                Description = Description?.Trim(),
                IsActive = IsActive,
                DisplayOrder = DisplayOrder,
                DefaultGrade = DefaultGrade,
                ProcessClass = ProcessClass,
                GradeName1 = GradeName1?.Trim(),
                GradeName2 = GradeName2?.Trim(),
                GradeName3 = GradeName3?.Trim()
            };

            // Close dialog with success result
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(true, null);
        }

        private void Cancel(object parameter)
        {
            if (_hasUnsavedChanges && !IsReadOnly)
            {
                // Could add confirmation dialog here if needed
            }

            // Close dialog with cancel result
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(false, null);
        }

        #region IDataErrorInfo Implementation

        public string Error
        {
            get
            {
                // Return first error found
                if (!string.IsNullOrWhiteSpace(this[nameof(ProcessId)]))
                    return this[nameof(ProcessId)];
                if (!string.IsNullOrWhiteSpace(this[nameof(ProcessCode)]))
                    return this[nameof(ProcessCode)];
                if (!string.IsNullOrWhiteSpace(this[nameof(Description)]))
                    return this[nameof(Description)];
                if (!string.IsNullOrWhiteSpace(this[nameof(DefGrade)]))
                    return this[nameof(DefGrade)];
                if (!string.IsNullOrWhiteSpace(this[nameof(ProcClass)]))
                    return this[nameof(ProcClass)];

                return string.Empty;
            }
        }

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;

                switch (columnName)
                {
                    case nameof(ProcessId):
                        if (ProcessId <= 0)
                            error = "Process ID must be a positive integer.";
                        break;

                    case nameof(ProcessCode):
                        if (string.IsNullOrWhiteSpace(ProcessCode))
                            error = "Process Code is required.";
                        else if (ProcessCode.Length > 10)
                            error = "Process Code must be 10 characters or less.";
                        break;

                    case nameof(ProcessName):
                        if (string.IsNullOrWhiteSpace(ProcessName))
                            error = "Process Name is required.";
                        else if (ProcessName.Length > 100)
                            error = "Process Name must be 100 characters or less.";
                        break;

                    case nameof(Description):
                        if (!string.IsNullOrWhiteSpace(Description) && Description.Length > 500)
                            error = "Description must be 500 characters or less.";
                        break;

                    case nameof(DefaultGrade):
                        if (DefaultGrade.HasValue && (DefaultGrade < 1 || DefaultGrade > 3))
                            error = "Default Grade must be between 1 and 3.";
                        break;

                    case nameof(ProcessClass):
                        if (ProcessClass.HasValue && (ProcessClass < 1 || ProcessClass > 4))
                            error = "Process Class must be between 1 and 4.";
                        break;

                    case nameof(GradeName1):
                        if (!string.IsNullOrWhiteSpace(GradeName1) && GradeName1.Length > 20)
                            error = "Grade Name 1 must be 20 characters or less.";
                        break;

                    case nameof(GradeName2):
                        if (!string.IsNullOrWhiteSpace(GradeName2) && GradeName2.Length > 20)
                            error = "Grade Name 2 must be 20 characters or less.";
                        break;

                    case nameof(GradeName3):
                        if (!string.IsNullOrWhiteSpace(GradeName3) && GradeName3.Length > 20)
                            error = "Grade Name 3 must be 20 characters or less.";
                        break;
                }

                return error;
            }
        }

        #endregion
    }
}
