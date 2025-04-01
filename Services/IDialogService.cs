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
        void ShowMessageBox(string message, string title, DialogResult buttons = DialogResult.OK); // Simple version for now
        
        // Example for a more complex message box
        // Task<DialogResult> ShowMessageBoxAsync(string message, string title, DialogResult buttons); 

        // Method specific to the Grower Search interaction
        (bool? DialogResult, decimal? SelectedGrowerNumber) ShowGrowerSearchDialog(); 
    }
}
