using CommunityToolkit.Mvvm.ComponentModel;

namespace WPFGrowerApp.ViewModels
{
    public abstract partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;
    }
}
