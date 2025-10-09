using System.Windows.Input;
using WPFGrowerApp.Commands;
using MaterialDesignThemes.Wpf;

namespace WPFGrowerApp.ViewModels.Dialogs
{
    /// <summary>
    /// ViewModel for displaying contextual help content
    /// </summary>
    public class HelpDialogViewModel : ViewModelBase
    {
        private string _title = string.Empty;
        private string _content = string.Empty;
        private string? _quickTips;
        private string? _keyboardShortcuts;

        /// <summary>
        /// The title of the help section (e.g., "User Management Help")
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// The main help content (HTML or rich text)
        /// </summary>
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        /// <summary>
        /// Quick tips section
        /// </summary>
        public string? QuickTips
        {
            get => _quickTips;
            set => SetProperty(ref _quickTips, value);
        }

        /// <summary>
        /// Keyboard shortcuts section
        /// </summary>
        public string? KeyboardShortcuts
        {
            get => _keyboardShortcuts;
            set => SetProperty(ref _keyboardShortcuts, value);
        }

        /// <summary>
        /// Command to close the help dialog
        /// </summary>
        public ICommand CloseCommand { get; }

        public HelpDialogViewModel(string title, string content, string? quickTips = null, string? keyboardShortcuts = null)
        {
            Title = title;
            Content = content;
            QuickTips = quickTips;
            KeyboardShortcuts = keyboardShortcuts;

            CloseCommand = new RelayCommand(Close);
        }

        private void Close(object parameter)
        {
            DialogHost.CloseDialogCommand.Execute(null, null);
        }
    }
}

