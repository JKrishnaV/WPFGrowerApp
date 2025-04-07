using System; 
using System.Windows;
using Microsoft.Extensions.DependencyInjection; 
using WPFGrowerApp.Views; 
using System.Threading.Tasks; // Added for Task
using MaterialDesignThemes.Wpf; // Added for DialogHost
using WPFGrowerApp.Views.Dialogs; // Added for custom dialog views

namespace WPFGrowerApp.Services
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;
        private const string RootDialogHostId = "RootDialogHost"; // Identifier for the DialogHost in MainWindow

        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // ShowMessageBoxAsync using Material Design DialogHost
        public async Task ShowMessageBoxAsync(string message, string title) 
        {
            var view = new MessageDialogView();
            view.SetContent(message, title); // Use the method we added to set content

            // Show the view as a dialog using the DialogHost
            await DialogHost.Show(view, RootDialogHostId); 
            // We don't need to return anything for a simple message box
        }

        // ShowConfirmationDialogAsync using Material Design DialogHost
        public async Task<bool> ShowConfirmationDialogAsync(string message, string title)
        {
            var view = new ConfirmationDialogView();
            view.SetContent(message, title); // Use the method we added

            // Show the view as a dialog and wait for the result
            var result = await DialogHost.Show(view, RootDialogHostId);

            // Check if the result is the string "True"
            return result is string stringResult && stringResult.Equals("True", StringComparison.OrdinalIgnoreCase);
        }

        // ShowGrowerSearchDialog remains synchronous for now, using standard Window.ShowDialog()
        public (bool? DialogResult, decimal? SelectedGrowerNumber) ShowGrowerSearchDialog()
        {
            // Resolve the view using the DI container
            // This ensures the view and its ViewModel are constructed via DI
            var searchView = _serviceProvider.GetRequiredService<GrowerSearchView>(); 
            
            // Consider setting owner if applicable (e.g., Application.Current.MainWindow)
            // searchView.Owner = Application.Current.MainWindow; 

            bool? dialogResult = searchView.ShowDialog(); 
            
            if (dialogResult == true)
            {
                return (true, searchView.SelectedGrowerNumber);
            }
            else
            {
                return (dialogResult, null); // Return false or null result, null grower number
            }
        }
    }
}
