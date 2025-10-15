using System;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.Commands;
using WPFGrowerApp.Services;

namespace WPFGrowerApp.ViewModels
{
    /// <summary>
    /// Host ViewModel for grower management with parent-child navigation.
    /// Manages switching between GrowerListView and GrowerDetailView.
    /// </summary>
    public class GrowerManagementHostViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDialogService _dialogService;
        private ViewModelBase _currentChildView;
        private bool _isShowingList = true;
        private string _currentBreadcrumbText = "Growers";
        private string _currentGrowerDisplayText = string.Empty;

        public GrowerManagementHostViewModel(
            IServiceProvider serviceProvider,
            IDialogService dialogService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Initialize commands
            NavigateToDashboardCommand = new RelayCommand(ExecuteNavigateToDashboard);
            NavigateToListCommand = new RelayCommand(ExecuteNavigateToList);

            // Initialize with list view
            NavigateToList();
        }

        #region Properties

        public ViewModelBase CurrentChildView
        {
            get => _currentChildView;
            set
            {
                if (_currentChildView != value)
                {
                    _currentChildView = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShowingList
        {
            get => _isShowingList;
            set
            {
                if (_isShowingList != value)
                {
                    _isShowingList = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsShowingDetail));
                    UpdateBreadcrumb();
                }
            }
        }

        public string CurrentBreadcrumbText
        {
            get => _currentBreadcrumbText;
            set
            {
                if (_currentBreadcrumbText != value)
                {
                    _currentBreadcrumbText = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsShowingDetail
        {
            get => !_isShowingList;
        }

        public string CurrentGrowerDisplayText
        {
            get => _currentGrowerDisplayText;
            set
            {
                if (_currentGrowerDisplayText != value)
                {
                    _currentGrowerDisplayText = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToListCommand { get; }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigates to the grower list view.
        /// </summary>
        public void NavigateToList()
        {
            try
            {
                var listViewModel = _serviceProvider.GetRequiredService<GrowerListViewModel>();
                
                // Set parent reference for navigation back to this host
                if (listViewModel is GrowerListViewModel growerListVm)
                {
                    growerListVm.SetParentHost(this);
                }

                CurrentChildView = listViewModel;
                IsShowingList = true;
                UpdateBreadcrumb();
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error navigating to grower list view", ex);
                _dialogService.ShowMessageBoxAsync($"Error loading grower list: {ex.Message}", "Navigation Error");
            }
        }

        /// <summary>
        /// Navigates to the grower detail view.
        /// </summary>
        /// <param name="growerId">The ID of the grower to view/edit, null for new grower</param>
        /// <param name="isEditMode">True for edit mode, false for view mode</param>
        public async void NavigateToDetail(int? growerId = null, bool isEditMode = false)
        {
            try
            {
                var detailViewModel = _serviceProvider.GetRequiredService<GrowerDetailViewModel>();
                
                // Set parent reference for navigation back to this host
                if (detailViewModel is GrowerDetailViewModel growerDetailVm)
                {
                    growerDetailVm.SetParentHost(this);
                }

                // Set the child view first so UI can bind
                CurrentChildView = detailViewModel;
                IsShowingList = false;
                UpdateBreadcrumb(growerId, isEditMode);

                // Initialize the detail view with grower data asynchronously
                await InitializeDetailViewAsync(detailViewModel, growerId, isEditMode);
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error navigating to grower detail view", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading grower detail: {ex.Message}", "Navigation Error");
            }
        }

        /// <summary>
        /// Navigates back to the list view from detail view.
        /// </summary>
        public void NavigateBackToList()
        {
            NavigateToList();
        }

        #endregion

        #region Private Methods

        private async System.Threading.Tasks.Task InitializeDetailViewAsync(GrowerDetailViewModel detailViewModel, int? growerId, bool isEditMode)
        {
            try
            {
                if (growerId.HasValue && growerId.Value > 0)
                {
                    // Load existing grower
                    await detailViewModel.LoadGrowerAsync(growerId.Value, isEditMode);
                    
                    // Update breadcrumb with grower name after loading
                    if (detailViewModel.CurrentGrower != null)
                    {
                        var growerName = detailViewModel.CurrentGrower.GrowerName ?? detailViewModel.CurrentGrower.FullName;
                        CurrentGrowerDisplayText = $"Grower #{growerId}-{growerName}";
                    }
                }
                else
                {
                    // Create new grower
                    detailViewModel.CreateNewGrower();
                    CurrentGrowerDisplayText = "New Grower";
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error initializing detail view", ex);
                await _dialogService.ShowMessageBoxAsync($"Error loading grower data: {ex.Message}", "Load Error");
            }
        }

        private void UpdateBreadcrumb(int? growerId = null, bool isEditMode = false)
        {
            if (IsShowingList)
            {
                CurrentBreadcrumbText = "Growers";
                CurrentGrowerDisplayText = string.Empty;
            }
            else
            {
                if (growerId.HasValue && growerId.Value > 0)
                {
                    var action = isEditMode ? "Edit" : "View";
                    CurrentBreadcrumbText = $"{action} Grower #{growerId}";
                    // For now, we'll set a placeholder. The actual grower name will be loaded asynchronously
                    CurrentGrowerDisplayText = $"Grower #{growerId}";
                }
                else
                {
                    CurrentBreadcrumbText = "New Grower";
                    CurrentGrowerDisplayText = "New Grower";
                }
            }
        }

        private void ExecuteNavigateToDashboard(object parameter)
        {
            try
            {
                // Get the MainViewModel and navigate to dashboard
                var mainViewModel = _serviceProvider.GetService<MainViewModel>();
                if (mainViewModel != null)
                {
                    mainViewModel.NavigateToDashboardCommand.Execute(null);
                }
                else
                {
                    Infrastructure.Logging.Logger.Warn("Could not get MainViewModel reference for dashboard navigation");
                }
            }
            catch (Exception ex)
            {
                Infrastructure.Logging.Logger.Error("Error navigating to dashboard", ex);
            }
        }

        private void ExecuteNavigateToList(object parameter)
        {
            NavigateToList();
        }

        #endregion
    }
}
