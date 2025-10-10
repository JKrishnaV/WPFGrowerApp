using System.Windows.Input;
using WPFGrowerApp.Commands;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// ViewModel for modern Material Design input dialogs
    /// </summary>
    public class InputDialogViewModel : ViewModelBase
    {
        private string _message = string.Empty;
        private string _title = string.Empty;
        private string _inputText = string.Empty;
        private string _placeholderText = "Enter text here...";
        private bool _isMultiline;
        private bool _result;

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string InputText
        {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public string PlaceholderText
        {
            get => _placeholderText;
            set => SetProperty(ref _placeholderText, value);
        }

        public bool IsMultiline
        {
            get => _isMultiline;
            set => SetProperty(ref _isMultiline, value);
        }

        public bool Result
        {
            get => _result;
            private set => SetProperty(ref _result, value);
        }

        public ICommand OkCommand { get; }
        public ICommand CancelCommand { get; }

        public InputDialogViewModel()
        {
            OkCommand = new RelayCommand(_ => Ok());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        public InputDialogViewModel(string message, string title, string? initialText = null, string? placeholder = null, bool multiline = false)
            : this()
        {
            Message = message;
            Title = title;
            InputText = initialText ?? string.Empty;
            PlaceholderText = placeholder ?? "Enter text here...";
            IsMultiline = multiline;
        }

        private void Ok()
        {
            Result = true;
            CloseDialog();
        }

        private void Cancel()
        {
            Result = false;
            InputText = string.Empty;
            CloseDialog();
        }

        private void CloseDialog()
        {
            // This will be handled by the DialogHost
            MaterialDesignThemes.Wpf.DialogHost.CloseDialogCommand.Execute(null, null);
        }
    }
}
