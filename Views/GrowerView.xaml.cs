using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WPFGrowerApp.ViewModels;
using WPFGrowerApp.DataAccess.Services;
using System.Windows.Controls;

namespace WPFGrowerApp.Views
{
    /// <summary>
    /// Interaction logic for GrowerView.xaml
    /// </summary>
    public partial class GrowerView : UserControl
    {
        private readonly GrowerViewModel _viewModel;
        public GrowerView()
        {
            InitializeComponent();
        }
        public GrowerView(IGrowerService growerService, decimal growerNumber = 0):this()
        {
            _viewModel = new GrowerViewModel(growerService);
            DataContext = _viewModel;

            Loaded += async (s, e) =>
            {
                if (growerNumber > 0)
                {
                    await _viewModel.LoadGrowerAsync(growerNumber);
                }
            };
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool result = await _viewModel.SaveGrowerAsync();
            if (result)
            {
                //DialogResult = true;
                //Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            //DialogResult = false;
            //Close();
        }
    }
}
