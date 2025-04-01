using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFGrowerApp.ViewModels
{
    // Inherit from ViewModelBase
    public class DashboardViewModel : ViewModelBase 
    {
        public DashboardViewModel()
        {
            // Initialize dashboard data if needed
        }

        // Removed redundant INotifyPropertyChanged implementation
        // OnPropertyChanged is inherited from ViewModelBase
    }
}
