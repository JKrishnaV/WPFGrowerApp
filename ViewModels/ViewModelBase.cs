using System.Collections.Generic; // Added for EqualityComparer
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        /// <summary>
        /// Sets property if it does not equal existing value. Notifies listeners if change occurs.
        /// </summary>
        /// <typeparam name="T">Type of property.</typeparam>
        /// <param name="member">The address of the member field.</param>
        /// <param name="value">New value for property.</param>
        /// <param name="propertyName">Name of property used to notify listeners. This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        /// <returns>True if value was changed, false if existing value matched.</returns>
        protected virtual bool SetProperty<T>(ref T member, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(member, value))
            {
                return false;
            }

            member = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
