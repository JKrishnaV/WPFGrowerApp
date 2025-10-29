using System;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel;
using WPFGrowerApp.Helpers;
using WPFGrowerApp.Infrastructure.Logging;
using WPFGrowerApp.Views.Reports;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for ReportsHostView.xaml
    /// </summary>
    public partial class ReportsHostView : UserControl
    {
        private ViewModels.ReportsHostViewModel _viewModel;
        private UserControl _currentView;

        public ReportsHostView()
        {
            InitializeComponent();
            ThemeHelper.EnableThemeSupport(this);
            this.DataContextChanged += ReportsHostView_DataContextChanged;
        }

        private void ReportsHostView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Logger.Info($"ReportsHostView DataContext changed from {e.OldValue?.GetType().Name} to {e.NewValue?.GetType().Name}");
            
            if (e.NewValue != null)
            {
                _viewModel = e.NewValue as ViewModels.ReportsHostViewModel;
                if (_viewModel != null)
                {
                    Logger.Info("ReportsHostView subscribed to PropertyChanged events");
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    
                    // Test the subscription immediately
                    Logger.Info("Testing PropertyChanged subscription immediately after subscription");
                    _viewModel.CurrentReportViewModel = _viewModel.CurrentReportViewModel;
                    
                    // Show the initial view
                    ShowCurrentView();
                    
                    // Test the subscription again after a delay to see if it's still working
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => 
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Logger.Info("Testing PropertyChanged subscription after 1 second delay");
                            _viewModel.CurrentReportViewModel = _viewModel.CurrentReportViewModel;
                        });
                    });
                }
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Logger.Info($"ReportsHostView received PropertyChanged event for property: {e.PropertyName}");
            
            if (e.PropertyName == nameof(ViewModels.ReportsHostViewModel.CurrentReportViewModel))
            {
                Logger.Info($"CurrentReportViewModel changed to: {_viewModel?.CurrentReportViewModel?.GetType().Name}");
                ShowCurrentView();
            }
        }

        private void ShowCurrentView()
        {
            if (_viewModel?.CurrentReportViewModel == null)
            {
                Logger.Warn("CurrentReportViewModel is null, cannot show view");
                return;
            }

            try
            {
                Logger.Info($"Showing view for ViewModel: {_viewModel.CurrentReportViewModel.GetType().Name}");

                // Clear existing content
                ReportContentGrid.Children.Clear();
                _currentView = null;

                // Create the appropriate view based on the ViewModel type
                switch (_viewModel.CurrentReportViewModel)
                {
                    case ViewModels.ReportsCardsViewModel:
                        _currentView = new ReportsCardsView();
                        break;
                    case ViewModels.ReportsViewModel:
                        _currentView = new ReportsView();
                        break;
                    case ViewModels.PaymentSummaryReportViewModel:
                        _currentView = new PaymentSummaryReportView();
                        break;
                    case ViewModels.GrowerReportViewModel:
                        _currentView = new GrowerReportView();
                        break;
                    default:
                        Logger.Warn($"Unknown ViewModel type: {_viewModel.CurrentReportViewModel.GetType().Name}");
                        return;
                }

                // Set the DataContext and add to the grid
                if (_currentView != null)
                {
                    _currentView.DataContext = _viewModel.CurrentReportViewModel;
                    ReportContentGrid.Children.Add(_currentView);
                    Logger.Info($"Successfully displayed view: {_currentView.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error showing current view", ex);
            }
        }
    }
}
