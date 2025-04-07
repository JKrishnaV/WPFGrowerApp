using System.Threading.Tasks;

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

        // Method specific to the Grower Search interaction (remains synchronous for now)
        (bool? DialogResult, decimal? SelectedGrowerNumber) ShowGrowerSearchDialog(); 
    }
}
