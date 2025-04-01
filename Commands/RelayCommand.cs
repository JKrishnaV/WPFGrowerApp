using System;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFGrowerApp.Infrastructure.Logging; // Assuming Logger is accessible

namespace WPFGrowerApp.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, Task> _executeAsync;
        private readonly Predicate<object> _canExecute;
        private bool _isExecuting; // To prevent re-entrancy for async commands

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Constructor for async commands
        public RelayCommand(Func<object, Task> executeAsync, Predicate<object> canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            // Prevent execution if already running (for async)
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        public async void Execute(object parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
                return;
            }

            if (_executeAsync != null)
            {
                if (CanExecute(parameter))
                {
                    try
                    {
                        _isExecuting = true;
                        RaiseCanExecuteChanged(); // Update UI state (e.g., disable button)
                        await _executeAsync(parameter);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception from the async command execution
                        Logger.Error($"Exception during async command execution: {ex.Message}", ex);
                        // Optionally re-throw or handle differently (e.g., show message)
                        // Depending on desired behavior, might need a global exception handler
                    }
                    finally
                    {
                        _isExecuting = false;
                        RaiseCanExecuteChanged(); // Re-enable UI state
                    }
                }
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
