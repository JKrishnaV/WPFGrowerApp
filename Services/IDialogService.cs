using System.Threading.Tasks;
using System.Windows; // Added for Window

namespace WPFGrowerApp.Services
{
    // Defines different message box results if needed later
    public enum DialogResult
    {
        OK,
        Cancel,
        Yes,
        No
    }

    public interface IDialogService
    {
        /// <summary>
        /// Shows a simple message box with an OK button using Material Design DialogHost.
        /// </summary>
        Task ShowMessageBoxAsync(string message, string title); // Changed to async

        /// <summary>
        /// Shows a confirmation message box with Yes/No buttons using Material Design DialogHost.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        /// <returns>True if the user clicked Yes/OK, False otherwise.</returns>
        Task<bool> ShowConfirmationDialogAsync(string message, string title); // Changed to async

        /// <summary>
        /// Shows a modern Material Design input dialog for text entry.
        /// </summary>
        /// <param name="message">The message/prompt to display.</param>
        /// <param name="title">The title of the input dialog.</param>
        /// <param name="initialText">Optional initial text in the input field.</param>
        /// <param name="placeholder">Optional placeholder text.</param>
        /// <param name="multiline">Whether the input should be multiline.</param>
        /// <returns>The entered text, or null if cancelled.</returns>
        Task<string?> ShowInputDialogAsync(string message, string title, string? initialText = null, string? placeholder = null, bool multiline = false);
        
        /// <summary>
        /// Shows a modern Material Design input dialog for text entry (backward compatibility overload).
        /// </summary>
        /// <param name="message">The message/prompt to display.</param>
        /// <param name="title">The title of the input dialog.</param>
        /// <returns>The entered text, or null if cancelled.</returns>
        Task<string?> ShowInputDialogAsync(string message, string title);

        // Method specific to the Grower Search interaction (remains synchronous for now)
        GrowerSearchDialogResult ShowGrowerSearchDialog();

        /// <summary>
        /// Shows a custom dialog associated with the provided ViewModel using Material Design DialogHost.
        /// </summary>
        /// <param name="viewModel">The ViewModel for the dialog content.</param>
        /// <returns>A task representing the asynchronous operation. The result might indicate how the dialog was closed if needed.</returns>
        Task ShowDialogAsync(object viewModel); // Use object or a base ViewModel type

        /// <summary>
        /// Shows a custom Window-based dialog with the specified View and ViewModel.
        /// </summary>
        /// <typeparam name="TView">The type of View (Window) to display.</typeparam>
        /// <param name="viewModel">The ViewModel to set as the DataContext.</param>
        /// <returns>The DialogResult (true/false/null).</returns>
        Task<bool?> ShowDialogAsync<TView>(object viewModel) where TView : Window, new();

        /// <summary>
        /// Shows a confirmation dialog with custom message.
        /// </summary>
        Task<bool?> ShowConfirmationAsync(string message, string title);
    }
}
