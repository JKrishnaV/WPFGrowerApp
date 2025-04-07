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
        /// Shows a simple message box with an OK button.
        /// </summary>
        void ShowMessageBox(string message, string title); 

        /// <summary>
        /// Shows a confirmation message box with Yes/No buttons.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        /// <returns>True if the user clicked Yes, False otherwise.</returns>
        bool ShowConfirmationDialog(string message, string title);

        // Method specific to the Grower Search interaction
        (bool? DialogResult, decimal? SelectedGrowerNumber) ShowGrowerSearchDialog(); 
    }
}
