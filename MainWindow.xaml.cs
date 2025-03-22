using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.Views;
using WPFGrowerApp.DataAccess.Services;

namespace WPFGrowerApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IGrowerService _growerService;

        public MainWindow(IServiceProvider serviceProvider, IGrowerService growerService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _growerService = growerService;
        }

        private void SearchGrower_Click(object sender, RoutedEventArgs e)
        {
            var searchView = new GrowerSearchView(_growerService);
            bool? result = searchView.ShowDialog();

            if (result == true && searchView.SelectedGrowerNumber.HasValue)
            {
                OpenGrowerView(searchView.SelectedGrowerNumber.Value);
            }
        }

        private void NewGrower_Click(object sender, RoutedEventArgs e)
        {
            OpenGrowerView();
        }

        private void OpenGrowerView(decimal growerNumber = 0)
        {
            var growerView = new GrowerView(_growerService, growerNumber);
            growerView.ShowDialog();
        }
    }
}
