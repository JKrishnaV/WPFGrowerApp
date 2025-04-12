using System;
using System.Windows;
using System.Windows.Controls;
using WPFGrowerApp.DataAccess.Models;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Services; // For IDialogService access via ViewModel
using WPFGrowerApp.DataAccess.Interfaces; // For IPayGroupService access via ViewModel

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for PaymentGroupView.xaml
    /// </summary>
    public partial class PaymentGroupView : UserControl
    {
        private PaymentGroupViewModel _viewModel;
        // Direct service references are generally discouraged in code-behind with MVVM,
        // but we access them via the ViewModel as per the plan.
        private IPayGroupService _payGroupService => _viewModel?.GetService<IPayGroupService>();
        private IDialogService _dialogService => _viewModel?.GetService<IDialogService>();


        public PaymentGroupView()
        {
            InitializeComponent();
            // Ensure ViewModel is ready when the control loads
            this.Loaded += PaymentGroupView_Loaded;
        }

        private void PaymentGroupView_Loaded(object sender, RoutedEventArgs e)
        {
            // Get the ViewModel from the DataContext
            _viewModel = DataContext as PaymentGroupViewModel;
            if (_viewModel == null)
            {
                // Handle error: ViewModel not set or of wrong type
                MessageBox.Show("ViewModel is not set correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
             // Attempt to load data if not already loading
            if (_viewModel != null && !_viewModel.IsLoading && _viewModel.LoadPayGroupsCommand.CanExecute(null))
            {
                 _viewModel.LoadPayGroupsCommand.Execute(null);
            }
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null || _dialogService == null) return;

            // Create the dialog ViewModel for adding
            var dialogViewModel = new ViewModels.Dialogs.PayGroupEditDialogViewModel(); 

            // Show the dialog using the DialogService
            await _dialogService.ShowDialogAsync(dialogViewModel);

            // Check if the user saved the dialog
            if (dialogViewModel.WasSaved && _payGroupService != null) // Ensure service is available
            {
                try
                {
                    bool success = await _payGroupService.AddPayGroupAsync(dialogViewModel.PayGroupData);
                    if (success)
                    {
                        await _dialogService.ShowMessageBoxAsync("Success", "Payment Group added successfully.");
                        // Refresh the list
                        if (_viewModel?.LoadPayGroupsCommand?.CanExecute(null) == true)
                        {
                            _viewModel.LoadPayGroupsCommand.Execute(null);
                        }
                    }
                    else
                    {
                        // Check if the error might be due to duplicate ID
                        var existing = await _payGroupService.GetPayGroupByIdAsync(dialogViewModel.PayGroupData.PayGroupId);
                        if (existing != null) {
                             await _dialogService.ShowMessageBoxAsync("Error", $"Failed to add Payment Group. ID '{dialogViewModel.PayGroupData.PayGroupId}' already exists.");
                        } else {
                             await _dialogService.ShowMessageBoxAsync("Error", "Failed to add the Payment Group.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error
                    Infrastructure.Logging.Logger.Error($"Error adding Payment Group: {ex.Message}", ex);
                    await _dialogService.ShowMessageBoxAsync("Error", $"An error occurred while adding: {ex.Message}");
                }
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
             if (_viewModel == null || _dialogService == null || _payGroupService == null) return;

            if (sender is Button button && button.CommandParameter is PayGroup payGroupToEdit)
            {
                 // Create the dialog ViewModel for editing, passing the selected item
                var dialogViewModel = new ViewModels.Dialogs.PayGroupEditDialogViewModel(payGroupToEdit);

                // Show the dialog
                await _dialogService.ShowDialogAsync(dialogViewModel);

                // Check if the user saved
                if (dialogViewModel.WasSaved)
                {
                     try
                    {
                        bool success = await _payGroupService.UpdatePayGroupAsync(dialogViewModel.PayGroupData);
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Success", "Payment Group updated successfully.");
                            // Refresh the list
                            if (_viewModel?.LoadPayGroupsCommand?.CanExecute(null) == true)
                            {
                                _viewModel.LoadPayGroupsCommand.Execute(null);
                            }
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Error", "Failed to update the Payment Group.");
                        }
                    }
                    catch (Exception ex)
                    {
                         // Log error
                        Infrastructure.Logging.Logger.Error($"Error updating Payment Group {payGroupToEdit.PayGroupId}: {ex.Message}", ex);
                        await _dialogService.ShowMessageBoxAsync("Error", $"An error occurred while updating: {ex.Message}");
                    }
                }
            }
             else
            {
                await _dialogService.ShowMessageBoxAsync("Edit Error", "Could not determine the payment group to edit.");
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null || _payGroupService == null || _dialogService == null) return;

            if (sender is Button button && button.CommandParameter is PayGroup payGroupToDelete)
            {
                string message = $"Are you sure you want to delete Payment Group '{payGroupToDelete.PayGroupId}' ({payGroupToDelete.Description})?";
                bool confirmed = await _dialogService.ShowConfirmationDialogAsync(message, "Confirm Delete");

                if (confirmed)
                {
                    try
                    {
                        bool success = await _payGroupService.DeletePayGroupAsync(payGroupToDelete.PayGroupId);
                        if (success)
                        {
                            await _dialogService.ShowMessageBoxAsync("Success", $"Payment Group '{payGroupToDelete.PayGroupId}' deleted successfully.");
                            // Refresh the list
                            if (_viewModel.LoadPayGroupsCommand.CanExecute(null))
                            {
                                _viewModel.LoadPayGroupsCommand.Execute(null);
                            }
                        }
                        else
                        {
                            await _dialogService.ShowMessageBoxAsync("Error", $"Failed to delete Payment Group '{payGroupToDelete.PayGroupId}'. It might be in use or already deleted.");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error (assuming a logger exists or add one)
                        await _dialogService.ShowMessageBoxAsync("Deletion Error", $"An error occurred while deleting: {ex.Message}");
                    }
                }
            }
             else
            {
                 await _dialogService.ShowMessageBoxAsync("Delete Error", "Could not determine the payment group to delete.");
            }
        }
    }

    // Add extension method to ViewModel base or specific ViewModel if needed,
    // or adjust how services are accessed if DI container is directly accessible.
    // This is a common pattern if the ViewModel acts as a service locator for the View.
    public static class ViewModelExtensions
    {
        // Change the extension method to target the correct ViewModelBase
        public static T GetService<T>(this ViewModelBase viewModel) where T : class 
        {
            // This is a placeholder. The actual implementation depends on how
            // services are managed and accessible from the ViewModel.
            // Option 1: ViewModel exposes services directly via properties.
            // Option 2: Use a Service Locator pattern if available.
            // Option 3: Pass services via constructor (already done for VM, but View needs access).

            // Example assuming ViewModel holds references (adjust based on actual BaseViewModel/setup)
            var vmType = viewModel.GetType();
            var field = vmType.GetField($"_{typeof(T).Name.Substring(1).ToLower()}Service", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
             if (field != null) return field.GetValue(viewModel) as T;

            // Fallback or throw if service not found
             // This might indicate a design issue if the View needs direct service access often.
             // Consider commands in ViewModel instead.
             if (typeof(T) == typeof(IDialogService))
             {
                 var dialogField = vmType.GetField("_dialogService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 return dialogField?.GetValue(viewModel) as T;
             }
             if (typeof(T) == typeof(IPayGroupService))
             {
                 var payGroupField = vmType.GetField("_payGroupService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 return payGroupField?.GetValue(viewModel) as T;
             }


            return null; // Or throw exception
        }
    }
}
