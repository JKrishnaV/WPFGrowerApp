using System; // Added for IServiceProvider
using System.Windows;
using Microsoft.Extensions.DependencyInjection; // Added for GetRequiredService
using WPFGrowerApp.Views; 

namespace WPFGrowerApp.Services
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        // Inject IServiceProvider
        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Maps our enum to WPF MessageBoxButton (can be expanded)
        private MessageBoxButton GetMessageBoxButton(DialogResult buttons)
        {
            switch (buttons)
            {
                // Add cases for YesNo, YesNoCancel etc. if needed
                case DialogResult.OK:
                default:
                    return MessageBoxButton.OK;
            }
        }

        // Maps our enum to WPF MessageBoxImage (can be expanded)
        private MessageBoxImage GetMessageBoxImage(string title)
        {
            if (title.Contains("Error")) return MessageBoxImage.Error;
            if (title.Contains("Warning")) return MessageBoxImage.Warning;
            if (title.Contains("Information") || title.Contains("Success")) return MessageBoxImage.Information;
            return MessageBoxImage.None; // Default
        }


        public void ShowMessageBox(string message, string title, DialogResult buttons = DialogResult.OK)
        {
            MessageBox.Show(message, title, GetMessageBoxButton(buttons), GetMessageBoxImage(title));
        }

        public (bool? DialogResult, decimal? SelectedGrowerNumber) ShowGrowerSearchDialog()
        {
            // Resolve the view using the DI container
            var searchView = _serviceProvider.GetRequiredService<GrowerSearchView>(); 
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
