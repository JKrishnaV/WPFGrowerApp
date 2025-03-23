using System;
using System.Windows;
using WPFGrowerApp.DataAccess.Services;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.Views;

namespace WPFGrowerApp
{
    public partial class MainWindow : Window
    {
        private readonly GrowerService _growerService;

        public MainWindow(GrowerService growerService)
        {
            InitializeComponent();
            _growerService = growerService;
            
            // Initialize with the grower search view
            ShowGrowerSearchView();
        }

        private void ShowGrowerSearchView()
        {
            // Create the view model with the grower service
            var viewModel = new GrowerSearchViewModel(_growerService);
            
            // Create the view with the view model
            var view = new GrowerSearchView(viewModel);
            
            // Set the view as the main content
            MainContent.Content = view;
        }
    }
}
