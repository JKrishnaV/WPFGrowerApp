using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        public DashboardViewModel()
        {
            // Initialize dashboard data if needed
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
