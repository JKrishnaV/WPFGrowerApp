using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.Views;
using System.Threading.Tasks; // Added for Task
using MaterialDesignThemes.Wpf; // Added for DialogHost
using WPFGrowerApp.Views.Dialogs; // Added for custom dialog views
using System.Linq;
using System.Reflection;

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
        GrowerSearchDialogResult IDialogService.ShowGrowerSearchDialog()
        {
            var searchView = _serviceProvider.GetRequiredService<GrowerSearchView>();
            bool? dialogResult = searchView.ShowDialog();
            if (dialogResult == true)
            {
                return new GrowerSearchDialogResult(true, searchView.SelectedGrowerNumber);
            }
            else
            {
                return new GrowerSearchDialogResult(dialogResult, null);
            }
        }

        // Implementation for showing custom ViewModels as dialogs
        public async Task ShowDialogAsync(object viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            // Get the View type based on the ViewModel type
            var viewModelType = viewModel.GetType();
            var viewType = GetViewTypeForViewModel(viewModelType);

            if (viewType == null)
                throw new InvalidOperationException($"No view type found for view model type {viewModelType.Name}");

            // Create an instance of the view
            var view = Activator.CreateInstance(viewType) as FrameworkElement;
            if (view == null)
                throw new InvalidOperationException($"Failed to create view of type {viewType.Name}");

            // Set the DataContext
            view.DataContext = viewModel;

            // Show the dialog directly
            await DialogHost.Show(view, RootDialogHostId);
        }

        /// <summary>
        /// Shows a Window-based dialog (not Material Design DialogHost).
        /// Used for complex dialogs like ReceiptEntryView.
        /// </summary>
        public async Task<bool?> ShowDialogAsync<TView>(object viewModel) where TView : Window, new()
        {
            return await Task.Run(() =>
            {
                return Application.Current.Dispatcher.Invoke(() =>
                {
                    var view = new TView
                    {
                        DataContext = viewModel,
                        Owner = Application.Current.MainWindow
                    };

                    return view.ShowDialog();
                });
            });
        }

        /// <summary>
        /// Shows an input dialog for text entry.
        /// </summary>
        public async Task<string?> ShowInputDialogAsync(string message, string title)
        {
            var result = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var inputWindow = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Application.Current.MainWindow,
                    ResizeMode = ResizeMode.NoResize
                };

                var stackPanel = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20)
                };

                var messageBlock = new System.Windows.Controls.TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var inputBox = new System.Windows.Controls.TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var buttonPanel = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var okButton = new System.Windows.Controls.Button
                {
                    Content = "OK",
                    Width = 80,
                    Margin = new Thickness(0, 0, 10, 0),
                    IsDefault = true
                };
                okButton.Click += (s, e) =>
                {
                    inputWindow.DialogResult = true;
                    inputWindow.Close();
                };

                var cancelButton = new System.Windows.Controls.Button
                {
                    Content = "Cancel",
                    Width = 80,
                    IsCancel = true
                };
                cancelButton.Click += (s, e) =>
                {
                    inputWindow.DialogResult = false;
                    inputWindow.Close();
                };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);

                stackPanel.Children.Add(messageBlock);
                stackPanel.Children.Add(inputBox);
                stackPanel.Children.Add(buttonPanel);

                inputWindow.Content = stackPanel;

                var dialogResult = inputWindow.ShowDialog();
                return dialogResult == true ? inputBox.Text : null;
            });

            return result;
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// </summary>
        public async Task<bool?> ShowConfirmationAsync(string message, string title)
        {
            return await ShowConfirmationDialogAsync(message, title);
        }

        private Type GetViewTypeForViewModel(Type viewModelType)
        {
            // Get the assembly containing the views
            var assembly = Assembly.GetAssembly(typeof(Views.Dialogs.MessageDialogView));

            // Get the namespace for dialog views
            var dialogNamespace = "WPFGrowerApp.Views.Dialogs";

            // Get the expected view name by replacing \"ViewModel\" with \"View\"
            var viewModelName = viewModelType.Name;
            var expectedViewName = viewModelName.Replace("ViewModel", "View");

            // Try to find the view type in the dialog namespace
            var viewType = assembly.GetType($"{dialogNamespace}.{expectedViewName}");

            if (viewType == null)
            {
                // If not found, try to find it in the main views namespace
                viewType = assembly.GetType($"WPFGrowerApp.Views.{expectedViewName}");
            }

            return viewType;
        }
    }
}